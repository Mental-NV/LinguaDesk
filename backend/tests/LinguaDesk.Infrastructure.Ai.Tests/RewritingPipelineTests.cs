using System.Text.Json;
using LinguaDesk.Core;
using LinguaDesk.Infrastructure.Ai;
using Microsoft.Extensions.AI;

namespace LinguaDesk.Infrastructure.Ai.Tests;

[TestClass]
public sealed class RewritingPipelineTests
{
    private const string RewrittenText = "Rewritten synthetic text.";

    private static string SourceFor(string language) => language switch
    {
        "en" => "Please send the report tomorrow.",
        "ru" => "Пожалуйста, пришлите отчёт завтра.",
        "ro" => "Vă rog să trimiteți raportul mâine.",
        "zh" => "请明天发送报告。",
        _ => throw new ArgumentOutOfRangeException(nameof(language)),
    };

    [TestMethod]
    [DataRow("correctionOnly")]
    [DataRow("simple")]
    [DataRow("casual")]
    [DataRow("business")]
    [DataRow("academic")]
    [DataRow("enthusiastic")]
    [DataRow("friendly")]
    [DataRow("confident")]
    [DataRow("diplomatic")]
    public async Task EveryCatalogModeIsAcceptedWithOneValidatedResult(string mode)
    {
        using var client = new ScriptedRewritingClient(
            "{\"status\":\"eligible\",\"language\":\"en\"}",
            $"{{\"status\":\"result\",\"text\":\"{RewrittenText}\"}}");

        var outcome = await RewritingPipeline.RewriteAsync(
            new RewritingInput(SourceFor("en"), SourceSelection: "en", Mode: mode),
            client);

        Assert.AreEqual(RewritingDecision.Succeeded, outcome.Decision, mode);
        Assert.AreEqual("en", outcome.ResolvedSourceLanguage, mode);
        Assert.AreEqual(mode, outcome.Mode, mode);
        Assert.AreEqual(RewrittenText, outcome.Text, mode);
        Assert.IsNull(outcome.Category, mode);
        Assert.AreEqual(1, outcome.EligibilityDispatches, mode);
        Assert.AreEqual(1, outcome.TransformationDispatches, mode);
        Assert.AreEqual(2, outcome.TotalDispatches, mode);
        Assert.AreEqual(2, client.CallCount, mode);
        Assert.IsFalse(outcome.ChargesCharacters, mode);
        Assert.AreEqual("eligibility.v1", outcome.EligibilityPromptId, mode);
        Assert.AreEqual("rewriting.v1", outcome.PromptId, mode);
    }

    [TestMethod]
    public async Task OmittedModeDefaultsToCorrectionOnly()
    {
        using var client = new ScriptedRewritingClient(
            "{\"status\":\"eligible\",\"language\":\"en\"}",
            $"{{\"status\":\"result\",\"text\":\"{RewrittenText}\"}}");

        var outcome = await RewritingPipeline.RewriteAsync(
            new RewritingInput(SourceFor("en"), SourceSelection: "en"),
            client);

        Assert.AreEqual(RewritingDecision.Succeeded, outcome.Decision);
        Assert.AreEqual(ProductCatalog.DefaultRewritingMode, outcome.Mode);

        using var userData = JsonDocument.Parse(client.UserTexts[1]);
        Assert.AreEqual(
            ProductCatalog.DefaultRewritingMode,
            userData.RootElement.GetProperty("mode").GetString());
    }

    [TestMethod]
    [DataRow("", "empty mode")]
    [DataRow("correction", "unknown mode")]
    [DataRow("CORRECTIONONLY", "wrong-case mode")]
    [DataRow("simple,casual", "multiple modes")]
    public async Task UnknownEmptyOrMultipleModesAreRejectedWithZeroDispatches(string mode, string label)
    {
        using var client = new ScriptedRewritingClient();

        var outcome = await RewritingPipeline.RewriteAsync(
            new RewritingInput(SourceFor("en"), SourceSelection: "en", Mode: mode),
            client);

        Assert.AreEqual(RewritingDecision.Ineligible, outcome.Decision, label);
        Assert.AreEqual("invalid-mode", outcome.Category, label);
        Assert.AreEqual(0, outcome.EligibilityDispatches, label);
        Assert.AreEqual(0, outcome.TransformationDispatches, label);
        Assert.AreEqual(0, outcome.TotalDispatches, label);
        Assert.AreEqual(0, client.CallCount, label);
        Assert.IsNull(outcome.Text, label);
        Assert.IsFalse(outcome.ChargesCharacters, label);
    }

