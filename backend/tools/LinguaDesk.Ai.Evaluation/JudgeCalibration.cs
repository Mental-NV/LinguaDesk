using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LinguaDesk.Infrastructure.Ai;

namespace LinguaDesk.Ai.Evaluation;

internal sealed class JudgeCalibrationException : InvalidOperationException
{
    public JudgeCalibrationException(string message)
        : base(message)
    {
    }
}

// Approval metadata for a separately reviewed development/calibration set.
// It intentionally stores hashes and score records, not credential material or
// hidden reasoning. The raw development fixtures remain a separately frozen
// evaluation artifact identified by DevelopmentCorpusSha256.
internal static class JudgeCalibration
{
    public const string SchemaRevision = "judge-calibration.v1";

    private static readonly JsonSerializerOptions InputOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static JudgeCalibrationApproval Load(
        string path,
        CandidateProfile judgeProfile,
        IReadOnlyList<BatchCase> releaseCases)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(judgeProfile);
        ArgumentNullException.ThrowIfNull(releaseCases);

        byte[] raw;
        try
        {
            raw = File.ReadAllBytes(path);
        }
        catch (IOException exception)
        {
            throw new JudgeCalibrationException($"The judge calibration record could not be read: {exception.Message}");
        }

        JudgeCalibrationDocument? document;
        try
        {
            document = JsonSerializer.Deserialize<JudgeCalibrationDocument>(raw, InputOptions);
        }
        catch (JsonException exception)
        {
            throw new JudgeCalibrationException($"The judge calibration record is not valid JSON: {exception.Message}");
        }

        if (document is null)
        {
            throw new JudgeCalibrationException("The judge calibration record is empty.");
        }

        if (document.Reviewers is null || document.Cases is null || document.Limitations is null)
        {
            throw new JudgeCalibrationException(
                "The judge calibration record requires reviewers, cases, and limitations arrays.");
        }

        var errors = Validate(document, judgeProfile, releaseCases);
        if (errors.Count > 0)
        {
            throw new JudgeCalibrationException(string.Join(" ", errors));
        }

        var sha = Convert.ToHexStringLower(SHA256.HashData(raw));
        var languages = document.Cases
            .GroupBy(item => item.Language, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        var agreement = Agreement(document.Cases);
        return new JudgeCalibrationApproval(
            document.CalibrationId,
            sha,
            document.Status,
            document.DevelopmentCorpusSha256,
            document.JudgeRunReportSha256,
            document.Cases.Count,
            languages,
            document.Reviewers.Count,
            agreement,
            document.ApprovedUtc,
            document.Limitations);
    }

