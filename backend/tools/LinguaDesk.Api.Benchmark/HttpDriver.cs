using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace LinguaDesk.Api.Benchmark;

/// <summary>
/// Per-stage durations. The synchronous operation API exposes no internal
/// stage timing boundary, so rehearsal observations carry the authoritative
/// submission-to-complete-body clock while stages stay explicitly unknown
/// with provenance instead of invented partitions.
/// </summary>
public sealed record StageDurations(
    double? AdmissionMs,
    double? EligibilityMs,
    double? TransformationMs,
    double? SettlementMs);

/// <summary>
/// One measured request. Carries hashes, lengths, counts and cohorts only:
/// never source text, result text, tokens or secrets.
/// </summary>
public sealed record RequestObservation(
    int SequenceIndex,
    string ComboId,
    string Family,
    string Route,
    string BandId,
    string? SourceSelection,
    string SourceSha256,
    int SourceLength,
    bool IsRepeat,
    bool FirstSeen,
    string OperationId,
    bool SubmittedFresh,
    DateTimeOffset SubmittedAtUtc,
    double TotalMs,
    int HttpStatus,
    string Outcome,
    string? FailureCategory,
    int? CharacterCount,
    string? AdmissionDay,
    string? UsageJson,
    int? OutputLength,
    string? OutputSha256,
    string CacheCohort,
    StageDurations Stages,
    string StageProvenance);

public sealed record DriverRun(
    IReadOnlyList<RequestObservation> Observations,
    int MaxInFlight,
    double DrainMs,
    DateTimeOffset StartedUtc,
    DateTimeOffset FinishedUtc);

/// <summary>
/// Real-HTTP driver: registers and signs in one local account through the
/// real auth boundary, then submits every work item with a fresh UUIDv7
/// submission identity under bounded concurrency. The driver keeps no
/// response cache: reuse is disabled by construction.
/// </summary>
public sealed class BenchmarkHttpDriver
{
    public const string StageProvenanceText =
        "synchronous-api-total-only: the synchronous operation API exposes no per-stage " +
        "timing boundary, so the submission-to-complete-body clock is authoritative and " +
        "stage durations stay unknown rather than assumed.";

    private static readonly JsonSerializerOptions BodyOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly HttpClient client;
    private readonly string baseUrl;

