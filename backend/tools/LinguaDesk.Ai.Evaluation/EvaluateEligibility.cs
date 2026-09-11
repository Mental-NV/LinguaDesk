using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using LinguaDesk.Core;
using LinguaDesk.Infrastructure.Ai;
using Microsoft.Extensions.AI;

namespace LinguaDesk.Ai.Evaluation;

internal static class EvaluateEligibility
{
    private const string CredentialPrefix = "LINGUADESK_AIEVALUATION__CREDENTIALS__";

    private const string CredentialSuffix = "__APIKEY";

    private static readonly JsonSerializerOptions OutputOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    private static readonly IReadOnlyList<EvalCase> Cases =
    [
        new("elig-en", "Please send the report tomorrow.", EligibilityOperation.Translation, null, "ru", null,
            "{\"status\":\"eligible\",\"language\":\"en\"}", EligibilityDecision.Eligible, "en"),
        new("elig-ru", "Пожалуйста, пришлите отчёт завтра.", EligibilityOperation.Translation, null, "en", null,
            "{\"status\":\"eligible\",\"language\":\"ru\"}", EligibilityDecision.Eligible, "ru"),
        new("elig-ro", "Vă rog să trimiteți raportul mâine.", EligibilityOperation.Translation, null, "en", null,
            "{\"status\":\"eligible\",\"language\":\"ro\"}", EligibilityDecision.Eligible, "ro"),
        new("elig-zh-hans", "请明天发送报告。", EligibilityOperation.Translation, null, "en", null,
            "{\"status\":\"eligible\",\"language\":\"zh\"}", EligibilityDecision.Eligible, "zh"),
        new("elig-zh-hant", "請明天發送報告。", EligibilityOperation.Translation, null, "en", null,
            "{\"status\":\"eligible\",\"language\":\"zh\"}", EligibilityDecision.Eligible, "zh"),
        new("rewriting-simple-en", "Please send the report tomorrow.", EligibilityOperation.Rewriting, null, null, "simple",
            "{\"status\":\"eligible\",\"language\":\"en\"}", EligibilityDecision.Eligible, "en"),
        new("short-ambiguous", "12345", EligibilityOperation.Translation, null, "ru", null,
            "{\"status\":\"uncertain\",\"language\":null}", EligibilityDecision.Uncertain, null),
        new("mixed-content",
            "The quarterly results are strong, and we will publish them on Friday. " +
            "Финансовые показатели за квартал оказались значительно выше ожиданий аналитиков.",
            EligibilityOperation.Translation, null, "ru", null,
            "{\"status\":\"mixed\",\"language\":null}", EligibilityDecision.Mixed, null),
        new("unsupported-content", "يرجى إرسال التقرير غداً.", EligibilityOperation.Translation, null, "en", null,
            "{\"status\":\"unsupported\",\"language\":null}", EligibilityDecision.Unsupported, null),
        new("source-mismatch", "Пожалуйста, пришлите отчёт завтра.", EligibilityOperation.Translation, "en", "ru", null,
            "{\"status\":\"source_mismatch\",\"language\":\"ru\"}", EligibilityDecision.SourceMismatch, null),
        new("gate-empty", "   ", EligibilityOperation.Translation, null, "ru", null,
            null, EligibilityDecision.LocallyRejected, null),
        new("gate-oversize", null, EligibilityOperation.Translation, null, "ru", null,
            null, EligibilityDecision.LocallyRejected, null),
    ];

