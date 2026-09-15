using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using LinguaDesk.Infrastructure.Ai;

namespace LinguaDesk.Ai.Evaluation;

// Runner-only M036 judge. Candidate identity is deliberately absent from the
// grading messages; the caller supplies only an opaque label bound to the
// case/output hashes. Live calls use the selected registry profile and the
// same EvaluationBudget as candidate generation.
internal static class BatchJudge
{
    public const string JudgeId = "batch-judge.v2";

    public const string GradingPromptRevision = "batch-grading-prompt.v2";

    public const string OfflineCalibrationRef = "dev-calibration.fixture.v1";

    public const int MaxAttempts = 2;

    private static readonly IReadOnlySet<string> AllowedCriticalErrors = new HashSet<string>(
        [
            "invented-fact",
            "material-omission",
            "meaning-reversal",
            "changed-numerical-value",
            "wrong-output-language",
        ],
        StringComparer.Ordinal);

    public const string GradingPromptText =
        "Grade one blinded LinguaDesk candidate output against the supplied synthetic source, " +
        "meaning assertions, and acceptable-output notes. Score exactly four dimensions from 0 " +
        "through 3: meaningFidelity, grammarNaturalness, styleOrMode, and languageAndFormat. " +
        "A score of 3 meets the dimension without a material defect; 2 is usable with only minor " +
        "surface edits; 1 requires substantive editing or misses requested intent; 0 fails the " +
        "dimension or contains a critical error. Critical-error values are restricted to " +
        "invented-fact, material-omission, meaning-reversal, changed-numerical-value, and " +
        "wrong-output-language. Return only one JSON object with exactly these properties: " +
        "scores (an object containing exactly the four integer scores), criticalErrors (an array " +
        "of unique allowed strings), and assessment (a concise conclusion of at most 512 " +
        "characters). Do not identify or guess the candidate, and do not provide hidden reasoning " +
        "or a chain of thought.";

