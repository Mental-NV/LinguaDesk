namespace LinguaDesk.Infrastructure.Ai;

public enum ChainFamily
{
    Translation,
    Rewriting,
}

public sealed record FamilyChain(
    ChainFamily Family,
    CandidateProfile Primary,
    CandidateProfile? Fallback)
{
    public const string EvaluationPrimaryId = "DeepSeek-V4.1-Flash";

    public const string EvaluationFallbackId = "DeepSeek-V4.1-Flash-SecondaryRef";

    public static FamilyChain Create(
        ChainFamily family,
        IReadOnlyList<CandidateProfile> registry,
        string primaryId,
        string? fallbackId = null)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentException.ThrowIfNullOrWhiteSpace(primaryId);

        var primary = CandidateRegistry.Select(registry, primaryId);

        if (fallbackId is null)
        {
            return new FamilyChain(family, primary, null);
        }

        var fallback = CandidateRegistry.Select(registry, fallbackId);

        if (fallback == primary)
        {
            throw new CandidateProfileException(
                fallback.CandidateId,
                [$"Family chain '{family}' rejects identical primary/fallback profiles: repeating a candidate is a hidden retry."]);
        }

        return new FamilyChain(family, primary, fallback);
    }

    public static FamilyChain EvaluationDefault(ChainFamily family) =>
        Create(family, CandidateRegistry.Default, EvaluationPrimaryId, EvaluationFallbackId);

    public IEnumerable<CandidateProfile> CandidatesInOrder()
    {
        yield return Primary;

        if (Fallback is not null)
        {
            yield return Fallback;
        }
    }
}
