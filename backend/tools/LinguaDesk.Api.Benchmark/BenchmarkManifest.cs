using System.Text.Json;
using LinguaDesk.Core;

namespace LinguaDesk.Api.Benchmark;

/// <summary>
/// Declared rehearsal composition: family cells, length bands, distinct
/// combinations, the repeat-with-fresh-identity rule and the interleave seed.
/// </summary>
public sealed record LengthBand(string Id, int MinCanonicalChars, int MaxCanonicalChars);

public sealed record TranslationCombo(string Id, string Band, int TargetLength, string SourceSelection, string BaseId);

public sealed record TranslationDirection(string From, string To, IReadOnlyList<TranslationCombo> Combos);

public sealed record RewritingCombo(string Id, string Band, int TargetLength, string SourceSelection, string BaseId);

public sealed record RewritingCell(string Language, string Mode, IReadOnlyList<RewritingCombo> Combos);

public sealed record SuccessorMapping(string RuleId, string? RehearsedAs, string? SuccessorOnly);

/// <summary>
/// One expanded measured request: the original or its fresh-identity repeat.
/// Repeats share the combo identity and source text; the submission identity
/// is minted at run time so no two submissions ever share an operation id.
/// </summary>
public sealed record WorkItem(
    string ComboId,
    string Family,
    string Route,
    string BandId,
    string? SourceSelection,
    string? Target,
    string? Mode,
    string SourceText,
    string SourceSha256,
    int SourceLength,
    bool IsRepeat,
    int SequenceIndex);

public sealed record BenchmarkManifest
{
    public required string SchemaRevision { get; init; }

    public required string WorkloadName { get; init; }

    public required int Concurrency { get; init; }

    public required int InterleaveSeed { get; init; }

    public required int TargetMs { get; init; }

    public required IReadOnlyList<LengthBand> LengthBands { get; init; }

    public required IReadOnlyList<TranslationDirection> TranslationDirections { get; init; }

    public required IReadOnlyList<RewritingCell> RewritingCells { get; init; }

    public required IReadOnlyDictionary<string, string> Bases { get; init; }

    public required IReadOnlyList<SuccessorMapping> SuccessorMapping { get; init; }
}

