using System.Text.Json;
using System.Text.Json.Nodes;

namespace LinguaDesk.Infrastructure.Ai;

public enum AttemptFailureKind
{
    Blocked,
    BudgetDenied,
    ProviderFailure,
    InvalidEnvelope,
    LengthRejected,
    Timeout,
}

public sealed class ChatCompletionsAdapterException : InvalidOperationException
{
    public ChatCompletionsAdapterException(AttemptFailureKind kind, int? statusCode, string message)
        : base(message)
    {
        Kind = kind;
        StatusCode = statusCode;
    }

    public AttemptFailureKind Kind { get; }

    public int? StatusCode { get; }
}

public sealed record UsageEvidence(
    long? PromptTokens,
    long? CompletionTokens,
    long? TotalTokens,
    long? CacheHitTokens,
    long? CacheMissTokens)
{
    public bool Known => PromptTokens.HasValue && CompletionTokens.HasValue;

    public static UsageEvidence? FromResponse(JsonElement usage)
    {
        if (usage.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        static long? Read(JsonElement element, string name) =>
            element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var parsed)
                ? parsed
                : null;

        var evidence = new UsageEvidence(
            Read(usage, "prompt_tokens"),
            Read(usage, "completion_tokens"),
            Read(usage, "total_tokens"),
            Read(usage, "prompt_cache_hit_tokens"),
            Read(usage, "prompt_cache_miss_tokens"));

        return evidence.Known ? evidence : null;
    }
}

public sealed record AttemptObservation(
    string CandidateId,
    string CredentialRef,
    bool CredentialPresent,
    int DispatchCount,
    string FinishCategory,
    string ResponseText,
    UsageEvidence? Usage,
    string? ReturnedModel,
    string? ReturnedFingerprint,
    decimal ReservedUsd,
    decimal? ActualUsd);

public sealed class ChatCompletionsAdapter
{
    private readonly CandidateProfile profile;
    private readonly HttpClient httpClient;
    private int dispatchCount;

    public ChatCompletionsAdapter(CandidateProfile profile, HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(httpClient);
        profile.Validate();

        this.profile = profile;
        this.httpClient = httpClient;
    }

    public int DispatchCount => Volatile.Read(ref dispatchCount);

    public async Task<AttemptObservation> SendAsync(
        IReadOnlyList<PromptMessageSnapshot> messages,
        TransportCredential? credential,
        EvaluationBudget? budget,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messages);

        if (credential is null)
        {
            throw new ChatCompletionsAdapterException(
                AttemptFailureKind.Blocked,
                null,
                $"Candidate '{profile.CandidateId}' has no transport credential; a requested live run without a credential is blocked, never faked.");
        }

        var body = BuildRequestBody(messages);
        long estimatedInputTokens = body.Length;
        if (estimatedInputTokens > profile.Bounds.MaxInputTokens)
        {
            throw new ChatCompletionsAdapterException(
                AttemptFailureKind.LengthRejected,
                null,
                "The serialized prompt exceeds the profile input bound; the byte count is a conservative token upper bound.");
        }

        if (estimatedInputTokens + profile.Bounds.MaxOutputTokens > profile.Bounds.ContextCapacityTokens)
        {
            throw new ChatCompletionsAdapterException(
                AttemptFailureKind.LengthRejected,
                null,
                "The prompt plus maximum output exceeds the profile context capacity.");
        }

