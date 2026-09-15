using System.Security.Cryptography;
using System.Text;
using LinguaDesk.Infrastructure.Ai;

namespace LinguaDesk.Ai.Evaluation;

internal static class BatchReviewSelector
{
    // Coverage strata are derived per batch: every tag carried by at least two
    // cases must appear in the sample. For B1 this yields exactly the pinned
    // M036 classes (fidelity-risk, paragraph/list, instruction-as-content,
    // upper-length-band); singleton attack tags in guardrail batches stay
    // out of the requirement while recurrent classes are still covered.
    private static List<string> RequiredTags(IReadOnlyList<BatchCase> cases)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        var order = new List<string>();
        foreach (var kase in cases)
        {
            foreach (var tag in kase.CoverageTags)
            {
                if (!counts.TryGetValue(tag, out var count))
                {
                    counts[tag] = 1;
                    order.Add(tag);
                }
                else
                {
                    counts[tag] = count + 1;
                }
            }
        }

        return order.Where(tag => counts[tag] >= 2).ToList();
    }

    public static BatchReviewSelection Select(IReadOnlyList<BatchCase> cases, string seed)
    {
        ArgumentNullException.ThrowIfNull(cases);
        ArgumentException.ThrowIfNullOrWhiteSpace(seed);

        var requiredTags = RequiredTags(cases);
        var translations = cases.Where(item => item.Family == "translation").ToArray();
        var rewritings = cases.Where(item => item.Family == "rewriting").ToArray();
        if (translations.Length < 6 || rewritings.Length < 6)
        {
            throw new InvalidOperationException("The review selector requires at least six cases in each family.");
        }

        var ranked = cases
            .OrderBy(item => Rank(seed, item.Id), StringComparer.Ordinal)
            .ThenBy(item => item.Id, StringComparer.Ordinal)
            .Select((item, index) => (item.Id, Index: index))
            .ToDictionary(item => item.Id, item => item.Index, StringComparer.Ordinal);
        var shortest = cases
            .OrderBy(item => item.SourceText.EnumerateRunes().Count())
            .ThenBy(item => item.Id, StringComparer.Ordinal)
            .Take(6)
            .Select(item => item.Id)
            .ToHashSet(StringComparer.Ordinal);
        var longest = cases
            .OrderByDescending(item => item.SourceText.EnumerateRunes().Count())
            .ThenBy(item => item.Id, StringComparer.Ordinal)
            .Take(6)
            .Select(item => item.Id)
            .ToHashSet(StringComparer.Ordinal);

        BatchCase[]? best = null;
        var bestScore = int.MaxValue;
        string? bestTie = null;
        foreach (var translationSet in Combinations(translations, 6))
        {
            foreach (var rewritingSet in Combinations(rewritings, 6))
            {
                var selected = translationSet.Concat(rewritingSet).ToArray();
                if (!SatisfiesStrata(selected, requiredTags, shortest, longest))
                {
                    continue;
                }

                var score = selected.Sum(item => ranked[item.Id]);
                var tie = string.Join("\n", selected.Select(item => item.Id).Order(StringComparer.Ordinal));
                if (score < bestScore
                    || (score == bestScore && string.CompareOrdinal(tie, bestTie) < 0))
                {
                    best = selected;
                    bestScore = score;
                    bestTie = tie;
                }
            }
        }

        if (best is null)
        {
            throw new InvalidOperationException("No 12-case review sample satisfies the coverage strata.");
        }

        var selectedIds = best
            .OrderBy(item => ranked[item.Id])
            .ThenBy(item => item.Id, StringComparer.Ordinal)
            .Select(item => item.Id)
            .ToArray();
        return new BatchReviewSelection(
            seed,
            Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(seed))),
            "sha256-rank.v1",
            selectedIds,
            requiredTags,
            IncludesSimplifiedChinese: best.Any(item => item.ChineseScript == "simplified"),
            IncludesTraditionalChinese: best.Any(item => item.ChineseScript == "traditional"),
            TranslationCount: best.Count(item => item.Family == "translation"),
            RewritingCount: best.Count(item => item.Family == "rewriting"),
            IncludesShortLengthBand: best.Any(item => shortest.Contains(item.Id)),
            IncludesLongLengthBand: best.Any(item => longest.Contains(item.Id)));
    }

    private static bool SatisfiesStrata(
        IReadOnlyList<BatchCase> selected,
        IReadOnlyList<string> requiredTags,
        HashSet<string> shortest,
        HashSet<string> longest) =>
        selected.Any(item => item.ChineseScript == "simplified")
        && selected.Any(item => item.ChineseScript == "traditional")
        && selected.Any(item => shortest.Contains(item.Id))
        && selected.Any(item => longest.Contains(item.Id))
        && requiredTags.All(tag => selected.Any(item => item.CoverageTags.Contains(tag, StringComparer.Ordinal)));

    private static string Rank(string seed, string caseId) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes($"{seed}\0{caseId}")));

    private static IEnumerable<IReadOnlyList<BatchCase>> Combinations(BatchCase[] cases, int count)
    {
        var indexes = Enumerable.Range(0, count).ToArray();
        while (true)
        {
            yield return indexes.Select(index => cases[index]).ToArray();

            var position = count - 1;
            while (position >= 0 && indexes[position] == cases.Length - count + position)
            {
                position--;
            }

            if (position < 0)
            {
                yield break;
            }

            indexes[position]++;
            for (var index = position + 1; index < count; index++)
            {
                indexes[index] = indexes[index - 1] + 1;
            }
        }
    }
}

internal sealed record BatchReviewSelection(
    string Seed,
    string SeedSha256,
    string Algorithm,
    IReadOnlyList<string> PreselectedCaseIds,
    IReadOnlyList<string> RequiredCoverageTags,
    bool IncludesSimplifiedChinese,
    bool IncludesTraditionalChinese,
    int TranslationCount,
    int RewritingCount,
    bool IncludesShortLengthBand,
    bool IncludesLongLengthBand);
