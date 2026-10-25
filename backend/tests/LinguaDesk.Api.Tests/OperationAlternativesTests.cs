using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using LinguaDesk.Api.Features.Operations;
using LinguaDesk.Api.Infrastructure.Smoke;
using LinguaDesk.Infrastructure.Ai;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LinguaDesk.Api.Tests;

[TestClass]
public sealed class OperationAlternativesTests
{
    private static readonly JsonSerializerOptions RequestJson = new(JsonSerializerDefaults.Web) { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull };
    private static readonly JsonSerializerOptions StructuralJson = new(JsonSerializerDefaults.Web);
    private const string Route = "/api/rewriting/alternatives";
    private const string Context = "The report is ready. We send it today.";
    private static readonly Guid Association = Guid.Parse("120df0f9-bd5a-4b8c-9ac9-4c5b56269c78");
    private static AlternativesRequest Request(OperationFixture fixture) => new(OperationIdentity.CreateForTime(fixture.Time.GetUtcNow()), Context, 0, 20, Association, "en");
    private static Task<OperationFixture> CreateAsync(IRewritingClientProvider? provider = null, IReadOnlyDictionary<string, string?>? overrides = null) => OperationFixture.CreateAsync(
        overrides ?? new Dictionary<string, string?> { ["MonetaryAdmission:MonthlyCapMinorUnits"] = "1000000", ["MonetaryAdmission:Currency"] = "USD" }, services =>
        {
            services.RemoveAll<IRewritingClientProvider>();
            services.AddSingleton(provider ?? new DeterministicSmokeRewritingProvider());
        });
    private static async Task<string> AccountAsync(OperationFixture fixture, string email = "alternatives@example.test")
    { await fixture.CreateAccountAsync(email, true); return await fixture.SignInAsync(email); }
    private static Task<HttpResponseMessage> PostAsync(OperationFixture fixture, AlternativesRequest request, string? access) => fixture.PostRawAsync(Route, JsonSerializer.Serialize(request, RequestJson), access);

