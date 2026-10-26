using System.Text;
using System.Text.Json;
using LinguaDesk.Core;
using LinguaDesk.Infrastructure.Ai;
using Microsoft.Extensions.AI;

namespace LinguaDesk.Infrastructure.Ai.Tests;

[TestClass]
public sealed class AlternativesTests
{
    private static AlternativesInput Input(string context = "The report is ready. Next sentence.") => new(context, 0, 20, Guid.NewGuid(), "en");

    [TestMethod]
    [DataRow("{\"status\":\"result\",\"options\":[\"Ready.\",\"Ready.\"]}")]
    [DataRow("{\"status\":\"result\",\"options\":[\"Ready.\"]}")]
    [DataRow("{\"status\":\"result\",\"options\":[\"The report is ready.\",\"Different.\"]}")]
    [DataRow("{\"status\":\"result\",\"options\":[\" \u0020\",\"Different.\"]}")]
    [DataRow("{\"status\":\"result\",\"options\":[\"Two. Sentences.\",\"Different.\"]}")]
    [DataRow("{\"status\":\"result\",\"options\":[\"Ready.\",\"Different.\"],\"extra\":0}")]
    [DataRow("{\"status\":\"result\",\"status\":\"result\",\"options\":[\"Ready.\",\"Different.\"]}")]
    [DataRow("{\"status\":\"refused\",\"options\":[\"Ready.\",\"Different.\"]}")]
    public void StrictParserRejectsUnusableSets(string json) => Assert.IsFalse(AlternativesEnvelopeParser.TryParse(json, "The report is ready.", out _));

    [TestMethod]
    [DataRow("למירה יש 12 ספרים", "למירה יש 12 ספרים.", "ברשות מירה 12 ספרים.")]
    [DataRow("मीरा के पास 12 किताबें हैं", "मीरा के पास 12 किताबें हैं।", "मीरा 12 किताबों की मालकिन है।")]
    [DataRow("التقرير جاهز.", "التقرير جاهز!", "التقرير مكتمل.")]
    [DataRow("报告准备好了。", "报告已完成。", "报告已完成！")]
    public void PunctuationAndWhitespaceAloneCannotCreateAlternativeWording(string selected, string first, string second)
    {
        var json = JsonSerializer.Serialize(new { status = "result", options = new[] { first, second } });
        Assert.IsFalse(AlternativesEnvelopeParser.TryParse(json, selected, out _));
    }

    [TestMethod]
    public void RealNativeWordingRemainsExactAndAccepted()
    {
        var expected = new[] { "מירה מחזיקה ב-12 ספרים.", "ברשות מירה 12 ספרים." };
        var json = JsonSerializer.Serialize(new { status = "result", options = expected });
        Assert.IsTrue(AlternativesEnvelopeParser.TryParse(json, "למירה יש 12 ספרים", out var parsed, "he"));
        CollectionAssert.AreEqual(expected, parsed!.Options!.ToArray());
    }

    [TestMethod]
    public async Task RewritingChainUsesBoundedContextAndAtMostThreeDispatches()
    {
        var chain = FamilyChain.EvaluationDefault(ChainFamily.Rewriting);
        using var primary = new Client("{\"status\":\"eligible\",\"language\":\"en\"}", "{\"status\":\"result\",\"options\":[]}");
        using var fallback = new Client("{\"status\":\"result\",\"options\":[\"The report is prepared.\",\"The report is complete.\"]}");
        var result = await ChainOrchestrator.ExecuteAlternativesAsync(chain, Input(), profile => profile.CandidateId == chain.Primary.CandidateId ? primary : fallback);
        Assert.AreEqual(ChainDecision.Succeeded, result.Decision);
        Assert.AreEqual(3, result.Dispatches);
        Assert.HasCount(2, result.Options!);
        using var prompt = JsonDocument.Parse(fallback.LastUserText!);
        Assert.AreEqual(Input().Context, prompt.RootElement.GetProperty("context").GetString());
        Assert.AreEqual("The report is ready.", prompt.RootElement.GetProperty("selectedText").GetString());
    }

