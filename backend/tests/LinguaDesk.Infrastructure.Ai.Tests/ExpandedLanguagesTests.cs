using System.Text;
using System.Text.Json;
using LinguaDesk.Core;
using LinguaDesk.Infrastructure.Ai;
using LinguaDesk.Ai.Evaluation;
using Microsoft.Extensions.AI;

namespace LinguaDesk.Infrastructure.Ai.Tests;

[TestClass]
public sealed class ExpandedLanguagesTests
{
    [TestMethod]
    public void FrozenDevelopmentManifestCoversOnlyTheSelectedMatrix()
    {
        var manifest = M046Suite.Load();
        Assert.AreEqual(264, manifest.Counts["translation"]);
        Assert.AreEqual(1116, manifest.Counts["rewriting"]);
        Assert.AreEqual(1116, manifest.Counts["alternatives"]);
    }

    [TestMethod]
    public void ReducedLiveManifestHasExactlyOneHundredDiverseCasesWithoutExcludedScripts()
    {
        var manifest = M046Suite.Load(reduced: true);
        Assert.HasCount(100, manifest.Cases);
        Assert.AreEqual(16, manifest.Counts["translation"]);
        Assert.AreEqual(40, manifest.Counts["rewriting"]);
        Assert.AreEqual(36, manifest.Counts["alternatives"]);
        Assert.AreEqual(8, manifest.Counts["eligibility"]);
        Assert.HasCount(33, manifest.Cases.Where(c => c.Operation != "eligibility").Select(c => c.Language).Distinct());
        foreach (var operation in new[] { "rewriting", "alternatives" })
            Assert.HasCount(9, manifest.Cases.Where(c => c.Operation == operation).Select(c => c.Mode).Distinct());
    }

    [TestMethod]
    public async Task FullMatrixCannotAccidentallyDispatchBeyondTheReducedLiveScope()
    {
        var output = Path.Combine(Path.GetTempPath(), "m046-no-dispatch-" + Guid.NewGuid().ToString("N") + ".json");
        var exit = await M046Suite.RunAsync("translation", ["--suite", "m046", "--live", "--profile", "deliberately-unknown", "--max-dispatches", "528", "--max-spend-usd", "0.10", "--output", output]);
        Assert.AreEqual(2, exit);
        Assert.IsFalse(File.Exists(output));
    }

    [TestMethod]
    public async Task EveryCanonicalCodeTraversesBothFamiliesWithoutAdditionalStages()
    {
        foreach (var language in ProductCatalog.Languages)
        {
            using var client = new FixtureClient(language.Id);
            var translation = await TranslationPipeline.TranslateAsync(new("Synthetic fixture.", language.Id, language.Id == "en" ? "ar" : "en"), client);
            Assert.AreEqual(TranslationDecision.Succeeded, translation.Decision, language.Id);
            Assert.AreEqual(2, translation.TotalDispatches, language.Id);
            var rewriting = await RewritingPipeline.RewriteAsync(new("Synthetic fixture.", language.Id), client);
            Assert.AreEqual(RewritingDecision.Succeeded, rewriting.Decision, language.Id);
            Assert.AreEqual(2, rewriting.TotalDispatches, language.Id);
            Assert.IsTrue(EligibilityEnvelopeParser.TryParse(JsonSerializer.Serialize(new { status = "eligible", language = language.Id }), language.Id, out _, out _));
            Assert.IsFalse(EligibilityEnvelopeParser.TryParse(JsonSerializer.Serialize(new { status = "eligible", language = language.Id }), language.Id == "en" ? "ar" : "en", out _, out _));
        }
    }

    [TestMethod]
    public void AliasesAndWrongCasesAreNeverNormalizedIntoTheCatalog()
    {
        foreach (var id in new[] { "EN", "en-US", "zh-Hant", "sr-Latn", "tl", "no", "ara", "unknown", "", "123", null })
        {
            Assert.IsFalse(ProductCatalog.IsLanguage(id), id);
            Assert.IsFalse(EligibilityEnvelopeParser.TryParse(JsonSerializer.Serialize(new { status = "eligible", language = id }), null, out _, out _), id);
        }
    }

    [TestMethod]
    public void CatalogAndSelectedScriptRulesAreTrustedAndHashedWithoutUserText()
    {
        var eligibility = EligibilityPrompt.Create("untrusted input");
        foreach (var language in ProductCatalog.Languages)
            StringAssert.Contains(eligibility.Messages[0].Content, language.Id + "=" + language.Name);
        var translation = TranslationPrompt.Create("untrusted input", "ar", "sr");
        StringAssert.Contains(translation.Messages[0].Content, ProductCatalog.GetLanguage("sr").Policy);
        Assert.DoesNotContain("untrusted input", translation.Messages[0].Content);
        Assert.DoesNotContain("af=Afrikaans", translation.Messages[0].Content);
        Assert.AreEqual(translation.ResourceSha256, TranslationPrompt.Create("different text", "ar", "sr").ResourceSha256);
        Assert.AreNotEqual(translation.ResourceSha256, TranslationPrompt.Create("different text", "ar", "zh").ResourceSha256);
    }

