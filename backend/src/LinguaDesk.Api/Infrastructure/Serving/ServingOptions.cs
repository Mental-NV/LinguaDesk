namespace LinguaDesk.Api.Infrastructure.Serving;

/// <summary>
/// Per-family serving selection: the candidate to serve, the purpose-scoped
/// credential reference naming its key material, and the finite per-operation
/// spend ceiling enforced on the serving path. Key material itself is never
/// stored here; it is resolved from the owner-exported environment variable
/// for the credential reference at dispatch time.
/// </summary>
public sealed class ServingFamilyOptions
{
    public string CandidateId { get; set; } = string.Empty;

    public string CredentialRef { get; set; } = string.Empty;

    public decimal MaxSpendUsdPerOperation { get; set; } = 0.05m;
}

/// <summary>
/// Validated <c>Serving</c> configuration section. Defaults are intentionally
/// fail-closed: candidate/credential references are empty so the section does
/// not validate until the operator configures both families. The per-operation
/// ceiling defaults to the owner-locked $0.05.
/// </summary>
public sealed class ServingOptions
{
    public const string SectionName = "Serving";

    public ServingFamilyOptions Translation { get; set; } = new();

    public ServingFamilyOptions Rewriting { get; set; } = new();
}
