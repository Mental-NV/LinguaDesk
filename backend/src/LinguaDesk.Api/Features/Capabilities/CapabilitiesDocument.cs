using LinguaDesk.Core;

namespace LinguaDesk.Api.Features.Capabilities;

internal static class CapabilitiesDocument
{
    private static readonly IReadOnlyList<LanguageCapability> Languages = Array.AsReadOnly(
        ProductCatalog.Languages
            .Select(static language => new LanguageCapability(ToLanguageId(language.Id), language.Name))
            .ToArray());

    private static readonly IReadOnlyList<SourceSelectionValue> SourceSelections = Array.AsReadOnly(
        ProductCatalog.SourceSelections.Select(ToSourceSelection).ToArray());

    private static readonly IReadOnlyList<ChineseScript> ChineseInputScripts = Array.AsReadOnly(
        ProductCatalog.ChineseInputScripts.Select(ToChineseScript).ToArray());

    private static readonly IReadOnlyList<TranslationDirectionCapability> TranslationDirections = Array.AsReadOnly(
        ProductCatalog.TranslationDirections
            .Select(static direction => new TranslationDirectionCapability(
                ToLanguageId(direction.Source),
                ToLanguageId(direction.Target)))
            .ToArray());

    private static readonly IReadOnlyList<RewritingModeCapability> RewritingModes = Array.AsReadOnly(
        ProductCatalog.RewritingModes
            .Select(static mode => new RewritingModeCapability(
                ToRewritingModeId(mode.Id),
                mode.Name,
                ToRewritingModeKind(mode.Kind)))
            .ToArray());

    public static CapabilitiesResponse Create(DateTimeOffset serverTimeUtc) =>
        new(
            serverTimeUtc,
            Languages,
            new(ToSourceSelection(ProductCatalog.AutomaticSource), SourceSelections),
            new(ChineseInputScripts, ToChineseScript(ProductCatalog.ChineseOutputScript)),
            new(
                ScalarInputPolicy.Id,
                CountingUnit.UnicodeScalar,
                NormalizationPolicy.None,
                LineEndingPolicy.Preserve,
                InvalidUnicodePolicy.Reject,
                EmptyOrWhitespacePolicy.Reject,
                ScalarInputPolicy.WhitespaceCodePointRanges),
            new(
                ProductCatalog.TranslationMaximumSourceCharacters,
                OversizeHandling.RejectWhole,
                ProductCatalog.OverallDeadlineSeconds,
                TargetRequired: true,
                TranslationDirections),
            new(
                ProductCatalog.RewritingMaximumSourceCharacters,
                OversizeHandling.RejectWhole,
                ProductCatalog.OverallDeadlineSeconds,
                ToRewritingModeId(ProductCatalog.DefaultRewritingMode),
                RewritingModes),
            new(
                OperationIdentityFormat.UuidV7,
                ProductCatalog.OperationIdentityValidForSeconds,
                ProductCatalog.OperationIdentityMaximumFutureSkewSeconds));

    private static LanguageId ToLanguageId(string id) => id switch
    {
        "en" => LanguageId.English,
        "ru" => LanguageId.Russian,
        "ro" => LanguageId.Romanian,
        "zh" => LanguageId.Chinese,
        _ => throw UnexpectedPolicy(),
    };

    private static SourceSelectionValue ToSourceSelection(string id) => id switch
    {
        "auto" => SourceSelectionValue.Automatic,
        "en" => SourceSelectionValue.English,
        "ru" => SourceSelectionValue.Russian,
        "ro" => SourceSelectionValue.Romanian,
        "zh" => SourceSelectionValue.Chinese,
        _ => throw UnexpectedPolicy(),
    };

    private static ChineseScript ToChineseScript(string id) => id switch
    {
        "simplified" => ChineseScript.Simplified,
        "traditional" => ChineseScript.Traditional,
        _ => throw UnexpectedPolicy(),
    };

    private static RewritingModeId ToRewritingModeId(string id) => id switch
    {
        "correctionOnly" => RewritingModeId.CorrectionOnly,
        "simple" => RewritingModeId.Simple,
        "casual" => RewritingModeId.Casual,
        "business" => RewritingModeId.Business,
        "academic" => RewritingModeId.Academic,
        "enthusiastic" => RewritingModeId.Enthusiastic,
        "friendly" => RewritingModeId.Friendly,
        "confident" => RewritingModeId.Confident,
        "diplomatic" => RewritingModeId.Diplomatic,
        _ => throw UnexpectedPolicy(),
    };

    private static RewritingModeKind ToRewritingModeKind(string kind) => kind switch
    {
        "correction" => RewritingModeKind.Correction,
        "style" => RewritingModeKind.Style,
        "tone" => RewritingModeKind.Tone,
        _ => throw UnexpectedPolicy(),
    };

    private static InvalidOperationException UnexpectedPolicy() =>
        new("The Core product catalog contains a value that the capability wire contract cannot represent.");
}
