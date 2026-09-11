using LinguaDesk.Infrastructure.Ai;

namespace LinguaDesk.Infrastructure.Ai.Tests;

[TestClass]
public sealed class TranslationEnvelopeParserTests
{
    [TestMethod]
    public void ResultAcceptsCompleteText()
    {
        var accepted = TranslationEnvelopeParser.TryParse(
            "{\"status\":\"result\",\"text\":\"Please send the report tomorrow.\"}",
            out var result,
            out var rejection);

        Assert.IsTrue(accepted);
        Assert.IsNull(rejection);
        Assert.AreEqual(
            new TranslationResult(TranslationStatus.Result, "Please send the report tomorrow."),
            result);
    }

    [TestMethod]
    public void RefusedAcceptsNullText()
    {
        var accepted = TranslationEnvelopeParser.TryParse(
            "{\"status\":\"refused\",\"text\":null}",
            out var result,
            out var rejection);

        Assert.IsTrue(accepted);
        Assert.IsNull(rejection);
        Assert.AreEqual(new TranslationResult(TranslationStatus.Refused, null), result);
    }

    [TestMethod]
    public void RefusalWordingInsideResultTextIsNeverAParserRefusal()
    {
        var accepted = TranslationEnvelopeParser.TryParse(
            "{\"status\":\"result\",\"text\":\"I cannot attend the meeting tomorrow.\"}",
            out var result,
            out _);

        Assert.IsTrue(accepted);
        Assert.IsNotNull(result);
        Assert.AreEqual(TranslationStatus.Result, result.Status);
        Assert.AreEqual("I cannot attend the meeting tomorrow.", result.Text);
    }

    [TestMethod]
    [DataRow("{\"status\":\"result\",\"text\":null}", "result/null pairing")]
    [DataRow("{\"status\":\"result\",\"text\":\"\"}", "result/empty pairing")]
    [DataRow("{\"status\":\"result\",\"text\":\"   \"}", "result/whitespace pairing")]
    [DataRow("{\"status\":\"refused\",\"text\":\"Please send the report tomorrow.\"}", "refused/text pairing")]
    [DataRow("{\"status\":\"refused\",\"text\":\"\"}", "refused/empty pairing")]
    public void InvalidStatusTextPairingsAreRejected(string response, string label)
    {
        Assert.IsFalse(TranslationEnvelopeParser.TryParse(response, out _, out _), label);
    }

    [TestMethod]
    public void UnknownFieldIsRejected()
    {
        Assert.IsFalse(TranslationEnvelopeParser.TryParse(
            "{\"status\":\"result\",\"text\":\"Hello.\",\"confidence\":0.9}",
            out _,
            out _));
    }

    [TestMethod]
    public void DuplicateStatusFieldIsRejected()
    {
        Assert.IsFalse(TranslationEnvelopeParser.TryParse(
            "{\"status\":\"result\",\"status\":\"result\",\"text\":\"Hello.\"}",
            out _,
            out _));
    }

    [TestMethod]
    public void DuplicateTextFieldIsRejected()
    {
        Assert.IsFalse(TranslationEnvelopeParser.TryParse(
            "{\"status\":\"result\",\"text\":\"Hello.\",\"text\":\"Hello.\"}",
            out _,
            out _));
    }

    [TestMethod]
    [DataRow("{\"status\":\"likely\",\"text\":\"Hello.\"}")]
    [DataRow("{\"status\":\"eligible\",\"text\":\"Hello.\"}")]
    [DataRow("{\"status\":\"result\",\"text\":42}")]
    [DataRow("{\"status\":7,\"text\":\"Hello.\"}")]
    [DataRow("{\"status\":\"result\"}")]
    [DataRow("{\"text\":\"Hello.\"}")]
    [DataRow("{}")]
    public void UnknownEnumsWrongTypesAndMissingFieldsAreRejected(string response)
    {
        Assert.IsFalse(TranslationEnvelopeParser.TryParse(response, out _, out _));
    }

    [TestMethod]
    public void MultipleJsonValuesAreRejected()
    {
        Assert.IsFalse(TranslationEnvelopeParser.TryParse(
            "{\"status\":\"result\",\"text\":\"Hello.\"} {\"status\":\"result\",\"text\":\"Hello.\"}",
            out _,
            out _));
    }

    [TestMethod]
    public void ExcessiveNestingIsRejected()
    {
        Assert.IsFalse(TranslationEnvelopeParser.TryParse(
            "{\"status\":{\"value\":\"result\"},\"text\":\"Hello.\"}",
            out _,
            out _));
    }

    [TestMethod]
    public void NonStringTextShapeIsRejected()
    {
        Assert.IsFalse(TranslationEnvelopeParser.TryParse(
            "{\"status\":\"result\",\"text\":{\"value\":\"Hello.\"}}",
            out _,
            out _));
    }

    [TestMethod]
    [DataRow("```json\n{\"status\":\"result\",\"text\":\"Hello.\"}\n```")]
    [DataRow("{\"status\":\"result\",\"text\":\"Hello.\"} done")]
    [DataRow("Result: {\"status\":\"result\",\"text\":\"Hello.\"}")]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow("result")]
    public void TrailingProseCodeFencesAndNonObjectsAreRejected(string response)
    {
        Assert.IsFalse(TranslationEnvelopeParser.TryParse(response, out _, out _));
    }

    [TestMethod]
    public void RawResponseBytesAreBoundedBeforeDeserialization()
    {
        var oversized = "{\"status\":\"result\",\"text\":\"Hello.\"}".PadRight(
            TranslationEnvelopeParser.MaxRawBytes + 1, ' ');

        Assert.IsFalse(TranslationEnvelopeParser.TryParse(oversized, out _, out var rejection));
        Assert.IsNotNull(rejection);
    }

    [TestMethod]
    public void SurroundingWhitespaceIsAccepted()
    {
        var accepted = TranslationEnvelopeParser.TryParse(
            "  \n{\"status\":\"result\",\"text\":\"Hello.\"}\n ",
            out var result,
            out _);

        Assert.IsTrue(accepted);
        Assert.AreEqual(new TranslationResult(TranslationStatus.Result, "Hello."), result);
    }
}
