using System.Net;
using System.Text;
using System.Text.Json;
using LinguaDesk.Infrastructure.Ai;

namespace LinguaDesk.Infrastructure.Ai.Tests;

[TestClass]
public sealed class ChatCompletionsAdapterTests
{
    private const string SyntheticKey = "test-synthetic-key-plainly-fake-001";
    private const string SuccessBody =
        "{\"id\":\"chatcmpl-test\",\"object\":\"chat.completion\",\"created\":1757548800," +
        "\"model\":\"deepseek-flash\",\"system_fingerprint\":\"fp_test\"," +
        "\"choices\":[{\"index\":0,\"message\":{\"role\":\"assistant\",\"content\":\"{\\\"status\\\":\\\"eligible\\\"}\"}," +
        "\"finish_reason\":\"stop\"}]," +
        "\"usage\":{\"prompt_tokens\":120,\"completion_tokens\":12,\"total_tokens\":132," +
        "\"prompt_cache_hit_tokens\":0,\"prompt_cache_miss_tokens\":120}}";

    [TestMethod]
    public async Task RequestCarriesProfileEndpointModelAndRequiredSettings()
    {
        using var handler = new CapturingHandler(_ => SuccessResponse());
        var adapter = CreateAdapter(handler, out var profile);

        using var credential = new TransportCredential(SyntheticKey);
        await adapter.SendAsync(FixedMessages(), credential, null);

        Assert.HasCount(1, handler.Requests);
        var request = handler.Requests[0];
        Assert.AreEqual("https://api.deepseek.com/chat/completions", request.RequestUri!.ToString());
        Assert.AreEqual("Bearer", request.Headers.Authorization!.Scheme);
        Assert.AreEqual(SyntheticKey, request.Headers.Authorization.Parameter);

        using var body = JsonDocument.Parse(handler.LastRequestBody!);
        var root = body.RootElement;
        Assert.AreEqual("deepseek-flash", root.GetProperty("model").GetString());
        Assert.AreEqual(0, root.GetProperty("temperature").GetDouble());
        Assert.IsFalse(root.TryGetProperty("top_p", out _));
        Assert.IsFalse(root.TryGetProperty("tools", out _));
        Assert.AreEqual("disabled", root.GetProperty("thinking").GetProperty("type").GetString());
        Assert.AreEqual("json_object", root.GetProperty("response_format").GetProperty("type").GetString());
        Assert.AreEqual(profile.Bounds.MaxOutputTokens, root.GetProperty("max_tokens").GetInt32());
        Assert.IsFalse(root.GetProperty("stream").GetBoolean());
        Assert.HasCount(2, root.GetProperty("messages").EnumerateArray().ToArray());
        Assert.AreEqual("system", root.GetProperty("messages")[0].GetProperty("role").GetString());
        Assert.AreEqual("user", root.GetProperty("messages")[1].GetProperty("role").GetString());
    }

    [TestMethod]
    public async Task SuccessMapsToTheInternalEnvelopeWithUsage()
    {
        using var handler = new CapturingHandler(_ => SuccessResponse());
        var adapter = CreateAdapter(handler, out _);
        var budget = new EvaluationBudget(4, 1.00m);

        using var credential = new TransportCredential(SyntheticKey);
        var observation = await adapter.SendAsync(FixedMessages(), credential, budget);

        Assert.AreEqual("DeepSeek-V4.1-Flash", observation.CandidateId);
        Assert.AreEqual("deepseek", observation.CredentialRef);
        Assert.IsTrue(observation.CredentialPresent);
        Assert.AreEqual(1, observation.DispatchCount);
        Assert.AreEqual("completed", observation.FinishCategory);
        Assert.AreEqual("{\"status\":\"eligible\"}", observation.ResponseText);
        Assert.IsNotNull(observation.Usage);
        Assert.AreEqual(120, observation.Usage.PromptTokens);
        Assert.AreEqual(12, observation.Usage.CompletionTokens);
        Assert.AreEqual(132, observation.Usage.TotalTokens);
        Assert.AreEqual("deepseek-flash", observation.ReturnedModel);
        Assert.AreEqual("fp_test", observation.ReturnedFingerprint);
        Assert.IsGreaterThan(0m, observation.ReservedUsd);
        Assert.IsNotNull(observation.ActualUsd);
        Assert.IsLessThanOrEqualTo(observation.ReservedUsd, observation.ActualUsd.GetValueOrDefault(decimal.MaxValue));
        Assert.AreEqual(0m, budget.UnresolvedUsd);
    }

