using System.ComponentModel;
using System.Text.Json.Serialization;

namespace LinguaDesk.Api.Features.Capabilities;

[JsonConverter(typeof(JsonStringEnumConverter<LanguageId>))]
public enum LanguageId
{
    [JsonStringEnumMemberName("en")]
    English,
    [JsonStringEnumMemberName("ru")]
    Russian,
    [JsonStringEnumMemberName("ro")]
    Romanian,
    [JsonStringEnumMemberName("zh")]
    Chinese,
    [JsonStringEnumMemberName("af")]
    Afrikaans,
    [JsonStringEnumMemberName("ak")]
    Akan,
    [JsonStringEnumMemberName("sq")]
    Albanian,
    [JsonStringEnumMemberName("am")]
    Amharic,
    [JsonStringEnumMemberName("ar")]
    Arabic,
    [JsonStringEnumMemberName("hy")]
    Armenian,
    [JsonStringEnumMemberName("as")]
    Assamese,
    [JsonStringEnumMemberName("az")]
    Azerbaijani,
    [JsonStringEnumMemberName("bm")]
    Bambara,
    [JsonStringEnumMemberName("eu")]
    Basque,
    [JsonStringEnumMemberName("be")]
    Belarusian,
    [JsonStringEnumMemberName("bn")]
    Bengali,
    [JsonStringEnumMemberName("bs")]
    Bosnian,
    [JsonStringEnumMemberName("br")]
    Breton,
    [JsonStringEnumMemberName("bg")]
    Bulgarian,
    [JsonStringEnumMemberName("my")]
    Burmese,
    [JsonStringEnumMemberName("ca")]
    Catalan,
    [JsonStringEnumMemberName("ceb")]
    Cebuano,
    [JsonStringEnumMemberName("ny")]
    Chichewa,
    [JsonStringEnumMemberName("co")]
    Corsican,
    [JsonStringEnumMemberName("hr")]
    Croatian,
    [JsonStringEnumMemberName("cs")]
    Czech,
    [JsonStringEnumMemberName("da")]
    Danish,
    [JsonStringEnumMemberName("nl")]
    Dutch,
    [JsonStringEnumMemberName("eo")]
    Esperanto,
    [JsonStringEnumMemberName("et")]
    Estonian,
    [JsonStringEnumMemberName("ee")]
    Ewe,
    [JsonStringEnumMemberName("fil")]
    Filipino,
    [JsonStringEnumMemberName("fi")]
    Finnish,
    [JsonStringEnumMemberName("fr")]
    French,
    [JsonStringEnumMemberName("gl")]
    Galician,
    [JsonStringEnumMemberName("ka")]
    Georgian,
    [JsonStringEnumMemberName("de")]
    German,
    [JsonStringEnumMemberName("el")]
    Greek,
    [JsonStringEnumMemberName("gu")]
    Gujarati,
    [JsonStringEnumMemberName("ht")]
    HaitianCreole,
    [JsonStringEnumMemberName("ha")]
    Hausa,
    [JsonStringEnumMemberName("haw")]
    Hawaiian,
    [JsonStringEnumMemberName("he")]
    Hebrew,
    [JsonStringEnumMemberName("hi")]
    Hindi,
    [JsonStringEnumMemberName("hu")]
    Hungarian,
    [JsonStringEnumMemberName("is")]
    Icelandic,
    [JsonStringEnumMemberName("ig")]
    Igbo,
    [JsonStringEnumMemberName("id")]
    Indonesian,
    [JsonStringEnumMemberName("ga")]
    Irish,
    [JsonStringEnumMemberName("it")]
    Italian,
    [JsonStringEnumMemberName("ja")]
    Japanese,
    [JsonStringEnumMemberName("jv")]
    Javanese,
    [JsonStringEnumMemberName("kn")]
    Kannada,
    [JsonStringEnumMemberName("kk")]
    Kazakh,
    [JsonStringEnumMemberName("km")]
    Khmer,
    [JsonStringEnumMemberName("rw")]
    Kinyarwanda,
    [JsonStringEnumMemberName("ko")]
    Korean,
    [JsonStringEnumMemberName("ku")]
    KurdishKurmanji,
    [JsonStringEnumMemberName("ckb")]
    KurdishSorani,
    [JsonStringEnumMemberName("ky")]
    Kyrgyz,
    [JsonStringEnumMemberName("lo")]
    Lao,
    [JsonStringEnumMemberName("la")]
    Latin,
    [JsonStringEnumMemberName("lv")]
    Latvian,
    [JsonStringEnumMemberName("ln")]
    Lingala,
    [JsonStringEnumMemberName("lt")]
    Lithuanian,
    [JsonStringEnumMemberName("lg")]
    Luganda,
    [JsonStringEnumMemberName("lb")]
    Luxembourgish,
    [JsonStringEnumMemberName("mk")]
    Macedonian,
    [JsonStringEnumMemberName("mg")]
    Malagasy,
    [JsonStringEnumMemberName("ms")]
    Malay,
    [JsonStringEnumMemberName("ml")]
    Malayalam,
    [JsonStringEnumMemberName("mt")]
    Maltese,
    [JsonStringEnumMemberName("mi")]
    Maori,
    [JsonStringEnumMemberName("mr")]
    Marathi,
    [JsonStringEnumMemberName("mn")]
    Mongolian,
    [JsonStringEnumMemberName("ne")]
    Nepali,
    [JsonStringEnumMemberName("nb")]
    NorwegianBokml,
    [JsonStringEnumMemberName("nn")]
    NorwegianNynorsk,
    [JsonStringEnumMemberName("oc")]
    Occitan,
    [JsonStringEnumMemberName("or")]
    Odia,
    [JsonStringEnumMemberName("om")]
    Oromo,
    [JsonStringEnumMemberName("ps")]
    Pashto,
    [JsonStringEnumMemberName("fa")]
    Persian,
    [JsonStringEnumMemberName("pl")]
    Polish,
    [JsonStringEnumMemberName("pt")]
    Portuguese,
    [JsonStringEnumMemberName("pa")]
    Punjabi,
    [JsonStringEnumMemberName("qu")]
    Quechua,
    [JsonStringEnumMemberName("sm")]
    Samoan,
    [JsonStringEnumMemberName("sa")]
    Sanskrit,
    [JsonStringEnumMemberName("gd")]
    ScottishGaelic,
    [JsonStringEnumMemberName("sr")]
    Serbian,
    [JsonStringEnumMemberName("st")]
    Sesotho,
    [JsonStringEnumMemberName("sn")]
    Shona,
    [JsonStringEnumMemberName("sd")]
    Sindhi,
    [JsonStringEnumMemberName("si")]
    Sinhala,
    [JsonStringEnumMemberName("sk")]
    Slovak,
    [JsonStringEnumMemberName("sl")]
    Slovenian,
    [JsonStringEnumMemberName("so")]
    Somali,
    [JsonStringEnumMemberName("es")]
    Spanish,
    [JsonStringEnumMemberName("su")]
    Sundanese,
    [JsonStringEnumMemberName("sw")]
    Swahili,
    [JsonStringEnumMemberName("sv")]
    Swedish,
    [JsonStringEnumMemberName("tg")]
    Tajik,
    [JsonStringEnumMemberName("ta")]
    Tamil,
    [JsonStringEnumMemberName("tt")]
    Tatar,
    [JsonStringEnumMemberName("te")]
    Telugu,
    [JsonStringEnumMemberName("th")]
    Thai,
    [JsonStringEnumMemberName("ti")]
    Tigrinya,
    [JsonStringEnumMemberName("ts")]
    Tsonga,
    [JsonStringEnumMemberName("tn")]
    Tswana,
    [JsonStringEnumMemberName("tr")]
    Turkish,
    [JsonStringEnumMemberName("tk")]
    Turkmen,
    [JsonStringEnumMemberName("uk")]
    Ukrainian,
    [JsonStringEnumMemberName("ur")]
    Urdu,
    [JsonStringEnumMemberName("ug")]
    Uyghur,
    [JsonStringEnumMemberName("uz")]
    Uzbek,
    [JsonStringEnumMemberName("vi")]
    Vietnamese,
    [JsonStringEnumMemberName("cy")]
    Welsh,
    [JsonStringEnumMemberName("fy")]
    WesternFrisian,
    [JsonStringEnumMemberName("wo")]
    Wolof,
    [JsonStringEnumMemberName("xh")]
    Xhosa,
    [JsonStringEnumMemberName("yi")]
    Yiddish,
    [JsonStringEnumMemberName("yo")]
    Yoruba,
    [JsonStringEnumMemberName("zu")]
    Zulu,
}

