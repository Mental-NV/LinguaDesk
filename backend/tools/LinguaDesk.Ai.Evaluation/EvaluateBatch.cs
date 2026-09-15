using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using LinguaDesk.Infrastructure.Ai;
using Microsoft.Extensions.AI;

namespace LinguaDesk.Ai.Evaluation;

// M036 batch-input runner: frozen B1 through one primary-only candidate
// chain plus a separately selected, blinded judge. Offline (default) is scripted
// with zero dispatches for CI; --live executes the production traversal
// (ChainOrchestrator, primary-only, ChainPolicy stage timeouts) under the
// admitted dispatch/spend budget. Every row is labeled offline_fixture or
// live_qualification; the two are never mixed.
internal static class EvaluateBatch
{
    private const string CredentialPrefix = "LINGUADESK_AIEVALUATION__CREDENTIALS__";

    private const string CredentialSuffix = "__APIKEY";

    private const string RunnerRevision = "evaluate-batch.v2";

    private const string SelectedJudgeCandidateId = "Qwen-Qwen3.8-Flash";

    private static readonly JsonSerializerOptions OutputOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    internal static async Task<int> RunAsync(string[] options)
    {
        if (!EvaluateBatchOptions.TryParse(options, out var selected, out var error))
        {
            Console.Error.WriteLine(error);
            Console.Error.WriteLine(
                "Usage: dotnet LinguaDesk.Ai.Evaluation.dll evaluate-batch [--offline|--live] " +
                "--corpus <path> --profile <id> --judge-profile Qwen-Qwen3.8-Flash " +
                "--max-dispatches <n> --max-spend-usd <amount> " +
                "[--calibration <path>] [--review-seed <value>] " +
                "[--serving-scope <quiescent|shared-concurrent>] " +
                "[--deadline-ms <n>] [--output <path>]");
            return 2;
        }

        CandidateProfile profile;
        CandidateProfile judgeProfile;
        try
        {
            profile = CandidateRegistry.Select(CandidateRegistry.Default, selected.Profile);
            judgeProfile = CandidateRegistry.Select(CandidateRegistry.Default, selected.JudgeProfile);
        }
        catch (CandidateProfileException exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 2;
        }

        if (!string.Equals(judgeProfile.CandidateId, SelectedJudgeCandidateId, StringComparison.Ordinal))
        {
            Console.Error.WriteLine(
                $"M036 pins judge profile '{SelectedJudgeCandidateId}'; '{judgeProfile.CandidateId}' is not admitted.");
            return 2;
        }

        if (!File.Exists(selected.Corpus))
        {
            Console.Error.WriteLine($"Batch corpus file is missing: {selected.Corpus}");
            return 2;
        }

        var raw = await File.ReadAllBytesAsync(selected.Corpus).ConfigureAwait(false);
        var observedSha = BatchCorpus.ComputeContentSha256(raw);
        var batchId = BatchCorpus.ReadBatchId(Encoding.UTF8.GetString(raw));
        var frozenPin = BatchCorpus.FrozenPinForBatchId(batchId);
        if (frozenPin is null || !BatchCorpus.IsFrozenRevision(raw, batchId))
        {
            // AC-001: any mismatch blocks the run before any dispatch.
            var detail = frozenPin is null
                ? $"Corpus batchId is missing or unrecognized; no frozen revision applies " +
                  $"(observed content hash {observedSha}). The run is blocked with zero dispatches."
                : $"Corpus content hash {observedSha} does not equal the frozen {batchId} revision " +
                  $"{frozenPin}; the run is blocked with zero dispatches.";
            var blocked = BlockedReport(
                profile, judgeProfile, selected, batchId ?? "unknown", frozenPin ?? "unknown", observedSha, detail);
            return WriteReport(blocked, selected, 3);
        }

        IReadOnlyList<BatchCase> cases;
        try
        {
            cases = BatchCorpus.Load(Encoding.UTF8.GetString(raw));
        }
        catch (BatchCorpusException exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 2;
        }

        BatchReviewSelection reviewSelection;
        try
        {
            reviewSelection = BatchReviewSelector.Select(
                cases,
                selected.ReviewSeed ?? "offline-fixture");
        }
        catch (InvalidOperationException exception)
        {
            Console.Error.WriteLine($"Review sample selection failed: {exception.Message}");
            return 2;
        }

        if (!selected.Live)
        {
            return RunOfflineAsync(
                profile, judgeProfile, reviewSelection, selected, batchId!, frozenPin, observedSha, cases);
        }

        JudgeCalibrationApproval calibration;
        try
        {
            calibration = JudgeCalibration.Load(selected.Calibration!, judgeProfile, cases);
        }
        catch (JudgeCalibrationException exception)
        {
            var blocked = BlockedReport(
                profile,
                judgeProfile,
                selected,
                batchId!,
                frozenPin,
                observedSha,
                $"The live judge calibration gate failed: {exception.Message} No provider dispatch was made.",
                reviewSelection);
            return WriteReport(blocked, selected, 3);
        }

        return await RunLiveAsync(
            profile, judgeProfile, calibration, reviewSelection,
            selected, batchId!, frozenPin, observedSha, cases).ConfigureAwait(false);
    }

