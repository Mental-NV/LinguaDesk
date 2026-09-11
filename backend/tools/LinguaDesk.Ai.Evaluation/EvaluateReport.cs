using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using LinguaDesk.Core;
using LinguaDesk.Infrastructure.Ai;

namespace LinguaDesk.Ai.Evaluation;

internal sealed record SectionRow(string CaseId, string Decision, object Observation);

internal sealed record SectionOutcome(IReadOnlyList<SectionRow> Rows, int Failed, int Mismatches);

public sealed record ReportRowInput(
    string Section,
    string CaseId,
    string Route,
    string Family,
    string Decision,
    bool MatchesReference,
    bool Skipped,
    string Disposition,
    JsonNode Observation,
    int Dispatches,
    decimal ReservedUsd,
    decimal? ActualUsd,
    bool UsageKnown,
    long? PromptTokens,
    long? CompletionTokens,
    long? ElapsedMs);

public sealed record ReportSectionInput(
    string Name,
    string Kind,
    string FamilyScope,
    bool Live,
    bool CredentialPresent,
    string Status,
    string? Detail,
    long DurationMs,
    int MaxDispatches,
    decimal MaxSpendUsd,
    int DispatchesUsed,
    decimal ReservedUsd,
    decimal UnresolvedUsd,
    IReadOnlyList<string> CaseIds,
    IReadOnlyList<ReportRowInput> Rows);

public sealed record ReportManifestInput(
    string CandidateId,
    string AdapterId,
    string Endpoint,
    string Model,
    string CredentialRef,
    bool CredentialPresent,
    bool Live,
    string SdkRevision,
    string RunnerRevision,
    string LibraryRevision,
    string EligibilityPromptRevision,
    string EligibilityPromptResourceSha256,
    string TranslationPromptRevision,
    string TranslationPromptResourceSha256,
    string RewritingPromptRevision,
    string RewritingPromptResourceSha256,
    string ValidatorRevision,
    double Temperature,
    string Thinking,
    bool JsonResponseMode,
    int MaxOutputTokens,
    int MaxResponseBytes,
    int AttemptTimeoutSeconds,
    string BillingCurrency,
    string BillingPriceSource,
    string BillingPriceCheckDate,
    decimal PeakInputPerMillionTokens,
    decimal PeakOutputPerMillionTokens,
    int MaxDispatches,
    decimal MaxSpendUsd,
    int DispatchesUsed,
    decimal ReservedUsd,
    decimal UnresolvedUsd,
    decimal? ActualUsd,
    int Concurrency,
    string StartedUtc,
    string FinishedUtc,
    string ServingScopeNote,
    IReadOnlyList<string> SectionNames,
    IReadOnlyList<string> BlockedSections);

public sealed record ReportRevisionSet(
    [property: JsonPropertyOrder(0)] string Sdk,
    [property: JsonPropertyOrder(1)] string Runner,
    [property: JsonPropertyOrder(2)] string Library,
    [property: JsonPropertyOrder(3)] string EligibilityPrompt,
    [property: JsonPropertyOrder(4)] string EligibilityPromptResourceSha256,
    [property: JsonPropertyOrder(5)] string TranslationPrompt,
    [property: JsonPropertyOrder(6)] string TranslationPromptResourceSha256,
    [property: JsonPropertyOrder(7)] string RewritingPrompt,
    [property: JsonPropertyOrder(8)] string RewritingPromptResourceSha256,
    [property: JsonPropertyOrder(9)] string Validator);

public sealed record ReportSettings(
    [property: JsonPropertyOrder(0)] double Temperature,
    [property: JsonPropertyOrder(1)] string Thinking,
    [property: JsonPropertyOrder(2)] bool JsonResponseMode,
    [property: JsonPropertyOrder(3)] int MaxOutputTokens,
    [property: JsonPropertyOrder(4)] int MaxResponseBytes,
    [property: JsonPropertyOrder(5)] int AttemptTimeoutSeconds);

public sealed record ReportBilling(
    [property: JsonPropertyOrder(0)] string Currency,
    [property: JsonPropertyOrder(1)] string PriceSource,
    [property: JsonPropertyOrder(2)] string PriceCheckDate,
    [property: JsonPropertyOrder(3)] decimal PeakInputPerMillionTokens,
    [property: JsonPropertyOrder(4)] decimal PeakOutputPerMillionTokens);

public sealed record ReportBudget(
    [property: JsonPropertyOrder(0)] int MaxDispatches,
    [property: JsonPropertyOrder(1)] decimal MaxSpendUsd,
    [property: JsonPropertyOrder(2)] int DispatchesUsed,
    [property: JsonPropertyOrder(3)] decimal ReservedUsd,
    [property: JsonPropertyOrder(4)] decimal? ActualUsd,
    [property: JsonPropertyOrder(5)] decimal UnresolvedUsd);

public sealed record ReportSelection(
    [property: JsonPropertyOrder(0)] string Profile,
    [property: JsonPropertyOrder(1)] IReadOnlyList<string> Sections,
    [property: JsonPropertyOrder(2)] int Concurrency);

public sealed record ReportCorpusSection(
    [property: JsonPropertyOrder(0)] string Section,
    [property: JsonPropertyOrder(1)] IReadOnlyList<string> CaseIds);

public sealed record ReportCorpus(
    [property: JsonPropertyOrder(0)] string Provenance,
    [property: JsonPropertyOrder(1)] IReadOnlyList<ReportCorpusSection> Sections);

public sealed record ReportTimestamps(
    [property: JsonPropertyOrder(0)] string StartedUtc,
    [property: JsonPropertyOrder(1)] string FinishedUtc);

