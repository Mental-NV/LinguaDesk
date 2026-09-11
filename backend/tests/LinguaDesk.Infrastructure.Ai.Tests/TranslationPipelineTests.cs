using System.Text.Json;
using LinguaDesk.Infrastructure.Ai;
using Microsoft.Extensions.AI;

namespace LinguaDesk.Infrastructure.Ai.Tests;

[TestClass]
public sealed class TranslationPipelineTests
{
    private const string TranslatedText = "Translated synthetic text.";

    private static string SourceFor(string language) => language switch
    {
        "en" => "Please send the report tomorrow.",
        "ru" => "Пожалуйста, пришлите отчёт завтра.",
        "ro" => "Vă rog să trimiteți raportul mâine.",
        "zh" => "请明天发送报告。",
        _ => throw new ArgumentOutOfRangeException(nameof(language)),
    };

    [TestMethod]
    [DataRow("en", "ru")]
    [DataRow("en", "ro")]
    [DataRow("en", "zh")]
    [DataRow("ru", "en")]
    [DataRow("ru", "ro")]
    [DataRow("ru", "zh")]
    [DataRow("ro", "en")]
    [DataRow("ro", "ru")]
    [DataRow("ro", "zh")]
    [DataRow("zh", "en")]
    [DataRow("zh", "ru")]
    [DataRow("zh", "ro")]
    public async Task EveryDirectionYieldsOneValidatedPlainTextResult(string source, string target)
    {
        using var client = new ScriptedTranslationClient(
            $"{{\"status\":\"eligible\",\"language\":\"{source}\"}}",
            $"{{\"status\":\"result\",\"text\":\"{TranslatedText}\"}}");

        var outcome = await TranslationPipeline.TranslateAsync(
            new TranslationInput(SourceFor(source), SourceSelection: source, Target: target),
            client);

        Assert.AreEqual(TranslationDecision.Succeeded, outcome.Decision);
        Assert.AreEqual(source, outcome.ResolvedSourceLanguage);
        Assert.AreEqual(target, outcome.Target);
        Assert.AreEqual(TranslatedText, outcome.Text);
        Assert.IsNull(outcome.Category);
        Assert.AreEqual(1, outcome.EligibilityDispatches);
        Assert.AreEqual(1, outcome.TransformationDispatches);
        Assert.AreEqual(2, outcome.TotalDispatches);
        Assert.AreEqual(2, client.CallCount);
        Assert.IsFalse(outcome.ChargesCharacters);
        Assert.AreEqual("eligibility.v1", outcome.EligibilityPromptId);
        Assert.AreEqual("translation.v1", outcome.PromptId);
    }

    [TestMethod]
    public async Task TraditionalChineseInputTranslatesThroughTheSamePipeline()
    {
        using var client = new ScriptedTranslationClient(
            "{\"status\":\"eligible\",\"language\":\"zh\"}",
            $"{{\"status\":\"result\",\"text\":\"{TranslatedText}\"}}");

        var outcome = await TranslationPipeline.TranslateAsync(
            new TranslationInput("請明天發送報告。", SourceSelection: "zh", Target: "en"),
            client);

        Assert.AreEqual(TranslationDecision.Succeeded, outcome.Decision);
        Assert.AreEqual("zh", outcome.ResolvedSourceLanguage);
        Assert.AreEqual(TranslatedText, outcome.Text);
    }

    [TestMethod]
    public async Task PromptCarriesOriginalSourceWithValidatedLanguagesAndNoHistory()
    {
        const string source = "Пожалуйста, пришлите отчёт завтра.";
        using var client = new ScriptedTranslationClient(
            "{\"status\":\"eligible\",\"language\":\"ru\"}",
            $"{{\"status\":\"result\",\"text\":\"{TranslatedText}\"}}");

        await TranslationPipeline.TranslateAsync(
            new TranslationInput(source, SourceSelection: "ru", Target: "en"),
            client);

        Assert.HasCount(2, client.UserTexts);
        using var userData = JsonDocument.Parse(client.UserTexts[1]);
        Assert.AreEqual(source, userData.RootElement.GetProperty("source").GetString());
        Assert.AreEqual("ru", userData.RootElement.GetProperty("sourceLanguage").GetString());
        Assert.AreEqual("en", userData.RootElement.GetProperty("targetLanguage").GetString());
        Assert.AreEqual(2, client.MessageCounts[1]);
    }