    [TestMethod]
    [DataRow("en")]
    [DataRow("ru")]
    [DataRow("ro")]
    [DataRow("zh")]
    public async Task EveryLanguageYieldsOneSameLanguageValidatedResult(string language)
    {
        using var client = new ScriptedRewritingClient(
            $"{{\"status\":\"eligible\",\"language\":\"{language}\"}}",
            $"{{\"status\":\"result\",\"text\":\"{RewrittenText}\"}}");

        var outcome = await RewritingPipeline.RewriteAsync(
            new RewritingInput(SourceFor(language), SourceSelection: language, Mode: "business"),
            client);

        Assert.AreEqual(RewritingDecision.Succeeded, outcome.Decision, language);
        Assert.AreEqual(language, outcome.ResolvedSourceLanguage, language);
        Assert.AreEqual("business", outcome.Mode, language);
        Assert.AreEqual(RewrittenText, outcome.Text, language);
        Assert.AreEqual(2, client.CallCount, language);
    }

    [TestMethod]
    public async Task TraditionalChineseInputRewritesThroughTheSamePipeline()
    {
        using var client = new ScriptedRewritingClient(
            "{\"status\":\"eligible\",\"language\":\"zh\"}",
            $"{{\"status\":\"result\",\"text\":\"{RewrittenText}\"}}");

        var outcome = await RewritingPipeline.RewriteAsync(
            new RewritingInput("請明天發送報告。", SourceSelection: "zh", Mode: "simple"),
            client);

        Assert.AreEqual(RewritingDecision.Succeeded, outcome.Decision);
        Assert.AreEqual("zh", outcome.ResolvedSourceLanguage);
        Assert.AreEqual(RewrittenText, outcome.Text);
    }

    [TestMethod]
    public async Task CorrectionOnlyPermitsUnchangedCorrectText()
    {
        const string source = "Please send the report tomorrow.";
        using var client = new ScriptedRewritingClient(
            "{\"status\":\"eligible\",\"language\":\"en\"}",
            $"{{\"status\":\"result\",\"text\":\"{source}\"}}");

        var outcome = await RewritingPipeline.RewriteAsync(
            new RewritingInput(source, SourceSelection: "en", Mode: "correctionOnly"),
            client);

        Assert.AreEqual(RewritingDecision.Succeeded, outcome.Decision);
        Assert.AreEqual(source, outcome.Text);
    }

    [TestMethod]
    public async Task PromptCarriesOriginalSourceWithValidatedLanguageAndModeAndNoHistory()
    {
        const string source = "Пожалуйста, пришлите отчёт завтра.";
        using var client = new ScriptedRewritingClient(
            "{\"status\":\"eligible\",\"language\":\"ru\"}",
            $"{{\"status\":\"result\",\"text\":\"{RewrittenText}\"}}");

        await RewritingPipeline.RewriteAsync(
            new RewritingInput(source, SourceSelection: "ru", Mode: "friendly"),
            client);

        Assert.HasCount(2, client.UserTexts);
        using var userData = JsonDocument.Parse(client.UserTexts[1]);
        Assert.AreEqual(source, userData.RootElement.GetProperty("source").GetString());
        Assert.AreEqual("ru", userData.RootElement.GetProperty("sourceLanguage").GetString());
        Assert.AreEqual("friendly", userData.RootElement.GetProperty("mode").GetString());
        Assert.AreEqual(2, client.MessageCounts[1]);
    }

    [TestMethod]
    public async Task OnlyValidatedPlainTextIsReturnedNeverTheEnvelope()
    {
        using var client = new ScriptedRewritingClient(
            "{\"status\":\"eligible\",\"language\":\"en\"}",
            $"{{\"status\":\"result\",\"text\":\"{RewrittenText}\"}}");

        var outcome = await RewritingPipeline.RewriteAsync(
            new RewritingInput(SourceFor("en"), Mode: "academic"),
            client);

        Assert.IsFalse(outcome.Text!.Contains("status", StringComparison.Ordinal));
        Assert.AreEqual(RewrittenText, outcome.Text);
    }