public sealed record ReportAccessProbe(
    [property: JsonPropertyOrder(0)] string Disposition,
    [property: JsonPropertyOrder(1)] string CandidateId,
    [property: JsonPropertyOrder(2)] string CredentialRef,
    [property: JsonPropertyOrder(3)] bool CredentialPresent,
    [property: JsonPropertyOrder(4)] bool Live,
    [property: JsonPropertyOrder(5)] int Dispatches,
    [property: JsonPropertyOrder(6)] string Status,
    [property: JsonPropertyOrder(7)] string? Detail,
    [property: JsonPropertyOrder(8)] long? PromptTokens,
    [property: JsonPropertyOrder(9)] long? CompletionTokens,
    [property: JsonPropertyOrder(10)] bool UsageKnown,
    [property: JsonPropertyOrder(11)] decimal ReservedUsd,
    [property: JsonPropertyOrder(12)] decimal? ActualUsd,
    [property: JsonPropertyOrder(13)] decimal UnresolvedUsd,
    [property: JsonPropertyOrder(14)] string? ReturnedModel,
    [property: JsonPropertyOrder(15)] string? ReturnedFingerprint,
    [property: JsonPropertyOrder(16)] long ElapsedMs);

public sealed record ReportSectionCase(
    [property: JsonPropertyOrder(0)] string Disposition,
    [property: JsonPropertyOrder(1)] JsonNode Observation);

public sealed record ReportSection(
    [property: JsonPropertyOrder(0)] string Name,
    [property: JsonPropertyOrder(1)] string Kind,
    [property: JsonPropertyOrder(2)] string FamilyScope,
    [property: JsonPropertyOrder(3)] bool Live,
    [property: JsonPropertyOrder(4)] bool CredentialPresent,
    [property: JsonPropertyOrder(5)] string Status,
    [property: JsonPropertyOrder(6)] string? Detail,
    [property: JsonPropertyOrder(7)] long DurationMs,
    [property: JsonPropertyOrder(8)] ReportBudget Budget,
    [property: JsonPropertyOrder(9)] IReadOnlyList<string> CaseIds,
    [property: JsonPropertyOrder(10)] IReadOnlyList<ReportSectionCase> Cases);

public sealed record ReportAggregate(
    [property: JsonPropertyOrder(0)] string Key,
    [property: JsonPropertyOrder(1)] int Total,
    [property: JsonPropertyOrder(2)] int Matched,
    [property: JsonPropertyOrder(3)] int Mismatched,
    [property: JsonPropertyOrder(4)] int Failed,
    [property: JsonPropertyOrder(5)] int Skipped,
    [property: JsonPropertyOrder(6)] int FaultInjected,
    [property: JsonPropertyOrder(7)] int Dispatches,
    [property: JsonPropertyOrder(8)] decimal ReservedUsd,
    [property: JsonPropertyOrder(9)] decimal? ActualUsd);

public sealed record ReportAggregates(
    [property: JsonPropertyOrder(0)] IReadOnlyList<ReportAggregate> BySection,
    [property: JsonPropertyOrder(1)] IReadOnlyList<ReportAggregate> ByFamily,
    [property: JsonPropertyOrder(2)] IReadOnlyList<ReportAggregate> ByRoute,
    [property: JsonPropertyOrder(3)] IReadOnlyList<ReportAggregate> RewritingModes,
    [property: JsonPropertyOrder(4)] IReadOnlyDictionary<string, int> Dispositions);

public sealed record ReportFamilyExposure(
    [property: JsonPropertyOrder(0)] string Family,
    [property: JsonPropertyOrder(1)] decimal ReservedUsd,
    [property: JsonPropertyOrder(2)] decimal? ActualUsd,
    [property: JsonPropertyOrder(3)] decimal UnresolvedUsd);

public sealed record ReportExposure(
    [property: JsonPropertyOrder(0)] decimal ReservedUsd,
    [property: JsonPropertyOrder(1)] decimal? ActualUsd,
    [property: JsonPropertyOrder(2)] decimal UnresolvedUsd,
    [property: JsonPropertyOrder(3)] IReadOnlyList<ReportFamilyExposure> PerFamily);

public sealed record ReportFindings(
    [property: JsonPropertyOrder(0)] int Failed,
    [property: JsonPropertyOrder(1)] int Mismatched,
    [property: JsonPropertyOrder(2)] IReadOnlyList<string> BlockedSections,
    [property: JsonPropertyOrder(3)] int FaultInjected,
    [property: JsonPropertyOrder(4)] int LiveDevelopment,
    [property: JsonPropertyOrder(5)] int OfflineFixture,
    [property: JsonPropertyOrder(6)] int LiveAccess);

public sealed record EvaluationReportDocument(
    [property: JsonPropertyOrder(0)] string Kind,
    [property: JsonPropertyOrder(1)] int FormatVersion,
    [property: JsonPropertyOrder(2)] string CandidateId,
    [property: JsonPropertyOrder(3)] string AdapterId,
    [property: JsonPropertyOrder(4)] string Endpoint,
    [property: JsonPropertyOrder(5)] string Model,
    [property: JsonPropertyOrder(6)] string CredentialRef,
    [property: JsonPropertyOrder(7)] bool CredentialPresent,
    [property: JsonPropertyOrder(8)] bool Live,
    [property: JsonPropertyOrder(9)] ReportRevisionSet Revisions,
    [property: JsonPropertyOrder(10)] ReportSettings Settings,
    [property: JsonPropertyOrder(11)] ReportBilling Billing,
    [property: JsonPropertyOrder(12)] ReportBudget Budget,
    [property: JsonPropertyOrder(13)] ReportSelection Selection,
    [property: JsonPropertyOrder(14)] ReportCorpus Corpus,
    [property: JsonPropertyOrder(15)] ReportTimestamps Timestamps,
    [property: JsonPropertyOrder(16)] string ServingScope,
    [property: JsonPropertyOrder(17)] ReportAccessProbe? AccessProbe,
    [property: JsonPropertyOrder(18)] IReadOnlyList<ReportSection> Sections,
    [property: JsonPropertyOrder(19)] ReportAggregates Aggregates,
    [property: JsonPropertyOrder(20)] ReportExposure Exposure,
    [property: JsonPropertyOrder(21)] ReportFindings Findings,
    [property: JsonPropertyOrder(22)] string Status,
    [property: JsonPropertyOrder(23)] string? Detail,
    [property: JsonPropertyOrder(24)] IReadOnlyList<string> Limitations);

