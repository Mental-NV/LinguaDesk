namespace LinguaDesk.Core;

public sealed record InputAnalysis(
    string Source,
    bool IsUnicodeValid,
    int? ScalarCount,
    bool IsEmptyOrWhitespace,
    bool IsOversized,
    int Excess,
    bool IsValid);

public sealed record TranslationInputValidation(
    InputAnalysis Input,
    string EffectiveSourceSelection,
    bool IsSourceSelectionValid,
    bool IsTargetValid,
    bool IsDistinctSelection,
    bool IsValid);

public sealed record RewritingInputValidation(
    InputAnalysis Input,
    string EffectiveSourceSelection,
    string EffectiveMode,
    bool IsSourceSelectionValid,
    bool IsModeValid,
    bool IsValid);

public static class ScalarInputPolicy
{
    public const string Id = "unicode-scalar-v1";

    public static IReadOnlyList<string> WhitespaceCodePointRanges { get; } = Array.AsReadOnly(
        new[]
        {
            "U+0009-U+000D",
            "U+0020",
            "U+0085",
            "U+00A0",
            "U+1680",
            "U+2000-U+200A",
            "U+2028-U+2029",
            "U+202F",
            "U+205F",
            "U+3000",
        });

    public static InputAnalysis Analyze(string source, int maximumSourceCharacters)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumSourceCharacters);

        var scalarCount = 0;
        var onlyWhitespace = true;

        for (var index = 0; index < source.Length; index++)
        {
            var current = source[index];
            int scalar;

            if (char.IsHighSurrogate(current))
            {
                if (index + 1 >= source.Length || !char.IsLowSurrogate(source[index + 1]))
                {
                    return InvalidUnicode(source);
                }

                scalar = char.ConvertToUtf32(current, source[++index]);
            }
            else if (char.IsLowSurrogate(current))
            {
                return InvalidUnicode(source);
            }
            else
            {
                scalar = current;
            }

            scalarCount++;
            onlyWhitespace &= IsWhitespaceScalar(scalar);
        }

        var emptyOrWhitespace = source.Length == 0 || onlyWhitespace;
        var excess = Math.Max(0, scalarCount - maximumSourceCharacters);
        return new(
            source,
            IsUnicodeValid: true,
            scalarCount,
            emptyOrWhitespace,
            IsOversized: excess > 0,
            excess,
            IsValid: !emptyOrWhitespace && excess == 0);
    }

    public static TranslationInputValidation ValidateTranslation(
        string source,
        int maximumSourceCharacters,
        string? sourceSelection,
        string? target)
    {
        var effectiveSource = sourceSelection ?? ProductCatalog.AutomaticSource;
        var sourceIsValid = ProductCatalog.IsSourceSelection(effectiveSource);
        var targetIsValid = ProductCatalog.IsLanguage(target);
        var selectionsAreDistinct = !sourceIsValid
            || !targetIsValid
            || string.Equals(effectiveSource, ProductCatalog.AutomaticSource, StringComparison.Ordinal)
            || !string.Equals(effectiveSource, target, StringComparison.Ordinal);
        var input = Analyze(source, maximumSourceCharacters);

        return new(
            input,
            effectiveSource,
            sourceIsValid,
            targetIsValid,
            selectionsAreDistinct,
            input.IsValid && sourceIsValid && targetIsValid && selectionsAreDistinct);
    }

    public static RewritingInputValidation ValidateRewriting(
        string source,
        int maximumSourceCharacters,
        string? sourceSelection,
        string? mode)
    {
        var effectiveSource = sourceSelection ?? ProductCatalog.AutomaticSource;
        var effectiveMode = mode ?? ProductCatalog.DefaultRewritingMode;
        var sourceIsValid = ProductCatalog.IsSourceSelection(effectiveSource);
        var modeIsValid = ProductCatalog.IsRewritingMode(effectiveMode);
        var input = Analyze(source, maximumSourceCharacters);

        return new(
            input,
            effectiveSource,
            effectiveMode,
            sourceIsValid,
            modeIsValid,
            input.IsValid && sourceIsValid && modeIsValid);
    }

    public static bool IsWhitespaceScalar(int scalar) =>
        scalar is >= 0x0009 and <= 0x000D
            or 0x0020
            or 0x0085
            or 0x00A0
            or 0x1680
            or >= 0x2000 and <= 0x200A
            or 0x2028
            or 0x2029
            or 0x202F
            or 0x205F
            or 0x3000;

    private static InputAnalysis InvalidUnicode(string source) =>
        new(
            source,
            IsUnicodeValid: false,
            ScalarCount: null,
            IsEmptyOrWhitespace: false,
            IsOversized: false,
            Excess: 0,
            IsValid: false);
}
