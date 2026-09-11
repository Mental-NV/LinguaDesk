using System.Text;
using System.Text.Json;

namespace LinguaDesk.Infrastructure.Ai;

public enum TranslationStatus
{
    Result,
    Refused,
}

public sealed record TranslationResult(TranslationStatus Status, string? Text);

public static class TranslationEnvelopeParser
{
    public const string PromptRevision = "translation.v1";

    public const int MaxRawBytes = 65536;

    private const int MaxDepth = 8;

    public static bool TryParse(
        string responseText,
        out TranslationResult? result,
        out string? rejectionReason)
    {
        result = null;

        if (responseText is null)
        {
            rejectionReason = "The translation response is missing.";
            return false;
        }

        if (Encoding.UTF8.GetByteCount(responseText) > MaxRawBytes)
        {
            rejectionReason = "The translation response exceeds the raw byte bound.";
            return false;
        }

        var trimmed = responseText.Trim();
        if (!trimmed.StartsWith('{'))
        {
            rejectionReason = "The translation response is not a single JSON object.";
            return false;
        }

        string? statusText = null;
        var statusSeen = false;
        string? resultText = null;
        var textIsNull = false;
        var textSeen = false;

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
                rejectionReason = "The translation response is not a single JSON object.";
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
                    rejectionReason = "The translation response has an unexpected shape.";
                    return false;
                }

                var name = reader.GetString();
                if (!reader.Read())
                {
                    rejectionReason = "The translation response ends inside a property.";
                    return false;
                }

                switch (name)
                {
                    case "status":
                        if (statusSeen)
                        {
                            rejectionReason = "The translation response repeats 'status'.";
                            return false;
                        }

                        statusSeen = true;
                        if (reader.TokenType != JsonTokenType.String)
                        {
                            rejectionReason = "The translation 'status' is not a string.";
                            return false;
                        }

                        statusText = reader.GetString();
                        break;
                    case "text":
                        if (textSeen)
                        {
                            rejectionReason = "The translation response repeats 'text'.";
                            return false;
                        }

                        textSeen = true;
                        if (reader.TokenType == JsonTokenType.Null)
                        {
                            textIsNull = true;
                        }
                        else if (reader.TokenType == JsonTokenType.String)
                        {
                            resultText = reader.GetString();
                        }
                        else
                        {
                            rejectionReason = "The translation 'text' is neither a string nor null.";
                            return false;
                        }

                        break;
                    default:
                        rejectionReason = $"The translation response carries an unknown field '{name}'.";
                        return false;
                }
            }

            if (reader.Read())
            {
                rejectionReason = "The translation response carries more than one JSON value.";
                return false;
            }
        }
        catch (JsonException)
        {
            rejectionReason = "The translation response is not well-formed JSON.";
            return false;
        }

        if (!statusSeen || !textSeen)
        {
            rejectionReason = "The translation response is missing a required field.";
            return false;
        }

        if (!TryMapStatus(statusText, out var status))
        {
            rejectionReason = $"The translation 'status' value '{statusText}' is unknown.";
            return false;
        }

        switch (status)
        {
            case TranslationStatus.Result when textIsNull || string.IsNullOrWhiteSpace(resultText):
                rejectionReason = "The 'result' translation requires non-whitespace complete text.";
                return false;
            case TranslationStatus.Refused when !textIsNull:
                rejectionReason = "The 'refused' translation requires null text.";
                return false;
        }

        result = new TranslationResult(status, textIsNull ? null : resultText);
        rejectionReason = null;
        return true;
    }

    private static bool TryMapStatus(string? value, out TranslationStatus status)
    {
        switch (value)
        {
            case "result":
                status = TranslationStatus.Result;
                return true;
            case "refused":
                status = TranslationStatus.Refused;
                return true;
            default:
                status = default;
                return false;
        }
    }
}
