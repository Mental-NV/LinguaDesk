using System.Net;
using System.Text.Json;
using LinguaDesk.Api.Features.Operations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LinguaDesk.Api.Tests;

[TestClass]
public sealed class OperationSettlementTests
{
    private static async Task<(OperationFixture Fixture, string Access, string UserId, string OperationId, int ScalarCount)> AdmitAsync(
        OperationFixture? fixture = null,
        string email = "settler@example.test",
        string source = "Settlement probe text",
        string family = "translation",
        string? target = "ru")
    {
        var owned = fixture ?? await OperationFixture.CreateAsync();
        var user = await owned.CreateAccountAsync(email, confirmed: true);
        var access = await owned.SignInAsync(email);
        var operationId = OperationIdentity.CreateForTime(owned.Time.GetUtcNow()).ToString("D");
        var body = target is null
            ? JsonSerializer.Serialize(new { operationId, family, source })
            : JsonSerializer.Serialize(new { operationId, family, source, target });
        using var admitted = await owned.PostRawAsync("/api/operations", body, access);
        var admittedBody = await admitted.Content.ReadAsStringAsync();
        Assert.AreEqual(HttpStatusCode.Accepted, admitted.StatusCode, admittedBody);
        var scalarCount = JsonDocument.Parse(admittedBody).RootElement.GetProperty("characterCount").GetInt32();
        return (owned, access, user.Id, operationId, scalarCount);
    }

    [TestMethod]
    public async Task SettleSuccessCommitsSingleChargeAndDuplicateIsIdempotent()
    {
        var (fixture, _, userId, operationId, scalarCount) = await AdmitAsync();
        await using (fixture)
        {
            SettlementResult first;
            using (var scope = fixture.Factory.Services.CreateScope())
            {
                var settlements = scope.ServiceProvider.GetRequiredService<OperationSettlementService>();
                first = await settlements.SettleSuccessAsync(
                    userId, "translation", "Settlement probe text", null, "ru", null, Guid.Parse(operationId), CancellationToken.None);
            }

            Assert.AreEqual(SettlementOutcome.Settled, first.Outcome);
            Assert.AreEqual(OperationStates.Succeeded, first.Submission!.State);
            Assert.AreEqual(scalarCount, first.Submission.ScalarCount);

            await using (var verification = fixture.Database.CreateContext())
            {
                var stored = await verification.OperationSubmissions.SingleAsync();
                Assert.AreEqual(OperationStates.Succeeded, stored.State);
                Assert.IsNotNull(stored.SettledUtc);
                var userLedger = await verification.CharacterLedgerEntries
                    .SingleAsync(entry => entry.Scope == "user" && entry.Day == "2026-09-11");
                Assert.AreEqual(scalarCount, userLedger.ConsumedCharacters);
                Assert.AreEqual(0, userLedger.ReservedCharacters);
                var globalLedger = await verification.CharacterLedgerEntries
                    .SingleAsync(entry => entry.Scope == "global" && entry.Day == "2026-09-11");
                Assert.AreEqual(scalarCount, globalLedger.ConsumedCharacters);
                Assert.AreEqual(0, globalLedger.ReservedCharacters);
                Assert.AreEqual(2L, (await verification.LedgerRevisions.SingleAsync()).Value);
            }

            SettlementResult duplicate;
            using (var scope = fixture.Factory.Services.CreateScope())
            {
                var settlements = scope.ServiceProvider.GetRequiredService<OperationSettlementService>();
                duplicate = await settlements.SettleSuccessAsync(
                    userId, "translation", "Settlement probe text", null, "ru", null, Guid.Parse(operationId), CancellationToken.None);
            }

            Assert.AreEqual(SettlementOutcome.DuplicateObserved, duplicate.Outcome);
            Assert.AreEqual(OperationStates.Succeeded, duplicate.Submission!.State);

            await using (var verification = fixture.Database.CreateContext())
            {
                var userLedger = await verification.CharacterLedgerEntries
                    .SingleAsync(entry => entry.Scope == "user" && entry.Day == "2026-09-11");
                Assert.AreEqual(scalarCount, userLedger.ConsumedCharacters);
                Assert.AreEqual(0, userLedger.ReservedCharacters);
                Assert.AreEqual(2L, (await verification.LedgerRevisions.SingleAsync()).Value);
            }
        }
    }

