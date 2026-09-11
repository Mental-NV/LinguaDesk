using System.Text.Json;
using System.Text.Json.Nodes;
using LinguaDesk.Ai.Evaluation;

namespace LinguaDesk.Infrastructure.Ai.Tests;

[TestClass]
public sealed class EvaluationReportTests
{
    private static readonly JsonSerializerOptions Canonical = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    private static readonly string[] BlockedSample = ["live-access", "eligibility-live"];

    [TestMethod]
    public void CombinedReportCarriesEveryRequiredSection72Field()
    {
        var document = EvaluationReportBuilder.Build(
            Manifest(),
            Probe(),
            [FixtureSection(), LiveSection()],
            "success",
            null);

        Assert.AreEqual("evaluation_report", document.Kind);
        Assert.AreEqual(1, document.FormatVersion);
        Assert.AreEqual("DeepSeek-V4.1-Flash", document.CandidateId);
        Assert.AreEqual("openai-chat-completions-v1", document.AdapterId);
        Assert.AreEqual("https://api.deepseek.com", document.Endpoint);
        Assert.AreEqual("deepseek-flash", document.Model);
        Assert.AreEqual("deepseek", document.CredentialRef);
        Assert.IsTrue(document.CredentialPresent);
        Assert.IsTrue(document.Live);
        Assert.AreEqual("eligibility.v1", document.Revisions.EligibilityPrompt);
        Assert.IsFalse(string.IsNullOrWhiteSpace(document.Revisions.EligibilityPromptResourceSha256));
        Assert.IsFalse(string.IsNullOrWhiteSpace(document.Revisions.TranslationPromptResourceSha256));
        Assert.IsFalse(string.IsNullOrWhiteSpace(document.Revisions.RewritingPromptResourceSha256));
        Assert.IsFalse(string.IsNullOrWhiteSpace(document.Revisions.Validator));
        Assert.IsFalse(string.IsNullOrWhiteSpace(document.Revisions.Sdk));
        Assert.AreEqual("USD", document.Billing.Currency);
        Assert.AreEqual(1, document.Selection.Concurrency);
        Assert.IsNotEmpty(document.Selection.Sections);
        Assert.IsFalse(string.IsNullOrWhiteSpace(document.Timestamps.StartedUtc));
        Assert.IsFalse(string.IsNullOrWhiteSpace(document.Timestamps.FinishedUtc));
        Assert.IsNotNull(document.AccessProbe);
        Assert.HasCount(2, document.Sections);
        Assert.IsNotEmpty(document.Limitations);
        Assert.AreEqual("success", document.Status);
    }

    [TestMethod]
    public void AggregatesMatchSectionRowsExactly()
    {
        var document = EvaluationReportBuilder.Build(
            Manifest(), probe: null, [FixtureSection(), LiveSection()], "fail", "detail");

        var rows = document.Sections.SelectMany(section => section.Cases).ToList();
        var bySection = document.Aggregates.BySection;
        Assert.HasCount(2, bySection);
        foreach (var aggregate in bySection)
        {
            var section = document.Sections.Single(s => s.Name == aggregate.Key);
            Assert.AreEqual(section.Cases.Count, aggregate.Total);
        }

        var total = bySection.Sum(a => a.Total);
        Assert.AreEqual(rows.Count, total);
        Assert.AreEqual(rows.Count, document.Aggregates.ByFamily.Sum(a => a.Total));
        Assert.AreEqual(5, document.Findings.FaultInjected + document.Findings.LiveDevelopment + document.Findings.OfflineFixture);
        Assert.AreEqual(0, document.Findings.LiveAccess); // probe is null here
    }

    [TestMethod]
    public void FaultInjectedAndSkippedRowsNeverCountAsFailures()
    {
        var document = EvaluationReportBuilder.Build(
            Manifest(), probe: null, [FixtureSection(), LiveSection()], "fail", "detail");

        // The live section carries one Failed fault_injected row and one Skipped row.
        Assert.AreEqual(0, document.Findings.Failed);
        var live = document.Aggregates.BySection.Single(a => a.Key == "translation-live");
        Assert.AreEqual(2, live.FaultInjected);
        Assert.AreEqual(1, live.Skipped);
        Assert.AreEqual(0, live.Failed);
        Assert.AreEqual(3, live.Matched);
    }

    [TestMethod]
    public void BlockedSectionsCarryZeroRowsAndListEveryBlock()
    {
        var manifest = Manifest() with
        {
            BlockedSections = ["live-access", "eligibility-live"],
        };
        var blocked = new ReportSectionInput(
            "eligibility-live", "eligibility_evaluation_report", "Eligibility",
            Live: true, CredentialPresent: false, "blocked",
            "The evaluation credential is missing or blank; no provider dispatch was made.",
            0, 10, 0.50m, 0, 0m, 0m, [], []);
        var document = EvaluationReportBuilder.Build(manifest, probe: null, [blocked], "blocked", "detail");

        Assert.AreEqual("blocked", document.Status);
        Assert.IsEmpty(document.Sections.Single().Cases);
        CollectionAssert.AreEqual(BlockedSample, document.Findings.BlockedSections.ToList());
        Assert.AreEqual(0, document.Budget.DispatchesUsed);
    }

