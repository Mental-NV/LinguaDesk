namespace LinguaDesk.Infrastructure.Ai.Tests;

// Offline validation for the M036 batch-input runner contract: the frozen
// B1 content hash gate (AC-001), the batch loader shape, and the
// deterministic digit-preservation finding. No provider dispatch, no
// credential read, no network.
[TestClass]
public sealed class BatchCorpusTests
{
    private static readonly string[] ExpectedDropped = ["21:45"];

    private static string BatchPath(string fileName = "batch-b1.json")
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
        return Path.Combine(
            directory.FullName, "backend", "tools", "LinguaDesk.Ai.Evaluation", "Corpus", fileName);
    }

    [TestMethod]
    public void FrozenHashPinMatchesBatchFile()
    {
        var raw = File.ReadAllBytes(BatchPath());

        Assert.AreEqual(BatchCorpus.FrozenContentSha256, BatchCorpus.ComputeContentSha256(raw));
        Assert.IsTrue(BatchCorpus.IsFrozenRevision(raw));
    }

    [TestMethod]
    public void TamperedBytesAreNotFrozenRevision()
    {
        var raw = File.ReadAllBytes(BatchPath());
        raw[raw.Length - 1] = (byte)(raw[raw.Length - 1] ^ 0xFF);

        Assert.IsFalse(BatchCorpus.IsFrozenRevision(raw));
    }

    [TestMethod]
    public void LoaderParsesTwentyFourCasesWithExpectedFields()
    {
        var json = File.ReadAllText(BatchPath());
        var cases = BatchCorpus.Load(json);

        Assert.HasCount(24, cases.ToList());
        foreach (var kase in cases)
        {
            Assert.IsTrue(kase.Id.StartsWith("b1-", StringComparison.Ordinal));
            Assert.IsFalse(string.IsNullOrWhiteSpace(kase.SourceText));
            Assert.AreEqual("eligible", kase.ExpectedEligibility);
            Assert.IsNotEmpty(kase.MeaningAssertions.ToList());
        }

        Assert.HasCount(12, cases.Where(kase => kase.Family == "translation").ToList());
        Assert.HasCount(12, cases.Where(kase => kase.Family == "rewriting").ToList());
    }

    [TestMethod]
    public void FrozenSecurityHashPinMatchesBatchFile()
    {
        var raw = File.ReadAllBytes(BatchPath("batch-security.json"));

        Assert.AreEqual(BatchCorpus.FrozenSecurityContentSha256, BatchCorpus.ComputeContentSha256(raw));
        Assert.IsTrue(BatchCorpus.IsFrozenRevision(raw, BatchCorpus.ExpectedSecurityBatchId));
    }

    [TestMethod]
    public void LoaderParsesSecurityBatchWithUniqueIds()
    {
        var json = File.ReadAllText(BatchPath("batch-security.json"));
        var cases = BatchCorpus.Load(json).ToList();

        Assert.HasCount(24, cases);
        Assert.HasCount(12, cases.Where(kase => kase.Family == "translation").ToList());
        Assert.HasCount(12, cases.Where(kase => kase.Family == "rewriting").ToList());
        foreach (var kase in cases)
        {
            Assert.IsTrue(kase.Id.StartsWith("bsec-", StringComparison.Ordinal));
            Assert.IsFalse(string.IsNullOrWhiteSpace(kase.SourceText));
            Assert.AreEqual("eligible", kase.ExpectedEligibility);
        }
    }

    [TestMethod]
    public void LoaderRejectsWrongSchemaRevision()
    {
        var json = File.ReadAllText(BatchPath()).Replace("corpus-b1.v1", "corpus-b1.v9");

        Assert.Throws<BatchCorpusException>(() => BatchCorpus.Load(json));
    }

    [TestMethod]
    public void DigitTokensDetectChangedNumericalValues()
    {
        var source = BatchCorpus.DigitTokens("The night train departs at 21:45; two seats reserved.");
        var preserved = BatchCorpus.DigitTokens("Le train part à 21:45 ; deux places.");
        var dropped = BatchCorpus.DigitTokens("Le train part à 15:30 ; deux places.");

        Assert.Contains("21:45", source.ToList());
        Assert.IsEmpty(source.Where(token => !preserved.Contains(token)).ToList());
        CollectionAssert.AreEqual(ExpectedDropped, source.Where(token => !dropped.Contains(token)).ToArray());
    }
}