[JsonConverter(typeof(JsonStringEnumConverter<SourceSelectionValue>))]
public enum SourceSelectionValue
{
    [JsonStringEnumMemberName("auto")]
    Automatic,
    [JsonStringEnumMemberName("en")]
    English,
    [JsonStringEnumMemberName("ru")]
    Russian,
    [JsonStringEnumMemberName("ro")]
    Romanian,
    [JsonStringEnumMemberName("zh")]
    Chinese,
    [JsonStringEnumMemberName("af")]
    Afrikaans,
    [JsonStringEnumMemberName("ak")]
    Akan,
    [JsonStringEnumMemberName("sq")]
    Albanian,
    [JsonStringEnumMemberName("am")]
    Amharic,
    [JsonStringEnumMemberName("ar")]
    Arabic,
    [JsonStringEnumMemberName("hy")]
    Armenian,
    [JsonStringEnumMemberName("as")]
    Assamese,
    [JsonStringEnumMemberName("az")]
    Azerbaijani,
    [JsonStringEnumMemberName("bm")]
    Bambara,
    [JsonStringEnumMemberName("eu")]
    Basque,
    [JsonStringEnumMemberName("be")]
    Belarusian,
    [JsonStringEnumMemberName("bn")]
    Bengali,
    [JsonStringEnumMemberName("bs")]
    Bosnian,
    [JsonStringEnumMemberName("br")]
    Breton,
    [JsonStringEnumMemberName("bg")]
    Bulgarian,
    [JsonStringEnumMemberName("my")]
    Burmese,
    [JsonStringEnumMemberName("ca")]
    Catalan,
    [JsonStringEnumMemberName("ceb")]
    Cebuano,
    [JsonStringEnumMemberName("ny")]
    Chichewa,
    [JsonStringEnumMemberName("co")]
    Corsican,
    [JsonStringEnumMemberName("hr")]
    Croatian,
    [JsonStringEnumMemberName("cs")]
    Czech,
    [JsonStringEnumMemberName("da")]
    Danish,
    [JsonStringEnumMemberName("nl")]
    Dutch,
    [JsonStringEnumMemberName("eo")]
    Esperanto,
    [JsonStringEnumMemberName("et")]
    Estonian,
    [JsonStringEnumMemberName("ee")]
    Ewe,
    [JsonStringEnumMemberName("fil")]
    Filipino,
    [JsonStringEnumMemberName("fi")]
    Finnish,
    [JsonStringEnumMemberName("fr")]
    French,
    [JsonStringEnumMemberName("gl")]
    Galician,
    [JsonStringEnumMemberName("ka")]
    Georgian,
    [JsonStringEnumMemberName("de")]
    German,
    [JsonStringEnumMemberName("el")]
    Greek,
    [JsonStringEnumMemberName("gu")]
    Gujarati,
    [JsonStringEnumMemberName("ht")]
    HaitianCreole,
    [JsonStringEnumMemberName("ha")]
    Hausa,
    [JsonStringEnumMemberName("haw")]
    Hawaiian,
    [JsonStringEnumMemberName("he")]
    Hebrew,
    [JsonStringEnumMemberName("hi")]
    Hindi,
    [JsonStringEnumMemberName("hu")]
    Hungarian,
    [JsonStringEnumMemberName("is")]
    Icelandic,
    [JsonStringEnumMemberName("ig")]
    Igbo,
    [JsonStringEnumMemberName("id")]
    Indonesian,
    [JsonStringEnumMemberName("ga")]
    Irish,
    [JsonStringEnumMemberName("it")]
    Italian,
    [JsonStringEnumMemberName("ja")]
    Japanese,
    [JsonStringEnumMemberName("jv")]
    Javanese,
    [JsonStringEnumMemberName("kn")]
    Kannada,
    [JsonStringEnumMemberName("kk")]
    Kazakh,
    [JsonStringEnumMemberName("km")]
    Khmer,
    [JsonStringEnumMemberName("rw")]
    Kinyarwanda,
    [JsonStringEnumMemberName("ko")]
    Korean,
    [JsonStringEnumMemberName("ku")]
    KurdishKurmanji,
    [JsonStringEnumMemberName("ckb")]
    KurdishSorani,
    [JsonStringEnumMemberName("ky")]
    Kyrgyz,
    [JsonStringEnumMemberName("lo")]
    Lao,
    [JsonStringEnumMemberName("la")]
    Latin,
    [JsonStringEnumMemberName("lv")]
    Latvian,
    [JsonStringEnumMemberName("ln")]
    Lingala,
    [JsonStringEnumMemberName("lt")]
    Lithuanian,
    [JsonStringEnumMemberName("lg")]
    Luganda,
    [JsonStringEnumMemberName("lb")]
    Luxembourgish,
    [JsonStringEnumMemberName("mk")]
    Macedonian,
    [JsonStringEnumMemberName("mg")]
    Malagasy,
    [JsonStringEnumMemberName("ms")]
    Malay,
    [JsonStringEnumMemberName("ml")]
    Malayalam,
    [JsonStringEnumMemberName("mt")]
    Maltese,
    [JsonStringEnumMemberName("mi")]
    Maori,
    [JsonStringEnumMemberName("mr")]
    Marathi,
    [JsonStringEnumMemberName("mn")]
    Mongolian,
    [JsonStringEnumMemberName("ne")]
    Nepali,
    [JsonStringEnumMemberName("nb")]
    NorwegianBokml,
    [JsonStringEnumMemberName("nn")]
    NorwegianNynorsk,
    [JsonStringEnumMemberName("oc")]
    Occitan,
    [JsonStringEnumMemberName("or")]
    Odia,
    [JsonStringEnumMemberName("om")]
    Oromo,
    [JsonStringEnumMemberName("ps")]
    Pashto,
    [JsonStringEnumMemberName("fa")]
    Persian,
    [JsonStringEnumMemberName("pl")]
    Polish,
    [JsonStringEnumMemberName("pt")]
    Portuguese,
    [JsonStringEnumMemberName("pa")]
    Punjabi,
    [JsonStringEnumMemberName("qu")]
    Quechua,
    [JsonStringEnumMemberName("sm")]
    Samoan,
    [JsonStringEnumMemberName("sa")]
    Sanskrit,
    [JsonStringEnumMemberName("gd")]
    ScottishGaelic,
    [JsonStringEnumMemberName("sr")]
    Serbian,
    [JsonStringEnumMemberName("st")]
    Sesotho,
    [JsonStringEnumMemberName("sn")]
    Shona,
    [JsonStringEnumMemberName("sd")]
    Sindhi,
    [JsonStringEnumMemberName("si")]
    Sinhala,
    [JsonStringEnumMemberName("sk")]
    Slovak,
    [JsonStringEnumMemberName("sl")]
    Slovenian,
    [JsonStringEnumMemberName("so")]
    Somali,
    [JsonStringEnumMemberName("es")]
    Spanish,
    [JsonStringEnumMemberName("su")]
    Sundanese,
    [JsonStringEnumMemberName("sw")]
    Swahili,
    [JsonStringEnumMemberName("sv")]
    Swedish,
    [JsonStringEnumMemberName("tg")]
    Tajik,
    [JsonStringEnumMemberName("ta")]
    Tamil,
    [JsonStringEnumMemberName("tt")]
    Tatar,
    [JsonStringEnumMemberName("te")]
    Telugu,
    [JsonStringEnumMemberName("th")]
    Thai,
    [JsonStringEnumMemberName("ti")]
    Tigrinya,
    [JsonStringEnumMemberName("ts")]
    Tsonga,
    [JsonStringEnumMemberName("tn")]
    Tswana,
    [JsonStringEnumMemberName("tr")]
    Turkish,
    [JsonStringEnumMemberName("tk")]
    Turkmen,
    [JsonStringEnumMemberName("uk")]
    Ukrainian,
    [JsonStringEnumMemberName("ur")]
    Urdu,
    [JsonStringEnumMemberName("ug")]
    Uyghur,
    [JsonStringEnumMemberName("uz")]
    Uzbek,
    [JsonStringEnumMemberName("vi")]
    Vietnamese,
    [JsonStringEnumMemberName("cy")]
    Welsh,
    [JsonStringEnumMemberName("fy")]
    WesternFrisian,
    [JsonStringEnumMemberName("wo")]
    Wolof,
    [JsonStringEnumMemberName("xh")]
    Xhosa,
    [JsonStringEnumMemberName("yi")]
    Yiddish,
    [JsonStringEnumMemberName("yo")]
    Yoruba,
    [JsonStringEnumMemberName("zu")]
    Zulu,
}