    [TestMethod]
    public void SerializedReportNamesOnlyCredentialReferences()
    {
        var document = EvaluationReportBuilder.Build(
            Manifest(), Probe(), [FixtureSection(), LiveSection()], "success", null);
        var payload = JsonSerializer.Serialize(document, Canonical);

        Assert.IsTrue(payload.Contains("credentialRef", StringComparison.Ordinal));
        Assert.IsTrue(payload.Contains("credentialPresent", StringComparison.Ordinal));
        Assert.IsFalse(payload.Contains("apiKey", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(payload.Contains("authorization", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(payload.Contains("BEGIN", StringComparison.Ordinal));
    }

    [TestMethod]
    public void ExposureTotalsEqualRowSums()
    {
        var document = EvaluationReportBuilder.Build(
            Manifest() with { ReservedUsd = 0.010m, UnresolvedUsd = 0.004m, ActualUsd = 0.006m },
            Probe(),
            [FixtureSection(), LiveSection()],
            "success",
            null);

        Assert.AreEqual(0.010m, document.Exposure.ReservedUsd);
        Assert.AreEqual(0.006m, document.Exposure.ActualUsd);
        Assert.AreEqual(0.004m, document.Exposure.UnresolvedUsd);
        var translation = document.Exposure.PerFamily.Single(f => f.Family == "Translation");
        Assert.AreEqual(0.003m, translation.ReservedUsd);
        Assert.AreEqual(0.002m, translation.ActualUsd);
    }

    private static ReportManifestInput Manifest() => new(
        "DeepSeek-V4.1-Flash",
        "openai-chat-completions-v1",
        "https://api.deepseek.com",
        "deepseek-flash",
        "deepseek",
        true,
        true,
        ".NET 10.0.10",
        "1.0.0",
        "1.0.0",
        "eligibility.v1",
        "e2e2e2",
        "translation.v1",
        "t3t3t3",
        "rewriting.v1",
        "r4r4r4",
        "validator.v3",
        0,
        "disabled",
        true,
        256,
        4096,
        30,
        "USD",
        "provider-page",
        "2026-09-11",
        0.27m,
        1.10m,
        10,
        0.50m,
        0,
        0.010m,
        0.004m,
        0.006m,
        1,
        "2026-09-11T00:00:00Z",
        "2026-09-11T00:01:00Z",
        "standalone",
        ["eligibility", "translation-live"],
        []);

    private static ReportAccessProbe Probe() => new(
        "live_access",
        "DeepSeek-V4.1-Flash",
        "deepseek",
        true,
        true,
        1,
        "success",
        null,
        64,
        8,
        true,
        0.001m,
        0.001m,
        0m,
        "deepseek-flash",
        null,
        1200);

    private static ReportSectionInput FixtureSection() => new(
        "eligibility",
        "eligibility_evaluation_report",
        "Eligibility",
        false,
        false,
        "pass",
        null,
        12,
        10,
        0.50m,
        0,
        0m,
        0m,
        ["elig-en"],
        [Row("eligibility", "elig-en", "translation", "Eligibility", "Eligible", true, false, "offline_fixture", 0, 0m, null)]);

    private static ReportSectionInput LiveSection() => new(
        "translation-live",
        "translation_evaluation_report",
        "Translation",
        true,
        true,
        "success",
        null,
        34,
        10,
        0.50m,
        3,
        0.003m,
        0m,
        ["tr-en-ru", "refusal-scripted", "malformed-scripted"],
        [
            Row("translation-live", "tr-en-ru", "en->ru", "Translation", "Succeeded", true, false, "live_development", 2, 0.001m, 0.001m),
            Row("translation-live", "tr-en-ru-2", "en->ru", "Translation", "Succeeded", true, false, "live_development", 2, 0.001m, 0.001m),
            Row("translation-live", "refusal-scripted", "en->ru", "Translation", "Skipped", true, true, "fault_injected", 0, 0m, null),
            Row("translation-live", "malformed-scripted", "en->ru", "Translation", "Failed", true, false, "fault_injected", 0, 0.001m, null),
        ]);

    private static ReportRowInput Row(
        string section,
        string caseId,
        string route,
        string family,
        string decision,
        bool matches,
        bool skipped,
        string disposition,
        int dispatches,
        decimal reserved,
        decimal? actual) => new(
        section,
        caseId,
        route,
        family,
        decision,
        matches,
        skipped,
        disposition,
        JsonNode.Parse($"{{\"caseId\":\"{caseId}\",\"decision\":\"{decision}\"}}")!,
        dispatches,
        reserved,
        actual,
        actual.HasValue,
        actual.HasValue ? 10 : null,
        actual.HasValue ? 5 : null,
        null);
}
