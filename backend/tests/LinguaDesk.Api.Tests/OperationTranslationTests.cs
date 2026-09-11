using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using LinguaDesk.Api.Features.Operations;
using LinguaDesk.Infrastructure.Ai;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LinguaDesk.Api.Tests;

[TestClass]
public sealed class OperationTranslationTests
{
    private static Dictionary<string, string?> MonetaryConfig(
        long cap,
        string currency = "USD",
        IReadOnlyDictionary<string, string?>? extra = null)
    {
        var config = new Dictionary<string, string?>
        {
            ["MonetaryAdmission:MonthlyCapMinorUnits"] = cap.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["MonetaryAdmission:Currency"] = currency,
        };
        if (extra is not null)
        {
            foreach (var setting in extra)
            {
                config[setting.Key] = setting.Value;
            }
        }

        return config;
    }

    private static async Task<(OperationFixture Fixture, FakeTranslationProvider Fake)> CreateTranslationFixtureAsync(
        FakeTranslationClient primary,
        FakeTranslationClient fallback,
        IReadOnlyDictionary<string, string?>? overrides = null)
    {
        var fake = new FakeTranslationProvider(primary, fallback);
        var fixture = await OperationFixture.CreateAsync(
            MonetaryConfig(1_000_000, extra: overrides),
            services =>
            {
                services.RemoveAll<ITranslationClientProvider>();
                services.AddSingleton<ITranslationClientProvider>(fake);
            });
        return (fixture, fake);
    }

    private static async Task<string> CreateVerifiedAccountAsync(OperationFixture fixture, string email)
    {
        await fixture.CreateAccountAsync(email, confirmed: true);
        return await fixture.SignInAsync(email);
    }

    private static string SubmitBody(string operationId, string source, string? target = "ru", string? sourceSelection = null)
    {
        var payload = new Dictionary<string, object?>
        {
            ["operationId"] = operationId,
            ["family"] = "translation",
            ["source"] = source,
        };
        if (target is not null)
        {
            payload["target"] = target;
        }

        if (sourceSelection is not null)
        {
            payload["sourceSelection"] = sourceSelection;
        }

        return JsonSerializer.Serialize(payload);
    }

    private static void EnqueueSuccess(FakeTranslationClient client, string language, string text) =>
        EnqueuePair(client, $"{{\"status\":\"eligible\",\"language\":\"{language}\"}}", $"{{\"status\":\"result\",\"text\":\"{text}\"}}");

    private static void EnqueuePair(FakeTranslationClient client, string eligibilityJson, string transformationJson)
    {
        client.EnqueueResponse(eligibilityJson);
        client.EnqueueResponse(transformationJson);
    }

    [TestMethod]
    public async Task TranslationSubmitExecutesSynchronouslyWithChargeAndSnapshot()
    {
        using var primary = new FakeTranslationClient();
        using var fallback = new FakeTranslationClient();
        EnqueueSuccess(primary, "en", "RU-RESULT-TEXT");
        var (fixture, _) = await CreateTranslationFixtureAsync(primary, fallback);
        await using (fixture)
        {
            var access = await CreateVerifiedAccountAsync(fixture, "translate@example.test");
            const string source = "Please translate this report.";
            var operationId = OperationIdentity.CreateForTime(fixture.Time.GetUtcNow()).ToString("D");

            using var response = await fixture.PostRawAsync("/api/operations", SubmitBody(operationId, source), access);

            Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
            Assert.AreEqual("no-store", response.Headers.CacheControl?.ToString());
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.AreEqual(operationId, body.GetProperty("operationId").GetString());
            Assert.AreEqual("translation", body.GetProperty("family").GetString());
            Assert.AreEqual("succeeded", body.GetProperty("status").GetString());
            Assert.AreEqual("RU-RESULT-TEXT", body.GetProperty("translatedText").GetString());
            Assert.AreEqual(source.Length, body.GetProperty("characterCount").GetInt32());
            Assert.AreEqual("2026-09-11", body.GetProperty("admissionDay").GetString());
            var serverTime = body.GetProperty("serverTimeUtc").GetDateTimeOffset();
            Assert.AreEqual(TimeSpan.FromSeconds(30), body.GetProperty("deadlineUtc").GetDateTimeOffset() - serverTime);
            var usage = body.GetProperty("usage");
            Assert.AreEqual("2026-09-11", usage.GetProperty("day").GetString());
            Assert.AreEqual(source.Length, usage.GetProperty("consumedCharacters").GetInt32());
            Assert.AreEqual(0, usage.GetProperty("reservedCharacters").GetInt32());
            Assert.AreEqual(20000 - source.Length, usage.GetProperty("availableCharacters").GetInt32());
            Assert.AreEqual("available", usage.GetProperty("availability").GetString());
            Assert.AreEqual(2, primary.CallCount);
            Assert.AreEqual(0, fallback.CallCount);

            await using var verification = fixture.Database.CreateContext();
            var stored = await verification.OperationSubmissions.SingleAsync();
            Assert.AreEqual("succeeded", stored.State);
            Assert.AreEqual(source.Length, stored.ScalarCount);
            Assert.AreEqual("2026-09-11", stored.AdmissionDay);
            var reservations = await verification.MonetaryAttemptReservations.ToListAsync();
            Assert.HasCount(2, reservations);
            Assert.IsTrue(reservations.All(item => string.Equals(item.State, MonetaryReservationStates.Released, StringComparison.Ordinal)));
            Assert.AreEqual(0, await verification.MonetaryCostLedgers.SumAsync(entry => entry.UnresolvedExposureMinorUnits));
            Assert.AreEqual(0, await verification.MonetaryCostLedgers.SumAsync(entry => entry.KnownSpendMinorUnits));

            using var status = await fixture.GetAsync($"/api/operations/{operationId}", access);
            Assert.AreEqual(HttpStatusCode.OK, status.StatusCode);
            var statusBody = await status.Content.ReadFromJsonAsync<JsonElement>();
            Assert.AreEqual("succeeded", statusBody.GetProperty("status").GetString());
            Assert.IsFalse(statusBody.GetProperty("outputAvailable").GetBoolean());
            Assert.AreEqual(source.Length, statusBody.GetProperty("characterCount").GetInt32());
            Assert.AreEqual(2, primary.CallCount);

            using var usageRead = await fixture.GetAsync("/api/usage", access);
            Assert.AreEqual(HttpStatusCode.OK, usageRead.StatusCode);
            var usageBody = await usageRead.Content.ReadFromJsonAsync<JsonElement>();
            Assert.AreEqual(source.Length, usageBody.GetProperty("consumedCharacters").GetInt32());
            Assert.AreEqual(2, primary.CallCount);
        }
    }