[JsonConverter(typeof(JsonStringEnumConverter<ContentDirection>))]
public enum ContentDirection
{
    [JsonStringEnumMemberName("ltr")] LeftToRight,
    [JsonStringEnumMemberName("rtl")] RightToLeft,
}

[JsonConverter(typeof(JsonStringEnumConverter<ChineseScript>))]
public enum ChineseScript
{
    [JsonStringEnumMemberName("simplified")]
    Simplified,
    [JsonStringEnumMemberName("traditional")]
    Traditional,
}

[JsonConverter(typeof(JsonStringEnumConverter<CountingUnit>))]
public enum CountingUnit
{
    [JsonStringEnumMemberName("unicodeScalar")]
    UnicodeScalar,
}

[JsonConverter(typeof(JsonStringEnumConverter<NormalizationPolicy>))]
public enum NormalizationPolicy
{
    [JsonStringEnumMemberName("none")]
    None,
}

[JsonConverter(typeof(JsonStringEnumConverter<LineEndingPolicy>))]
public enum LineEndingPolicy
{
    [JsonStringEnumMemberName("preserve")]
    Preserve,
}

[JsonConverter(typeof(JsonStringEnumConverter<InvalidUnicodePolicy>))]
public enum InvalidUnicodePolicy
{
    [JsonStringEnumMemberName("reject")]
    Reject,
}

