using System.Text.Json;
using LinguaDesk.Api.Features.Operations;
using LinguaDesk.Api.Infrastructure.Smoke;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace LinguaDesk.Api.Tests;

[TestClass]
public sealed class SmokeDeterministicRewritingTests
{
    private static ChatMessage UserPrompt(string json) => new(ChatRole.User, json);

    private static string ResponseText(ChatResponse response) =>
        response.Messages.Last().Text ?? string.Empty;

    private static JsonElement ParseResult(string responseJson)
    {
        using var document = JsonDocument.Parse(responseJson);
        return document.RootElement.Clone();
    }

    [TestMethod]
    public void ProviderResolvesEvaluationChain()
    {
        var provider = new DeterministicSmokeRewritingProvider();
        Assert.IsTrue(provider.TryGetClients(out var router, out var chain));
        Assert.IsNotNull(router);
        Assert.IsNotNull(chain);
        var profile = LinguaDesk.Infrastructure.Ai.CandidateRegistry.Select(
            LinguaDesk.Infrastructure.Ai.CandidateRegistry.Default,
            LinguaDesk.Infrastructure.Ai.FamilyChain.EvaluationPrimaryId);
        Assert.IsNotNull(router(profile));
    }

    [TestMethod]
    public async Task EligibilityCallIsEligibleEnglishForOrdinarySource()
    {
        var client = DeterministicSmokeRewritingClient.Instance;
        var response = await client.GetResponseAsync(
            [UserPrompt("""{"source":"The report is really ready. We sends it today.","sourceHint":null}""")],
            cancellationToken: TestContext.CancellationToken);
        var result = ParseResult(ResponseText(response));
        Assert.AreEqual("eligible", result.GetProperty("status").GetString());
        Assert.AreEqual("en", result.GetProperty("language").GetString());
    }

    [TestMethod]
    public async Task EligibilityRejectSentinelIsUnsupportedWithoutLanguage()
    {
        var client = DeterministicSmokeRewritingClient.Instance;
        var response = await client.GetResponseAsync(
            [UserPrompt("""{"source":"Report M029-ELIGIBILITY-REJECT","sourceHint":null}""")],
            cancellationToken: TestContext.CancellationToken);
        var result = ParseResult(ResponseText(response));
        Assert.AreEqual("unsupported", result.GetProperty("status").GetString());
        Assert.AreEqual(JsonValueKind.Null, result.GetProperty("language").ValueKind);
    }

    [TestMethod]
    public async Task TransformationCallReturnsFixedRewritingFixture()
    {
        var client = DeterministicSmokeRewritingClient.Instance;
        var response = await client.GetResponseAsync(
            [UserPrompt("""{"source":"The report is really ready. We sends it today.","sourceLanguage":"en","mode":"correctionOnly"}""")],
            cancellationToken: TestContext.CancellationToken);
        var result = ParseResult(ResponseText(response));
        Assert.AreEqual("result", result.GetProperty("status").GetString());
        Assert.AreEqual(
            DeterministicSmokeRewritingClient.FixtureResult,
            result.GetProperty("text").GetString());
    }

    [TestMethod]
    public async Task ProcessingFailureSentinelThrowsTransiently()
    {
        var client = DeterministicSmokeRewritingClient.Instance;
        await Assert.ThrowsExactlyAsync<HttpRequestException>(() => client.GetResponseAsync(
            [UserPrompt("""{"source":"M029-PROCESSING-FAILURE probe","sourceLanguage":"en","mode":"correctionOnly"}""")],
            cancellationToken: TestContext.CancellationToken));
    }

    [TestMethod]
    public void RegistrationKeepsUnavailableProviderOutsideSmoke()
    {
        var services = new ServiceCollection();
        services.AddScoped<IRewritingClientProvider, UnavailableRewritingClientProvider>();
        services.AddSmokeDeterministicRewritingProvider(new TestHostEnvironment("Production"));
        var registrations = services
            .Where(descriptor => descriptor.ServiceType == typeof(IRewritingClientProvider))
            .ToList();
        Assert.HasCount(1, registrations);
        Assert.AreEqual(typeof(UnavailableRewritingClientProvider), registrations[0].ImplementationType);
    }

    [TestMethod]
    public void RegistrationReplacesProviderOnlyInSmoke()
    {
        var services = new ServiceCollection();
        services.AddScoped<IRewritingClientProvider, UnavailableRewritingClientProvider>();
        services.AddSmokeDeterministicRewritingProvider(new TestHostEnvironment("Smoke"));
        var registrations = services
            .Where(descriptor => descriptor.ServiceType == typeof(IRewritingClientProvider))
            .ToList();
        Assert.HasCount(1, registrations);
        Assert.AreEqual(typeof(DeterministicSmokeRewritingProvider), registrations[0].ImplementationType);
    }

    public TestContext TestContext { get; set; } = null!;

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;

        public string ApplicationName { get; set; } = "smoke-test";

        public string ContentRootPath { get; set; } = ".";

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
