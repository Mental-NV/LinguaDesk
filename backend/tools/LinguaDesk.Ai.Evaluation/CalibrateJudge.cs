using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using LinguaDesk.Infrastructure.Ai;
using Microsoft.Extensions.AI;

namespace LinguaDesk.Ai.Evaluation;

// Builds the judge-calibration work pack: runs the candidate chain and the
// judge grading over a development corpus, then writes blind review materials.
// Reviewers label outputs WITHOUT seeing judge scores; a separate finalize
// step merges human labels into the accepted calibration record.
internal static class CalibrateJudge
{
    private const string CredentialPrefix = "LINGUADESK_AIEVALUATION__CREDENTIALS__";

    private const string CredentialSuffix = "__APIKEY";

    internal static async Task<int> RunAsync(string[] options)
    {
        if (!CalibrateJudgeOptions.TryParse(options, out var selected, out var error))
        {
            Console.Error.WriteLine(error);
            return 2;
        }

        DevCorpusDocument document;
        try
        {
            document = DevCorpus.LoadFile(selected.DevCorpus);
        }
        catch (DevCorpusException exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 2;
        }

        var devCases = document.Cases;
        var devCorpusSha = document.Sha256;

        CandidateProfile profile;
        CandidateProfile judgeProfile;
        try
        {
            profile = CandidateRegistry.Select(CandidateRegistry.Default, selected.CandidateProfile);
            judgeProfile = CandidateRegistry.Select(CandidateRegistry.Default, selected.JudgeProfile);
        }
        catch (CandidateProfileException exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 2;
        }

        TransportCredential? credential = null;
        TransportCredential? judgeCredential = null;
        if (selected.Live)
        {
            if (!TryResolveCredential(profile.CredentialRef, out credential) || credential is null
                || !TryResolveCredential(judgeProfile.CredentialRef, out judgeCredential) || judgeCredential is null)
            {
                Console.Error.WriteLine(
                    "The candidate or judge credential is missing or blank; no provider dispatch was made.");
                return 3;
            }
        }

        var started = DateTime.UtcNow;
        var budget = new EvaluationBudget(selected.MaxDispatches, selected.MaxSpendUsd);
        var rows = new List<CalibrateRow>();
        var halted = false;
        foreach (var devCase in devCases)
        {
            var row = selected.Live
                ? await RunLiveCaseAsync(devCase, devCorpusSha, profile, judgeProfile, credential!, judgeCredential!, budget, selected).ConfigureAwait(false)
                : RunOfflineCase(devCase, devCorpusSha, profile, judgeProfile, selected);
            rows.Add(row);
            if (row.Decision == "BudgetDenied" || row.DimensionStatus.Contains("budget-denied", StringComparison.Ordinal))
            {
                halted = true;
                break;
            }
        }

        WritePack(selected.OutputDir, devCorpusSha, profile, judgeProfile, selected, started, budget, halted, rows);

        var clean = rows.Count == devCases.Count
            && rows.All(row => string.Equals(row.Decision, "Succeeded", StringComparison.Ordinal) && row.Usable);
        Console.WriteLine($"Calibration draft: {selected.OutputDir} ({rows.Count}/{devCases.Count} rows).");
        return clean ? 0 : 1;
    }

