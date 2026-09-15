namespace LinguaDesk.Api.Infrastructure.Serving;

/// <summary>
/// Authoritative serving readiness check shared by startup and tests. Returns
/// the missing variable names when the portal must not serve: unconfigured or
/// partially configured families, or blank/absent key material for a
/// configured credential reference. An empty result means the portal may
/// serve. No key material is ever returned.
/// </summary>
internal static class ServingConfiguration
{
    internal static IReadOnlyList<string> CollectMissingVariables(
        ServingOptions? options,
        Func<string, string?> readEnvironment)
    {
        ArgumentNullException.ThrowIfNull(readEnvironment);

        var missing = new List<string>();
        var seenKeys = new HashSet<string>(StringComparer.Ordinal);
        CollectFamily("Translation", options?.Translation, readEnvironment, missing, seenKeys);
        CollectFamily("Rewriting", options?.Rewriting, readEnvironment, missing, seenKeys);
        if (missing.Count == 0 && !IsSectionValid(options))
        {
            missing.Add("Serving__Translation__CandidateId");
            missing.Add("Serving__Rewriting__CandidateId");
        }

        return missing.AsReadOnly();
    }

    internal static bool IsSectionValid(ServingOptions? options) =>
        options is not null && ServingOptionsValidator.IsSectionValid(options);

    private static void CollectFamily(
        string family,
        ServingFamilyOptions? selected,
        Func<string, string?> readEnvironment,
        List<string> missing,
        HashSet<string> seenKeys)
    {
        var candidateKey = $"Serving__{family}__CandidateId";
        var credentialKey = $"Serving__{family}__CredentialRef";

        if (selected is null || string.IsNullOrWhiteSpace(selected.CandidateId))
        {
            missing.Add(candidateKey);
        }

        if (selected is null || string.IsNullOrWhiteSpace(selected.CredentialRef))
        {
            missing.Add(credentialKey);
        }
        else
        {
            var variable = ServingCredential.VariableFor(selected.CredentialRef);
            if (seenKeys.Add(variable) && string.IsNullOrWhiteSpace(readEnvironment(variable)))
            {
                missing.Add(variable);
            }
        }

        if (selected is not null && !(selected.MaxSpendUsdPerOperation > 0))
        {
            missing.Add($"Serving__{family}__MaxSpendUsdPerOperation");
        }
    }
}
