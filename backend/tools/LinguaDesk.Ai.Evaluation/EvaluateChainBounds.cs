using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using LinguaDesk.Core;
using LinguaDesk.Infrastructure.Ai;
using Microsoft.Extensions.AI;

namespace LinguaDesk.Ai.Evaluation;

internal static class EvaluateChainBounds
{
    private const string CredentialPrefix = "LINGUADESK_AIEVALUATION__CREDENTIALS__";

    private const string CredentialSuffix = "__APIKEY";

    private static readonly JsonSerializerOptions OutputOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    private static readonly IReadOnlyList<ChainBoundsCase> Cases =
    [
        new("cb-tr-en-ru", ChainFamily.Translation, "Please send the report tomorrow.", "en", "ru", null,
            "{\"status\":\"eligible\",\"language\":\"en\"}",
            "{\"status\":\"result\",\"text\":\"Пожалуйста, пришлите отчёт завтра.\"}"),
        new("cb-tr-ru-en", ChainFamily.Translation, "Пожалуйста, пришлите отчёт завтра.", "ru", "en", null,
            "{\"status\":\"eligible\",\"language\":\"ru\"}",
            "{\"status\":\"result\",\"text\":\"Please send the report tomorrow.\"}"),
        new("cb-tr-zh-hans-en", ChainFamily.Translation, "请明天发送报告。", "zh", "en", null,
            "{\"status\":\"eligible\",\"language\":\"zh\"}",
            "{\"status\":\"result\",\"text\":\"Please send the report tomorrow.\"}"),
        new("cb-tr-zh-hant-ru", ChainFamily.Translation, "請明天發送報告。", "zh", "ru", null,
            "{\"status\":\"eligible\",\"language\":\"zh\"}",
            "{\"status\":\"result\",\"text\":\"Пожалуйста, пришлите отчёт завтра.\"}"),
        new("cb-tr-ro-zh", ChainFamily.Translation, "Vă rog să trimiteți raportul mâine.", "ro", "zh", null,
            "{\"status\":\"eligible\",\"language\":\"ro\"}",
            "{\"status\":\"result\",\"text\":\"请明天发送报告。\"}"),
        new("cb-rw-en-simple", ChainFamily.Rewriting, "Please send the report tomorrow.", "en", null, "simple",
            "{\"status\":\"eligible\",\"language\":\"en\"}",
            "{\"status\":\"result\",\"text\":\"Rewritten synthetic text.\"}"),
        new("cb-rw-ru-business", ChainFamily.Rewriting, "Пожалуйста, пришлите отчёт завтра.", "ru", null, "business",
            "{\"status\":\"eligible\",\"language\":\"ru\"}",
            "{\"status\":\"result\",\"text\":\"Rewritten synthetic text.\"}"),
        new("cb-rw-ro-casual", ChainFamily.Rewriting, "Vă rog să trimiteți raportul mâine.", "ro", null, "casual",
            "{\"status\":\"eligible\",\"language\":\"ro\"}",
            "{\"status\":\"result\",\"text\":\"Rewritten synthetic text.\"}"),
        new("cb-rw-zh-correction", ChainFamily.Rewriting, "请明天发送报告。", "zh", null, "correctionOnly",
            "{\"status\":\"eligible\",\"language\":\"zh\"}",
            "{\"status\":\"result\",\"text\":\"Rewritten synthetic text.\"}"),
    ];

