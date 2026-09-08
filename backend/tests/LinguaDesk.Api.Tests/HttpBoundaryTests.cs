using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;

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

    private static WebApplicationFactory<Program> CreateFactory() =>
        new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment("Testing"));
}
