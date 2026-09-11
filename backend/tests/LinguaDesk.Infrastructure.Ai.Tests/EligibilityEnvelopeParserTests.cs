using LinguaDesk.Infrastructure.Ai;

namespace LinguaDesk.Infrastructure.Ai.Tests;

[TestClass]
public sealed class EligibilityEnvelopeParserTests
{
    [TestMethod]
    [DataRow("en")]
    [DataRow("ru")]
    [DataRow("ro")]
    [DataRow("zh")]
    public void EligibleAcceptsEverySupportedLanguage(string language)
    {
        var accepted = EligibilityEnvelopeParser.TryParse(
            $"{{\"status\":\"eligible\",\"language\":\"{language}\"}}",
            null,
            out var classification,
            out var rejection);

        Assert.IsTrue(accepted);
        Assert.IsNull(rejection);
        Assert.AreEqual(new EligibilityClassification(EligibilityStatus.Eligible, language), classification);
    }

    [TestMethod]
    [DataRow("uncertain")]
    [DataRow("unsupported")]
    [DataRow("mixed")]
    [DataRow("refused")]
    public void NegativeClassesRequireNullLanguage(string status)
    {
        var accepted = EligibilityEnvelopeParser.TryParse(
            $"{{\"status\":\"{status}\",\"language\":null}}",
            null,
            out var classification,
            out var rejection);

        Assert.IsTrue(accepted);
        Assert.IsNull(rejection);
        Assert.IsNotNull(classification);
        Assert.IsNull(classification.Language);
    }

    [TestMethod]
    public void SourceMismatchRequiresDetectedLanguageDifferingFromHint()
    {
        var accepted = EligibilityEnvelopeParser.TryParse(
            "{\"status\":\"source_mismatch\",\"language\":\"ru\"}",
            "en",
            out var classification,
            out var rejection);

        Assert.IsTrue(accepted);
        Assert.IsNull(rejection);
        Assert.AreEqual(
            new EligibilityClassification(EligibilityStatus.SourceMismatch, "ru"),
            classification);
    }

    [TestMethod]
    public void SourceMismatchWithoutHintIsInvalid()
    {
        var accepted = EligibilityEnvelopeParser.TryParse(
            "{\"status\":\"source_mismatch\",\"language\":\"ru\"}",
            null,
            out var classification,
            out var rejection);

        Assert.IsFalse(accepted);
        Assert.IsNull(classification);
        Assert.IsNotNull(rejection);
    }

    [TestMethod]
    public void SourceMismatchEqualToHintIsInvalid()
    {
        var accepted = EligibilityEnvelopeParser.TryParse(
            "{\"status\":\"source_mismatch\",\"language\":\"ru\"}",
            "ru",
            out var classification,
            out _);

        Assert.IsFalse(accepted);
        Assert.IsNull(classification);
    }

    [TestMethod]
    public void EligibleContradictingValidatedHintIsInvalidOutput()
    {
        var accepted = EligibilityEnvelopeParser.TryParse(
            "{\"status\":\"eligible\",\"language\":\"ru\"}",
            "en",
            out var classification,
            out var rejection);

        Assert.IsFalse(accepted);
        Assert.IsNull(classification);
        Assert.IsNotNull(rejection);
    }

    [TestMethod]
    public void EligibleMatchingValidatedHintIsAccepted()
    {
        var accepted = EligibilityEnvelopeParser.TryParse(
            "{\"status\":\"eligible\",\"language\":\"en\"}",
            "en",
            out var classification,
            out _);

        Assert.IsTrue(accepted);
        Assert.AreEqual(new EligibilityClassification(EligibilityStatus.Eligible, "en"), classification);
    }

    [TestMethod]
    public void AutomaticHintDoesNotConstrainEligibleLanguage()
    {
        var accepted = EligibilityEnvelopeParser.TryParse(
            "{\"status\":\"eligible\",\"language\":\"zh\"}",
            "auto",
            out var classification,
            out _);

        Assert.IsTrue(accepted);
        Assert.AreEqual(new EligibilityClassification(EligibilityStatus.Eligible, "zh"), classification);
    }

