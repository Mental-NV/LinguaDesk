using System.Text;
using System.Text.RegularExpressions;

namespace LinguaDesk.Core;

public sealed record SentenceSpan(int Start, int Length);

/// <summary>Versioned scalar spans; gaps and source text are never normalized.</summary>
public static class SentenceBoundaryPolicy
{
    public const string Revision = "sentence-boundaries-v1";
    private static readonly HashSet<string> Abbreviations = new(StringComparer.OrdinalIgnoreCase)
    { "mr.", "mrs.", "ms.", "dr.", "prof.", "e.g.", "i.e.", "etc.", "dl.", "dna.", "г.", "ул.", "т.д.", "т.п." };
    private const string Closers = "\"'”’»)]}」』】）";

    public static IReadOnlyList<SentenceSpan> Segment(string text)
    {
        if (!ScalarInputPolicy.Analyze(text, int.MaxValue).IsUnicodeValid) return [];
        var scalars = text.EnumerateRunes().Select(r => r.ToString()).ToArray();
        var spans = new List<SentenceSpan>();
        var start = 0;
        void Emit(int end)
        {
            while (start < end && White(scalars[start])) start++;
            while (end > start && White(scalars[end - 1])) end--;
            if (end > start) spans.Add(new(start, end - start));
        }
        for (var i = 0; i < scalars.Length; i++)
        {
            var ch = scalars[i];
            if (ch is "\r" or "\n" or "\u2028" or "\u2029")
            {
                Emit(i);
                if (ch == "\r" && i + 1 < scalars.Length && scalars[i + 1] == "\n") i++;
                start = i + 1;
                continue;
            }
            if (!".!?。！？".Contains(ch, StringComparison.Ordinal)) continue;
            var end = i + 1;
            while (end < scalars.Length && "!?。！？".Contains(scalars[end], StringComparison.Ordinal)) end++;
            while (end < scalars.Length && Closers.Contains(scalars[end], StringComparison.Ordinal)) end++;
            if (ch == ".")
            {
                if (end < scalars.Length && !White(scalars[end])) continue;
                var prefix = string.Concat(scalars.Take(i + 1));
                var token = Regex.Match(prefix, @"[\p{L}.]+\.$", RegexOptions.CultureInvariant).Value;
                var hasNextWord = end < scalars.Length && Regex.IsMatch(string.Concat(scalars.Skip(end)), @"^[^\S\r\n]*[\p{L}\p{N}]", RegexOptions.CultureInvariant);
                if (Abbreviations.Contains(token)
                    || Regex.IsMatch(token, @"^(?:\p{L}\.){2,}$", RegexOptions.CultureInvariant)
                    || (Regex.IsMatch(token, @"^\p{L}\.$", RegexOptions.CultureInvariant) && hasNextWord)) continue;
            }
            Emit(end);
            start = end;
            i = end - 1;
        }
        Emit(scalars.Length);
        return spans;
    }

    public static string Slice(string text, int start, int length) =>
        string.Concat(text.EnumerateRunes().Skip(start).Take(length).Select(r => r.ToString()));

    private static bool White(string scalar) => ScalarInputPolicy.IsWhitespaceScalar(Rune.GetRuneAt(scalar, 0).Value);
}

public sealed record AlternativesInputValidation(bool IsValid, string? Reason, string? SelectedText, int CharacterCount, string EffectiveMode);

public static class AlternativesInputPolicy
{
    public const int MaximumContextScalars = 8000;
    public const int MaximumSentenceScalars = 2000;
    public const int RequestBodyBytes = 131072;
    public const int DeadlineSeconds = 15;
    public const int OptionCount = 2;

    public static AlternativesInputValidation Validate(string context, int start, int length, Guid association, string language, string? mode = null)
    {
        var effectiveMode = mode ?? ProductCatalog.DefaultRewritingMode;
        var input = ScalarInputPolicy.Analyze(context, MaximumContextScalars);
        string? reason = !input.IsUnicodeValid ? "invalidUnicode"
            : context.Contains('\0', StringComparison.Ordinal) ? "invalidContext"
            : input.IsEmptyOrWhitespace ? "emptyContext"
            : input.IsOversized ? "oversizedContext"
            : !ProductCatalog.IsLanguage(language) ? "invalidLanguage"
            : !ProductCatalog.IsRewritingMode(effectiveMode) ? "invalidMode"
            : association == Guid.Empty ? "invalidAssociation"
            : start < 0 || length <= 0 || length > MaximumSentenceScalars || (long)start + length > input.ScalarCount ? "invalidSpan"
            : !SentenceBoundaryPolicy.Segment(context).Contains(new SentenceSpan(start, length)) ? "invalidSentence"
            : null;
        return new(reason is null, reason, reason is null ? SentenceBoundaryPolicy.Slice(context, start, length) : null, reason is null ? length : 0, effectiveMode);
    }
}
