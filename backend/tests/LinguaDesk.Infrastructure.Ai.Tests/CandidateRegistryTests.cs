using System.Text.Json;
using LinguaDesk.Infrastructure.Ai;

namespace LinguaDesk.Infrastructure.Ai.Tests;

[TestClass]
public sealed class CandidateRegistryTests
{
    [TestMethod]
    public void DefaultRegistryHoldsTwoSelectableNonSecretProfiles()
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
        Assert.AreEqual("disabled", deepSeek.Settings.Thinking);
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
    public void EnabledThinkingIsRejected()
    {
        var profile = CandidateRegistry.Default[0] with
        {
            Settings = CandidateRegistry.Default[0].Settings with { Thinking = "enabled" },
        };

        Assert.ThrowsExactly<CandidateProfileException>(
            () => CandidateRegistry.Load([profile]));
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