    public static string GradingPromptSha256 =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(GradingPromptText)));

    public static string BlindedLabel(string caseId, string outputSha256)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(caseId);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputSha256);
        return Convert.ToHexStringLower(
            SHA256.HashData(Encoding.UTF8.GetBytes($"batch-grade:{caseId}:{outputSha256}")));
    }

    internal static IReadOnlyList<PromptMessageSnapshot> BuildMessages(
        BatchCase kase,
        string outputText,
        string blindedLabel)
    {
        ArgumentNullException.ThrowIfNull(kase);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputText);
        ArgumentException.ThrowIfNullOrWhiteSpace(blindedLabel);

        var evidence = new JsonObject
        {
            ["blindedLabel"] = blindedLabel,
            ["operation"] = kase.Family,
            ["sourceText"] = kase.SourceText,
            ["sourceSelection"] = kase.SourceSelection,
            ["target"] = kase.Target,
            ["language"] = kase.Language,
            ["mode"] = kase.Mode,
            ["meaningAssertions"] = new JsonArray(
                kase.MeaningAssertions.Select(item => JsonValue.Create(item)).ToArray()),
            ["acceptableOutputNotes"] = kase.AcceptableOutputNotes,
            ["candidateOutput"] = outputText,
        };

        return
        [
            new PromptMessageSnapshot("system", GradingPromptText),
            new PromptMessageSnapshot("user", evidence.ToJsonString()),
        ];
    }

    public static BatchGradeRecord DeterministicFindings(
        BatchCase kase,
        string decision,
        string? category,
        string? outputText,
        string calibrationRef,
        string calibrationSha256)
    {
        ArgumentNullException.ThrowIfNull(kase);

        var outputSha = outputText is null
            ? null
            : Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(outputText)));
        var flags = new List<string>();
        var succeeded = string.Equals(decision, "Succeeded", StringComparison.Ordinal);
        var expectedEligible = string.Equals(kase.ExpectedEligibility, "eligible", StringComparison.Ordinal);
        var eligibilityMatch = expectedEligible == succeeded;

        if (!eligibilityMatch)
        {
            flags.Add("unexpected-eligibility-outcome");
        }

        List<string> missingDigits = [];
        if (succeeded)
        {
            if (string.IsNullOrWhiteSpace(outputText))
            {
                flags.Add("unusable-output");
            }
            else
            {
                var sourceDigits = BatchCorpus.DigitTokens(kase.SourceText);
                var outputDigits = BatchCorpus.DigitTokens(outputText);
                missingDigits = sourceDigits.Where(token => !outputDigits.Contains(token)).ToList();
                if (missingDigits.Count > 0)
                {
                    flags.Add("changed-numerical-value-suspect");
                }
            }
        }
        else if (string.Equals(category, "provider-failure", StringComparison.Ordinal)
            || string.Equals(decision, "Failed", StringComparison.Ordinal))
        {
            flags.Add("provider-failure");
        }

        return new BatchGradeRecord(
            JudgeId,
            GradingPromptRevision,
            GradingPromptSha256,
            outputSha is null ? null : BlindedLabel(kase.Id, outputSha),
            outputSha,
            eligibilityMatch,
            missingDigits.AsReadOnly(),
            flags.AsReadOnly(),
            DimensionStatus: succeeded ? "unresolved-model-grading-pending" : "not-applicable-no-successful-output",
            DimensionReason: succeeded
                ? "A successful output requires a valid model grade; no score is silently defaulted."
                : "Only successful candidate outputs receive four-dimension model grades.",
            calibrationRef,
            calibrationSha256,
            JudgeCandidateId: null,
            JudgeAdapterId: null,
            JudgeEndpoint: null,
            JudgeModel: null,
            JudgeCredentialRef: null,
            JudgeAttempts: 0,
            JudgeDispatches: 0,
            JudgePromptTokens: null,
            JudgeCompletionTokens: null,
            JudgeUsageKnown: false,
            JudgeReservedUsd: 0m,
            JudgeActualUsd: null,
            Scores: null,
            ModelCriticalErrors: [],
            Assessment: null,
            Usable: false);
    }

    public static async Task<BatchGradeRecord> GradeAsync(
        BatchCase kase,
        string decision,
        string? category,
        string? outputText,
        CandidateProfile judgeProfile,
        string calibrationRef,
        string calibrationSha256,
        bool live,
        Func<IReadOnlyList<PromptMessageSnapshot>, CancellationToken, Task<BatchJudgeAttemptResult>> sendAsync,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(judgeProfile);
        ArgumentNullException.ThrowIfNull(sendAsync);
        judgeProfile.Validate();

        var grade = DeterministicFindings(
            kase, decision, category, outputText, calibrationRef, calibrationSha256);
        if (!string.Equals(decision, "Succeeded", StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(outputText)
            || grade.BlindedLabel is null)
        {
            return grade;
        }

        var messages = BuildMessages(kase, outputText, grade.BlindedLabel);
        var observations = new List<AttemptObservation>();
        var dispatches = 0;
        string? lastInvalidReason = null;

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            var result = await sendAsync(messages, cancellationToken).ConfigureAwait(false);
            if (result.Dispatched)
            {
                dispatches++;
            }

            if (result.Observation is not null)
            {
                observations.Add(result.Observation);
            }

            if (result.FailureCategory is not null)
            {
                return CompleteUnresolved(
                    grade,
                    judgeProfile,
                    live,
                    attempt,
                    dispatches,
                    observations,
                    $"unresolved-{result.FailureCategory}",
                    "The judge transport failed; the output remains unresolved and was not defaulted.");
            }

            if (result.Observation is null)
            {
                return CompleteUnresolved(
                    grade,
                    judgeProfile,
                    live,
                    attempt,
                    dispatches,
                    observations,
                    "unresolved-judge-transport",
                    "The judge transport returned no observation; the output remains unresolved.");
            }

            if (TryParseEnvelope(result.Observation.ResponseText, out var envelope, out lastInvalidReason))
            {
                var status = live ? "resolved-model-grade" : "offline-scripted-grade";
                return CompleteResolved(
                    grade, judgeProfile, status, attempt, dispatches, observations, envelope);
            }
        }

        return CompleteUnresolved(
            grade,
            judgeProfile,
            live,
            MaxAttempts,
            dispatches,
            observations,
            "unresolved-invalid-model-grade",
            $"The judge returned invalid grade envelopes for both bounded attempts: {lastInvalidReason}");
    }

    internal static bool TryParseEnvelope(
        string response,
        out BatchJudgeEnvelope envelope,
        out string error)
    {
        envelope = null!;
        error = string.Empty;
        try
        {
            using var document = JsonDocument.Parse(response);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !HasExactProperties(root, "scores", "criticalErrors", "assessment"))
            {
                error = "The grade root must contain exactly scores, criticalErrors, and assessment.";
                return false;
            }

            var scoresElement = root.GetProperty("scores");
            if (scoresElement.ValueKind != JsonValueKind.Object
                || !HasExactProperties(
                    scoresElement,
                    "meaningFidelity",
                    "grammarNaturalness",
                    "styleOrMode",
                    "languageAndFormat"))
            {
                error = "The scores object does not match the pinned four-dimension schema.";
                return false;
            }

            if (!TryReadScore(scoresElement, "meaningFidelity", out var meaning)
                || !TryReadScore(scoresElement, "grammarNaturalness", out var grammar)
                || !TryReadScore(scoresElement, "styleOrMode", out var style)
                || !TryReadScore(scoresElement, "languageAndFormat", out var language))
            {
                error = "Every dimension score must be an integer from 0 through 3.";
                return false;
            }

            var criticalElement = root.GetProperty("criticalErrors");
            if (criticalElement.ValueKind != JsonValueKind.Array)
            {
                error = "criticalErrors must be an array.";
                return false;
            }

            var criticals = new List<string>();
            foreach (var item in criticalElement.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.String
                    || item.GetString() is not { } critical
                    || !CriticalErrorsValid([.. criticals, critical]))
                {
                    error = "criticalErrors contains an unknown, duplicate, or non-string value.";
                    return false;
                }

                criticals.Add(critical);
            }

            var assessmentElement = root.GetProperty("assessment");
            if (assessmentElement.ValueKind != JsonValueKind.String
                || assessmentElement.GetString() is not { } assessment
                || string.IsNullOrWhiteSpace(assessment)
                || assessment.Length > 512)
            {
                error = "assessment must be a nonblank string of at most 512 characters.";
                return false;
            }

            envelope = new BatchJudgeEnvelope(
                new BatchDimensionScores(meaning, grammar, style, language),
                criticals.AsReadOnly(),
                assessment);
            return true;
        }
        catch (JsonException)
        {
            error = "The grade response is not valid JSON.";
            return false;
        }
    }

    private static bool HasExactProperties(JsonElement element, params string[] expected)
    {
        var properties = element.EnumerateObject().Select(property => property.Name).ToList();
        return properties.Count == expected.Length
            && expected.All(name => properties.Contains(name, StringComparer.Ordinal));
    }

    private static bool TryReadScore(JsonElement scores, string property, out int score)
    {
        score = -1;
        return scores.TryGetProperty(property, out var value)
            && value.ValueKind == JsonValueKind.Number
            && value.TryGetInt32(out score)
            && score is >= 0 and <= 3;
    }

    internal static bool CriticalErrorsValid(IReadOnlyList<string>? criticalErrors) =>
        criticalErrors is not null
        && criticalErrors.All(AllowedCriticalErrors.Contains)
        && criticalErrors.Distinct(StringComparer.Ordinal).Count() == criticalErrors.Count;

    private static BatchGradeRecord CompleteResolved(
        BatchGradeRecord grade,
        CandidateProfile profile,
        string status,
        int attempts,
        int dispatches,
        List<AttemptObservation> observations,
        BatchJudgeEnvelope envelope)
    {
        var usable = envelope.Scores.MeaningFidelity >= 2
            && envelope.Scores.GrammarNaturalness >= 2
            && envelope.Scores.StyleOrMode >= 2
            && envelope.Scores.LanguageAndFormat >= 2
            && envelope.CriticalErrors.Count == 0;

        return grade with
        {
            DimensionStatus = status,
            DimensionReason = "The strict pinned judge envelope was accepted.",
            JudgeCandidateId = profile.CandidateId,
            JudgeAdapterId = profile.AdapterId,
            JudgeEndpoint = profile.Endpoint,
            JudgeModel = profile.Model,
            JudgeCredentialRef = profile.CredentialRef,
            JudgeAttempts = attempts,
            JudgeDispatches = dispatches,
            JudgePromptTokens = CombineKnownTokens(observations, prompt: true),
            JudgeCompletionTokens = CombineKnownTokens(observations, prompt: false),
            JudgeUsageKnown = observations.Count > 0 && observations.All(item => item.Usage is not null),
            JudgeReservedUsd = observations.Sum(item => item.ReservedUsd),
            JudgeActualUsd = CombineActual(observations),
            Scores = envelope.Scores,
            ModelCriticalErrors = envelope.CriticalErrors,
            Assessment = envelope.Assessment,
            Usable = usable,
        };
    }

    private static BatchGradeRecord CompleteUnresolved(
        BatchGradeRecord grade,
        CandidateProfile profile,
        bool live,
        int attempts,
        int dispatches,
        List<AttemptObservation> observations,
        string status,
        string reason) =>
        grade with
        {
            DimensionStatus = live ? status : $"offline-{status}",
            DimensionReason = reason,
            JudgeCandidateId = profile.CandidateId,
            JudgeAdapterId = profile.AdapterId,
            JudgeEndpoint = profile.Endpoint,
            JudgeModel = profile.Model,
            JudgeCredentialRef = profile.CredentialRef,
            JudgeAttempts = attempts,
            JudgeDispatches = dispatches,
            JudgePromptTokens = CombineKnownTokens(observations, prompt: true),
            JudgeCompletionTokens = CombineKnownTokens(observations, prompt: false),
            JudgeUsageKnown = observations.Count > 0 && observations.All(item => item.Usage is not null),
            JudgeReservedUsd = observations.Sum(item => item.ReservedUsd),
            JudgeActualUsd = CombineActual(observations),
        };

    private static long? CombineKnownTokens(List<AttemptObservation> observations, bool prompt)
    {
        if (observations.Count == 0 || observations.Any(item => item.Usage is null))
        {
            return null;
        }

        return observations.Sum(item => prompt
            ? item.Usage!.PromptTokens!.Value
            : item.Usage!.CompletionTokens!.Value);
    }

    private static decimal? CombineActual(List<AttemptObservation> observations)
    {
        if (observations.Count == 0 || observations.Any(item => !item.ActualUsd.HasValue))
        {
            return null;
        }

        return observations.Sum(item => item.ActualUsd!.Value);
    }
}