[JsonConverter(typeof(JsonStringEnumConverter<EmptyOrWhitespacePolicy>))]
public enum EmptyOrWhitespacePolicy
{
    [JsonStringEnumMemberName("reject")]
    Reject,
}

[JsonConverter(typeof(JsonStringEnumConverter<OversizeHandling>))]
public enum OversizeHandling
{
    [JsonStringEnumMemberName("rejectWhole")]
    RejectWhole,
}

[JsonConverter(typeof(JsonStringEnumConverter<RewritingModeId>))]
public enum RewritingModeId
{
    [JsonStringEnumMemberName("correctionOnly")]
    CorrectionOnly,
    [JsonStringEnumMemberName("simple")]
    Simple,
    [JsonStringEnumMemberName("casual")]
    Casual,
    [JsonStringEnumMemberName("business")]
    Business,
    [JsonStringEnumMemberName("academic")]
    Academic,
    [JsonStringEnumMemberName("enthusiastic")]
    Enthusiastic,
    [JsonStringEnumMemberName("friendly")]
    Friendly,
    [JsonStringEnumMemberName("confident")]
    Confident,
    [JsonStringEnumMemberName("diplomatic")]
    Diplomatic,
}

[JsonConverter(typeof(JsonStringEnumConverter<RewritingModeKind>))]
public enum RewritingModeKind
{
    [JsonStringEnumMemberName("correction")]
    Correction,
    [JsonStringEnumMemberName("style")]
    Style,
    [JsonStringEnumMemberName("tone")]
    Tone,
}

