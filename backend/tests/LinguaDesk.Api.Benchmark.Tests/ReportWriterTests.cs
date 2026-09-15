using LinguaDesk.Api.Benchmark;

namespace LinguaDesk.Api.Benchmark.Tests;

[TestClass]
public sealed class ReportWriterTests
{
    private static RequestObservation Observation(
        string combo, string family, bool isRepeat, bool success, double totalMs) =>
        new(
            0, combo, family, family == "translation" ? "en->zh" : "en/correctionOnly", "band-1-100",
            null, "hash-" + combo, 64, isRepeat, !isRepeat,
            Guid.CreateVersion7().ToString("D"), SubmittedFresh: true,
            DateTimeOffset.UtcNow, totalMs, success ? 201 : 503,
            success ? "success" : "failure", success ? null : "processingFailure",
            success ? 64 : null, success ? "2026-09-11" : null,
            success ? """{"day":"2026-09-11"}""" : null,
            success ? 10 : null, success ? "outputhash" : null,
            "unknown",
            new StageDurations(null, null, null, null),
            BenchmarkHttpDriver.StageProvenanceText);

    private static (BenchmarkManifest Manifest, FullRulesDocument Rules) ShippedInputs() =>
    (
        ManifestLoader.Load(Path.Combine(AppContext.BaseDirectory, "Fixtures", "m037-rehearsal-24.json")),
        ReportWriter.LoadRules(Path.Combine(AppContext.BaseDirectory, "Fixtures", "full-360-rules.json"))
    );

    [TestMethod]
    public void CompleteReportCarriesEverySection61Field()
    {
        var (manifest, rules) = ShippedInputs();
        var observations = new[]
        {
            Observation("t-enzh-b1", "translation", isRepeat: false, success: true, totalMs: 40),
            Observation("t-enzh-b1", "translation", isRepeat: true, success: true, totalMs: 45),
            Observation("r-enco-b1", "rewriting", isRepeat: false, success: false, totalMs: 50),
            Observation("r-enco-b1", "rewriting", isRepeat: true, success: true, totalMs: 55),
        };
        var run = new DriverRun(observations, 2, 5, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

        var report = ReportWriter.Build(
            manifest, rules, observations, run, new AuditResult(true, []),
            "rev-test", "sdk-test", "manifest-sha", "rules-sha",
            readinessProbes: 2, "http://127.0.0.1:9", "runtime-test", DateTimeOffset.UtcNow);

        Assert.AreEqual("complete", report.Status);
        Assert.HasCount(4, report.Observations);
        Assert.AreEqual(1.0, report.Translation.SuccessWithinTargetRate);
        Assert.AreEqual(0.5, report.Rewriting.SuccessWithinTargetRate);
        Assert.AreEqual(40.0, report.Translation.P50Ms);
        Assert.AreEqual(2, report.Cohorts.FirstSeen.Total);
        Assert.AreEqual(2, report.Cohorts.Repeated.Total);
        Assert.AreEqual(4, report.Cohorts.CacheUnknown);
        Assert.AreEqual(0, report.Exposure.PaidDispatches);
        Assert.HasCount(12, report.RuleMapping);
        Assert.IsEmpty(report.AuditViolations);
    }

    [TestMethod]
    public void FailedAuditMarksTheReportFailed()
    {
        var (manifest, rules) = ShippedInputs();
        var observations = new[] { Observation("t-enzh-b1", "translation", false, true, 40) };
        var run = new DriverRun(observations, 1, 0, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

        var report = ReportWriter.Build(
            manifest, rules, observations, run, new AuditResult(false, ["dropped requests"]),
            "rev-test", "sdk-test", "manifest-sha", "rules-sha",
            2, "http://127.0.0.1:9", "runtime-test", DateTimeOffset.UtcNow);

        Assert.AreEqual("failed", report.Status);
        Assert.HasCount(1, report.AuditViolations);
    }

    [TestMethod]
    public void ReportNeverCarriesSourceResultTextOrSecrets()
    {
        var (manifest, rules) = ShippedInputs();
        var items = new[] { Observation("t-enzh-b1", "translation", false, true, 40) };
        var run = new DriverRun(items, 1, 0, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        var report = ReportWriter.Build(
            manifest, rules, items, run, new AuditResult(true, []),
            "rev-test", "sdk-test", "manifest-sha", "rules-sha",
            2, "http://127.0.0.1:9", "runtime-test", DateTimeOffset.UtcNow);

        var json = ReportWriter.Serialize(report);

        Assert.IsFalse(json.Contains("secret probe", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains("synthetic-token", StringComparison.Ordinal));
        Assert.IsTrue(json.Contains("credentialPresent", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(json.Contains("synchronous-api-total-only", StringComparison.Ordinal));
    }

    [TestMethod]
    public void UnmappedRuleFailsReportConstruction()
    {
        var (manifest, rules) = ShippedInputs();
        var trimmed = manifest with
        {
            SuccessorMapping = manifest.SuccessorMapping.Where(entry => entry.RuleId != "R12").ToList(),
        };
        var observations = new[] { Observation("t-enzh-b1", "translation", false, true, 40) };
        var run = new DriverRun(observations, 1, 0, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

        Assert.ThrowsExactly<InvalidOperationException>(() => ReportWriter.Build(
            trimmed, rules, observations, run, new AuditResult(true, []),
            "rev-test", "sdk-test", "manifest-sha", "rules-sha",
            2, "http://127.0.0.1:9", "runtime-test", DateTimeOffset.UtcNow));
    }
}