    [TestMethod]
    public async Task RefusalStoppingWordInsideSourceStillRewrites()
    {
        const string source = "I cannot attend the meeting tomorrow.";
        using var client = new ScriptedRewritingClient(
            "{\"status\":\"eligible\",\"language\":\"en\"}",
            "{\"status\":\"result\",\"text\":\"I am unable to attend the meeting tomorrow.\"}");

        var outcome = await RewritingPipeline.RewriteAsync(
            new RewritingInput(source, Mode: "business"),
            client);

        Assert.AreEqual(RewritingDecision.Succeeded, outcome.Decision);
        Assert.IsNotNull(outcome.Text);
    }

    [TestMethod]
    public async Task ProviderRefusalIsRecordedWithoutRetry()
    {
        using var client = new ScriptedRewritingClient(
            "{\"status\":\"eligible\",\"language\":\"en\"}",
            "{\"status\":\"refused\",\"text\":null}");

        var outcome = await RewritingPipeline.RewriteAsync(
            new RewritingInput(SourceFor("en"), Mode: "casual"),
            client);

        Assert.AreEqual(RewritingDecision.Refused, outcome.Decision);
        Assert.AreEqual("refused", outcome.Category);
        Assert.IsNull(outcome.Text);
        Assert.IsNull(outcome.ResolvedSourceLanguage);
        Assert.AreEqual(2, client.CallCount);
        Assert.IsTrue(outcome.IsTerminalRejection);
    }

    [TestMethod]
    [DataRow("{\"status\":\"eligible\"}", "malformed transformation")]
    [DataRow("", "empty transformation")]
    [DataRow("   ", "whitespace transformation")]
    [DataRow("{\"status\":\"result\",\"text\":\"\"}", "empty result text")]
    public async Task InvalidTransformationOutputIsRecordedFailure(string transformation, string label)
    {
        using var client = new ScriptedRewritingClient(
            "{\"status\":\"eligible\",\"language\":\"en\"}",
            transformation);

        var outcome = await RewritingPipeline.RewriteAsync(
            new RewritingInput(SourceFor("en"), Mode: "diplomatic"),
            client);

        Assert.AreEqual(RewritingDecision.Failed, outcome.Decision, label);
        Assert.AreEqual("invalid-envelope", outcome.Category, label);
        Assert.IsNull(outcome.Text, label);
        Assert.AreEqual(2, client.CallCount, label);
    }

    [TestMethod]
    public async Task TruncatedTransformationIsRecordedFailure()
    {
        var truncated = new ChatResponse(new ChatMessage(ChatRole.Assistant, "{\"status\":\"result\","))
        {
            FinishReason = ChatFinishReason.Length,
        };
        using var client = new ScriptedRewritingClient(
            "{\"status\":\"eligible\",\"language\":\"en\"}",
            truncated);

        var outcome = await RewritingPipeline.RewriteAsync(
            new RewritingInput(SourceFor("en"), Mode: "simple"),
            client);

        Assert.AreEqual(RewritingDecision.Failed, outcome.Decision);
        Assert.AreEqual("truncated", outcome.Category);
        Assert.IsNull(outcome.Text);
    }

    [TestMethod]
    public async Task ToolCallContentNeverBecomesSuccess()
    {
        var toolCall = new ChatResponse(new ChatMessage(
            ChatRole.Assistant,
            [new FunctionCallContent("call-1", "rewrite"), new TextContent(RewrittenText)]));
        using var client = new ScriptedRewritingClient(
            "{\"status\":\"eligible\",\"language\":\"en\"}",
            toolCall);

        var outcome = await RewritingPipeline.RewriteAsync(
            new RewritingInput(SourceFor("en"), Mode: "confident"),
            client);

        Assert.AreEqual(RewritingDecision.Failed, outcome.Decision);
        Assert.AreEqual("invalid-envelope", outcome.Category);
        Assert.IsNull(outcome.Text);
    }