    private static int RunOfflineAsync(
        CandidateProfile profile,
        CandidateProfile judgeProfile,
        BatchReviewSelection reviewSelection,
        EvaluateBatchOptions selected,
        string batchId,
        string frozenPin,
        string observedSha,
        IReadOnlyList<BatchCase> cases)
    {
        var started = DateTime.UtcNow;
        var calibration = OfflineCalibration();
        var rows = new List<BatchCaseRow>();
        foreach (var kase in cases)
        {
            using var client = new ScriptedBatchClient(ScriptedClassification(kase), ScriptedTransformation(kase));
            var outcome = ExecutePrimaryOnly(profile, kase, _ => client, selected.DeadlineMs);
            var grade = BatchJudge.GradeAsync(
                kase,
                outcome.Decision.ToString(),
                outcome.Category,
                outcome.Text,
                judgeProfile,
                calibration.CalibrationId,
                calibration.CalibrationSha256,
                live: false,
                (_, _) => Task.FromResult(ScriptedJudgeAttempt(judgeProfile)))
                .GetAwaiter().GetResult();
            rows.Add(Observe(kase, outcome, [], grade, "offline_fixture"));
        }

        var report = BuildReport(
            profile, judgeProfile, calibration, reviewSelection, selected, batchId, frozenPin, observedSha,
            hashVerified: true, live: false, credentialPresent: false, judgeCredentialPresent: false,
            started, rows, budget: null, detail: null);
        var exit = report.Findings.Failed == 0
            && report.Findings.Mismatched == 0
            && report.Findings.UnresolvedGrades == 0
            ? 0
            : 1;
        return WriteReport(report with { Status = exit == 0 ? "pass" : "fail" }, selected, exit);
    }

    private static async Task<int> RunLiveAsync(
        CandidateProfile profile,
        CandidateProfile judgeProfile,
        JudgeCalibrationApproval calibration,
        BatchReviewSelection reviewSelection,
        EvaluateBatchOptions selected,
        string batchId,
        string frozenPin,
        string observedSha,
        IReadOnlyList<BatchCase> cases)
    {
        var started = DateTime.UtcNow;
        var candidateCredentialPresent = TryResolveCredential(profile.CredentialRef, out var credential)
            && credential is not null;
        var judgeCredentialPresent = TryResolveCredential(judgeProfile.CredentialRef, out var judgeCredential)
            && judgeCredential is not null;
        if (!candidateCredentialPresent || !judgeCredentialPresent)
        {
            var blocked = BuildReport(
                profile, judgeProfile, calibration, reviewSelection, selected, batchId, frozenPin, observedSha,
                hashVerified: true, live: false, candidateCredentialPresent, judgeCredentialPresent,
                started, [], budget: null,
                "A candidate or judge credential is missing or blank; no provider dispatch was made.");
            credential?.Dispose();
            judgeCredential?.Dispose();
            return WriteReport(blocked with { Status = "blocked" }, selected, 3);
        }

        using (credential!)
        using (judgeCredential!)
        {
            using var candidateHttpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(profile.Bounds.AttemptTimeoutSeconds),
            };
            using var judgeHttpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(judgeProfile.Bounds.AttemptTimeoutSeconds),
            };
            var adapter = new ChatCompletionsAdapter(profile, candidateHttpClient);
            var judgeAdapter = new ChatCompletionsAdapter(judgeProfile, judgeHttpClient);
            var budget = new EvaluationBudget(selected.MaxDispatches, selected.MaxSpendUsd);
            var rows = new List<BatchCaseRow>();
            var halted = false;

            foreach (var kase in cases)
            {
                var attempts = new List<AttemptObservation>();
                var outcome = await ExecutePrimaryOnlyAsync(
                    profile, kase,
                    _ => new BatchLiveClient(adapter, credential!, budget, attempts),
                    budget,
                    selected.DeadlineMs).ConfigureAwait(false);
                var grade = await BatchJudge.GradeAsync(
                    kase,
                    outcome.Decision.ToString(),
                    outcome.Category,
                    outcome.Text,
                    judgeProfile,
                    calibration.CalibrationId,
                    calibration.CalibrationSha256,
                    live: true,
                    async (messages, cancellationToken) =>
                    {
                        var before = judgeAdapter.DispatchCount;
                        try
                        {
                            var observation = await judgeAdapter.SendAsync(
                                messages, judgeCredential!, budget, cancellationToken).ConfigureAwait(false);
                            return new BatchJudgeAttemptResult(
                                judgeAdapter.DispatchCount > before,
                                observation,
                                FailureCategory: null);
                        }
                        catch (ChatCompletionsAdapterException exception)
                        {
                            return new BatchJudgeAttemptResult(
                                judgeAdapter.DispatchCount > before,
                                Observation: null,
                                ChatCompletionsAdapter.FailureCategory(exception));
                        }
                    }).ConfigureAwait(false);
                rows.Add(Observe(kase, outcome, attempts, grade, "live_qualification"));

                if (outcome.Decision is ChainDecision.BudgetDenied
                    || grade.DimensionStatus.Contains("budget-denied", StringComparison.Ordinal))
                {
                    halted = true;
                    break;
                }
            }

