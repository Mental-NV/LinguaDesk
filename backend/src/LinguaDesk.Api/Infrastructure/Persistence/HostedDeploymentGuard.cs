using LinguaDesk.Api.Infrastructure.Serving;
using LinguaDesk.Api.Features.Operations;

namespace LinguaDesk.Api.Infrastructure.Persistence;

public static class HostedDeploymentGuard
{
    public static void Validate(IConfiguration configuration, IHostEnvironment environment, bool contractGeneration)
    {
        if (contractGeneration || !configuration.GetValue<bool>("Deployment:AzureAppService")) return;
        if (!configuration.GetValue<bool>("Deployment:Enabled")) throw new InvalidOperationException("Deployment is not enabled. Complete caps/settings/package reconciliation before startup.");
        if (string.IsNullOrWhiteSpace(configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"])) throw new InvalidOperationException("Hosted telemetry connection is missing.");
        if (environment.EnvironmentName is not ("Staging" or "Production")) throw new InvalidOperationException("Invalid hosted environment.");
        if (Environment.GetEnvironmentVariables().Keys.Cast<string>().Any(k => k.StartsWith("LINGUADESK_SMOKE_", StringComparison.Ordinal) || k.StartsWith("LINGUADESK_E2E_", StringComparison.Ordinal)))
            throw new InvalidOperationException("Test flags are forbidden in hosted profiles.");
        var serving = configuration.GetSection("Serving").Get<ServingOptions>();
        if (ServingConfiguration.CollectMissingVariables(serving, name => configuration[ServingCredential.ConfigurationKeyFor(name)]).Count != 0)
            throw new InvalidOperationException("Hosted serving configuration or credentials are missing.");
        if (configuration.GetValue<long>("MonetaryAdmission:MonthlyCapMinorUnits") <= 0 || string.IsNullOrWhiteSpace(configuration["MonetaryAdmission:Currency"]))
            throw new InvalidOperationException("Hosted monetary bounds are required.");
        if (configuration["Storage:JournalMode"] != "Delete" || configuration["Storage:DatabasePath"] != "/home/linguadesk/data/linguadesk.db" || configuration["Security:DataProtectionKeysPath"] != "/home/linguadesk/keys")
            throw new InvalidOperationException("Hosted durable storage profile is invalid.");
    }
}