public static class ManifestLoader
{
    private static readonly JsonSerializerOptions LoaderOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    public static BenchmarkManifest Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var json = File.ReadAllText(path);
        var document = JsonSerializer.Deserialize<ManifestDocument>(json, LoaderOptions)
            ?? throw new InvalidOperationException($"The benchmark manifest '{path}' is empty.");
        return document.ToManifest(path);
    }

    public static IReadOnlyList<WorkItem> Expand(BenchmarkManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        var bands = manifest.LengthBands.ToDictionary(band => band.Id, StringComparer.Ordinal);
        var items = new List<WorkItem>();

        foreach (var direction in manifest.TranslationDirections)
        {
            ValidateDirection(direction);
            foreach (var combo in direction.Combos)
            {
                var band = RequireBand(bands, combo);
                var source = BuildSource(manifest, combo.BaseId, combo.TargetLength, band, combo.Id);
                ValidateSelection(combo.SourceSelection, combo.Id);
                var route = $"{direction.From}->{direction.To}";
                items.Add(new WorkItem(
                    combo.Id, "translation", route, band.Id,
                    NormalizeSelection(combo.SourceSelection), direction.To, null,
                    source.Text, source.Hash, source.Length, IsRepeat: false, SequenceIndex: -1));
            }
        }

        foreach (var cell in manifest.RewritingCells)
        {
            ValidateCell(cell);
            foreach (var combo in cell.Combos)
            {
                var band = RequireBand(bands, combo);
                var source = BuildSource(manifest, combo.BaseId, combo.TargetLength, band, combo.Id);
                ValidateSelection(combo.SourceSelection, combo.Id);
                var route = $"{cell.Language}/{cell.Mode}";
                items.Add(new WorkItem(
                    combo.Id, "rewriting", route, band.Id,
                    NormalizeSelection(combo.SourceSelection), null, cell.Mode,
                    source.Text, source.Hash, source.Length, IsRepeat: false, SequenceIndex: -1));
            }
        }

        AssertDistinct(items.Select(item => item.ComboId), "combo id");
        AssertDistinct(items.Select(item => item.SourceSha256), "distinct source text");

        var ordered = items
            .SelectMany(item => new[]
            {
                item,
                item with { IsRepeat = true },
            })
            .ToList();

        var shuffled = new List<WorkItem>(ordered);
        var random = new Random(manifest.InterleaveSeed);
        for (var index = shuffled.Count - 1; index > 0; index--)
        {
            var swap = random.Next(index + 1);
            (shuffled[index], shuffled[swap]) = (shuffled[swap], shuffled[index]);
        }

        return shuffled
            .Select((item, sequence) => item with { SequenceIndex = sequence })
            .ToList();
    }

    private static (string Text, string Hash, int Length) BuildSource(
        BenchmarkManifest manifest, string baseId, int targetLength, LengthBand band, string comboId)
    {
        if (!manifest.Bases.TryGetValue(baseId, out var baseText) || string.IsNullOrWhiteSpace(baseText))
        {
            throw new InvalidOperationException($"Combo '{comboId}' references unknown base text '{baseId}'.");
        }

        if (targetLength < band.MinCanonicalChars || targetLength > band.MaxCanonicalChars)
        {
            throw new InvalidOperationException(
                $"Combo '{comboId}' targets {targetLength} chars outside band '{band.Id}'.");
        }

        var text = SourceSynthesis.Synthesize(baseText, targetLength);
        var length = SourceSynthesis.CountScalars(text);
        if (length != targetLength)
        {
            throw new InvalidOperationException($"Combo '{comboId}' synthesized {length} chars, expected {targetLength}.");
        }

        var analysis = ScalarInputPolicy.Analyze(text, ProductCatalog.TranslationMaximumSourceCharacters);
        if (analysis.ScalarCount != length)
        {
            throw new InvalidOperationException($"Combo '{comboId}' disagrees with the canonical scalar count.");
        }

        return (text, SourceSynthesis.Sha256Hex(text), length);
    }

    private static LengthBand RequireBand(Dictionary<string, LengthBand> bands, TranslationCombo combo) =>
        RequireBand(bands, combo.Band, combo.Id);

    private static LengthBand RequireBand(Dictionary<string, LengthBand> bands, RewritingCombo combo) =>
        RequireBand(bands, combo.Band, combo.Id);

    private static LengthBand RequireBand(Dictionary<string, LengthBand> bands, string bandId, string comboId)
    {
        if (!bands.TryGetValue(bandId, out var band))
        {
            throw new InvalidOperationException($"Combo '{comboId}' references unknown band '{bandId}'.");
        }

        return band;
    }

    private static void ValidateDirection(TranslationDirection direction)
    {
        if (!ProductCatalog.IsLanguage(direction.From) || !ProductCatalog.IsLanguage(direction.To))
        {
            throw new InvalidOperationException($"Unknown translation direction '{direction.From}->{direction.To}'.");
        }

        if (string.Equals(direction.From, direction.To, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Translation direction '{direction.From}->{direction.To}' needs distinct ends.");
        }
    }

    private static void ValidateCell(RewritingCell cell)
    {
        if (!ProductCatalog.IsLanguage(cell.Language))
        {
            throw new InvalidOperationException($"Unknown rewriting language '{cell.Language}'.");
        }

        if (!ProductCatalog.IsRewritingMode(cell.Mode))
        {
            throw new InvalidOperationException($"Unknown rewriting mode '{cell.Mode}'.");
        }
    }

    private static void ValidateSelection(string selection, string comboId)
    {
        if (!string.Equals(selection, ProductCatalog.AutomaticSource, StringComparison.Ordinal)
            && !ProductCatalog.IsSourceSelection(selection))
        {
            throw new InvalidOperationException($"Combo '{comboId}' carries unknown source selection '{selection}'.");
        }
    }

    private static string? NormalizeSelection(string selection) =>
        string.Equals(selection, ProductCatalog.AutomaticSource, StringComparison.Ordinal) ? null : selection;

    private static void AssertDistinct(IEnumerable<string> values, string what)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var value in values)
        {
            if (!seen.Add(value))
            {
                throw new InvalidOperationException($"The manifest repeats {what} '{value}'.");
            }
        }
    }

    private sealed class ManifestDocument
    {
        public string? SchemaRevision { get; set; }

        public string? WorkloadName { get; set; }

        public int Concurrency { get; set; }

        public int InterleaveSeed { get; set; }

        public int TargetMs { get; set; }

        public List<LengthBandRecord>? LengthBands { get; set; }

        public TranslationSection? Translation { get; set; }

        public RewritingSection? Rewriting { get; set; }

        public Dictionary<string, string>? Bases { get; set; }

        public List<SuccessorMapping>? SuccessorMapping { get; set; }

        public BenchmarkManifest ToManifest(string path)
        {
            if (!string.Equals(SchemaRevision, "benchmark-manifest.v1", StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Manifest '{path}' carries unknown schema '{SchemaRevision}'.");
            }

            if (string.IsNullOrWhiteSpace(WorkloadName))
            {
                throw new InvalidOperationException($"Manifest '{path}' is missing its workload name.");
            }

            if (Concurrency <= 0 || TargetMs <= 0)
            {
                throw new InvalidOperationException($"Manifest '{path}' carries a non-positive concurrency or target.");
            }

            if (LengthBands is null || LengthBands.Count == 0)
            {
                throw new InvalidOperationException($"Manifest '{path}' declares no length bands.");
            }

            if (Translation?.Directions is null || Translation.Directions.Count == 0)
            {
                throw new InvalidOperationException($"Manifest '{path}' declares no translation directions.");
            }

            if (Rewriting?.Cells is null || Rewriting.Cells.Count == 0)
            {
                throw new InvalidOperationException($"Manifest '{path}' declares no rewriting cells.");
            }

            if (Bases is null || Bases.Count == 0)
            {
                throw new InvalidOperationException($"Manifest '{path}' declares no base texts.");
            }

            return new BenchmarkManifest
            {
                SchemaRevision = SchemaRevision!,
                WorkloadName = WorkloadName!,
                Concurrency = Concurrency,
                InterleaveSeed = InterleaveSeed,
                TargetMs = TargetMs,
                LengthBands = LengthBands
                    .Select(band => new LengthBand(band.Id!, band.MinCanonicalChars, band.MaxCanonicalChars))
                    .ToList(),
                TranslationDirections = Translation.Directions
                    .Select(direction => new TranslationDirection(
                        direction.From!,
                        direction.To!,
                        direction.Combos!.Select(combo => new TranslationCombo(
                            combo.Id!, combo.Band!, combo.TargetLength, combo.SourceSelection!, combo.BaseId!)).ToList()))
                    .ToList(),
                RewritingCells = Rewriting.Cells
                    .Select(cell => new RewritingCell(
                        cell.Language!,
                        cell.Mode!,
                        cell.Combos!.Select(combo => new RewritingCombo(
                            combo.Id!, combo.Band!, combo.TargetLength, combo.SourceSelection!, combo.BaseId!)).ToList()))
                    .ToList(),
                Bases = new Dictionary<string, string>(Bases, StringComparer.Ordinal),
                SuccessorMapping = SuccessorMapping ?? [],
            };
        }
    }

    private sealed class LengthBandRecord
    {
        public string? Id { get; set; }

        public int MinCanonicalChars { get; set; }

        public int MaxCanonicalChars { get; set; }
    }

    private sealed class TranslationSection
    {
        public List<DirectionRecord>? Directions { get; set; }
    }

    private sealed class DirectionRecord
    {
        public string? From { get; set; }

        public string? To { get; set; }

        public List<ComboRecord>? Combos { get; set; }
    }

    private sealed class RewritingSection
    {
        public List<CellRecord>? Cells { get; set; }
    }

    private sealed class CellRecord
    {
        public string? Language { get; set; }

        public string? Mode { get; set; }

        public List<ComboRecord>? Combos { get; set; }
    }

    private sealed class ComboRecord
    {
        public string? Id { get; set; }

        public string? Band { get; set; }

        public int TargetLength { get; set; }

        public string? SourceSelection { get; set; }

        public string? BaseId { get; set; }
    }
}
