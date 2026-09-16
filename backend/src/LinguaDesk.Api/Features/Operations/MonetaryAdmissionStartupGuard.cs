namespace LinguaDesk.Api.Features.Operations;

/// <summary>
/// Fail-fast monetary guard for the local <c>run</c> entry point: refuses to
/// serve the portal from the Development environment when the monetary
/// admission section is missing or invalid, so no user reaches a
/// Translate/Rewrite suspension caused by missing monetary configuration.
/// <c>scripts/backend.sh run</c> enforces the same check before restoring or
/// listening. Production-like hosting keeps the established fail-closed
/// contract (unconfigured paid attempts deny with monetary suspension at
/// request time) and never crashes on missing monetary configuration.
/// </summary>
internal static class MonetaryAdmissionStartupGuard
{
    internal static void ThrowWhenMonetaryUnconfigured(
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

        var monetary = configuration.GetSection(MonetaryAdmissionOptions.SectionName).Get<MonetaryAdmissionOptions>();
        var missing = new List<string>();
        if (monetary is null || monetary.MonthlyCapMinorUnits <= 0)
        {
            missing.Add("MonetaryAdmission__MonthlyCapMinorUnits");
        }

        if (monetary is null || string.IsNullOrWhiteSpace(monetary.Currency))
        {
            missing.Add("MonetaryAdmission__Currency");
        }

        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                "LinguaDesk monetary admission is not configured; the portal refuses to start without a monthly spend ceiling. Missing or invalid: "
                + string.Join(", ", missing)
                + ". Set the MonetaryAdmission section in appsettings.json or override per run with the MonetaryAdmission__MonthlyCapMinorUnits / MonetaryAdmission__Currency environment variables.");
        }
    }
}