    internal static async Task<int> RunAsync(string[] options)
    {
        if (!EvaluateChainBoundsOptions.TryParse(options, out var selected, out var error))
        {
            Console.Error.WriteLine(error);
            Console.Error.WriteLine(
                "Usage: dotnet LinguaDesk.Ai.Evaluation.dll evaluate-chain-bounds [--offline|--live] " +
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

        if (!selected.Live)
        {
            return RunOfflineAsync(profile, policy, selected);
        }

        return await RunLiveAsync(profile, policy, selected).ConfigureAwait(false);
    }

    private static int RunOfflineAsync(
        CandidateProfile profile,
        ChainPolicy policy,
        EvaluateChainBoundsOptions selected)
    {
        var budget = new EvaluationBudget(selected.MaxDispatches, selected.MaxSpendUsd);
        var observations = new List<ChainBoundsCaseObservation>();

        foreach (var kase in Cases)
        {
            var chain = kase.Family == ChainFamily.Translation
                ? FamilyChain.Create(ChainFamily.Translation, CandidateRegistry.Default, profile.CandidateId)
                : FamilyChain.Create(ChainFamily.Rewriting, CandidateRegistry.Default, profile.CandidateId);
            using var client = new ScriptedChainBoundsClient(
                kase.ScriptedClassification, kase.ScriptedTransformation);
            var outcome = kase.Family == ChainFamily.Translation
                ? ChainOrchestrator.ExecuteTranslationAsync(
                    chain, kase.ToTranslationInput(), _ => client, policy, budget).GetAwaiter().GetResult()
                : ChainOrchestrator.ExecuteRewritingAsync(
                    chain, kase.ToRewritingInput(), _ => client, policy, budget).GetAwaiter().GetResult();
            observations.Add(Observe(kase, outcome, null));
        }

        var mismatches = observations.Count(observation => !observation.MatchesReference);
        var report = new ChainBoundsReport(
            "chain_bounds_report",
            profile.CandidateId,
            profile.CredentialRef,
            CredentialPresent: false,
            Live: false,
            new ChainSnapshot(profile.CandidateId, null),
            SnapPolicy(policy),
            new BudgetObservation(
                selected.MaxDispatches, selected.MaxSpendUsd,
                budget.DispatchesUsed, budget.ReservedUsd, budget.UnresolvedUsd),
            [.. observations],
            mismatches,
            mismatches,
            mismatches == 0 ? "pass" : "fail",
            mismatches == 0 ? null : $"{mismatches} offline case(s) disagree with the allowlisted reference.");

        return WriteReport(report, selected, mismatches == 0 ? 0 : 1);
    }

    private static async Task<int> RunLiveAsync(
        CandidateProfile profile,
        ChainPolicy policy,
        EvaluateChainBoundsOptions selected)
    {
        if (!TryResolveCredential(profile.CredentialRef, out var credential) || credential is null)
        {
            var blocked = new ChainBoundsReport(
                "chain_bounds_report",
                profile.CandidateId,
                profile.CredentialRef,
                CredentialPresent: false,
                Live: false,
                new ChainSnapshot(profile.CandidateId, null),
                SnapPolicy(policy),
                new BudgetObservation(selected.MaxDispatches, selected.MaxSpendUsd, 0, 0m, 0m),
                [],
                0,
                0,
                "blocked",
                "The evaluation credential is missing or blank; no provider dispatch was made.");
            return WriteReport(blocked, selected, 3);
        }

        using (credential)
        {
            var budget = new EvaluationBudget(selected.MaxDispatches, selected.MaxSpendUsd);
            using var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(profile.Bounds.AttemptTimeoutSeconds),
            };
            var adapter = new ChatCompletionsAdapter(profile, httpClient);
            var observations = new List<ChainBoundsCaseObservation>();

            foreach (var kase in Cases)
            {
                var chain = kase.Family == ChainFamily.Translation
                    ? FamilyChain.Create(ChainFamily.Translation, CandidateRegistry.Default, profile.CandidateId)
                    : FamilyChain.Create(ChainFamily.Rewriting, CandidateRegistry.Default, profile.CandidateId);
                var attempts = new List<AttemptObservation>();
                using var client = new ChainLiveClient(adapter, credential, attempts);
                var outcome = kase.Family == ChainFamily.Translation
                    ? await ChainOrchestrator.ExecuteTranslationAsync(
                        chain, kase.ToTranslationInput(), _ => client, policy, budget).ConfigureAwait(false)
                    : await ChainOrchestrator.ExecuteRewritingAsync(
                        chain, kase.ToRewritingInput(), _ => client, policy, budget).ConfigureAwait(false);

                observations.Add(Observe(kase, outcome, attempts));
                if (outcome.Decision == ChainDecision.BudgetDenied)
                {
                    break;
                }
            }

            var failed = observations.Count(observation =>
                !string.Equals(observation.Decision, nameof(ChainDecision.Succeeded), StringComparison.Ordinal));
            var mismatches = observations.Count(observation => !observation.MatchesReference);
            var report = new ChainBoundsReport(
                "chain_bounds_report",
                profile.CandidateId,
                profile.CredentialRef,
                CredentialPresent: true,
                Live: true,
                new ChainSnapshot(profile.CandidateId, null),
                SnapPolicy(policy),
                new BudgetObservation(
                    selected.MaxDispatches, selected.MaxSpendUsd,
                    budget.DispatchesUsed, budget.ReservedUsd, budget.UnresolvedUsd),
                [.. observations],
                failed,
                mismatches,
                failed == 0 ? "success" : "fail",
                failed == 0
                    ? (mismatches == 0
                        ? null
                        : $"{mismatches} live case(s) disagree with the development reference; see per-case rows.")
                    : $"{failed} live case(s) did not succeed; failures are retained per case with usage/exposure metadata.");

            return WriteReport(report, selected, failed == 0 ? 0 : 1);
        }
    }

    private static ChainBoundsCaseObservation Observe(
        ChainBoundsCase kase,
        ChainOutcome outcome,
        List<AttemptObservation>? liveAttempts)
    {
        UsageEvidence? usage = null;
        decimal reserved = 0m;
        decimal? actual = null;
        if (liveAttempts is not null)
        {
            long prompt = 0;
            long completion = 0;
            var usageKnown = liveAttempts.Count > 0;
            decimal actualTotal = 0m;
            var anyActual = false;
            foreach (var attempt in liveAttempts)
            {
                reserved += attempt.ReservedUsd;
                if (attempt.Usage is null)
                {
                    usageKnown = false;
                }
                else
                {
                    prompt += attempt.Usage.PromptTokens ?? 0;
                    completion += attempt.Usage.CompletionTokens ?? 0;
                }

                if (attempt.ActualUsd.HasValue)
                {
                    actualTotal += attempt.ActualUsd.Value;
                    anyActual = true;
                }
            }

            usage = usageKnown ? new UsageEvidence(prompt, completion, prompt + completion, null, null) : null;
            actual = anyActual ? actualTotal : null;
        }
        else
        {
            reserved = outcome.Attempts.Sum(static attempt => attempt.ReservedUsd);
        }

        return new ChainBoundsCaseObservation(
            kase.CaseId,
            kase.Family.ToString(),
            kase.DirectionOrCell,
            kase.Source.Length,
            kase.SourceSha256,
            outcome.Decision.ToString(),
            outcome.Category,
            outcome.ResolvedLanguage,
            outcome.TargetOrMode,
            outcome.Dispatches,
            outcome.ChargesCharacters,
            outcome.Text?.Length ?? 0,
            [.. outcome.Attempts.Select(static attempt => new ChainAttemptObservation(
                attempt.CandidateId,
                attempt.CredentialRef,
                attempt.Stage,
                attempt.FinishCategory,
                attempt.StageDispatches,
                attempt.ReservedUsd,
                attempt.ActualUsd,
                attempt.ElapsedMs))],
            usage?.PromptTokens,
            usage?.CompletionTokens,
            usage is not null,
            reserved,
            actual,
            outcome.ElapsedMs,
            MatchesReference: string.Equals(outcome.Decision.ToString(), "Succeeded", StringComparison.Ordinal)
                && outcome.Category is null);
    }

    private static int WriteReport(ChainBoundsReport report, EvaluateChainBoundsOptions selected, int exitCode)
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
        Console.Error.WriteLine($"Chain bounds report: {path}");
        return exitCode;
    }