    [TestMethod]
    [DataRow("{\"status\":\"eligible\",\"language\":null}", "eligible/null pairing")]
    [DataRow("{\"status\":\"uncertain\",\"language\":\"en\"}", "uncertain/language pairing")]
    [DataRow("{\"status\":\"unsupported\",\"language\":\"en\"}", "unsupported/language pairing")]
    [DataRow("{\"status\":\"mixed\",\"language\":\"ro\"}", "mixed/language pairing")]
    [DataRow("{\"status\":\"refused\",\"language\":\"en\"}", "refused/language pairing")]
    public void InvalidStatusLanguagePairingsAreRejected(string response, string label)
    {
        Assert.IsFalse(EligibilityEnvelopeParser.TryParse(response, null, out _, out _), label);
    }

    [TestMethod]
    public void UnknownFieldIsRejected()
    {
        Assert.IsFalse(EligibilityEnvelopeParser.TryParse(
            "{\"status\":\"eligible\",\"language\":\"en\",\"confidence\":0.9}",
            null,
            out _,
            out _));
    }

    [TestMethod]
    public void DuplicateStatusFieldIsRejected()
    {
        Assert.IsFalse(EligibilityEnvelopeParser.TryParse(
            "{\"status\":\"eligible\",\"status\":\"eligible\",\"language\":\"en\"}",
            null,
            out _,
            out _));
    }

    [TestMethod]
    public void DuplicateLanguageFieldIsRejected()
    {
        Assert.IsFalse(EligibilityEnvelopeParser.TryParse(
            "{\"status\":\"eligible\",\"language\":\"en\",\"language\":\"en\"}",
            null,
            out _,
            out _));
    }

    [TestMethod]
    [DataRow("{\"status\":\"likely\",\"language\":\"en\"}")]
    [DataRow("{\"status\":\"eligible\",\"language\":\"de\"}")]
    [DataRow("{\"status\":\"eligible\",\"language\":42}")]
    [DataRow("{\"status\":7,\"language\":\"en\"}")]
    [DataRow("{\"status\":\"eligible\"}")]
    [DataRow("{\"language\":\"en\"}")]
    [DataRow("{}")]
    public void UnknownEnumsWrongTypesAndMissingFieldsAreRejected(string response)
    {
        Assert.IsFalse(EligibilityEnvelopeParser.TryParse(response, null, out _, out _));
    }

    [TestMethod]
    public void MultipleJsonValuesAreRejected()
    {
        Assert.IsFalse(EligibilityEnvelopeParser.TryParse(
            "{\"status\":\"eligible\",\"language\":\"en\"} {\"status\":\"eligible\",\"language\":\"en\"}",
            null,
            out _,
            out _));
    }

    [TestMethod]
    public void ExcessiveNestingIsRejected()
    {
        Assert.IsFalse(EligibilityEnvelopeParser.TryParse(
            "{\"status\":{\"value\":\"eligible\"},\"language\":\"en\"}",
            null,
            out _,
            out _));
    }

    [TestMethod]
    [DataRow("```json\n{\"status\":\"eligible\",\"language\":\"en\"}\n```")]
    [DataRow("{\"status\":\"eligible\",\"language\":\"en\"} done")]
    [DataRow("Result: {\"status\":\"eligible\",\"language\":\"en\"}")]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow("eligible")]
    public void TrailingProseCodeFencesAndNonObjectsAreRejected(string response)
    {
        Assert.IsFalse(EligibilityEnvelopeParser.TryParse(response, null, out _, out _));
    }

    [TestMethod]
    public void RawResponseBytesAreBoundedBeforeDeserialization()
    {
        var oversized = "{\"status\":\"eligible\",\"language\":\"en\"}".PadRight(
            EligibilityEnvelopeParser.MaxRawBytes + 1, ' ');

        Assert.IsFalse(EligibilityEnvelopeParser.TryParse(oversized, null, out _, out var rejection));
        Assert.IsNotNull(rejection);
    }

    [TestMethod]
    public void SurroundingWhitespaceIsAccepted()
    {
        var accepted = EligibilityEnvelopeParser.TryParse(
            "  \n{\"status\":\"eligible\",\"language\":\"en\"}\n ",
            null,
            out var classification,
            out _);

        Assert.IsTrue(accepted);
        Assert.AreEqual(new EligibilityClassification(EligibilityStatus.Eligible, "en"), classification);
    }
}
