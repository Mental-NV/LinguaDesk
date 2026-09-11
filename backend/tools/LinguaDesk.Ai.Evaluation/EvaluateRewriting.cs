using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using LinguaDesk.Core;
using LinguaDesk.Infrastructure.Ai;
using Microsoft.Extensions.AI;

namespace LinguaDesk.Ai.Evaluation;

internal static class EvaluateRewriting
{
    private const string CredentialPrefix = "LINGUADESK_AIEVALUATION__CREDENTIALS__";

    private const string CredentialSuffix = "__APIKEY";

    private const string RewrittenText = "Rewritten synthetic text.";

    private static readonly JsonSerializerOptions OutputOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    private static readonly Dictionary<string, string> SourcesByLanguage =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["en"] = "Please send the report tomorrow.",
            ["ru"] = "Пожалуйста, пришлите отчёт завтра.",
            ["ro"] = "Vă rog să trimiteți raportul mâine.",
            ["zh-hans"] = "请明天发送报告。",
            ["zh-hant"] = "請明天發送報告。",
        };

    private static readonly IReadOnlyList<EvalCase> Cases = BuildCases();

    private static List<EvalCase> BuildCases()
    {
        var cases = new List<EvalCase>();
        var modeIndex = 0;
        foreach (var mode in ProductCatalog.RewritingModes)
        {
            foreach (var language in ProductCatalog.Languages)
            {
                var sourceKey = language.Id;
                var cellSuffix = language.Id;
                if (string.Equals(language.Id, "zh", StringComparison.Ordinal))
                {
                    // Alternate both Chinese input scripts across the nine zh cells.
                    var traditional = modeIndex % 2 == 1;
                    sourceKey = traditional ? "zh-hant" : "zh-hans";
                    cellSuffix = sourceKey;
                }

                var source = SourcesByLanguage[sourceKey];
                cases.Add(new EvalCase(
                    $"rw-{cellSuffix}-{mode.Id}",
                    source,
                    language.Id,
                    mode.Id,
                    $"{{\"status\":\"eligible\",\"language\":\"{language.Id}\"}}",
                    $"{{\"status\":\"result\",\"text\":\"{RewrittenText}\"}}",
                    RewritingDecision.Succeeded,
                    null));
            }

            modeIndex++;
        }

        cases.Add(new EvalCase(
            "short-ambiguous",
            "12345",
            null,
            "simple",
            "{\"status\":\"uncertain\",\"language\":null}",
            null,
            RewritingDecision.Ineligible,
            "uncertain"));
        cases.Add(new EvalCase(
            "refusal-scripted",
            SourcesByLanguage["en"],
            "en",
            "business",
            "{\"status\":\"eligible\",\"language\":\"en\"}",
            "{\"status\":\"refused\",\"text\":null}",
            RewritingDecision.Refused,
            "refused",
            OfflineOnly: true));
        cases.Add(new EvalCase(
            "malformed-scripted",
            SourcesByLanguage["en"],
            "en",
            "business",
            "{\"status\":\"eligible\",\"language\":\"en\"}",
            "{\"status\":\"eligible\"}",
            RewritingDecision.Failed,
            "invalid-envelope",
            OfflineOnly: true));
        cases.Add(new EvalCase(
            "gate-empty",
            "   ",
            null,
            "simple",
            null,
            null,
            RewritingDecision.Ineligible,
            "empty"));
        cases.Add(new EvalCase(
            "gate-oversize",
            null,
            null,
            "simple",
            null,
            null,
            RewritingDecision.Ineligible,
            "oversize"));
        cases.Add(new EvalCase(
            "invalid-mode",
            SourcesByLanguage["en"],
            "en",
            "correction",
            null,
            null,
            RewritingDecision.Ineligible,
            "invalid-mode"));

        return cases;
    }

    internal static async Task<int> RunAsync(string[] options)
    {
        if (!EvaluateRewritingOptions.TryParse(options, out var selected, out var error))
        {
            Console.Error.WriteLine(error);
            Console.Error.WriteLine(
                "Usage: dotnet LinguaDesk.Ai.Evaluation.dll evaluate-rewriting [--offline|--live] " +
                "--profile <id> --max-dispatches <n> --max-spend-usd <amount> [--output <path>]");
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

        var eligibilityProbe = EligibilityPrompt.Create(string.Empty, null);
        var rewritingProbe = RewritingPrompt.Create(
            string.Empty, "en", ProductCatalog.DefaultRewritingMode);

        if (!selected.Live)
        {
            return RunOfflineAsync(profile, eligibilityProbe, rewritingProbe, selected);
        }

        return await RunLiveAsync(profile, eligibilityProbe, rewritingProbe, selected).ConfigureAwait(false);
    }

    private static int RunOfflineAsync(
        CandidateProfile profile,
        PromptSnapshot eligibilityProbe,
        PromptSnapshot rewritingProbe,
        EvaluateRewritingOptions selected)
    {
        var observations = new List<CaseObservation>();
        foreach (var kase in Cases)
        {
            using var client = new ScriptedRewritingRunnerClient(
                kase.ScriptedClassification, kase.ScriptedTransformation);
            var outcome = RewritingPipeline.RewriteAsync(kase.ToInput(), client).GetAwaiter().GetResult();
            observations.Add(Observe(kase, outcome, Usage: null, ReservedUsd: 0m, ActualUsd: null));
        }

        var mismatches = observations.Count(observation => !observation.MatchesReference);
        var report = new RewritingEvaluationReport(
            "rewriting_evaluation_report",
            profile.CandidateId,
            profile.CredentialRef,
            CredentialPresent: false,
            Live: false,
            RewritingEnvelopeParser.PromptRevision,
            rewritingProbe.ResourceSha256,
            EligibilityEnvelopeParser.PromptRevision,
            eligibilityProbe.ResourceSha256,
            new BudgetObservation(selected.MaxDispatches, selected.MaxSpendUsd, 0, 0m, 0m),
            [.. observations],
            mismatches,
            mismatches == 0 ? "pass" : "fail",
            mismatches == 0 ? null : $"{mismatches} offline case(s) disagree with the allowlisted reference.");

        return WriteReport(report, selected, mismatches == 0 ? 0 : 1);
    }

    private static async Task<int> RunLiveAsync(
        CandidateProfile profile,
        PromptSnapshot eligibilityProbe,
        PromptSnapshot rewritingProbe,
        EvaluateRewritingOptions selected)
    {
        if (!TryResolveCredential(profile.CredentialRef, out var credential) || credential is null)
        {
            var blocked = new RewritingEvaluationReport(
                "rewriting_evaluation_report",
                profile.CandidateId,
                profile.CredentialRef,
                CredentialPresent: false,
                Live: false,
                RewritingEnvelopeParser.PromptRevision,
                rewritingProbe.ResourceSha256,
                EligibilityEnvelopeParser.PromptRevision,
                eligibilityProbe.ResourceSha256,
                new BudgetObservation(selected.MaxDispatches, selected.MaxSpendUsd, 0, 0m, 0m),
                [],
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
            using var client = new AdapterChatClient(adapter, credential, budget);
            var observations = new List<CaseObservation>();

            foreach (var kase in Cases)
            {
                if (kase.OfflineOnly)
                {
                    observations.Add(SkippedObservation(kase));
                    continue;
                }

                client.Attempts.Clear();
                RewritingOutcome outcome;
                try
                {
                    outcome = await RewritingPipeline.RewriteAsync(kase.ToInput(), client).ConfigureAwait(false);
                }
                catch (ChatCompletionsAdapterException exception) when (exception.Kind == AttemptFailureKind.BudgetDenied)
                {
                    observations.Add(Observe(
                        kase,
                        new RewritingOutcome(
                            RewritingDecision.Failed,
                            null,
                            kase.Mode,
                            null,
                            "budget-denied",
                            client.CallCount,
                            0,
                            client.CallCount,
                            ChargesCharacters: false,
                            null,
                            eligibilityProbe.PromptId,
                            eligibilityProbe.ResourceSha256,
                            rewritingProbe.PromptId,
                            rewritingProbe.ResourceSha256),
                        Usage: null,
                        budget.ReservedUsd,
                        ActualUsd: null));
                    break;
                }

                observations.Add(Observe(
                    kase,
                    outcome,
                    CombineUsage(client.Attempts),
                    client.Attempts.Sum(attempt => attempt.ReservedUsd),
                    CombineActual(client.Attempts)));
            }

            var failed = observations.Count(observation =>
                string.Equals(observation.Decision, nameof(RewritingDecision.Failed), StringComparison.Ordinal));
            var mismatches = observations.Count(observation => !observation.MatchesReference);
            var report = new RewritingEvaluationReport(
                "rewriting_evaluation_report",
                profile.CandidateId,
                profile.CredentialRef,
                CredentialPresent: true,
                Live: true,
                RewritingEnvelopeParser.PromptRevision,
                rewritingProbe.ResourceSha256,
                EligibilityEnvelopeParser.PromptRevision,
                eligibilityProbe.ResourceSha256,
                new BudgetObservation(
                    selected.MaxDispatches,
                    selected.MaxSpendUsd,
                    budget.DispatchesUsed,
                    budget.ReservedUsd,
                    budget.UnresolvedUsd),
                [.. observations],
                mismatches,
                failed == 0 ? "success" : "fail",
                failed == 0
                    ? (mismatches == 0
                        ? null
                        : $"{mismatches} live case(s) disagree with the development reference; see per-case rows.")
                    : $"{failed} live case(s) failed; failures are retained per case with usage/exposure metadata.");

            return WriteReport(report, selected, failed == 0 ? 0 : 1);
        }
    }

    private static UsageEvidence? CombineUsage(List<AttemptObservation> attempts)
    {
        if (attempts.Count == 0)
        {
            return null;
        }

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

        return new UsageEvidence(prompt, completion, prompt + completion, null, null);
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

    private static CaseObservation SkippedObservation(EvalCase kase) =>
        new(
            kase.CaseId,
            kase.Cell,
            kase.SourceSelection,
            kase.SourceLength,
            kase.SourceSha256,
            "Skipped",
            "offline-only",
            null,
            kase.Mode,
            0,
            0,
            0,
            false,
            null,
            0,
            null,
            null,
            false,
            0m,
            null,
            MatchesReference: true);

    private static CaseObservation Observe(
        EvalCase kase,
        RewritingOutcome outcome,
        UsageEvidence? Usage,
        decimal ReservedUsd,
        decimal? ActualUsd) =>
        new(
            kase.CaseId,
            kase.Cell,
            kase.SourceSelection,
            kase.SourceLength,
            kase.SourceSha256,
            outcome.Decision.ToString(),
            outcome.Category,
            outcome.ResolvedSourceLanguage,
            outcome.Mode,
            outcome.EligibilityDispatches,
            outcome.TransformationDispatches,
            outcome.TotalDispatches,
            outcome.ChargesCharacters,
            outcome.ExcessCharacters,
            outcome.Text?.Length ?? 0,
            Usage?.PromptTokens,
            Usage?.CompletionTokens,
            Usage is not null,
            ReservedUsd,
            ActualUsd,
            MatchesReference: outcome.Decision == kase.ExpectedDecision
                && string.Equals(outcome.Category, kase.ExpectedCategory, StringComparison.Ordinal));

    private static int WriteReport(RewritingEvaluationReport report, EvaluateRewritingOptions selected, int exitCode)
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
        Console.Error.WriteLine($"Rewriting evaluation report: {path}");
        return exitCode;
    }

    private static string DefaultReportPath(bool live)
    {
        var root = FindRepositoryRoot() ?? Directory.GetCurrentDirectory();
        var stamp = DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ", System.Globalization.CultureInfo.InvariantCulture);
        return Path.Combine(root, "artifacts", "rewriting", $"evaluate-rewriting-{stamp}-{(live ? "live" : "offline")}.json");
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

    private sealed record EvalCase(
        string CaseId,
        string? Source,
        string? SourceSelection,
        string? Mode,
        string? ScriptedClassification,
        string? ScriptedTransformation,
        RewritingDecision ExpectedDecision,
        string? ExpectedCategory,
        bool OfflineOnly = false)
    {
        public string Cell => $"{SourceSelection ?? "auto"}:{Mode ?? "default"}";

        public int SourceLength => (Source ?? OversizeSource()).Length;

        public string SourceSha256 => Convert.ToHexStringLower(
            SHA256.HashData(Encoding.UTF8.GetBytes(Source ?? OversizeSource())));

        public RewritingInput ToInput() => new(Source ?? OversizeSource(), SourceSelection, Mode);

        private static string OversizeSource() => new('a', ProductCatalog.RewritingMaximumSourceCharacters + 1);
    }

    private sealed record EvaluateRewritingOptions(
        bool Live,
        string Profile,
        int MaxDispatches,
        decimal MaxSpendUsd,
        string? Output)
    {
        public static bool TryParse(string[] options, out EvaluateRewritingOptions selected, out string error)
        {
            var live = false;
            var offline = false;
            string? profile = null;
            string? output = null;
            int maxDispatches = 0;
            decimal maxSpend = 0m;

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
                    default:
                        selected = null!;
                        error = $"Unknown or incomplete evaluate-rewriting option '{options[index]}'.";
                        return false;
                }
            }

            if (live && offline)
            {
                selected = null!;
                error = "evaluate-rewriting accepts at most one of --live and --offline.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(profile))
            {
                selected = null!;
                error = "evaluate-rewriting requires --profile <candidate-id>.";
                return false;
            }

            if (maxDispatches <= 0 || maxSpend <= 0)
            {
                selected = null!;
                error = "evaluate-rewriting requires a positive finite --max-dispatches and --max-spend-usd budget.";
                return false;
            }

            selected = new EvaluateRewritingOptions(live, profile, maxDispatches, maxSpend, output);
            error = string.Empty;
            return true;
        }
    }

    private sealed record BudgetObservation(
        [property: JsonPropertyOrder(0)] int MaxDispatches,
        [property: JsonPropertyOrder(1)] decimal MaxSpendUsd,
        [property: JsonPropertyOrder(2)] int DispatchesUsed,
        [property: JsonPropertyOrder(3)] decimal ReservedUsd,
        [property: JsonPropertyOrder(4)] decimal UnresolvedUsd);

    private sealed record CaseObservation(
        [property: JsonPropertyOrder(0)] string CaseId,
        [property: JsonPropertyOrder(1)] string Cell,
        [property: JsonPropertyOrder(2)] string? SourceHint,
        [property: JsonPropertyOrder(3)] int SourceLength,
        [property: JsonPropertyOrder(4)] string SourceSha256,
        [property: JsonPropertyOrder(5)] string Decision,
        [property: JsonPropertyOrder(6)] string? Category,
        [property: JsonPropertyOrder(7)] string? ResolvedLanguage,
        [property: JsonPropertyOrder(8)] string? Mode,
        [property: JsonPropertyOrder(9)] int EligibilityDispatches,
        [property: JsonPropertyOrder(10)] int TransformationDispatches,
        [property: JsonPropertyOrder(11)] int Dispatches,
        [property: JsonPropertyOrder(12)] bool ChargesCharacters,
        [property: JsonPropertyOrder(13)] int? ExcessCharacters,
        [property: JsonPropertyOrder(14)] int ResultLength,
        [property: JsonPropertyOrder(15)] long? PromptTokens,
        [property: JsonPropertyOrder(16)] long? CompletionTokens,
        [property: JsonPropertyOrder(17)] bool UsageKnown,
        [property: JsonPropertyOrder(18)] decimal ReservedUsd,
        [property: JsonPropertyOrder(19)] decimal? ActualUsd,
        [property: JsonPropertyOrder(20)] bool MatchesReference);

    private sealed record RewritingEvaluationReport(
        [property: JsonPropertyOrder(0)] string Kind,
        [property: JsonPropertyOrder(1)] string CandidateId,
        [property: JsonPropertyOrder(2)] string CredentialRef,
        [property: JsonPropertyOrder(3)] bool CredentialPresent,
        [property: JsonPropertyOrder(4)] bool Live,
        [property: JsonPropertyOrder(5)] string PromptRevision,
        [property: JsonPropertyOrder(6)] string PromptResourceSha256,
        [property: JsonPropertyOrder(7)] string EligibilityPromptRevision,
        [property: JsonPropertyOrder(8)] string EligibilityPromptResourceSha256,
        [property: JsonPropertyOrder(9)] BudgetObservation Budget,
        [property: JsonPropertyOrder(10)] IReadOnlyList<CaseObservation> Cases,
        [property: JsonPropertyOrder(11)] int ReferenceMismatches,
        [property: JsonPropertyOrder(12)] string Status,
        [property: JsonPropertyOrder(13)] string? Detail = null);

    private sealed class ScriptedRewritingRunnerClient(
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
                    "A local gate leaked a rewriting dispatch in the offline evaluation."));
            }

            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, response)));
        }

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("The rewriting evaluation never streams.");
    }

    private sealed class AdapterChatClient(
        ChatCompletionsAdapter adapter,
        TransportCredential credential,
        EvaluationBudget budget) : IChatClient
    {
        public List<AttemptObservation> Attempts { get; } = [];

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
            Attempts.Add(observation);
            return new ChatResponse(new ChatMessage(ChatRole.Assistant, observation.ResponseText));
        }

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("The rewriting evaluation never streams.");
    }
}