internal sealed record BatchJudgeAttemptResult(
    bool Dispatched,
    AttemptObservation? Observation,
    string? FailureCategory);

internal sealed record BatchJudgeEnvelope(
    BatchDimensionScores Scores,
    IReadOnlyList<string> CriticalErrors,
    string Assessment);

internal sealed record BatchDimensionScores(
    [property: JsonPropertyOrder(0)] int MeaningFidelity,
    [property: JsonPropertyOrder(1)] int GrammarNaturalness,
    [property: JsonPropertyOrder(2)] int StyleOrMode,
    [property: JsonPropertyOrder(3)] int LanguageAndFormat);

internal sealed record BatchGradeRecord(
    [property: JsonPropertyOrder(0)] string JudgeId,
    [property: JsonPropertyOrder(1)] string GradingPromptRevision,
    [property: JsonPropertyOrder(2)] string GradingPromptSha256,
    [property: JsonPropertyOrder(3)] string? BlindedLabel,
    [property: JsonPropertyOrder(4)] string? OutputSha256,
    [property: JsonPropertyOrder(5)] bool EligibilityMatch,
    [property: JsonPropertyOrder(6)] IReadOnlyList<string> MissingDigitTokens,
    [property: JsonPropertyOrder(7)] IReadOnlyList<string> CriticalFlags,
    [property: JsonPropertyOrder(8)] string DimensionStatus,
    [property: JsonPropertyOrder(9)] string DimensionReason,
    [property: JsonPropertyOrder(10)] string CalibrationRef,
    [property: JsonPropertyOrder(11)] string CalibrationSha256,
    [property: JsonPropertyOrder(12)] string? JudgeCandidateId,
    [property: JsonPropertyOrder(13)] string? JudgeAdapterId,
    [property: JsonPropertyOrder(14)] string? JudgeEndpoint,
    [property: JsonPropertyOrder(15)] string? JudgeModel,
    [property: JsonPropertyOrder(16)] string? JudgeCredentialRef,
    [property: JsonPropertyOrder(17)] int JudgeAttempts,
    [property: JsonPropertyOrder(18)] int JudgeDispatches,
    [property: JsonPropertyOrder(19)] long? JudgePromptTokens,
    [property: JsonPropertyOrder(20)] long? JudgeCompletionTokens,
    [property: JsonPropertyOrder(21)] bool JudgeUsageKnown,
    [property: JsonPropertyOrder(22)] decimal JudgeReservedUsd,
    [property: JsonPropertyOrder(23)] decimal? JudgeActualUsd,
    [property: JsonPropertyOrder(24)] BatchDimensionScores? Scores,
    [property: JsonPropertyOrder(25)] IReadOnlyList<string> ModelCriticalErrors,
    [property: JsonPropertyOrder(26)] string? Assessment,
    [property: JsonPropertyOrder(27)] bool Usable);
