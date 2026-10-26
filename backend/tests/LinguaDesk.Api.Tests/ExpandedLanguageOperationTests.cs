using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LinguaDesk.Api.Features.Operations;
using LinguaDesk.Api.Infrastructure.Smoke;
using LinguaDesk.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LinguaDesk.Api.Tests;

[TestClass]
public sealed class ExpandedLanguageOperationTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    [TestMethod]
    public async Task RealBearerExpandedScriptsAndThreeLetterCodesRetainOnceOnlyScalarChargingAndTextFreeRecovery()
    {
        await using var fixture = await OperationFixture.CreateAsync(new Dictionary<string, string?> { ["MonetaryAdmission:MonthlyCapMinorUnits"] = "1000000", ["MonetaryAdmission:Currency"] = "USD" }, services =>
        {
            services.RemoveAll<IRewritingClientProvider>(); services.AddSingleton<IRewritingClientProvider, DeterministicSmokeRewritingProvider>();
        });
        await fixture.CreateAccountAsync("expanded@example.test", true);
        var access = await fixture.SignInAsync("expanded@example.test");
        var charged = 0;
        foreach (var language in new[] { "ar", "hi", "fil" })
        {
            var sample = ExpandedSmokeFixtures.Cases.Single(c => c.Language == language);
            var id = OperationIdentity.CreateForTime(fixture.Time.GetUtcNow());
            var body = JsonSerializer.Serialize(new { operationId = id, family = "rewriting", source = sample.Source, sourceSelection = language }, Json);
            using var response = await fixture.PostRawAsync("/api/operations", body, access);
            Assert.AreEqual(HttpStatusCode.Created, response.StatusCode, await response.Content.ReadAsStringAsync());
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.AreEqual(sample.Result, result.GetProperty("rewrittenText").GetString());
            Assert.AreEqual(language, result.GetProperty("resolvedSourceLanguage").GetString());
            var count = ScalarInputPolicy.Analyze(sample.Source, 2000).ScalarCount!.Value;
            Assert.AreEqual(count, result.GetProperty("characterCount").GetInt32()); charged += count;
            using var replay = await fixture.PostRawAsync("/api/operations", body, access);
            Assert.AreEqual(HttpStatusCode.OK, replay.StatusCode);
            Assert.IsFalse((await replay.Content.ReadFromJsonAsync<JsonElement>()).TryGetProperty("rewrittenText", out _));
            using var changed = await fixture.PostRawAsync("/api/operations", JsonSerializer.Serialize(new { operationId = id, family = "rewriting", source = sample.Source + " ", sourceSelection = language }, Json), access);
            Assert.AreEqual(HttpStatusCode.Conflict, changed.StatusCode);
            var span = SentenceBoundaryPolicy.Segment(sample.Result, language)[0];
            var request = new AlternativesRequest(OperationIdentity.CreateForTime(fixture.Time.GetUtcNow()), sample.Result, span.Start, span.Length, Guid.NewGuid(), language);
            var alternativesBody = JsonSerializer.Serialize(request, Json);
            using var alternatives = await fixture.PostRawAsync("/api/rewriting/alternatives", alternativesBody, access);
            Assert.AreEqual(HttpStatusCode.Created, alternatives.StatusCode, await alternatives.Content.ReadAsStringAsync());
            var options = await alternatives.Content.ReadFromJsonAsync<JsonElement>();
            Assert.AreEqual(span.Length, options.GetProperty("characterCount").GetInt32()); charged += span.Length;
            Assert.AreEqual(language, options.GetProperty("resolvedSourceLanguage").GetString());
            using var retry = await fixture.PostRawAsync("/api/rewriting/alternatives", alternativesBody, access);
            Assert.AreEqual(HttpStatusCode.OK, retry.StatusCode);
            var metadata = await retry.Content.ReadFromJsonAsync<JsonElement>();
            Assert.IsFalse(metadata.TryGetProperty("options", out _)); Assert.IsFalse(metadata.TryGetProperty("context", out _));
            using var status = await fixture.GetAsync($"/api/operations/{request.OperationId}", access);
            Assert.IsFalse((await status.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("outputAvailable").GetBoolean());
        }
        foreach (var (source, target) in new[] { ("ar", "ar"), ("en-US", "ar"), ("tl", "ar"), ("en", "sr-Latn") })
        {
            using var rejected = await fixture.PostRawAsync("/api/operations", JsonSerializer.Serialize(new { operationId = OperationIdentity.CreateForTime(fixture.Time.GetUtcNow()), family = "translation", source = "Synthetic text.", sourceSelection = source, target }, Json), access);
            Assert.AreEqual(HttpStatusCode.UnprocessableEntity, rejected.StatusCode);
        }
        using var usage = await fixture.GetAsync("/api/usage", access);
        Assert.AreEqual(charged, (await usage.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("consumedCharacters").GetInt32());
    }
}