[JsonConverter(typeof(JsonStringEnumConverter<OperationIdentityFormat>))]
public enum OperationIdentityFormat
{
    [JsonStringEnumMemberName("uuidV7")]
    UuidV7,
}

/// <summary>Public contract choices and local validation policy for LinguaDesk clients.</summary>
public sealed record CapabilitiesResponse(
    [property: Description("Current server time in UTC, used by clients when validating new UUIDv7 timestamps.")] DateTimeOffset ServerTimeUtc,
    [property: Description("Ordered supported language identifiers and English names.")] IReadOnlyList<LanguageCapability> Languages,
    [property: Description("Accepted source-language values and omission default.")] SourceSelectionCapability SourceSelection,
    [property: Description("Accepted Chinese input scripts and required Chinese output script.")] ChineseScriptPolicyCapability ChineseScriptPolicy,
    [property: Description("Canonical decoded-input counting and local rejection policy.")] CountingPolicyCapability CountingPolicy,
    [property: Description("Translation directions, whole-input limit, and overall deadline.")] TranslationCapability Translation,
    [property: Description("Exclusive rewriting modes, whole-input limit, and overall deadline.")] RewritingCapability Rewriting,
    [property: Description("UUIDv7 operation-identity recovery bounds.")] OperationIdentityCapability OperationIdentity)
{
    [Description("Supported sentence alternatives operation, exact input bounds, segmentation and deadline.")]
    public AlternativesCapability Alternatives { get; init; } = new(8000, 2000, 131072, LinguaDesk.Core.SentenceBoundaryPolicy.Revision, 2, 15, "sentenceAlternatives");
}

