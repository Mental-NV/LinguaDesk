namespace LinguaDesk.Infrastructure.Ai;

public static class CandidateRegistry
{
    public const string ChatCompletionsAdapterId = "openai-chat-completions-v1";

    public static IReadOnlyList<CandidateProfile> Default { get; } = Load(
        [
            new CandidateProfile(
                "DeepSeek-V4.1-Flash",
                ChatCompletionsAdapterId,
                "https://api.deepseek.com",
                "deepseek-flash",
                "deepseek",
                new EffectiveSettings(
                    Temperature: 0,
                    TopP: null,
                    HasTools: false,
                    Thinking: CandidateProfile.RequiredThinkingMode,
                    JsonResponseMode: true),
                PromptRevision: "eligibility.v1",
                ValidatorRevision: "envelope.v1",
                new ContextBounds(
                    MaxOutputTokens: 256,
                    MaxResponseBytes: 65536,
                    MaxInputTokens: 8192,
                    ContextCapacityTokens: 1000000,
                    AttemptTimeoutSeconds: 60),
                new BillingProfile(
                    Currency: "USD",
                    PriceSource: "DeepSeek models and pricing",
                    PriceCheckDate: "2026-09-11",
                    PeakInputPerMillionTokens: 0.30m,
                    PeakOutputPerMillionTokens: 1.20m)),
            new CandidateProfile(
                "DeepSeek-V4.1-Flash-SecondaryRef",
                ChatCompletionsAdapterId,
                "https://api.deepseek.com",
                "deepseek-flash",
                "deepseek-secondary",
                new EffectiveSettings(
                    Temperature: 0,
                    TopP: null,
                    HasTools: false,
                    Thinking: CandidateProfile.RequiredThinkingMode,
                    JsonResponseMode: true),
                PromptRevision: "eligibility.v1",
                ValidatorRevision: "envelope.v1",
                new ContextBounds(
                    MaxOutputTokens: 256,
                    MaxResponseBytes: 65536,
                    MaxInputTokens: 8192,
                    ContextCapacityTokens: 1000000,
                    AttemptTimeoutSeconds: 60),
                new BillingProfile(
                    Currency: "USD",
                    PriceSource: "DeepSeek models and pricing",
                    PriceCheckDate: "2026-09-11",
                    PeakInputPerMillionTokens: 0.30m,
                    PeakOutputPerMillionTokens: 1.20m)),
        ]);

    public static IReadOnlyList<CandidateProfile> Load(IEnumerable<CandidateProfile> profiles)
    {
        ArgumentNullException.ThrowIfNull(profiles);

        var loaded = profiles.ToArray();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var profile in loaded)
        {
            ArgumentNullException.ThrowIfNull(profile);
            profile.Validate();

            if (!seen.Add(profile.CandidateId))
            {
                throw new CandidateProfileException(
                    profile.CandidateId,
                    [$"Duplicate candidate ID '{profile.CandidateId}'."]);
            }
        }

        return Array.AsReadOnly(loaded);
    }

    public static CandidateProfile Select(IReadOnlyList<CandidateProfile> profiles, string candidateId)
    {
        ArgumentNullException.ThrowIfNull(profiles);
        ArgumentException.ThrowIfNullOrWhiteSpace(candidateId);

        foreach (var profile in profiles)
        {
            if (string.Equals(profile.CandidateId, candidateId, StringComparison.Ordinal))
            {
                return profile;
            }
        }

        throw new CandidateProfileException(
            candidateId,
            [$"Unknown candidate ID '{candidateId}'. Selection is by candidate ID only; there are no family-owned credentials or route-table defaults."]);
    }
}