    [TestMethod]
    public async Task ProviderFailureKeepsStatusCodeAndMakesExactlyOneDispatch()
    {
        using var handler = new CapturingHandler(_ => ErrorResponse(
            HttpStatusCode.TooManyRequests,
            "{\"error\":{\"message\":\"SECRET-ERROR-MARKER-429\",\"type\":\"rate_limit\"}}"));
        var adapter = CreateAdapter(handler, out _);

        using var credential = new TransportCredential(SyntheticKey);
        var exception = await Assert.ThrowsExactlyAsync<ChatCompletionsAdapterException>(
            () => adapter.SendAsync(FixedMessages(), credential, null));

        Assert.AreEqual(AttemptFailureKind.ProviderFailure, exception.Kind);
        Assert.AreEqual(429, exception.StatusCode);
        Assert.IsFalse(exception.Message.Contains("SECRET-ERROR-MARKER-429", StringComparison.Ordinal));
        Assert.HasCount(1, handler.Requests);
        Assert.AreEqual(1, adapter.DispatchCount);
    }

    [TestMethod]
    public async Task ServerErrorIsNotRetriedOrHedged()
    {
        using var handler = new CapturingHandler(_ => ErrorResponse(HttpStatusCode.BadGateway, "{}"));
        var adapter = CreateAdapter(handler, out _);

        using var credential = new TransportCredential(SyntheticKey);
        await Assert.ThrowsExactlyAsync<ChatCompletionsAdapterException>(
            () => adapter.SendAsync(FixedMessages(), credential, null));

        Assert.HasCount(1, handler.Requests);
    }

    [TestMethod]
    public async Task CancelledCallIsBoundedWithNoRetry()
    {
        using var handler = new CapturingHandler(_ => SuccessResponse());
        var adapter = CreateAdapter(handler, out _);
        var budget = new EvaluationBudget(4, 1.00m);
        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        using var credential = new TransportCredential(SyntheticKey);
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => adapter.SendAsync(FixedMessages(), credential, budget, source.Token));