    private static string DefaultReportPath(bool live)
    {
        var root = FindRepositoryRoot() ?? Directory.GetCurrentDirectory();
        var stamp = DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ", System.Globalization.CultureInfo.InvariantCulture);
        return Path.Combine(root, "artifacts", "chain-bounds", $"evaluate-chain-bounds-{stamp}-{(live ? "live" : "offline")}.json");
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

    private sealed record ChainBoundsCase(
        string CaseId,
        ChainFamily Family,
        string Source,
        string? SourceSelection,
        string? Target,
        string? Mode,
        string? ScriptedClassification,
        string? ScriptedTransformation)
    {
        public string DirectionOrCell => Family == ChainFamily.Translation
            ? $"{SourceSelection ?? "auto"}->{Target}"
            : $"{SourceSelection ?? "auto"}:{Mode}";

        public string? TargetOrMode => Family == ChainFamily.Translation ? Target : Mode;

        public string SourceSha256 => Convert.ToHexStringLower(
            SHA256.HashData(Encoding.UTF8.GetBytes(Source)));

        public TranslationInput ToTranslationInput() => new(Source, SourceSelection, Target);

        public RewritingInput ToRewritingInput() => new(Source, SourceSelection, Mode);
    }

    private sealed record EvaluateChainBoundsOptions(
        bool Live,
        string Profile,
        int MaxDispatches,
        decimal MaxSpendUsd,
        int DeadlineMs,
        string? Output)
    {
        public static bool TryParse(string[] options, out EvaluateChainBoundsOptions selected, out string error)
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
                    case "--fallback-profile":
                        selected = null!;
                        error = "evaluate-chain-bounds is primary-only; a configured fallback is refused so scripted faults are never counted as live evidence.";
                        return false;
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
                        error = $"Unknown or incomplete evaluate-chain-bounds option '{options[index]}'.";
                        return false;
                }
            }

