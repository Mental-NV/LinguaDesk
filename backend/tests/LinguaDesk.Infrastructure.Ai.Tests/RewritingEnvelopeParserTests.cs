using LinguaDesk.Infrastructure.Ai;

namespace LinguaDesk.Infrastructure.Ai.Tests;

[TestClass]
public sealed class RewritingEnvelopeParserTests
{
    [TestMethod]
    public void ResultAcceptsCompleteText()
    {
        var accepted = RewritingEnvelopeParser.TryParse(
            "{\"status\":\"result\",\"text\":\"Please send the report tomorrow.\"}",
            out var result,
            out var rejection);

        Assert.IsTrue(accepted);
        Assert.IsNull(rejection);
        Assert.AreEqual(
            new RewritingResult(RewritingStatus.Result, "Please send the report tomorrow."),
            result);
    }

    [TestMethod]
    public void RefusedAcceptsNullText()
    {
        var accepted = RewritingEnvelopeParser.TryParse(
            "{\"status\":\"refused\",\"text\":null}",
            out var result,
            out var rejection);

        Assert.IsTrue(accepted);
        Assert.IsNull(rejection);
        Assert.AreEqual(new RewritingResult(RewritingStatus.Refused, null), result);
    }

    [TestMethod]
    public void RefusalWordingInsideResultTextIsNeverAParserRefusal()
    {
        var accepted = RewritingEnvelopeParser.TryParse(
            "{\"status\":\"result\",\"text\":\"I cannot attend the meeting tomorrow.\"}",
            out var result,
            out _);

        Assert.IsTrue(accepted);
        Assert.IsNotNull(result);
        Assert.AreEqual(RewritingStatus.Result, result.Status);
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
        Assert.IsFalse(RewritingEnvelopeParser.TryParse(response, out _, out _), label);
    }

    [TestMethod]
    public void UnknownFieldIsRejected()
    {
        Assert.IsFalse(RewritingEnvelopeParser.TryParse(
            "{\"status\":\"result\",\"text\":\"Hello.\",\"confidence\":0.9}",
            out _,
            out _));
    }

    [TestMethod]
    public void DuplicateStatusFieldIsRejected()
    {
        Assert.IsFalse(RewritingEnvelopeParser.TryParse(
            "{\"status\":\"result\",\"status\":\"result\",\"text\":\"Hello.\"}",
            out _,
            out _));
    }

    [TestMethod]
    public void DuplicateTextFieldIsRejected()
    {
        Assert.IsFalse(RewritingEnvelopeParser.TryParse(
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
        Assert.IsFalse(RewritingEnvelopeParser.TryParse(response, out _, out _));
    }

    [TestMethod]
    public void MultipleJsonValuesAreRejected()
    {
        Assert.IsFalse(RewritingEnvelopeParser.TryParse(
            "{\"status\":\"result\",\"text\":\"Hello.\"} {\"status\":\"result\",\"text\":\"Hello.\"}",
            out _,
            out _));
    }

    [TestMethod]
    public void ExcessiveNestingIsRejected()
    {
        Assert.IsFalse(RewritingEnvelopeParser.TryParse(
            "{\"status\":{\"value\":\"result\"},\"text\":\"Hello.\"}",
            out _,
            out _));
    }

    [TestMethod]
    public void NonStringTextShapeIsRejected()
    {
        Assert.IsFalse(RewritingEnvelopeParser.TryParse(
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
        Assert.IsFalse(RewritingEnvelopeParser.TryParse(response, out _, out _));
    }

    [TestMethod]
    public void RawResponseBytesAreBoundedBeforeDeserialization()
    {
        var oversized = "{\"status\":\"result\",\"text\":\"Hello.\"}".PadRight(
            RewritingEnvelopeParser.MaxRawBytes + 1, ' ');

        Assert.IsFalse(RewritingEnvelopeParser.TryParse(oversized, out _, out var rejection));
        Assert.IsNotNull(rejection);
    }

    [TestMethod]
    public void SurroundingWhitespaceIsAccepted()
    {
        var accepted = RewritingEnvelopeParser.TryParse(
            "  \n{\"status\":\"result\",\"text\":\"Hello.\"}\n ",
            out var result,
            out _);

        Assert.IsTrue(accepted);
        Assert.AreEqual(new RewritingResult(RewritingStatus.Result, "Hello."), result);
    }
}
