using System.Text;
using System.Text.Json;
using LinguaDesk.Core;

namespace LinguaDesk.Infrastructure.Ai;

public sealed record AlternativesInput(string Context, int Start, int Length, Guid Association, string Language, string? Mode = null)
{
    public AlternativesInputValidation Validate() => AlternativesInputPolicy.Validate(Context, Start, Length, Association, Language, Mode);
}

public static class AlternativesPrompt
{
    public const string PromptId = "alternatives.v10";


    public static PromptSnapshot Create(AlternativesInput input)
    {
        var validation = input.Validate();
        if (!validation.IsValid) throw new ArgumentException("Invalid alternatives input.", nameof(input));
        var resource = PromptResource.Compose("LinguaDesk.Infrastructure.Ai.Prompts.alternatives.v10.txt", "Source: " + PromptResource.Descriptor(input.Language));
        var scaffold = JsonSerializer.Serialize(new { context = "", selectedText = "", start = input.Start, length = input.Length, sourceLanguage = input.Language, mode = validation.EffectiveMode });
        if (Encoding.UTF8.GetByteCount(resource.Content) + Encoding.UTF8.GetByteCount(scaffold) > 4096)
            throw new InvalidOperationException("Alternatives prompt scaffolding exceeds 4 KiB.");
        return new(PromptId, resource.Sha256,
        [
            new("system", resource.Content),
            new("user", JsonSerializer.Serialize(new
            {
                context = input.Context, selectedText = validation.SelectedText,
                start = input.Start, length = input.Length, sourceLanguage = input.Language, mode = validation.EffectiveMode,
            })),
        ]);
    }
}

/// <summary>Operation-specific view. UTF-8 bytes of the complete model prompt content, including inner JSON escaping, plus framing bound tokens conservatively.</summary>
public static class AlternativesProfile
{
    public const int MaxInputTokens = 131072;
    public const int MaxOutputTokens = 8192;
    public const int MaxResponseBytes = 131072;
    public static CandidateProfile ForInput(CandidateProfile profile, AlternativesInput input)
    {
        if (profile.Bounds.ContextCapacityTokens < MaxInputTokens + MaxOutputTokens + 512)
            throw new InvalidOperationException("The candidate cannot support the alternatives context bound.");
        var view = profile with
        {
            PromptRevision = AlternativesPrompt.PromptId,
            ValidatorRevision = "alternatives-envelope-v1",
            Bounds = new(MaxOutputTokens, MaxResponseBytes, MaxInputTokens, profile.Bounds.ContextCapacityTokens, 5),
        };
        var eligibility = EligibilityPrompt.Create(input.Context, input.Language);
        var generation = AlternativesPrompt.Create(input);
        var bound = Math.Max(ChatCompletionsAdapter.ConservativeInputTokenBound(view, eligibility.Messages),
            ChatCompletionsAdapter.ConservativeInputTokenBound(view, generation.Messages));
        if (bound > MaxInputTokens) throw new InvalidOperationException("The alternatives prompt exceeds its input bound.");
        return view with { Bounds = view.Bounds with { MaxInputTokens = bound } };
    }
}
