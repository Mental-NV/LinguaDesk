using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LinguaDesk.Api.Features.Operations;
using LinguaDesk.Infrastructure.Ai;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LinguaDesk.Api.Tests;

/// <summary>
/// M043 deterministic ceiling evidence (AC-003): the per-operation ceiling is
/// enforced before any provider call, settles as monetary suspension with
/// zero charge and no fallback, admits an exactly-at-cap bound, and applies
/// to both families. The monthly-cap path is unchanged.
/// </summary>
[TestClass]
public sealed class ServingCeilingTests
{
    private const string CandidateId = "DeepSeek-V4.1-Flash";

    private static Dictionary<string, string?> ServingConfig(
        string translationCeiling,
        string rewritingCeiling) => new()
        {
            ["MonetaryAdmission:MonthlyCapMinorUnits"] = "1000000",
            ["MonetaryAdmission:Currency"] = "USD",
            ["Serving:Translation:CandidateId"] = CandidateId,
            ["Serving:Translation:CredentialRef"] = "deepseek",
            ["Serving:Translation:MaxSpendUsdPerOperation"] = translationCeiling,
            ["Serving:Rewriting:CandidateId"] = CandidateId,
            ["Serving:Rewriting:CredentialRef"] = "deepseek",
            ["Serving:Rewriting:MaxSpendUsdPerOperation"] = rewritingCeiling,
        };

    private static async Task<string> CreateVerifiedAccountAsync(OperationFixture fixture, string email)
    {
        await fixture.CreateAccountAsync(email, confirmed: true);
        return await fixture.SignInAsync(email);
    }