    [TestMethod]
    public async Task OnlyValidatedPlainTextIsReturnedNeverTheEnvelope()
    {
        using var client = new ScriptedTranslationClient(
            "{\"status\":\"eligible\",\"language\":\"en\"}",
            $"{{\"status\":\"result\",\"text\":\"{TranslatedText}\"}}");

        var outcome = await TranslationPipeline.TranslateAsync(
            new TranslationInput(SourceFor("en"), Target: "ru"),
            client);

        Assert.IsFalse(outcome.Text!.Contains("status", StringComparison.Ordinal));
        Assert.AreEqual(TranslatedText, outcome.Text);
    }

    [TestMethod]
    public async Task RefusalStoppingWordInsideSourceStillTranslates()
    {
        const string source = "I cannot attend the meeting tomorrow.";
        using var client = new ScriptedTranslationClient(
            "{\"status\":\"eligible\",\"language\":\"en\"}",
            "{\"status\":\"result\",\"text\":\"Я не могу присутствовать на встрече завтра.\"}");

        var outcome = await TranslationPipeline.TranslateAsync(
            new TranslationInput(source, Target: "ru"),
            client);

        Assert.AreEqual(TranslationDecision.Succeeded, outcome.Decision);
        Assert.IsNotNull(outcome.Text);
    }

    [TestMethod]
    public async Task ProviderRefusalIsRecordedWithoutRetry()
    {
        using var client = new ScriptedTranslationClient(
            "{\"status\":\"eligible\",\"language\":\"en\"}",
            "{\"status\":\"refused\",\"text\":null}");

        var outcome = await TranslationPipeline.TranslateAsync(
            new TranslationInput(SourceFor("en"), Target: "ru"),
            client);

        Assert.AreEqual(TranslationDecision.Refused, outcome.Decision);
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
        using var client = new ScriptedTranslationClient(
            "{\"status\":\"eligible\",\"language\":\"en\"}",
            transformation);

        var outcome = await TranslationPipeline.TranslateAsync(
            new TranslationInput(SourceFor("en"), Target: "ru"),
            client);

        Assert.AreEqual(TranslationDecision.Failed, outcome.Decision, label);
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
        using var client = new ScriptedTranslationClient(
            "{\"status\":\"eligible\",\"language\":\"en\"}",
            truncated);

        var outcome = await TranslationPipeline.TranslateAsync(
            new TranslationInput(SourceFor("en"), Target: "ru"),
            client);

        Assert.AreEqual(TranslationDecision.Failed, outcome.Decision);
        Assert.AreEqual("truncated", outcome.Category);
        Assert.IsNull(outcome.Text);
    }

    [TestMethod]
    public async Task ToolCallContentNeverBecomesSuccess()
    {
        var toolCall = new ChatResponse(new ChatMessage(
            ChatRole.Assistant,
            [new FunctionCallContent("call-1", "translate"), new TextContent(TranslatedText)]));
        using var client = new ScriptedTranslationClient(
            "{\"status\":\"eligible\",\"language\":\"en\"}",
            toolCall);

        var outcome = await TranslationPipeline.TranslateAsync(
            new TranslationInput(SourceFor("en"), Target: "ru"),
            client);

        Assert.AreEqual(TranslationDecision.Failed, outcome.Decision);
        Assert.AreEqual("invalid-envelope", outcome.Category);
        Assert.IsNull(outcome.Text);
    }

