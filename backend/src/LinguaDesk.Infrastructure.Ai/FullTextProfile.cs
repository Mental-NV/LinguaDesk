using LinguaDesk.Core;

namespace LinguaDesk.Infrastructure.Ai;

/// <summary>Finite per-input view. Decoded prompt UTF-8 bytes plus framing bound tokens; no average characters/token admission.</summary>
public static class FullTextProfile
{
    public const string Revision = "full-text-prompts-v2";
    public const int MaximumInputTokens = 65536;
    private static readonly string LongestDescriptorLanguage = ProductCatalog.Languages
        .MaxBy(language => System.Text.Encoding.UTF8.GetByteCount(PromptResource.Descriptor(language.Id)) + language.Id.Length)!.Id;

    public static CandidateProfile ForTranslation(CandidateProfile profile, TranslationInput input) => ForSource(profile,
        input.Source, input.SourceSelection, sourceLanguage => TranslationPrompt.Create(input.Source, sourceLanguage, input.Target!));

    public static CandidateProfile ForRewriting(CandidateProfile profile, RewritingInput input) => ForSource(profile,
        input.Source, input.SourceSelection, sourceLanguage => RewritingPrompt.Create(input.Source, sourceLanguage, input.Mode ?? ProductCatalog.DefaultRewritingMode));

    private static CandidateProfile ForSource(CandidateProfile profile, string source, string? selection, Func<string, PromptSnapshot> transform)
    {
        var sourceLanguage = ProductCatalog.IsLanguage(selection) ? selection! : LongestDescriptorLanguage;
        var eligibility = EligibilityPrompt.Create(source, ProductCatalog.IsLanguage(selection) ? selection : null);
        var generation = transform(sourceLanguage);
        var bound = Math.Max(ChatCompletionsAdapter.ConservativeInputTokenBound(profile, eligibility.Messages),
            ChatCompletionsAdapter.ConservativeInputTokenBound(profile, generation.Messages));
        if (bound > MaximumInputTokens || (long)bound + profile.Bounds.MaxOutputTokens + 512 > profile.Bounds.ContextCapacityTokens)
            throw new InvalidOperationException("The candidate cannot support the complete full-text input bound.");
        return profile with { PromptRevision = Revision, Bounds = profile.Bounds with { MaxInputTokens = bound } };
    }
}
