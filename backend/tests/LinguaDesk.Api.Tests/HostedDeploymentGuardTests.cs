using LinguaDesk.Api.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting.Internal;

namespace LinguaDesk.Api.Tests;

[TestClass]
[DoNotParallelize]
public sealed class HostedDeploymentGuardTests
{
    private static IConfiguration Configuration(Dictionary<string, string?>? overrides = null)
    {
        var root = StoragePathPolicy.FindRepositoryRoot(AppContext.BaseDirectory)!;
        return new ConfigurationBuilder().SetBasePath(Path.Combine(root, "backend/src/LinguaDesk.Api"))
            .AddJsonFile("appsettings.json").AddJsonFile("appsettings.Production.json")
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Deployment:Enabled"] = "true",
                ["APPLICATIONINSIGHTS_CONNECTION_STRING"] = "InstrumentationKey=offline-test-only",
                ["LINGUADESK_AIEVALUATION:CREDENTIALS:DEEPSEEK:APIKEY"] = "offline-test-only-key",
            }).AddInMemoryCollection(overrides ?? []).Build();
    }

    [TestMethod]
    public void VersionedProfilesAndPlatformValuesPassPreflight() =>
        HostedDeploymentGuard.Validate(Configuration(), new HostingEnvironment { EnvironmentName = "Production" }, false);

    [TestMethod]
    [DataRow("Deployment:Enabled", "false")]
    [DataRow("APPLICATIONINSIGHTS_CONNECTION_STRING", "")]
    [DataRow("LINGUADESK_AIEVALUATION:CREDENTIALS:DEEPSEEK:APIKEY", "")]
    [DataRow("MonetaryAdmission:MonthlyCapMinorUnits", "0")]
    [DataRow("Storage:JournalMode", "Wal")]
    [DataRow("Storage:DatabasePath", "/tmp/volatile.db")]
    public void MissingOrOverriddenUnsafeConfigurationFailsBeforePreparation(string key, string value) =>
        Assert.ThrowsExactly<InvalidOperationException>(() => HostedDeploymentGuard.Validate(
            Configuration(new Dictionary<string, string?> { [key] = value }), new HostingEnvironment { EnvironmentName = "Production" }, false));

    [TestMethod]
    public void HostedProfilesRejectTestFlags()
    {
        const string flag = "LINGUADESK_SMOKE_SEED_ACCOUNTS";
        var previous = Environment.GetEnvironmentVariable(flag);
        try
        {
            Environment.SetEnvironmentVariable(flag, "1");
            Assert.ThrowsExactly<InvalidOperationException>(() => HostedDeploymentGuard.Validate(
                Configuration(), new HostingEnvironment { EnvironmentName = "Production" }, false));
        }
        finally { Environment.SetEnvironmentVariable(flag, previous); }
    }
}