    [TestMethod]
    public async Task ExpandedContextEligibilityDoesNotUseFullRewritingLimitAndRejectsTruncation()
    {
        var input = Input("The report is ready. " + new string('文', 7979));
        using var client = new Client("{\"status\":\"eligible\",\"language\":\"en\"}", "{\"status\":\"result\",\"options\":[\"Prepared.\",\"Complete.\"]}") { TruncateSecond = true };
        var chain = new FamilyChain(ChainFamily.Rewriting, CandidateRegistry.Default[0], null);
        var result = await ChainOrchestrator.ExecuteAlternativesAsync(chain, input, _ => client);
        Assert.AreEqual(2, client.Calls);
        Assert.AreEqual(ChainDecision.Failed, result.Decision);
        Assert.IsNull(result.Options);
    }

    [TestMethod]
    public void LegalMaximumEscapedUnicodePromptAndScaffoldingFitDeclaredFiniteBounds()
    {
        foreach (var scalar in new[] { "文", "😀", "\u0001", "\"", "\\" })
        {
            var sentence = string.Concat(Enumerable.Repeat(scalar, 2000));
            var context = sentence + "\n" + string.Concat(Enumerable.Repeat(scalar, 5999));
            var input = new AlternativesInput(context, 0, 2000, Guid.NewGuid(), "zh");
            var original = CandidateRegistry.Default[0];
            var view = AlternativesProfile.ForInput(original, input);
            Assert.IsLessThanOrEqualTo(AlternativesProfile.MaxInputTokens, view.Bounds.MaxInputTokens);
            Assert.IsLessThan(view.Bounds.ContextCapacityTokens, view.Bounds.MaxInputTokens + view.Bounds.MaxOutputTokens + 512);
            Assert.AreEqual(8192, view.Bounds.MaxOutputTokens);
            Assert.AreEqual(131072, view.Bounds.MaxResponseBytes);
            var prompt = AlternativesPrompt.Create(input);
            var scaffold = JsonSerializer.Serialize(new { context = "", selectedText = "", start = input.Start, length = input.Length, sourceLanguage = input.Language, mode = "correctionOnly" });
            Assert.IsLessThanOrEqualTo(4096, Encoding.UTF8.GetByteCount(prompt.Messages[0].Content) + Encoding.UTF8.GetByteCount(scaffold));
            Assert.IsLessThanOrEqualTo(view.Bounds.MaxInputTokens, ChatCompletionsAdapter.ConservativeInputTokenBound(view, prompt.Messages));
            Assert.AreEqual(8192, original.Bounds.MaxInputTokens);
            Assert.AreEqual(4096, original.Bounds.MaxOutputTokens);
        }
    }

    [TestMethod]
    public async Task CostDenialMakesNoProviderCall()
    {
        using var client = new Client();
        var outcome = await ChainOrchestrator.ExecuteAlternativesAsync(new(ChainFamily.Rewriting, CandidateRegistry.Default[0], null), Input(), _ => client, budget: new EvaluationBudget(3, 0.000001m));
        Assert.AreEqual(ChainDecision.BudgetDenied, outcome.Decision);
        Assert.AreEqual(0, client.Calls);
    }

    private sealed class Client(params string[] responses) : IChatClient
    {
        private readonly Queue<string> queue = new(responses);
        public int Calls { get; private set; }
        public string? LastUserText { get; private set; }
        public bool TruncateSecond { get; init; }
        public void Dispose() { }
        public object? GetService(Type serviceType, object? serviceKey = null) => null;
        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            Calls++; LastUserText = messages.Last().Text;
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, queue.Dequeue())) { FinishReason = Calls == 2 && TruncateSecond ? ChatFinishReason.Length : ChatFinishReason.Stop });
        }
        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
