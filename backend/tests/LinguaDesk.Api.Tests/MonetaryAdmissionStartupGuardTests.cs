using LinguaDesk.Api.Features.Operations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace LinguaDesk.Api.Tests;

[TestClass]
public sealed class MonetaryAdmissionStartupGuardTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public void ValidConfigurationPasses()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MonetaryAdmission:MonthlyCapMinorUnits"] = "1000000",
                ["MonetaryAdmission:Currency"] = "USD",
            })
            .Build();

        MonetaryAdmissionStartupGuard.ThrowWhenMonetaryUnconfigured(configuration, Development(), isContractGeneration: false);
    }

    [TestMethod]
    public void MissingSectionThrowsNamingBothVariables()
    {
        var configuration = new ConfigurationBuilder().Build();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            MonetaryAdmissionStartupGuard.ThrowWhenMonetaryUnconfigured(configuration, Development(), isContractGeneration: false));

        StringAssert.Contains(exception.Message, "MonetaryAdmission__MonthlyCapMinorUnits");
        StringAssert.Contains(exception.Message, "MonetaryAdmission__Currency");
    }

    [TestMethod]
    public void ZeroCapThrowsNamingCapVariable()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MonetaryAdmission:MonthlyCapMinorUnits"] = "0",
                ["MonetaryAdmission:Currency"] = "USD",
            })
            .Build();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            MonetaryAdmissionStartupGuard.ThrowWhenMonetaryUnconfigured(configuration, Development(), isContractGeneration: false));

        StringAssert.Contains(exception.Message, "MonetaryAdmission__MonthlyCapMinorUnits");
    }

    [TestMethod]
    public void BlankCurrencyThrowsNamingCurrencyVariable()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MonetaryAdmission:MonthlyCapMinorUnits"] = "1000000",
                ["MonetaryAdmission:Currency"] = "  ",
            })
            .Build();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            MonetaryAdmissionStartupGuard.ThrowWhenMonetaryUnconfigured(configuration, Development(), isContractGeneration: false));

        StringAssert.Contains(exception.Message, "MonetaryAdmission__Currency");
    }

    [TestMethod]
    public void ProductionKeepsFailClosedContract()
    {
        var configuration = new ConfigurationBuilder().Build();

        MonetaryAdmissionStartupGuard.ThrowWhenMonetaryUnconfigured(configuration, new TestHostEnvironment("Production"), isContractGeneration: false);
    }

    [TestMethod]
    public void ContractGenerationSkipsGuard()
    {
        var configuration = new ConfigurationBuilder().Build();

        MonetaryAdmissionStartupGuard.ThrowWhenMonetaryUnconfigured(configuration, Development(), isContractGeneration: true);
    }

    [TestMethod]
    public void CommittedAppSettingsBindsToValidMonetarySection()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(FindAppSettings(), optional: false, reloadOnChange: false)
            .Build();
        var options = configuration.GetSection(MonetaryAdmissionOptions.SectionName).Get<MonetaryAdmissionOptions>();

        Assert.IsNotNull(options);
        Assert.IsGreaterThan(0, options.MonthlyCapMinorUnits);
        Assert.IsFalse(string.IsNullOrWhiteSpace(options.Currency));

        MonetaryAdmissionStartupGuard.ThrowWhenMonetaryUnconfigured(configuration, Development(), isContractGeneration: false);
    }

    [TestMethod]
    public void EnvironmentOverrideWinsOverJsonFile()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(FindAppSettings(), optional: false, reloadOnChange: false)
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MonetaryAdmission:MonthlyCapMinorUnits"] = "2500000",
            })
            .Build();
        var options = configuration.GetSection(MonetaryAdmissionOptions.SectionName).Get<MonetaryAdmissionOptions>();

        Assert.IsNotNull(options);
        Assert.AreEqual(2500000, options.MonthlyCapMinorUnits);

        MonetaryAdmissionStartupGuard.ThrowWhenMonetaryUnconfigured(configuration, Development(), isContractGeneration: false);
    }

    private static TestHostEnvironment Development() => new("Development");

    private static string FindAppSettings()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var path = Path.Combine(
                directory.FullName,
                "backend",
                "src",
                "LinguaDesk.Api",
                "appsettings.json");
            if (File.Exists(path))
            {
                return path;
            }

            directory = directory.Parent;
        }

        Assert.Fail("Could not locate the committed appsettings.json from the test output directory.");
        return string.Empty;
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;

        public string ApplicationName { get; set; } = "monetary-guard-test";

        public string ContentRootPath { get; set; } = ".";

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
