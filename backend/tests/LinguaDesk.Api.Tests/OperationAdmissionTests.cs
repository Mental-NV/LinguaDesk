using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using LinguaDesk.Api.Features.Identity.Registration;
using LinguaDesk.Api.Features.Operations;
using LinguaDesk.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LinguaDesk.Api.Tests;

[TestClass]
public sealed class OperationAdmissionTests
{
    internal const string Password = " exact password 15 ";

    [TestMethod]
    public async Task ConcurrentIdenticalClaimsAdmitExactlyOneReservation()
    {
        await using var fixture = await OperationFixture.CreateAsync();
        var user = await fixture.CreateAccountAsync("same@example.test", confirmed: true);
        var access = await fixture.SignInAsync("same@example.test");
        var operationId = OperationIdentity.CreateForTime(fixture.Time.GetUtcNow()).ToString("D");
        var body = JsonSerializer.Serialize(new
        {
            operationId,
            family = "translation",
            source = "Reservation probe text",
            target = "ru",
        });

        var attempts = Enumerable.Range(0, 8)
            .Select(_ => fixture.PostRawAsync("/api/operations", body, access))
            .ToArray();
        var responses = await Task.WhenAll(attempts);
        try
        {
            foreach (var response in responses)
            {
                Assert.AreEqual(HttpStatusCode.Accepted, response.StatusCode);
                Assert.AreEqual("no-store", response.Headers.CacheControl?.ToString());
            }

            var bodies = new List<string>();
            foreach (var response in responses)
            {
                bodies.Add(await response.Content.ReadAsStringAsync());
            }

            Assert.HasCount(8, bodies);
            foreach (var candidate in bodies.Skip(1))
            {
                Assert.AreEqual(bodies[0], candidate);
            }

            using var envelope = JsonDocument.Parse(bodies[0]);
            Assert.AreEqual(operationId, envelope.RootElement.GetProperty("operationId").GetString());
            Assert.AreEqual("pending", envelope.RootElement.GetProperty("status").GetString());
            Assert.AreEqual(22, envelope.RootElement.GetProperty("characterCount").GetInt32());
            Assert.AreEqual("2026-09-11", envelope.RootElement.GetProperty("admissionDay").GetString());
            var usage = envelope.RootElement.GetProperty("usage");
            Assert.AreEqual("2026-09-11", usage.GetProperty("day").GetString());
            Assert.AreEqual(20000, usage.GetProperty("allowanceCharacters").GetInt32());
            Assert.AreEqual(22, usage.GetProperty("reservedCharacters").GetInt32());
            Assert.AreEqual(20000 - 22, usage.GetProperty("availableCharacters").GetInt32());
            Assert.AreEqual(1L, usage.GetProperty("revision").GetInt64());
            var serverTime = envelope.RootElement.GetProperty("serverTimeUtc").GetDateTimeOffset();
            var deadline = envelope.RootElement.GetProperty("deadlineUtc").GetDateTimeOffset();
            Assert.AreEqual(TimeSpan.FromSeconds(30), deadline - serverTime);
        }
        finally
        {
            foreach (var response in responses)
            {
                response.Dispose();
            }
        }

        await using var verification = fixture.Database.CreateContext();
        Assert.AreEqual(1, await verification.OperationSubmissions.CountAsync());
        Assert.AreEqual(2, await verification.CharacterLedgerEntries.CountAsync());
        var stored = await verification.OperationSubmissions.SingleAsync();
        Assert.AreEqual(user.Id, stored.AccountId);
        Assert.AreEqual(operationId, stored.OperationId);
        Assert.AreEqual("pending", stored.State);
        Assert.AreEqual(22, stored.ScalarCount);
        Assert.AreEqual("2026-09-11", stored.AdmissionDay);
        Assert.AreEqual(22, await verification.CharacterLedgerEntries
            .Where(entry => entry.Scope == "user")
            .Select(entry => entry.ReservedCharacters)
            .SingleAsync());
        Assert.AreEqual(1L, (await verification.LedgerRevisions.SingleAsync()).Value);

        using var status = await fixture.GetAsync($"/api/operations/{operationId}", access);
        Assert.AreEqual(HttpStatusCode.OK, status.StatusCode);
        Assert.AreEqual("no-store", status.Headers.CacheControl?.ToString());
        StringAssert.Contains(await status.Content.ReadAsStringAsync(), "\"status\":\"pending\"");
    }