    public BenchmarkHttpDriver(HttpClient client, string baseUrl)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);
        this.client = client;
        this.baseUrl = baseUrl.TrimEnd('/');
    }

    public async Task RegisterAsync(string email, string password, CancellationToken cancellationToken)
    {
        var body = JsonSerializer.Serialize(new { email, password }, BodyOptions);
        using var response = await client.PostAsync(
            $"{baseUrl}/api/accounts/register",
            new StringContent(body, Encoding.UTF8, "application/json"),
            cancellationToken).ConfigureAwait(false);
        if ((int)response.StatusCode != StatusCodes.Accepted)
        {
            throw new InvalidOperationException(
                $"Account registration failed with HTTP {(int)response.StatusCode}; the rehearsal needs one verified local account.");
        }
    }

    public async Task<string> SignInAsync(string email, string password, CancellationToken cancellationToken)
    {
        var body = JsonSerializer.Serialize(new { email, password }, BodyOptions);
        using var response = await client.PostAsync(
            $"{baseUrl}/api/accounts/bearer-sign-in",
            new StringContent(body, Encoding.UTF8, "application/json"),
            cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Bearer [REDACTED] failed with HTTP {(int)response.StatusCode}; the rehearsal needs the real auth boundary.");
        }

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));
        if (!document.RootElement.TryGetProperty("accessToken", out var token)
            || token.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(token.GetString()))
        {
            throw new InvalidOperationException("Bearer [REDACTED] returned no access token.");
        }

        return token.GetString()!;
    }

    public async Task<DriverRun> RunAsync(
        IReadOnlyList<WorkItem> items,
        string accessToken,
        int concurrency,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
        if (concurrency <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(concurrency), "Concurrency must stay positive.");
        }

        var startedUtc = DateTimeOffset.UtcNow;
        using var gate = new SemaphoreSlim(concurrency, concurrency);
        var results = new ConcurrentDictionary<int, RequestObservation>();
        var inFlight = 0;
        var maxInFlight = 0;
        var startedCount = 0;
        var queueEmptyUtc = DateTimeOffset.MinValue;
        var stateLock = new object();

        var tasks = items.Select(item => Task.Run(async () =>
        {
            await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var current = Interlocked.Increment(ref inFlight);
                lock (stateLock)
                {
                    maxInFlight = Math.Max(maxInFlight, current);
                    startedCount++;
                    if (startedCount == items.Count)
                    {
                        queueEmptyUtc = DateTimeOffset.UtcNow;
                    }
                }

                var observation = await SubmitOneAsync(item, accessToken, cancellationToken).ConfigureAwait(false);
                results[item.SequenceIndex] = observation;
            }
            finally
            {
                Interlocked.Decrement(ref inFlight);
                gate.Release();
            }
        }, cancellationToken)).ToList();

        await Task.WhenAll(tasks).ConfigureAwait(false);
        var finishedUtc = DateTimeOffset.UtcNow;
        var ordered = results.OrderBy(pair => pair.Key).Select(pair => pair.Value).ToList();
        if (ordered.Count != items.Count)
        {
            throw new InvalidOperationException($"The driver lost observations: {ordered.Count} of {items.Count} recorded.");
        }

        var drainMs = queueEmptyUtc == DateTimeOffset.MinValue
            ? 0
            : Math.Max(0, (finishedUtc - queueEmptyUtc).TotalMilliseconds);
        return new DriverRun(ordered, maxInFlight, drainMs, startedUtc, finishedUtc);
    }

    private async Task<RequestObservation> SubmitOneAsync(
        WorkItem item, string accessToken, CancellationToken cancellationToken)
    {
        var operationId = Guid.CreateVersion7().ToString("D");
        var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["operationId"] = operationId,
            ["family"] = item.Family,
            ["source"] = item.SourceText,
        };
        if (item.SourceSelection is not null)
        {
            payload["sourceSelection"] = item.SourceSelection;
        }

        if (item.Family == "translation")
        {
            payload["target"] = item.Target!;
        }
        else
        {
            payload["mode"] = item.Mode!;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/api/operations")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload, BodyOptions), Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.CacheControl = new CacheControlHeaderValue { NoStore = true };

        var submittedAt = DateTimeOffset.UtcNow;
        var clock = Stopwatch.StartNew();
        try
        {
            using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var bodyBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
            clock.Stop();
            return ParseResponse(item, operationId, submittedAt, clock.Elapsed.TotalMilliseconds, (int)response.StatusCode, bodyBytes);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            clock.Stop();
            return Failure(item, operationId, submittedAt, clock.Elapsed.TotalMilliseconds, 0, "timeout");
        }
        catch (HttpRequestException)
        {
            clock.Stop();
            return Failure(item, operationId, submittedAt, clock.Elapsed.TotalMilliseconds, 0, "transport");
        }
    }

    private static RequestObservation ParseResponse(
        WorkItem item, string operationId, DateTimeOffset submittedAt, double totalMs, int status, byte[] bodyBytes)
    {
        var body = Encoding.UTF8.GetString(bodyBytes);
        if (status == StatusCodes.Created)
        {
            try
            {
                using var document = JsonDocument.Parse(body);
                var root = document.RootElement;
                var output = root.TryGetProperty("translatedText", out var translated)
                    ? translated.GetString()
                    : root.TryGetProperty("rewrittenText", out var rewritten)
                        ? rewritten.GetString()
                        : null;
                var usage = root.TryGetProperty("usage", out var usageElement) ? usageElement.GetRawText() : null;
                return new RequestObservation(
                    item.SequenceIndex, item.ComboId, item.Family, item.Route, item.BandId,
                    item.SourceSelection, item.SourceSha256, item.SourceLength,
                    item.IsRepeat, !item.IsRepeat,
                    operationId, SubmittedFresh: true, submittedAt, totalMs,
                    status, "success", null,
                    root.TryGetProperty("characterCount", out var count) ? count.GetInt32() : null,
                    root.TryGetProperty("admissionDay", out var day) ? day.GetString() : null,
                    usage,
                    output is null ? null : SourceSynthesis.CountScalars(output),
                    output is null ? null : SourceSynthesis.Sha256Hex(output),
                    "unknown",
                    new StageDurations(null, null, null, null),
                    StageProvenanceText);
            }
            catch (JsonException)
            {
                return Failure(item, operationId, submittedAt, totalMs, status, "invalid-success-body");
            }
        }

        string? category = null;
        int? characterCount = null;
        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("category", out var categoryElement)
                    && categoryElement.ValueKind == JsonValueKind.String)
                {
                    category = categoryElement.GetString();
                }

                if (root.TryGetProperty("characterCount", out var countElement)
                    && countElement.ValueKind == JsonValueKind.Number)
                {
                    characterCount = countElement.GetInt32();
                }
            }
        }
        catch (JsonException)
        {
            category = "unparsable-body";
        }

        return Failure(item, operationId, submittedAt, totalMs, status, category ?? "unknown", characterCount);
    }

    private static RequestObservation Failure(
        WorkItem item, string operationId, DateTimeOffset submittedAt, double totalMs,
        int status, string category, int? characterCount = null) =>
        new(
            item.SequenceIndex, item.ComboId, item.Family, item.Route, item.BandId,
            item.SourceSelection, item.SourceSha256, item.SourceLength,
            item.IsRepeat, !item.IsRepeat,
            operationId, SubmittedFresh: true, submittedAt, totalMs,
            status, "failure", category,
            characterCount, null, null, null, null,
            "unknown",
            new StageDurations(null, null, null, null),
            StageProvenanceText);

    private static class StatusCodes
    {
        public const int Created = 201;
        public const int Accepted = 202;
    }
}
