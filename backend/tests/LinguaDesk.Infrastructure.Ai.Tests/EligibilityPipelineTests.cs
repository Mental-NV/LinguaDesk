using System.Text.Json;
using LinguaDesk.Infrastructure.Ai;
using Microsoft.Extensions.AI;

namespace LinguaDesk.Infrastructure.Ai.Tests;

[TestClass]
public sealed class EligibilityPipelineTests
{
    [TestMethod]
    public async Task EmptyInputEndsLocallyWithZeroDispatches()
    {
        using var client = new ScriptedEligibilityClient("{\"status\":\"eligible\",\"language\":\"en\"}");

        var outcome = await EligibilityPipeline.EvaluateAsync(
            new EligibilityInput("   ", EligibilityOperation.Translation, Target: "ru"),
            client);

        Assert.AreEqual(EligibilityDecision.LocallyRejected, outcome.Decision);
        Assert.AreEqual("empty", outcome.Category);
        Assert.AreEqual(0, outcome.ClassificationDispatches);
        Assert.AreEqual(0, client.CallCount);
        Assert.IsFalse(outcome.ChargesCharacters);
    }

    [TestMethod]
    public async Task OversizeTranslationReportsExcessWithZeroDispatches()
    {
        using var client = new ScriptedEligibilityClient("{\"status\":\"eligible\",\"language\":\"en\"}");
        var oversized = new string('a', 5001);

        var outcome = await EligibilityPipeline.EvaluateAsync(
            new EligibilityInput(oversized, EligibilityOperation.Translation, Target: "ru"),
            client);

        Assert.AreEqual(EligibilityDecision.LocallyRejected, outcome.Decision);
        Assert.AreEqual("oversize", outcome.Category);
        Assert.AreEqual(1, outcome.ExcessCharacters);
        Assert.AreEqual(0, client.CallCount);
    }

    [TestMethod]
    public async Task OversizeRewritingUsesTheRewritingLimit()
    {
        using var client = new ScriptedEligibilityClient("{\"status\":\"eligible\",\"language\":\"en\"}");

        var outcome = await EligibilityPipeline.EvaluateAsync(
            new EligibilityInput(new string('a', 2001), EligibilityOperation.Rewriting),
            client);

        Assert.AreEqual(EligibilityDecision.LocallyRejected, outcome.Decision);
        Assert.AreEqual("oversize", outcome.Category);
        Assert.AreEqual(1, outcome.ExcessCharacters);
        Assert.AreEqual(0, client.CallCount);
    }

    [TestMethod]
    public async Task ExactLimitsPassTheLocalGates()
    {
        using var client = new ScriptedEligibilityClient("{\"status\":\"eligible\",\"language\":\"en\"}");

        var translation = await EligibilityPipeline.EvaluateAsync(
            new EligibilityInput(new string('a', 5000), EligibilityOperation.Translation, Target: "ru"),
            client);
        var rewriting = await EligibilityPipeline.EvaluateAsync(
            new EligibilityInput(new string('a', 2000), EligibilityOperation.Rewriting),
            client);

        Assert.AreEqual(EligibilityDecision.Eligible, translation.Decision);
        Assert.AreEqual(EligibilityDecision.Eligible, rewriting.Decision);
        Assert.AreEqual(2, client.CallCount);
    }

    [TestMethod]
    public async Task ExplicitEqualSourceAndTargetIsLocallyInvalid()
    {
        using var client = new ScriptedEligibilityClient("{\"status\":\"eligible\",\"language\":\"en\"}");

        var outcome = await EligibilityPipeline.EvaluateAsync(
            new EligibilityInput("Please send the report tomorrow.", EligibilityOperation.Translation, "en", "en"),
            client);

        Assert.AreEqual(EligibilityDecision.LocallyRejected, outcome.Decision);
        Assert.AreEqual("equal-source-target", outcome.Category);
        Assert.AreEqual(0, client.CallCount);
    }

    [TestMethod]
    public async Task AutomaticSourceWithExplicitTargetReachesClassification()
    {
        using var client = new ScriptedEligibilityClient("{\"status\":\"eligible\",\"language\":\"en\"}");

        var outcome = await EligibilityPipeline.EvaluateAsync(
            new EligibilityInput("Please send the report tomorrow.", EligibilityOperation.Translation, "auto", "ru"),
            client);

        Assert.AreEqual(EligibilityDecision.Eligible, outcome.Decision);
        Assert.AreEqual(1, client.CallCount);
    }

    [TestMethod]
    [DataRow("uncertain", EligibilityDecision.Uncertain)]
    [DataRow("unsupported", EligibilityDecision.Unsupported)]
    [DataRow("mixed", EligibilityDecision.Mixed)]
    [DataRow("refused", EligibilityDecision.Refused)]
    public async Task NegativeClassificationsEndWithoutTransformationOrCharge(
        string status, EligibilityDecision expected)
    {
        using var client = new ScriptedEligibilityClient(
            $"{{\"status\":\"{status}\",\"language\":null}}");

        var outcome = await EligibilityPipeline.EvaluateAsync(
            new EligibilityInput("Please send the report tomorrow.", EligibilityOperation.Translation, Target: "ru"),
            client);

        Assert.AreEqual(expected, outcome.Decision);
        Assert.IsTrue(outcome.IsTerminalRejection);
        Assert.IsNull(outcome.ResolvedLanguage);
        Assert.AreEqual(1, outcome.ClassificationDispatches);
        Assert.AreEqual(1, client.CallCount);
        Assert.IsFalse(outcome.ChargesCharacters);
    }

