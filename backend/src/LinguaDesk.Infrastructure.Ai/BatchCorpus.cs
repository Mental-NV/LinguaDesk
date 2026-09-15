using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LinguaDesk.Infrastructure.Ai;

public sealed class BatchCorpusException : InvalidOperationException
{
    public BatchCorpusException(string message)
        : base(message)
    {
    }
}

public sealed record BatchCase(
    string Id,
    string Family,
    string SourceText,
    string? SourceSelection,
    string? Target,
    string? Language,
    string? Mode,
    string ExpectedEligibility,
    IReadOnlyList<string> MeaningAssertions,
    string AcceptableOutputNotes,
    IReadOnlyList<string> CoverageTags,
    string? ChineseScript);

public static class BatchCorpus
{
    public const string ExpectedSchemaRevision = "corpus-b1.v1";

    public const string ExpectedBatchId = "B1";

    public const string ExpectedSecurityBatchId = "SECURITY";

    public const int ExpectedCaseCount = 24;

    // B1 content pin, checked by M036 spec AC-001. M035 froze
    // `aebdc2dc…` (see docs/08-backlogs/M035/tasks.md); re-pinned by owner
    // request 2026-09-13 after rewriting sources gained grammar/style errors
    // (plus the in-progress b1-tr-ru-ro source edit), then again after
    // `b1-rw-ru-academic` was shortened to fit the adapter input bound.
    // Any further case change requires a new reviewed batch revision,
    // never a silent edit.
    public const string FrozenContentSha256 =
        "245b8f76df8f2dd10c5d7177d7679cf87d17c817a135950eba0947a72bd7a4cc";

    // Guardrail batch pin, added by owner request 2026-09-13 for
    // evaluate-batch --corpus .../batch-security.json runs. Same freeze
    // discipline as B1: any case change needs a reviewed revision, never
    // a silent edit.
    public const string FrozenSecurityContentSha256 =
        "2f22484f75ad5c7f95e2b490c79a50a15f72c1cc59ca431cef83ddc75e91d263";

    public static string? FrozenPinForBatchId(string? batchId)
    {
        if (string.Equals(batchId, ExpectedBatchId, StringComparison.Ordinal))
        {
            return FrozenContentSha256;
        }

        if (string.Equals(batchId, ExpectedSecurityBatchId, StringComparison.Ordinal))
        {
            return FrozenSecurityContentSha256;
        }

        return null;
    }

    public static string ComputeContentSha256(byte[] content)
    {
        ArgumentNullException.ThrowIfNull(content);
        return Convert.ToHexStringLower(SHA256.HashData(content));
    }

    public static bool IsFrozenRevision(byte[] content) =>
        string.Equals(
            ComputeContentSha256(content),
            FrozenContentSha256,
            StringComparison.OrdinalIgnoreCase);

    public static bool IsFrozenRevision(byte[] content, string? batchId)
    {
        var frozen = FrozenPinForBatchId(batchId);
        return frozen is not null
            && string.Equals(
                ComputeContentSha256(content),
                frozen,
                StringComparison.OrdinalIgnoreCase);
    }

    public static string? ReadBatchId(string json)
    {
        try
        {
            using var batch = JsonDocument.Parse(json);
            if (batch.RootElement.TryGetProperty("batchId", out var batchId)
                && batchId.ValueKind == JsonValueKind.String)
            {
                return batchId.GetString();
            }
        }
        catch (JsonException)
        {
        }

        return null;
    }

