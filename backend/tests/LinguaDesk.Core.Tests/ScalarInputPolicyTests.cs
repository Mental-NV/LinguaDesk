using System.Text.Json;
using LinguaDesk.Core;

namespace LinguaDesk.Core.Tests;

[TestClass]
public sealed class ScalarInputPolicyTests
{
    private static readonly JsonSerializerOptions FixtureSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };
    private static readonly string[] ExpectedLanguages = ["en:English", "ru:Russian", "ro:Romanian", "zh:Chinese"];
    private static readonly string[] ExpectedSources = ["auto", "en", "ru", "ro", "zh"];
    private static readonly string[] ExpectedChineseInputScripts = ["simplified", "traditional"];
    private static readonly string[] ExpectedWhitespaceRanges =
    [
        "U+0009-U+000D", "U+0020", "U+0085", "U+00A0", "U+1680",
        "U+2000-U+200A", "U+2028-U+2029", "U+202F", "U+205F", "U+3000",
    ];
    private static readonly string[] ExpectedDirections =
    [
        "en:ru", "en:ro", "en:zh", "ru:en", "ru:ro", "ru:zh",
        "ro:en", "ro:ru", "ro:zh", "zh:en", "zh:ru", "zh:ro",
    ];
    private static readonly string[] ExpectedModes =
    [
        "correctionOnly:correction", "simple:style", "casual:style", "business:style",
        "academic:style", "enthusiastic:tone", "friendly:tone", "confident:tone", "diplomatic:tone",
    ];
    private static readonly Fixture FixtureData = LoadFixture();

    [TestMethod]
    public void CatalogContainsOnlyTheSelectedOrderedValues()
    {
        CollectionAssert.AreEqual(
            ExpectedLanguages,
            ProductCatalog.Languages.Select(static item => $"{item.Id}:{item.Name}").ToArray());
        CollectionAssert.AreEqual(
            ExpectedSources,
            ProductCatalog.SourceSelections.ToArray());
        CollectionAssert.AreEqual(ExpectedChineseInputScripts, ProductCatalog.ChineseInputScripts.ToArray());
        Assert.AreEqual("simplified", ProductCatalog.ChineseOutputScript);
        CollectionAssert.AreEqual(
            ExpectedDirections,
            ProductCatalog.TranslationDirections.Select(static item => $"{item.Source}:{item.Target}").ToArray());
        CollectionAssert.AreEqual(
            ExpectedModes,
            ProductCatalog.RewritingModes.Select(static item => $"{item.Id}:{item.Kind}").ToArray());
    }

    [TestMethod]
    public void ValidFixtureCasesHaveCanonicalCountsAndWhitespaceDisposition()
    {
        Assert.AreEqual(ScalarInputPolicy.Id, FixtureData.PolicyId);

        foreach (var fixtureCase in FixtureData.ValidCases)
        {
            var result = ScalarInputPolicy.Analyze(fixtureCase.Text, 10_000);
            Assert.IsTrue(result.IsUnicodeValid, fixtureCase.Id);
            Assert.AreEqual(fixtureCase.ExpectedCount, result.ScalarCount, fixtureCase.Id);
            Assert.AreEqual(fixtureCase.ExpectedWhitespaceOnly, result.IsEmptyOrWhitespace, fixtureCase.Id);
            Assert.AreEqual(fixtureCase.Text, result.Source, fixtureCase.Id);
            Assert.AreEqual(!fixtureCase.ExpectedWhitespaceOnly, result.IsValid, fixtureCase.Id);
        }
    }

    [TestMethod]
    public void EveryAdvertisedWhitespaceScalarIsRecognizedIndividually()
    {
        CollectionAssert.AreEqual(
            ExpectedWhitespaceRanges,
            ScalarInputPolicy.WhitespaceCodePointRanges.ToArray());
        Assert.HasCount(25, FixtureData.WhitespaceCodePoints);
        foreach (var codePoint in FixtureData.WhitespaceCodePoints)
        {
            Assert.IsTrue(ScalarInputPolicy.IsWhitespaceScalar(codePoint), $"U+{codePoint:X4}");
            var result = ScalarInputPolicy.Analyze(char.ConvertFromUtf32(codePoint), 10);
            Assert.IsTrue(result.IsEmptyOrWhitespace, $"U+{codePoint:X4}");
        }

        Assert.IsFalse(ScalarInputPolicy.IsWhitespaceScalar(0x200B));
        Assert.IsFalse(ScalarInputPolicy.IsWhitespaceScalar(0x200D));
        Assert.IsFalse(ScalarInputPolicy.IsWhitespaceScalar(0x0301));
    }

    [TestMethod]
    public void MalformedUtf16FixtureCasesAreRejectedWithoutReplacementCounts()
    {
        foreach (var fixtureCase in FixtureData.MalformedCases)
        {
            var source = new string(fixtureCase.Utf16CodeUnits.Select(static value => (char)value).ToArray());
            var result = ScalarInputPolicy.Analyze(source, 10);
            Assert.IsFalse(result.IsUnicodeValid, fixtureCase.Id);
            Assert.IsNull(result.ScalarCount, fixtureCase.Id);
            Assert.IsFalse(result.IsValid, fixtureCase.Id);
        }
    }

    [TestMethod]
    public void BothOperationLimitsAcceptLAndRejectLPlusOneWhole()
    {
        foreach (var fixtureCase in FixtureData.LimitCases)
        {
            var source = string.Concat(Enumerable.Repeat(fixtureCase.Scalar, fixtureCase.Repeat));
            var result = ScalarInputPolicy.Analyze(source, fixtureCase.Maximum);
            Assert.AreEqual(fixtureCase.Repeat, result.ScalarCount, fixtureCase.Id);
            Assert.AreEqual(fixtureCase.ExpectedValid, result.IsValid, fixtureCase.Id);
            Assert.AreEqual(fixtureCase.ExpectedExcess, result.Excess, fixtureCase.Id);
            Assert.AreEqual(source, result.Source, fixtureCase.Id);
        }
    }

    [TestMethod]
    public void TranslationSelectorsDefaultAndRejectInvalidOrSameLanguageChoices()
    {
        var defaulted = ScalarInputPolicy.ValidateTranslation("text", 5000, null, "en");
        Assert.IsTrue(defaulted.IsValid);
        Assert.AreEqual("auto", defaulted.EffectiveSourceSelection);

        Assert.IsFalse(ScalarInputPolicy.ValidateTranslation("text", 5000, "xx", "en").IsValid);
        Assert.IsFalse(ScalarInputPolicy.ValidateTranslation("text", 5000, "en", null).IsValid);
        Assert.IsFalse(ScalarInputPolicy.ValidateTranslation("text", 5000, "en", "en").IsValid);
        Assert.IsTrue(ScalarInputPolicy.ValidateTranslation("text", 5000, "en", "ru").IsValid);
    }

    [TestMethod]
    public void RewritingSelectorsDefaultAndAcceptExactlyOneKnownMode()
    {
        var defaulted = ScalarInputPolicy.ValidateRewriting("text", 2000, null, null);
        Assert.IsTrue(defaulted.IsValid);
        Assert.AreEqual("auto", defaulted.EffectiveSourceSelection);
        Assert.AreEqual("correctionOnly", defaulted.EffectiveMode);

        Assert.IsTrue(ScalarInputPolicy.ValidateRewriting("text", 2000, "ro", "friendly").IsValid);
        Assert.IsFalse(ScalarInputPolicy.ValidateRewriting("text", 2000, "xx", "friendly").IsValid);
        Assert.IsFalse(ScalarInputPolicy.ValidateRewriting("text", 2000, "ro", "friendly,business").IsValid);
        Assert.IsFalse(ScalarInputPolicy.ValidateRewriting("text", 2000, "ro", string.Empty).IsValid);
    }

    [TestMethod]
    public void EmptyWhitespaceMalformedAndOversizedInputsFailBeforeSelectorsCanPass()
    {
        Assert.IsFalse(ScalarInputPolicy.ValidateTranslation(string.Empty, 5000, null, "ru").IsValid);
        Assert.IsFalse(ScalarInputPolicy.ValidateTranslation("\t\u3000", 5000, null, "ru").IsValid);
        Assert.IsFalse(ScalarInputPolicy.ValidateRewriting(new string('\uD800', 1), 2000, null, null).IsValid);
        Assert.IsFalse(ScalarInputPolicy.ValidateRewriting(new string('a', 2001), 2000, null, null).IsValid);
    }

    [TestMethod]
    public void AnalysisIsDeterministicAndNeverNormalizesTheSource()
    {
        const string source = " é\r\n😀 ";
        var first = ScalarInputPolicy.Analyze(source, 20);
        var second = ScalarInputPolicy.Analyze(source, 20);

        Assert.AreEqual(first, second);
        Assert.AreEqual(source, first.Source);
        Assert.AreEqual(7, first.ScalarCount);
    }

    [TestMethod]
    public void MaximumMustBePositive()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => ScalarInputPolicy.Analyze("text", 0));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => ScalarInputPolicy.Analyze("text", -1));
    }

    private static Fixture LoadFixture()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "unicode-scalar-v1.json");
        var fixture = JsonSerializer.Deserialize<Fixture>(
            File.ReadAllText(path),
            FixtureSerializerOptions);
        return fixture ?? throw new InvalidOperationException($"Could not deserialize fixture '{path}'.");
    }

    private sealed record Fixture(
        string PolicyId,
        int[] WhitespaceCodePoints,
        ValidFixtureCase[] ValidCases,
        MalformedFixtureCase[] MalformedCases,
        LimitFixtureCase[] LimitCases);

    private sealed record ValidFixtureCase(
        string Id,
        string Text,
        int ExpectedCount,
        bool ExpectedWhitespaceOnly);

    private sealed record MalformedFixtureCase(string Id, int[] Utf16CodeUnits);

    private sealed record LimitFixtureCase(
        string Id,
        string Family,
        string Scalar,
        int Repeat,
        int Maximum,
        bool ExpectedValid,
        int ExpectedExcess);
}
