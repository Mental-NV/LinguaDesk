using System.Text;
using System.Text.Json;
using LinguaDesk.Core;

namespace LinguaDesk.Infrastructure.Ai;

public sealed record AlternativesEnvelope(bool Refused, IReadOnlyList<string>? Options);

public static class AlternativesEnvelopeParser
{
    public const string Revision = "alternatives-envelope-v2";

    // Comparison only: never alter the decoded source/options or their scalar spans.
    private static string WordingKey(string value) => string.Concat(value.EnumerateRunes()
        .Where(rune => !Rune.IsPunctuation(rune) && !ScalarInputPolicy.IsWhitespaceScalar(rune.Value)));

    public static bool TryParse(string? text, string selected, out AlternativesEnvelope? envelope, string? language = null)
    {
        envelope = null;
        if (text is null || Encoding.UTF8.GetByteCount(text) > AlternativesProfile.MaxResponseBytes) return false;
        try
        {
            using var document = JsonDocument.Parse(text, new JsonDocumentOptions { MaxDepth = 4 });
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return false;
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in root.EnumerateObject())
                if (!seen.Add(property.Name) || property.Name is not ("status" or "options")) return false;
            if (seen.Count != 2 || root.GetProperty("status").ValueKind != JsonValueKind.String) return false;
            var status = root.GetProperty("status").GetString();
            var options = root.GetProperty("options");
            if (status == "refused" && options.ValueKind == JsonValueKind.Null)
            {
                envelope = new(true, null);
                return true;
            }
            if (status != "result" || options.ValueKind != JsonValueKind.Array || options.GetArrayLength() != 2) return false;
            var values = new List<string>();
            var selectedKey = WordingKey(selected);
            var wordingKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var option in options.EnumerateArray())
            {
                if (option.ValueKind != JsonValueKind.String) return false;
                var value = option.GetString()!;
                var analysis = ScalarInputPolicy.Analyze(value, AlternativesInputPolicy.MaximumSentenceScalars);
                var spans = SentenceBoundaryPolicy.Segment(value, language);
                if (!analysis.IsValid || value.Contains('\0', StringComparison.Ordinal)
                    || spans.Count != 1 || spans[0].Start != 0 || spans[0].Length != analysis.ScalarCount
                    || string.Equals(value, selected, StringComparison.Ordinal)) return false;
                var key = WordingKey(value);
                if (string.Equals(key, selectedKey, StringComparison.Ordinal) || !wordingKeys.Add(key)) return false;
                values.Add(value);
            }
            if (string.Equals(values[0], values[1], StringComparison.Ordinal)) return false;
            envelope = new(false, values.AsReadOnly());
            return true;
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or ArgumentException)
        {
            return false;
        }
    }
}