            if (live && offline)
            {
                selected = null!;
                error = "evaluate-chain-bounds accepts at most one of --live and --offline.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(profile))
            {
                selected = null!;
                error = "evaluate-chain-bounds requires --profile <candidate-id>.";
                return false;
            }

            if (maxDispatches <= 0 || maxSpend <= 0)
            {
                selected = null!;
                error = "evaluate-chain-bounds requires a positive finite --max-dispatches and --max-spend-usd budget.";
                return false;
            }

            if (deadlineMs <= 0)
            {
                selected = null!;
                error = "evaluate-chain-bounds requires a positive finite --deadline-ms overall deadline.";
                return false;
            }

            selected = new EvaluateChainBoundsOptions(live, profile, maxDispatches, maxSpend, deadlineMs, output);
            error = string.Empty;
            return true;
        }
    }

    private sealed record ChainSnapshot(
        [property: JsonPropertyOrder(0)] string Primary,
        [property: JsonPropertyOrder(1)] string? Fallback);

    private sealed record PolicySnapshot(
        [property: JsonPropertyOrder(0)] int OverallDeadlineMs,
        [property: JsonPropertyOrder(1)] int EligibilityTimeoutMs,
        [property: JsonPropertyOrder(2)] int TransformationTimeoutMs,
        [property: JsonPropertyOrder(3)] int FinalizationReserveMs);

    private sealed record BudgetObservation(
        [property: JsonPropertyOrder(0)] int MaxDispatches,
        [property: JsonPropertyOrder(1)] decimal MaxSpendUsd,
        [property: JsonPropertyOrder(2)] int DispatchesUsed,
        [property: JsonPropertyOrder(3)] decimal ReservedUsd,
        [property: JsonPropertyOrder(4)] decimal UnresolvedUsd);

    private sealed record ChainAttemptObservation(
        [property: JsonPropertyOrder(0)] string CandidateId,
        [property: JsonPropertyOrder(1)] string CredentialRef,
        [property: JsonPropertyOrder(2)] string Stage,
        [property: JsonPropertyOrder(3)] string FinishCategory,
        [property: JsonPropertyOrder(4)] int StageDispatches,
        [property: JsonPropertyOrder(5)] decimal ReservedUsd,
        [property: JsonPropertyOrder(6)] decimal? ActualUsd,
        [property: JsonPropertyOrder(7)] long ElapsedMs);

    private sealed record ChainBoundsCaseObservation(
        [property: JsonPropertyOrder(0)] string CaseId,
        [property: JsonPropertyOrder(1)] string Family,
        [property: JsonPropertyOrder(2)] string DirectionOrCell,
        [property: JsonPropertyOrder(3)] int SourceLength,
        [property: JsonPropertyOrder(4)] string SourceSha256,
        [property: JsonPropertyOrder(5)] string Decision,
        [property: JsonPropertyOrder(6)] string? Category,
        [property: JsonPropertyOrder(7)] string? ResolvedLanguage,
        [property: JsonPropertyOrder(8)] string? TargetOrMode,
        [property: JsonPropertyOrder(9)] int Dispatches,
        [property: JsonPropertyOrder(10)] bool ChargesCharacters,
        [property: JsonPropertyOrder(11)] int ResultLength,
        [property: JsonPropertyOrder(12)] IReadOnlyList<ChainAttemptObservation> Attempts,
        [property: JsonPropertyOrder(13)] long? PromptTokens,
        [property: JsonPropertyOrder(14)] long? CompletionTokens,
        [property: JsonPropertyOrder(15)] bool UsageKnown,
        [property: JsonPropertyOrder(16)] decimal ReservedUsd,
        [property: JsonPropertyOrder(17)] decimal? ActualUsd,
        [property: JsonPropertyOrder(18)] long ElapsedMs,
        [property: JsonPropertyOrder(19)] bool MatchesReference);

    private sealed record ChainBoundsReport(
        [property: JsonPropertyOrder(0)] string Kind,
        [property: JsonPropertyOrder(1)] string CandidateId,
        [property: JsonPropertyOrder(2)] string CredentialRef,
        [property: JsonPropertyOrder(3)] bool CredentialPresent,
        [property: JsonPropertyOrder(4)] bool Live,
        [property: JsonPropertyOrder(5)] ChainSnapshot Chain,
        [property: JsonPropertyOrder(6)] PolicySnapshot Policy,
        [property: JsonPropertyOrder(7)] BudgetObservation Budget,
        [property: JsonPropertyOrder(8)] IReadOnlyList<ChainBoundsCaseObservation> Cases,
        [property: JsonPropertyOrder(9)] int FailedCases,
        [property: JsonPropertyOrder(10)] int ReferenceMismatches,
        [property: JsonPropertyOrder(11)] string Status,
        [property: JsonPropertyOrder(12)] string? Detail = null);

    private sealed class ScriptedChainBoundsClient(
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
                    "A local gate leaked a chain-bounds dispatch in the offline evaluation."));
            }

            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, response)));
        }

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("The chain bounds evaluation never streams.");
    }

    private sealed class ChainLiveClient(
        ChatCompletionsAdapter adapter,
        TransportCredential credential,
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
            var observation = await adapter.SendAsync(snapshots, credential, null, cancellationToken)
                .ConfigureAwait(false);
            attempts.Add(observation);
            LastAttempt = observation;
            return new ChatResponse(new ChatMessage(ChatRole.Assistant, observation.ResponseText));
        }

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("The chain bounds evaluation never streams.");
    }

    private static PolicySnapshot SnapPolicy(ChainPolicy policy) => new(
        (int)policy.OverallDeadline.TotalMilliseconds,
        (int)policy.EligibilityTimeout.TotalMilliseconds,
        (int)policy.TransformationTimeout.TotalMilliseconds,
        (int)policy.FinalizationReserve.TotalMilliseconds);
}