    [TestMethod]
    public async Task ChangedEffectivePayloadConflictsWithoutLedgerChange()
    {
        await using var fixture = await OperationFixture.CreateAsync();
        await fixture.CreateAccountAsync("conflict@example.test", confirmed: true);
        var access = await fixture.SignInAsync("conflict@example.test");
        var operationId = OperationIdentity.CreateForTime(fixture.Time.GetUtcNow()).ToString("D");
        using var admitted = await fixture.PostRawAsync(
            "/api/operations",
            JsonSerializer.Serialize(new
            {
                operationId,
                family = "translation",
                source = "Line one\nLine two",
                sourceSelection = "en",
                target = "ru",
            }),
            access);
        Assert.AreEqual(HttpStatusCode.Accepted, admitted.StatusCode);

        var conflicts = new[]
        {
            new { family = "rewriting", source = "Line one\nLine two", sourceSelection = "en", target = (string?)null, mode = (string?)null },
            new { family = "translation", source = "Line one\nLine two!", sourceSelection = "en", target = (string?)"ru", mode = (string?)null },
            new { family = "translation", source = "Line one\r\nLine two", sourceSelection = "en", target = (string?)"ru", mode = (string?)null },
            new { family = "translation", source = "Line one\nLine two", sourceSelection = "auto", target = (string?)"ru", mode = (string?)null },
            new { family = "translation", source = "Line one\nLine two", sourceSelection = "en", target = (string?)"ro", mode = (string?)null },
        };
        var omitNulls = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        };
        foreach (var conflict in conflicts)
        {
            using var response = await fixture.PostRawAsync(
                "/api/operations",
                JsonSerializer.Serialize(new
                {
                    operationId,
                    family = conflict.family,
                    source = conflict.source,
                    sourceSelection = conflict.sourceSelection,
                    target = conflict.target,
                    mode = conflict.mode,
                }, omitNulls),
                access);
            var body = await response.Content.ReadAsStringAsync();
            Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode, body);
            Assert.AreEqual("no-store", response.Headers.CacheControl?.ToString());
            using var problem = JsonDocument.Parse(body);
            Assert.AreEqual("identityConflict", problem.RootElement.GetProperty("category").GetString());
        }

        using var rewriting = await fixture.PostRawAsync(
            "/api/operations",
            JsonSerializer.Serialize(new
            {
                operationId = OperationIdentity.CreateForTime(fixture.Time.GetUtcNow()).ToString("D"),
                family = "rewriting",
                source = "Rewrite probe text",
                mode = "simple",
            }),
            access);
        Assert.AreEqual(HttpStatusCode.Accepted, rewriting.StatusCode);
        var rewritingId = JsonDocument.Parse(await rewriting.Content.ReadAsStringAsync())
            .RootElement.GetProperty("operationId").GetString()!;
        using var modeConflict = await fixture.PostRawAsync(
            "/api/operations",
            JsonSerializer.Serialize(new
            {
                operationId = rewritingId,
                family = "rewriting",
                source = "Rewrite probe text",
                mode = "casual",
            }),
            access);
        Assert.AreEqual(HttpStatusCode.Conflict, modeConflict.StatusCode);

        await using var verification = fixture.Database.CreateContext();
        Assert.AreEqual(2, await verification.OperationSubmissions.CountAsync());
        Assert.AreEqual(35, await verification.CharacterLedgerEntries
            .Where(entry => entry.Scope == "user")
            .Select(entry => entry.ReservedCharacters)
            .SingleAsync());
        Assert.AreEqual(2L, (await verification.LedgerRevisions.SingleAsync()).Value);
    }

    [TestMethod]
    public async Task EquivalentJsonSpellingsMatchTheSameOperation()
    {
        await using var fixture = await OperationFixture.CreateAsync();
        await fixture.CreateAccountAsync("spelling@example.test", confirmed: true);
        var access = await fixture.SignInAsync("spelling@example.test");
        var operationId = OperationIdentity.CreateForTime(fixture.Time.GetUtcNow()).ToString("D");
        using var first = await fixture.PostRawAsync(
            "/api/operations",
            JsonSerializer.Serialize(new
            {
                operationId,
                family = "translation",
                source = "Hello",
                target = "ru",
            }),
            access);
        Assert.AreEqual(HttpStatusCode.Accepted, first.StatusCode);
        var firstBody = await first.Content.ReadAsStringAsync();

        var reordered = "{\"target\":\"ru\",\"source\":\"Hello\",\"family\":\"translation\",\"operationId\":\"" + operationId + "\"}";
        using var second = await fixture.PostRawAsync("/api/operations", reordered, access);
        Assert.AreEqual(HttpStatusCode.Accepted, second.StatusCode);
        Assert.AreEqual(firstBody, await second.Content.ReadAsStringAsync());

        var escaped = "{\"operationId\":\"" + operationId + "\",\"family\":\"translation\",\"source\":\"\\u0048\\u0065\\u006c\\u006c\\u006f\",\"target\":\"ru\"}";
        using var third = await fixture.PostRawAsync("/api/operations", escaped, access);
        Assert.AreEqual(HttpStatusCode.Accepted, third.StatusCode);
        Assert.AreEqual(firstBody, await third.Content.ReadAsStringAsync());

        var defaulted = JsonSerializer.Serialize(new
        {
            operationId,
            family = "translation",
            source = "Hello",
            sourceSelection = "auto",
            target = "ru",
        });
        using var fourth = await fixture.PostRawAsync("/api/operations", defaulted, access);
        Assert.AreEqual(HttpStatusCode.Accepted, fourth.StatusCode);
        Assert.AreEqual(firstBody, await fourth.Content.ReadAsStringAsync());

        await using var verification = fixture.Database.CreateContext();
        Assert.AreEqual(1, await verification.OperationSubmissions.CountAsync());
        Assert.AreEqual(5, await verification.CharacterLedgerEntries
            .Where(entry => entry.Scope == "user")
            .Select(entry => entry.ReservedCharacters)
            .SingleAsync());
        Assert.AreEqual(1L, (await verification.LedgerRevisions.SingleAsync()).Value);
    }

    [TestMethod]
    public async Task ExpiredIdentityIsRejectedEvenAfterRecordCleanup()
    {
        await using var fixture = await OperationFixture.CreateAsync();
        await fixture.CreateAccountAsync("expiry@example.test", confirmed: true);
        var access = await fixture.SignInAsync("expiry@example.test");
        var operationId = OperationIdentity.CreateForTime(fixture.Time.GetUtcNow()).ToString("D");
        using var admitted = await fixture.PostRawAsync(
            "/api/operations",
            JsonSerializer.Serialize(new { operationId, family = "translation", source = "Expiry probe", target = "ru" }),
            access);
        Assert.AreEqual(HttpStatusCode.Accepted, admitted.StatusCode);

        await using (var cleanup = fixture.Database.CreateContext())
        {
            var stored = await cleanup.OperationSubmissions.SingleAsync();
            cleanup.OperationSubmissions.Remove(stored);
            await cleanup.SaveChangesAsync();
        }

        fixture.Time.Advance(TimeSpan.FromHours(25));
        var freshAccess = await fixture.SignInAsync("expiry@example.test");
        using var replay = await fixture.PostRawAsync(
            "/api/operations",
            JsonSerializer.Serialize(new { operationId, family = "translation", source = "Expiry probe", target = "ru" }),
            freshAccess);
        var replayBody = await replay.Content.ReadAsStringAsync();
        Assert.AreEqual(HttpStatusCode.Gone, replay.StatusCode, replayBody);
        using var replayProblem = JsonDocument.Parse(replayBody);
        Assert.AreEqual("identityExpired", replayProblem.RootElement.GetProperty("category").GetString());
        Assert.IsTrue(replayProblem.RootElement.TryGetProperty("serverTimeUtc", out _));

        using var status = await fixture.GetAsync($"/api/operations/{operationId}", freshAccess);
        Assert.AreEqual(HttpStatusCode.Gone, status.StatusCode);

        await using var verification = fixture.Database.CreateContext();
        Assert.AreEqual(0, await verification.OperationSubmissions.CountAsync());
    }

    [TestMethod]
    public async Task MalformedAndFutureIdentitiesAreRejectedWithoutRecords()
    {
        await using var fixture = await OperationFixture.CreateAsync();
        await fixture.CreateAccountAsync("identity@example.test", confirmed: true);
        var access = await fixture.SignInAsync("identity@example.test");
        var future = OperationIdentity.CreateForTime(fixture.Time.GetUtcNow().AddMinutes(10)).ToString("D");
        var cases = new[] { "not-a-guid", Guid.NewGuid().ToString("D"), future };
        foreach (var operationId in cases)
        {
            using var response = await fixture.PostRawAsync(
                "/api/operations",
                JsonSerializer.Serialize(new { operationId, family = "translation", source = "Identity probe", target = "ru" }),
                access);
            var body = await response.Content.ReadAsStringAsync();
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode, body);
            using var problem = JsonDocument.Parse(body);
            Assert.AreEqual("invalidIdentity", problem.RootElement.GetProperty("category").GetString());
            Assert.IsTrue(problem.RootElement.TryGetProperty("serverTimeUtc", out _));
        }

        await using var verification = fixture.Database.CreateContext();
        Assert.AreEqual(0, await verification.OperationSubmissions.CountAsync());
        Assert.AreEqual(0, await verification.CharacterLedgerEntries.CountAsync());
        Assert.AreEqual(0L, (await verification.LedgerRevisions.SingleAsync()).Value);
    }

    [TestMethod]
    public async Task UserAndGlobalAllowancesDenyOverCapacityWithoutOverrun()
    {
        await using var fixture = await OperationFixture.CreateAsync(new Dictionary<string, string?>
        {
            ["Operations:UserDailyAllowanceCharacters"] = "100",
            ["Operations:GlobalDailyAllowanceCharacters"] = "1000000",
        });
        await fixture.CreateAccountAsync("capacity@example.test", confirmed: true);
        var access = await fixture.SignInAsync("capacity@example.test");
        var sixty = new string('a', 60);
        var forty = new string('b', 40);
        using (await fixture.PostRawAsync(
            "/api/operations",
            JsonSerializer.Serialize(new { operationId = OperationIdentity.CreateForTime(fixture.Time.GetUtcNow()).ToString("D"), family = "translation", source = sixty, target = "ru" }),
            access))
        {
        }

        using (await fixture.PostRawAsync(
            "/api/operations",
            JsonSerializer.Serialize(new { operationId = OperationIdentity.CreateForTime(fixture.Time.GetUtcNow()).ToString("D"), family = "translation", source = forty, target = "ru" }),
            access))
        {
        }

        using var denied = await fixture.PostRawAsync(
            "/api/operations",
            JsonSerializer.Serialize(new { operationId = OperationIdentity.CreateForTime(fixture.Time.GetUtcNow()).ToString("D"), family = "translation", source = "c", target = "ru" }),
            access);
        var deniedBody = await denied.Content.ReadAsStringAsync();
        Assert.AreEqual((HttpStatusCode)429, denied.StatusCode, deniedBody);
        using var deniedProblem = JsonDocument.Parse(deniedBody);
        Assert.AreEqual("userAllowance", deniedProblem.RootElement.GetProperty("category").GetString());
        Assert.IsTrue(deniedProblem.RootElement.TryGetProperty("resetAtUtc", out _));
        Assert.IsFalse(deniedBody.Contains("global", StringComparison.OrdinalIgnoreCase));

        await using var verification = fixture.Database.CreateContext();
        Assert.AreEqual(100, await verification.CharacterLedgerEntries
            .Where(entry => entry.Scope == "user")
            .Select(entry => entry.ReservedCharacters)
            .SingleAsync());

        await using var global = await OperationFixture.CreateAsync(new Dictionary<string, string?>
        {
            ["Operations:UserDailyAllowanceCharacters"] = "1000000",
            ["Operations:GlobalDailyAllowanceCharacters"] = "100",
        });
        var tokens = new List<string>();
        for (var index = 0; index < 3; index++)
        {
            var email = $"global-{index}@example.test";
            await global.CreateAccountAsync(email, confirmed: true);
            tokens.Add(await global.SignInAsync(email));
        }

        using (await global.PostRawAsync(
            "/api/operations",
            JsonSerializer.Serialize(new { operationId = OperationIdentity.CreateForTime(fixture.Time.GetUtcNow()).ToString("D"), family = "translation", source = sixty, target = "ru" }),
            tokens[0]))
        {
        }

        using (await global.PostRawAsync(
            "/api/operations",
            JsonSerializer.Serialize(new { operationId = OperationIdentity.CreateForTime(fixture.Time.GetUtcNow()).ToString("D"), family = "translation", source = forty, target = "ru" }),
            tokens[1]))
        {
        }

        using var globalDenied = await global.PostRawAsync(
            "/api/operations",
            JsonSerializer.Serialize(new { operationId = OperationIdentity.CreateForTime(fixture.Time.GetUtcNow()).ToString("D"), family = "translation", source = "c", target = "ru" }),
            tokens[2]);
        var globalBody = await globalDenied.Content.ReadAsStringAsync();
        Assert.AreEqual((HttpStatusCode)429, globalDenied.StatusCode, globalBody);
        using var globalProblem = JsonDocument.Parse(globalBody);
        Assert.AreEqual("globalAllowance", globalProblem.RootElement.GetProperty("category").GetString());
        Assert.IsFalse(globalBody.Contains("ConsumedCharacters", StringComparison.Ordinal));
        Assert.IsFalse(globalBody.Contains("ReservedCharacters", StringComparison.Ordinal));

        await using var globalVerification = global.Database.CreateContext();
        Assert.AreEqual(100, await globalVerification.CharacterLedgerEntries
            .Where(entry => entry.Scope == "global")
            .Select(entry => entry.ReservedCharacters)
            .SingleAsync());
        Assert.AreEqual(2, await globalVerification.OperationSubmissions.CountAsync());
    }

    [TestMethod]
    public async Task PreAdmissionFailuresLeaveNoRecordReservationOrDispatch()
    {
        await using var fixture = await OperationFixture.CreateAsync();
        await fixture.CreateAccountAsync("rejected@example.test", confirmed: true);
        var access = await fixture.SignInAsync("rejected@example.test");
        var oversized = new string('z', 5001);

        var unprocessable = new (string Name, string Body, string Reason)[]
        {
            ("empty", JsonSerializer.Serialize(new { operationId = OperationIdentity.CreateForTime(fixture.Time.GetUtcNow()).ToString("D"), family = "translation", source = "   ", target = "ru" }), "emptySource"),
            ("oversize", JsonSerializer.Serialize(new { operationId = OperationIdentity.CreateForTime(fixture.Time.GetUtcNow()).ToString("D"), family = "translation", source = oversized, target = "ru" }), "oversizedSource"),
            ("missingTarget", JsonSerializer.Serialize(new { operationId = OperationIdentity.CreateForTime(fixture.Time.GetUtcNow()).ToString("D"), family = "translation", source = "Eligible text" }), "targetRequired"),
            ("invalidTarget", JsonSerializer.Serialize(new { operationId = OperationIdentity.CreateForTime(fixture.Time.GetUtcNow()).ToString("D"), family = "translation", source = "Eligible text", target = "xx" }), "invalidTarget"),
            ("sameLanguage", JsonSerializer.Serialize(new { operationId = OperationIdentity.CreateForTime(fixture.Time.GetUtcNow()).ToString("D"), family = "translation", source = "Eligible text", sourceSelection = "en", target = "en" }), "sameLanguage"),
            ("invalidMode", JsonSerializer.Serialize(new { operationId = OperationIdentity.CreateForTime(fixture.Time.GetUtcNow()).ToString("D"), family = "rewriting", source = "Eligible text", mode = "shouty" }), "invalidMode"),
        };
        foreach (var item in unprocessable)
        {
            using var response = await fixture.PostRawAsync("/api/operations", item.Body, access);
            var body = await response.Content.ReadAsStringAsync();
            Assert.AreEqual(HttpStatusCode.UnprocessableEntity, response.StatusCode, $"{item.Name}: {body}");
            using var problem = JsonDocument.Parse(body);
            Assert.AreEqual("inputEligibility", problem.RootElement.GetProperty("category").GetString());
            Assert.AreEqual(item.Reason, problem.RootElement.GetProperty("reason").GetString(), item.Name);
        }

        using var unknownFamily = await fixture.PostRawAsync(
            "/api/operations",
            JsonSerializer.Serialize(new { operationId = OperationIdentity.CreateForTime(fixture.Time.GetUtcNow()).ToString("D"), family = "summarization", source = "Eligible text" }),
            access);
        Assert.AreEqual(HttpStatusCode.BadRequest, unknownFamily.StatusCode);

        using var duplicateField = await fixture.PostRawAsync(
            "/api/operations",
            "{\"operationId\":\"" + OperationIdentity.CreateForTime(fixture.Time.GetUtcNow()).ToString("D") + "\",\"family\":\"translation\",\"family\":\"translation\",\"source\":\"Eligible text\",\"target\":\"ru\"}",
            access);
        Assert.AreEqual(HttpStatusCode.BadRequest, duplicateField.StatusCode);

        using var unknownField = await fixture.PostRawAsync(
            "/api/operations",
            JsonSerializer.Serialize(new { operationId = OperationIdentity.CreateForTime(fixture.Time.GetUtcNow()).ToString("D"), family = "translation", source = "Eligible text", target = "ru", traceId = "x" }),
            access);
        Assert.AreEqual(HttpStatusCode.BadRequest, unknownField.StatusCode);

        using var query = await fixture.PostWithQueryAsync(
            "/api/operations?debug=true",
            JsonSerializer.Serialize(new { operationId = OperationIdentity.CreateForTime(fixture.Time.GetUtcNow()).ToString("D"), family = "translation", source = "Eligible text", target = "ru" }),
            access);
        Assert.AreEqual(HttpStatusCode.BadRequest, query.StatusCode);

        using var media = await fixture.PostMediaAsync(
            "/api/operations",
            new StringContent("{}", Encoding.UTF8, "text/plain"),
            access);
        Assert.AreEqual(HttpStatusCode.UnsupportedMediaType, media.StatusCode);

        using var anonymous = await fixture.PostRawAsync(
            "/api/operations",
            JsonSerializer.Serialize(new { operationId = OperationIdentity.CreateForTime(fixture.Time.GetUtcNow()).ToString("D"), family = "translation", source = "Eligible text", target = "ru" }),
            accessToken: null);
        Assert.AreEqual(HttpStatusCode.Unauthorized, anonymous.StatusCode);

        await fixture.CreateAccountAsync("unverified-ops@example.test", confirmed: false);
        var unverifiedAccess = await fixture.SignInAsync("unverified-ops@example.test");
        using var unverified = await fixture.PostRawAsync(
            "/api/operations",
            JsonSerializer.Serialize(new { operationId = OperationIdentity.CreateForTime(fixture.Time.GetUtcNow()).ToString("D"), family = "translation", source = "Eligible text", target = "ru" }),
            unverifiedAccess);
        Assert.AreEqual(HttpStatusCode.Forbidden, unverified.StatusCode);

        await using var verification = fixture.Database.CreateContext();
        Assert.AreEqual(0, await verification.OperationSubmissions.CountAsync());
        Assert.AreEqual(0, await verification.CharacterLedgerEntries.CountAsync());
        Assert.AreEqual(0L, (await verification.LedgerRevisions.SingleAsync()).Value);
    }

    [TestMethod]
    public async Task RestartPreservesReservationRevisionAndDuplicateObservation()
    {
        await using var fixture = await OperationFixture.CreateAsync();
        await fixture.CreateAccountAsync("restart@example.test", confirmed: true);
        var access = await fixture.SignInAsync("restart@example.test");
        var operationId = OperationIdentity.CreateForTime(fixture.Time.GetUtcNow()).ToString("D");
        var operationBody = JsonSerializer.Serialize(new
        {
            operationId,
            family = "translation",
            source = "Restart durable text",
            target = "ru",
        });
        string firstBody;
        using (var admitted = await fixture.PostRawAsync("/api/operations", operationBody, access))
        {
            Assert.AreEqual(HttpStatusCode.Accepted, admitted.StatusCode);
            firstBody = await admitted.Content.ReadAsStringAsync();
        }

        await using var restarted = await fixture.RestartAsync();
        var restartedAccess = await restarted.SignInAsync("restart@example.test");
        using var status = await restarted.GetAsync($"/api/operations/{operationId}", restartedAccess);
        Assert.AreEqual(HttpStatusCode.OK, status.StatusCode);
        var statusBody = await status.Content.ReadAsStringAsync();
        StringAssert.Contains(statusBody, "\"status\":\"pending\"");
        StringAssert.Contains(statusBody, "\"revision\":1");

        using var duplicate = await restarted.PostRawAsync("/api/operations", operationBody, restartedAccess);
        Assert.AreEqual(HttpStatusCode.Accepted, duplicate.StatusCode);
        Assert.AreEqual(firstBody, await duplicate.Content.ReadAsStringAsync());

        await using var verification = restarted.Database.CreateContext();
        Assert.AreEqual(1, await verification.OperationSubmissions.CountAsync());
        Assert.AreEqual(20, await verification.CharacterLedgerEntries
            .Where(entry => entry.Scope == "user")
            .Select(entry => entry.ReservedCharacters)
            .SingleAsync());
        Assert.AreEqual(1L, (await verification.LedgerRevisions.SingleAsync()).Value);
    }

    [TestMethod]
    public async Task OperationRecordsHoldMetadataOnlyWithoutSourceOrSecrets()
    {
        await using var fixture = await OperationFixture.CreateAsync();
        await fixture.CreateAccountAsync("sentinel@example.test", confirmed: true);
        var access = await fixture.SignInAsync("sentinel@example.test");
        var sourceSentinel = "sentinel-source-" + Guid.NewGuid().ToString("N");
        var secretSentinel = "sentinel-secret-" + Guid.NewGuid().ToString("N");
        var operationId = OperationIdentity.CreateForTime(fixture.Time.GetUtcNow()).ToString("D");
        using var admitted = await fixture.PostRawAsync(
            "/api/operations",
            JsonSerializer.Serialize(new { operationId, family = "translation", source = sourceSentinel, target = "ru" }),
            access);
        Assert.AreEqual(HttpStatusCode.Accepted, admitted.StatusCode);
        var responseBody = await admitted.Content.ReadAsStringAsync();
        Assert.IsFalse(responseBody.Contains(sourceSentinel, StringComparison.Ordinal));
        Assert.IsFalse(responseBody.Contains(secretSentinel, StringComparison.Ordinal));

        using var status = await fixture.GetAsync($"/api/operations/{operationId}", access);
        Assert.IsFalse((await status.Content.ReadAsStringAsync()).Contains(sourceSentinel, StringComparison.Ordinal));

        await using var verification = fixture.Database.CreateContext();
        var stored = await verification.OperationSubmissions.SingleAsync();
        Assert.AreEqual(64, stored.Fingerprint.Length);
        Assert.IsFalse(stored.Fingerprint.Contains(sourceSentinel, StringComparison.Ordinal));
        var tables = await verification.Database.SqlQueryRaw<string>(
            """
            SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%';
            """).ToListAsync();
        foreach (var table in tables.Where(name => name is not ("__EFMigrationsHistory" or "AspNetUsers" or "AspNetUserClaims" or "AspNetUserLogins" or "AspNetUserTokens")))
        {
            var safeTable = table.Replace("'", "''", StringComparison.Ordinal);
            var columns = await verification.Database.SqlQueryRaw<string>(
                string.Concat("SELECT name FROM pragma_table_info('", safeTable, "');")).ToListAsync();
            foreach (var column in columns)
            {
                var safeColumn = column.Replace("\"", "\"\"", StringComparison.Ordinal);
                var safeFrom = table.Replace("\"", "\"\"", StringComparison.Ordinal);
                var values = await verification.Database.SqlQueryRaw<string>(
                    string.Concat("SELECT COALESCE(CAST(\"", safeColumn, "\" AS TEXT), '') FROM \"", safeFrom, "\";")).ToListAsync();
                foreach (var value in values)
                {
                    Assert.IsFalse(value.Contains(sourceSentinel, StringComparison.Ordinal), $"{table}.{column}");
                    Assert.IsFalse(value.Contains(secretSentinel, StringComparison.Ordinal), $"{table}.{column}");
                }
            }
        }
    }

    [TestMethod]
    public async Task StatusReadsAreAccountScopedWithMethodAndShapeBoundaries()
    {
        await using var fixture = await OperationFixture.CreateAsync();
        await fixture.CreateAccountAsync("owner@example.test", confirmed: true);
        await fixture.CreateAccountAsync("other@example.test", confirmed: true);
        var ownerAccess = await fixture.SignInAsync("owner@example.test");
        var otherAccess = await fixture.SignInAsync("other@example.test");
        var operationId = OperationIdentity.CreateForTime(fixture.Time.GetUtcNow()).ToString("D");
        using (await fixture.PostRawAsync(
            "/api/operations",
            JsonSerializer.Serialize(new { operationId, family = "translation", source = "Scoped text", target = "ru" }),
            ownerAccess))
        {
        }

        using var foreign = await fixture.GetAsync($"/api/operations/{operationId}", otherAccess);
        Assert.AreEqual(HttpStatusCode.NotFound, foreign.StatusCode);
        using var foreignProblem = JsonDocument.Parse(await foreign.Content.ReadAsStringAsync());
        Assert.AreEqual("unknownOperation", foreignProblem.RootElement.GetProperty("category").GetString());

        using var unknown = await fixture.GetAsync($"/api/operations/{OperationIdentity.CreateForTime(fixture.Time.GetUtcNow()):D}", ownerAccess);
        Assert.AreEqual(HttpStatusCode.NotFound, unknown.StatusCode);

        using var malformed = await fixture.GetAsync("/api/operations/not-a-guid", ownerAccess);
        Assert.AreEqual(HttpStatusCode.BadRequest, malformed.StatusCode);

        using var query = await fixture.GetAsync($"/api/operations/{operationId}?verbose=true", ownerAccess);
        Assert.AreEqual(HttpStatusCode.BadRequest, query.StatusCode);

        using var anonymous = await fixture.GetAsync($"/api/operations/{operationId}", accessToken: null);
        Assert.AreEqual(HttpStatusCode.Unauthorized, anonymous.StatusCode);

        var unverifiedAccess = await fixture.CreateUnverifiedAndSignInAsync("unverified-status@example.test");
        using var forbidden = await fixture.GetAsync($"/api/operations/{operationId}", unverifiedAccess);
        Assert.AreEqual(HttpStatusCode.Forbidden, forbidden.StatusCode);

        using var wrongMethod = await fixture.SendAsync(new HttpRequestMessage(HttpMethod.Post, $"/api/operations/{operationId}"), ownerAccess);
        Assert.AreEqual(HttpStatusCode.MethodNotAllowed, wrongMethod.StatusCode);
        Assert.AreEqual("GET", wrongMethod.Content.Headers.Allow.Single());

        using var submitWrongMethod = await fixture.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/api/operations"), ownerAccess);
        Assert.AreEqual(HttpStatusCode.MethodNotAllowed, submitWrongMethod.StatusCode);
        Assert.AreEqual("POST", submitWrongMethod.Content.Headers.Allow.Single());
    }

    [TestMethod]
    public async Task SameIdentityIsIndependentAcrossAccounts()
    {
        await using var fixture = await OperationFixture.CreateAsync();
        await fixture.CreateAccountAsync("first-owner@example.test", confirmed: true);
        await fixture.CreateAccountAsync("second-owner@example.test", confirmed: true);
        var firstAccess = await fixture.SignInAsync("first-owner@example.test");
        var secondAccess = await fixture.SignInAsync("second-owner@example.test");
        var operationId = OperationIdentity.CreateForTime(fixture.Time.GetUtcNow()).ToString("D");
        var body = JsonSerializer.Serialize(new { operationId, family = "translation", source = "Shared identity text", target = "ru" });

        using var first = await fixture.PostRawAsync("/api/operations", body, firstAccess);
        using var second = await fixture.PostRawAsync("/api/operations", body, secondAccess);
        Assert.AreEqual(HttpStatusCode.Accepted, first.StatusCode);
        Assert.AreEqual(HttpStatusCode.Accepted, second.StatusCode);

        await using var verification = fixture.Database.CreateContext();
        Assert.AreEqual(2, await verification.OperationSubmissions.CountAsync());
        Assert.AreEqual(2, await verification.CharacterLedgerEntries.CountAsync(entry => entry.Scope == "user"));
    }

    [TestMethod]
    public void UuidV7HelperRoundTripsEmbeddedTime()
    {
        var moment = new DateTimeOffset(2026, 9, 11, 8, 30, 0, TimeSpan.Zero);
        var value = OperationIdentity.CreateForTime(moment);
        Assert.IsTrue(OperationIdentity.IsUuidV7(value));
        Assert.AreEqual(moment, OperationIdentity.ToIdentityTime(value));
        Assert.IsTrue(OperationIdentity.IsUuidV7(Guid.CreateVersion7()));
        Assert.IsFalse(OperationIdentity.IsUuidV7(Guid.NewGuid()));
    }
}