public sealed record AlternativesCapability(
    [property: Description("Maximum exact context in Unicode scalars.")] int MaximumContextCharacters,
    [property: Description("Maximum selected complete sentence in Unicode scalars.")] int MaximumSentenceCharacters,
    [property: Description("Maximum UTF-8 request body bytes.")] int MaximumRequestBytes,
    [property: Description("Versioned deterministic scalar-span segmentation policy.")] string SegmentationRevision,
    [property: Description("Exactly two distinct complete options per successful original response.")] int OptionCount,
    [property: Description("Overall server deadline including eligibility, fallback and settlement.")] int OverallDeadlineSeconds,
    [property: Description("Supported operation kind: sentenceAlternatives.")] string Kind);

/// <summary>A supported language identifier and its English display name.</summary>
public sealed record LanguageCapability(
    [property: Description("Stable language identifier.")] LanguageId Id,
    [property: Description("English display name.")] string Name,
    [property: Description("Content direction: ltr or rtl; application controls remain LTR.")] ContentDirection Direction,
    [property: Description("Selected writing system.")] string Script,
    [property: Description("Selected script and variety constraints.")] string Policy);

/// <summary>Accepted source-language selections and the default used when omitted.</summary>
public sealed record SourceSelectionCapability(
    [property: Description("Source selection used when a client omits the setting.")] SourceSelectionValue Default,
    [property: Description("Ordered accepted automatic and manual source selections.")] IReadOnlyList<SourceSelectionValue> Values);