public static class EvaluationReportBuilder
{
    public static EvaluationReportDocument Build(
        ReportManifestInput manifest,
        ReportAccessProbe? probe,
        IReadOnlyList<ReportSectionInput> sections,
        string status,
        string? detail)
    {
        var rows = sections.SelectMany(section => section.Rows).ToList();
        var reportSections = sections.Select(section => new ReportSection(
            section.Name,
            section.Kind,
            section.FamilyScope,
            section.Live,
            section.CredentialPresent,
            section.Status,
            section.Detail,
            section.DurationMs,
            new ReportBudget(
                section.MaxDispatches, section.MaxSpendUsd,
                section.DispatchesUsed, section.ReservedUsd, ActualFor(section.Rows), section.UnresolvedUsd),
            section.CaseIds,
            [.. section.Rows.Select(row => new ReportSectionCase(row.Disposition, row.Observation))])).ToList();

        return new EvaluationReportDocument(
            "evaluation_report",
            1,
            manifest.CandidateId,
            manifest.AdapterId,
            manifest.Endpoint,
            manifest.Model,
            manifest.CredentialRef,
            manifest.CredentialPresent,
            manifest.Live,
            new ReportRevisionSet(
                manifest.SdkRevision, manifest.RunnerRevision, manifest.LibraryRevision,
                manifest.EligibilityPromptRevision, manifest.EligibilityPromptResourceSha256,
                manifest.TranslationPromptRevision, manifest.TranslationPromptResourceSha256,
                manifest.RewritingPromptRevision, manifest.RewritingPromptResourceSha256,
                manifest.ValidatorRevision),
            new ReportSettings(
                manifest.Temperature, manifest.Thinking, manifest.JsonResponseMode,
                manifest.MaxOutputTokens, manifest.MaxResponseBytes, manifest.AttemptTimeoutSeconds),
            new ReportBilling(
                manifest.BillingCurrency, manifest.BillingPriceSource, manifest.BillingPriceCheckDate,
                manifest.PeakInputPerMillionTokens, manifest.PeakOutputPerMillionTokens),
            new ReportBudget(
                manifest.MaxDispatches, manifest.MaxSpendUsd,
                manifest.DispatchesUsed, manifest.ReservedUsd, manifest.ActualUsd, manifest.UnresolvedUsd),
            new ReportSelection(manifest.CandidateId, [.. manifest.SectionNames], manifest.Concurrency),
            new ReportCorpus(
                "Allowlisted synthetic development slices from M015-M018 (eligibility, translation, rewriting, chain-bounds); no production text.",
                [.. sections.Select(section => new ReportCorpusSection(section.Name, section.CaseIds))]),
            new ReportTimestamps(manifest.StartedUtc, manifest.FinishedUtc),
            manifest.ServingScopeNote,
            probe,
            reportSections,
            Aggregate(rows, probe),
            Expose(manifest, sections),
            new ReportFindings(
                rows.Count(IsFailed),
                rows.Count(row => !row.Skipped && !row.MatchesReference),
                [.. manifest.BlockedSections],
                rows.Count(row => row.Disposition == "fault_injected"),
                rows.Count(row => row.Disposition == "live_development"),
                rows.Count(row => row.Disposition == "offline_fixture"),
                probe is null ? 0 : 1),
            status,
            detail,
            Limitations);
    }

    private static decimal? ActualFor(IEnumerable<ReportRowInput> rows)
    {
        decimal total = 0m;
        var any = false;
        foreach (var row in rows)
        {
            if (row.ActualUsd.HasValue)
            {
                total += row.ActualUsd.Value;
                any = true;
            }
        }

        return any ? total : null;
    }

    private static bool IsFailed(ReportRowInput row) =>
        row.Skipped || string.Equals(row.Disposition, "fault_injected", StringComparison.Ordinal)
            ? false
            : row.Section.StartsWith("chain-bounds", StringComparison.Ordinal)
                ? !string.Equals(row.Decision, "Succeeded", StringComparison.Ordinal)
                : string.Equals(row.Decision, "Failed", StringComparison.Ordinal);