    public static IReadOnlyList<BatchCase> Load(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        JsonDocument batch;
        try
        {
            batch = JsonDocument.Parse(json);
        }
        catch (JsonException exception)
        {
            throw new BatchCorpusException($"Batch corpus is not valid JSON: {exception.Message}");
        }

        using (batch)
        {
            var root = batch.RootElement;
            var schema = GetRequiredString(root, "schemaRevision");
            if (!string.Equals(schema, ExpectedSchemaRevision, StringComparison.Ordinal))
            {
                throw new BatchCorpusException(
                    $"Batch B1 schema '{schema}' does not match '{ExpectedSchemaRevision}'.");
            }

            var batchId = GetRequiredString(root, "batchId");
            if (!string.Equals(batchId, ExpectedBatchId, StringComparison.Ordinal)
                && !string.Equals(batchId, ExpectedSecurityBatchId, StringComparison.Ordinal))
            {
                throw new BatchCorpusException(
                    $"Batch batchId '{batchId}' does not match '{ExpectedBatchId}' or '{ExpectedSecurityBatchId}'.");
            }

            if (!root.TryGetProperty("cases", out var cases) || cases.ValueKind != JsonValueKind.Array)
            {
                throw new BatchCorpusException($"Batch '{batchId}' has no 'cases' array.");
            }

            var parsed = cases.EnumerateArray().Select(kase => ParseCase(kase, batchId)).ToList();
            if (parsed.Count != ExpectedCaseCount)
            {
                throw new BatchCorpusException(
                    $"Batch '{batchId}' holds {parsed.Count} cases; {ExpectedCaseCount} are required.");
            }

            return parsed.AsReadOnly();
        }
    }

    // Deterministic digit-token preservation finding. Digits normally survive
    // translation/rewriting verbatim, so a dropped numeric token is a fidelity
    // flag for the human reviewer, not a grade by itself.
    public static IReadOnlySet<string> DigitTokens(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var tokens = new HashSet<string>(StringComparer.Ordinal);
        var current = new StringBuilder();
        foreach (var rune in text)
        {
            if (char.IsAsciiDigit(rune) || (current.Length > 0 && (rune == ':' || rune == '.' || rune == ',')))
            {
                current.Append(rune);
            }
            else
            {
                Flush();
            }
        }

        Flush();
        return tokens;

        void Flush()
        {
            var token = current.ToString().Trim(':').Trim('.').Trim(',');
            if (token.Length > 0 && token.Any(char.IsAsciiDigit))
            {
                tokens.Add(token);
            }

            current.Clear();
        }
    }

    private static BatchCase ParseCase(JsonElement kase, string batchId)
    {
        var id = GetRequiredString(kase, "id");
        var family = GetRequiredString(kase, "family");
        var source = GetRequiredString(kase, "sourceText");
        if (string.IsNullOrWhiteSpace(source))
        {
            throw new BatchCorpusException($"Batch '{batchId}' case '{id}' has a blank sourceText.");
        }

        string? sourceSelection = null;
        string? target = null;
        string? language = null;
        string? mode = null;
        if (kase.TryGetProperty("operation", out var operation) && operation.ValueKind == JsonValueKind.Object)
        {
            sourceSelection = GetOptionalString(operation, "sourceSelection");
            target = GetOptionalString(operation, "target");
            language = GetOptionalString(operation, "language");
            mode = GetOptionalString(operation, "mode");
        }

        var expectedEligibility = GetRequiredString(kase, "expectedEligibility");
        var assertions = kase.TryGetProperty("meaningAssertions", out var notes) && notes.ValueKind == JsonValueKind.Array
            ? notes.EnumerateArray().Select(note => note.GetString() ?? string.Empty).ToList()
            : [];
        var outputNotes = GetRequiredString(kase, "acceptableOutputNotes");
        var tags = kase.TryGetProperty("coverageTags", out var rawTags) && rawTags.ValueKind == JsonValueKind.Array
            ? rawTags.EnumerateArray().Select(tag => tag.GetString() ?? string.Empty).ToList()
            : [];
        string? chineseScript = null;
        if (kase.TryGetProperty("chineseScript", out var script))
        {
            chineseScript = script.GetString();
        }

        return new BatchCase(
            id, family, source, sourceSelection, target, language, mode,
            expectedEligibility, assertions.AsReadOnly(), outputNotes,
            tags.AsReadOnly(), chineseScript);
    }

    private static string GetRequiredString(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value) || value.ValueKind != JsonValueKind.String)
        {
            throw new BatchCorpusException($"Batch entry is missing required string '{property}'.");
        }

        return value.GetString() ?? string.Empty;
    }

    private static string? GetOptionalString(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