    [TestMethod]
    public async Task TranslationWithoutConfiguredProviderRemainsPending()
    {
        await using var fixture = await OperationFixture.CreateAsync();
        await fixture.CreateAccountAsync("pending@example.test", confirmed: true);
        var access = await fixture.SignInAsync("pending@example.test");
        var operationId = OperationIdentity.CreateForTime(fixture.Time.GetUtcNow()).ToString("D");

        using var response = await fixture.PostRawAsync(
            "/api/operations",
            SubmitBody(operationId, "Reservation probe text"),
            access);

        Assert.AreEqual(HttpStatusCode.Accepted, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.AreEqual("pending", body.GetProperty("status").GetString());
    }

    [TestMethod]
    public async Task TranslationValidationMatrixRejectsWithoutDispatchOrCharge()
    {
        using var primary = new FakeTranslationClient();
        using var fallback = new FakeTranslationClient();
        var (fixture, _) = await CreateTranslationFixtureAsync(primary, fallback);
        await using (fixture)
        {
            var access = await CreateVerifiedAccountAsync(fixture, "validation@example.test");
            var oversized = new string('a', 5001);
            var cases = new (string Name, string Body, HttpStatusCode Status, string Category)[]
            {
                ("malformed", "{not-json", HttpStatusCode.BadRequest, "invalidRequest"),
                ("unknown-field", $"{{\"operationId\":\"{OperationIdentity.CreateForTime(fixture.Time.GetUtcNow()):D}\",\"family\":\"translation\",\"source\":\"Hello.\",\"targte\":\"ru\"}}", HttpStatusCode.BadRequest, "invalidRequest"),
                ("missing-target", $"{{\"operationId\":\"{OperationIdentity.CreateForTime(fixture.Time.GetUtcNow()):D}\",\"family\":\"translation\",\"source\":\"Hello.\"}}", HttpStatusCode.UnprocessableEntity, "inputEligibility"),
                ("unsupported-target", SubmitBody(OperationIdentity.CreateForTime(fixture.Time.GetUtcNow()).ToString("D"), "Hello.", target: "xx"), HttpStatusCode.UnprocessableEntity, "inputEligibility"),
                ("equal-selection", SubmitBody(OperationIdentity.CreateForTime(fixture.Time.GetUtcNow()).ToString("D"), "Hello.", target: "en", sourceSelection: "en"), HttpStatusCode.UnprocessableEntity, "inputEligibility"),
                ("empty", SubmitBody(OperationIdentity.CreateForTime(fixture.Time.GetUtcNow()).ToString("D"), string.Empty), HttpStatusCode.UnprocessableEntity, "inputEligibility"),
                ("whitespace", SubmitBody(OperationIdentity.CreateForTime(fixture.Time.GetUtcNow()).ToString("D"), "  \t\n "), HttpStatusCode.UnprocessableEntity, "inputEligibility"),
                ("oversized", SubmitBody(OperationIdentity.CreateForTime(fixture.Time.GetUtcNow()).ToString("D"), oversized), HttpStatusCode.UnprocessableEntity, "inputEligibility"),
            };

            foreach (var (name, body, status, category) in cases)
            {
                using var response = await fixture.PostRawAsync("/api/operations", body, access);
                Assert.AreEqual(status, response.StatusCode, name);
                Assert.AreEqual("no-store", response.Headers.CacheControl?.ToString(), name);
                var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
                Assert.AreEqual(category, problem.GetProperty("category").GetString(), name);
            }

            var duplicateKey = $"{{\"operationId\":\"{OperationIdentity.CreateForTime(fixture.Time.GetUtcNow()):D}\",\"operationId\":\"{OperationIdentity.CreateForTime(fixture.Time.GetUtcNow()):D}\",\"family\":\"translation\",\"source\":\"Hello.\",\"target\":\"ru\"}}";
            using var duplicate = await fixture.PostRawAsync("/api/operations", duplicateKey, access);
            Assert.AreEqual(HttpStatusCode.BadRequest, duplicate.StatusCode);

            Assert.AreEqual(0, primary.CallCount);
            Assert.AreEqual(0, fallback.CallCount);

            await using var verification = fixture.Database.CreateContext();
            Assert.AreEqual(0, await verification.OperationSubmissions.CountAsync());
            using var usage = await fixture.GetAsync("/api/usage", access);
            var usageBody = await usage.Content.ReadFromJsonAsync<JsonElement>();
            Assert.AreEqual(0, usageBody.GetProperty("consumedCharacters").GetInt32());
            Assert.AreEqual(0, usageBody.GetProperty("reservedCharacters").GetInt32());
        }
    }

    [TestMethod]
    public async Task EligibilityRejectionsEndWithoutTransformationOrCharge()
    {
        var rejections = new (string Name, string Classification, string? SourceSelection, string Reason)[]
        {
            ("uncertain", "{\"status\":\"uncertain\",\"language\":null}", null, "uncertain"),
            ("unsupported", "{\"status\":\"unsupported\",\"language\":null}", null, "unsupported"),
            ("mixed", "{\"status\":\"mixed\",\"language\":null}", null, "mixed"),
            ("mismatch", "{\"status\":\"source_mismatch\",\"language\":\"ru\"}", "en", "sourceMismatch"),
        };

        foreach (var (name, classification, hint, reason) in rejections)
        {
            using var primary = new FakeTranslationClient();
            using var fallback = new FakeTranslationClient();
            primary.EnqueueResponse(classification);
            var (fixture, _) = await CreateTranslationFixtureAsync(primary, fallback);
            await using (fixture)
            {
                var access = await CreateVerifiedAccountAsync(fixture, $"{name}@example.test");
                var operationId = OperationIdentity.CreateForTime(fixture.Time.GetUtcNow()).ToString("D");

                using var response = await fixture.PostRawAsync(
                    "/api/operations",
                    SubmitBody(operationId, "Bonjour le monde.", sourceSelection: hint),
                    access);

                Assert.AreEqual(HttpStatusCode.UnprocessableEntity, response.StatusCode, name);
                var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
                Assert.AreEqual("inputEligibility", problem.GetProperty("category").GetString(), name);
                Assert.AreEqual(reason, problem.GetProperty("reason").GetString(), name);
                Assert.AreEqual(1, primary.CallCount, name);
                Assert.AreEqual(0, fallback.CallCount, name);

                using var status = await fixture.GetAsync($"/api/operations/{operationId}", access);
                Assert.AreEqual(HttpStatusCode.OK, status.StatusCode, name);
                var statusBody = await status.Content.ReadFromJsonAsync<JsonElement>();
                Assert.AreEqual("failed", statusBody.GetProperty("status").GetString(), name);
                Assert.AreEqual(0, statusBody.GetProperty("characterCount").GetInt32(), name);
                Assert.AreEqual(1, primary.CallCount, name);

                using var usage = await fixture.GetAsync("/api/usage", access);
                var usageBody = await usage.Content.ReadFromJsonAsync<JsonElement>();
                Assert.AreEqual(0, usageBody.GetProperty("consumedCharacters").GetInt32(), name);
                Assert.AreEqual(0, usageBody.GetProperty("reservedCharacters").GetInt32(), name);
            }
        }
    }

    [TestMethod]
    public async Task DetectedSameLanguageRejectsWithoutCharacterCharge()
    {
        using var primary = new FakeTranslationClient();
        using var fallback = new FakeTranslationClient();
        primary.EnqueueResponse("{\"status\":\"eligible\",\"language\":\"en\"}");
        var (fixture, _) = await CreateTranslationFixtureAsync(primary, fallback);
        await using (fixture)
        {
            var access = await CreateVerifiedAccountAsync(fixture, "samelanguage@example.test");
            var operationId = OperationIdentity.CreateForTime(fixture.Time.GetUtcNow()).ToString("D");

            using var response = await fixture.PostRawAsync(
                "/api/operations",
                SubmitBody(operationId, "Hello, world.", target: "en"),
                access);

            Assert.AreEqual(HttpStatusCode.UnprocessableEntity, response.StatusCode);
            var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.AreEqual("inputEligibility", problem.GetProperty("category").GetString());
            Assert.AreEqual("sameLanguage", problem.GetProperty("reason").GetString());
            Assert.AreEqual(1, primary.CallCount);
            Assert.AreEqual(0, fallback.CallCount);

            using var usage = await fixture.GetAsync("/api/usage", access);
            var usageBody = await usage.Content.ReadFromJsonAsync<JsonElement>();
            Assert.AreEqual(0, usageBody.GetProperty("consumedCharacters").GetInt32());
            Assert.AreEqual(0, usageBody.GetProperty("reservedCharacters").GetInt32());
        }
    }

    [TestMethod]
    public async Task DuplicateSamePayloadReplaysMetadataWithoutNewChargeOrDispatch()
    {
        using var primary = new FakeTranslationClient();
        using var fallback = new FakeTranslationClient();
        EnqueueSuccess(primary, "en", "RU-RESULT-TEXT");
        var (fixture, _) = await CreateTranslationFixtureAsync(primary, fallback);
        await using (fixture)
        {
            var access = await CreateVerifiedAccountAsync(fixture, "duplicate@example.test");
            const string source = "Charge exactly once.";
            var operationId = OperationIdentity.CreateForTime(fixture.Time.GetUtcNow()).ToString("D");
            var body = SubmitBody(operationId, source);

            using var first = await fixture.PostRawAsync("/api/operations", body, access);
            Assert.AreEqual(HttpStatusCode.Created, first.StatusCode);
            Assert.AreEqual(2, primary.CallCount);

            using var replay = await fixture.PostRawAsync("/api/operations", body, access);
            Assert.AreEqual(HttpStatusCode.OK, replay.StatusCode);
            var replayBody = await replay.Content.ReadFromJsonAsync<JsonElement>();
            Assert.AreEqual("succeeded", replayBody.GetProperty("status").GetString());
            Assert.IsFalse(replayBody.GetProperty("outputAvailable").GetBoolean());
            Assert.AreEqual(source.Length, replayBody.GetProperty("characterCount").GetInt32());
            Assert.AreEqual("2026-09-11", replayBody.GetProperty("admissionDay").GetString());
            Assert.AreEqual(2, primary.CallCount);
            Assert.AreEqual(0, fallback.CallCount);

            using var conflict = await fixture.PostRawAsync(
                "/api/operations",
                SubmitBody(operationId, "A different payload."),
                access);
            Assert.AreEqual(HttpStatusCode.Conflict, conflict.StatusCode);
            var conflictBody = await conflict.Content.ReadFromJsonAsync<JsonElement>();
            Assert.AreEqual("identityConflict", conflictBody.GetProperty("category").GetString());
            Assert.AreEqual(2, primary.CallCount);

            await using var verification = fixture.Database.CreateContext();
            Assert.AreEqual(1, await verification.OperationSubmissions.CountAsync());
            using var usage = await fixture.GetAsync("/api/usage", access);
            var usageBody = await usage.Content.ReadFromJsonAsync<JsonElement>();
            Assert.AreEqual(source.Length, usageBody.GetProperty("consumedCharacters").GetInt32());
            Assert.AreEqual(0, usageBody.GetProperty("reservedCharacters").GetInt32());
        }
    }

    [TestMethod]
    public async Task TransientProviderFailuresExhaustToProcessingFailure()
    {
        using var primary = new FakeTranslationClient();
        using var fallback = new FakeTranslationClient();
        primary.EnqueueThrow(new HttpRequestException("synthetic provider failure"));
        fallback.EnqueueThrow(new HttpRequestException("synthetic provider failure"));
        var (fixture, _) = await CreateTranslationFixtureAsync(primary, fallback);
        await using (fixture)
        {
            var access = await CreateVerifiedAccountAsync(fixture, "transient@example.test");
            var operationId = OperationIdentity.CreateForTime(fixture.Time.GetUtcNow()).ToString("D");

            using var response = await fixture.PostRawAsync(
                "/api/operations",
                SubmitBody(operationId, "Please translate this report."),
                access);

            Assert.AreEqual(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            Assert.AreEqual("no-store", response.Headers.CacheControl?.ToString());
            var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.AreEqual("processingFailure", problem.GetProperty("category").GetString());
            Assert.AreEqual(1, primary.CallCount);
            Assert.AreEqual(1, fallback.CallCount);

            using var status = await fixture.GetAsync($"/api/operations/{operationId}", access);
            var statusBody = await status.Content.ReadFromJsonAsync<JsonElement>();
            Assert.AreEqual("failed", statusBody.GetProperty("status").GetString());
            Assert.AreEqual(0, statusBody.GetProperty("characterCount").GetInt32());

            using var usage = await fixture.GetAsync("/api/usage", access);
            var usageBody = await usage.Content.ReadFromJsonAsync<JsonElement>();
            Assert.AreEqual(0, usageBody.GetProperty("consumedCharacters").GetInt32());
            Assert.AreEqual(0, usageBody.GetProperty("reservedCharacters").GetInt32());
        }
    }

    [TestMethod]
    public async Task InvalidProviderOutputExhaustsToProcessingFailure()
    {
        using var primary = new FakeTranslationClient();
        using var fallback = new FakeTranslationClient();
        EnqueuePair(primary, "{\"status\":\"eligible\",\"language\":\"en\"}", "{\"status\":\"eligible\"}");
        fallback.EnqueueResponse("{\"status\":\"eligible\"}");
        var (fixture, _) = await CreateTranslationFixtureAsync(primary, fallback);
        await using (fixture)
        {
            var access = await CreateVerifiedAccountAsync(fixture, "invalidoutput@example.test");
            var operationId = OperationIdentity.CreateForTime(fixture.Time.GetUtcNow()).ToString("D");

            using var response = await fixture.PostRawAsync(
                "/api/operations",
                SubmitBody(operationId, "Please translate this report."),
                access);

            Assert.AreEqual(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.AreEqual("processingFailure", problem.GetProperty("category").GetString());
            Assert.AreEqual(2, primary.CallCount);
            Assert.AreEqual(1, fallback.CallCount);

            using var usage = await fixture.GetAsync("/api/usage", access);
            var usageBody = await usage.Content.ReadFromJsonAsync<JsonElement>();
            Assert.AreEqual(0, usageBody.GetProperty("consumedCharacters").GetInt32());
            Assert.AreEqual(0, usageBody.GetProperty("reservedCharacters").GetInt32());
        }
    }

    [TestMethod]
    public async Task ProviderRefusalsExhaustToProcessingFailure()
    {
        using var primary = new FakeTranslationClient();
        using var fallback = new FakeTranslationClient();
        EnqueuePair(primary, "{\"status\":\"eligible\",\"language\":\"en\"}", "{\"status\":\"refused\",\"text\":null}");
        fallback.EnqueueResponse("{\"status\":\"refused\",\"text\":null}");
        var (fixture, _) = await CreateTranslationFixtureAsync(primary, fallback);
        await using (fixture)
        {
            var access = await CreateVerifiedAccountAsync(fixture, "refusal@example.test");
            var operationId = OperationIdentity.CreateForTime(fixture.Time.GetUtcNow()).ToString("D");

            using var response = await fixture.PostRawAsync(
                "/api/operations",
                SubmitBody(operationId, "Please translate this report."),
                access);

            Assert.AreEqual(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.AreEqual("processingFailure", problem.GetProperty("category").GetString());

            using var usage = await fixture.GetAsync("/api/usage", access);
            var usageBody = await usage.Content.ReadFromJsonAsync<JsonElement>();
            Assert.AreEqual(0, usageBody.GetProperty("consumedCharacters").GetInt32());
            Assert.AreEqual(0, usageBody.GetProperty("reservedCharacters").GetInt32());
        }
    }

    [TestMethod]
    public async Task FallbackReceivesOriginalSourceAndSettings()
    {
        using var primary = new FakeTranslationClient();
        using var fallback = new FakeTranslationClient();
        primary.EnqueueResponse("{\"status\":\"eligible\",\"language\":\"en\"}");
        primary.EnqueueThrow(new HttpRequestException("synthetic primary transformation failure"));
        fallback.EnqueueResponse("{\"status\":\"result\",\"text\":\"FALLBACK-TEXT\"}");
        var (fixture, _) = await CreateTranslationFixtureAsync(primary, fallback);
        await using (fixture)
        {
            var access = await CreateVerifiedAccountAsync(fixture, "fallback@example.test");
            const string source = "Please send the report tomorrow.";
            var operationId = OperationIdentity.CreateForTime(fixture.Time.GetUtcNow()).ToString("D");

            using var response = await fixture.PostRawAsync(
                "/api/operations",
                SubmitBody(operationId, source),
                access);

            Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.AreEqual("FALLBACK-TEXT", body.GetProperty("translatedText").GetString());
            Assert.AreEqual(2, primary.CallCount);
            Assert.AreEqual(1, fallback.CallCount);
            using var request = JsonDocument.Parse(fallback.UserTexts[^1]);
            Assert.AreEqual(source, request.RootElement.GetProperty("source").GetString());
            Assert.AreEqual("en", request.RootElement.GetProperty("sourceLanguage").GetString());
            Assert.AreEqual("ru", request.RootElement.GetProperty("targetLanguage").GetString());
        }
    }

    [TestMethod]
    public async Task DeadlineExpiryFencesExecutionWithZeroCharge()
    {
        using var primary = new FakeTranslationClient();
        using var fallback = new FakeTranslationClient();
        OperationFixture? captured = null;
        primary.EnqueueBehavior(token =>
        {
            captured!.Time.Advance(TimeSpan.FromSeconds(60));
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "{\"status\":\"eligible\",\"language\":\"en\"}")));
        });
        primary.EnqueueResponse("{\"status\":\"result\",\"text\":\"LATE-TEXT\"}");
        var fake = new FakeTranslationProvider(primary, fallback);
        var fixture = await OperationFixture.CreateAsync(
            MonetaryConfig(1_000_000),
            services =>
            {
                services.RemoveAll<ITranslationClientProvider>();
                services.AddSingleton<ITranslationClientProvider>(fake);
            });
        captured = fixture;
        await using (fixture)
        {
            var access = await CreateVerifiedAccountAsync(fixture, "deadline@example.test");
            var operationId = OperationIdentity.CreateForTime(fixture.Time.GetUtcNow()).ToString("D");

            using var response = await fixture.PostRawAsync(
                "/api/operations",
                SubmitBody(operationId, "Please translate this report."),
                access);

            Assert.AreEqual(HttpStatusCode.GatewayTimeout, response.StatusCode);
            Assert.AreEqual("no-store", response.Headers.CacheControl?.ToString());
            var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.AreEqual("deadlineExceeded", problem.GetProperty("category").GetString());
            Assert.AreEqual(1, primary.CallCount);
            Assert.AreEqual(0, fallback.CallCount);

            using var status = await fixture.GetAsync($"/api/operations/{operationId}", access);
            var statusBody = await status.Content.ReadFromJsonAsync<JsonElement>();
            Assert.AreEqual("failed", statusBody.GetProperty("status").GetString());
            Assert.AreEqual(0, statusBody.GetProperty("characterCount").GetInt32());

            using var usage = await fixture.GetAsync("/api/usage", access);
            var usageBody = await usage.Content.ReadFromJsonAsync<JsonElement>();
            Assert.AreEqual(0, usageBody.GetProperty("consumedCharacters").GetInt32());
            Assert.AreEqual(0, usageBody.GetProperty("reservedCharacters").GetInt32());
        }
    }

    [TestMethod]
    public async Task DeniedMonetaryAdmissionStopsDispatchWithSuspension()
    {
        using var primary = new FakeTranslationClient();
        using var fallback = new FakeTranslationClient();
        EnqueueSuccess(primary, "en", "RU-RESULT-TEXT");
        var fake = new FakeTranslationProvider(primary, fallback);
        await using var fixture = await OperationFixture.CreateAsync(
            MonetaryConfig(1),
            services =>
            {
                services.RemoveAll<ITranslationClientProvider>();
                services.AddSingleton<ITranslationClientProvider>(fake);
            });
        await fixture.CreateAccountAsync("suspended@example.test", confirmed: true);
        var access = await fixture.SignInAsync("suspended@example.test");
        var operationId = OperationIdentity.CreateForTime(fixture.Time.GetUtcNow()).ToString("D");

        using var response = await fixture.PostRawAsync(
            "/api/operations",
            SubmitBody(operationId, "Please translate this report."),
            access);

        Assert.AreEqual(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.AreEqual("monetarySuspension", problem.GetProperty("category").GetString());
        Assert.AreEqual(0, primary.CallCount);
        Assert.AreEqual(0, fallback.CallCount);

        await using var verification = fixture.Database.CreateContext();
        Assert.AreEqual(0, await verification.MonetaryAttemptReservations.CountAsync());
        using var usage = await fixture.GetAsync("/api/usage", access);
        var usageBody = await usage.Content.ReadFromJsonAsync<JsonElement>();
        Assert.AreEqual(0, usageBody.GetProperty("consumedCharacters").GetInt32());
        Assert.AreEqual(0, usageBody.GetProperty("reservedCharacters").GetInt32());
        Assert.AreEqual("available", usageBody.GetProperty("availability").GetString());
    }

    [TestMethod]
    public async Task MissingMonetaryConfigurationSuspendsDispatch()
    {
        using var primary = new FakeTranslationClient();
        using var fallback = new FakeTranslationClient();
        EnqueueSuccess(primary, "en", "RU-RESULT-TEXT");
        var fake = new FakeTranslationProvider(primary, fallback);
        await using var fixture = await OperationFixture.CreateAsync(
            null,
            services =>
            {
                services.RemoveAll<ITranslationClientProvider>();
                services.AddSingleton<ITranslationClientProvider>(fake);
            });
        await fixture.CreateAccountAsync("unconfigured@example.test", confirmed: true);
        var access = await fixture.SignInAsync("unconfigured@example.test");

        using var response = await fixture.PostRawAsync(
            "/api/operations",
            SubmitBody(OperationIdentity.CreateForTime(fixture.Time.GetUtcNow()).ToString("D"), "Please translate this report."),
            access);

        Assert.AreEqual(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.AreEqual("monetarySuspension", problem.GetProperty("category").GetString());
        Assert.AreEqual(0, primary.CallCount);
    }

    [TestMethod]
    public async Task ExhaustedUserAllowanceDeniesWithoutDispatch()
    {
        using var primary = new FakeTranslationClient();
        using var fallback = new FakeTranslationClient();
        EnqueueSuccess(primary, "en", "FIRST-RESULT");
        EnqueueSuccess(primary, "en", "SECOND-RESULT");
        var (fixture, _) = await CreateTranslationFixtureAsync(
            primary,
            fallback,
            new Dictionary<string, string?>
            {
                ["Operations:UserDailyAllowanceCharacters"] = "60",
                ["Operations:GlobalDailyAllowanceCharacters"] = "1000000",
            });
        await using (fixture)
        {
            var access = await CreateVerifiedAccountAsync(fixture, "allowance@example.test");
            var firstSource = new string('b', 60);
            using var first = await fixture.PostRawAsync(
                "/api/operations",
                SubmitBody(OperationIdentity.CreateForTime(fixture.Time.GetUtcNow()).ToString("D"), firstSource),
                access);
            Assert.AreEqual(HttpStatusCode.Created, first.StatusCode);
            Assert.AreEqual(2, primary.CallCount);

            using var denied = await fixture.PostRawAsync(
                "/api/operations",
                SubmitBody(OperationIdentity.CreateForTime(fixture.Time.GetUtcNow()).ToString("D"), new string('c', 60)),
                access);
            Assert.AreEqual((HttpStatusCode)429, denied.StatusCode);
            var problem = await denied.Content.ReadFromJsonAsync<JsonElement>();
            Assert.AreEqual("userAllowance", problem.GetProperty("category").GetString());
            Assert.AreNotEqual(DateTimeOffset.MinValue, problem.GetProperty("resetAtUtc").GetDateTimeOffset());
            Assert.AreEqual(2, primary.CallCount);
            Assert.AreEqual(0, fallback.CallCount);

            using var usage = await fixture.GetAsync("/api/usage", access);
            var usageBody = await usage.Content.ReadFromJsonAsync<JsonElement>();
            Assert.AreEqual(60, usageBody.GetProperty("consumedCharacters").GetInt32());
            Assert.AreEqual("user-exhausted", usageBody.GetProperty("availability").GetString());
        }
    }

    [TestMethod]
    public async Task SettledTranslationSurvivesRestartWithDispatchFreeReads()
    {
        using var primary = new FakeTranslationClient();
        using var fallback = new FakeTranslationClient();
        EnqueueSuccess(primary, "en", "RU-RESULT-TEXT");
        var (fixture, _) = await CreateTranslationFixtureAsync(primary, fallback);
        var access = await CreateVerifiedAccountAsync(fixture, "restart@example.test");
        const string source = "Restart durability probe.";
        var operationId = OperationIdentity.CreateForTime(fixture.Time.GetUtcNow()).ToString("D");

        using (await fixture.PostRawAsync("/api/operations", SubmitBody(operationId, source), access))
        {
        }

        Assert.AreEqual(2, primary.CallCount);
        var restarted = await fixture.RestartAsync();
        await using (restarted)
        {
            var restartedAccess = await restarted.SignInAsync("restart@example.test");
            using var status = await restarted.GetAsync($"/api/operations/{operationId}", restartedAccess);
            Assert.AreEqual(HttpStatusCode.OK, status.StatusCode);
            var statusBody = await status.Content.ReadFromJsonAsync<JsonElement>();
            Assert.AreEqual("succeeded", statusBody.GetProperty("status").GetString());
            Assert.IsFalse(statusBody.GetProperty("outputAvailable").GetBoolean());
            Assert.AreEqual(source.Length, statusBody.GetProperty("characterCount").GetInt32());
            Assert.AreEqual("2026-09-11", statusBody.GetProperty("admissionDay").GetString());

            using var usage = await restarted.GetAsync("/api/usage", restartedAccess);
            var usageBody = await usage.Content.ReadFromJsonAsync<JsonElement>();
            Assert.AreEqual(source.Length, usageBody.GetProperty("consumedCharacters").GetInt32());
            Assert.AreEqual(0, usageBody.GetProperty("reservedCharacters").GetInt32());

            using var unknown = await restarted.GetAsync($"/api/operations/{OperationIdentity.CreateForTime(restarted.Time.GetUtcNow()):D}", restartedAccess);
            Assert.AreEqual(HttpStatusCode.NotFound, unknown.StatusCode);
            Assert.AreEqual(2, primary.CallCount);
            Assert.AreEqual(0, fallback.CallCount);
        }
    }

    [TestMethod]
    public async Task TranslationFailureCarriesNoSourceTextOrSecrets()
    {
        const string sourceSentinel = "SENTINEL-SOURCE-9f3c2a";
        const string resultSentinel = "SENTINEL-RESULT-7a1e4b";
        using var primary = new FakeTranslationClient();
        using var fallback = new FakeTranslationClient();
        primary.EnqueueThrow(new HttpRequestException("synthetic provider failure"));
        fallback.EnqueueThrow(new HttpRequestException("synthetic provider failure"));
        var (fixture, _) = await CreateTranslationFixtureAsync(primary, fallback);
        await using (fixture)
        {
            var access = await CreateVerifiedAccountAsync(fixture, "privacy@example.test");

            using var failed = await fixture.PostRawAsync(
                "/api/operations",
                SubmitBody(OperationIdentity.CreateForTime(fixture.Time.GetUtcNow()).ToString("D"), $"Translate {sourceSentinel} now."),
                access);
            Assert.AreEqual(HttpStatusCode.ServiceUnavailable, failed.StatusCode);
            var failedBody = await failed.Content.ReadAsStringAsync();
            StringAssert.DoesNotMatch(failedBody, new System.Text.RegularExpressions.Regex("SENTINEL"));
            StringAssert.DoesNotMatch(failedBody, new System.Text.RegularExpressions.Regex("synthetic provider"));

            using var rejected = await fixture.PostRawAsync(
                "/api/operations",
                SubmitBody(OperationIdentity.CreateForTime(fixture.Time.GetUtcNow()).ToString("D"), "   "),
                access);
            var rejectedBody = await rejected.Content.ReadAsStringAsync();
            StringAssert.DoesNotMatch(rejectedBody, new System.Text.RegularExpressions.Regex("SENTINEL"));

            await using var verification = fixture.Database.CreateContext();
            var submissions = await verification.OperationSubmissions.ToListAsync();
            foreach (var submission in submissions)
            {
                Assert.IsFalse(submission.Fingerprint.Contains(sourceSentinel, StringComparison.Ordinal));
                Assert.IsFalse(submission.Fingerprint.Contains(resultSentinel, StringComparison.Ordinal));
            }

            var texts = await verification.MonetaryAttemptReservations
                .Select(item => item.TariffReference + "|" + (item.EvidenceReference ?? string.Empty) + "|" + item.AttemptId + "|" + item.OperationReference)
                .ToListAsync();
            foreach (var text in texts)
            {
                Assert.IsFalse(text.Contains(sourceSentinel, StringComparison.Ordinal));
                Assert.IsFalse(text.Contains(resultSentinel, StringComparison.Ordinal));
            }
        }
    }

    [TestMethod]
    public async Task TranslationSuccessOmitsSourceTextOutsideTranslatedText()
    {
        const string sourceSentinel = "SENTINEL-SOURCE-55aa01";
        using var primary = new FakeTranslationClient();
        using var fallback = new FakeTranslationClient();
        EnqueueSuccess(primary, "en", "CLEAN-RESULT");
        var (fixture, _) = await CreateTranslationFixtureAsync(primary, fallback);
        await using (fixture)
        {
            var access = await CreateVerifiedAccountAsync(fixture, "successprivacy@example.test");

            using var response = await fixture.PostRawAsync(
                "/api/operations",
                SubmitBody(OperationIdentity.CreateForTime(fixture.Time.GetUtcNow()).ToString("D"), $"Translate {sourceSentinel} now."),
                access);

            Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.AreEqual("CLEAN-RESULT", body.GetProperty("translatedText").GetString());
            var raw = await response.Content.ReadAsStringAsync();
            Assert.IsFalse(raw.Contains(sourceSentinel, StringComparison.Ordinal));
        }
    }

    private sealed class FakeTranslationProvider(
        FakeTranslationClient primary,
        FakeTranslationClient fallback) : ITranslationClientProvider
    {
        public bool TryGetClients(out Func<CandidateProfile, IChatClient> clients, out FamilyChain chain)
        {
            clients = profile => string.Equals(profile.CandidateId, FamilyChain.EvaluationPrimaryId, StringComparison.Ordinal)
                ? primary
                : fallback;
            chain = FamilyChain.EvaluationDefault(ChainFamily.Translation);
            return true;
        }
    }

    private sealed class FakeTranslationClient : IChatClient, IDisposable
    {
        private readonly Queue<Func<CancellationToken, Task<ChatResponse>>> behaviors = new();

        public int CallCount { get; private set; }

        public List<string> UserTexts { get; } = [];

        public void EnqueueResponse(string json) =>
            behaviors.Enqueue(_ => Task.FromResult(
                new ChatResponse(new ChatMessage(ChatRole.Assistant, json))));

        public void EnqueueThrow(Exception failure) =>
            behaviors.Enqueue(_ => Task.FromException<ChatResponse>(failure));

        public void EnqueueBehavior(Func<CancellationToken, Task<ChatResponse>> behavior) =>
            behaviors.Enqueue(behavior);

        public void Dispose()
        {
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            var materialized = messages.ToArray();
            UserTexts.Add(materialized[^1].Text);
            return behaviors.Dequeue()(cancellationToken);
        }

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("The translation fake never streams.");
    }
}
