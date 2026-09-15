using System.Text.Json;
using LinguaDesk.Infrastructure.Ai;

namespace LinguaDesk.Infrastructure.Ai.Tests;

[TestClass]
public sealed class CandidateRegistryTests
{
    [TestMethod]
    public void DefaultRegistryHoldsSelectableNonSecretProfiles()
    {
        var defaults = CandidateRegistry.Default;

        Assert.IsGreaterThanOrEqualTo(2, defaults.Count);
        var deepSeek = CandidateRegistry.Select(defaults, "DeepSeek-V4.1-Flash");

        Assert.AreEqual(CandidateRegistry.ChatCompletionsAdapterId, deepSeek.AdapterId);
        Assert.AreEqual("https://api.deepseek.com", deepSeek.Endpoint);
        Assert.AreEqual("deepseek-flash", deepSeek.Model);
        Assert.AreEqual("deepseek", deepSeek.CredentialRef);
        Assert.AreEqual(0, deepSeek.Settings.Temperature);
        Assert.IsNull(deepSeek.Settings.TopP);
        Assert.IsFalse(deepSeek.Settings.HasTools);
        Assert.AreEqual(CandidateProfile.DisabledReasoningMode, deepSeek.Settings.Reasoning.Mode);
        Assert.IsNull(deepSeek.Settings.Reasoning.Effort);
        Assert.IsTrue(deepSeek.Settings.JsonResponseMode);
        Assert.IsGreaterThan(0, deepSeek.Bounds.MaxOutputTokens);
        Assert.IsGreaterThan(0, deepSeek.Bounds.MaxResponseBytes);
        Assert.IsGreaterThan(0, deepSeek.Bounds.MaxInputTokens);
        Assert.IsGreaterThan(0, deepSeek.Bounds.AttemptTimeoutSeconds);
        Assert.IsGreaterThan(0m, deepSeek.Billing.PeakInputPerMillionTokens);
        Assert.IsGreaterThan(0m, deepSeek.Billing.PeakOutputPerMillionTokens);

        var second = CandidateRegistry.Select(defaults, "DeepSeek-V4.1-Flash-SecondaryRef");
        Assert.AreNotEqual(deepSeek.CandidateId, second.CandidateId);
        Assert.AreNotEqual(deepSeek.CredentialRef, second.CredentialRef);
        Assert.AreEqual(deepSeek.AdapterId, second.AdapterId);
        Assert.AreEqual(4096, deepSeek.Bounds.MaxOutputTokens);
        Assert.AreEqual(4096, second.Bounds.MaxOutputTokens);

        var muse = CandidateRegistry.Select(defaults, "Muse-Spark-1.3-Contributor");
        Assert.AreEqual("Muse-Spark-1.3-Contributor", muse.CandidateId);
        Assert.AreEqual("https://openrouter.ai/api/v1", muse.Endpoint);
        Assert.AreEqual("meta/muse-spark-1.3-contributor", muse.Model);
        Assert.AreEqual("judgment", muse.CredentialRef);
        Assert.AreEqual(10000, muse.Bounds.MaxOutputTokens);

        var qwen = CandidateRegistry.Select(defaults, "Qwen-Qwen3.8-Flash");
        Assert.AreEqual("Qwen-Qwen3.8-Flash", qwen.CandidateId);
        Assert.AreEqual("https://openrouter.ai/api/v1", qwen.Endpoint);
        Assert.AreEqual("qwen/qwen3.8-flash", qwen.Model);
        Assert.AreEqual("judgment", qwen.CredentialRef);
        Assert.AreEqual(10000, qwen.Bounds.MaxOutputTokens);

        var serialized = JsonSerializer.Serialize(defaults);
        Assert.IsFalse(serialized.Contains("apiKey", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(serialized.Contains("Bearer", StringComparison.Ordinal));
    }

    [TestMethod]
    public void DuplicateCandidateIdsAreRejected()
    {
        var first = CandidateRegistry.Default[0];

        var exception = Assert.ThrowsExactly<CandidateProfileException>(
            () => CandidateRegistry.Load([first, first with { }]));

        StringAssert.Contains(exception.Message, "Duplicate");
    }

    [TestMethod]
    public void PlainHttpEndpointIsRejected()
    {
        var profile = CandidateRegistry.Default[0] with { Endpoint = "http://api.deepseek.com" };

        var exception = Assert.ThrowsExactly<CandidateProfileException>(
            () => CandidateRegistry.Load([profile]));

        StringAssert.Contains(exception.Message, "HTTPS");
    }

    [TestMethod]
    public void UnknownReasoningModeIsRejected()
    {
        var profile = CandidateRegistry.Default[0] with
        {
            Settings = CandidateRegistry.Default[0].Settings with
            {
                Reasoning = new ReasoningConfiguration("enabled", null),
            },
        };

        Assert.ThrowsExactly<CandidateProfileException>(
            () => CandidateRegistry.Load([profile]));
    }

    [TestMethod]
    public void UnknownReasoningEffortIsRejected()
    {
        var muse = CandidateRegistry.Select(CandidateRegistry.Default, "Muse-Spark-1.3-Contributor");
        var profile = muse with
        {
            Settings = muse.Settings with
            {
                Reasoning = new ReasoningConfiguration(CandidateProfile.EffortReasoningMode, "extreme"),
            },
        };

        Assert.ThrowsExactly<CandidateProfileException>(
            () => CandidateRegistry.Load([profile]));
    }

    [TestMethod]
    public void ProviderReasoningModesAreEnforced()
    {
        var deepSeek = CandidateRegistry.Default[0];
        var deepSeekWithEffort = deepSeek with
        {
            Settings = deepSeek.Settings with
            {
                Reasoning = new ReasoningConfiguration(CandidateProfile.EffortReasoningMode, "low"),
            },
        };
        var muse = CandidateRegistry.Select(CandidateRegistry.Default, "Muse-Spark-1.3-Contributor");
        var museWithDisabledReasoning = muse with
        {
            Settings = muse.Settings with
            {
                Reasoning = new ReasoningConfiguration(CandidateProfile.DisabledReasoningMode, null),
            },
        };

        Assert.ThrowsExactly<CandidateProfileException>(() => CandidateRegistry.Load([deepSeekWithEffort]));
        Assert.ThrowsExactly<CandidateProfileException>(() => CandidateRegistry.Load([museWithDisabledReasoning]));
    }

    [TestMethod]
    public void DefaultProfilesStayWithinPerOperationBudgetCapacityAndResponseBounds()
    {
        foreach (var profile in CandidateRegistry.Default)
        {
            var bound = EvaluationBudget.UpperBoundUsd(
                profile.Bounds.MaxInputTokens,
                profile.Bounds.MaxOutputTokens,
                profile.Billing.PeakInputPerMillionTokens,
                profile.Billing.PeakOutputPerMillionTokens);
            var expectedBound = profile.CandidateId switch
            {
                "DeepSeek-V4.1-Flash" => 0.007373m,
                "DeepSeek-V4.1-Flash-SecondaryRef" => 0.007373m,
                "Muse-Spark-1.3-Contributor" => 0.002820m,
                "Qwen-Qwen3.8-Flash" => 0.005929m,
                _ => throw new AssertFailedException($"Missing expected bound for '{profile.CandidateId}'."),
            };

            Assert.AreEqual(expectedBound, bound, profile.CandidateId);
            Assert.IsLessThanOrEqualTo(0.20m, bound, profile.CandidateId);
            Assert.IsLessThanOrEqualTo(
                profile.Bounds.ContextCapacityTokens,
                profile.Bounds.MaxInputTokens + profile.Bounds.MaxOutputTokens,
                profile.CandidateId);
            Assert.IsLessThan(
                profile.Bounds.MaxResponseBytes,
                profile.Bounds.MaxOutputTokens * 4,
                profile.CandidateId);
        }
    }

    [TestMethod]
    public void TopPOverrideIsRejected()
    {
        var profile = CandidateRegistry.Default[0] with
        {
            Settings = CandidateRegistry.Default[0].Settings with { TopP = 0.9 },
        };

        Assert.ThrowsExactly<CandidateProfileException>(
            () => CandidateRegistry.Load([profile]));
    }

    [TestMethod]
    public void NonZeroTemperatureIsRejected()
    {
        var profile = CandidateRegistry.Default[0] with
        {
            Settings = CandidateRegistry.Default[0].Settings with { Temperature = 0.7 },
        };

        Assert.ThrowsExactly<CandidateProfileException>(
            () => CandidateRegistry.Load([profile]));
    }

    [TestMethod]
    public void RequestedToolsAreRejected()
    {
        var profile = CandidateRegistry.Default[0] with
        {
            Settings = CandidateRegistry.Default[0].Settings with { HasTools = true },
        };

        Assert.ThrowsExactly<CandidateProfileException>(
            () => CandidateRegistry.Load([profile]));
    }

    [TestMethod]
    public void UnlimitedBoundsAreRejected()
    {
        var profile = CandidateRegistry.Default[0] with
        {
            Bounds = CandidateRegistry.Default[0].Bounds with { MaxOutputTokens = 0 },
        };

        Assert.ThrowsExactly<CandidateProfileException>(
            () => CandidateRegistry.Load([profile]));
    }

    [TestMethod]
    public void MissingBillingBoundIsRejected()
    {
        var profile = CandidateRegistry.Default[0] with
        {
            Billing = CandidateRegistry.Default[0].Billing with { PeakOutputPerMillionTokens = 0m },
        };

        Assert.ThrowsExactly<CandidateProfileException>(
            () => CandidateRegistry.Load([profile]));
    }

    [TestMethod]
    public void UnknownCandidateSelectionFailsWithoutFamilyOrRouteDefaults()
    {
        var exception = Assert.ThrowsExactly<CandidateProfileException>(
            () => CandidateRegistry.Select(CandidateRegistry.Default, "No-Such-Candidate"));

        StringAssert.Contains(exception.Message, "Unknown candidate ID");
    }
}