    [TestMethod]
    public async Task SettleFailureReleasesReservationAndDuplicateIsIdempotent()
    {
        var (fixture, _, userId, operationId, scalarCount) = await AdmitAsync(email: "failure@example.test");
        await using (fixture)
        {
            SettlementResult first;
            using (var scope = fixture.Factory.Services.CreateScope())
            {
                var settlements = scope.ServiceProvider.GetRequiredService<OperationSettlementService>();
                first = await settlements.SettleFailureAsync(
                    userId, "translation", "Settlement probe text", null, "ru", null, Guid.Parse(operationId), CancellationToken.None);
            }

            Assert.AreEqual(SettlementOutcome.Settled, first.Outcome);
            Assert.AreEqual(OperationStates.Failed, first.Submission!.State);
            Assert.IsGreaterThan(0, scalarCount);

            await using (var verification = fixture.Database.CreateContext())
            {
                var stored = await verification.OperationSubmissions.SingleAsync();
                Assert.AreEqual(OperationStates.Failed, stored.State);
                Assert.IsNotNull(stored.SettledUtc);
                var userLedger = await verification.CharacterLedgerEntries
                    .SingleAsync(entry => entry.Scope == "user" && entry.Day == "2026-09-11");
                Assert.AreEqual(0, userLedger.ConsumedCharacters);
                Assert.AreEqual(0, userLedger.ReservedCharacters);
                var globalLedger = await verification.CharacterLedgerEntries
                    .SingleAsync(entry => entry.Scope == "global" && entry.Day == "2026-09-11");
                Assert.AreEqual(0, globalLedger.ConsumedCharacters);
                Assert.AreEqual(0, globalLedger.ReservedCharacters);
                Assert.AreEqual(2L, (await verification.LedgerRevisions.SingleAsync()).Value);
            }

            SettlementResult duplicate;
            using (var scope = fixture.Factory.Services.CreateScope())
            {
                var settlements = scope.ServiceProvider.GetRequiredService<OperationSettlementService>();
                duplicate = await settlements.SettleFailureAsync(
                    userId, "translation", "Settlement probe text", null, "ru", null, Guid.Parse(operationId), CancellationToken.None);
            }

            Assert.AreEqual(SettlementOutcome.DuplicateObserved, duplicate.Outcome);

            await using (var verification = fixture.Database.CreateContext())
            {
                Assert.AreEqual(2L, (await verification.LedgerRevisions.SingleAsync()).Value);
            }
        }
    }

    [TestMethod]
    public async Task LateCrossOutcomeSettlementIsFencedWithoutLedgerChange()
    {
        var (fixture, _, userId, operationId, scalarCount) = await AdmitAsync(email: "fence-success@example.test");
        await using (fixture)
        {
            using (var scope = fixture.Factory.Services.CreateScope())
            {
                var settlements = scope.ServiceProvider.GetRequiredService<OperationSettlementService>();
                var settled = await settlements.SettleSuccessAsync(
                    userId, "translation", "Settlement probe text", null, "ru", null, Guid.Parse(operationId), CancellationToken.None);
                Assert.AreEqual(SettlementOutcome.Settled, settled.Outcome);
            }

            SettlementResult late;
            using (var scope = fixture.Factory.Services.CreateScope())
            {
                var settlements = scope.ServiceProvider.GetRequiredService<OperationSettlementService>();
                late = await settlements.SettleFailureAsync(
                    userId, "translation", "Settlement probe text", null, "ru", null, Guid.Parse(operationId), CancellationToken.None);
            }

            Assert.AreEqual(SettlementOutcome.Fenced, late.Outcome);

            await using (var verification = fixture.Database.CreateContext())
            {
                var stored = await verification.OperationSubmissions.SingleAsync();
                Assert.AreEqual(OperationStates.Succeeded, stored.State);
                var userLedger = await verification.CharacterLedgerEntries
                    .SingleAsync(entry => entry.Scope == "user" && entry.Day == "2026-09-11");
                Assert.AreEqual(scalarCount, userLedger.ConsumedCharacters);
                Assert.AreEqual(0, userLedger.ReservedCharacters);
                Assert.AreEqual(2L, (await verification.LedgerRevisions.SingleAsync()).Value);
            }
        }
    }