/// <summary>Accepted Chinese input scripts and the script produced for Chinese output.</summary>
public sealed record ChineseScriptPolicyCapability(
    [property: Description("Chinese scripts accepted as input.")] IReadOnlyList<ChineseScript> AcceptedInput,
    [property: Description("Chinese script used for output.")] ChineseScript Output);

/// <summary>The exact decoded-input counting and local rejection policy.</summary>
public sealed record CountingPolicyCapability(
    [property: Description("Versioned counting policy identifier.")] string Id,
    [property: Description("Unit counted in exact decoded source text.")] CountingUnit Unit,
    [property: Description("Unicode normalization applied before counting; none for this policy.")] NormalizationPolicy Normalization,
    [property: Description("Line-ending treatment before counting; exact line endings are preserved.")] LineEndingPolicy LineEndings,
    [property: Description("Disposition of isolated UTF-16 surrogates or malformed decoded Unicode.")] InvalidUnicodePolicy InvalidUnicode,
    [property: Description("Disposition of empty or fixed-set whitespace-only source.")] EmptyOrWhitespacePolicy EmptyOrWhitespace,
    [property: Description("Inclusive Unicode code-point ranges used only for the all-whitespace decision.")] IReadOnlyList<string> WhitespaceCodePointRanges);

/// <summary>Translation limits, deadline, target rule, and supported directed pairs.</summary>
public sealed record TranslationCapability(
    [property: Description("Maximum complete decoded source in Unicode scalar values.")] int MaximumSourceCharacters,
    [property: Description("Whole-input behavior when the source exceeds the maximum.")] OversizeHandling OversizeHandling,
    [property: Description("Overall server deadline including eligibility and fallback attempts.")] int OverallDeadlineSeconds,
    [property: Description("Whether the client must explicitly select a translation target.")] bool TargetRequired,
    [property: Description("Ordered supported distinct source-to-target language pairs.")] IReadOnlyList<TranslationDirectionCapability> SupportedDirections);

/// <summary>One supported translation source-to-target direction.</summary>
public sealed record TranslationDirectionCapability(
    [property: Description("Translation source language.")] LanguageId Source,
    [property: Description("Translation target language.")] LanguageId Target);

/// <summary>Rewriting limits, deadline, default, and mutually exclusive writing modes.</summary>
public sealed record RewritingCapability(
    [property: Description("Maximum complete decoded source in Unicode scalar values.")] int MaximumSourceCharacters,
    [property: Description("Whole-input behavior when the source exceeds the maximum.")] OversizeHandling OversizeHandling,
    [property: Description("Overall server deadline including eligibility and fallback attempts.")] int OverallDeadlineSeconds,
    [property: Description("Writing mode used when a client omits the mode.")] RewritingModeId DefaultMode,
    [property: Description("Ordered mutually exclusive rewriting choices; correction applies to every choice.")] IReadOnlyList<RewritingModeCapability> Modes);

/// <summary>A supported exclusive rewriting mode and its behavioral category.</summary>
public sealed record RewritingModeCapability(
    [property: Description("Stable writing-mode identifier.")] RewritingModeId Id,
    [property: Description("English display name.")] string Name,
    [property: Description("Correction, writing-style, or tone category.")] RewritingModeKind Kind);

/// <summary>Client operation-identity format and its bounded recovery window.</summary>
public sealed record OperationIdentityCapability(
    [property: Description("Client-generated operation identity format.")] OperationIdentityFormat Format,
    [property: Description("Retry and status validity after the UUID embedded timestamp, in seconds.")] int ValidForSeconds,
    [property: Description("Maximum accepted future UUID timestamp skew, in seconds.")] int MaximumFutureSkewSeconds);
