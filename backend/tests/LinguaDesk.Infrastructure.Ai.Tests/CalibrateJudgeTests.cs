using LinguaDesk.Ai.Evaluation;

namespace LinguaDesk.Infrastructure.Ai.Tests;

[TestClass]
public sealed class CalibrateJudgeTests
{
    [TestMethod]
    public async Task OfflineRunWritesBlindReviewPack()
    {
        var directory = Directory.CreateDirectory(
            Path.Combine(Path.GetTempPath(), $"calibrate-judge-{Guid.NewGuid():N}"));
        try
        {
            var corpus = Path.Combine(directory.FullName, "dev-corpus.json");
            await File.WriteAllTextAsync(corpus, """
                {"schemaRevision":"dev-corpus.v1","corpusId":"TEST","cases":[
                {"id":"t-en-01","family":"translation","language":"en","source":"Hello.","operation":{"sourceSelection":"en","target":"ru"}},
                {"id":"t-ru-01","family":"rewriting","language":"ru","source":"Привет.","operation":{"language":"ru","mode":"simple"}}]}
                """);
            var outputDir = Path.Combine(directory.FullName, "out");

            var exit = await CalibrateJudge.RunAsync(
            [
                "--offline",
                "--dev-corpus", corpus,
                "--max-dispatches", "50",
                "--max-spend-usd", "1.00",
                "--output-dir", outputDir,
            ]);

            Assert.AreEqual(0, exit);
            var draft = File.ReadAllText(Path.Combine(outputDir, "calibration-draft.json"));
            Assert.Contains("\"Kind\": \"calibration_draft_report\"", draft);
            Assert.Contains("\"OverallDeadlineMs\": 30000", draft);
            Assert.Contains("\"EligibilityTimeoutMs\": 5000", draft);
            Assert.Contains("\"TransformationTimeoutMs\": 10000", draft);
            Assert.Contains("\"FinalizationReserveMs\": 2000", draft);
            Assert.Contains("Привет", draft);
            Assert.DoesNotContain("\\u041f", draft);
            var review = File.ReadAllText(Path.Combine(outputDir, "review-pack.json"));
            Assert.DoesNotContain("Scores", review);
            Assert.Contains("Привет", review);
            Assert.DoesNotContain("\\u041f", review);
            Assert.IsTrue(File.Exists(Path.Combine(outputDir, "human-labels.template.json")));
        }
        finally
        {
            Directory.Delete(directory.FullName, recursive: true);
        }
    }

    [TestMethod]
    public async Task OversizedStageTimeoutIsUsageError()
    {
        var directory = Directory.CreateDirectory(
            Path.Combine(Path.GetTempPath(), $"calibrate-judge-{Guid.NewGuid():N}"));
        try
        {
            var corpus = Path.Combine(directory.FullName, "dev-corpus.json");
            await File.WriteAllTextAsync(corpus, """
                {"schemaRevision":"dev-corpus.v1","corpusId":"TEST","cases":[
                {"id":"t-en-01","family":"translation","language":"en","source":"Hello.","operation":{"sourceSelection":"en","target":"ru"}}]}
                """);

            var exit = await CalibrateJudge.RunAsync(
            [
                "--offline",
                "--dev-corpus", corpus,
                "--max-dispatches", "50",
                "--max-spend-usd", "1.00",
                "--deadline-ms", "30000",
                "--eligibility-timeout-ms", "29000",
                "--output-dir", Path.Combine(directory.FullName, "out"),
            ]);

            Assert.AreEqual(2, exit);
        }
        finally
        {
            Directory.Delete(directory.FullName, recursive: true);
        }
    }

    [TestMethod]
    public async Task MalformedDevCorpusIsUsageError()
    {
        var directory = Directory.CreateDirectory(
            Path.Combine(Path.GetTempPath(), $"calibrate-judge-{Guid.NewGuid():N}"));
        try
        {
            var corpus = Path.Combine(directory.FullName, "dev-corpus.json");
            await File.WriteAllTextAsync(corpus, """{"schemaRevision":"dev-corpus.v1","cases":[]}""");

            var exit = await CalibrateJudge.RunAsync(
            [
                "--offline",
                "--dev-corpus", corpus,
                "--max-dispatches", "50",
                "--max-spend-usd", "1.00",
                "--output-dir", Path.Combine(directory.FullName, "out"),
            ]);

            Assert.AreEqual(2, exit);
        }
        finally
        {
            Directory.Delete(directory.FullName, recursive: true);
        }
    }
}