internal sealed class OperationFixture : IAsyncDisposable
{
    private OperationFixture(
        StorageTestDatabase database,
        string keysPath,
        AdjustableTimeProvider time,
        AccountWebApplicationFactory factory,
        HttpClient client)
    {
        Database = database;
        KeysPath = keysPath;
        Time = time;
        Factory = factory;
        Client = client;
    }

    public StorageTestDatabase Database { get; }

    public string KeysPath { get; }

    public AdjustableTimeProvider Time { get; }

    public AccountWebApplicationFactory Factory { get; }

    public HttpClient Client { get; }

    public static async Task<OperationFixture> CreateAsync(
        IReadOnlyDictionary<string, string?>? overrides = null,
        Action<IServiceCollection>? configureServices = null)
    {
        var database = StorageTestDatabase.Create();
        await database.MigrateAsync();
        var keysPath = Directory.CreateDirectory(Path.Combine(database.RootPath, "keys")).FullName;
        return Attach(database, keysPath, overrides, configureServices: configureServices);
    }

    public static OperationFixture Attach(
        StorageTestDatabase database,
        string keysPath,
        IReadOnlyDictionary<string, string?>? overrides = null,
        DateTimeOffset? initialTime = null,
        Action<IServiceCollection>? configureServices = null)
    {
        var time = new AdjustableTimeProvider(new DateTimeOffset(2026, 9, 11, 8, 30, 0, TimeSpan.Zero));
        if (initialTime.HasValue)
        {
            time.Advance(initialTime.Value - time.GetUtcNow());
        }

        var factory = new AccountWebApplicationFactory(
            database.DatabasePath,
            keysPath,
            new CapturingConfirmationSender(),
            timeProvider: time,
            configurationOverrides: overrides,
            configureServices: configureServices);
        var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = false, AllowAutoRedirect = false });
        return new OperationFixture(database, keysPath, time, factory, client);
    }

    public async Task<OperationFixture> RestartAsync()
    {
        var current = Time.GetUtcNow();
        Client.Dispose();
        await Factory.DisposeAsync();
        detached = true;
        var restarted = Attach(Database, KeysPath, initialTime: current);
        return restarted;
    }

    public async Task<IdentityUser> CreateAccountAsync(string email, bool confirmed)
    {
        using var scope = Factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = confirmed };
        var result = await users.CreateAsync(user, OperationAdmissionTests.Password);
        Assert.IsTrue(result.Succeeded, string.Join(',', result.Errors.Select(error => error.Code)));
        return user;
    }

    public async Task<string> CreateUnverifiedAndSignInAsync(string email)
    {
        await CreateAccountAsync(email, confirmed: false);
        return await SignInAsync(email);
    }

    public async Task<string> SignInAsync(string email)
    {
        using var response = await Client.PostAsJsonAsync(
            "/api/accounts/bearer-sign-in",
            new { email, password = OperationAdmissionTests.Password });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("accessToken").GetString()!;
    }

    public Task<HttpResponseMessage> PostRawAsync(string path, string json, string? accessToken)
    {
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return SendAsync(new HttpRequestMessage(HttpMethod.Post, path) { Content = content }, accessToken);
    }

    public Task<HttpResponseMessage> PostWithQueryAsync(string path, string json, string accessToken) =>
        PostRawAsync(path, json, accessToken);

    public Task<HttpResponseMessage> PostMediaAsync(string path, HttpContent content, string accessToken) =>
        SendAsync(new HttpRequestMessage(HttpMethod.Post, path) { Content = content }, accessToken);

    public Task<HttpResponseMessage> GetAsync(string path, string? accessToken) =>
        SendAsync(new HttpRequestMessage(HttpMethod.Get, path), accessToken);

    public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, string? accessToken)
    {
        if (accessToken is not null)
        {
            request.Headers.Authorization = new("Bearer", accessToken);
        }

        return Client.SendAsync(request);
    }

    public async ValueTask DisposeAsync()
    {
        if (detached)
        {
            return;
        }

        Client.Dispose();
        await Factory.DisposeAsync();
        await Database.DisposeAsync();
    }

    private bool detached;
}