        var reservation = Reserve(profile, budget, estimatedInputTokens);

        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                new Uri(new Uri(profile.Endpoint, UriKind.Absolute), "chat/completions"));
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer",
                credential.GetApiKey());
            request.Content = new ByteArrayContent(body);
            request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            Interlocked.Increment(ref dispatchCount);
            HttpResponseMessage response;
            try
            {
                response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            }
            catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
            {
                Settle(budget, reservation, null);
                throw new ChatCompletionsAdapterException(
                    AttemptFailureKind.Timeout,
                    null,
                    $"Candidate '{profile.CandidateId}' exceeded its bounded attempt window: {exception.Message}");
            }

            using (response)
            {
                if (!response.IsSuccessStatusCode)
                {
                    Settle(budget, reservation, null);
                    throw new ChatCompletionsAdapterException(
                        AttemptFailureKind.ProviderFailure,
                        (int)response.StatusCode,
                        $"Candidate '{profile.CandidateId}' returned HTTP {(int)response.StatusCode}; only the status code is retained.");
                }

                if (response.Content.Headers.ContentLength > profile.Bounds.MaxResponseBytes)
                {
                    Settle(budget, reservation, null);
                    throw new ChatCompletionsAdapterException(
                        AttemptFailureKind.LengthRejected,
                        (int)response.StatusCode,
                        "The provider response exceeds the bounded read cap.");
                }

                var payload = await ReadBoundedAsync(response, cancellationToken).ConfigureAwait(false);
                return MapSuccess(payload, budget, reservation, (int)response.StatusCode);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            Settle(budget, reservation, null);
            throw;
        }
    }

    private byte[] BuildRequestBody(IReadOnlyList<PromptMessageSnapshot> messages)
    {
        var wire = new JsonArray();
        foreach (var message in messages)
        {
            if (!string.Equals(message.Role, "system", StringComparison.Ordinal)
                && !string.Equals(message.Role, "user", StringComparison.Ordinal))
            {
                throw new ArgumentException($"Unsupported prompt role '{message.Role}'.", nameof(messages));
            }

            wire.Add(new JsonObject
            {
                ["role"] = message.Role,
                ["content"] = message.Content,
            });
        }

        var payload = new JsonObject
        {
            ["model"] = profile.Model,
            ["messages"] = wire,
            ["temperature"] = 0,
            ["response_format"] = new JsonObject { ["type"] = "json_object" },
            ["thinking"] = new JsonObject { ["type"] = CandidateProfile.RequiredThinkingMode },
            ["max_tokens"] = profile.Bounds.MaxOutputTokens,
            ["stream"] = false,
        };

        return JsonSerializer.SerializeToUtf8Bytes(payload);
    }

    private static BudgetReservation? Reserve(CandidateProfile candidate, EvaluationBudget? budget, long estimatedInputTokens)
    {
        if (budget is null)
        {
            return null;
        }

        var upperBound = EvaluationBudget.UpperBoundUsd(
            estimatedInputTokens,
            candidate.Bounds.MaxOutputTokens,
            candidate.Billing.PeakInputPerMillionTokens,
            candidate.Billing.PeakOutputPerMillionTokens);

        if (!budget.TryReserve(upperBound, out var reservation))
        {
            throw new ChatCompletionsAdapterException(
                AttemptFailureKind.BudgetDenied,
                null,
                "The finite evaluation budget denied this dispatch; no provider call was made.");
        }

        return reservation;
    }

    private static void Settle(EvaluationBudget? budget, BudgetReservation? reservation, decimal? actualUsd)
    {
        if (budget is not null && reservation is not null)
        {
            budget.Settle(reservation, actualUsd);
        }
    }

    private async Task<byte[]> ReadBoundedAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var cap = (long)profile.Bounds.MaxResponseBytes;
        using var source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var bounded = new MemoryStream();

        var chunk = new byte[8192];
        int read;
        while ((read = await source.ReadAsync(chunk, cancellationToken).ConfigureAwait(false)) > 0)
        {
            if (bounded.Length + read > cap)
            {
                throw new ChatCompletionsAdapterException(
                    AttemptFailureKind.LengthRejected,
                    (int)response.StatusCode,
                    "The provider response exceeds the bounded read cap.");
            }

            bounded.Write(chunk, 0, read);
        }

        return bounded.ToArray();
    }

    private AttemptObservation MapSuccess(
        byte[] payload,
        EvaluationBudget? budget,
        BudgetReservation? reservation,
        int statusCode)
    {
        try
        {
            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;

            if (!root.TryGetProperty("choices", out var choices)
                || choices.ValueKind != JsonValueKind.Array
                || choices.GetArrayLength() != 1)
            {
                throw Failure(AttemptFailureKind.InvalidEnvelope, statusCode, "The provider response has no single choice.");
            }

            var choice = choices[0];
            var finish = choice.TryGetProperty("finish_reason", out var finishElement) && finishElement.ValueKind == JsonValueKind.String
                ? finishElement.GetString()
                : null;

            if (string.Equals(finish, "length", StringComparison.Ordinal))
            {
                throw Failure(AttemptFailureKind.LengthRejected, statusCode, "The provider reported a length finish; no partial success is kept.");
            }

            if (!string.Equals(finish, "stop", StringComparison.Ordinal))
            {
                throw Failure(AttemptFailureKind.InvalidEnvelope, statusCode, "The provider finish category is not a clean stop.");
            }

            if (!choice.TryGetProperty("message", out var message)
                || !message.TryGetProperty("content", out var content)
                || content.ValueKind != JsonValueKind.String)
            {
                throw Failure(AttemptFailureKind.InvalidEnvelope, statusCode, "The provider choice carries no text content.");
            }

            var text = content.GetString();
            if (string.IsNullOrWhiteSpace(text))
            {
                throw Failure(AttemptFailureKind.InvalidEnvelope, statusCode, "The provider returned empty content.");
            }

            UsageEvidence? usage = null;
            if (root.TryGetProperty("usage", out var usageElement))
            {
                usage = UsageEvidence.FromResponse(usageElement);
            }

            string? returnedModel = root.TryGetProperty("model", out var modelElement) && modelElement.ValueKind == JsonValueKind.String
                ? modelElement.GetString()
                : null;
            string? fingerprint = root.TryGetProperty("system_fingerprint", out var fingerprintElement) && fingerprintElement.ValueKind == JsonValueKind.String
                ? fingerprintElement.GetString()
                : null;

            decimal? actual = null;
            if (usage is not null)
            {
                actual = ((decimal)usage.PromptTokens!.Value * profile.Billing.PeakInputPerMillionTokens
                    + (decimal)usage.CompletionTokens!.Value * profile.Billing.PeakOutputPerMillionTokens) / 1_000_000m;
            }

            Settle(budget, reservation, actual);

            return new AttemptObservation(
                profile.CandidateId,
                profile.CredentialRef,
                CredentialPresent: true,
                DispatchCount,
                "completed",
                text,
                usage,
                returnedModel,
                fingerprint,
                reservation?.AmountUsd ?? 0m,
                actual);
        }
        catch (JsonException exception)
        {
            throw Failure(AttemptFailureKind.InvalidEnvelope, statusCode, $"The provider response is not valid JSON: {exception.Message}");
        }

        ChatCompletionsAdapterException Failure(AttemptFailureKind kind, int code, string message)
        {
            Settle(budget, reservation, null);
            return new ChatCompletionsAdapterException(kind, code, message);
        }
    }
}
