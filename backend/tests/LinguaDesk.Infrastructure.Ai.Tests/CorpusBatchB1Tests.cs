using System.Text.Json;
using LinguaDesk.Core;

namespace LinguaDesk.Infrastructure.Ai.Tests;

// Offline validation for M035 batch B1 (first frozen release-corpus batch).
// No provider dispatch, no credential read, no network: pure file + policy checks.
// Development-slice IDs/sources below are transcribed from
// backend/tools/LinguaDesk.Ai.Evaluation/EvaluateTranslation.cs and
// backend/tools/LinguaDesk.Ai.Evaluation/EvaluateRewriting.cs; B1 must stay
// disjoint from them by ID and by exact source text.
[TestClass]
public sealed class CorpusBatchB1Tests
{
    private const string ExpectedSchemaRevision = "corpus-b1.v1";

    private static readonly string[] DevelopmentTranslationIds =
    [
        "tr-en-ru", "tr-en-ro", "tr-en-zh", "tr-ru-en", "tr-ru-ro", "tr-ru-zh",
        "tr-ro-en", "tr-ro-ru", "tr-ro-zh", "tr-zh-hans-en", "tr-zh-hant-ru", "tr-zh-hans-ro",
        "short-ambiguous", "refusal-scripted", "malformed-scripted", "gate-empty", "gate-oversize",
    ];

    private static readonly string[] DevelopmentSources =
    [
        "Please send the report tomorrow.",
        "Пожалуйста, пришлите отчёт завтра.",
        "Vă rog să trimiteți raportul mâine.",
        "请明天发送报告。",
        "請明天發送報告。",
        "请在周五之前发布季度结果。",
        "12345",
        "   ",
    ];

    private static readonly string[] RequiredTagClasses =
        ["fidelity-risk", "paragraph/list", "instruction-as-content", "upper-length-band"];

    private static readonly string[] AdditionalDevelopmentIds =
        ["short-ambiguous", "refusal-scripted", "malformed-scripted", "gate-empty", "gate-oversize", "invalid-mode"];

    private static readonly string[] AllForbiddenIds = DevelopmentTranslationIds.Concat(AdditionalDevelopmentIds).ToArray();

    private static readonly string[] SecretSentinels = ["APIKEY", "BEGIN PRIVATE", "BEGIN RSA", "sk-live"];

    private static readonly string[] Families = ["translation", "rewriting"];

