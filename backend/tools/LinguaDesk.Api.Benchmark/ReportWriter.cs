using System.Text.Json;
using System.Text.Json.Serialization;

namespace LinguaDesk.Api.Benchmark;

public sealed record DeploymentSection(
    string Kind,
    string BaseUrl,
    string Environment,
    string Transport,
    string Database,
    string Provider,
    string? CredentialRef,
    bool CredentialPresent,
    int PaidDispatches,
    string PaidDispatchBasis,
    string Runtime,
    string Network);

public sealed record ConfigurationSection(
    int Concurrency,
    int InterleaveSeed,
    int TargetMs,
    string MonetaryAdmission,
    string Account,
    IReadOnlyList<string> ExecutionOrder,
    int MaxInFlightObserved,
    double DrainMs,
    DateTimeOffset StartedUtc,
    DateTimeOffset FinishedUtc);

public sealed record ReadinessSection(
    int Probes,
    string Detail,
    string InitializationCost);

public sealed record RuleMappingEntry(string RuleId, string FullRule, string? RehearsedAs, string? SuccessorOnly);

public sealed record RouteDistribution(
    string Route,
    int Total,
    int Succeeded,
    int SuccessWithinTarget);

public sealed record FamilyAggregation(
    string Family,
    int Total,
    int Succeeded,
    IReadOnlyDictionary<string, int> FailuresByCategory,
    int SuccessWithinTarget,
    double SuccessWithinTargetRate,
    double? P50Ms,
    double? P95Ms,
    IReadOnlyList<RouteDistribution> Routes);

public sealed record CohortSlice(int Total, int Succeeded, int SuccessWithinTarget);

public sealed record CohortSection(
    CohortSlice FirstSeen,
    CohortSlice Repeated,
    int CacheHit,
    int CacheMiss,
    int CacheUnknown,
    string CacheNote);

public sealed record MonetaryExposure(decimal? ReservedUsd, decimal? ActualUsd, decimal? UnresolvedUsd, string Note);

public sealed record ExposureSection(
    int PaidDispatches,
    int CharacterSpendCommitted,
    MonetaryExposure Monetary,
    string Basis);

public sealed record ReuseAssertionSection(
    string ApplicationReuse,
    string RunnerReuse,
    string Evidence);

public sealed record BenchmarkReport(
    string Kind,
    string SchemaRevision,
    string WorkloadName,
    string Scale,
    DateTimeOffset GeneratedUtc,
    string CodeRevision,
    string DotnetSdk,
    string ManifestSha256,
    string RulesSha256,
    DeploymentSection Deployment,
    ConfigurationSection Configuration,
    ReadinessSection Readiness,
    IReadOnlyList<RuleMappingEntry> RuleMapping,
    IReadOnlyList<RequestObservation> Observations,
    FamilyAggregation Translation,
    FamilyAggregation Rewriting,
    CohortSection Cohorts,
    ExposureSection Exposure,
    ReuseAssertionSection ReuseAssertion,
    IReadOnlyList<string> Limitations,
    string Status,
    IReadOnlyList<string> AuditViolations);

public sealed record FullRulesDocument(IReadOnlyList<FullRule> Rules);

public sealed record FullRule(string Id, string Rule);