    [TestMethod]
    public void ModelDataRetainsNativeLettersAndExactJsonDataBoundaries()
    {
        foreach (var source in new[] { "ስብሰባ", "შეხვედრა", "មីរ៉ា", "बैठक", "التقارير", "Δευτέρα" })
        {
            var hostile = source + "\" <tag>\n\\\t";
            foreach (var snapshot in new[] { EligibilityPrompt.Create(hostile), TranslationPrompt.Create(hostile, "ar", "he"), RewritingPrompt.Create(hostile, "ar", "correctionOnly") })
            {
                StringAssert.Contains(snapshot.Messages[1].Content, source);
                using var data = JsonDocument.Parse(snapshot.Messages[1].Content);
                Assert.AreEqual(hostile, data.RootElement.GetProperty("source").GetString());
                Assert.DoesNotContain(hostile, snapshot.Messages[0].Content);
            }
            var alternatives = AlternativesPrompt.Create(new(source, 0, source.Length, Guid.NewGuid(), "ar"));
            StringAssert.Contains(alternatives.Messages[1].Content, source);
            using var alternativeData = JsonDocument.Parse(alternatives.Messages[1].Content);
            Assert.AreEqual(source, alternativeData.RootElement.GetProperty("context").GetString());
            Assert.AreEqual(source, alternativeData.RootElement.GetProperty("selectedText").GetString());
        }
    }

    [TestMethod]
    public void EveryLanguageFitsAlternativesScaffoldAndMaximumEscapedContext()
    {
        foreach (var language in ProductCatalog.Languages)
        foreach (var scalar in new[] { "😀", "\u0001", "\"", "क", "\\" })
        {
            var sentence = string.Concat(Enumerable.Repeat(scalar, 2000));
            var input = new AlternativesInput(sentence + "\n" + string.Concat(Enumerable.Repeat(scalar, 5999)), 0, 2000, Guid.NewGuid(), language.Id, "correctionOnly");
            var view = AlternativesProfile.ForInput(CandidateRegistry.Default[0], input);
            Assert.IsLessThanOrEqualTo(131072, view.Bounds.MaxInputTokens, language.Id);
            Assert.IsLessThanOrEqualTo(4096, Encoding.UTF8.GetByteCount(AlternativesPrompt.Create(input).Messages[0].Content) + 200, language.Id);
        }
    }

    [TestMethod]
    public void GreekOptionsAreValidatedInTheirResolvedLanguage()
    {
        const string json = """{"status":"result","options":["Ready; Yes.","Prepared; Certainly."]}""";
        Assert.IsTrue(AlternativesEnvelopeParser.TryParse(json, "Original.", out _, "en"));
        Assert.IsFalse(AlternativesEnvelopeParser.TryParse(json, "Original.", out _, "el"));
        Assert.IsTrue(AlternativesInputPolicy.Validate("Έτοιμο; Ναι.", 0, 7, Guid.NewGuid(), "el").IsValid);
        Assert.IsFalse(AlternativesInputPolicy.Validate("Έτοιμο; Ναι.", 0, 7, Guid.NewGuid(), "en").IsValid);
    }

    [TestMethod]
    public void FullTextMaximumUnicodeFitsFiniteViewsAndPreservesOutputAndSpendLimits()
    {
        var profile = CandidateRegistry.Default[0];
        foreach (var scalar in new[] { "😀", "\u0001", "文", "\"", "\\" })
        {
            var source = string.Concat(Enumerable.Repeat(scalar, 5000));
            foreach (var target in ProductCatalog.Languages)
            {
                var input = new TranslationInput(source, null, target.Id);
                var view = FullTextProfile.ForTranslation(profile, input);
                Assert.IsLessThanOrEqualTo(65536, view.Bounds.MaxInputTokens);
                Assert.AreEqual(profile.Bounds.MaxOutputTokens, view.Bounds.MaxOutputTokens);
                Assert.AreEqual(profile.Bounds.MaxResponseBytes, view.Bounds.MaxResponseBytes);
                Assert.IsLessThanOrEqualTo(view.Bounds.MaxInputTokens, ChatCompletionsAdapter.ConservativeInputTokenBound(view, EligibilityPrompt.Create(source).Messages));
                Assert.IsLessThanOrEqualTo(0.05m, 2 * EvaluationBudget.UpperBoundUsd(view.Bounds.MaxInputTokens, view.Bounds.MaxOutputTokens, profile.Billing.PeakInputPerMillionTokens, profile.Billing.PeakOutputPerMillionTokens));
            }
            var rewriting = FullTextProfile.ForRewriting(profile, new(string.Concat(Enumerable.Repeat(scalar, 2000))));
            Assert.IsLessThanOrEqualTo(65536, rewriting.Bounds.MaxInputTokens);
        }
        Assert.AreEqual(8192, profile.Bounds.MaxInputTokens);
    }

    private sealed class FixtureClient(string language) : IChatClient
    {
        public void Dispose() { }
        public object? GetService(Type serviceType, object? serviceKey = null) => null;
        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            using var data = JsonDocument.Parse(messages.Last().Text);
            var response = data.RootElement.TryGetProperty("sourceHint", out _) ? JsonSerializer.Serialize(new { status = "eligible", language }) : """{"status":"result","text":"Synthetic complete result."}""";
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, response)));
        }
        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