    [TestMethod]
    public async Task BearerSuccessChargesSentenceOnceReplayAndRestartNeverReturnText()
    {
        await using var fixture = await CreateAsync();
        var access = await AccountAsync(fixture);
        var request = Request(fixture);
        using var response = await PostAsync(fixture, request, access);
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode, await response.Content.ReadAsStringAsync());
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.AreEqual("sentenceAlternatives", result.GetProperty("kind").GetString());
        Assert.AreEqual("en", result.GetProperty("resolvedSourceLanguage").GetString());
        Assert.AreEqual(2, result.GetProperty("options").GetArrayLength());
        Assert.AreEqual(20, result.GetProperty("characterCount").GetInt32());
        Assert.AreEqual(Association, result.GetProperty("association").GetGuid());
        Assert.AreEqual(fixture.Time.GetUtcNow().AddSeconds(15), result.GetProperty("deadlineUtc").GetDateTimeOffset());
        using var duplicate = await PostAsync(fixture, request, access);
        Assert.AreEqual(HttpStatusCode.OK, duplicate.StatusCode);
        var metadata = await duplicate.Content.ReadFromJsonAsync<JsonElement>();
        Assert.IsFalse(metadata.TryGetProperty("options", out _));
        Assert.IsFalse(metadata.TryGetProperty("association", out _));
        Assert.IsFalse(metadata.GetProperty("outputAvailable").GetBoolean());
        using var conflict = await PostAsync(fixture, request with { Association = Guid.NewGuid() }, access);
        Assert.AreEqual(HttpStatusCode.Conflict, conflict.StatusCode);
        using var fullTextConflict = await fixture.PostRawAsync("/api/operations", JsonSerializer.Serialize(new { operationId = request.OperationId, family = "rewriting", source = "The report is ready.", sourceSelection = "en" }), access);
        Assert.AreEqual(HttpStatusCode.Conflict, fullTextConflict.StatusCode);
        using var usage = await fixture.GetAsync("/api/usage", access);
        var snapshot = await usage.Content.ReadFromJsonAsync<JsonElement>();
        Assert.AreEqual(20, snapshot.GetProperty("consumedCharacters").GetInt32());
        Assert.AreEqual(0, snapshot.GetProperty("reservedCharacters").GetInt32());
        await using var restarted = await fixture.RestartAsync();
        var restartedAccess = await restarted.SignInAsync("alternatives@example.test");
        using var status = await restarted.GetAsync($"/api/operations/{request.OperationId}", restartedAccess);
        var statusBody = await status.Content.ReadFromJsonAsync<JsonElement>();
        Assert.AreEqual("sentenceAlternatives", statusBody.GetProperty("kind").GetString());
        Assert.IsFalse(statusBody.TryGetProperty("options", out _));
        Assert.AreEqual(20, statusBody.GetProperty("characterCount").GetInt32());
    }

    [TestMethod]
    public async Task ContextAssociationAndOptionsNeverEnterDurableRowsOrStatus()
    {
        await using var fixture = await CreateAsync();
        var access = await AccountAsync(fixture, "alternatives-privacy@example.test");
        const string sentinel = "PRIVATE-CONTEXT-f38a2";
        var request = Request(fixture) with { Context = Context + " " + sentinel + "." };
        using var original = await PostAsync(fixture, request, access);
        Assert.AreEqual(HttpStatusCode.Created, original.StatusCode);
        await using var database = fixture.Database.CreateContext();
        var durable = JsonSerializer.Serialize(await database.OperationSubmissions.AsNoTracking().ToListAsync())
            + JsonSerializer.Serialize(await database.MonetaryAttemptReservations.AsNoTracking().ToListAsync());
        using var status = await fixture.GetAsync($"/api/operations/{request.OperationId}", access);
        var metadata = await status.Content.ReadAsStringAsync();
        foreach (var transient in new[] { sentinel, request.Association.ToString("D"), "The report is prepared.", "The report is complete." })
        {
            Assert.IsFalse(durable.Contains(transient, StringComparison.Ordinal));
            Assert.IsFalse(metadata.Contains(transient, StringComparison.Ordinal));
        }
        var fields = database.Model.FindEntityType(typeof(OperationSubmission))!.GetProperties().Select(property => property.Name).ToArray();
        foreach (var forbidden in new[] { "Context", "Source", "Options", "Association", "SelectedText" }) Assert.DoesNotContain(forbidden, fields);
    }

    [TestMethod]
    public async Task CookieClientUsesRealSignInAndReceivesOriginalOptions()
    {
        await using var fixture = await CreateAsync();
        await fixture.CreateAccountAsync("cookie-alt@example.test", true);
        using var client = fixture.Factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = true });
        using var bootstrap = await client.GetAsync("/api/accounts/antiforgery");
        var token = (await bootstrap.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("requestToken").GetString();
        using var signIn = new HttpRequestMessage(HttpMethod.Post, "/api/accounts/sign-in") { Content = JsonContent.Create(new { email = "cookie-alt@example.test", password = OperationAdmissionTests.Password }) };
        signIn.Headers.Add("X-LinguaDesk-Antiforgery", token);
        using var login = await client.SendAsync(signIn);
        Assert.AreEqual(HttpStatusCode.OK, login.StatusCode);
        using var result = await client.PostAsJsonAsync(Route, Request(fixture) with { Mode = "correctionOnly" });
        Assert.AreEqual(HttpStatusCode.Created, result.StatusCode, await result.Content.ReadAsStringAsync());
    }

    [TestMethod]
    public async Task StructuralAndSemanticRejectionsMakeNoProviderCalls()
    {
        var provider = new RecordingProvider();
        await using var fixture = await CreateAsync(provider);
        var access = await AccountAsync(fixture);
        var request = Request(fixture);
        foreach (var invalid in new[] { request with { Start = 1 }, request with { Length = 19 }, request with { Language = "auto" }, request with { Association = Guid.Empty }, request with { Context = new string('a', 8001) }, request with { Context = "a\0b", Length = 3 }, request with { Mode = "unknown" } })
        {
            using var response = await PostAsync(fixture, invalid, access);
            Assert.AreEqual(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        }
        var valid = JsonSerializer.Serialize(request, StructuralJson);
        foreach (var invalid in new[] { valid[..^1] + ",\"language\":\"en\"}", valid[..^1] + ",\"count\":20}", valid.Replace("\"mode\":\"correctionOnly\"", "\"mode\":null", StringComparison.Ordinal), valid.Replace("\"length\":20", "\"length\":20.5", StringComparison.Ordinal) })
        {
            using var response = await fixture.PostRawAsync(Route, invalid, access);
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }
        using var tooLarge = await fixture.PostRawAsync(Route, new string(' ', 131073), access);
        Assert.AreEqual(HttpStatusCode.RequestEntityTooLarge, tooLarge.StatusCode);
        Assert.AreEqual(0, provider.Calls);
        await using var database = fixture.Database.CreateContext();
        Assert.AreEqual(0, await database.OperationSubmissions.CountAsync());
    }

    [TestMethod]
    public async Task AuthOwnershipAndSelectedSentenceAllowanceAreIndependent()
    {
        await using var fixture = await CreateAsync(overrides: new Dictionary<string, string?> { ["Operations:UserDailyAllowanceCharacters"] = "20", ["MonetaryAdmission:MonthlyCapMinorUnits"] = "1000000", ["MonetaryAdmission:Currency"] = "USD" });
        var access = await AccountAsync(fixture);
        using var anonymous = await PostAsync(fixture, Request(fixture), null);
        Assert.AreEqual(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        using var first = await PostAsync(fixture, Request(fixture) with { Mode = "simple" }, access);
        Assert.AreEqual(HttpStatusCode.Created, first.StatusCode);
        using var exhausted = await PostAsync(fixture, Request(fixture) with { Mode = "simple" }, access);
        Assert.AreEqual(HttpStatusCode.TooManyRequests, exhausted.StatusCode);
        var other = await AccountAsync(fixture, "other-alt@example.test");
        var original = await first.Content.ReadFromJsonAsync<JsonElement>();
        using var hidden = await fixture.GetAsync($"/api/operations/{original.GetProperty("operationId").GetGuid()}", other);
        Assert.AreEqual(HttpStatusCode.NotFound, hidden.StatusCode);
    }

    [TestMethod]
    public async Task UnavailableProviderSettlesFailureInsteadOfPendingScaffold()
    {
        await using var fixture = await CreateAsync(new UnavailableRewritingClientProvider());
        var access = await AccountAsync(fixture);
        var request = Request(fixture) with { Mode = "simple" };
        using var response = await PostAsync(fixture, request, access);
        Assert.AreEqual(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        using var status = await fixture.GetAsync($"/api/operations/{request.OperationId}", access);
        var body = await status.Content.ReadFromJsonAsync<JsonElement>();
        Assert.AreEqual("failed", body.GetProperty("status").GetString());
        Assert.AreEqual(0, body.GetProperty("characterCount").GetInt32());
    }

    [TestMethod]
    public void AlternativeFingerprintIsLengthDelimitedAndOldFullTextBytesAreUnchanged()
    {
        var key = Encoding.UTF8.GetBytes("test-key");
        var request = new AlternativesRequest(Guid.NewGuid(), Context, 0, 20, Association, "en");
        Assert.AreEqual(OperationFingerprint.ComputeAlternatives(key, request), OperationFingerprint.ComputeAlternatives(key, request with { Mode = "correctionOnly" }));
        Assert.AreNotEqual(OperationFingerprint.ComputeAlternatives(key, request), OperationFingerprint.ComputeAlternatives(key, request with { Start = 1 }));
        var canonical = Encoding.UTF8.GetBytes("linguadesk-operation-v1\0rewriting\0hello\0auto\0\0correctionOnly\0unicode-scalar-v1");
        Assert.AreEqual(Convert.ToHexString(System.Security.Cryptography.HMACSHA256.HashData(key, canonical)), OperationFingerprint.Compute(key, "rewriting", "hello", "auto", null, "correctionOnly"));
    }

    [TestMethod]
    public async Task ConcurrentDuplicateObservesPendingAndOnlyOriginalDeliveryHasOptions()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var provider = new CallbackProvider(async token => { started.SetResult(); await release.Task.WaitAsync(token); });
        await using var fixture = await CreateAsync(provider);
        var access = await AccountAsync(fixture);
        var request = Request(fixture);
        var first = PostAsync(fixture, request, access);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        using var duplicate = await PostAsync(fixture, request, access);
        Assert.AreEqual(HttpStatusCode.Accepted, duplicate.StatusCode);
        var metadata = await duplicate.Content.ReadFromJsonAsync<JsonElement>();
        Assert.AreEqual("sentenceAlternatives", metadata.GetProperty("kind").GetString());
        Assert.AreEqual(20, metadata.GetProperty("characterCount").GetInt32());
        release.SetResult();
        using var original = await first;
        Assert.AreEqual(HttpStatusCode.Created, original.StatusCode);
        Assert.AreEqual(2, provider.Calls);
        await using var database = fixture.Database.CreateContext();
        Assert.AreEqual(1, await database.OperationSubmissions.CountAsync());
        Assert.AreEqual(20, (await database.CharacterLedgerEntries.SingleAsync(row => row.Scope == "user")).ConsumedCharacters);
    }

    [TestMethod]
    public async Task DeadlineFencesLateOptionsAndRetainsUncertainProviderCost()
    {
        OperationFixture? captured = null;
        var provider = new CallbackProvider(_ => { captured!.Time.Advance(TimeSpan.FromSeconds(16)); return Task.CompletedTask; });
        await using var fixture = await CreateAsync(provider);
        captured = fixture;
        var access = await AccountAsync(fixture);
        var request = Request(fixture);
        using var response = await PostAsync(fixture, request, access);
        Assert.AreEqual(HttpStatusCode.GatewayTimeout, response.StatusCode);
        using var status = await fixture.GetAsync($"/api/operations/{request.OperationId}", access);
        var metadata = await status.Content.ReadFromJsonAsync<JsonElement>();
        Assert.AreEqual(0, metadata.GetProperty("characterCount").GetInt32());
        Assert.IsFalse(metadata.TryGetProperty("options", out _));
        await using var database = fixture.Database.CreateContext();
        Assert.AreEqual(0, (await database.CharacterLedgerEntries.SingleAsync(row => row.Scope == "user")).ConsumedCharacters);
        Assert.IsTrue(await database.MonetaryAttemptReservations.AllAsync(row => row.State == "reserved"));
    }

    [TestMethod]
    public async Task MidnightSettlementChargesAdmissionDayAndReturnsFreshNewDayUsage()
    {
        OperationFixture? captured = null;
        var provider = new CallbackProvider(_ => { captured!.Time.Advance(TimeSpan.FromSeconds(2)); return Task.CompletedTask; });
        await using var fixture = await CreateAsync(provider);
        captured = fixture;
        fixture.Time.Advance(new DateTimeOffset(2026, 9, 11, 23, 59, 59, TimeSpan.Zero) - fixture.Time.GetUtcNow());
        var access = await AccountAsync(fixture);
        using var response = await PostAsync(fixture, Request(fixture), access);
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.AreEqual("2026-09-11", body.GetProperty("admissionDay").GetString());
        Assert.AreEqual("2026-09-12", body.GetProperty("usage").GetProperty("day").GetString());
        Assert.AreEqual(0, body.GetProperty("usage").GetProperty("consumedCharacters").GetInt32());
        await using var database = fixture.Database.CreateContext();
        Assert.AreEqual(20, (await database.CharacterLedgerEntries.SingleAsync(row => row.Scope == "user" && row.Day == "2026-09-11")).ConsumedCharacters);
    }

    [TestMethod]
    public async Task PerOperationCeilingDeniesBeforeAnyProviderWorkAndReleasesCharacterReservation()
    {
        var provider = new RecordingProvider();
        await using var fixture = await CreateAsync(provider, new Dictionary<string, string?>
        { ["MonetaryAdmission:MonthlyCapMinorUnits"] = "1000000", ["MonetaryAdmission:Currency"] = "USD", ["Serving:Rewriting:MaxSpendUsdPerOperation"] = "0.000001" });
        var access = await AccountAsync(fixture);
        using var response = await PostAsync(fixture, Request(fixture), access);
        Assert.AreEqual(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.AreEqual("monetarySuspension", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("category").GetString());
        Assert.AreEqual(0, provider.Calls);
        await using var database = fixture.Database.CreateContext();
        Assert.AreEqual(0, (await database.CharacterLedgerEntries.SingleAsync(row => row.Scope == "user")).ReservedCharacters);
    }

    [TestMethod]
    public async Task PriorSchemaCopyBackfillsKindAndPreservesUnexpiredFullTextFingerprintAcrossRestart()
    {
        await using var original = await OperationFixture.CreateAsync(configureServices: services =>
        {
            services.RemoveAll<ITranslationClientProvider>();
            services.AddSingleton<ITranslationClientProvider, UnavailableTranslationClientProvider>();
        });
        var access = await AccountAsync(original, "legacy-upgrade@example.test");
        var operationId = OperationIdentity.CreateForTime(original.Time.GetUtcNow());
        var payload = JsonSerializer.Serialize(new { operationId, family = "translation", source = "Legacy.", target = "ro" });
        using var admitted = await original.PostRawAsync("/api/operations", payload, access);
        Assert.AreEqual(HttpStatusCode.Accepted, admitted.StatusCode);
        var deadline = (await admitted.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("deadlineUtc").GetDateTimeOffset();
        string fingerprint;
        await using (var context = original.Database.CreateContext())
        {
            fingerprint = (await context.OperationSubmissions.SingleAsync()).Fingerprint;
            // Produce the prior schema only in this isolated fixture; it contains no alternative identities.
            await context.GetService<IMigrator>().MigrateAsync("20260911032126_MonetaryAdmission");
        }
        await using var copied = StorageTestDatabase.Create();
        using (var source = new SqliteConnection(original.Database.ConnectionPolicy.CreateRuntimeConnectionString()))
        using (var destination = new SqliteConnection(copied.ConnectionPolicy.CreateMigrationConnectionString()))
        {
            await source.OpenAsync(); await destination.OpenAsync(); source.BackupDatabase(destination);
        }
        await copied.MigrateAsync();
        var keys = Directory.CreateDirectory(Path.Combine(copied.RootPath, "keys")).FullName;
        foreach (var file in Directory.GetFiles(original.KeysPath)) File.Copy(file, Path.Combine(keys, Path.GetFileName(file)));
        await using var upgraded = OperationFixture.Attach(copied, keys, initialTime: original.Time.GetUtcNow());
        var upgradedAccess = await upgraded.SignInAsync("legacy-upgrade@example.test");
        using var replay = await upgraded.PostRawAsync("/api/operations", payload, upgradedAccess);
        Assert.AreEqual(HttpStatusCode.Accepted, replay.StatusCode);
        var body = await replay.Content.ReadFromJsonAsync<JsonElement>();
        Assert.AreEqual("fullText", body.GetProperty("kind").GetString());
        Assert.AreEqual(deadline, body.GetProperty("deadlineUtc").GetDateTimeOffset());
        await using (var context = copied.CreateContext())
        {
            Assert.AreEqual(fingerprint, (await context.OperationSubmissions.SingleAsync()).Fingerprint);
            Assert.AreEqual(7, (await context.CharacterLedgerEntries.SingleAsync(row => row.Scope == "user")).ReservedCharacters);
        }
        upgraded.Time.Advance(TimeSpan.FromSeconds(31));
        await using var restarted = await upgraded.RestartAsync();
        var restartedAccess = await restarted.SignInAsync("legacy-upgrade@example.test");
        using var status = await restarted.GetAsync($"/api/operations/{operationId}", restartedAccess);
        var terminal = await status.Content.ReadFromJsonAsync<JsonElement>();
        Assert.AreEqual("interrupted", terminal.GetProperty("status").GetString());
        Assert.AreEqual("fullText", terminal.GetProperty("kind").GetString());
        Assert.AreEqual(0, terminal.GetProperty("characterCount").GetInt32());
    }

    private sealed class CallbackProvider(Func<CancellationToken, Task> generation) : IRewritingClientProvider, IChatClient
    {
        public int Calls { get; private set; }
        public bool TryGetClients(out Func<CandidateProfile, IChatClient> clients, out FamilyChain chain) { clients = _ => this; chain = new(ChainFamily.Rewriting, CandidateRegistry.Default[0], null); return true; }
        public void Dispose() { }
        public object? GetService(Type serviceType, object? serviceKey = null) => null;
        public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            Calls++;
            var eligibility = messages.Last().Text.Contains("sourceHint", StringComparison.Ordinal);
            if (!eligibility) await generation(cancellationToken);
            return new(new ChatMessage(ChatRole.Assistant, eligibility ? "{\"status\":\"eligible\",\"language\":\"en\"}" : "{\"status\":\"result\",\"options\":[\"The report is prepared.\",\"The report is complete.\"]}"));
        }
        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class RecordingProvider : IRewritingClientProvider, IChatClient
    {
        public int Calls { get; private set; }
        public bool TryGetClients(out Func<CandidateProfile, IChatClient> clients, out FamilyChain chain) { clients = _ => this; chain = new(ChainFamily.Rewriting, CandidateRegistry.Default[0], null); return true; }
        public void Dispose() { }
        public object? GetService(Type serviceType, object? serviceKey = null) => null;
        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) { Calls++; throw new InvalidOperationException("Unexpected provider dispatch."); }
        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