    [TestMethod]
    public async Task EligibleReturnsResolvedLanguageForPipelineReuse()
    {
        using var client = new ScriptedEligibilityClient("{\"status\":\"eligible\",\"language\":\"ro\"}");

        var outcome = await EligibilityPipeline.EvaluateAsync(
            new EligibilityInput("Un alt text sintetic.", EligibilityOperation.Translation, Target: "en"),
            client);

        Assert.AreEqual(EligibilityDecision.Eligible, outcome.Decision);
        Assert.AreEqual("ro", outcome.ResolvedLanguage);
        Assert.IsFalse(outcome.IsTerminalRejection);
        Assert.AreEqual(1, outcome.ClassificationDispatches);
        Assert.AreEqual(1, client.CallCount);
        Assert.AreEqual("eligibility.v1", outcome.PromptId);
    }

    [TestMethod]
    public async Task EligibleContradictingManualChoiceIsRecordedFailure()
    {
        using var client = new ScriptedEligibilityClient("{\"status\":\"eligible\",\"language\":\"ru\"}");

        var outcome = await EligibilityPipeline.EvaluateAsync(
            new EligibilityInput("Vă rog să trimiteți raportul.", EligibilityOperation.Translation, "en", "ro"),
            client);

        Assert.AreEqual(EligibilityDecision.Failed, outcome.Decision);
        Assert.AreEqual("invalid-envelope", outcome.Category);
        Assert.IsNull(outcome.ResolvedLanguage);
    }

    [TestMethod]
    public async Task SourceMismatchMapsToTerminalRejection()
    {
        using var client = new ScriptedEligibilityClient("{\"status\":\"source_mismatch\",\"language\":\"ru\"}");

        var outcome = await EligibilityPipeline.EvaluateAsync(
            new EligibilityInput("Пожалуйста, пришлите отчёт.", EligibilityOperation.Translation, "en", "ru"),
            client);

        Assert.AreEqual(EligibilityDecision.SourceMismatch, outcome.Decision);
        Assert.IsTrue(outcome.IsTerminalRejection);
    }

    [TestMethod]
    public async Task MalformedClassificationIsRecordedFailureNeverEligible()
    {
        using var client = new ScriptedEligibilityClient("{\"status\":\"eligible\"}");

        var outcome = await EligibilityPipeline.EvaluateAsync(
            new EligibilityInput("Please send the report tomorrow.", EligibilityOperation.Translation, Target: "ru"),
            client);

        Assert.AreEqual(EligibilityDecision.Failed, outcome.Decision);
        Assert.AreEqual("invalid-envelope", outcome.Category);
        Assert.IsNull(outcome.ResolvedLanguage);
    }

    [TestMethod]
    public async Task ProviderFailureIsRecordedFailureNeverEligible()
    {
        using var client = new FailingEligibilityClient();

        var outcome = await EligibilityPipeline.EvaluateAsync(
            new EligibilityInput("Please send the report tomorrow.", EligibilityOperation.Translation, Target: "ru"),
            client);

        Assert.AreEqual(EligibilityDecision.Failed, outcome.Decision);
        Assert.AreEqual("provider-failure", outcome.Category);
        Assert.IsNull(outcome.ResolvedLanguage);
        Assert.AreEqual(1, outcome.ClassificationDispatches);
    }

    [TestMethod]
    public async Task PromptCarriesOriginalSourceAsDataWithValidatedHint()
    {
        using var client = new ScriptedEligibilityClient("{\"status\":\"eligible\",\"language\":\"ru\"}");
        const string source = "Пожалуйста, пришлите отчёт завтра.";

        await EligibilityPipeline.EvaluateAsync(
            new EligibilityInput(source, EligibilityOperation.Translation, "ru", "en"),
            client);

        Assert.IsNotNull(client.LastUserText);
        using var userData = JsonDocument.Parse(client.LastUserText);
        Assert.AreEqual(source, userData.RootElement.GetProperty("source").GetString());
        Assert.AreEqual("ru", userData.RootElement.GetProperty("sourceHint").GetString());
    }

    [TestMethod]
    public async Task InvalidSelectorsEndLocallyWithZeroDispatches()
    {
        using var client = new ScriptedEligibilityClient("{\"status\":\"eligible\",\"language\":\"en\"}");

        var badTarget = await EligibilityPipeline.EvaluateAsync(
            new EligibilityInput("Hello.", EligibilityOperation.Translation, Target: "de"),
            client);
        var badMode = await EligibilityPipeline.EvaluateAsync(
            new EligibilityInput("Hello.", EligibilityOperation.Rewriting, Mode: "shouty"),
            client);

        Assert.AreEqual("invalid-target", badTarget.Category);
        Assert.AreEqual("invalid-mode", badMode.Category);
        Assert.AreEqual(0, client.CallCount);
    }

    private sealed class ScriptedEligibilityClient(string responseText) : IChatClient
    {
        public int CallCount { get; private set; }

        public string? LastUserText { get; private set; }

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
            LastUserText = messages.Last().Text;
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, responseText)));
        }

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("The eligibility pipeline never streams.");
    }

    private sealed class FailingEligibilityClient : IChatClient
    {
        public void Dispose()
        {
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default) =>
            Task.FromException<ChatResponse>(new InvalidOperationException("Synthetic provider failure."));

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("The eligibility pipeline never streams.");
    }
}
