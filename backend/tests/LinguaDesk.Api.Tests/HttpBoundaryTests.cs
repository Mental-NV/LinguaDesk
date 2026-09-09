using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace LinguaDesk.Api.Tests;

[TestClass]
public sealed class HttpBoundaryTests
{
    [TestMethod]
    public async Task LiveHealthProbeReportsProcessLivenessOnly()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/health/live");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("text/plain", response.Content.Headers.ContentType?.MediaType);
        Assert.AreEqual("Healthy", await response.Content.ReadAsStringAsync());
        Assert.IsFalse(response.Headers.Contains("Set-Cookie"));
    }

    [TestMethod]
    public async Task LiveHealthProbeRejectsPost()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var response = await client.PostAsync("/health/live", content: null);

        Assert.AreEqual(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [TestMethod]
    [DataRow("/api")]
    [DataRow("/api/not-implemented")]
    [DataRow("/api/nested/not-implemented")]
    public async Task MissingApiRoutesReturnProblemDetails(string path)
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(path);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        Assert.AreEqual("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.IsNotNull(problem);
        Assert.AreEqual(StatusCodes.Status404NotFound, problem.Status);
        Assert.AreEqual("Not Found", problem.Title);
    }

    [TestMethod]
    [DataRow("/")]
    [DataRow("/weatherforecast")]
    [DataRow("/sample")]
    public async Task MissingNonApiRoutesRemainOrdinaryNotFound(string path)
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(path);

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        Assert.AreNotEqual("text/html", response.Content.Headers.ContentType?.MediaType);
    }

    [TestMethod]
    [DataRow("/")]
    [DataRow("/login")]
    [DataRow("/register")]
    [DataRow("/unknown-client-page")]
    public async Task EligibleNavigationUsesPopulatedSpaDocument(string path)
    {
        await using var factory = CreateFactory(PopulateWebRoot);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(path);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("text/html", response.Content.Headers.ContentType?.MediaType);
        StringAssert.Contains(await response.Content.ReadAsStringAsync(), "published-shell-fixture");
    }

    [TestMethod]
    public async Task EligibleHeadNavigationUsesSpaDocumentWithoutBody()
    {
        await using var factory = CreateFactory(PopulateWebRoot);
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Head, "/register");
        using var response = await client.SendAsync(request);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.IsEmpty(await response.Content.ReadAsByteArrayAsync());
    }

    [TestMethod]
    [DataRow("/assets/missing")]
    [DataRow("/assets/missing.js")]
    [DataRow("/missing.js")]
    [DataRow("/health/missing")]
    public async Task ReservedAndFileLikePathsNeverUseSpaDocument(string path)
    {
        await using var factory = CreateFactory(PopulateWebRoot);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(path);

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        Assert.AreNotEqual("text/html", response.Content.Headers.ContentType?.MediaType);
    }

    [TestMethod]
    public async Task NonNavigationMethodNeverUsesSpaDocument()
    {
        await using var factory = CreateFactory(PopulateWebRoot);
        using var client = factory.CreateClient();

        using var response = await client.PostAsync("/register", content: null);

        Assert.AreEqual(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        Assert.AreNotEqual("text/html", response.Content.Headers.ContentType?.MediaType);
    }

    [TestMethod]
    public async Task ExistingStaticAssetUsesItsOwnContentType()
    {
        await using var factory = CreateFactory(PopulateWebRoot);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/assets/app.js");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("text/javascript", response.Content.Headers.ContentType?.MediaType);
        Assert.AreEqual("console.log('fixture');", await response.Content.ReadAsStringAsync());
    }

    [TestMethod]
    public async Task IndependentlyOwnedFactoriesCanRunConcurrently()
    {
        await using var firstFactory = CreateFactory();
        await using var secondFactory = CreateFactory();
        using var firstClient = firstFactory.CreateClient();
        using var secondClient = secondFactory.CreateClient();

        var firstRequest = firstClient.GetAsync("/health/live");
        var secondRequest = secondClient.GetAsync("/api/missing");
        await Task.WhenAll(firstRequest, secondRequest);

        using var firstResponse = await firstRequest;
        using var secondResponse = await secondRequest;
        Assert.AreEqual(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.AreEqual(HttpStatusCode.NotFound, secondResponse.StatusCode);
    }

    private static OwnedWebApplicationFactory CreateFactory(Action<string>? populateWebRoot = null) =>
        new(populateWebRoot);

    private static void PopulateWebRoot(string webRoot)
    {
        File.WriteAllText(
            Path.Combine(webRoot, "index.html"),
            "<!doctype html><html><body>published-shell-fixture</body></html>");
        var assets = Directory.CreateDirectory(Path.Combine(webRoot, "assets"));
        File.WriteAllText(Path.Combine(assets.FullName, "app.js"), "console.log('fixture');");
    }

    private sealed class OwnedWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string webRoot = Directory.CreateTempSubdirectory("linguadesk-webroot-").FullName;
        private readonly string keysPath = Directory.CreateTempSubdirectory("linguadesk-http-keys-").FullName;

        public OwnedWebApplicationFactory(Action<string>? populateWebRoot)
        {
            populateWebRoot?.Invoke(webRoot);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseWebRoot(webRoot);
            builder.ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
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
}