    private static JsonDocument LoadBatch(out string rawJson)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "scripts", "ai.sh")))
            {
                break;
            }

            directory = directory.Parent;
        }

        Assert.IsNotNull(directory, "Repository root (scripts/ai.sh) was not found.");
        var path = Path.Combine(
            directory.FullName, "backend", "tools", "LinguaDesk.Ai.Evaluation", "Corpus", "batch-b1.json");
        Assert.IsTrue(File.Exists(path), $"Batch B1 file is missing: {path}.");
        rawJson = File.ReadAllText(path);
        return JsonDocument.Parse(rawJson);
    }

    private static List<JsonElement> LoadCases(out string rawJson)
    {
        using var batch = LoadBatch(out rawJson);
        Assert.AreEqual(ExpectedSchemaRevision, batch.RootElement.GetProperty("schemaRevision").GetString());
        Assert.AreEqual("B1", batch.RootElement.GetProperty("batchId").GetString());
        return batch.RootElement.GetProperty("cases").EnumerateArray().Select(element => element.Clone()).ToList();
    }

    private static List<JsonElement> Family(List<JsonElement> cases, string family) =>
        cases.Where(c => c.GetProperty("family").GetString() == family).ToList();

    [TestMethod]
    public void BatchHoldsExactlyTwentyFourCases()
    {
        var cases = LoadCases(out _);

        Assert.HasCount(24, cases);
        Assert.HasCount(12, Family(cases, "translation"));
        Assert.HasCount(12, Family(cases, "rewriting"));
    }

    [TestMethod]
    public void TranslationCoversAllTwelveDirectedPairs()
    {
        var cases = LoadCases(out _);
        var pairs = Family(cases, "translation")
            .Select(c => c.GetProperty("operation").GetProperty("sourceSelection").GetString()
                + "->" + c.GetProperty("operation").GetProperty("target").GetString())
            .OrderBy(pair => pair, StringComparer.Ordinal)
            .ToList();
        var expected = ProductCatalog.TranslationDirections
            .Select(direction => direction.Source + "->" + direction.Target)
            .OrderBy(pair => pair, StringComparer.Ordinal)
            .ToList();

        CollectionAssert.AreEqual(expected.ToArray(), pairs.ToArray());
    }

    [TestMethod]
    public void ChineseSourceCasesLabelBothScripts()
    {
        var cases = LoadCases(out _);
        var chineseSource = Family(cases, "translation")
            .Where(c => c.GetProperty("operation").GetProperty("sourceSelection").GetString() == "zh")
            .ToList();

        Assert.HasCount(3, chineseSource);
        var scripts = chineseSource.Select(c => c.GetProperty("chineseScript").GetString()).ToList();
        Assert.Contains("simplified", scripts, "At least one Hans Chinese-source case is required.");
        Assert.Contains("traditional", scripts, "At least one Hant Chinese-source case is required.");
    }

    [TestMethod]
    public void RewritingCoversThreeModesPerLanguageWithCorrectionOnly()
    {
        var cases = LoadCases(out _);

        foreach (var language in ProductCatalog.Languages)
        {
            var modes = Family(cases, "rewriting")
                .Where(c => c.GetProperty("operation").GetProperty("language").GetString() == language.Id)
                .Select(c => c.GetProperty("operation").GetProperty("mode").GetString())
                .ToList();

            Assert.HasCount(3, modes, $"Language {language.Id} must have exactly three B1 cases.");
            Assert.Contains(ProductCatalog.DefaultRewritingMode, modes);
            Assert.AreEqual(modes.Count, modes.Distinct().Count(), "Modes must be distinct per language.");
            foreach (var mode in modes)
            {
                Assert.IsTrue(ProductCatalog.IsRewritingMode(mode), $"Unknown rewriting mode: {mode}.");
            }
        }
    }

    [TestMethod]
    public void CaseIdsAreUniqueStableAndDisjointFromDevelopmentSlices()
    {
        var cases = LoadCases(out _);
        var ids = cases.Select(c => c.GetProperty("id").GetString()!).ToList();

        Assert.AreEqual(ids.Count, ids.Distinct(StringComparer.Ordinal).Count(), "Case IDs must be unique.");
        foreach (var id in ids)
        {
            Assert.IsTrue(id.StartsWith("b1-", StringComparison.Ordinal), $"B1 ID must carry the b1- prefix: {id}.");
        }

        foreach (var id in ids)
        {
            var reusesForbidden = AllForbiddenIds.Contains(id, StringComparer.Ordinal)
                || id.StartsWith("rw-", StringComparison.Ordinal)
                || id.StartsWith("tr-", StringComparison.Ordinal);
            Assert.IsFalse(reusesForbidden, $"B1 ID reuses a development-slice ID: {id}.");
        }
    }

    [TestMethod]
    public void RequiredFieldsTagsAndCoverageMatrixPerFamily()
    {
        var cases = LoadCases(out _);

        foreach (var kase in cases)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(kase.GetProperty("sourceText").GetString()));
            Assert.IsFalse(string.IsNullOrWhiteSpace(kase.GetProperty("provenance").GetString()));
            Assert.IsFalse(string.IsNullOrWhiteSpace(kase.GetProperty("license").GetString()));
            Assert.IsFalse(string.IsNullOrWhiteSpace(kase.GetProperty("expectedEligibility").GetString()));
            Assert.IsGreaterThanOrEqualTo(1, kase.GetProperty("meaningAssertions").GetArrayLength());
            Assert.IsFalse(string.IsNullOrWhiteSpace(kase.GetProperty("acceptableOutputNotes").GetString()));
            Assert.IsTrue(kase.TryGetProperty("canonicalScalarCount", out var count) && count.GetInt32() > 0);
            var tags = kase.GetProperty("coverageTags").EnumerateArray().Select(t => t.GetString()).ToList();
            Assert.IsNotEmpty(tags, "Every case must carry at least one coverage tag.");
        }

        foreach (var family in Families)
        {
            var tags = Family(cases, family)
                .SelectMany(c => c.GetProperty("coverageTags").EnumerateArray().Select(t => t.GetString()))
                .ToList();
            foreach (var required in RequiredTagClasses)
            {
                Assert.Contains(required, tags, $"Family {family} is missing tag class {required}.");
            }
        }
    }

    [TestMethod]
    public void CanonicalScalarCountsRecomputedAndWithinLimits()
    {
        var cases = LoadCases(out _);

        foreach (var kase in cases)
        {
            var source = kase.GetProperty("sourceText").GetString()!;
            var recorded = kase.GetProperty("canonicalScalarCount").GetInt32();
            var family = kase.GetProperty("family").GetString();
            var maximum = family == "translation"
                ? ProductCatalog.TranslationMaximumSourceCharacters
                : ProductCatalog.RewritingMaximumSourceCharacters;

            var analysis = ScalarInputPolicy.Analyze(source, maximum);
            Assert.IsTrue(analysis.IsUnicodeValid);
            Assert.AreEqual(recorded, analysis.ScalarCount, $"Scalar-count mismatch on {kase.GetProperty("id").GetString()}.");
            Assert.IsTrue(analysis.IsValid, "B1 cases must be eligible-size, non-blank inputs.");
            Assert.AreEqual("eligible", kase.GetProperty("expectedEligibility").GetString());
            Assert.IsFalse(analysis.IsOversized);
        }
    }

    [TestMethod]
    public void ChineseOutputCasesCarrySimplifiedPolicyNote()
    {
        var cases = LoadCases(out _);

        var chineseOutput = cases.Where(kase =>
            (kase.GetProperty("family").GetString() == "translation"
                && kase.GetProperty("operation").GetProperty("target").GetString() == "zh")
            || (kase.GetProperty("family").GetString() == "rewriting"
                && kase.GetProperty("operation").GetProperty("language").GetString() == "zh")).ToList();

        Assert.IsNotEmpty(chineseOutput);
        foreach (var kase in chineseOutput)
        {
            StringAssert.Contains(
                kase.GetProperty("acceptableOutputNotes").GetString()!,
                "Simplified",
                $"Missing Simplified-output policy note on {kase.GetProperty("id").GetString()}.");
        }
    }

    [TestMethod]
    public void SourcesDisjointFromDevelopmentSlicesAndFreeOfSecrets()
    {
        var cases = LoadCases(out var rawJson);
        var sources = cases.Select(c => c.GetProperty("sourceText").GetString()!).ToList();

        Assert.AreEqual(sources.Count, sources.Distinct(StringComparer.Ordinal).Count(), "B1 sources must be distinct.");
        foreach (var source in sources)
        {
            var reusesDevelopment = DevelopmentSources.Contains(source, StringComparer.Ordinal);
            Assert.IsFalse(reusesDevelopment, "B1 source reuses a development-slice text.");
            Assert.IsLessThan(ProductCatalog.TranslationMaximumSourceCharacters + 1, source.Length);
        }

        foreach (var sentinel in SecretSentinels)
        {
            var leaked = rawJson.Contains(sentinel, StringComparison.Ordinal);
            Assert.IsFalse(leaked, $"Secret sentinel in batch file: {sentinel}.");
        }
    }

    [TestMethod]
    public void ReviewMetadataBlocksPresentForHumanGate()
    {
        var cases = LoadCases(out _);

        foreach (var kase in cases)
        {
            var review = kase.GetProperty("review");
            Assert.IsFalse(string.IsNullOrWhiteSpace(review.GetProperty("author").GetProperty("role").GetString()));
            Assert.IsFalse(string.IsNullOrWhiteSpace(review.GetProperty("independentReviewer").GetProperty("role").GetString()));
        }
    }
}
