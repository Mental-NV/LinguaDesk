using LinguaDesk.Api.Benchmark;

namespace LinguaDesk.Api.Benchmark.Tests;

[TestClass]
public sealed class SourceSynthesisTests
{
    [TestMethod]
    public void SynthesisReachesExactCanonicalLengths()
    {
        Assert.AreEqual(64, SourceSynthesis.CountScalars(SourceSynthesis.Synthesize("The M037 rehearsal schedule. ", 64)));
        Assert.AreEqual(320, SourceSynthesis.CountScalars(SourceSynthesis.Synthesize("The M037 rehearsal report. ", 320)));
        Assert.AreEqual(800, SourceSynthesis.CountScalars(SourceSynthesis.Synthesize("M037排练用简体中文记录季度交付通知。", 800)));
        Assert.AreEqual(300, SourceSynthesis.CountScalars(SourceSynthesis.Synthesize("M037排練用繁體中文記錄季度交付通知。", 300)));
    }

    [TestMethod]
    public void ScalarCountingUsesRunesNotUtf16Units()
    {
        // U+1F600 needs two UTF-16 units but counts as one canonical scalar.
        Assert.AreEqual(2, SourceSynthesis.CountScalars("A\U0001F600"));
        Assert.AreEqual(1, SourceSynthesis.CountScalars(SourceSynthesis.TruncateToScalars("A\U0001F600B", 1)));
        Assert.AreEqual("A\U0001F600", SourceSynthesis.TruncateToScalars("A\U0001F600B", 2));
    }

    [TestMethod]
    public void SynthesisIsDeterministic()
    {
        var first = SourceSynthesis.Synthesize("The M037 rehearsal courier. ", 800);
        var second = SourceSynthesis.Synthesize("The M037 rehearsal courier. ", 800);

        Assert.AreEqual(first, second);
        Assert.AreEqual(SourceSynthesis.Sha256Hex(first), SourceSynthesis.Sha256Hex(second));
    }

    [TestMethod]
    public void HashesAreLowercaseSha256()
    {
        var hash = SourceSynthesis.Sha256Hex("M037 synthetic");

        Assert.AreEqual(64, hash.Length);
        Assert.IsTrue(hash.All(character => char.IsAsciiDigit(character) || (character >= 'a' && character <= 'f')));
    }
}