    private static List<string> Validate(
        JudgeCalibrationDocument document,
        CandidateProfile judgeProfile,
        IReadOnlyList<BatchCase> releaseCases)
    {
        var errors = new List<string>();
        if (!string.Equals(document.SchemaRevision, SchemaRevision, StringComparison.Ordinal))
        {
            errors.Add($"Calibration schema must be '{SchemaRevision}'.");
        }

        if (string.IsNullOrWhiteSpace(document.CalibrationId))
        {
            errors.Add("CalibrationId is required.");
        }

        if (!string.Equals(document.Status, "accepted", StringComparison.Ordinal))
        {
            errors.Add("Calibration status must be explicitly 'accepted'.");
        }

        if (!string.Equals(document.JudgeCandidateId, judgeProfile.CandidateId, StringComparison.Ordinal))
        {
            errors.Add("Calibration judgeCandidateId does not match the selected judge profile.");
        }

        if (!string.Equals(document.GradingPromptRevision, BatchJudge.GradingPromptRevision, StringComparison.Ordinal)
            || !string.Equals(document.GradingPromptSha256, BatchJudge.GradingPromptSha256, StringComparison.Ordinal))
        {
            errors.Add("Calibration grading prompt revision/hash does not match the pinned judge prompt.");
        }

        if (!IsSha256(document.DevelopmentCorpusSha256) || !IsSha256(document.JudgeRunReportSha256))
        {
            errors.Add("Calibration corpus and judge-run report need lowercase SHA-256 identifiers.");
        }

        if (!DateTimeOffset.TryParse(
                document.ApprovedUtc,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AssumeUniversal,
                out _))
        {
            errors.Add("Calibration approvedUtc must be a valid timestamp.");
        }

        if (document.Reviewers.Count == 0
            || document.Reviewers.Any(item => string.IsNullOrWhiteSpace(item.ReviewerId)
                || string.IsNullOrWhiteSpace(item.Competence)))
        {
            errors.Add("Calibration requires identified independent reviewers with competence statements.");
        }

        if (document.Limitations.Count == 0 || document.Limitations.Any(string.IsNullOrWhiteSpace))
        {
            errors.Add("Calibration requires at least one nonblank limitation statement.");
        }

        var reviewerIds = document.Reviewers.Select(item => item.ReviewerId).ToHashSet(StringComparer.Ordinal);
        if (reviewerIds.Count != document.Reviewers.Count)
        {
            errors.Add("Calibration reviewer IDs must be unique.");
        }

        if (reviewerIds.Contains(judgeProfile.CandidateId))
        {
            errors.Add("Calibration reviewers must be independent of the judge model.");
        }

        if (document.Cases.Count < 40)
        {
            errors.Add("Calibration requires at least 40 independently labeled development cases.");
        }

        var ids = new HashSet<string>(StringComparer.Ordinal);
        var releaseIds = releaseCases.Select(item => item.Id).ToHashSet(StringComparer.Ordinal);
        var releaseSourceHashes = releaseCases
            .Select(item => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(item.SourceText))))
            .ToHashSet(StringComparer.Ordinal);
        foreach (var item in document.Cases)
        {
            if (string.IsNullOrWhiteSpace(item.CaseId) || !ids.Add(item.CaseId))
            {
                errors.Add("Calibration case IDs must be nonblank and unique.");
                break;
            }

            if (releaseIds.Contains(item.CaseId)
                || releaseSourceHashes.Contains(item.SourceSha256))
            {
                errors.Add("Calibration cases must be ID/source-disjoint from the release batch.");
                break;
            }

            if (!IsSha256(item.SourceSha256) || !IsSha256(item.OutputSha256))
            {
                errors.Add("Every calibration case needs lowercase source/output SHA-256 identifiers.");
                break;
            }

            if (!reviewerIds.Contains(item.ReviewerId))
            {
                errors.Add("Every calibration case reviewerId must resolve to the reviewer list.");
                break;
            }

            if (!ValidScores(item.HumanScores) || !ValidScores(item.JudgeScores))
            {
                errors.Add("Every calibration human/judge score must be an integer from 0 through 3.");
                break;
            }


            if (!BatchJudge.CriticalErrorsValid(item.HumanCriticalErrors)
                || !BatchJudge.CriticalErrorsValid(item.JudgeCriticalErrors))
            {
                errors.Add("Calibration critical-error labels contain unknown or duplicate values.");
                break;
            }
        }

        foreach (var language in new[] { "en", "ru", "ro", "zh" })
        {
            if (document.Cases.Count(item => string.Equals(item.Language, language, StringComparison.Ordinal)) < 10)
            {
                errors.Add($"Calibration requires at least 10 cases for language '{language}'.");
            }
        }

        if (document.Cases.Any(item => item.Language is not ("en" or "ru" or "ro" or "zh")))
        {
            errors.Add("Calibration languages are restricted to en, ru, ro, and zh.");
        }

        return errors;
    }

    private static JudgeCalibrationAgreement Agreement(IReadOnlyList<JudgeCalibrationCase> cases)
    {
        var exact = 0;
        var withinOne = 0;
        var criticalSetMatches = 0;
        foreach (var item in cases)
        {
            var pairs = new[]
            {
                (item.HumanScores.MeaningFidelity, item.JudgeScores.MeaningFidelity),
                (item.HumanScores.GrammarNaturalness, item.JudgeScores.GrammarNaturalness),
                (item.HumanScores.StyleOrMode, item.JudgeScores.StyleOrMode),
                (item.HumanScores.LanguageAndFormat, item.JudgeScores.LanguageAndFormat),
            };
            exact += pairs.Count(pair => pair.Item1 == pair.Item2);
            withinOne += pairs.Count(pair => Math.Abs(pair.Item1 - pair.Item2) <= 1);

            var humanCriticals = item.HumanCriticalErrors.ToHashSet(StringComparer.Ordinal);
            if (humanCriticals.SetEquals(item.JudgeCriticalErrors))
            {
                criticalSetMatches++;
            }
        }

        return new JudgeCalibrationAgreement(
            DimensionComparisons: cases.Count * 4,
            ExactDimensionMatches: exact,
            WithinOneDimensionMatches: withinOne,
            CriticalSetMatches: criticalSetMatches,
            CriticalSetComparisons: cases.Count);
    }

    private static bool ValidScores(BatchDimensionScores? scores) =>
        scores is not null
        && scores.MeaningFidelity is >= 0 and <= 3
        && scores.GrammarNaturalness is >= 0 and <= 3
        && scores.StyleOrMode is >= 0 and <= 3
        && scores.LanguageAndFormat is >= 0 and <= 3;

    private static bool IsSha256(string? value) =>
        value is not null
        && value.Length == 64
        && value.All(character => char.IsAsciiHexDigit(character) && !char.IsUpper(character));
}

internal sealed record JudgeCalibrationDocument(
    string SchemaRevision,
    string CalibrationId,
    string Status,
    string JudgeCandidateId,
    string GradingPromptRevision,
    string GradingPromptSha256,
    string DevelopmentCorpusSha256,
    string JudgeRunReportSha256,
    string ApprovedUtc,
    IReadOnlyList<JudgeCalibrationReviewer> Reviewers,
    IReadOnlyList<JudgeCalibrationCase> Cases,
    IReadOnlyList<string> Limitations);

internal sealed record JudgeCalibrationReviewer(string ReviewerId, string Competence);

internal sealed record JudgeCalibrationCase(
    string CaseId,
    string Language,
    string SourceSha256,
    string OutputSha256,
    string ReviewerId,
    BatchDimensionScores HumanScores,
    IReadOnlyList<string> HumanCriticalErrors,
    BatchDimensionScores JudgeScores,
    IReadOnlyList<string> JudgeCriticalErrors);

internal sealed record JudgeCalibrationApproval(
    string CalibrationId,
    string CalibrationSha256,
    string Status,
    string DevelopmentCorpusSha256,
    string JudgeRunReportSha256,
    int CaseCount,
    IReadOnlyDictionary<string, int> CasesPerLanguage,
    int ReviewerCount,
    JudgeCalibrationAgreement Agreement,
    string ApprovedUtc,
    IReadOnlyList<string> Limitations);

internal sealed record JudgeCalibrationAgreement(
    int DimensionComparisons,
    int ExactDimensionMatches,
    int WithinOneDimensionMatches,
    int CriticalSetMatches,
    int CriticalSetComparisons);