    [TestMethod]
    public async Task LateSuccessAfterFailureIsFencedWithoutLedgerChange()
    {
        var (fixture, _, userId, operationId, _) = await AdmitAsync(email: "fence-failure@example.test");
        await using (fixture)
        {
            using (var scope = fixture.Factory.Services.CreateScope())
            {
                var settlements = scope.ServiceProvider.GetRequiredService<OperationSettlementService>();
                var settled = await settlements.SettleFailureAsync(
                    userId, "translation", "Settlement probe text", null, "ru", null, Guid.Parse(operationId), CancellationToken.None);
                Assert.AreEqual(SettlementOutcome.Settled, settled.Outcome);
            }

            SettlementResult late;
            using (var scope = fixture.Factory.Services.CreateScope())
            {
                var settlements = scope.ServiceProvider.GetRequiredService<OperationSettlementService>();
                late = await settlements.SettleSuccessAsync(
                    userId, "translation", "Settlement probe text", null, "ru", null, Guid.Parse(operationId), CancellationToken.None);
            }

            Assert.AreEqual(SettlementOutcome.Fenced, late.Outcome);

            await using (var verification = fixture.Database.CreateContext())
            {
                Assert.AreEqual(OperationStates.Failed, (await verification.OperationSubmissions.SingleAsync()).State);
                var userLedger = await verification.CharacterLedgerEntries
                    .SingleAsync(entry => entry.Scope == "user" && entry.Day == "2026-09-11");
                Assert.AreEqual(0, userLedger.ConsumedCharacters);
                Assert.AreEqual(0, userLedger.ReservedCharacters);
                Assert.AreEqual(2L, (await verification.LedgerRevisions.SingleAsync()).Value);
            }
        }
    }