    private static string TranslationBody(string operationId, string source) =>
        JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["operationId"] = operationId,
            ["family"] = "translation",
            ["source"] = source,
            ["target"] = "ru",
        });

    private static string RewritingBody(string operationId, string source) =>
        JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["operationId"] = operationId,
            ["family"] = "rewriting",
            ["source"] = source,
            ["mode"] = "simple",
        });

    [TestMethod]
    public async Task OverCapTranslationDeniedBeforeAnyProviderCall()
    {
        using var primary = new ScriptedClient();
        using var fallback = new ScriptedClient();
        await using var fixture = await OperationFixture.CreateAsync(
            ServingConfig("0.000001", "0.05"),
            services =>
            {
                services.RemoveAll<ITranslationClientProvider>();
                services.AddSingleton<ITranslationClientProvider>(
                    new ScriptedTranslationProvider(primary, fallback));
            });
        var access = await CreateVerifiedAccountAsync(fixture, "ceiling-over@example.test");
        var operationId = OperationIdentity.CreateForTime(fixture.Time.GetUtcNow()).ToString("D");

        using var response = await fixture.PostRawAsync(
            "/api/operations",
            TranslationBody(operationId, "Hello."),
            access);

        Assert.AreEqual(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.AreEqual("monetarySuspension", problem.GetProperty("category").GetString());
        Assert.AreEqual(0, primary.CallCount);
        Assert.AreEqual(0, fallback.CallCount);

        using var usage = await fixture.GetAsync("/api/usage", access);
        var usageBody = await usage.Content.ReadFromJsonAsync<JsonElement>();
        Assert.AreEqual(0, usageBody.GetProperty("consumedCharacters").GetInt32());
    }

    [TestMethod]
    public async Task AtCapTranslationAdmitted()
    {
        using var primary = new ScriptedClient();
        using var fallback = new ScriptedClient();
        primary.EnqueueResponse("{\"status\":\"eligible\",\"language\":\"en\"}");
        primary.EnqueueResponse("{\"status\":\"result\",\"text\":\"Привет.\"}");
        await using var fixture = await OperationFixture.CreateAsync(
            ServingConfig("0.014746", "0.05"),
            services =>
            {
                services.RemoveAll<ITranslationClientProvider>();
                services.AddSingleton<ITranslationClientProvider>(
                    new ScriptedTranslationProvider(primary, fallback));
            });
        var access = await CreateVerifiedAccountAsync(fixture, "ceiling-atcap@example.test");
        var operationId = OperationIdentity.CreateForTime(fixture.Time.GetUtcNow()).ToString("D");

        using var response = await fixture.PostRawAsync(
            "/api/operations",
            TranslationBody(operationId, "Hello."),
            access);

        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.AreEqual("Привет.", body.GetProperty("translatedText").GetString());
        Assert.AreEqual("Hello.".Length, body.GetProperty("characterCount").GetInt32());
        Assert.AreEqual(2, primary.CallCount);
        Assert.AreEqual(0, fallback.CallCount);
    }

    [TestMethod]
    public async Task JustBelowCapDeniedOnSecondStageWithoutFallback()
    {
        using var primary = new ScriptedClient();
        using var fallback = new ScriptedClient();
        primary.EnqueueResponse("{\"status\":\"eligible\",\"language\":\"en\"}");
        await using var fixture = await OperationFixture.CreateAsync(
            ServingConfig("0.014745", "0.05"),
            services =>
            {
                services.RemoveAll<ITranslationClientProvider>();
                services.AddSingleton<ITranslationClientProvider>(
                    new ScriptedTranslationProvider(primary, fallback));
            });
        var access = await CreateVerifiedAccountAsync(fixture, "ceiling-edge@example.test");
        var operationId = OperationIdentity.CreateForTime(fixture.Time.GetUtcNow()).ToString("D");

        using var response = await fixture.PostRawAsync(
            "/api/operations",
            TranslationBody(operationId, "Hello."),
            access);

        Assert.AreEqual(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.AreEqual("monetarySuspension", problem.GetProperty("category").GetString());
        Assert.AreEqual(1, primary.CallCount);
        Assert.AreEqual(0, fallback.CallCount);

        using var usage = await fixture.GetAsync("/api/usage", access);
        var usageBody = await usage.Content.ReadFromJsonAsync<JsonElement>();
        Assert.AreEqual(0, usageBody.GetProperty("consumedCharacters").GetInt32());
    }

    [TestMethod]
    public async Task OverCapRewritingDeniedBeforeAnyProviderCall()
    {
        using var primary = new ScriptedClient();
        using var fallback = new ScriptedClient();
        await using var fixture = await OperationFixture.CreateAsync(
            ServingConfig("0.05", "0.000001"),
            services =>
            {
                services.RemoveAll<IRewritingClientProvider>();
                services.AddSingleton<IRewritingClientProvider>(
                    new ScriptedRewritingProvider(primary, fallback));
            });
        var access = await CreateVerifiedAccountAsync(fixture, "ceiling-rewrite@example.test");
        var operationId = OperationIdentity.CreateForTime(fixture.Time.GetUtcNow()).ToString("D");

        using var response = await fixture.PostRawAsync(
            "/api/operations",
            RewritingBody(operationId, "The report is really ready."),
            access);

        Assert.AreEqual(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.AreEqual("monetarySuspension", problem.GetProperty("category").GetString());
        Assert.AreEqual(0, primary.CallCount);
        Assert.AreEqual(0, fallback.CallCount);
    }

    [TestMethod]
    public async Task AtCapRewritingAdmitted()
    {
        using var primary = new ScriptedClient();
        using var fallback = new ScriptedClient();
        primary.EnqueueResponse("{\"status\":\"eligible\",\"language\":\"en\"}");
        primary.EnqueueResponse("{\"status\":\"result\",\"text\":\"The report is ready.\"}");
        await using var fixture = await OperationFixture.CreateAsync(
            ServingConfig("0.05", "0.014746"),
            services =>
            {
                services.RemoveAll<IRewritingClientProvider>();
                services.AddSingleton<IRewritingClientProvider>(
                    new ScriptedRewritingProvider(primary, fallback));
            });
        var access = await CreateVerifiedAccountAsync(fixture, "ceiling-rewrite-ok@example.test");
        const string source = "The report is really ready.";
        var operationId = OperationIdentity.CreateForTime(fixture.Time.GetUtcNow()).ToString("D");

        using var response = await fixture.PostRawAsync(
            "/api/operations",
            RewritingBody(operationId, source),
            access);

        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.AreEqual("The report is ready.", body.GetProperty("rewrittenText").GetString());
        Assert.AreEqual(2, primary.CallCount);
        Assert.AreEqual(0, fallback.CallCount);
    }

    private sealed class ScriptedTranslationProvider(
        ScriptedClient primary,
        ScriptedClient fallback) : ITranslationClientProvider
    {
        public bool TryGetClients(out Func<CandidateProfile, IChatClient> clients, out FamilyChain chain)
        {
            clients = profile => string.Equals(profile.CandidateId, FamilyChain.EvaluationPrimaryId, StringComparison.Ordinal)
                ? primary
                : fallback;
            chain = FamilyChain.EvaluationDefault(ChainFamily.Translation);
            return true;
        }
    }

    private sealed class ScriptedRewritingProvider(
        ScriptedClient primary,
        ScriptedClient fallback) : IRewritingClientProvider
    {
        public bool TryGetClients(out Func<CandidateProfile, IChatClient> clients, out FamilyChain chain)
        {
            clients = profile => string.Equals(profile.CandidateId, FamilyChain.EvaluationPrimaryId, StringComparison.Ordinal)
                ? primary
                : fallback;
            chain = FamilyChain.EvaluationDefault(ChainFamily.Rewriting);
            return true;
        }
    }

    private sealed class ScriptedClient : IChatClient, IDisposable
    {
        private readonly Queue<string> responses = new();

        public int CallCount { get; private set; }

        public void EnqueueResponse(string json) => responses.Enqueue(json);

        public void Dispose()
        {
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            _ = messages.ToArray();
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, responses.Dequeue())));
        }

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("The ceiling fake never streams.");
    }
}
