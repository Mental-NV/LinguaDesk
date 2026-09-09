using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LinguaDesk.Api.Features.Capabilities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LinguaDesk.Api.Tests;

[TestClass]
public sealed class CapabilitiesEndpointTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 9, 7, 17, 40, 0, TimeSpan.Zero);
    private static readonly string[] ExpectedWhitespaceRanges =
    [
        "U+0009-U+000D", "U+0020", "U+0085", "U+00A0", "U+1680",
        "U+2000-U+200A", "U+2028-U+2029", "U+202F", "U+205F", "U+3000",
    ];
    private static readonly string[] ExpectedLanguages = ["en:English", "ru:Russian", "ro:Romanian", "zh:Chinese"];
    private static readonly string[] ExpectedSources = ["auto", "en", "ru", "ro", "zh"];
    private static readonly ChineseScript[] ExpectedChineseScripts = [ChineseScript.Simplified, ChineseScript.Traditional];
    private static readonly string[] ExpectedDirections =
    [
        "en:ru", "en:ro", "en:zh", "ru:en", "ru:ro", "ru:zh",
        "ro:en", "ro:ru", "ro:zh", "zh:en", "zh:ru", "zh:ro",
    ];
    private static readonly string[] ExpectedModes =
    [
        "correctionOnly:Correction only:correction", "simple:Simple:style", "casual:Casual:style",
        "business:Business:style", "academic:Academic:style", "enthusiastic:Enthusiastic:tone",
        "friendly:Friendly:tone", "confident:Confident:tone", "diplomatic:Diplomatic:tone",
    ];
    private static readonly string[] ExpectedTopLevelFields =
    [
        "serverTimeUtc", "languages", "sourceSelection", "chineseScriptPolicy",
        "countingPolicy", "translation", "rewriting", "operationIdentity",
    ];
    private static readonly string[] ExcludedResponseTerms =
        ["provider", "routing", "allowance", "spend", "secret", "account", "usage"];

    [TestMethod]
    public async Task AnonymousGetUsesFakeUtcTimeAndNoStoreWithoutOpeningStorage()
    {
        var root = Directory.CreateTempSubdirectory("linguadesk-capabilities-").FullName;
        var databasePath = Path.Combine(root, "unused.db");
        try
        {
            await using var factory = new CapabilityWebApplicationFactory(root, databasePath);
            using var client = factory.CreateClient();

            using var response = await client.GetAsync("/api/capabilities");
            var capability = await response.Content.ReadFromJsonAsync<CapabilitiesResponse>();

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual("application/json", response.Content.Headers.ContentType?.MediaType);
            Assert.AreEqual("utf-8", response.Content.Headers.ContentType?.CharSet);
            Assert.AreEqual("no-store", response.Headers.CacheControl?.ToString());
            Assert.IsFalse(response.Headers.Contains("WWW-Authenticate"));
            Assert.IsFalse(response.Headers.Contains("Set-Cookie"));
            Assert.IsNotNull(capability);
            Assert.AreEqual(FixedNow, capability.ServerTimeUtc);
            Assert.IsFalse(File.Exists(databasePath));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [TestMethod]
    public async Task CatalogContainsOnlyTheSelectedOrderedChoices()
    {
        await using var factory = new CapabilityWebApplicationFactory();
        using var client = factory.CreateClient();
        var capability = await client.GetFromJsonAsync<CapabilitiesResponse>("/api/capabilities");

        Assert.IsNotNull(capability);
        CollectionAssert.AreEqual(
            ExpectedLanguages,
            capability.Languages.Select(static language => $"{ToWire(language.Id)}:{language.Name}").ToArray());
        Assert.AreEqual(SourceSelectionValue.Automatic, capability.SourceSelection.Default);
        CollectionAssert.AreEqual(
            ExpectedSources,
            capability.SourceSelection.Values.Select(ToWire).ToArray());
        CollectionAssert.AreEqual(
            ExpectedChineseScripts,
            capability.ChineseScriptPolicy.AcceptedInput.ToArray());
        Assert.AreEqual(ChineseScript.Simplified, capability.ChineseScriptPolicy.Output);
        CollectionAssert.AreEqual(
            ExpectedDirections,
            capability.Translation.SupportedDirections
                .Select(static direction => $"{ToWire(direction.Source)}:{ToWire(direction.Target)}")
                .ToArray());
        Assert.HasCount(12, capability.Translation.SupportedDirections.Distinct());
        CollectionAssert.AreEqual(
            ExpectedModes,
            capability.Rewriting.Modes
                .Select(static mode => $"{ToWire(mode.Id)}:{mode.Name}:{ToWire(mode.Kind)}")
                .ToArray());
        Assert.AreEqual(RewritingModeId.CorrectionOnly, capability.Rewriting.DefaultMode);
    }

    [TestMethod]
    public async Task PolicyAndRecoveryMetadataMatchesTheSharedDesign()
    {
        await using var factory = new CapabilityWebApplicationFactory();
        using var client = factory.CreateClient();
        var capability = await client.GetFromJsonAsync<CapabilitiesResponse>("/api/capabilities");

        Assert.IsNotNull(capability);
        Assert.AreEqual("unicode-scalar-v1", capability.CountingPolicy.Id);
        Assert.AreEqual(CountingUnit.UnicodeScalar, capability.CountingPolicy.Unit);
        Assert.AreEqual(NormalizationPolicy.None, capability.CountingPolicy.Normalization);
        Assert.AreEqual(LineEndingPolicy.Preserve, capability.CountingPolicy.LineEndings);
        Assert.AreEqual(InvalidUnicodePolicy.Reject, capability.CountingPolicy.InvalidUnicode);
        Assert.AreEqual(EmptyOrWhitespacePolicy.Reject, capability.CountingPolicy.EmptyOrWhitespace);
        CollectionAssert.AreEqual(ExpectedWhitespaceRanges, capability.CountingPolicy.WhitespaceCodePointRanges.ToArray());
        Assert.AreEqual(5000, capability.Translation.MaximumSourceCharacters);
        Assert.AreEqual(2000, capability.Rewriting.MaximumSourceCharacters);
        Assert.AreEqual(OversizeHandling.RejectWhole, capability.Translation.OversizeHandling);
        Assert.AreEqual(OversizeHandling.RejectWhole, capability.Rewriting.OversizeHandling);
        Assert.AreEqual(30, capability.Translation.OverallDeadlineSeconds);
        Assert.AreEqual(30, capability.Rewriting.OverallDeadlineSeconds);
        Assert.IsTrue(capability.Translation.TargetRequired);
        Assert.AreEqual(OperationIdentityFormat.UuidV7, capability.OperationIdentity.Format);
        Assert.AreEqual(86400, capability.OperationIdentity.ValidForSeconds);
        Assert.AreEqual(300, capability.OperationIdentity.MaximumFutureSkewSeconds);
    }

    [TestMethod]
    public async Task JsonContainsEveryRequiredTopLevelMemberAndNoPrivateOrFutureData()
    {
        await using var factory = new CapabilityWebApplicationFactory();
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/capabilities");
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);

        CollectionAssert.AreEqual(
            ExpectedTopLevelFields,
            document.RootElement.EnumerateObject().Select(static property => property.Name).ToArray());
        var lowercaseBody = body.ToLowerInvariant();
        foreach (var excluded in ExcludedResponseTerms)
        {
            Assert.IsFalse(lowercaseBody.Contains(excluded, StringComparison.Ordinal), excluded);
        }
    }

    [TestMethod]
    public async Task RouteMethodAndApiFallbackBoundariesRemainExplicit()
    {
        await using var factory = new CapabilityWebApplicationFactory();
        using var client = factory.CreateClient();

        using var postResponse = await client.PostAsync("/api/capabilities", content: null);
        using var missingResponse = await client.GetAsync("/api/not-implemented");
        var problem = await missingResponse.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.AreEqual(HttpStatusCode.MethodNotAllowed, postResponse.StatusCode);
        Assert.AreEqual("GET", postResponse.Content.Headers.Allow.Single());
        Assert.AreEqual(HttpStatusCode.NotFound, missingResponse.StatusCode);
        Assert.AreEqual("application/problem+json", missingResponse.Content.Headers.ContentType?.MediaType);
        Assert.IsNotNull(problem);
        Assert.AreEqual(404, problem.Status);
    }

    private static string ToWire(LanguageId value) => value switch
    {
        LanguageId.English => "en",
        LanguageId.Russian => "ru",
        LanguageId.Romanian => "ro",
        LanguageId.Chinese => "zh",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
    };

    private static string ToWire(SourceSelectionValue value) => value switch
    {
        SourceSelectionValue.Automatic => "auto",
        SourceSelectionValue.English => "en",
        SourceSelectionValue.Russian => "ru",
        SourceSelectionValue.Romanian => "ro",
        SourceSelectionValue.Chinese => "zh",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
    };

    private static string ToWire(RewritingModeId value) => value switch
    {
        RewritingModeId.CorrectionOnly => "correctionOnly",
        RewritingModeId.Simple => "simple",
        RewritingModeId.Casual => "casual",
        RewritingModeId.Business => "business",
        RewritingModeId.Academic => "academic",
        RewritingModeId.Enthusiastic => "enthusiastic",
        RewritingModeId.Friendly => "friendly",
        RewritingModeId.Confident => "confident",
        RewritingModeId.Diplomatic => "diplomatic",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
    };

    private static string ToWire(RewritingModeKind value) => value switch
    {
        RewritingModeKind.Correction => "correction",
        RewritingModeKind.Style => "style",
        RewritingModeKind.Tone => "tone",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
    };

    private sealed class CapabilityWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string webRoot;
        private readonly string? databasePath;
        private readonly string keysPath = Directory.CreateTempSubdirectory("linguadesk-capability-keys-").FullName;

        public CapabilityWebApplicationFactory(string? webRoot = null, string? databasePath = null)
        {
            this.webRoot = webRoot ?? Directory.CreateTempSubdirectory("linguadesk-capability-webroot-").FullName;
            this.databasePath = databasePath;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseWebRoot(webRoot);
            builder.ConfigureServices(services => services.AddSingleton<TimeProvider>(new FixedTimeProvider(FixedNow)));
            builder.ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Storage:DatabasePath"] = databasePath,
                    ["Security:DataProtectionKeysPath"] = keysPath,
                }));
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing && Directory.Exists(webRoot))
            {
                Directory.Delete(webRoot, recursive: true);
            }
            if (disposing && Directory.Exists(keysPath))
            {
                Directory.Delete(keysPath, recursive: true);
            }
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => value;
    }
}
