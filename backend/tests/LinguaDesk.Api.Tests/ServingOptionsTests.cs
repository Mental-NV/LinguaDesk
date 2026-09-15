using LinguaDesk.Api.Infrastructure.Serving;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace LinguaDesk.Api.Tests;

[TestClass]
public sealed class ServingOptionsTests
{
    private static ServingOptions ValidOptions() => new()
    {
        Translation = new ServingFamilyOptions
        {
            CandidateId = "DeepSeek-V4.1-Flash",
            CredentialRef = "deepseek",
            MaxSpendUsdPerOperation = 0.05m,
        },
        Rewriting = new ServingFamilyOptions
        {
            CandidateId = "DeepSeek-V4.1-Flash",
            CredentialRef = "deepseek",
            MaxSpendUsdPerOperation = 0.05m,
        },
    };

    [TestMethod]
    public void DefaultsFailClosed()
    {
        var result = new ServingOptionsValidator().Validate(null, new ServingOptions());

        Assert.IsFalse(result.Succeeded);
    }

    [TestMethod]
    public void ValidSectionPasses()
    {
        var result = new ServingOptionsValidator().Validate(null, ValidOptions());

        Assert.IsTrue(result.Succeeded, result.FailureMessage);
        Assert.IsTrue(ServingConfiguration.IsSectionValid(ValidOptions()));
    }

    [TestMethod]
    public void UnknownCandidateFails()
    {
        var options = ValidOptions();
        options.Translation.CandidateId = "No-Such-Candidate";

        var result = new ServingOptionsValidator().Validate(null, options);

        Assert.IsFalse(result.Succeeded);
        StringAssert.Contains(result.FailureMessage ?? string.Empty, "Serving:Translation:CandidateId");
    }

    [TestMethod]
    public void MismatchedCredentialRefFails()
    {
        var options = ValidOptions();
        options.Translation.CredentialRef = "deepseek-secondary";

        var result = new ServingOptionsValidator().Validate(null, options);

        Assert.IsFalse(result.Succeeded);
        StringAssert.Contains(result.FailureMessage ?? string.Empty, "Serving:Translation:CredentialRef");
    }

    [TestMethod]
    public void BlankCredentialRefFails()
    {
        var options = ValidOptions();
        options.Rewriting.CredentialRef = "  ";

        var result = new ServingOptionsValidator().Validate(null, options);

        Assert.IsFalse(result.Succeeded);
        StringAssert.Contains(result.FailureMessage ?? string.Empty, "Serving:Rewriting:CredentialRef");
    }

    [TestMethod]
    public void NonPositiveCeilingFails()
    {
        var options = ValidOptions();
        options.Translation.MaxSpendUsdPerOperation = 0m;

        Assert.IsFalse(new ServingOptionsValidator().Validate(null, options).Succeeded);

        options.Translation.MaxSpendUsdPerOperation = -0.01m;
        Assert.IsFalse(new ServingOptionsValidator().Validate(null, options).Succeeded);
    }

    [TestMethod]
    public void PartialFamilyConfigurationFails()
    {
        var options = ValidOptions();
        options.Rewriting.CandidateId = string.Empty;

        var result = new ServingOptionsValidator().Validate(null, options);

        Assert.IsFalse(result.Succeeded);
        Assert.IsFalse(ServingConfiguration.IsSectionValid(options));
    }

    [TestMethod]
    public void MissingVariablesNameSectionAndKey()
    {
        var missing = ServingConfiguration.CollectMissingVariables(
            new ServingOptions(),
            static _ => null);

        CollectionAssert.Contains(missing.ToArray(), "Serving__Translation__CandidateId");
        CollectionAssert.Contains(missing.ToArray(), "Serving__Translation__CredentialRef");
        CollectionAssert.Contains(missing.ToArray(), "Serving__Rewriting__CandidateId");
        CollectionAssert.Contains(missing.ToArray(), "Serving__Rewriting__CredentialRef");
    }

    [TestMethod]
    public void BlankKeyMaterialIsReportedByVariableName()
    {
        var missing = ServingConfiguration.CollectMissingVariables(
            ValidOptions(),
            static name => name.EndsWith("__APIKEY", StringComparison.Ordinal) ? "  " : "present");

        Assert.HasCount(1, missing);
        Assert.AreEqual(
            "LINGUADESK_AIEVALUATION__CREDENTIALS__DEEPSEEK__APIKEY",
            missing[0]);
    }

    [TestMethod]
    public void ConfiguredSectionWithKeyPresentReportsNothing()
    {
        var missing = ServingConfiguration.CollectMissingVariables(
            ValidOptions(),
            static _ => "present");

        Assert.HasCount(0, missing);
    }

    [TestMethod]
    public void CredentialVariableDerivesFromReference()
    {
        Assert.AreEqual(
            "LINGUADESK_AIEVALUATION__CREDENTIALS__DEEPSEEK__APIKEY",
            ServingCredential.VariableFor("deepseek"));
        Assert.AreEqual(
            "LINGUADESK_AIEVALUATION__CREDENTIALS__DEEPSEEK_SECONDARY__APIKEY",
            ServingCredential.VariableFor("deepseek-secondary"));
    }

    [TestMethod]
    public void PerOperationBudgetDefaultsToFiveCents()
    {
        var translation = ServingOperationBudget.ForTranslation(Options.Create(new ServingOptions()));
        var rewriting = ServingOperationBudget.ForRewriting(Options.Create(new ServingOptions()));

        Assert.AreEqual(0.05m, translation.MaxSpendUsd);
        Assert.AreEqual(0.05m, rewriting.MaxSpendUsd);
        Assert.AreEqual(4, translation.MaxDispatches);
        Assert.AreEqual(4, rewriting.MaxDispatches);
    }

    [TestMethod]
    public void CommittedAppSettingsBindsToValidServingSection()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(FindAppSettings(), optional: false, reloadOnChange: false)
            .Build();
        var options = configuration.GetSection(ServingOptions.SectionName).Get<ServingOptions>();

        Assert.IsNotNull(options);
        Assert.IsTrue(new ServingOptionsValidator().Validate(null, options).Succeeded);
        Assert.AreEqual("DeepSeek-V4.1-Flash", options.Translation.CandidateId);
        Assert.AreEqual("deepseek", options.Translation.CredentialRef);
        Assert.AreEqual(0.05m, options.Translation.MaxSpendUsdPerOperation);
        Assert.AreEqual("DeepSeek-V4.1-Flash", options.Rewriting.CandidateId);
        Assert.AreEqual("deepseek", options.Rewriting.CredentialRef);
        Assert.AreEqual(0.05m, options.Rewriting.MaxSpendUsdPerOperation);
    }

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
}
