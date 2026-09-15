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
                    Reasoning: new ReasoningConfiguration(
                        Mode: CandidateProfile.DisabledReasoningMode,
                        Effort: null),
                    JsonResponseMode: true),
                PromptRevision: "eligibility.v1",
                ValidatorRevision: "envelope.v1",
                new ContextBounds(
                    MaxOutputTokens: 4096,
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
                    Reasoning: new ReasoningConfiguration(
                        Mode: CandidateProfile.DisabledReasoningMode,
                        Effort: null),
                    JsonResponseMode: true),
                PromptRevision: "eligibility.v1",
                ValidatorRevision: "envelope.v1",
                new ContextBounds(
                    MaxOutputTokens: 4096,
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
                "Muse-Spark-1.3-Contributor",
                ChatCompletionsAdapterId,
                "https://openrouter.ai/api/v1",
                "meta/muse-spark-1.3-contributor",
                "judgment",
                new EffectiveSettings(
                    Temperature: 0,
                    TopP: null,
                    HasTools: false,
                    Reasoning: new ReasoningConfiguration(
                        Mode: CandidateProfile.EffortReasoningMode,
                        Effort: CandidateProfile.DefaultReasoningEffort),
                    JsonResponseMode: true),
                PromptRevision: "eligibility.v1",
                ValidatorRevision: "envelope.v1",
                new ContextBounds(
                    MaxOutputTokens: 10000,
                    MaxResponseBytes: 65536,
                    MaxInputTokens: 8192,
                    ContextCapacityTokens: 1048576,
                    AttemptTimeoutSeconds: 60),
                new BillingProfile(
                    Currency: "USD",
                    PriceSource: "OpenRouter",
                    PriceCheckDate: "2026-09-14",
                    PeakInputPerMillionTokens: 0.10m,
                    PeakOutputPerMillionTokens: 0.20m)),
            new CandidateProfile(
                "Qwen-Qwen3.8-Flash",
                ChatCompletionsAdapterId,
                "https://openrouter.ai/api/v1",
                "qwen/qwen3.8-flash",
                "judgment",
                new EffectiveSettings(
                    Temperature: 0,
                    TopP: null,
                    HasTools: false,
                    Reasoning: new ReasoningConfiguration(
                        Mode: CandidateProfile.EffortReasoningMode,
                        Effort: CandidateProfile.DefaultReasoningEffort),
                    JsonResponseMode: true),
                PromptRevision: "eligibility.v1",
                ValidatorRevision: "envelope.v1",
                new ContextBounds(
                    MaxOutputTokens: 10000,
                    MaxResponseBytes: 65536,
                    MaxInputTokens: 8192,
                    ContextCapacityTokens: 1000000,
                    AttemptTimeoutSeconds: 60),
                new BillingProfile(
                    Currency: "USD",
                    PriceSource: "OpenRouter",
                    PriceCheckDate: "2026-09-14",
                    PeakInputPerMillionTokens: 0.15m,
                    PeakOutputPerMillionTokens: 0.47m)),
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