public static class ReportWriter
{
    private static readonly JsonSerializerOptions ReportOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };

    private static readonly JsonSerializerOptions RulesOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static FullRulesDocument LoadRules(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var options = RulesOptions;
        var json = File.ReadAllText(path);
        var document = JsonSerializer.Deserialize<RulesFile>(json, options)
            ?? throw new InvalidOperationException($"The rules file '{path}' is empty.");
        if (!string.Equals(document.SchemaRevision, "benchmark-rules.v1", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Rules file '{path}' carries unknown schema '{document.SchemaRevision}'.");
        }

        return new FullRulesDocument(
            document.Rules!.Select(rule => new FullRule(rule.Id!, rule.Rule!)).ToList());
    }

    public static BenchmarkReport Build(
        BenchmarkManifest manifest,
        FullRulesDocument rules,
        IReadOnlyList<RequestObservation> observations,
        DriverRun run,
        AuditResult audit,
        string codeRevision,
        string dotnetSdk,
        string manifestSha256,
        string rulesSha256,
        int readinessProbes,
        string baseUrl,
        string runtime,
        DateTimeOffset generatedUtc)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(observations);
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(audit);

        var mapping = rules.Rules.Select(rule =>
        {
            var entry = manifest.SuccessorMapping.FirstOrDefault(item =>
                string.Equals(item.RuleId, rule.Id, StringComparison.Ordinal));
            return new RuleMappingEntry(rule.Id, rule.Rule, entry?.RehearsedAs, entry?.SuccessorOnly);
        }).ToList();

        var unmapped = mapping.Where(entry => entry.RehearsedAs is null && entry.SuccessorOnly is null).ToList();
        if (unmapped.Count > 0)
        {
            throw new InvalidOperationException(
                $"Rules without a rehearsal analogue or deferral: {string.Join(",", unmapped.Select(entry => entry.RuleId))}.");
        }

        var translation = Aggregate("translation", observations, manifest.TargetMs);
        var rewriting = Aggregate("rewriting", observations, manifest.TargetMs);
        var firstSeen = Slice(observations.Where(item => item.FirstSeen).ToList(), manifest.TargetMs);
        var repeated = Slice(observations.Where(item => !item.FirstSeen).ToList(), manifest.TargetMs);

        var status = audit.Passed && observations.Count == run.Observations.Count ? "complete" : "failed";
        if (observations.Count == 0)
        {
            status = "failed";
        }

        return new BenchmarkReport(
            "benchmark_rehearsal_report",
            "benchmark-report.v1",
            manifest.WorkloadName,
            "1/15 of the section 6.1 percentile workload (24 of 360 measured requests)",
            generatedUtc,
            codeRevision,
            dotnetSdk,
            manifestSha256,
            rulesSha256,
            new DeploymentSection(
                "owned-loopback-rehearsal-host",
                baseUrl,
                "Smoke",
                "http over loopback (TLS terminates at deployment; out of rehearsal scope)",
                "isolated migrated file-backed SQLite (WAL) in a scratch directory",
                "deterministic-smoke-fake (in-process, zero network dispatches)",
                null,
                CredentialPresent: false,
                0,
                "credential environment stripped before launch; deterministic clients never dial the network",
                runtime,
                "loopback"),
            new ConfigurationSection(
                manifest.Concurrency,
                manifest.InterleaveSeed,
                manifest.TargetMs,
                "MonetaryAdmission monthly cap 1000000 minor USD (fixed test cap; rehearsal is unpaid)",
                "one synthetic local account via auto-verified registration plus real Bearer [REDACTED]",
                run.Observations.Select(item => item.ComboId + (item.IsRepeat ? "#repeat" : "#first")).ToList(),
                run.MaxInFlight,
                run.DrainMs,
                run.StartedUtc,
                run.FinishedUtc),
            new ReadinessSection(
                readinessProbes,
                "GET /health/live and /health/ready polled by the harness launcher until healthy; excluded from measured requests",
                "none (no paid warm-up; deterministic provider needs none)"),
            mapping,
            observations,
            translation,
            rewriting,
            new CohortSection(
                firstSeen,
                repeated,
                0,
                0,
                observations.Count,
                "insufficient evidence stays unknown, never assumed: first-seen inputs are not proof of a cold provider cache and the synchronous API exposes no cache signal"),
            new ExposureSection(
                0,
                observations.Where(item => item.Outcome == "success" && item.CharacterCount.HasValue).Sum(item => item.CharacterCount!.Value),
                new MonetaryExposure(null, null, null, "the synchronous API exposes no per-request monetary exposure fields; zero paid dispatches by construction"),
                "character spend from committed success charges; monetary exposure unknown-by-design in the rehearsal"),
            new ReuseAssertionSection(
                "disabled: every submission mints a fresh UUIDv7; the composition auditor fails repeated identities",
                "disabled: the driver keeps no response cache and every observation carries its own submission timing; replays fail the audit",
                "24 unique submission identities with independent submission-to-complete-body clocks"),
            [
                "Rehearsal proves harness recording fidelity only: it claims no NFR-002 compliance and discharges no part of G2.",
                "Fake-provider speed is not provider latency; timings prove the clock, not serving performance.",
                "Concurrency 4 proves the N-in-flight discipline logic, not 10-way host behavior.",
                "Per-stage server timings are unexposed by the synchronous API and stay unknown with provenance; totals are authoritative.",
                "Loopback timing noise is irrelevant: the rehearsal asserts recording, not targets.",
            ],
            status,
            audit.Violations);
    }

    public static void Write(BenchmarkReport report, string path)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        File.WriteAllText(path, JsonSerializer.Serialize(report, ReportOptions) + "\n");
    }

    public static string Serialize(BenchmarkReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        return JsonSerializer.Serialize(report, ReportOptions);
    }

    private static FamilyAggregation Aggregate(
        string family, IReadOnlyList<RequestObservation> observations, int targetMs)
    {
        var members = observations.Where(item => item.Family == family).ToList();
        var effective = members
            .Select(item => BenchmarkMath.EffectiveLatencyMs(item.Outcome == "success", item.TotalMs))
            .OrderBy(value => value)
            .ToList();
        var within = members.Count(item => item.Outcome == "success" && item.TotalMs <= targetMs);
        var (count, total, rate) = BenchmarkMath.SuccessWithinTarget(within, Math.Max(1, members.Count));
        var failures = members
            .Where(item => item.Outcome != "success")
            .GroupBy(item => item.FailureCategory ?? "unknown", StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        var routes = members
            .GroupBy(item => item.Route, StringComparer.Ordinal)
            .Select(group => new RouteDistribution(
                group.Key,
                group.Count(),
                group.Count(item => item.Outcome == "success"),
                group.Count(item => item.Outcome == "success" && item.TotalMs <= targetMs)))
            .OrderBy(route => route.Route, StringComparer.Ordinal)
            .ToList();

        return new FamilyAggregation(
            family,
            members.Count,
            members.Count(item => item.Outcome == "success"),
            failures,
            members.Count == 0 ? 0 : count,
            members.Count == 0 ? 0 : rate,
            FiniteOrNull(BenchmarkMath.PercentileNearestRank(effective.Count == 0 ? [double.PositiveInfinity] : effective, 50)),
            FiniteOrNull(BenchmarkMath.PercentileNearestRank(effective.Count == 0 ? [double.PositiveInfinity] : effective, 95)),
            routes);
    }

    private static CohortSlice Slice(List<RequestObservation> members, int targetMs) =>
        new(
            members.Count,
            members.Count(item => item.Outcome == "success"),
            members.Count(item => item.Outcome == "success" && item.TotalMs <= targetMs));

    private static double? FiniteOrNull(double value) =>
        double.IsPositiveInfinity(value) ? null : value;

    private sealed class RulesFile
    {
        public string? SchemaRevision { get; set; }

        public List<RuleRecord>? Rules { get; set; }
    }

    private sealed class RuleRecord
    {
        public string? Id { get; set; }

        public string? Rule { get; set; }
    }
}