    private static ReportAggregates Aggregate(IReadOnlyList<ReportRowInput> rows, ReportAccessProbe? probe)
    {
        var bySection = rows
            .GroupBy(row => row.Section, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => Summarize(group.Key, group)).ToList();
        var byFamily = rows
            .GroupBy(row => row.Family, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => Summarize(group.Key, group)).ToList();
        var byRoute = rows
            .GroupBy(row => $"{row.Family}|{row.Route}", StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => Summarize(group.Key, group)).ToList();
        var modes = rows
            .Where(row => string.Equals(row.Family, "Rewriting", StringComparison.Ordinal))
            .GroupBy(RewriteMode, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => Summarize(group.Key, group)).ToList();
        var dispositions = rows
            .GroupBy(row => row.Disposition, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        if (probe is not null)
        {
            dispositions["live_access"] = dispositions.TryGetValue("live_access", out var seen) ? seen + 1 : 1;
        }

        return new ReportAggregates(bySection, byFamily, byRoute, modes, dispositions);
    }

    private static ReportAggregate Summarize(string key, IEnumerable<ReportRowInput> rows)
    {
        var list = rows.ToList();
        return new ReportAggregate(
            key,
            list.Count,
            list.Count(row => !row.Skipped && row.MatchesReference),
            list.Count(row => !row.Skipped && !row.MatchesReference),
            list.Count(IsFailed),
            list.Count(row => row.Skipped),
            list.Count(row => row.Disposition == "fault_injected"),
            list.Sum(row => row.Dispatches),
            list.Sum(row => row.ReservedUsd),
            ActualFor(list));
    }

    private static string RewriteMode(ReportRowInput row)
    {
        var route = row.Route;
        var cut = route.LastIndexOf(':');
        return cut >= 0 && cut + 1 < route.Length ? route[(cut + 1)..] : route;
    }

    private static ReportExposure Expose(
        ReportManifestInput manifest,
        IReadOnlyList<ReportSectionInput> sections)
    {
        var attributed = new Dictionary<string, (decimal Reserved, decimal Unresolved)> (StringComparer.Ordinal);
        foreach (var section in sections)
        {
            var sectionReserved = section.Rows.Sum(row => row.ReservedUsd);
            foreach (var familyGroup in section.Rows.GroupBy(row => row.Family, StringComparer.Ordinal))
            {
                var familyReserved = familyGroup.Sum(row => row.ReservedUsd);
                var share = sectionReserved > 0m
                    ? familyReserved / sectionReserved
                    : section.Rows.Count > 0
                        ? (decimal)familyGroup.Count() / section.Rows.Count
                        : 0m;
                var current = attributed.TryGetValue(familyGroup.Key, out var seen)
                    ? seen
                    : (Reserved: 0m, Unresolved: 0m);
                attributed[familyGroup.Key] = (
                    current.Reserved + familyReserved,
                    current.Unresolved + section.UnresolvedUsd * share);
            }
        }

        var rowsByFamily = sections
            .SelectMany(section => section.Rows)
            .GroupBy(row => row.Family, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.Ordinal);
        var perFamily = attributed
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => new ReportFamilyExposure(
                pair.Key,
                pair.Value.Reserved,
                rowsByFamily.TryGetValue(pair.Key, out var familyRows) ? ActualFor(familyRows) : null,
                pair.Value.Unresolved))
            .ToList();
        return new ReportExposure(manifest.ReservedUsd, manifest.ActualUsd, manifest.UnresolvedUsd, perFamily);
    }

    private static IReadOnlyList<string> Limitations { get; } =
    [
        "Development evidence only: fallback liveness, quality/human qualification, corpus approval, percentile performance and production serving remain pending (M035-M037; Q-001/Q-005).",
        "The transport_fixture disposition has no rows in this report; adapter conformance is owned by M019 and reused, not re-proven.",
        "Per-case durations are recorded only where the slice shape carries them (chain-bounds elapsedMs); every section records wall-clock durationMs.",
        "Serving quiescence is unverified for standalone runs; the evaluation credential is shared with serving per AI section 5.2.",
        "No AI grading or human qualification review is included; the handoff human review covers disposition labeling, retained-failure handling and exposure completeness as development acceptance.",
    ];
}

internal static class EvaluateReport
{
    private const string CredentialPrefix = "LINGUADESK_AIEVALUATION__CREDENTIALS__";

    private const string CredentialSuffix = "__APIKEY";

    private static readonly JsonSerializerOptions OutputOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    internal static async Task<int> RunAsync(string[] options)
    {
        if (!EvaluateReportOptions.TryParse(options, out var selected, out var error))
        {
            Console.Error.WriteLine(error);
            Console.Error.WriteLine(
                "Usage: dotnet LinguaDesk.Ai.Evaluation.dll evaluate-report [--offline|--live] " +
                "--profile <id> --max-dispatches <n> --max-spend-usd <amount> [--deadline-ms <n>] [--output <path>]");
            return 2;
        }

        CandidateProfile profile;
        try
        {
            profile = CandidateRegistry.Select(CandidateRegistry.Default, selected.Profile);
        }
        catch (CandidateProfileException exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 2;
        }

        ChainPolicy policy;
        try
        {
            policy = new ChainPolicy(
                TimeSpan.FromMilliseconds(selected.DeadlineMs),
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(10),
                TimeSpan.FromSeconds(2),
                SystemChainClock.Instance);
            policy.Validate();
        }
        catch (Exception exception) when (exception is ArgumentOutOfRangeException or InvalidOperationException)
        {
            Console.Error.WriteLine($"The chain deadline policy is invalid: {exception.Message}");
            return 2;
        }

        var started = DateTime.UtcNow;
        var startedText = started.ToString("o", System.Globalization.CultureInfo.InvariantCulture);
        var eligibilityProbe = EligibilityPrompt.Create(string.Empty, null);
        var translationProbe = TranslationPrompt.Create(string.Empty, "en", "ru");
        var rewritingProbe = RewritingPrompt.Create(
            string.Empty, "en", ProductCatalog.DefaultRewritingMode);

        var fixture = RunFixtureSections(profile, policy, eligibilityProbe, translationProbe, rewritingProbe, selected);
        if (!selected.Live)
        {
            var offline = Manifest(profile, selected, startedText, credentialPresent: false, live: false, BlockedSections: []);
            return WriteCombined(selected, offline, probe: null, [.. fixture]);
        }

        if (!TryResolveCredential(profile.CredentialRef, out var credential) || credential is null)
        {
            var liveSections = BlockedLiveSections();
            var blocked = Manifest(profile, selected, startedText, credentialPresent: false, live: true,
                BlockedSections: ["live-access", "eligibility-live", "translation-live", "rewriting-live", "chain-bounds-live"]);
            return WriteCombined(selected, blocked, probe: null, [.. fixture, .. liveSections]);
        }

        using (credential)
        {
            var budget = new EvaluationBudget(selected.MaxDispatches, selected.MaxSpendUsd);
            var probe = await RunProbeAsync(profile, credential, budget).ConfigureAwait(false);
            var live = await RunLiveSectionsAsync(
                profile, policy, eligibilityProbe, translationProbe, rewritingProbe, budget, credential).ConfigureAwait(false);
            var manifest = Manifest(profile, selected, startedText, credentialPresent: true, live: true, BlockedSections: []);
            var actual = SumActual(fixture.Concat(live).SelectMany(section => section.Rows), probe.ActualUsd);
            manifest = manifest with
            {
                DispatchesUsed = budget.DispatchesUsed,
                ReservedUsd = budget.ReservedUsd,
                UnresolvedUsd = budget.UnresolvedUsd,
                ActualUsd = actual,
            };
            return WriteCombined(selected, manifest, probe, [.. fixture, .. live]);
        }
    }

