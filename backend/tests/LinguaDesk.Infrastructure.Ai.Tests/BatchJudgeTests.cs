using System.Text.Json;
using LinguaDesk.Ai.Evaluation;
using LinguaDesk.Infrastructure.Ai;

namespace LinguaDesk.Infrastructure.Ai.Tests;

[TestClass]
public sealed class BatchJudgeTests
{
    private static readonly JsonSerializerOptions CalibrationJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private const string ValidEnvelope =
        "{\"scores\":{\"meaningFidelity\":3,\"grammarNaturalness\":2," +
        "\"styleOrMode\":3,\"languageAndFormat\":3},\"criticalErrors\":[]," +
        "\"assessment\":\"Meaning is preserved; only a minor surface edit is suggested.\"}";

    [TestMethod]
    public void JudgeMessagesBlindCandidateIdentity()
    {
        var outputSha = new string('a', 64);
        var label = BatchJudge.BlindedLabel(Case().Id, outputSha);

        var messages = BatchJudge.BuildMessages(Case(), "Переведённый текст.", label);
        var wire = string.Join("\n", messages.Select(message => message.Content));
        using var evidence = JsonDocument.Parse(messages[1].Content);

        Assert.HasCount(2, messages.ToList());
        StringAssert.Contains(wire, label);
        Assert.AreEqual("Переведённый текст.", evidence.RootElement.GetProperty("candidateOutput").GetString());
        Assert.IsFalse(wire.Contains("DeepSeek", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(wire.Contains("deepseek-flash", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(wire.Contains("Qwen", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(wire.Contains(Case().Id, StringComparison.Ordinal));
    }

    [TestMethod]
    public void StrictEnvelopeParserAcceptsPinnedShape()
    {
        var accepted = BatchJudge.TryParseEnvelope(ValidEnvelope, out var envelope, out var error);

        Assert.IsTrue(accepted, error);
        Assert.AreEqual(3, envelope.Scores.MeaningFidelity);
        Assert.AreEqual(2, envelope.Scores.GrammarNaturalness);
        Assert.IsEmpty(envelope.CriticalErrors.ToList());
    }

    [DataRow("{\"scores\":{\"meaningFidelity\":4,\"grammarNaturalness\":3,\"styleOrMode\":3,\"languageAndFormat\":3},\"criticalErrors\":[],\"assessment\":\"bad\"}")]
    [DataRow("{\"scores\":{\"meaningFidelity\":3,\"grammarNaturalness\":3,\"styleOrMode\":3,\"languageAndFormat\":3},\"criticalErrors\":[\"other\"],\"assessment\":\"bad\"}")]
    [DataRow("{\"scores\":{\"meaningFidelity\":3,\"grammarNaturalness\":3,\"styleOrMode\":3,\"languageAndFormat\":3},\"criticalErrors\":[],\"assessment\":\"bad\",\"extra\":true}")]
    [DataRow("not-json")]
    [TestMethod]
    public void StrictEnvelopeParserRejectsAnythingOutsidePinnedShape(string payload)
    {
        Assert.IsFalse(BatchJudge.TryParseEnvelope(payload, out _, out var error));
        Assert.IsFalse(string.IsNullOrWhiteSpace(error));
    }

    [TestMethod]
    public async Task InvalidGradeGetsExactlyOneBoundedRetry()
    {
        var calls = 0;

        var grade = await BatchJudge.GradeAsync(
            Case(),
            "Succeeded",
            category: null,
            "Переведённый текст.",
            JudgeProfile(),
            "calibration.v1",
            new string('b', 64),
            live: true,
            (_, _) =>
            {
                calls++;
                return Task.FromResult(Attempt(calls == 1 ? "{}" : ValidEnvelope));
            });

        Assert.AreEqual(2, calls);
        Assert.AreEqual(2, grade.JudgeAttempts);
        Assert.AreEqual(2, grade.JudgeDispatches);
        Assert.AreEqual("resolved-model-grade", grade.DimensionStatus);
        Assert.IsTrue(grade.Usable);
        Assert.IsNotNull(grade.Scores);
    }

    [TestMethod]
    public async Task TwoInvalidGradesRemainUnresolved()
    {
        var calls = 0;

        var grade = await BatchJudge.GradeAsync(
            Case(),
            "Succeeded",
            category: null,
            "Переведённый текст.",
            JudgeProfile(),
            "calibration.v1",
            new string('b', 64),
            live: true,
            (_, _) =>
            {
                calls++;
                return Task.FromResult(Attempt("{}"));
            });

        Assert.AreEqual(BatchJudge.MaxAttempts, calls);
        Assert.AreEqual("unresolved-invalid-model-grade", grade.DimensionStatus);
        Assert.IsNull(grade.Scores);
        Assert.IsFalse(grade.Usable);
    }

    [TestMethod]
    public async Task TransportFailureIsRetainedWithoutRetry()
    {
        var calls = 0;

        var grade = await BatchJudge.GradeAsync(
            Case(),
            "Succeeded",
            category: null,
            "Переведённый текст.",
            JudgeProfile(),
            "calibration.v1",
            new string('b', 64),
            live: true,
            (_, _) =>
            {
                calls++;
                return Task.FromResult(new BatchJudgeAttemptResult(
                    Dispatched: false,
                    Observation: null,
                    FailureCategory: "budget-denied"));
            });

        Assert.AreEqual(1, calls);
        Assert.AreEqual(0, grade.JudgeDispatches);
        Assert.AreEqual("unresolved-budget-denied", grade.DimensionStatus);
    }

    [TestMethod]
    public async Task FailedCandidateOutputNeverCallsJudge()
    {
        var calls = 0;

        var grade = await BatchJudge.GradeAsync(
            Case(),
            "Failed",
            "provider-failure",
            outputText: null,
            JudgeProfile(),
            "calibration.v1",
            new string('b', 64),
            live: true,
            (_, _) =>
            {
                calls++;
                return Task.FromResult(Attempt(ValidEnvelope));
            });

        Assert.AreEqual(0, calls);
        Assert.AreEqual("not-applicable-no-successful-output", grade.DimensionStatus);
        Assert.Contains("provider-failure", grade.CriticalFlags.ToList());
    }

    [TestMethod]
    public void CalibrationRequiresAcceptedFortyCaseIndependentRecord()
    {
        var path = Path.Combine(Path.GetTempPath(), $"judge-calibration-{Guid.NewGuid():N}.json");
        try
        {
            var document = CalibrationDocument();
            File.WriteAllText(path, JsonSerializer.Serialize(document, CalibrationJsonOptions));

            var approval = JudgeCalibration.Load(path, JudgeProfile(), [Case()]);

            Assert.AreEqual("dev-calibration.v1", approval.CalibrationId);
            Assert.AreEqual(40, approval.CaseCount);
            Assert.AreEqual(10, approval.CasesPerLanguage["zh"]);
            Assert.AreEqual(1, approval.ReviewerCount);
            Assert.AreEqual(160, approval.Agreement.DimensionComparisons);
            Assert.AreEqual(160, approval.Agreement.ExactDimensionMatches);
            Assert.AreEqual(40, approval.Agreement.CriticalSetMatches);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void CalibrationRejectsJudgeAsReviewer()
    {
        var path = Path.Combine(Path.GetTempPath(), $"judge-calibration-{Guid.NewGuid():N}.json");
        try
        {
            var document = CalibrationDocument();
            var reviewers = new[]
            {
                new JudgeCalibrationReviewer(
                    JudgeProfile().CandidateId,
                    "Self-review by the judge model, which independence forbids."),
            };
            document = document with { Reviewers = reviewers };
            File.WriteAllText(path, JsonSerializer.Serialize(document, CalibrationJsonOptions));

            Assert.ThrowsExactly<JudgeCalibrationException>(
                () => JudgeCalibration.Load(path, JudgeProfile(), [Case()]));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void CalibrationRejectsReleaseCaseOverlap()
    {
        var path = Path.Combine(Path.GetTempPath(), $"judge-calibration-{Guid.NewGuid():N}.json");
        try
        {
            var document = CalibrationDocument();
            var cases = document.Cases.ToArray();
            cases[0] = cases[0] with { CaseId = Case().Id };
            document = document with { Cases = cases };
            File.WriteAllText(path, JsonSerializer.Serialize(document, CalibrationJsonOptions));

            Assert.ThrowsExactly<JudgeCalibrationException>(
                () => JudgeCalibration.Load(path, JudgeProfile(), [Case()]));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void ReviewSelectionIsDeterministicAndSatisfiesPinnedStrata()
    {
        var cases = LoadB1Cases();

        var first = BatchReviewSelector.Select(cases, "owner-seed-2026-09-14");
        var second = BatchReviewSelector.Select(cases, "owner-seed-2026-09-14");

        CollectionAssert.AreEqual(first.PreselectedCaseIds.ToArray(), second.PreselectedCaseIds.ToArray());
        Assert.HasCount(12, first.PreselectedCaseIds.ToList());
        Assert.AreEqual(6, first.TranslationCount);
        Assert.AreEqual(6, first.RewritingCount);
        Assert.IsTrue(first.IncludesSimplifiedChinese);
        Assert.IsTrue(first.IncludesTraditionalChinese);
        Assert.IsTrue(first.IncludesShortLengthBand);
        Assert.IsTrue(first.IncludesLongLengthBand);
        Assert.HasCount(64, first.SeedSha256);

        var selected = cases
            .Where(kase => first.PreselectedCaseIds.Contains(kase.Id, StringComparer.Ordinal))
            .ToArray();
        foreach (var tag in first.RequiredCoverageTags)
        {
            Assert.IsTrue(
                selected.Any(kase => kase.CoverageTags.Contains(tag, StringComparer.Ordinal)),
                $"The review sample did not cover required tag '{tag}'.");
        }
    }

    [TestMethod]
    public void ReviewSelectionCoversGuardrailBatchWithoutSingletonTags()
    {
        var cases = BatchCorpus.Load(File.ReadAllText(BatchPath("batch-security.json")));

        var selection = BatchReviewSelector.Select(cases, "owner-seed-2026-09-14");

        Assert.HasCount(12, selection.PreselectedCaseIds.ToList());
        Assert.AreEqual(6, selection.TranslationCount);
        Assert.AreEqual(6, selection.RewritingCount);
        var selected = cases
            .Where(kase => selection.PreselectedCaseIds.Contains(kase.Id, StringComparer.Ordinal))
            .ToArray();
        foreach (var tag in selection.RequiredCoverageTags)
        {
            Assert.IsTrue(
                selected.Any(kase => kase.CoverageTags.Contains(tag, StringComparer.Ordinal)),
                $"The review sample did not cover required tag '{tag}'.");
        }
    }

    [TestMethod]
    public async Task LiveBatchRejectsInvalidCalibrationBeforeAnyDispatch()
    {
        var calibrationPath = Path.Combine(Path.GetTempPath(), $"judge-calibration-{Guid.NewGuid():N}.json");
        var reportPath = Path.Combine(Path.GetTempPath(), $"batch-report-{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(calibrationPath, "{}");

            var exit = await EvaluateBatch.RunAsync(
            [
                "--live",
                "--corpus", BatchPath(),
                "--profile", "DeepSeek-V4.1-Flash",
                "--judge-profile", "Qwen-Qwen3.8-Flash",
                "--calibration", calibrationPath,
                "--review-seed", "fixed-before-results",
                "--serving-scope", "quiescent",
                "--max-dispatches", "96",
                "--max-spend-usd", "0.70",
                "--output", reportPath,
            ]);

            Assert.AreEqual(3, exit);
            using var report = JsonDocument.Parse(File.ReadAllText(reportPath));
            Assert.AreEqual("blocked", report.RootElement.GetProperty("status").GetString());
            Assert.AreEqual(0, report.RootElement.GetProperty("budget").GetProperty("dispatchesUsed").GetInt32());
            Assert.HasCount(
                12,
                report.RootElement
                    .GetProperty("reviewSelection")
                    .GetProperty("preselectedCaseIds")
                    .EnumerateArray()
                    .ToList());
        }
        finally
        {
            File.Delete(calibrationPath);
            File.Delete(reportPath);
        }
    }

    private static BatchJudgeAttemptResult Attempt(string response) => new(
        Dispatched: true,
        new AttemptObservation(
            "Qwen-Qwen3.8-Flash",
            "judgment",
            CredentialPresent: true,
            DispatchCount: 1,
            FinishCategory: "completed",
            response,
            new UsageEvidence(100, 20, 120, null, null),
            "qwen/qwen3.8-flash",
            ReturnedFingerprint: null,
            ReservedUsd: 0.0048m,
            ActualUsd: 0.0001m),
        FailureCategory: null);

    private static CandidateProfile JudgeProfile() =>
        CandidateRegistry.Select(CandidateRegistry.Default, "Qwen-Qwen3.8-Flash");

    private static BatchCase Case() => new(
        "b1-tr-en-ru",
        "translation",
        "The train leaves at 21:45.",
        "en",
        "ru",
        Language: null,
        Mode: null,
        "eligible",
        ["Departure time remains 21:45."],
        "Natural Russian preserving the departure time.",
        ["numbers", "fidelity-risk"],
        ChineseScript: null);

    private static IReadOnlyList<BatchCase> LoadB1Cases()
        => BatchCorpus.Load(File.ReadAllText(BatchPath()));

    private static string BatchPath(string fileName = "batch-b1.json")
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var path = Path.Combine(
                directory.FullName,
                "backend",
                "tools",
                "LinguaDesk.Ai.Evaluation",
                "Corpus",
                fileName);
            if (File.Exists(path))
            {
                return path;
            }

            directory = directory.Parent;
        }

        Assert.Fail("Could not locate the frozen B1 corpus from the test output directory.");
        return string.Empty;
    }

    private static JudgeCalibrationDocument CalibrationDocument()
    {
        var reviewer = new JudgeCalibrationReviewer("reviewer-1", "Qualified across the labeled development examples.");
        var scores = new BatchDimensionScores(3, 3, 3, 3);
        var languages = new[] { "en", "ru", "ro", "zh" };
        var cases = new List<JudgeCalibrationCase>();
        foreach (var language in languages)
        {
            for (var index = 0; index < 10; index++)
            {
                var digit = (index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
                cases.Add(new JudgeCalibrationCase(
                    $"cal-{language}-{index + 1}",
                    language,
                    new string(digit[0] is >= 'a' and <= 'f' ? digit[0] : 'c', 64),
                    new string('d', 63) + digit[^1],
                    reviewer.ReviewerId,
                    scores,
                    [],
                    scores,
                    []));
            }
        }

        return new JudgeCalibrationDocument(
            JudgeCalibration.SchemaRevision,
            "dev-calibration.v1",
            "accepted",
            JudgeProfile().CandidateId,
            BatchJudge.GradingPromptRevision,
            BatchJudge.GradingPromptSha256,
            new string('e', 64),
            new string('f', 64),
            "2026-09-14T00:00:00Z",
            [reviewer],
            cases,
            ["Development calibration remains separate from release scoring."]);
    }
}
