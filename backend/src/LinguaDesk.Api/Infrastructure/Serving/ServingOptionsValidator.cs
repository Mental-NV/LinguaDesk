using LinguaDesk.Infrastructure.Ai;
using Microsoft.Extensions.Options;

namespace LinguaDesk.Api.Infrastructure.Serving;

/// <summary>
/// Structural validation for the <c>Serving</c> section. Both families must
/// carry a known candidate ID, a non-empty credential reference matching the
/// selected candidate's purpose-scoped reference, and a positive finite
/// per-operation ceiling. Partial family configuration fails. Key presence is
/// checked separately at runtime and startup: blank/absent key material means
/// unconfigured, never a default credential.
/// </summary>
internal sealed class ServingOptionsValidator : IValidateOptions<ServingOptions>
{
    public ValidateOptionsResult Validate(string? name, ServingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();
        ValidateFamily("Translation", options.Translation, failures);
        ValidateFamily("Rewriting", options.Rewriting, failures);

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    internal static bool IsSectionValid(ServingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();
        ValidateFamily("Translation", options.Translation, failures);
        ValidateFamily("Rewriting", options.Rewriting, failures);
        return failures.Count == 0;
    }

    private static void ValidateFamily(string family, ServingFamilyOptions? selected, List<string> failures)
    {
        if (selected is null)
        {
            failures.Add($"Serving:{family} must be configured with a candidate, credential reference and per-operation ceiling.");
            return;
        }

        if (string.IsNullOrWhiteSpace(selected.CandidateId))
        {
            failures.Add($"Serving:{family}:CandidateId is required.");
            return;
        }

        CandidateProfile profile;
        try
        {
            profile = CandidateRegistry.Select(CandidateRegistry.Default, selected.CandidateId);
        }
        catch (CandidateProfileException)
        {
            failures.Add($"Serving:{family}:CandidateId '{selected.CandidateId}' is unknown; selection is by candidate ID only.");
            return;
        }

        if (string.IsNullOrWhiteSpace(selected.CredentialRef))
        {
            failures.Add($"Serving:{family}:CredentialRef is required and purpose-scoped.");
        }
        else if (!string.Equals(selected.CredentialRef, profile.CredentialRef, StringComparison.Ordinal))
        {
            failures.Add($"Serving:{family}:CredentialRef '{selected.CredentialRef}' does not match candidate '{profile.CandidateId}' credential reference '{profile.CredentialRef}'.");
        }

        if (!(selected.MaxSpendUsdPerOperation > 0))
        {
            failures.Add($"Serving:{family}:MaxSpendUsdPerOperation must be a positive finite USD amount.");
        }
    }
}