    internal static async Task<int> RunAsync(string[] options)
    {
        if (!EvaluateEligibilityOptions.TryParse(options, out var selected, out var error))
        {
            Console.Error.WriteLine(error);
            Console.Error.WriteLine(
                "Usage: dotnet LinguaDesk.Ai.Evaluation.dll evaluate-eligibility [--offline|--live] " +
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

        var promptProbe = EligibilityPrompt.Create(string.Empty, null);

        if (!selected.Live)
        {
            return RunOfflineAsync(profile, promptProbe, selected);
        }

        return await RunLiveAsync(profile, promptProbe, selected).ConfigureAwait(false);
    }

    internal static IReadOnlySet<string> FaultCaseIds { get; } =
        new HashSet<string>(StringComparer.Ordinal);

    internal static (IReadOnlyList<SectionRow> Rows, int Mismatches) RunOfflineSection(
        CandidateProfile profile,
        PromptSnapshot promptProbe)
    {
        var scripted = Cases.ToDictionary(
            kase => kase.CaseId,
            kase => kase.ScriptedResponse,
            StringComparer.Ordinal);
        var rows = new List<SectionRow>();
        foreach (var kase in Cases)
        {
            using var client = new ScriptedEligibilityClient(
                scripted.TryGetValue(kase.CaseId, out var response) ? response : null);
            var outcome = EligibilityPipeline.EvaluateAsync(kase.ToInput(), client).GetAwaiter().GetResult();
            var observation = Observe(kase, outcome, client.CallCount, Usage: null, ReservedUsd: 0m, ActualUsd: null);
            rows.Add(new SectionRow(kase.CaseId, outcome.Decision.ToString(), observation));
        }

        var mismatches = rows.Count(row => !((CaseObservation)row.Observation).MatchesReference);
        return (rows, mismatches);
    }

    private static int RunOfflineAsync(
        CandidateProfile profile,
        PromptSnapshot promptProbe,
        EvaluateEligibilityOptions selected)
    {
        var (rows, mismatches) = RunOfflineSection(profile, promptProbe);
        var observations = rows.Select(row => (CaseObservation)row.Observation).ToList();
        var report = new EligibilityEvaluationReport(
            "eligibility_evaluation_report",
            profile.CandidateId,
            profile.CredentialRef,
            CredentialPresent: false,
            Live: false,
            EligibilityEnvelopeParser.PromptRevision,
            promptProbe.ResourceSha256,
            new BudgetObservation(selected.MaxDispatches, selected.MaxSpendUsd, 0, 0m, 0m),
            [.. observations],
            mismatches,
            mismatches == 0 ? "pass" : "fail",
            mismatches == 0 ? null : $"{mismatches} offline case(s) disagree with the allowlisted reference.");

        return WriteReport(report, selected, mismatches == 0 ? 0 : 1);
    }

    private static async Task<int> RunLiveAsync(
        CandidateProfile profile,
        PromptSnapshot promptProbe,
        EvaluateEligibilityOptions selected)
    {
        if (!TryResolveCredential(profile.CredentialRef, out var credential) || credential is null)
        {
            var blocked = new EligibilityEvaluationReport(
                "eligibility_evaluation_report",
                profile.CandidateId,
                profile.CredentialRef,
                CredentialPresent: false,
                Live: false,
                EligibilityEnvelopeParser.PromptRevision,
                promptProbe.ResourceSha256,
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
            var (rows, failed, mismatches) = await RunLiveSectionAsync(profile, promptProbe, budget, credential).ConfigureAwait(false);
            var observations = rows.Select(row => (CaseObservation)row.Observation).ToList();
            var report = new EligibilityEvaluationReport(
                "eligibility_evaluation_report",
                profile.CandidateId,
                profile.CredentialRef,
                CredentialPresent: true,
                Live: true,
                EligibilityEnvelopeParser.PromptRevision,
                promptProbe.ResourceSha256,
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

    internal static async Task<SectionOutcome> RunLiveSectionAsync(
        CandidateProfile profile,
        PromptSnapshot promptProbe,
        EvaluationBudget budget,
        TransportCredential credential)
    {
        using var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(profile.Bounds.AttemptTimeoutSeconds),
        };
        var adapter = new ChatCompletionsAdapter(profile, httpClient);
        using var client = new AdapterChatClient(adapter, credential, budget);
        var rows = new List<SectionRow>();

        foreach (var kase in Cases)
        {
            client.LastObservation = null;
            EligibilityOutcome outcome;
            try
            {
                outcome = await EligibilityPipeline.EvaluateAsync(kase.ToInput(), client).ConfigureAwait(false);
            }
            catch (ChatCompletionsAdapterException exception) when (exception.Kind == AttemptFailureKind.BudgetDenied)
            {
                var denied = Observe(
                    kase,
                    new EligibilityOutcome(
                        EligibilityDecision.Failed,
                        null,
                        "budget-denied",
                        client.CallCount,
                        ChargesCharacters: false,
                        null,
                        promptProbe.PromptId,
                        promptProbe.ResourceSha256),
                    client.CallCount,
                    Usage: null,
                    budget.ReservedUsd,
                    ActualUsd: null);
                rows.Add(new SectionRow(kase.CaseId, nameof(EligibilityDecision.Failed), denied));
                break;
            }

            var observation = client.LastObservation;
            rows.Add(new SectionRow(
                kase.CaseId,
                outcome.Decision.ToString(),
                Observe(
                    kase,
                    outcome,
                    outcome.ClassificationDispatches,
                    observation?.Usage,
                    observation?.ReservedUsd ?? 0m,
                    observation?.ActualUsd)));
        }

        var failed = rows.Count(row =>
            string.Equals(row.Decision, nameof(EligibilityDecision.Failed), StringComparison.Ordinal));
        var mismatches = rows.Count(row => !((CaseObservation)row.Observation).MatchesReference);
        return new SectionOutcome(rows, failed, mismatches);
    }

    private static CaseObservation Observe(
        EvalCase kase,
        EligibilityOutcome outcome,
        int dispatches,
        UsageEvidence? Usage,
        decimal ReservedUsd,
        decimal? ActualUsd) =>
        new(
            kase.CaseId,
            kase.Operation.ToString().ToLowerInvariant(),
            kase.SourceSelection,
            kase.SourceLength,
            kase.SourceSha256,
            outcome.Decision.ToString(),
            outcome.Category,
            outcome.ResolvedLanguage,
            dispatches,
            outcome.ChargesCharacters,
            outcome.ExcessCharacters,
            Usage?.PromptTokens,
            Usage?.CompletionTokens,
            Usage is not null,
            ReservedUsd,
            ActualUsd,
            MatchesReference: outcome.Decision == kase.ExpectedDecision
                && string.Equals(outcome.ResolvedLanguage, kase.ExpectedLanguage, StringComparison.Ordinal));

    private static int WriteReport(EligibilityEvaluationReport report, EvaluateEligibilityOptions selected, int exitCode)
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
        Console.Error.WriteLine($"Eligibility evaluation report: {path}");
        return exitCode;
    }

    private static string DefaultReportPath(bool live)
    {
        var root = FindRepositoryRoot() ?? Directory.GetCurrentDirectory();
        var stamp = DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ", System.Globalization.CultureInfo.InvariantCulture);
        return Path.Combine(root, "artifacts", "eligibility", $"evaluate-eligibility-{stamp}-{(live ? "live" : "offline")}.json");
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
        EligibilityOperation Operation,
        string? SourceSelection,
        string? Target,
        string? Mode,
        string? ScriptedResponse,
        EligibilityDecision ExpectedDecision,
        string? ExpectedLanguage)
    {
        public int SourceLength => (Source ?? OversizeSource()).Length;

        public string SourceSha256 => Convert.ToHexStringLower(
            SHA256.HashData(Encoding.UTF8.GetBytes(Source ?? OversizeSource())));

        public EligibilityInput ToInput() => new(Source ?? OversizeSource(), Operation, SourceSelection, Target, Mode);

        private static string OversizeSource() => new('a', ProductCatalog.TranslationMaximumSourceCharacters + 1);
    }

    private sealed record EvaluateEligibilityOptions(
        bool Live,
        string Profile,
        int MaxDispatches,
        decimal MaxSpendUsd,
        string? Output)
    {
        public static bool TryParse(string[] options, out EvaluateEligibilityOptions selected, out string error)
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
                        error = $"Unknown or incomplete evaluate-eligibility option '{options[index]}'.";
                        return false;
                }
            }

            if (live && offline)
            {
                selected = null!;
                error = "evaluate-eligibility accepts at most one of --live and --offline.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(profile))
            {
                selected = null!;
                error = "evaluate-eligibility requires --profile <candidate-id>.";
                return false;
            }

            if (maxDispatches <= 0 || maxSpend <= 0)
            {
                selected = null!;
                error = "evaluate-eligibility requires a positive finite --max-dispatches and --max-spend-usd budget.";
                return false;
            }

            selected = new EvaluateEligibilityOptions(live, profile, maxDispatches, maxSpend, output);
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

    internal sealed record CaseObservation(
        [property: JsonPropertyOrder(0)] string CaseId,
        [property: JsonPropertyOrder(1)] string Operation,
        [property: JsonPropertyOrder(2)] string? SourceHint,
        [property: JsonPropertyOrder(3)] int SourceLength,
        [property: JsonPropertyOrder(4)] string SourceSha256,
        [property: JsonPropertyOrder(5)] string Decision,
        [property: JsonPropertyOrder(6)] string? Category,
        [property: JsonPropertyOrder(7)] string? ResolvedLanguage,
        [property: JsonPropertyOrder(8)] int Dispatches,
        [property: JsonPropertyOrder(9)] bool ChargesCharacters,
        [property: JsonPropertyOrder(10)] int? ExcessCharacters,
        [property: JsonPropertyOrder(11)] long? PromptTokens,
        [property: JsonPropertyOrder(12)] long? CompletionTokens,
        [property: JsonPropertyOrder(13)] bool UsageKnown,
        [property: JsonPropertyOrder(14)] decimal ReservedUsd,
        [property: JsonPropertyOrder(15)] decimal? ActualUsd,
        [property: JsonPropertyOrder(16)] bool MatchesReference);

    private sealed record EligibilityEvaluationReport(
        [property: JsonPropertyOrder(0)] string Kind,
        [property: JsonPropertyOrder(1)] string CandidateId,
        [property: JsonPropertyOrder(2)] string CredentialRef,
        [property: JsonPropertyOrder(3)] bool CredentialPresent,
        [property: JsonPropertyOrder(4)] bool Live,
        [property: JsonPropertyOrder(5)] string PromptRevision,
        [property: JsonPropertyOrder(6)] string PromptResourceSha256,
        [property: JsonPropertyOrder(7)] BudgetObservation Budget,
        [property: JsonPropertyOrder(8)] IReadOnlyList<CaseObservation> Cases,
        [property: JsonPropertyOrder(9)] int ReferenceMismatches,
        [property: JsonPropertyOrder(10)] string Status,
        [property: JsonPropertyOrder(11)] string? Detail = null);

    private sealed class ScriptedEligibilityClient(string? responseText) : IChatClient
    {
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
            if (responseText is null)
            {
                return Task.FromException<ChatResponse>(new InvalidOperationException(
                    "A local gate leaked a classification dispatch in the offline evaluation."));
            }

            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, responseText)));
        }

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("The eligibility evaluation never streams.");
    }

    private sealed class AdapterChatClient(
        ChatCompletionsAdapter adapter,
        TransportCredential credential,
        EvaluationBudget budget) : IChatClient
    {
        public AttemptObservation? LastObservation { get; set; }

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
            LastObservation = observation;
            return new ChatResponse(new ChatMessage(ChatRole.Assistant, observation.ResponseText));
        }

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("The eligibility evaluation never streams.");
    }
}