    [TestMethod]
    public async Task TransformationProviderFailureIsRecordedFailure()
    {
        using var client = new ScriptedRewritingClient(
            "{\"status\":\"eligible\",\"language\":\"en\"}",
            new InvalidOperationException("Synthetic transformation failure."));

        var outcome = await RewritingPipeline.RewriteAsync(
            new RewritingInput(SourceFor("en"), Mode: "enthusiastic"),
            client);

        Assert.AreEqual(RewritingDecision.Failed, outcome.Decision);
        Assert.AreEqual("provider-failure", outcome.Category);
        Assert.IsNull(outcome.Text);
        Assert.AreEqual(2, client.CallCount);
    }

    [TestMethod]
    [DataRow("uncertain")]
    [DataRow("unsupported")]
    [DataRow("mixed")]
    [DataRow("refused")]
    public async Task TerminalEligibilityEndsWithZeroTransformationDispatches(string status)
    {
        using var client = new ScriptedRewritingClient(
            $"{{\"status\":\"{status}\",\"language\":null}}");

        var outcome = await RewritingPipeline.RewriteAsync(
            new RewritingInput(SourceFor("en"), Mode: "academic"),
            client);

        Assert.AreEqual(RewritingDecision.Ineligible, outcome.Decision);
        Assert.AreEqual(status == "refused" ? "refused" : status, outcome.Category);
        Assert.AreEqual(0, outcome.TransformationDispatches);
        Assert.AreEqual(1, outcome.TotalDispatches);
        Assert.AreEqual(1, client.CallCount);
        Assert.IsNull(outcome.Text);
        Assert.IsFalse(outcome.ChargesCharacters);
    }

    [TestMethod]
    public async Task EmptyInputEndsLocallyWithZeroDispatches()
    {
        using var client = new ScriptedRewritingClient();

        var outcome = await RewritingPipeline.RewriteAsync(
            new RewritingInput("   ", Mode: "simple"),
            client);

        Assert.AreEqual(RewritingDecision.Ineligible, outcome.Decision);
        Assert.AreEqual("empty", outcome.Category);
        Assert.AreEqual(0, outcome.TotalDispatches);
        Assert.AreEqual(0, client.CallCount);
    }

    [TestMethod]
    public async Task OversizeInputReportsExcessWithZeroDispatches()
    {
        using var client = new ScriptedRewritingClient();
        var oversized = new string('a', 2001);

        var outcome = await RewritingPipeline.RewriteAsync(
            new RewritingInput(oversized, Mode: "simple"),
            client);

        Assert.AreEqual(RewritingDecision.Ineligible, outcome.Decision);
        Assert.AreEqual("oversize", outcome.Category);
        Assert.AreEqual(1, outcome.ExcessCharacters);
        Assert.AreEqual(0, client.CallCount);
    }

    [TestMethod]
    public async Task EligibilityFailureIsRecordedFailureWithoutTransformation()
    {
        using var client = new ScriptedRewritingClient("{\"status\":\"eligible\"}");

        var outcome = await RewritingPipeline.RewriteAsync(
            new RewritingInput(SourceFor("en"), Mode: "casual"),
            client);

        Assert.AreEqual(RewritingDecision.Failed, outcome.Decision);
        Assert.AreEqual("invalid-envelope", outcome.Category);
        Assert.AreEqual(0, outcome.TransformationDispatches);
        Assert.AreEqual(1, client.CallCount);
    }

    private sealed class ScriptedRewritingClient : IChatClient
    {
        private readonly Queue<object> scripted;

        public ScriptedRewritingClient(params object[] responses)
        {
            scripted = new Queue<object>(responses);
        }

        public int CallCount { get; private set; }

        public List<string> UserTexts { get; } = [];

        public List<int> MessageCounts { get; } = [];

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
            var materialized = messages.ToArray();
            MessageCounts.Add(materialized.Length);
            UserTexts.Add(materialized.Last().Text ?? string.Empty);

            if (scripted.Count == 0)
            {
                Assert.Fail("The rewriting pipeline dispatched more than the scripted responses allow.");
            }

            var next = scripted.Dequeue();
            return next switch
            {
                string text => Task.FromResult(
                    new ChatResponse(new ChatMessage(ChatRole.Assistant, text))),
                ChatResponse response => Task.FromResult(response),
                Exception failure => Task.FromException<ChatResponse>(failure),
                _ => throw new InvalidOperationException("Unsupported scripted rewriting response."),
            };
        }

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("The rewriting pipeline never streams.");
    }
}
