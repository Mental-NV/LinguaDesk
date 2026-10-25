using System.Text.Json;
using LinguaDesk.Core;

namespace LinguaDesk.Core.Tests;

[TestClass]
public sealed class SentenceBoundaryPolicyTests
{
    [TestMethod]
    public void SharedScalarSpansPreserveGapsAndRepeatedOccurrences()
    {
        using var fixtures = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "sentences.v1.json")));
        Assert.AreEqual(SentenceBoundaryPolicy.Revision, fixtures.RootElement.GetProperty("revision").GetString());
        foreach (var row in fixtures.RootElement.GetProperty("cases").EnumerateArray())
        {
            var expected = row.GetProperty("spans").EnumerateArray().Select(s => new SentenceSpan(s.GetProperty("start").GetInt32(), s.GetProperty("length").GetInt32())).ToArray();
            CollectionAssert.AreEqual(expected, SentenceBoundaryPolicy.Segment(row.GetProperty("text").GetString()!).ToArray(), row.GetProperty("id").GetString());
        }
    }

    [TestMethod]
    public void ValidationDerivesSelectedCountAndRejectsPartialInvalidAndOversizeSpans()
    {
        var association = Guid.NewGuid();
        var valid = AlternativesInputPolicy.Validate("😀 Ready.\r\nNext.", 0, 8, association, "en");
        Assert.IsTrue(valid.IsValid);
        Assert.AreEqual("😀 Ready.", valid.SelectedText);
        Assert.AreEqual(8, valid.CharacterCount);
        Assert.IsFalse(AlternativesInputPolicy.Validate("😀 Ready.\r\nNext.", 1, 7, association, "en").IsValid);
        Assert.IsFalse(AlternativesInputPolicy.Validate("bad\ud800", 0, 4, association, "en").IsValid);
        Assert.IsFalse(AlternativesInputPolicy.Validate(new string('a', 8001), 0, 1, association, "en").IsValid);
        Assert.IsFalse(AlternativesInputPolicy.Validate(new string('a', 2001), 0, 2001, association, "en").IsValid);
        Assert.IsFalse(AlternativesInputPolicy.Validate("Ready.", 0, 6, Guid.Empty, "en").IsValid);
        Assert.IsFalse(AlternativesInputPolicy.Validate("Ready.", 0, 6, association, "auto").IsValid);
        Assert.IsFalse(AlternativesInputPolicy.Validate("A\0B", 0, 3, association, "en").IsValid);
        Assert.IsTrue(AlternativesInputPolicy.Validate(new string('a', 2000), 0, 2000, association, "en").IsValid);
    }
}
