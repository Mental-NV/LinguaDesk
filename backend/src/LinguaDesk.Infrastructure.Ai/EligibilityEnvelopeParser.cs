using System.Text;
using System.Text.Json;

namespace LinguaDesk.Infrastructure.Ai;

public enum EligibilityStatus
{
    Eligible,
    Uncertain,
    Unsupported,
    Mixed,
    SourceMismatch,
    Refused,
}

public sealed record EligibilityClassification(EligibilityStatus Status, string? Language);

public static class EligibilityEnvelopeParser
{
    public const string PromptRevision = "eligibility.v1";

    public const int MaxRawBytes = 65536;

    private const int MaxDepth = 8;

    public static bool TryParse(
        string responseText,
        string? validatedSourceHint,
        out EligibilityClassification? classification,
        out string? rejectionReason)
    {
        classification = null;

        if (responseText is null)
        {
            rejectionReason = "The classification response is missing.";
            return false;
        }

        if (Encoding.UTF8.GetByteCount(responseText) > MaxRawBytes)
        {
            rejectionReason = "The classification response exceeds the raw byte bound.";
            return false;
        }

        var trimmed = responseText.Trim();
        if (!trimmed.StartsWith('{'))
        {
            rejectionReason = "The classification response is not a single JSON object.";
            return false;
        }

        string? statusText = null;
        var statusSeen = false;
        string? languageText = null;
        var languageIsNull = false;
        var languageSeen = false;

        try
        {
            var bytes = Encoding.UTF8.GetBytes(trimmed);
            var reader = new Utf8JsonReader(bytes, new JsonReaderOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow,
                MaxDepth = MaxDepth,
            });

            if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
            {
                rejectionReason = "The classification response is not a single JSON object.";
                return false;
            }

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    break;
                }

                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    rejectionReason = "The classification response has an unexpected shape.";
                    return false;
                }

                var name = reader.GetString();
                if (!reader.Read())
                {
                    rejectionReason = "The classification response ends inside a property.";
                    return false;
                }

                switch (name)
                {
                    case "status":
                        if (statusSeen)
                        {
                            rejectionReason = "The classification response repeats 'status'.";
                            return false;
                        }

                        statusSeen = true;
                        if (reader.TokenType != JsonTokenType.String)
                        {
                            rejectionReason = "The classification 'status' is not a string.";
                            return false;
                        }

                        statusText = reader.GetString();
                        break;
                    case "language":
                        if (languageSeen)
                        {
                            rejectionReason = "The classification response repeats 'language'.";
                            return false;
                        }

                        languageSeen = true;
                        if (reader.TokenType == JsonTokenType.Null)
                        {
                            languageIsNull = true;
                        }
                        else if (reader.TokenType == JsonTokenType.String)
                        {
                            languageText = reader.GetString();
                        }
                        else
                        {
                            rejectionReason = "The classification 'language' is neither a string nor null.";
                            return false;
                        }

                        break;
                    default:
                        rejectionReason = $"The classification response carries an unknown field '{name}'.";
                        return false;
                }
            }

            if (reader.Read())
            {
                rejectionReason = "The classification response carries more than one JSON value.";
                return false;
            }
        }
        catch (JsonException)
        {
            rejectionReason = "The classification response is not well-formed JSON.";
            return false;
        }

        if (!statusSeen || !languageSeen)
        {
            rejectionReason = "The classification response is missing a required field.";
            return false;
        }

        if (!TryMapStatus(statusText, out var status))
        {
            rejectionReason = $"The classification 'status' value '{statusText}' is unknown.";
            return false;
        }

        if (!languageIsNull && !IsSupportedLanguage(languageText))
        {
            rejectionReason = "The classification 'language' value is not a supported language or null.";
            return false;
        }

        var language = languageIsNull ? null : languageText;
        var hint = NormalizeHint(validatedSourceHint);

        switch (status)
        {
            case EligibilityStatus.Eligible when language is null:
            case EligibilityStatus.Uncertain when language is not null:
            case EligibilityStatus.Unsupported when language is not null:
            case EligibilityStatus.Mixed when language is not null:
            case EligibilityStatus.Refused when language is not null:
                rejectionReason = $"The '{statusText}' classification carries an invalid language pairing.";
                return false;
            case EligibilityStatus.SourceMismatch when language is null || hint is null || string.Equals(language, hint, StringComparison.Ordinal):
                rejectionReason = "The 'source_mismatch' classification must carry a detected language differing from the hint.";
                return false;
            case EligibilityStatus.Eligible when hint is not null && !string.Equals(language, hint, StringComparison.Ordinal):
                rejectionReason = "The 'eligible' classification contradicts the validated source choice.";
                return false;
        }

        classification = new EligibilityClassification(status, language);
        rejectionReason = null;
        return true;
    }

    public static bool IsSupportedLanguage(string? value) =>
        string.Equals(value, "en", StringComparison.Ordinal)
        || string.Equals(value, "ru", StringComparison.Ordinal)
        || string.Equals(value, "ro", StringComparison.Ordinal)
        || string.Equals(value, "zh", StringComparison.Ordinal);

    private static string? NormalizeHint(string? validatedSourceHint) =>
        IsSupportedLanguage(validatedSourceHint) ? validatedSourceHint : null;

    private static bool TryMapStatus(string? value, out EligibilityStatus status)
    {
        switch (value)
        {
            case "eligible":
                status = EligibilityStatus.Eligible;
                return true;
            case "uncertain":
                status = EligibilityStatus.Uncertain;
                return true;
            case "unsupported":
                status = EligibilityStatus.Unsupported;
                return true;
            case "mixed":
                status = EligibilityStatus.Mixed;
                return true;
            case "source_mismatch":
                status = EligibilityStatus.SourceMismatch;
                return true;
            case "refused":
                status = EligibilityStatus.Refused;
                return true;
            default:
                status = default;
                return false;
        }
    }
}