    private static List<ReportSectionInput> RunFixtureSections(
        CandidateProfile profile,
        ChainPolicy policy,
        PromptSnapshot eligibilityProbe,
        PromptSnapshot translationProbe,
        PromptSnapshot rewritingProbe,
        EvaluateReportOptions selected)
    {
        var sections = new List<ReportSectionInput>();

        var eligibilityWatch = Stopwatch.StartNew();
        var (eligibilityRows, eligibilityMismatches) =
            EvaluateEligibility.RunOfflineSection(profile, eligibilityProbe);
        eligibilityWatch.Stop();
        sections.Add(FixtureSection(
            "eligibility", "eligibility_evaluation_report", "Eligibility", selected,
            eligibilityWatch.ElapsedMilliseconds, eligibilityRows, eligibilityMismatches,
            EvaluateEligibility.FaultCaseIds, RouteFor("eligibility"), FamilyFor("eligibility"),
            Dispatches: 0, Reserved: 0m, Unresolved: 0m));

        var translationWatch = Stopwatch.StartNew();
        var (translationRows, translationMismatches) =
            EvaluateTranslation.RunOfflineSection(profile, eligibilityProbe, translationProbe);
        translationWatch.Stop();
        sections.Add(FixtureSection(
            "translation", "translation_evaluation_report", "Translation", selected,
            translationWatch.ElapsedMilliseconds, translationRows, translationMismatches,
            EvaluateTranslation.FaultCaseIds, RouteFor("translation"), FamilyFor("translation"),
            Dispatches: 0, Reserved: 0m, Unresolved: 0m));

        var rewritingWatch = Stopwatch.StartNew();
        var (rewritingRows, rewritingMismatches) =
            EvaluateRewriting.RunOfflineSection(profile, eligibilityProbe, rewritingProbe);
        rewritingWatch.Stop();
        sections.Add(FixtureSection(
            "rewriting", "rewriting_evaluation_report", "Rewriting", selected,
            rewritingWatch.ElapsedMilliseconds, rewritingRows, rewritingMismatches,
            EvaluateRewriting.FaultCaseIds, RouteFor("rewriting"), FamilyFor("rewriting"),
            Dispatches: 0, Reserved: 0m, Unresolved: 0m));

        var chainBudget = new EvaluationBudget(selected.MaxDispatches, selected.MaxSpendUsd);
        var chainWatch = Stopwatch.StartNew();
        var (chainRows, chainMismatches) =
            EvaluateChainBounds.RunOfflineSection(profile, policy, chainBudget);
        chainWatch.Stop();
        sections.Add(FixtureSection(
            "chain-bounds", "chain_bounds_report", "ChainBounds", selected,
            chainWatch.ElapsedMilliseconds, chainRows, chainMismatches,
            EvaluateChainBounds.FaultCaseIds, RouteFor("chain-bounds"), FamilyFor("chain-bounds"),
            chainBudget.DispatchesUsed, chainBudget.ReservedUsd, chainBudget.UnresolvedUsd));

        return sections;
    }

    private static ReportSectionInput FixtureSection(
        string name,
        string kind,
        string familyScope,
        EvaluateReportOptions selected,
        long durationMs,
        IReadOnlyList<SectionRow> rows,
        int mismatches,
        IReadOnlySet<string> faultCaseIds,
        Func<JsonNode, string> route,
        Func<JsonNode, string> family,
        int Dispatches,
        decimal Reserved,
        decimal Unresolved)
    {
        var mapped = MapRows(name, rows, faultCaseIds, Offline: true, route, family);
        return new ReportSectionInput(
            name, kind, familyScope, Live: false, CredentialPresent: false,
            mismatches == 0 ? "pass" : "fail",
            mismatches == 0 ? null : $"{mismatches} offline case(s) disagree with the allowlisted reference.",
            durationMs, selected.MaxDispatches, selected.MaxSpendUsd,
            Dispatches, Reserved, Unresolved,
            [.. rows.Select(row => row.CaseId)], mapped);
    }