        Assert.AreEqual(1, adapter.DispatchCount);
        Assert.HasCount(1, handler.Requests);
        Assert.IsGreaterThan(0m, budget.UnresolvedUsd);
    }

    [TestMethod]
    public async Task OversizedResponseHeaderIsRejectedBeforeReading()
    {
        using var handler = new CapturingHandler(_ =>
        {
            var response = SuccessResponse();
            response.Content.Headers.ContentLength = 1_000_000_000;
            return response;
        });
        var adapter = CreateAdapter(handler, out _);

        using var credential = new TransportCredential(SyntheticKey);
        var exception = await Assert.ThrowsExactlyAsync<ChatCompletionsAdapterException>(
            () => adapter.SendAsync(FixedMessages(), credential, null));

        Assert.AreEqual(AttemptFailureKind.LengthRejected, exception.Kind);
    }

    [TestMethod]
    public async Task OversizedResponseBodyIsRejectedAtTheCap()
    {
        var profile = CandidateRegistry.Default[0] with
        {
            Bounds = CandidateRegistry.Default[0].Bounds with { MaxResponseBytes = 32 },
        };
        using var handler = new CapturingHandler(_ => SuccessResponse());
        using var client = new HttpClient(handler);
        var adapter = new ChatCompletionsAdapter(profile, client);

        using var credential = new TransportCredential(SyntheticKey);
        var exception = await Assert.ThrowsExactlyAsync<ChatCompletionsAdapterException>(
            () => adapter.SendAsync(FixedMessages(), credential, null));

        Assert.AreEqual(AttemptFailureKind.LengthRejected, exception.Kind);
    }

    [TestMethod]
    public async Task LengthFinishKeepsNoPartialSuccess()
    {
        using var handler = new CapturingHandler(_ => JsonResponse(
            "{\"choices\":[{\"message\":{\"role\":\"assistant\",\"content\":\"{\\\"status\\\":\\\"elig\"},\"finish_reason\":\"length\"}]," +
            "\"usage\":{\"prompt_tokens\":10,\"completion_tokens\":256,\"total_tokens\":266}}"));
        var adapter = CreateAdapter(handler, out _);

        using var credential = new TransportCredential(SyntheticKey);
        var exception = await Assert.ThrowsExactlyAsync<ChatCompletionsAdapterException>(
            () => adapter.SendAsync(FixedMessages(), credential, null));

        Assert.AreEqual(AttemptFailureKind.LengthRejected, exception.Kind);
    }

    [TestMethod]
    public async Task EmptyContentIsAnInvalidEnvelope()
    {
        using var handler = new CapturingHandler(_ => JsonResponse(
            "{\"choices\":[{\"message\":{\"role\":\"assistant\",\"content\":\"  \"},\"finish_reason\":\"stop\"}]," +
            "\"usage\":{\"prompt_tokens\":10,\"completion_tokens\":1,\"total_tokens\":11}}"));
        var adapter = CreateAdapter(handler, out _);

        using var credential = new TransportCredential(SyntheticKey);
        var exception = await Assert.ThrowsExactlyAsync<ChatCompletionsAdapterException>(
            () => adapter.SendAsync(FixedMessages(), credential, null));

        Assert.AreEqual(AttemptFailureKind.InvalidEnvelope, exception.Kind);
    }

    [TestMethod]
    public async Task MissingUsageRetainsTheConservativeReservation()
    {
        using var handler = new CapturingHandler(_ => JsonResponse(
            "{\"choices\":[{\"message\":{\"role\":\"assistant\",\"content\":\"{\\\"status\\\":\\\"eligible\\\"}\"},\"finish_reason\":\"stop\"}]," +
            "\"model\":\"deepseek-flash\"}"));
        var adapter = CreateAdapter(handler, out _);
        var budget = new EvaluationBudget(4, 1.00m);

        using var credential = new TransportCredential(SyntheticKey);
        var observation = await adapter.SendAsync(FixedMessages(), credential, budget);

        Assert.AreEqual("completed", observation.FinishCategory);
        Assert.IsNull(observation.Usage);
        Assert.IsNull(observation.ActualUsd);
        Assert.AreEqual(observation.ReservedUsd, budget.UnresolvedUsd);
    }

    [TestMethod]
    public async Task MissingCredentialBlocksWithNoDispatch()
    {
        using var handler = new CapturingHandler(_ => SuccessResponse());
        var adapter = CreateAdapter(handler, out _);

        var exception = await Assert.ThrowsExactlyAsync<ChatCompletionsAdapterException>(
            () => adapter.SendAsync(FixedMessages(), null, null));

        Assert.AreEqual(AttemptFailureKind.Blocked, exception.Kind);
        Assert.HasCount(0, handler.Requests);
        Assert.AreEqual(0, adapter.DispatchCount);
    }

    [TestMethod]
    public async Task DeniedBudgetBlocksWithNoDispatch()
    {
        using var handler = new CapturingHandler(_ => SuccessResponse());
        var adapter = CreateAdapter(handler, out _);
        var budget = new EvaluationBudget(4, 0.000001m);

        using var credential = new TransportCredential(SyntheticKey);
        var exception = await Assert.ThrowsExactlyAsync<ChatCompletionsAdapterException>(
            () => adapter.SendAsync(FixedMessages(), credential, budget));

        Assert.AreEqual(AttemptFailureKind.BudgetDenied, exception.Kind);
        Assert.HasCount(0, handler.Requests);
        Assert.AreEqual(0, budget.DispatchesUsed);
    }

    [TestMethod]
    public void UnsupportedSettingsAreRejectedAtConstruction()
    {
        var profile = CandidateRegistry.Default[0] with
        {
            Settings = CandidateRegistry.Default[0].Settings with { TopP = 0.5 },
        };
        using var client = new HttpClient(new CapturingHandler(_ => SuccessResponse()));

        Assert.ThrowsExactly<CandidateProfileException>(
            () => new ChatCompletionsAdapter(profile, client));
    }

    [TestMethod]
    public async Task OversizedPromptIsRejectedBeforeDispatch()
    {
        var profile = CandidateRegistry.Default[0] with
        {
            Bounds = CandidateRegistry.Default[0].Bounds with { MaxInputTokens = 8 },
        };
        using var handler = new CapturingHandler(_ => SuccessResponse());
        using var client = new HttpClient(handler);
        var adapter = new ChatCompletionsAdapter(profile, client);

        using var credential = new TransportCredential(SyntheticKey);
        var exception = await Assert.ThrowsExactlyAsync<ChatCompletionsAdapterException>(
            () => adapter.SendAsync(FixedMessages(), credential, null));

        Assert.AreEqual(AttemptFailureKind.LengthRejected, exception.Kind);
        Assert.HasCount(0, handler.Requests);
    }

    private static IReadOnlyList<PromptMessageSnapshot> FixedMessages() =>
    [
        new PromptMessageSnapshot("system", "Return JSON only."),
        new PromptMessageSnapshot("user", "{\"source\":\"The sign says Welcome.\"}"),
    ];

    private static ChatCompletionsAdapter CreateAdapter(CapturingHandler handler, out CandidateProfile profile)
    {
        profile = CandidateRegistry.Default[0];
        return new ChatCompletionsAdapter(profile, new HttpClient(handler));
    }

    private static HttpResponseMessage SuccessResponse() => JsonResponse(SuccessBody);

    private static HttpResponseMessage JsonResponse(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json"),
    };

    private static HttpResponseMessage ErrorResponse(HttpStatusCode status, string json) => new(status)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json"),
    };

    private sealed class CapturingHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> responder = responder;

        public List<HttpRequestMessage> Requests { get; } = [];

        public string? LastRequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add(request);
            if (request.Content is not null)
            {
                LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            }

            return responder(request);
        }
    }
}