            var detail = halted
                ? "The per-run dispatch/spend budget was exhausted; remaining cases were not " +
                  "executed and the budget was never exceeded. Failures are retained per case."
                : rows.Any(row => row.Decision == "Failed")
                    ? "One or more live cases failed; failures are retained per case with usage/exposure metadata."
                    : rows.Any(row => string.Equals(row.Decision, "Succeeded", StringComparison.Ordinal)
                        && !string.Equals(row.Grade.DimensionStatus, "resolved-model-grade", StringComparison.Ordinal))
                        ? "One or more successful outputs has an unresolved judge grade; disposition is blocked."
                        : null;
            var report = BuildReport(
                profile, judgeProfile, calibration, reviewSelection, selected, batchId, frozenPin, observedSha,
                hashVerified: true, live: true, credentialPresent: true, judgeCredentialPresent: true,
                started, rows, budget, detail);
            var exit = report.Findings.Failed == 0 && report.Findings.UnresolvedGrades == 0 ? 0 : 1;
            return WriteReport(report with { Status = exit == 0 ? "success" : "fail" }, selected, exit);
        }
    }

    private static ChainPolicy BatchPolicy(int deadlineMs) => new(
        TimeSpan.FromMilliseconds(deadlineMs),
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(10),
        TimeSpan.FromSeconds(2),
        SystemChainClock.Instance);

    private static FamilyChain PrimaryOnlyChain(BatchCase kase, CandidateProfile profile) =>
        FamilyChain.Create(
            string.Equals(kase.Family, "rewriting", StringComparison.Ordinal)
                ? ChainFamily.Rewriting
                : ChainFamily.Translation,
            CandidateRegistry.Default,
            profile.CandidateId);

    private static ChainOutcome ExecutePrimaryOnly(
        CandidateProfile profile,
        BatchCase kase,
        Func<CandidateProfile, IChatClient> clients,
        int deadlineMs)
    {
        var chain = PrimaryOnlyChain(kase, profile);
        var policy = BatchPolicy(deadlineMs);
        policy.Validate();
        return string.Equals(kase.Family, "rewriting", StringComparison.Ordinal)
            ? ChainOrchestrator.ExecuteRewritingAsync(
                chain, ToRewritingInput(kase), clients, policy).GetAwaiter().GetResult()
            : ChainOrchestrator.ExecuteTranslationAsync(
                chain, ToTranslationInput(kase), clients, policy).GetAwaiter().GetResult();
    }

    private static Task<ChainOutcome> ExecutePrimaryOnlyAsync(
        CandidateProfile profile,
        BatchCase kase,
        Func<CandidateProfile, IChatClient> clients,
        EvaluationBudget budget,
        int deadlineMs)
    {
        ArgumentNullException.ThrowIfNull(clients);
        ArgumentNullException.ThrowIfNull(budget);
        var chain = PrimaryOnlyChain(kase, profile);
        var policy = BatchPolicy(deadlineMs);
        policy.Validate();
        return string.Equals(kase.Family, "rewriting", StringComparison.Ordinal)
            ? ChainOrchestrator.ExecuteRewritingAsync(
                chain, ToRewritingInput(kase), clients, policy, budget)
            : ChainOrchestrator.ExecuteTranslationAsync(
                chain, ToTranslationInput(kase), clients, policy, budget);
    }

    private static TranslationInput ToTranslationInput(BatchCase kase) =>
        new(kase.SourceText, kase.SourceSelection, kase.Target);

    private static RewritingInput ToRewritingInput(BatchCase kase) =>
        new(kase.SourceText, kase.Language ?? kase.SourceSelection, kase.Mode);

    private static string ScriptedClassification(BatchCase kase)
    {
        var language = kase.SourceSelection ?? kase.Language ?? "en";
        return $"{{\"status\":\"eligible\",\"language\":\"{language}\"}}";
    }

    private static string ScriptedTransformation(BatchCase kase) =>
        $"{{\"status\":\"result\",\"text\":\"Scripted offline output for {kase.Id}.\"}}";

    private static JudgeCalibrationApproval OfflineCalibration() => new(
        BatchJudge.OfflineCalibrationRef,
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(BatchJudge.OfflineCalibrationRef))),
        "offline-fixture",
        DevelopmentCorpusSha256: "offline-fixture",
        JudgeRunReportSha256: "offline-fixture",
        CaseCount: 0,
        CasesPerLanguage: new Dictionary<string, int>(StringComparer.Ordinal),
        ReviewerCount: 0,
        Agreement: new JudgeCalibrationAgreement(0, 0, 0, 0, 0),
        ApprovedUtc: "offline-fixture",
        Limitations: ["Scripted offline judge evidence is not human calibration or live quality evidence."]);

    private static BatchJudgeAttemptResult ScriptedJudgeAttempt(CandidateProfile judgeProfile) => new(
        Dispatched: false,
        new AttemptObservation(
            judgeProfile.CandidateId,
            judgeProfile.CredentialRef,
            CredentialPresent: false,
            DispatchCount: 0,
            FinishCategory: "offline_fixture",
            "{\"scores\":{\"meaningFidelity\":3,\"grammarNaturalness\":3," +
                "\"styleOrMode\":3,\"languageAndFormat\":3},\"criticalErrors\":[]," +
                "\"assessment\":\"Scripted offline grade fixture.\"}",
            Usage: null,
            ReturnedModel: null,
            ReturnedFingerprint: null,
            ReservedUsd: 0m,
            ActualUsd: null),
        FailureCategory: null);

    private static BatchCaseRow Observe(
        BatchCase kase,
        ChainOutcome outcome,
        List<AttemptObservation> attempts,
        BatchGradeRecord grade,
        string disposition)
    {
        var usage = CombineUsage(attempts);
        var reserved = attempts.Sum(attempt => attempt.ReservedUsd);
        var actual = CombineActual(attempts);
        var providerDispatches = attempts.Count + grade.JudgeDispatches;
        var route = string.Equals(kase.Family, "rewriting", StringComparison.Ordinal)
            ? $"{kase.Language ?? kase.SourceSelection ?? "auto"}:{kase.Mode}"
            : $"{kase.SourceSelection ?? "auto"}->{kase.Target}";

        return new BatchCaseRow(
            kase.Id,
            kase.Family,
            route,
            disposition,
            outcome.Decision.ToString(),
            outcome.Category,
            outcome.ResolvedLanguage,
            outcome.TargetOrMode,
            grade.EligibilityMatch,
            outcome.Dispatches,
            providerDispatches,
            outcome.ElapsedMs,
            outcome.Text,
            usage?.PromptTokens,
            usage?.CompletionTokens,
            usage is not null,
            reserved + grade.JudgeReservedUsd,
            CombineActual(actual, grade.JudgeActualUsd),
            grade);
    }

    private static decimal? CombineActual(decimal? candidate, decimal? judge)
    {
        if (!candidate.HasValue && !judge.HasValue)
        {
            return null;
        }

        return candidate.GetValueOrDefault() + judge.GetValueOrDefault();
    }

    private static UsageEvidence? CombineUsage(List<AttemptObservation> attempts)
    {
        long prompt = 0;
        long completion = 0;
        foreach (var attempt in attempts)
        {
            if (attempt.Usage is null)
            {
                return null;
            }

            prompt += attempt.Usage.PromptTokens ?? 0;
            completion += attempt.Usage.CompletionTokens ?? 0;
        }

        return attempts.Count == 0 ? null : new UsageEvidence(prompt, completion, prompt + completion, null, null);
    }

    private static decimal? CombineActual(List<AttemptObservation> attempts)
    {
        decimal total = 0m;
        var any = false;
        foreach (var attempt in attempts)
        {
            if (attempt.ActualUsd.HasValue)
            {
                total += attempt.ActualUsd.Value;
                any = true;
            }
        }

        return any ? total : null;
    }

    private static BatchEvaluationReport BuildReport(
        CandidateProfile profile,
        CandidateProfile judgeProfile,
        JudgeCalibrationApproval calibration,
        BatchReviewSelection reviewSelection,
        EvaluateBatchOptions selected,
        string batchId,
        string frozenPin,
        string observedSha,
        bool hashVerified,
        bool live,
        bool credentialPresent,
        bool judgeCredentialPresent,
        DateTime started,
        IReadOnlyList<BatchCaseRow> rows,
        EvaluationBudget? budget,
        string? detail)
    {
        var finished = DateTime.UtcNow;
        var aggregates = rows
            .GroupBy(row => $"{row.Family}|{row.Route}", StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => new BatchAggregate(
                group.Key,
                group.Count(),
                group.Count(row => row.Grade.EligibilityMatch),
                group.Count(row => !row.Grade.EligibilityMatch),
                group.Count(row => string.Equals(row.Decision, "Failed", StringComparison.Ordinal)),
                group.Sum(row => row.ProviderDispatches),
                group.Sum(row => row.ReservedUsd)))
            .ToList();
        var failed = rows.Count(row => string.Equals(row.Decision, "Failed", StringComparison.Ordinal)
            || string.Equals(row.Decision, "BudgetDenied", StringComparison.Ordinal)
            || string.Equals(row.Decision, "DeadlineExceeded", StringComparison.Ordinal)
            || string.Equals(row.Decision, "Cancelled", StringComparison.Ordinal));
        var criticals = rows
            .SelectMany(row => row.Grade.CriticalFlags
                .Concat(row.Grade.ModelCriticalErrors)
                .Select(flag => $"{row.CaseId}:{flag}"))
            .ToList();
        var unresolvedGrades = rows.Count(row =>
            string.Equals(row.Decision, "Succeeded", StringComparison.Ordinal)
            && !string.Equals(row.Grade.DimensionStatus, live ? "resolved-model-grade" : "offline-scripted-grade", StringComparison.Ordinal));

        return new BatchEvaluationReport(
            "batch_evaluation_report",
            profile.CandidateId,
            profile.AdapterId,
            profile.Endpoint,
            profile.Model,
            profile.CredentialRef,
            credentialPresent,
            live,
            new BatchCorpusPin(
                batchId,
                BatchCorpus.ExpectedSchemaRevision,
                frozenPin,
                observedSha,
                hashVerified),
            new BatchRevisions(
                System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
                RunnerRevision,
                EligibilityEnvelopeParser.PromptRevision,
                TranslationEnvelopeParser.PromptRevision,
                RewritingEnvelopeParser.PromptRevision,
                profile.ValidatorRevision,
                BatchJudge.JudgeId,
                BatchJudge.GradingPromptRevision,
                BatchJudge.GradingPromptSha256,
                calibration.CalibrationId,
                calibration.CalibrationSha256),
            new BatchPolicySnapshot(
                selected.DeadlineMs,
                5000,
                10000,
                2000),
            new BatchBudgetObservation(
                selected.MaxDispatches,
                selected.MaxSpendUsd,
                budget?.DispatchesUsed ?? rows.Sum(row => row.ProviderDispatches),
                budget?.ReservedUsd ?? rows.Sum(row => row.ReservedUsd),
                budget?.UnresolvedUsd ?? 0m,
                CombineRowActual(rows)),
            new BatchSelection(
                selected.Corpus,
                selected.Profile,
                selected.JudgeProfile,
                selected.Calibration,
                selected.ReviewSeed,
                selected.ServingScope,
                Concurrency: 1),
            new BatchTimestamps(
                started.ToString("o", System.Globalization.CultureInfo.InvariantCulture),
                finished.ToString("o", System.Globalization.CultureInfo.InvariantCulture)),
            ServingScopeDescription(selected),
            new BatchJudgeProfile(
                judgeProfile.CandidateId,
                judgeProfile.AdapterId,
                judgeProfile.Endpoint,
                judgeProfile.Model,
                judgeProfile.CredentialRef,
                judgeCredentialPresent,
                BatchJudge.MaxAttempts,
                calibration),
            reviewSelection,
            [.. rows],
            [.. aggregates],
            new BatchFindings(
                failed,
                rows.Count(row => !row.Grade.EligibilityMatch),
                unresolvedGrades,
                rows.Count(row => row.Grade.Scores is not null && !row.Grade.Usable),
                criticals.AsReadOnly(),
                rows.Count(row => row.Disposition == "live_qualification"),
                rows.Count(row => row.Disposition == "offline_fixture")),
            "pending",
            detail,
            [
                "Bounded-batch evidence only: full-corpus density, full human coverage, API performance and G1 qualification remain pending (successors; Q-001/Q-005).",
                live
                    ? "Qwen model grades are bounded and blinded; human review remains authoritative, and no per-direction 90% qualification is claimed at n=1."
                    : "Scripted judge grades exercise the offline contract only and are not live quality evidence.",
                "Model outputs above cover intentionally authored synthetic fixtures in a designated evaluation artifact; no production text is captured.",
                "Runner response reuse is disabled: every live row dispatches through the production traversal; offline rows are scripted and labeled separately.",
            ]);
    }

    private static BatchEvaluationReport BlockedReport(
        CandidateProfile profile,
        CandidateProfile judgeProfile,
        EvaluateBatchOptions selected,
        string batchId,
        string frozenPin,
        string observedSha,
        string detail,
        BatchReviewSelection? reviewSelection = null) =>
        BuildReport(
            profile,
            judgeProfile,
            OfflineCalibration(),
            reviewSelection ?? EmptyReviewSelection(selected.ReviewSeed),
            selected,
            batchId,
            frozenPin,
            observedSha,
            hashVerified: false,
            live: false,
            credentialPresent: false,
            judgeCredentialPresent: false,
            DateTime.UtcNow,
            [],
            budget: null,
            detail)
        with
        { Status = "blocked" };

    private static BatchReviewSelection EmptyReviewSelection(string? seed) => new(
        seed ?? "unavailable-before-corpus-validation",
        string.Empty,
        "sha256-rank.v1",
        [],
        [],
        IncludesSimplifiedChinese: false,
        IncludesTraditionalChinese: false,
        TranslationCount: 0,
        RewritingCount: 0,
        IncludesShortLengthBand: false,
        IncludesLongLengthBand: false);

    private static string ServingScopeDescription(EvaluateBatchOptions selected) => selected.ServingScope switch
    {
        "quiescent" =>
            "Operator attested that production serving was quiescent for the shared DeepSeek credential during this live run.",
        "shared-concurrent" =>
            "Operator attested that this live run shared the DeepSeek credential concurrently with production serving; report spend covers evaluation dispatches only.",
        _ => "Offline fixture run; no provider serving scope applies.",
    };

    private static decimal? CombineRowActual(IReadOnlyList<BatchCaseRow> rows)
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

    private static int WriteReport(BatchEvaluationReport report, EvaluateBatchOptions selected, int exitCode)
    {
        var payload = JsonSerializer.Serialize(report, OutputOptions) + "\n";
        Console.Out.Write(payload);

        var path = selected.Output ?? DefaultReportPath(report.Live);
        var directory = Path.GetDirectoryName(path);
        if (directory is not null)
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, payload, Encoding.UTF8);
        Console.Error.WriteLine($"Batch evaluation report: {path}");
        return exitCode;
    }

    private static string DefaultReportPath(bool live)
    {
        var root = FindRepositoryRoot() ?? Directory.GetCurrentDirectory();
        var stamp = DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ", System.Globalization.CultureInfo.InvariantCulture);
        return Path.Combine(root, "artifacts", "batch", $"evaluate-batch-{stamp}-{(live ? "live" : "offline")}.json");
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

    private sealed class ScriptedBatchClient(
        string? classificationResponse,
        string? transformationResponse) : IChatClient
    {
        private int stage;

        public int CallCount { get; private set; }

        public void Dispose()
        {
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            stage++;
            var response = stage switch
            {
                1 => classificationResponse,
                2 => transformationResponse,
                _ => null,
            };

            if (response is null)
            {
                return Task.FromException<ChatResponse>(new InvalidOperationException(
                    "A local gate leaked a batch dispatch in the offline evaluation."));
            }

            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, response)));
        }

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("The batch evaluation never streams.");
    }

    private sealed class BatchLiveClient(
        ChatCompletionsAdapter adapter,
        TransportCredential credential,
        EvaluationBudget budget,
        List<AttemptObservation> attempts) : IChatClient, IChainAttemptReporter
    {
        public AttemptObservation? LastAttempt { get; private set; }

        public int CallCount { get; private set; }

        public void Dispose()
        {
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public async Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            var snapshots = messages
                .Select(message => new PromptMessageSnapshot(message.Role.Value, message.Text))
                .ToArray();
            var observation = await adapter.SendAsync(snapshots, credential, budget, cancellationToken)
                .ConfigureAwait(false);
            attempts.Add(observation);
            LastAttempt = observation;
            return new ChatResponse(new ChatMessage(ChatRole.Assistant, observation.ResponseText));
        }

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("The batch evaluation never streams.");
    }

    private sealed record BatchCaseRow(
        [property: JsonPropertyOrder(0)] string CaseId,
        [property: JsonPropertyOrder(1)] string Family,
        [property: JsonPropertyOrder(2)] string Route,
        [property: JsonPropertyOrder(3)] string Disposition,
        [property: JsonPropertyOrder(4)] string Decision,
        [property: JsonPropertyOrder(5)] string? Category,
        [property: JsonPropertyOrder(6)] string? ResolvedLanguage,
        [property: JsonPropertyOrder(7)] string? TargetOrMode,
        [property: JsonPropertyOrder(8)] bool EligibilityMatch,
        [property: JsonPropertyOrder(9)] int Dispatches,
        [property: JsonPropertyOrder(10)] int ProviderDispatches,
        [property: JsonPropertyOrder(11)] long ElapsedMs,
        [property: JsonPropertyOrder(12)] string? OutputText,
        [property: JsonPropertyOrder(13)] long? PromptTokens,
        [property: JsonPropertyOrder(14)] long? CompletionTokens,
        [property: JsonPropertyOrder(15)] bool UsageKnown,
        [property: JsonPropertyOrder(16)] decimal ReservedUsd,
        [property: JsonPropertyOrder(17)] decimal? ActualUsd,
        [property: JsonPropertyOrder(18)] BatchGradeRecord Grade);

    private sealed record BatchCorpusPin(
        [property: JsonPropertyOrder(0)] string BatchId,
        [property: JsonPropertyOrder(1)] string SchemaRevision,
        [property: JsonPropertyOrder(2)] string FrozenSha256,
        [property: JsonPropertyOrder(3)] string ObservedSha256,
        [property: JsonPropertyOrder(4)] bool HashVerified);

    private sealed record BatchRevisions(
        [property: JsonPropertyOrder(0)] string Sdk,
        [property: JsonPropertyOrder(1)] string Runner,
        [property: JsonPropertyOrder(2)] string EligibilityPrompt,
        [property: JsonPropertyOrder(3)] string TranslationPrompt,
        [property: JsonPropertyOrder(4)] string RewritingPrompt,
        [property: JsonPropertyOrder(5)] string Validator,
        [property: JsonPropertyOrder(6)] string Judge,
        [property: JsonPropertyOrder(7)] string GradingPrompt,
        [property: JsonPropertyOrder(8)] string GradingPromptSha256,
        [property: JsonPropertyOrder(9)] string Calibration,
        [property: JsonPropertyOrder(10)] string CalibrationSha256);

    private sealed record BatchPolicySnapshot(
        [property: JsonPropertyOrder(0)] int OverallDeadlineMs,
        [property: JsonPropertyOrder(1)] int EligibilityTimeoutMs,
        [property: JsonPropertyOrder(2)] int TransformationTimeoutMs,
        [property: JsonPropertyOrder(3)] int FinalizationReserveMs);

    private sealed record BatchBudgetObservation(
        [property: JsonPropertyOrder(0)] int MaxDispatches,
        [property: JsonPropertyOrder(1)] decimal MaxSpendUsd,
        [property: JsonPropertyOrder(2)] int DispatchesUsed,
        [property: JsonPropertyOrder(3)] decimal ReservedUsd,
        [property: JsonPropertyOrder(4)] decimal UnresolvedUsd,
        [property: JsonPropertyOrder(5)] decimal? ActualUsd);

    private sealed record BatchSelection(
        [property: JsonPropertyOrder(0)] string Corpus,
        [property: JsonPropertyOrder(1)] string Profile,
        [property: JsonPropertyOrder(2)] string JudgeProfile,
        [property: JsonPropertyOrder(3)] string? Calibration,
        [property: JsonPropertyOrder(4)] string? ReviewSeed,
        [property: JsonPropertyOrder(5)] string? ServingScope,
        [property: JsonPropertyOrder(6)] int Concurrency);

    private sealed record BatchJudgeProfile(
        [property: JsonPropertyOrder(0)] string CandidateId,
        [property: JsonPropertyOrder(1)] string AdapterId,
        [property: JsonPropertyOrder(2)] string Endpoint,
        [property: JsonPropertyOrder(3)] string Model,
        [property: JsonPropertyOrder(4)] string CredentialRef,
        [property: JsonPropertyOrder(5)] bool CredentialPresent,
        [property: JsonPropertyOrder(6)] int MaxAttempts,
        [property: JsonPropertyOrder(7)] JudgeCalibrationApproval Calibration);

    private sealed record BatchTimestamps(
        [property: JsonPropertyOrder(0)] string StartedUtc,
        [property: JsonPropertyOrder(1)] string FinishedUtc);

    private sealed record BatchAggregate(
        [property: JsonPropertyOrder(0)] string Key,
        [property: JsonPropertyOrder(1)] int Total,
        [property: JsonPropertyOrder(2)] int Matched,
        [property: JsonPropertyOrder(3)] int Mismatched,
        [property: JsonPropertyOrder(4)] int Failed,
        [property: JsonPropertyOrder(5)] int Dispatches,
        [property: JsonPropertyOrder(6)] decimal ReservedUsd);

    private sealed record BatchFindings(
        [property: JsonPropertyOrder(0)] int Failed,
        [property: JsonPropertyOrder(1)] int Mismatched,
        [property: JsonPropertyOrder(2)] int UnresolvedGrades,
        [property: JsonPropertyOrder(3)] int UnusableModelGrades,
        [property: JsonPropertyOrder(4)] IReadOnlyList<string> CriticalFlags,
        [property: JsonPropertyOrder(5)] int LiveQualification,
        [property: JsonPropertyOrder(6)] int OfflineFixture);

    private sealed record BatchEvaluationReport(
        [property: JsonPropertyOrder(0)] string Kind,
        [property: JsonPropertyOrder(1)] string CandidateId,
        [property: JsonPropertyOrder(2)] string AdapterId,
        [property: JsonPropertyOrder(3)] string Endpoint,
        [property: JsonPropertyOrder(4)] string Model,
        [property: JsonPropertyOrder(5)] string CredentialRef,
        [property: JsonPropertyOrder(6)] bool CredentialPresent,
        [property: JsonPropertyOrder(7)] bool Live,
        [property: JsonPropertyOrder(8)] BatchCorpusPin Corpus,
        [property: JsonPropertyOrder(9)] BatchRevisions Revisions,
        [property: JsonPropertyOrder(10)] BatchPolicySnapshot Policy,
        [property: JsonPropertyOrder(11)] BatchBudgetObservation Budget,
        [property: JsonPropertyOrder(12)] BatchSelection Selection,
        [property: JsonPropertyOrder(13)] BatchTimestamps Timestamps,
        [property: JsonPropertyOrder(14)] string ServingScope,
        [property: JsonPropertyOrder(15)] BatchJudgeProfile Judge,
        [property: JsonPropertyOrder(16)] BatchReviewSelection ReviewSelection,
        [property: JsonPropertyOrder(17)] IReadOnlyList<BatchCaseRow> Cases,
        [property: JsonPropertyOrder(18)] IReadOnlyList<BatchAggregate> Aggregates,
        [property: JsonPropertyOrder(19)] BatchFindings Findings,
        [property: JsonPropertyOrder(20)] string Status,
        [property: JsonPropertyOrder(21)] string? Detail,
        [property: JsonPropertyOrder(22)] IReadOnlyList<string> Limitations);

    private sealed record EvaluateBatchOptions(
        bool Live,
        string Corpus,
        string Profile,
        string JudgeProfile,
        string? Calibration,
        string? ReviewSeed,
        string? ServingScope,
        int MaxDispatches,
        decimal MaxSpendUsd,
        int DeadlineMs,
        string? Output)
    {
        public static bool TryParse(string[] options, out EvaluateBatchOptions selected, out string error)
        {
            var live = false;
            var offline = false;
            string? corpus = null;
            string? profile = null;
            string? judgeProfile = null;
            string? calibration = null;
            string? reviewSeed = null;
            string? servingScope = null;
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
                    case "--corpus" when index + 1 < options.Length:
                        corpus = options[index + 1];
                        index++;
                        break;
                    case "--profile" when index + 1 < options.Length:
                        profile = options[index + 1];
                        index++;
                        break;
                    case "--judge-profile" when index + 1 < options.Length:
                        judgeProfile = options[index + 1];
                        index++;
                        break;
                    case "--calibration" when index + 1 < options.Length:
                        calibration = options[index + 1];
                        index++;
                        break;
                    case "--review-seed" when index + 1 < options.Length:
                        reviewSeed = options[index + 1];
                        index++;
                        break;
                    case "--serving-scope" when index + 1 < options.Length:
                        servingScope = options[index + 1];
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
                        error = $"Unknown or incomplete evaluate-batch option '{options[index]}'.";
                        return false;
                }
            }

            if (live && offline)
            {
                selected = null!;
                error = "evaluate-batch accepts at most one of --live / --offline.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(corpus))
            {
                selected = null!;
                error = "evaluate-batch requires --corpus <path> to the frozen batch file.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(profile))
            {
                selected = null!;
                error = "evaluate-batch requires --profile <candidate-id>.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(judgeProfile))
            {
                selected = null!;
                error = "evaluate-batch requires --judge-profile Qwen-Qwen3.8-Flash.";
                return false;
            }

            if (live && string.IsNullOrWhiteSpace(calibration))
            {
                selected = null!;
                error = "live evaluate-batch requires --calibration <path> to an accepted independently labeled calibration record.";
                return false;
            }

            if (live && string.IsNullOrWhiteSpace(reviewSeed))
            {
                selected = null!;
                error = "live evaluate-batch requires --review-seed <value> selected before outputs are visible.";
                return false;
            }

            if (live
                && !string.Equals(servingScope, "quiescent", StringComparison.Ordinal)
                && !string.Equals(servingScope, "shared-concurrent", StringComparison.Ordinal))
            {
                selected = null!;
                error = "live evaluate-batch requires --serving-scope <quiescent|shared-concurrent>.";
                return false;
            }

            if (!live && (!string.IsNullOrWhiteSpace(reviewSeed) || !string.IsNullOrWhiteSpace(servingScope)))
            {
                selected = null!;
                error = "--review-seed and --serving-scope are live-run attestations and require --live.";
                return false;
            }

            if (maxDispatches <= 0 || maxSpend <= 0)
            {
                selected = null!;
                error = "evaluate-batch requires a positive finite --max-dispatches and --max-spend-usd budget.";
                return false;
            }

            if (deadlineMs <= 0)
            {
                selected = null!;
                error = "evaluate-batch requires a positive finite --deadline-ms overall deadline.";
                return false;
            }

            selected = new EvaluateBatchOptions(
                live, corpus, profile, judgeProfile, calibration, reviewSeed, servingScope,
                maxDispatches, maxSpend, deadlineMs, output);
            error = string.Empty;
            return true;
        }
    }
}