    private static async Task<IReadOnlyList<ReportSectionInput>> RunLiveSectionsAsync(
        CandidateProfile profile,
        ChainPolicy policy,
        PromptSnapshot eligibilityProbe,
        PromptSnapshot translationProbe,
        PromptSnapshot rewritingProbe,
        EvaluationBudget budget,
        TransportCredential credential)
    {
        var sections = new List<ReportSectionInput>();

        sections.Add(await LiveSectionAsync(
            "eligibility-live", "eligibility_evaluation_report", "Eligibility",
            () => EvaluateEligibility.RunLiveSectionAsync(profile, eligibilityProbe, budget, credential),
            RouteFor("eligibility"), FamilyFor("eligibility")).ConfigureAwait(false));
        sections.Add(await LiveSectionAsync(
            "translation-live", "translation_evaluation_report", "Translation",
            () => EvaluateTranslation.RunLiveSectionAsync(profile, eligibilityProbe, translationProbe, budget, credential),
            RouteFor("translation"), FamilyFor("translation")).ConfigureAwait(false));
        sections.Add(await LiveSectionAsync(
            "rewriting-live", "rewriting_evaluation_report", "Rewriting",
            () => EvaluateRewriting.RunLiveSectionAsync(profile, eligibilityProbe, rewritingProbe, budget, credential),
            RouteFor("rewriting"), FamilyFor("rewriting")).ConfigureAwait(false));
        sections.Add(await LiveSectionAsync(
            "chain-bounds-live", "chain_bounds_report", "ChainBounds",
            () => EvaluateChainBounds.RunLiveSectionAsync(profile, policy, budget, credential),
            RouteFor("chain-bounds"), FamilyFor("chain-bounds")).ConfigureAwait(false));

        return sections;

        async Task<ReportSectionInput> LiveSectionAsync(
            string name,
            string kind,
            string familyScope,
            Func<Task<SectionOutcome>> run,
            Func<JsonNode, string> route,
            Func<JsonNode, string> family)
        {
            var before = Snapshot(budget);
            var watch = Stopwatch.StartNew();
            var outcome = await run().ConfigureAwait(false);
            watch.Stop();
            var delta = Delta(budget, before);
            var mapped = MapRows(name, outcome.Rows, FaultIds: null, Offline: false, route, family);
            return new ReportSectionInput(
                name, kind, familyScope, Live: true, CredentialPresent: true,
                outcome.Failed == 0 ? "success" : "fail",
                outcome.Failed == 0
                    ? (outcome.Mismatches == 0
                        ? null
                        : $"{outcome.Mismatches} live case(s) disagree with the development reference; see per-case rows.")
                    : $"{outcome.Failed} live case(s) failed; failures are retained per case with usage/exposure metadata.",
                watch.ElapsedMilliseconds, budget.MaxDispatches, budget.MaxSpendUsd,
                delta.Dispatches, delta.Reserved, delta.Unresolved,
                [.. outcome.Rows.Select(row => row.CaseId)], mapped);
        }
    }

    private static IReadOnlyList<ReportSectionInput> BlockedLiveSections() =>
    [
        BlockedSection("eligibility-live", "eligibility_evaluation_report", "Eligibility"),
        BlockedSection("translation-live", "translation_evaluation_report", "Translation"),
        BlockedSection("rewriting-live", "rewriting_evaluation_report", "Rewriting"),
        BlockedSection("chain-bounds-live", "chain_bounds_report", "ChainBounds"),
    ];

    private static ReportSectionInput BlockedSection(string name, string kind, string familyScope) =>
        new(name, kind, familyScope, Live: true, CredentialPresent: false,
            "blocked", "The evaluation credential is missing or blank; no provider dispatch was made.",
            0, 0, 0m, 0, 0m, 0m, [], []);

    private static (int Dispatches, decimal Reserved, decimal Unresolved) Snapshot(EvaluationBudget budget) =>
        (budget.DispatchesUsed, budget.ReservedUsd, budget.UnresolvedUsd);

    private static (int Dispatches, decimal Reserved, decimal Unresolved) Delta(
        EvaluationBudget budget,
        (int Dispatches, decimal Reserved, decimal Unresolved) before) =>
        (budget.DispatchesUsed - before.Dispatches,
            budget.ReservedUsd - before.Reserved,
            budget.UnresolvedUsd - before.Unresolved);

    private static List<ReportRowInput> MapRows(
        string section,
        IReadOnlyList<SectionRow> rows,
        IReadOnlySet<string>? FaultIds,
        bool Offline,
        Func<JsonNode, string> route,
        Func<JsonNode, string> family)
    {
        var mapped = new List<ReportRowInput>(rows.Count);
        foreach (var row in rows)
        {
            var node = JsonSerializer.SerializeToNode(row.Observation, OutputOptions)
                ?? throw new InvalidOperationException($"The {section}/{row.CaseId} observation did not serialize.");
            var skipped = string.Equals(row.Decision, "Skipped", StringComparison.Ordinal);
            var disposition = Offline
                ? (FaultIds is not null && FaultIds.Contains(row.CaseId) ? "fault_injected" : "offline_fixture")
                : (skipped ? "fault_injected" : "live_development");
            mapped.Add(new ReportRowInput(
                section,
                row.CaseId,
                route(node),
                family(node),
                row.Decision,
                node["matchesReference"]?.GetValue<bool>() ?? false,
                skipped,
                disposition,
                node,
                node["dispatches"]?.GetValue<int>() ?? 0,
                node["reservedUsd"]?.GetValue<decimal>() ?? 0m,
                node["actualUsd"]?.GetValue<decimal?>(),
                node["usageKnown"]?.GetValue<bool>() ?? false,
                node["promptTokens"]?.GetValue<long?>(),
                node["completionTokens"]?.GetValue<long?>(),
                node["elapsedMs"]?.GetValue<long?>()));
        }

        return mapped;
    }

