using System.Collections.ObjectModel;

namespace LinguaDesk.Core;

public sealed record LanguageDefinition(string Id, string Name, string Direction, string Script, string Policy);

public sealed record TranslationDirection(string Source, string Target);

public sealed record RewritingModeDefinition(string Id, string Name, string Kind);

public static class ProductCatalog
{
    public const string LanguageRevision = "languages-v1";
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
        new("af", "Afrikaans", "ltr", "Latin", "Use Latin script; preserve the source variety in rewriting where possible."),
        new("ak", "Akan", "ltr", "Latin", "Use Latin script; preserve the source variety in rewriting where possible."),
        new("sq", "Albanian", "ltr", "Latin", "Use Latin script; preserve the source variety in rewriting where possible."),
        new("am", "Amharic", "ltr", "Ethiopic", "Use Ethiopic script; preserve the source variety in rewriting where possible."),
        new("ar", "Arabic", "rtl", "Arabic", "Use Modern Standard Arabic; dialect conversion is not promised."),
        new("hy", "Armenian", "ltr", "Armenian", "Use Armenian script; preserve the source variety in rewriting where possible."),
        new("as", "Assamese", "ltr", "Bengali", "Use Bengali script; preserve the source variety in rewriting where possible."),
        new("az", "Azerbaijani", "ltr", "Latin", "Use Latin script; preserve the source variety in rewriting where possible."),
        new("bm", "Bambara", "ltr", "Latin", "Use Latin script; preserve the source variety in rewriting where possible."),
        new("eu", "Basque", "ltr", "Latin", "Use Latin script; preserve the source variety in rewriting where possible."),
        new("be", "Belarusian", "ltr", "Cyrillic", "Use Cyrillic script; preserve the source variety in rewriting where possible."),
        new("bn", "Bengali", "ltr", "Bengali", "Use Bengali script; preserve the source variety in rewriting where possible."),
        new("bs", "Bosnian", "ltr", "Latin", "Use Latin script; preserve the source variety in rewriting where possible."),
        new("br", "Breton", "ltr", "Latin", "Use Latin script; preserve the source variety in rewriting where possible."),
        new("bg", "Bulgarian", "ltr", "Cyrillic", "Use Cyrillic script; preserve the source variety in rewriting where possible."),
        new("my", "Burmese", "ltr", "Myanmar", "Use Myanmar script; preserve the source variety in rewriting where possible."),
        new("ca", "Catalan", "ltr", "Latin", "Use Latin script; preserve the source variety in rewriting where possible."),
        new("ceb", "Cebuano", "ltr", "Latin", "Use Latin script; preserve the source variety in rewriting where possible."),
        new("ny", "Chichewa", "ltr", "Latin", "Use Latin script; preserve the source variety in rewriting where possible."),
        new("zh", "Chinese", "ltr", "Chinese", "Accept Simplified and Traditional input; always output Simplified Chinese."),
        new("co", "Corsican", "ltr", "Latin", "Use Latin script; preserve the source variety in rewriting where possible."),
        new("hr", "Croatian", "ltr", "Latin", "Use Latin script; preserve the source variety in rewriting where possible."),
        new("cs", "Czech", "ltr", "Latin", "Use Latin script; preserve the source variety in rewriting where possible."),
        new("da", "Danish", "ltr", "Latin", "Use Latin script; preserve the source variety in rewriting where possible."),
        new("nl", "Dutch", "ltr", "Latin", "Use Latin script; preserve the source variety in rewriting where possible."),
        new("en", "English", "ltr", "Latin", "Use Latin script; preserve the source variety in rewriting where possible."),
        new("eo", "Esperanto", "ltr", "Latin", "Use Latin script; preserve the source variety in rewriting where possible."),
        new("et", "Estonian", "ltr", "Latin", "Use Latin script; preserve the source variety in rewriting where possible."),
        new("ee", "Ewe", "ltr", "Latin", "Use Latin script; preserve the source variety in rewriting where possible."),
        new("fil", "Filipino", "ltr", "Latin", "Filipino uses fil; tl is not an alias."),
        new("fi", "Finnish", "ltr", "Latin", "Use Latin script; preserve the source variety in rewriting where possible."),
        new("fr", "French", "ltr", "Latin", "Use Latin script; preserve the source variety in rewriting where possible."),
        new("gl", "Galician", "ltr", "Latin", "Use Latin script; preserve the source variety in rewriting where possible."),
        new("ka", "Georgian", "ltr", "Georgian", "Use Georgian script; preserve the source variety in rewriting where possible."),
        new("de", "German", "ltr", "Latin", "Use Latin script; preserve the source variety in rewriting where possible."),
        new("el", "Greek", "ltr", "Greek", "Use Greek script; preserve the source variety in rewriting where possible."),
        new("gu", "Gujarati", "ltr", "Gujarati", "Use Gujarati script; preserve the source variety in rewriting where possible."),
        new("ht", "Haitian Creole", "ltr", "Latin", "Use Latin script; preserve the source variety in rewriting where possible."),
        new("ha", "Hausa", "ltr", "Latin", "Use Latin script; preserve the source variety in rewriting where possible."),
        new("haw", "Hawaiian", "ltr", "Latin", "Use Latin script; preserve the source variety in rewriting where possible."),
        new("he", "Hebrew", "rtl", "Hebrew", "Use Hebrew script; preserve the source variety in rewriting where possible."),
        new("hi", "Hindi", "ltr", "Devanagari", "Use Devanagari script; preserve the source variety in rewriting where possible."),
        new("hu", "Hungarian", "ltr", "Latin", "Use Latin script; preserve the source variety in rewriting where possible."),
        new("is", "Icelandic", "ltr", "Latin", "Use Latin script; preserve the source variety in rewriting where possible."),
        new("ig", "Igbo", "ltr", "Latin", "Use Latin script; preserve the source variety in rewriting where possible."),
        new("id", "Indonesian", "ltr", "Latin", "Use Latin script; preserve the source variety in rewriting where possible."),
        new("ga", "Irish", "ltr", "Latin", "Use Latin script; preserve the source variety in rewriting where possible."),
        new("it", "Italian", "ltr", "Latin", "Use Latin script; preserve the source variety in rewriting where possible."),
        new("ja", "Japanese", "ltr", "Japanese", "Use Japanese script; preserve the source variety in rewriting where possible."),
        new("jv", "Javanese", "ltr", "Latin", "Use Latin script; preserve the source variety in rewriting where possible."),
        new("kn", "Kannada", "ltr", "Kannada", "Use Kannada script; preserve the source variety in rewriting where possible."),
        new("kk", "Kazakh", "ltr", "Cyrillic", "Use Cyrillic script; preserve the source variety in rewriting where possible."),
        new("km", "Khmer", "ltr", "Khmer", "Use Khmer script; preserve the source variety in rewriting where possible."),
        new("rw", "Kinyarwanda", "ltr", "Latin", "Use Latin script; preserve the source variety in rewriting where possible."),
        new("ko", "Korean", "ltr", "Korean", "Use Korean script; preserve the source variety in rewriting where possible."),
        new("ku", "Kurdish (Kurmanji)", "ltr", "Latin", "Use Latin-script Kurmanji."),
        new("ckb", "Kurdish (Sorani)", "rtl", "Arabic", "Use Arabic-script Sorani."),
        new("ky", "Kyrgyz", "ltr", "Cyrillic", "Use Cyrillic script; preserve the source variety in rewriting where possible."),
        new("lo", "Lao", "ltr", "Lao", "Use Lao script; preserve the source variety in rewriting where possible."),
        new("la", "Latin", "ltr", "Latin", "Use Latin script; preserve the source variety in rewriting where possible."),
        new("lv", "Latvian", "ltr", "Latin", "Use Latin script; preserve the source variety in rewriting where possible."),
        new("ln", "Lingala", "ltr", "Latin", "Use Latin script; preserve the source variety in rewriting where possible."),
        new("lt", "Lithuanian", "ltr", "Latin", "Use Latin script; preserve the source variety in rewriting where possible."),
        new("lg", "Luganda", "ltr", "Latin", "Use Latin script; preserve the source variety in rewriting where possible."),
        new("lb", "Luxembourgish", "ltr", "Latin", "Use Latin script; preserve the source variety in rewriting where possible."),
        new("mk", "Macedonian", "ltr", "Cyrillic", "Use Cyrillic script; preserve the source variety in rewriting where possible."),
        new("mg", "Malagasy", "ltr", "Latin", "Use Latin script; preserve the source variety in rewriting where possible."),
        new("ms", "Malay", "ltr", "Latin", "Use Latin script; preserve the source variety in rewriting where possible."),
        new("ml", "Malayalam", "ltr", "Malayalam", "Use Malayalam script; preserve the source variety in rewriting where possible."),
        new("mt", "Maltese", "ltr", "Latin", "Use Latin script; preserve the source variety in rewriting where possible."),
        new("mi", "Maori", "ltr", "Latin", "Use Latin script; preserve the source variety in rewriting where possible."),
        new("mr", "Marathi", "ltr", "Devanagari", "Use Devanagari script; preserve the source variety in rewriting where possible."),
        new("mn", "Mongolian", "ltr", "Cyrillic", "Use Cyrillic; vertical Mongolian is unsupported."),
        new("ne", "Nepali", "ltr", "Devanagari", "Use Devanagari script; preserve the source variety in rewriting where possible."),
        new("nb", "Norwegian Bokmål", "ltr", "Latin", "Use Latin script; preserve the source variety in rewriting where possible."),
        new("nn", "Norwegian Nynorsk", "ltr", "Latin", "Use Latin script; preserve the source variety in rewriting where possible."),
        new("oc", "Occitan", "ltr", "Latin", "Use Latin script; preserve the source variety in rewriting where possible."),
        new("or", "Odia", "ltr", "Odia", "Use Odia script; preserve the source variety in rewriting where possible."),
        new("om", "Oromo", "ltr", "Latin", "Use Latin script; preserve the source variety in rewriting where possible."),
        new("ps", "Pashto", "rtl", "Arabic", "Use Arabic script; preserve the source variety in rewriting where possible."),
        new("fa", "Persian", "rtl", "Arabic", "Use Arabic script; preserve the source variety in rewriting where possible."),
        new("pl", "Polish", "ltr", "Latin", "Use Latin script; preserve the source variety in rewriting where possible."),
        new("pt", "Portuguese", "ltr", "Latin", "Use Latin script; preserve the source variety in rewriting where possible."),
        new("pa", "Punjabi", "ltr", "Gurmukhi", "Use Gurmukhi; Shahmukhi is unsupported."),
        new("qu", "Quechua", "ltr", "Latin", "Use Latin script; preserve the source variety in rewriting where possible."),
        new("ro", "Romanian", "ltr", "Latin", "Use Latin script; preserve the source variety in rewriting where possible."),
        new("ru", "Russian", "ltr", "Cyrillic", "Use Cyrillic script; preserve the source variety in rewriting where possible."),
        new("sm", "Samoan", "ltr", "Latin", "Use Latin script; preserve the source variety in rewriting where possible."),
        new("sa", "Sanskrit", "ltr", "Devanagari", "Use Devanagari script; preserve the source variety in rewriting where possible."),
        new("gd", "Scottish Gaelic", "ltr", "Latin", "Use Latin script; preserve the source variety in rewriting where possible."),
        new("sr", "Serbian", "ltr", "Cyrillic", "Accept Cyrillic and Latin input. For rewriting and alternatives preserve the main input script; translation target defaults to Cyrillic. No same-language cross-script conversion."),
        new("st", "Sesotho", "ltr", "Latin", "Use Latin script; preserve the source variety in rewriting where possible."),
        new("sn", "Shona", "ltr", "Latin", "Use Latin script; preserve the source variety in rewriting where possible."),
        new("sd", "Sindhi", "rtl", "Arabic", "Use Arabic script; preserve the source variety in rewriting where possible."),
        new("si", "Sinhala", "ltr", "Sinhala", "Use Sinhala script; preserve the source variety in rewriting where possible."),
        new("sk", "Slovak", "ltr", "Latin", "Use Latin script; preserve the source variety in rewriting where possible."),
        new("sl", "Slovenian", "ltr", "Latin", "Use Latin script; preserve the source variety in rewriting where possible."),
        new("so", "Somali", "ltr", "Latin", "Use Latin script; preserve the source variety in rewriting where possible."),
        new("es", "Spanish", "ltr", "Latin", "Use Latin script; preserve the source variety in rewriting where possible."),
        new("su", "Sundanese", "ltr", "Latin", "Use Latin script; preserve the source variety in rewriting where possible."),
        new("sw", "Swahili", "ltr", "Latin", "Use Latin script; preserve the source variety in rewriting where possible."),
        new("sv", "Swedish", "ltr", "Latin", "Use Latin script; preserve the source variety in rewriting where possible."),
        new("tg", "Tajik", "ltr", "Cyrillic", "Use Cyrillic script; preserve the source variety in rewriting where possible."),
        new("ta", "Tamil", "ltr", "Tamil", "Use Tamil script; preserve the source variety in rewriting where possible."),
        new("tt", "Tatar", "ltr", "Cyrillic", "Use Cyrillic script; preserve the source variety in rewriting where possible."),
        new("te", "Telugu", "ltr", "Telugu", "Use Telugu script; preserve the source variety in rewriting where possible."),
        new("th", "Thai", "ltr", "Thai", "Use Thai script; preserve the source variety in rewriting where possible."),
        new("ti", "Tigrinya", "ltr", "Ethiopic", "Use Ethiopic script; preserve the source variety in rewriting where possible."),
        new("ts", "Tsonga", "ltr", "Latin", "Use Latin script; preserve the source variety in rewriting where possible."),
        new("tn", "Tswana", "ltr", "Latin", "Use Latin script; preserve the source variety in rewriting where possible."),
        new("tr", "Turkish", "ltr", "Latin", "Use Latin script; preserve the source variety in rewriting where possible."),
        new("tk", "Turkmen", "ltr", "Latin", "Use Latin script; preserve the source variety in rewriting where possible."),
        new("uk", "Ukrainian", "ltr", "Cyrillic", "Use Cyrillic script; preserve the source variety in rewriting where possible."),
        new("ur", "Urdu", "rtl", "Arabic", "Use Arabic script; preserve the source variety in rewriting where possible."),
        new("ug", "Uyghur", "rtl", "Arabic", "Use Arabic script; preserve the source variety in rewriting where possible."),
        new("uz", "Uzbek", "ltr", "Latin", "Use Latin script; preserve the source variety in rewriting where possible."),
        new("vi", "Vietnamese", "ltr", "Latin", "Use Latin script; preserve the source variety in rewriting where possible."),
        new("cy", "Welsh", "ltr", "Latin", "Use Latin script; preserve the source variety in rewriting where possible."),
        new("fy", "Western Frisian", "ltr", "Latin", "Use Latin script; preserve the source variety in rewriting where possible."),
        new("wo", "Wolof", "ltr", "Latin", "Use Latin script; preserve the source variety in rewriting where possible."),
        new("xh", "Xhosa", "ltr", "Latin", "Use Latin script; preserve the source variety in rewriting where possible."),
        new("yi", "Yiddish", "rtl", "Hebrew", "Use Hebrew script; preserve the source variety in rewriting where possible."),
        new("yo", "Yoruba", "ltr", "Latin", "Use Latin script; preserve the source variety in rewriting where possible."),
        new("zu", "Zulu", "ltr", "Latin", "Use Latin script; preserve the source variety in rewriting where possible."),
    });

    public static ReadOnlyCollection<string> SourceSelections { get; } = Array.AsReadOnly(
        new[] { AutomaticSource }.Concat(Languages.Select(static language => language.Id)).ToArray());

    public static ReadOnlyCollection<string> ChineseInputScripts { get; } = Array.AsReadOnly(
        new[] { "simplified", "traditional" });

    public static ReadOnlyCollection<TranslationDirection> TranslationDirections { get; } = Array.AsReadOnly(
    Languages.SelectMany(source => Languages
        .Where(target => target.Id != source.Id)
        .Select(target => new TranslationDirection(source.Id, target.Id))).ToArray());

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

    public static LanguageDefinition GetLanguage(string id) =>
        Languages.SingleOrDefault(language => language.Id == id)
        ?? throw new ArgumentException("Unknown canonical language.", nameof(id));

    public static bool IsLanguage(string? value) =>
        value is not null && Languages.Any(language => string.Equals(language.Id, value, StringComparison.Ordinal));

    public static bool IsSourceSelection(string? value) =>
        value is not null && SourceSelections.Contains(value, StringComparer.Ordinal);

    public static bool IsRewritingMode(string? value) =>
        value is not null && RewritingModes.Any(mode => string.Equals(mode.Id, value, StringComparison.Ordinal));
}