    [TestMethod]
    public async Task ChangedPayloadAgainstSettledIdentityConflicts()
    {
        var (fixture, access, userId, operationId, _) = await AdmitAsync(email: "settled-conflict@example.test");
        await using (fixture)
        {
            using (var scope = fixture.Factory.Services.CreateScope())
            {
                var settlements = scope.ServiceProvider.GetRequiredService<OperationSettlementService>();
                var settled = await settlements.SettleSuccessAsync(
                    userId, "translation", "Settlement probe text", null, "ru", null, Guid.Parse(operationId), CancellationToken.None);
                Assert.AreEqual(SettlementOutcome.Settled, settled.Outcome);
            }

            using (var scope = fixture.Factory.Services.CreateScope())
            {
                var settlements = scope.ServiceProvider.GetRequiredService<OperationSettlementService>();
                var conflict = await settlements.SettleSuccessAsync(
                    userId, "translation", "Settlement probe text!", null, "ru", null, Guid.Parse(operationId), CancellationToken.None);
                Assert.AreEqual(SettlementOutcome.IdentityConflict, conflict.Outcome);
            }

            using var response = await fixture.PostRawAsync(
                "/api/operations",
                JsonSerializer.Serialize(new { operationId, family = "translation", source = "Settlement probe text!", target = "ru" }),
                access);
            var body = await response.Content.ReadAsStringAsync();
            Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode, body);

            using var samePayload = await fixture.PostRawAsync(
                "/api/operations",
                JsonSerializer.Serialize(new { operationId, family = "translation", source = "Settlement probe text", target = "ru" }),
                access);
            Assert.AreEqual(HttpStatusCode.OK, samePayload.StatusCode);
            using var envelope = JsonDocument.Parse(await samePayload.Content.ReadAsStringAsync());
            Assert.AreEqual("succeeded", envelope.RootElement.GetProperty("status").GetString());

            await using (var verification = fixture.Database.CreateContext())
            {
                Assert.AreEqual(2L, (await verification.LedgerRevisions.SingleAsync()).Value);
            }
        }
    }

    [TestMethod]
    public async Task ConcurrentDuplicateSuccessSettlementsCommitExactlyOneCharge()
    {
        var (fixture, _, userId, operationId, scalarCount) = await AdmitAsync(email: "concurrent-settle@example.test");
        await using (fixture)
        {
            var attempts = Enumerable.Range(0, 8)
                .Select(_ => Task.Run(async () =>
                {
                    using var scope = fixture.Factory.Services.CreateScope();
                    var settlements = scope.ServiceProvider.GetRequiredService<OperationSettlementService>();
                    return await settlements.SettleSuccessAsync(
                        userId, "translation", "Settlement probe text", null, "ru", null, Guid.Parse(operationId), CancellationToken.None);
                }))
                .ToArray();
            var results = await Task.WhenAll(attempts);

            foreach (var result in results)
            {
                Assert.AreEqual(OperationStates.Succeeded, result.Submission!.State);
                Assert.AreEqual(scalarCount, result.Submission.ScalarCount);
            }

            await using (var verification = fixture.Database.CreateContext())
            {
                Assert.AreEqual(1, await verification.OperationSubmissions.CountAsync());
                var userLedger = await verification.CharacterLedgerEntries
                    .SingleAsync(entry => entry.Scope == "user" && entry.Day == "2026-09-11");
                Assert.AreEqual(scalarCount, userLedger.ConsumedCharacters);
                Assert.AreEqual(0, userLedger.ReservedCharacters);
                var globalLedger = await verification.CharacterLedgerEntries
                    .SingleAsync(entry => entry.Scope == "global" && entry.Day == "2026-09-11");
                Assert.AreEqual(scalarCount, globalLedger.ConsumedCharacters);
                Assert.AreEqual(2L, (await verification.LedgerRevisions.SingleAsync()).Value);
            }
        }
    }

    [TestMethod]
    public async Task SuccessAfterMidnightChargesTheOriginalAdmissionDay()
    {
        var (fixture, _, userId, operationId, scalarCount) = await AdmitAsync(email: "midnight@example.test");
        await using (fixture)
        {
            fixture.Time.Advance(TimeSpan.FromHours(17));

            SettlementResult settled;
            using (var scope = fixture.Factory.Services.CreateScope())
            {
                var settlements = scope.ServiceProvider.GetRequiredService<OperationSettlementService>();
                settled = await settlements.SettleSuccessAsync(
                    userId, "translation", "Settlement probe text", null, "ru", null, Guid.Parse(operationId), CancellationToken.None);
            }

            Assert.AreEqual(SettlementOutcome.Settled, settled.Outcome);
            Assert.AreEqual("2026-09-12", settled.Usage.Day);

            await using (var verification = fixture.Database.CreateContext())
            {
                var admissionDay = await verification.CharacterLedgerEntries
                    .SingleAsync(entry => entry.Scope == "user" && entry.Day == "2026-09-11");
                Assert.AreEqual(scalarCount, admissionDay.ConsumedCharacters);
                Assert.AreEqual(0, admissionDay.ReservedCharacters);
                var currentDay = await verification.CharacterLedgerEntries
                    .SingleOrDefaultAsync(entry => entry.Scope == "user" && entry.Day == "2026-09-12");
                Assert.IsTrue(currentDay is null || (currentDay.ConsumedCharacters == 0 && currentDay.ReservedCharacters == 0));
                var globalAdmissionDay = await verification.CharacterLedgerEntries
                    .SingleAsync(entry => entry.Scope == "global" && entry.Day == "2026-09-11");
                Assert.AreEqual(scalarCount, globalAdmissionDay.ConsumedCharacters);
            }

            var freshAccess = await fixture.SignInAsync("midnight@example.test");
            using var status = await fixture.GetAsync($"/api/operations/{operationId}", freshAccess);
            Assert.AreEqual(HttpStatusCode.OK, status.StatusCode);
            using var envelope = JsonDocument.Parse(await status.Content.ReadAsStringAsync());
            Assert.AreEqual("succeeded", envelope.RootElement.GetProperty("status").GetString());
            Assert.AreEqual("2026-09-11", envelope.RootElement.GetProperty("admissionDay").GetString());
            Assert.AreEqual("2026-09-12", envelope.RootElement.GetProperty("usage").GetProperty("day").GetString());
        }
    }

    [TestMethod]
    public async Task RestartPreservesSettlementRevisionAndFencing()
    {
        var (fixture, _, userId, operationId, scalarCount) = await AdmitAsync(email: "restart-settle@example.test");
        using (var scope = fixture.Factory.Services.CreateScope())
        {
            var settlements = scope.ServiceProvider.GetRequiredService<OperationSettlementService>();
            var settled = await settlements.SettleSuccessAsync(
                userId, "translation", "Settlement probe text", null, "ru", null, Guid.Parse(operationId), CancellationToken.None);
            Assert.AreEqual(SettlementOutcome.Settled, settled.Outcome);
        }

        await using var restarted = await fixture.RestartAsync();
        var restartedAccess = await restarted.SignInAsync("restart-settle@example.test");

        using var status = await restarted.GetAsync($"/api/operations/{operationId}", restartedAccess);
        Assert.AreEqual(HttpStatusCode.OK, status.StatusCode);
        var statusBody = await status.Content.ReadAsStringAsync();
        StringAssert.Contains(statusBody, "\"status\":\"succeeded\"");
        StringAssert.Contains(statusBody, "\"revision\":2");

        SettlementResult duplicate;
        using (var scope = restarted.Factory.Services.CreateScope())
        {
            var settlements = scope.ServiceProvider.GetRequiredService<OperationSettlementService>();
            duplicate = await settlements.SettleSuccessAsync(
                userId, "translation", "Settlement probe text", null, "ru", null, Guid.Parse(operationId), CancellationToken.None);
        }

        Assert.AreEqual(SettlementOutcome.DuplicateObserved, duplicate.Outcome);

        SettlementResult late;
        using (var scope = restarted.Factory.Services.CreateScope())
        {
            var settlements = scope.ServiceProvider.GetRequiredService<OperationSettlementService>();
            late = await settlements.SettleFailureAsync(
                userId, "translation", "Settlement probe text", null, "ru", null, Guid.Parse(operationId), CancellationToken.None);
        }

        Assert.AreEqual(SettlementOutcome.Fenced, late.Outcome);

        using var reread = await restarted.GetAsync($"/api/operations/{operationId}", restartedAccess);
        Assert.AreEqual(HttpStatusCode.OK, reread.StatusCode);

        await using (var verification = restarted.Database.CreateContext())
        {
            Assert.AreEqual(OperationStates.Succeeded, (await verification.OperationSubmissions.SingleAsync()).State);
            var userLedger = await verification.CharacterLedgerEntries
                .SingleAsync(entry => entry.Scope == "user" && entry.Day == "2026-09-11");
            Assert.AreEqual(scalarCount, userLedger.ConsumedCharacters);
            Assert.AreEqual(0, userLedger.ReservedCharacters);
            Assert.AreEqual(2L, (await verification.LedgerRevisions.SingleAsync()).Value);
        }
    }

    [TestMethod]
    public async Task TerminalStatusReadsReportMetadataOnly()
    {
        var (succeededFixture, _, succeededUser, succeededId, succeededCount) =
            await AdmitAsync(email: "status-succeeded@example.test", source: "Succeeded status text");
        await using (succeededFixture)
        {
            using (var scope = succeededFixture.Factory.Services.CreateScope())
            {
                var settlements = scope.ServiceProvider.GetRequiredService<OperationSettlementService>();
                var settled = await settlements.SettleSuccessAsync(
                    succeededUser, "translation", "Succeeded status text", null, "ru", null, Guid.Parse(succeededId), CancellationToken.None);
                Assert.AreEqual(SettlementOutcome.Settled, settled.Outcome);
            }

            var access = await succeededFixture.SignInAsync("status-succeeded@example.test");
            using var status = await succeededFixture.GetAsync($"/api/operations/{succeededId}", access);
            Assert.AreEqual(HttpStatusCode.OK, status.StatusCode);
            Assert.AreEqual("no-store", status.Headers.CacheControl?.ToString());
            using var envelope = JsonDocument.Parse(await status.Content.ReadAsStringAsync());
            Assert.AreEqual("succeeded", envelope.RootElement.GetProperty("status").GetString());
            Assert.AreEqual(succeededCount, envelope.RootElement.GetProperty("characterCount").GetInt32());
            Assert.AreEqual("2026-09-11", envelope.RootElement.GetProperty("admissionDay").GetString());
            Assert.IsFalse(envelope.RootElement.GetProperty("outputAvailable").GetBoolean());
            Assert.IsTrue(envelope.RootElement.TryGetProperty("usage", out _));

            using var unknown = await succeededFixture.GetAsync($"/api/operations/{OperationIdentity.CreateForTime(succeededFixture.Time.GetUtcNow()):D}", access);
            Assert.AreEqual(HttpStatusCode.NotFound, unknown.StatusCode);

            var revisionBefore = envelope.RootElement.GetProperty("usage").GetProperty("revision").GetInt64();
            using var reread = await succeededFixture.GetAsync($"/api/operations/{succeededId}", access);
            using var rereadEnvelope = JsonDocument.Parse(await reread.Content.ReadAsStringAsync());
            Assert.AreEqual(revisionBefore, rereadEnvelope.RootElement.GetProperty("usage").GetProperty("revision").GetInt64());

            await using (var verification = succeededFixture.Database.CreateContext())
            {
                Assert.AreEqual(2L, (await verification.LedgerRevisions.SingleAsync()).Value);
            }
        }

        var (failedFixture, _, failedUser, failedId, _) =
            await AdmitAsync(email: "status-failed@example.test", source: "Failed status text");
        await using (failedFixture)
        {
            using (var scope = failedFixture.Factory.Services.CreateScope())
            {
                var settlements = scope.ServiceProvider.GetRequiredService<OperationSettlementService>();
                var settled = await settlements.SettleFailureAsync(
                    failedUser, "translation", "Failed status text", null, "ru", null, Guid.Parse(failedId), CancellationToken.None);
                Assert.AreEqual(SettlementOutcome.Settled, settled.Outcome);
            }

            var access = await failedFixture.SignInAsync("status-failed@example.test");
            using var status = await failedFixture.GetAsync($"/api/operations/{failedId}", access);
            Assert.AreEqual(HttpStatusCode.OK, status.StatusCode);
            using var envelope = JsonDocument.Parse(await status.Content.ReadAsStringAsync());
            Assert.AreEqual("failed", envelope.RootElement.GetProperty("status").GetString());
            Assert.AreEqual(0, envelope.RootElement.GetProperty("characterCount").GetInt32());
            Assert.IsTrue(envelope.RootElement.TryGetProperty("usage", out _));
        }
    }

    [TestMethod]
    public async Task SettlementLeavesNoSourceTextInStorage()
    {
        var sourceSentinel = "settle-sentinel-source-" + Guid.NewGuid().ToString("N");
        var (fixture, access, userId, operationId, _) =
            await AdmitAsync(email: "settle-sentinel@example.test", source: sourceSentinel);
        await using (fixture)
        {
            using (var scope = fixture.Factory.Services.CreateScope())
            {
                var settlements = scope.ServiceProvider.GetRequiredService<OperationSettlementService>();
                var settled = await settlements.SettleSuccessAsync(
                    userId, "translation", sourceSentinel, null, "ru", null, Guid.Parse(operationId), CancellationToken.None);
                Assert.AreEqual(SettlementOutcome.Settled, settled.Outcome);
            }

            using var status = await fixture.GetAsync($"/api/operations/{operationId}", access);
            Assert.IsFalse((await status.Content.ReadAsStringAsync()).Contains(sourceSentinel, StringComparison.Ordinal));

            await using (var verification = fixture.Database.CreateContext())
            {
                var tables = await verification.Database.SqlQueryRaw<string>(
                    "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%';").ToListAsync();
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
                        }
                    }
                }
            }
        }
    }

    [TestMethod]
    public async Task UnknownOperationSettlesAsUnknownWithoutWrites()
    {
        await using var fixture = await OperationFixture.CreateAsync();
        var user = await fixture.CreateAccountAsync("unknown-settle@example.test", confirmed: true);
        SettlementResult result;
        using (var scope = fixture.Factory.Services.CreateScope())
        {
            var settlements = scope.ServiceProvider.GetRequiredService<OperationSettlementService>();
            result = await settlements.SettleSuccessAsync(
                user.Id, "translation", "Never admitted", null, "ru", null, Guid.CreateVersion7(), CancellationToken.None);
        }

        Assert.AreEqual(SettlementOutcome.UnknownOperation, result.Outcome);

        await using (var verification = fixture.Database.CreateContext())
        {
            Assert.AreEqual(0, await verification.OperationSubmissions.CountAsync());
            Assert.AreEqual(0, await verification.CharacterLedgerEntries.CountAsync());
            Assert.AreEqual(0L, (await verification.LedgerRevisions.SingleAsync()).Value);
        }
    }
}
