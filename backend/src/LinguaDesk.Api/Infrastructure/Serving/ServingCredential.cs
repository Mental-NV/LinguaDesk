using LinguaDesk.Infrastructure.Ai;

namespace LinguaDesk.Api.Infrastructure.Serving;

/// <summary>
/// Resolves purpose-scoped transport credentials through <see cref="IConfiguration"/>
/// so JSON, environment and other providers share one lookup. The value itself
/// stays env-only: variable shape mirrors the evaluation composition,
/// <c>LINGUADESK_AIEVALUATION__CREDENTIALS__&lt;REF&gt;__APIKEY</c> with the
/// reference uppercased and <c>-</c> mapped to <c>_</c>. Blank or absent
/// material means unconfigured and never yields a default credential. No key
/// material enters options, profiles, logs or evidence; diagnostics carry at
/// most the credential reference plus presence.
/// </summary>
internal static class ServingCredential
{
    internal const string Prefix = "LINGUADESK_AIEVALUATION__CREDENTIALS__";

    internal const string Suffix = "__APIKEY";

    internal static string VariableFor(string credentialRef)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(credentialRef);
        return Prefix
            + credentialRef.ToUpperInvariant().Replace("-", "_", StringComparison.Ordinal)
            + Suffix;
    }

    internal static string ConfigurationKeyFor(string variable)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(variable);
        return variable.Replace("__", ":", StringComparison.Ordinal);
    }

    internal static bool TryResolve(
        IConfiguration configuration,
        string credentialRef,
        out TransportCredential? credential)
    {
        credential = null;
        ArgumentNullException.ThrowIfNull(configuration);
        if (string.IsNullOrWhiteSpace(credentialRef))
        {
            return false;
        }

        var value = configuration[ConfigurationKeyFor(VariableFor(credentialRef))];
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        credential = new TransportCredential(value);
        return true;
    }
}