    private static Func<JsonNode, string> RouteFor(string section) => section switch
    {
        "eligibility" => node => node["operation"]?.GetValue<string>() ?? "unknown",
        "translation" => node => node["direction"]?.GetValue<string>() ?? "unknown",
        "rewriting" => node => node["cell"]?.GetValue<string>() ?? "unknown",
        _ => node => node["directionOrCell"]?.GetValue<string>() ?? "unknown",
    };

    private static Func<JsonNode, string> FamilyFor(string section) => section switch
    {
        "eligibility" => _ => "Eligibility",
        "translation" => _ => "Translation",
        "rewriting" => _ => "Rewriting",
        _ => node => node["family"]?.GetValue<string>() ?? "ChainBounds",
    };

    private static async Task<ReportAccessProbe> RunProbeAsync(
        CandidateProfile profile,
        TransportCredential credential,
        EvaluationBudget budget)
    {
        using var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(profile.Bounds.AttemptTimeoutSeconds),
        };
        var adapter = new ChatCompletionsAdapter(profile, httpClient);
        var watch = Stopwatch.StartNew();
        try
        {
            var observation = await adapter.SendAsync(
                EligibilityPrompt.Create(Program.FixedSource).Messages,
                credential,
                budget,
                CancellationToken.None).ConfigureAwait(false);
            watch.Stop();
            return new ReportAccessProbe(
                "live_access",
                observation.CandidateId,
                observation.CredentialRef,
                CredentialPresent: true,
                Live: true,
                observation.DispatchCount,
                "success",
                null,
                observation.Usage?.PromptTokens,
                observation.Usage?.CompletionTokens,
                observation.Usage is not null,
                observation.ReservedUsd,
                observation.ActualUsd,
                budget.UnresolvedUsd,
                observation.ReturnedModel,
                observation.ReturnedFingerprint,
                watch.ElapsedMilliseconds);
        }
        catch (ChatCompletionsAdapterException exception)
        {
            watch.Stop();
            return new ReportAccessProbe(
                "live_access",
                profile.CandidateId,
                profile.CredentialRef,
                CredentialPresent: true,
                Live: true,
                adapter.DispatchCount,
                "error",
                $"{exception.Kind}{(exception.StatusCode.HasValue ? $" HTTP {exception.StatusCode.Value}" : string.Empty)}",
                null,
                null,
                false,
                0m,
                null,
                budget.UnresolvedUsd,
                null,
                null,
                watch.ElapsedMilliseconds);
        }
        catch (OperationCanceledException)
        {
            watch.Stop();
            return new ReportAccessProbe(
                "live_access",
                profile.CandidateId,
                profile.CredentialRef,
                CredentialPresent: true,
                Live: true,
                adapter.DispatchCount,
                "error",
                "Timeout",
                null,
                null,
                false,
                0m,
                null,
                budget.UnresolvedUsd,
                null,
                null,
                watch.ElapsedMilliseconds);
        }
    }

    private static ReportManifestInput Manifest(
        CandidateProfile profile,
        EvaluateReportOptions selected,
        string startedText,
        bool credentialPresent,
        bool live,
        IReadOnlyList<string> BlockedSections) =>
        new(
            profile.CandidateId,
            profile.AdapterId,
            profile.Endpoint,
            profile.Model,
            profile.CredentialRef,
            credentialPresent,
            live,
            RuntimeInformation.FrameworkDescription,
            AssemblyVersion(typeof(EvaluateReport).Assembly),
            AssemblyVersion(typeof(EvaluationBudget).Assembly),
            EligibilityEnvelopeParser.PromptRevision,
            EligibilityPrompt.Create(string.Empty, null).ResourceSha256,
            TranslationEnvelopeParser.PromptRevision,
            TranslationPrompt.Create(string.Empty, "en", "ru").ResourceSha256,
            RewritingEnvelopeParser.PromptRevision,
            RewritingPrompt.Create(string.Empty, "en", ProductCatalog.DefaultRewritingMode).ResourceSha256,
            profile.ValidatorRevision,
            profile.Settings.Temperature,
            profile.Settings.Thinking,
            profile.Settings.JsonResponseMode,
            profile.Bounds.MaxOutputTokens,
            profile.Bounds.MaxResponseBytes,
            profile.Bounds.AttemptTimeoutSeconds,
            profile.Billing.Currency,
            profile.Billing.PriceSource,
            profile.Billing.PriceCheckDate,
            profile.Billing.PeakInputPerMillionTokens,
            profile.Billing.PeakOutputPerMillionTokens,
            selected.MaxDispatches,
            selected.MaxSpendUsd,
            0,
            0m,
            0m,
            null,
            1,
            startedText,
            DateTime.UtcNow.ToString("o", System.Globalization.CultureInfo.InvariantCulture),
            "Standalone runner with a per-run EvaluationBudget; the credential is shared with serving per AI section 5.2 and serving quiescence was not verified for this run.",
            ["eligibility", "translation", "rewriting", "chain-bounds"],
            BlockedSections);

    private static string AssemblyVersion(Assembly assembly) =>
        assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? assembly.GetName().Version?.ToString() ?? "unknown";

    private static decimal? SumActual(IEnumerable<ReportRowInput> rows, decimal? probeActual)
    {
        decimal total = probeActual ?? 0m;
        var any = probeActual.HasValue;
        foreach (var row in rows)
        {
            if (row.ActualUsd.HasValue)
            {
                total += row.ActualUsd.Value;
                any = true;
            }
        }

        return any ? total : null;
    }

    private static int WriteCombined(
        EvaluateReportOptions selected,
        ReportManifestInput manifest,
        ReportAccessProbe? probe,
        IReadOnlyList<ReportSectionInput> sections)
    {
        var rows = sections.SelectMany(section => section.Rows).ToList();
        var failed = rows.Count(row =>
            !row.Skipped
            && !string.Equals(row.Disposition, "fault_injected", StringComparison.Ordinal)
            && (row.Section.StartsWith("chain-bounds", StringComparison.Ordinal)
                ? !string.Equals(row.Decision, "Succeeded", StringComparison.Ordinal)
                : string.Equals(row.Decision, "Failed", StringComparison.Ordinal)));
        var mismatches = rows.Count(row => !row.Skipped && !row.MatchesReference);
        var blocked = manifest.BlockedSections.Count > 0;
        var probeFailed = probe is not null && !string.Equals(probe.Status, "success", StringComparison.Ordinal);

        string status;
        string? detail;
        int exitCode;
        if (blocked)
        {
            status = "blocked";
            detail = "The evaluation credential is missing or blank; live sections report blocked with zero dispatches and no scripted fallback while the offline fixture sections completed.";
            exitCode = 3;
        }
        else if (failed > 0 || mismatches > 0 || probeFailed)
        {
            status = "fail";
            detail = probeFailed && failed == 0 && mismatches == 0
                ? "The live access probe did not succeed; the failure is retained with usage/exposure metadata."
                : $"{failed} failed and {mismatches} mismatched case(s); failures are retained per case with usage/exposure metadata.";
            exitCode = 1;
        }
        else
        {
            status = manifest.Live ? "success" : "pass";
            detail = null;
            exitCode = 0;
        }

        var document = EvaluationReportBuilder.Build(manifest, probe, sections, status, detail);
        var payload = JsonSerializer.Serialize(document, OutputOptions) + "\n";
        Console.Out.Write(payload);

        var path = selected.Output ?? DefaultReportPath(manifest.Live);
        var directory = Path.GetDirectoryName(path);
        if (directory is not null)
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, payload, Encoding.UTF8);
        Console.Error.WriteLine($"Evaluation report: {path}");
        return exitCode;
    }

    private static string DefaultReportPath(bool live)
    {
        var root = FindRepositoryRoot() ?? Directory.GetCurrentDirectory();
        var stamp = DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ", System.Globalization.CultureInfo.InvariantCulture);
        return Path.Combine(root, "artifacts", "evaluation", $"evaluate-report-{stamp}-{(live ? "live" : "offline")}.json");
    }

    private static string? FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "scripts", "ai.sh")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return null;
    }

    private static bool TryResolveCredential(string credentialRef, out TransportCredential? credential)
    {
        var variable = CredentialPrefix
            + credentialRef.ToUpperInvariant().Replace("-", "_", StringComparison.Ordinal)
            + CredentialSuffix;
        var value = Environment.GetEnvironmentVariable(variable);
        if (string.IsNullOrWhiteSpace(value))
        {
            credential = null;
            return false;
        }

        credential = new TransportCredential(value);
        return true;
    }

    private sealed record EvaluateReportOptions(
        bool Live,
        string Profile,
        int MaxDispatches,
        decimal MaxSpendUsd,
        int DeadlineMs,
        string? Output)
    {
        public static bool TryParse(string[] options, out EvaluateReportOptions selected, out string error)
        {
            var live = false;
            var offline = false;
            string? profile = null;
            string? output = null;
            int maxDispatches = 0;
            decimal maxSpend = 0m;
            int deadlineMs = 30000;

            for (var index = 0; index < options.Length; index++)
            {
                switch (options[index])
                {
                    case "--live":
                        live = true;
                        break;
                    case "--offline":
                        offline = true;
                        break;
                    case "--profile" when index + 1 < options.Length:
                        profile = options[index + 1];
                        index++;
                        break;
                    case "--output" when index + 1 < options.Length:
                        output = options[index + 1];
                        index++;
                        break;
                    case "--max-dispatches" when index + 1 < options.Length
                        && int.TryParse(
                            options[index + 1],
                            System.Globalization.NumberStyles.Integer,
                            System.Globalization.CultureInfo.InvariantCulture,
                            out var dispatches):
                        maxDispatches = dispatches;
                        index++;
                        break;
                    case "--max-spend-usd" when index + 1 < options.Length
                        && decimal.TryParse(
                            options[index + 1],
                            System.Globalization.NumberStyles.Number,
                            System.Globalization.CultureInfo.InvariantCulture,
                            out var spend):
                        maxSpend = spend;
                        index++;
                        break;
                    case "--deadline-ms" when index + 1 < options.Length
                        && int.TryParse(
                            options[index + 1],
                            System.Globalization.NumberStyles.Integer,
                            System.Globalization.CultureInfo.InvariantCulture,
                            out var deadline):
                        deadlineMs = deadline;
                        index++;
                        break;
                    default:
                        selected = null!;
                        error = $"Unknown or incomplete evaluate-report option '{options[index]}'.";
                        return false;
                }
            }

            if (live && offline)
            {
                selected = null!;
                error = "evaluate-report accepts at most one of --live and --offline.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(profile))
            {
                selected = null!;
                error = "evaluate-report requires --profile <candidate-id>.";
                return false;
            }

            if (maxDispatches <= 0 || maxSpend <= 0)
            {
                selected = null!;
                error = "evaluate-report requires a positive finite --max-dispatches and --max-spend-usd budget.";
                return false;
            }

            if (deadlineMs <= 0)
            {
                selected = null!;
                error = "evaluate-report requires a positive finite --deadline-ms overall deadline.";
                return false;
            }

            selected = new EvaluateReportOptions(live, profile, maxDispatches, maxSpend, deadlineMs, output);
            error = string.Empty;
            return true;
        }
    }
}
