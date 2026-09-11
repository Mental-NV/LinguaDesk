using System.Text;
using System.Text.Json;

namespace LinguaDesk.Infrastructure.Ai;

public enum RewritingStatus
{
    Result,
    Refused,
}

public sealed record RewritingResult(RewritingStatus Status, string? Text);

public static class RewritingEnvelopeParser
{
    public const string PromptRevision = "rewriting.v1";

    public const int MaxRawBytes = 65536;

    private const int MaxDepth = 8;

    public static bool TryParse(
        string responseText,
        out RewritingResult? result,
        out string? rejectionReason)
    {
        result = null;

        if (responseText is null)
        {
            rejectionReason = "The rewriting response is missing.";
            return false;
        }

        if (Encoding.UTF8.GetByteCount(responseText) > MaxRawBytes)
        {
            rejectionReason = "The rewriting response exceeds the raw byte bound.";
            return false;
        }

        var trimmed = responseText.Trim();
        if (!trimmed.StartsWith('{'))
        {
            rejectionReason = "The rewriting response is not a single JSON object.";
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
                rejectionReason = "The rewriting response is not a single JSON object.";
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
                    rejectionReason = "The rewriting response has an unexpected shape.";
                    return false;
                }

                var name = reader.GetString();
                if (!reader.Read())
                {
                    rejectionReason = "The rewriting response ends inside a property.";
                    return false;
                }

                switch (name)
                {
                    case "status":
                        if (statusSeen)
                        {
                            rejectionReason = "The rewriting response repeats 'status'.";
                            return false;
                        }

                        statusSeen = true;
                        if (reader.TokenType != JsonTokenType.String)
                        {
                            rejectionReason = "The rewriting 'status' is not a string.";
                            return false;
                        }

                        statusText = reader.GetString();
                        break;
                    case "text":
                        if (textSeen)
                        {
                            rejectionReason = "The rewriting response repeats 'text'.";
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
                            rejectionReason = "The rewriting 'text' is neither a string nor null.";
                            return false;
                        }

                        break;
                    default:
                        rejectionReason = $"The rewriting response carries an unknown field '{name}'.";
                        return false;
                }
            }

            if (reader.Read())
            {
                rejectionReason = "The rewriting response carries more than one JSON value.";
                return false;
            }
        }
        catch (JsonException)
        {
            rejectionReason = "The rewriting response is not well-formed JSON.";
            return false;
        }

        if (!statusSeen || !textSeen)
        {
            rejectionReason = "The rewriting response is missing a required field.";
            return false;
        }

        if (!TryMapStatus(statusText, out var status))
        {
            rejectionReason = $"The rewriting 'status' value '{statusText}' is unknown.";
            return false;
        }

        switch (status)
        {
            case RewritingStatus.Result when textIsNull || string.IsNullOrWhiteSpace(resultText):
                rejectionReason = "The 'result' rewriting requires non-whitespace complete text.";
                return false;
            case RewritingStatus.Refused when !textIsNull:
                rejectionReason = "The 'refused' rewriting requires null text.";
                return false;
        }

        result = new RewritingResult(status, textIsNull ? null : resultText);
        rejectionReason = null;
        return true;
    }

    private static bool TryMapStatus(string? value, out RewritingStatus status)
    {
        switch (value)
        {
            case "result":
                status = RewritingStatus.Result;
                return true;
            case "refused":
                status = RewritingStatus.Refused;
                return true;
            default:
                status = default;
                return false;
        }
    }
}
