namespace LinguaDesk.Api.Infrastructure.Serving;

/// <summary>
/// Fail-fast serving guard for the local <c>run</c> entry point: refuses to
/// serve the portal from the Development environment when the serving section
/// or its key material is unconfigured, so no user can reach a
/// Translate/Rewrite retry loop caused by missing serving configuration.
/// <c>scripts/backend.sh run</c> enforces the same check before restoring or
/// listening, which also covers non-development invocations of the script.
/// Production-like hosting keeps the established fail-closed contract
/// (unconfigured submissions remain pending reservations; see the M006
/// startup tests) and never crashes on missing serving configuration.
/// </summary>
internal static class ServingStartupGuard
{
    internal static void ThrowWhenServingUnconfigured(
        IConfiguration configuration,
        IHostEnvironment environment,
        bool isContractGeneration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        if (isContractGeneration || !environment.IsEnvironment("Development"))
        {
            return;
        }

        var serving = configuration.GetSection(ServingOptions.SectionName).Get<ServingOptions>();
        var missing = ServingConfiguration.CollectMissingVariables(
            serving,
            static name => Environment.GetEnvironmentVariable(name));
        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                "LinguaDesk serving is not configured; the portal refuses to start without live translation/rewriting. Missing: "
                + string.Join(", ", missing)
                + ".");
        }
    }
}