    private static async Task<CalibrateRow> RunLiveCaseAsync(
        DevCase devCase,
        string devCorpusSha,
        CandidateProfile profile,
        CandidateProfile judgeProfile,
        TransportCredential credential,
        TransportCredential judgeCredential,
        EvaluationBudget budget,
        CalibrateJudgeOptions selected)
    {
        var kase = devCase.ToBatchCase();
        var chain = FamilyChain.Create(
            string.Equals(kase.Family, "rewriting", StringComparison.Ordinal)
                ? ChainFamily.Rewriting
                : ChainFamily.Translation,
            CandidateRegistry.Default,
            profile.CandidateId);
        var policy = selected.ToChainPolicy();
        policy.Validate();

        using var candidateHttpClient = new HttpClient();
        using var judgeHttpClient = new HttpClient();
        var candidateAdapter = new ChatCompletionsAdapter(profile, candidateHttpClient);
        var judgeAdapter = new ChatCompletionsAdapter(judgeProfile, judgeHttpClient);
        var attempts = new List<AttemptObservation>();
        ChainOutcome outcome = string.Equals(kase.Family, "rewriting", StringComparison.Ordinal)
            ? await ChainOrchestrator.ExecuteRewritingAsync(
                chain,
                new RewritingInput(kase.SourceText, kase.Language ?? kase.SourceSelection, kase.Mode),
                _ => new CalibrateLiveClient(candidateAdapter, credential, budget, attempts),
                policy,
                budget).ConfigureAwait(false)
            : await ChainOrchestrator.ExecuteTranslationAsync(
                chain,
                new TranslationInput(kase.SourceText, kase.SourceSelection, kase.Target),
                _ => new CalibrateLiveClient(candidateAdapter, credential, budget, attempts),
                policy,
                budget).ConfigureAwait(false);
        var grade = await BatchJudge.GradeAsync(
            kase,
            outcome.Decision.ToString(),
            outcome.Category,
            outcome.Text,
            judgeProfile,
            "calibration-draft",
            devCorpusSha,
            live: true,
            async (messages, cancellationToken) =>
            {
                var before = judgeAdapter.DispatchCount;
                try
                {
                    var observation = await judgeAdapter.SendAsync(
                        messages, judgeCredential, budget, cancellationToken).ConfigureAwait(false);
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
        return CalibrateRow.From(devCase, outcome, attempts, grade);
    }

    private static CalibrateRow RunOfflineCase(
        DevCase devCase,
        string devCorpusSha,
        CandidateProfile profile,
        CandidateProfile judgeProfile,
        CalibrateJudgeOptions selected)
    {
        var kase = devCase.ToBatchCase();
        var language = kase.SourceSelection ?? kase.Language ?? "en";
        using var client = new CalibrateScriptedClient(
            $"{{\"status\":\"eligible\",\"language\":\"{language}\"}}",
            $"{{\"status\":\"result\",\"text\":\"Scripted offline output for {kase.Id}.\"}}");
        ChainOutcome outcome = string.Equals(kase.Family, "rewriting", StringComparison.Ordinal)
            ? ChainOrchestrator.ExecuteRewritingAsync(
                FamilyChain.Create(ChainFamily.Rewriting, CandidateRegistry.Default, profile.CandidateId),
                new RewritingInput(kase.SourceText, kase.Language ?? kase.SourceSelection, kase.Mode),
                _ => client,
                OfflinePolicy(selected)).GetAwaiter().GetResult()
            : ChainOrchestrator.ExecuteTranslationAsync(
                FamilyChain.Create(ChainFamily.Translation, CandidateRegistry.Default, profile.CandidateId),
                new TranslationInput(kase.SourceText, kase.SourceSelection, kase.Target),
                _ => client,
                OfflinePolicy(selected)).GetAwaiter().GetResult();
        var grade = BatchJudge.GradeAsync(
            kase,
            outcome.Decision.ToString(),
            outcome.Category,
            outcome.Text,
            judgeProfile,
            "calibration-draft",
            devCorpusSha,
            live: false,
            (_, _) => Task.FromResult(new BatchJudgeAttemptResult(
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
                FailureCategory: null))).GetAwaiter().GetResult();
        return CalibrateRow.From(devCase, outcome, [], grade);
    }

    private static ChainPolicy OfflinePolicy(CalibrateJudgeOptions selected) => selected.ToChainPolicy();

    private static void WritePack(
        string outputDir,
        string devCorpusSha,
        CandidateProfile profile,
        CandidateProfile judgeProfile,
        CalibrateJudgeOptions selected,
        DateTime started,
        EvaluationBudget budget,
        bool halted,
        IReadOnlyList<CalibrateRow> rows)
    {
        Directory.CreateDirectory(outputDir);
        // Relaxed escaping keeps Cyrillic/CJK readable for human reviewers.
        // Hashes are computed over the in-memory text, so file escaping
        // never affects source/output identity.
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };
        File.WriteAllText(
            Path.Combine(outputDir, "calibration-draft.json"),
            JsonSerializer.Serialize(
                new CalibrateDraftReport(
                    "calibration_draft_report",
                    devCorpusSha,
                    profile.CandidateId,
                    judgeProfile.CandidateId,
                    started,
                    DateTime.UtcNow,
                    halted,
                    budget.DispatchesUsed,
                    budget.ReservedUsd,
                    budget.UnresolvedUsd,
                    rows,
                    selected.ToPolicySnapshot()),
                options));
        File.WriteAllText(
            Path.Combine(outputDir, "review-pack.json"),
            JsonSerializer.Serialize(
                new CalibrateReviewPack(
                    devCorpusSha,
                    rows.Select(row => new CalibrateReviewCase(
                        row.CaseId, row.Language, row.Family, row.Route, row.Source, row.Output)).ToList()),
                options));
        File.WriteAllText(
            Path.Combine(outputDir, "human-labels.template.json"),
            JsonSerializer.Serialize(
                rows.Select(row => new CalibrateHumanLabel(
                    row.CaseId,
                    ReviewerId: string.Empty,
                    new CalibrateScores(0, 0, 0, 0),
                    [])).ToList(),
                options));
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

    private sealed class CalibrateLiveClient(
        ChatCompletionsAdapter adapter,
        TransportCredential credential,
        EvaluationBudget budget,
        List<AttemptObservation> attempts) : IChatClient
    {
        public void Dispose()
        {
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public async Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            var snapshots = messages
                .Select(message => new PromptMessageSnapshot(message.Role.Value, message.Text))
                .ToArray();
            var observation = await adapter.SendAsync(snapshots, credential, budget, cancellationToken)
                .ConfigureAwait(false);
            attempts.Add(observation);
            return new ChatResponse(new ChatMessage(ChatRole.Assistant, observation.ResponseText));
        }

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("The calibration run never streams.");
    }

    private sealed class CalibrateScriptedClient(
        string? classificationResponse,
        string? transformationResponse) : IChatClient
    {
        private int stage;

        public void Dispose()
        {
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
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
                    "A local gate leaked a calibration dispatch in the offline run."));
            }

            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, response)));
        }

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("The calibration run never streams.");
    }

    private const int DefaultEligibilityTimeoutMs = 5000;

    private const int DefaultTransformationTimeoutMs = 10000;

    private const int FinalizationReserveMs = 2000;

    private sealed record CalibrateJudgeOptions(
        bool Live,
        string DevCorpus,
        string CandidateProfile,
        string JudgeProfile,
        int MaxDispatches,
        decimal MaxSpendUsd,
        int DeadlineMs,
        int EligibilityTimeoutMs,
        int TransformationTimeoutMs,
        string OutputDir)
    {
        internal ChainPolicy ToChainPolicy() => new(
            TimeSpan.FromMilliseconds(DeadlineMs),
            TimeSpan.FromMilliseconds(EligibilityTimeoutMs),
            TimeSpan.FromMilliseconds(TransformationTimeoutMs),
            TimeSpan.FromMilliseconds(FinalizationReserveMs),
            SystemChainClock.Instance);

        internal CalibratePolicySnapshot ToPolicySnapshot() => new(
            DeadlineMs,
            EligibilityTimeoutMs,
            TransformationTimeoutMs,
            FinalizationReserveMs);

        internal static bool TryParse(string[] options, out CalibrateJudgeOptions selected, out string error)
        {
            var live = false;
            var offline = false;
            string? devCorpus = null;
            string? candidateProfile = null;
            string? judgeProfile = null;
            string? outputDir = null;
            int maxDispatches = 0;
            decimal maxSpend = 0m;
            int deadlineMs = 30000;
            int eligibilityTimeoutMs = DefaultEligibilityTimeoutMs;
            int transformationTimeoutMs = DefaultTransformationTimeoutMs;

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
                    case "--dev-corpus" when index + 1 < options.Length:
                        devCorpus = options[++index];
                        break;
                    case "--candidate-profile" when index + 1 < options.Length:
                        candidateProfile = options[++index];
                        break;
                    case "--judge-profile" when index + 1 < options.Length:
                        judgeProfile = options[++index];
                        break;
                    case "--output-dir" when index + 1 < options.Length:
                        outputDir = options[++index];
                        break;
                    case "--max-dispatches" when index + 1 < options.Length
                        && int.TryParse(options[++index], out var parsedDispatches):
                        maxDispatches = parsedDispatches;
                        break;
                    case "--max-spend-usd" when index + 1 < options.Length
                        && decimal.TryParse(
                            options[++index],
                            System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture,
                            out var parsedSpend):
                        maxSpend = parsedSpend;
                        break;
                    case "--deadline-ms" when index + 1 < options.Length
                        && int.TryParse(options[++index], out var deadline):
                        deadlineMs = deadline;
                        break;
                    case "--eligibility-timeout-ms" when index + 1 < options.Length
                        && int.TryParse(options[++index], out var eligibilityTimeout):
                        eligibilityTimeoutMs = eligibilityTimeout;
                        break;
                    case "--transformation-timeout-ms" when index + 1 < options.Length
                        && int.TryParse(options[++index], out var transformationTimeout):
                        transformationTimeoutMs = transformationTimeout;
                        break;
                    default:
                        selected = null!;
                        error = $"Unknown calibrate-judge option '{options[index]}'.";
                        return false;
                }
            }

            if (live == offline)
            {
                selected = null!;
                error = "calibrate-judge requires exactly one of --live or --offline.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(devCorpus)
                || string.IsNullOrWhiteSpace(outputDir))
            {
                selected = null!;
                error = "calibrate-judge requires --dev-corpus <path> and --output-dir <dir>.";
                return false;
            }

            if (maxDispatches <= 0 || maxSpend <= 0m)
            {
                selected = null!;
                error = "calibrate-judge requires a positive finite --max-dispatches and --max-spend-usd budget.";
                return false;
            }

            if (deadlineMs <= 0)
            {
                selected = null!;
                error = "calibrate-judge requires a positive finite --deadline-ms overall deadline.";
                return false;
            }

            // The defaults mirror the release chain policy (5s/10s/2s reserve);
            // non-default stage timeouts diverge from release conditions and
            // are recorded in the draft report policy snapshot.
            if (eligibilityTimeoutMs <= 0 || transformationTimeoutMs <= 0)
            {
                selected = null!;
                error = "calibrate-judge requires positive --eligibility-timeout-ms and --transformation-timeout-ms stage timeouts.";
                return false;
            }

            if (eligibilityTimeoutMs + FinalizationReserveMs > deadlineMs
                || transformationTimeoutMs + FinalizationReserveMs > deadlineMs)
            {
                selected = null!;
                error = "calibrate-judge requires each stage timeout plus the 2000ms finalization reserve to fit inside --deadline-ms.";
                return false;
            }

            selected = new CalibrateJudgeOptions(
                live,
                devCorpus,
                string.IsNullOrWhiteSpace(candidateProfile) ? "DeepSeek-V4.1-Flash" : candidateProfile,
                string.IsNullOrWhiteSpace(judgeProfile) ? "Qwen-Qwen3.8-Flash" : judgeProfile,
                maxDispatches,
                maxSpend,
                deadlineMs,
                eligibilityTimeoutMs,
                transformationTimeoutMs,
                outputDir);
            error = string.Empty;
            return true;
        }
    }

    private sealed record DevCase(
        string Id,
        string Family,
        string Language,
        string Source,
        string? SourceSelection,
        string? Target,
        string? RewriteLanguage,
        string? Mode)
    {
        internal BatchCase ToBatchCase() => new(
            Id,
            Family,
            Source,
            SourceSelection,
            Target,
            RewriteLanguage,
            Mode,
            "eligible",
            [],
            string.Empty,
            ["calibration-dev"],
            ChineseScript: null);
    }

    private static class DevCorpus
    {
        internal static DevCorpusDocument LoadFile(string path)
        {
            string raw;
            try
            {
                raw = File.ReadAllText(path);
            }
            catch (IOException exception)
            {
                throw new DevCorpusException($"The development corpus could not be read: {exception.Message}");
            }

            JsonDocument document;
            try
            {
                document = JsonDocument.Parse(raw);
            }
            catch (JsonException exception)
            {
                throw new DevCorpusException($"The development corpus is not valid JSON: {exception.Message}");
            }

            using (document)
            {
                if (!document.RootElement.TryGetProperty("cases", out var cases)
                    || cases.ValueKind != JsonValueKind.Array)
                {
                    throw new DevCorpusException("The development corpus has no 'cases' array.");
                }

                var parsed = new List<DevCase>();
                var ids = new HashSet<string>(StringComparer.Ordinal);
                foreach (var kase in cases.EnumerateArray())
                {
                    parsed.Add(ParseCase(kase, ids));
                }

                if (parsed.Count == 0)
                {
                    throw new DevCorpusException("The development corpus holds no cases.");
                }

                return new DevCorpusDocument(
                    Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(raw))),
                    parsed);
            }
        }

        private static DevCase ParseCase(JsonElement kase, HashSet<string> ids)
        {
            var id = RequiredString(kase, "id");
            if (!ids.Add(id))
            {
                throw new DevCorpusException($"Development case ID '{id}' is duplicated.");
            }

            var family = RequiredString(kase, "family");
            var language = RequiredString(kase, "language");
            if (language is not ("en" or "ru" or "ro" or "zh"))
            {
                throw new DevCorpusException($"Development case '{id}' has an unsupported language '{language}'.");
            }

            var source = RequiredString(kase, "source");
            if (string.IsNullOrWhiteSpace(source))
            {
                throw new DevCorpusException($"Development case '{id}' has a blank source.");
            }

            if (!kase.TryGetProperty("operation", out var operation)
                || operation.ValueKind != JsonValueKind.Object)
            {
                throw new DevCorpusException($"Development case '{id}' has no operation object.");
            }

            string? sourceSelection = null;
            string? target = null;
            string? rewriteLanguage = null;
            string? mode = null;
            if (string.Equals(family, "rewriting", StringComparison.Ordinal))
            {
                rewriteLanguage = OptionalString(operation, "language");
                mode = OptionalString(operation, "mode");
                if (string.IsNullOrWhiteSpace(rewriteLanguage) || string.IsNullOrWhiteSpace(mode))
                {
                    throw new DevCorpusException($"Development case '{id}' needs operation.language and operation.mode.");
                }
            }
            else if (string.Equals(family, "translation", StringComparison.Ordinal))
            {
                sourceSelection = OptionalString(operation, "sourceSelection");
                target = OptionalString(operation, "target");
                if (string.IsNullOrWhiteSpace(sourceSelection) || string.IsNullOrWhiteSpace(target))
                {
                    throw new DevCorpusException($"Development case '{id}' needs operation.sourceSelection and operation.target.");
                }
            }
            else
            {
                throw new DevCorpusException($"Development case '{id}' has an unsupported family '{family}'.");
            }

            return new DevCase(id, family, language, source, sourceSelection, target, rewriteLanguage, mode);
        }

        private static string RequiredString(JsonElement element, string property)
        {
            if (element.TryGetProperty(property, out var value)
                && value.ValueKind == JsonValueKind.String
                && !string.IsNullOrWhiteSpace(value.GetString()))
            {
                return value.GetString()!;
            }

            throw new DevCorpusException($"Development entry is missing required string '{property}'.");
        }

        private static string? OptionalString(JsonElement element, string property) =>
            element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;
    }

    private sealed record DevCorpusDocument(string Sha256, List<DevCase> Cases);

    private sealed class DevCorpusException(string message) : InvalidOperationException(message);

    private sealed record CalibrateRow(
        string CaseId,
        string Language,
        string Family,
        string Route,
        string Source,
        string SourceSha256,
        string? Output,
        string? OutputSha256,
        string Decision,
        string? Category,
        int CandidateDispatches,
        int JudgeAttempts,
        int JudgeDispatches,
        BatchDimensionScores? JudgeScores,
        IReadOnlyList<string> JudgeCriticalErrors,
        string DimensionStatus,
        bool Usable)
    {
        internal static CalibrateRow From(
            DevCase devCase,
            ChainOutcome outcome,
            IReadOnlyList<AttemptObservation> attempts,
            BatchGradeRecord grade)
        {
            var output = outcome.Text;
            return new CalibrateRow(
                devCase.Id,
                devCase.Language,
                devCase.Family,
                string.Equals(devCase.Family, "rewriting", StringComparison.Ordinal)
                    ? $"{devCase.Language}:{devCase.Mode}"
                    : $"{devCase.SourceSelection}->{devCase.Target}",
                devCase.Source,
                Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(devCase.Source))),
                output,
                output is null
                    ? null
                    : Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(output))),
                outcome.Decision.ToString(),
                outcome.Category,
                outcome.Dispatches,
                grade.JudgeAttempts,
                grade.JudgeDispatches,
                grade.Scores,
                grade.ModelCriticalErrors,
                grade.DimensionStatus,
                string.Equals(outcome.Decision.ToString(), "Succeeded", StringComparison.Ordinal) && grade.Usable);
        }
    }

    private sealed record CalibrateDraftReport(
        string Kind,
        string DevCorpusSha256,
        string CandidateId,
        string JudgeCandidateId,
        DateTime StartedUtc,
        DateTime FinishedUtc,
        bool BudgetHalted,
        int DispatchesUsed,
        decimal ReservedUsd,
        decimal UnresolvedUsd,
        IReadOnlyList<CalibrateRow> Cases,
        CalibratePolicySnapshot Policy);

    private sealed record CalibratePolicySnapshot(
        int OverallDeadlineMs,
        int EligibilityTimeoutMs,
        int TransformationTimeoutMs,
        int FinalizationReserveMs);

    private sealed record CalibrateReviewCase(
        string CaseId,
        string Language,
        string Family,
        string Route,
        string Source,
        string? Output);

    private sealed record CalibrateReviewPack(
        string DevCorpusSha256,
        IReadOnlyList<CalibrateReviewCase> Cases);

    private sealed record CalibrateScores(
        int MeaningFidelity,
        int GrammarNaturalness,
        int StyleOrMode,
        int LanguageAndFormat);

    private sealed record CalibrateHumanLabel(
        string CaseId,
        string ReviewerId,
        CalibrateScores HumanScores,
        IReadOnlyList<string> HumanCriticalErrors);
}