    [TestMethod]
    public async Task TransformationProviderFailureIsRecordedFailure()
    {
        using var client = new ScriptedTranslationClient(
            "{\"status\":\"eligible\",\"language\":\"en\"}",
            new InvalidOperationException("Synthetic transformation failure."));

        var outcome = await TranslationPipeline.TranslateAsync(
            new TranslationInput(SourceFor("en"), Target: "ru"),
            client);

        Assert.AreEqual(TranslationDecision.Failed, outcome.Decision);
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
        using var client = new ScriptedTranslationClient(
            $"{{\"status\":\"{status}\",\"language\":null}}");

        var outcome = await TranslationPipeline.TranslateAsync(
            new TranslationInput(SourceFor("en"), Target: "ru"),
            client);

        Assert.AreEqual(TranslationDecision.Ineligible, outcome.Decision);
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
        using var client = new ScriptedTranslationClient();

        var outcome = await TranslationPipeline.TranslateAsync(
            new TranslationInput("   ", Target: "ru"),
            client);

        Assert.AreEqual(TranslationDecision.Ineligible, outcome.Decision);
        Assert.AreEqual("empty", outcome.Category);
        Assert.AreEqual(0, outcome.TotalDispatches);
        Assert.AreEqual(0, client.CallCount);
    }

    [TestMethod]
    public async Task OversizeInputReportsExcessWithZeroDispatches()
    {
        using var client = new ScriptedTranslationClient();
        var oversized = new string('a', 5001);

        var outcome = await TranslationPipeline.TranslateAsync(
            new TranslationInput(oversized, Target: "ru"),
            client);

        Assert.AreEqual(TranslationDecision.Ineligible, outcome.Decision);
        Assert.AreEqual("oversize", outcome.Category);
        Assert.AreEqual(1, outcome.ExcessCharacters);
        Assert.AreEqual(0, client.CallCount);
    }

    [TestMethod]
    public async Task AutoResolvedLanguageEqualToTargetEndsWithoutTransformation()
    {
        using var client = new ScriptedTranslationClient(
            "{\"status\":\"eligible\",\"language\":\"ru\"}");

        var outcome = await TranslationPipeline.TranslateAsync(
            new TranslationInput(SourceFor("ru"), Target: "ru"),
            client);

        Assert.AreEqual(TranslationDecision.Ineligible, outcome.Decision);
        Assert.AreEqual("equal-source-target", outcome.Category);
        Assert.AreEqual(0, outcome.TransformationDispatches);
        Assert.AreEqual(1, client.CallCount);
        Assert.IsNull(outcome.Text);
    }

    [TestMethod]
    public async Task EligibilityFailureIsRecordedFailureWithoutTransformation()
    {
        using var client = new ScriptedTranslationClient("{\"status\":\"eligible\"}");

        var outcome = await TranslationPipeline.TranslateAsync(
            new TranslationInput(SourceFor("en"), Target: "ru"),
            client);

        Assert.AreEqual(TranslationDecision.Failed, outcome.Decision);
        Assert.AreEqual("invalid-envelope", outcome.Category);
        Assert.AreEqual(0, outcome.TransformationDispatches);
        Assert.AreEqual(1, client.CallCount);
    }

    private sealed class ScriptedTranslationClient : IChatClient
    {
        private readonly Queue<object> scripted;

        public ScriptedTranslationClient(params object[] responses)
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
                Assert.Fail("The translation pipeline dispatched more than the scripted responses allow.");
            }

            var next = scripted.Dequeue();
            return next switch
            {
                string text => Task.FromResult(
                    new ChatResponse(new ChatMessage(ChatRole.Assistant, text))),
                ChatResponse response => Task.FromResult(response),
                Exception failure => Task.FromException<ChatResponse>(failure),
                _ => throw new InvalidOperationException("Unsupported scripted translation response."),
            };
        }

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("The translation pipeline never streams.");
    }
}
