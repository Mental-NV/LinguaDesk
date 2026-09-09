using System.Collections.ObjectModel;

namespace LinguaDesk.Core;

public sealed record LanguageDefinition(string Id, string Name);

public sealed record TranslationDirection(string Source, string Target);

public sealed record RewritingModeDefinition(string Id, string Name, string Kind);

public static class ProductCatalog
{
    public const string AutomaticSource = "auto";
    public const string DefaultRewritingMode = "correctionOnly";
    public const int TranslationMaximumSourceCharacters = 5000;
    public const int RewritingMaximumSourceCharacters = 2000;
    public const int OverallDeadlineSeconds = 30;
    public const int OperationIdentityValidForSeconds = 86400;
    public const int OperationIdentityMaximumFutureSkewSeconds = 300;
    public static string ChineseOutputScript { get; } = "simplified";

    public static ReadOnlyCollection<LanguageDefinition> Languages { get; } = Array.AsReadOnly(
    new LanguageDefinition[]
    {
        new("en", "English"),
        new("ru", "Russian"),
        new("ro", "Romanian"),
        new("zh", "Chinese"),
    });

    public static ReadOnlyCollection<string> SourceSelections { get; } = Array.AsReadOnly(
        new[] { AutomaticSource }.Concat(Languages.Select(static language => language.Id)).ToArray());

    public static ReadOnlyCollection<string> ChineseInputScripts { get; } = Array.AsReadOnly(
        new[] { "simplified", "traditional" });

    public static ReadOnlyCollection<TranslationDirection> TranslationDirections { get; } = Array.AsReadOnly(
    new TranslationDirection[]
    {
        new("en", "ru"),
        new("en", "ro"),
        new("en", "zh"),
        new("ru", "en"),
        new("ru", "ro"),
        new("ru", "zh"),
        new("ro", "en"),
        new("ro", "ru"),
        new("ro", "zh"),
        new("zh", "en"),
        new("zh", "ru"),
        new("zh", "ro"),
    });

    public static ReadOnlyCollection<RewritingModeDefinition> RewritingModes { get; } = Array.AsReadOnly(
    new RewritingModeDefinition[]
    {
        new(DefaultRewritingMode, "Correction only", "correction"),
        new("simple", "Simple", "style"),
        new("casual", "Casual", "style"),
        new("business", "Business", "style"),
        new("academic", "Academic", "style"),
        new("enthusiastic", "Enthusiastic", "tone"),
        new("friendly", "Friendly", "tone"),
        new("confident", "Confident", "tone"),
        new("diplomatic", "Diplomatic", "tone"),
    });

    public static bool IsLanguage(string? value) =>
        value is not null && Languages.Any(language => string.Equals(language.Id, value, StringComparison.Ordinal));

    public static bool IsSourceSelection(string? value) =>
        value is not null && SourceSelections.Contains(value, StringComparer.Ordinal);

    public static bool IsRewritingMode(string? value) =>
        value is not null && RewritingModes.Any(mode => string.Equals(mode.Id, value, StringComparison.Ordinal));
}
