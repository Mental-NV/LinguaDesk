using System.Net;
using System.Text.Json;
using LinguaDesk.Api.Features.Operations;
using LinguaDesk.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LinguaDesk.Api.Tests;

[TestClass]
public sealed class OperationRecoveryTests
{
    private const string Currency = "USD";

    private static async Task<(OperationFixture Fixture, string Access, string UserId, string OperationId, int ScalarCount)> AdmitAsync(
        OperationFixture? fixture = null,
        string email = "recover@example.test",
        string source = "Recovery probe text",
        string family = "translation",
        string? target = "ru")
    {
        var owned = fixture ?? await OperationFixture.CreateAsync();
        var user = await owned.CreateAccountAsync(email, confirmed: true);
        var access = await owned.SignInAsync(email);
        var operationId = Guid.CreateVersion7().ToString("D");
        var body = target is null
            ? JsonSerializer.Serialize(new { operationId, family, source })
            : JsonSerializer.Serialize(new { operationId, family, source, target });
        using var admitted = await owned.PostRawAsync("/api/operations", body, access);
        var admittedBody = await admitted.Content.ReadAsStringAsync();
        Assert.AreEqual(HttpStatusCode.Accepted, admitted.StatusCode, admittedBody);
        var scalarCount = JsonDocument.Parse(admittedBody).RootElement.GetProperty("characterCount").GetInt32();
        return (owned, access, user.Id, operationId, scalarCount);
    }

    private static async Task<RecoveryResult> InterruptAsync(
        OperationFixture fixture,
        string userId,
        string operationId,
        string source = "Recovery probe text",
        string family = "translation",
        string? target = "ru")
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var recovery = scope.ServiceProvider.GetRequiredService<OperationRecoveryService>();
        return await recovery.InterruptAsync(
            userId, family, source, null, target, null, Guid.Parse(operationId), CancellationToken.None);
    }

    private static Dictionary<string, string?> MonetaryConfig(long cap) => new()
    {
        ["MonetaryAdmission:MonthlyCapMinorUnits"] = cap.ToString(System.Globalization.CultureInfo.InvariantCulture),
        ["MonetaryAdmission:Currency"] = Currency,
    };

    private static async Task<string> WaitForStatusAsync(
        OperationFixture fixture,
        string operationId,
        string access,
        string expectedStatus,
        int timeoutMilliseconds = 10000)
    {
        var deadline = DateTimeOffset.UtcNow.AddMilliseconds(timeoutMilliseconds);
        var last = string.Empty;
        while (DateTimeOffset.UtcNow < deadline)
        {
            using var status = await fixture.GetAsync($"/api/operations/{operationId}", access);
            last = await status.Content.ReadAsStringAsync();
            if (status.StatusCode == HttpStatusCode.OK
                && last.Contains($"\"status\":\"{expectedStatus}\"", StringComparison.Ordinal))
            {
                return last;
            }

            await Task.Delay(50);
        }

        return last;
    }

    [TestMethod]
    public async Task InterruptPendingReleasesReservationWithZeroCharge()
    {
        var (fixture, access, userId, operationId, scalarCount) = await AdmitAsync();
        await using (fixture)
        {
            RecoveryResult first;
            using (var scope = fixture.Factory.Services.CreateScope())
            {
                var recovery = scope.ServiceProvider.GetRequiredService<OperationRecoveryService>();
                first = await recovery.InterruptAsync(
                    userId, "translation", "Recovery probe text", null, "ru", null, Guid.Parse(operationId), CancellationToken.None);
            }

            Assert.AreEqual(RecoveryOutcome.Interrupted, first.Outcome);
            Assert.AreEqual(OperationStates.Interrupted, first.Submission!.State);
            Assert.IsNotNull(first.Submission.SettledUtc);

            await using (var verification = fixture.Database.CreateContext())
            {
                var stored = await verification.OperationSubmissions.SingleAsync();
                Assert.AreEqual(OperationStates.Interrupted, stored.State);
                Assert.IsNotNull(stored.SettledUtc);
                Assert.AreEqual("2026-09-11", stored.AdmissionDay);
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

            using var status = await fixture.GetAsync($"/api/operations/{operationId}", access);
            Assert.AreEqual(HttpStatusCode.OK, status.StatusCode);
            using var envelope = JsonDocument.Parse(await status.Content.ReadAsStringAsync());
            Assert.AreEqual("interrupted", envelope.RootElement.GetProperty("status").GetString());
            Assert.AreEqual(0, envelope.RootElement.GetProperty("characterCount").GetInt32());
            Assert.AreEqual("2026-09-11", envelope.RootElement.GetProperty("admissionDay").GetString());
            Assert.IsFalse(envelope.RootElement.GetProperty("outputAvailable").GetBoolean());
            Assert.AreEqual("2026-09-11", envelope.RootElement.GetProperty("usage").GetProperty("day").GetString());
            Assert.AreEqual(2L, envelope.RootElement.GetProperty("usage").GetProperty("revision").GetInt64());

            using var resubmitted = await fixture.PostRawAsync(
                "/api/operations",
                JsonSerializer.Serialize(new { operationId, family = "translation", source = "Recovery probe text", target = "ru" }),
                access);
            var resubmittedBody = await resubmitted.Content.ReadAsStringAsync();
            Assert.AreEqual(HttpStatusCode.OK, resubmitted.StatusCode, resubmittedBody);
            StringAssert.Contains(resubmittedBody, "\"status\":\"interrupted\"");

            await using (var verification = fixture.Database.CreateContext())
            {
                Assert.AreEqual(scalarCount, (await verification.OperationSubmissions.SingleAsync()).ScalarCount);
                Assert.AreEqual(2L, (await verification.LedgerRevisions.SingleAsync()).Value);
            }
        }
    }

    [TestMethod]
    public async Task StartupScanFinalizesDeadlinePassedPendingAfterRestart()
    {
        var (fixture, _, userId, operationId, scalarCount) = await AdmitAsync(email: "restart-orphan@example.test");
        Assert.IsGreaterThan(0, scalarCount);
        Assert.IsNotNull(userId);

        fixture.Time.Advance(TimeSpan.FromSeconds(60));

        await using var restarted = await fixture.RestartAsync();
        var access = await restarted.SignInAsync("restart-orphan@example.test");

        var body = await WaitForStatusAsync(restarted, operationId, access, "interrupted");
        StringAssert.Contains(body, "\"status\":\"interrupted\"");
        StringAssert.Contains(body, "\"characterCount\":0");

        await using (var verification = restarted.Database.CreateContext())
        {
            var stored = await verification.OperationSubmissions.SingleAsync();
            Assert.AreEqual(OperationStates.Interrupted, stored.State);
            Assert.IsNotNull(stored.SettledUtc);
            var userLedger = await verification.CharacterLedgerEntries
                .SingleAsync(entry => entry.Scope == "user" && entry.Day == "2026-09-11");
            Assert.AreEqual(0, userLedger.ConsumedCharacters);
            Assert.AreEqual(0, userLedger.ReservedCharacters);
            Assert.AreEqual(2L, (await verification.LedgerRevisions.SingleAsync()).Value);
        }
    }

    [TestMethod]
    public async Task DeadlineScanLeavesUnexpiredPendingUntouchedAndCommittable()
    {
        var (fixture, _, userId, firstId, _) = await AdmitAsync(
            email: "deadline-first@example.test", source: "First deadline text");
        await using (fixture)
        {
            fixture.Time.Advance(TimeSpan.FromSeconds(60));

            var owned = fixture;
            var user = await owned.CreateAccountAsync("deadline-second@example.test", confirmed: true);
            var secondId = OperationIdentity.CreateForTime(owned.Time.GetUtcNow()).ToString("D");
            using (var admitted = await owned.PostRawAsync(
                "/api/operations",
                JsonSerializer.Serialize(new { operationId = secondId, family = "translation", source = "Second deadline text", target = "ru" }),
                await owned.SignInAsync("deadline-second@example.test")))
            {
                Assert.AreEqual(HttpStatusCode.Accepted, admitted.StatusCode);
            }

            int interrupted;
            using (var scope = fixture.Factory.Services.CreateScope())
            {
                var recovery = scope.ServiceProvider.GetRequiredService<OperationRecoveryService>();
                interrupted = await recovery.ReconcileOrphansAsync(CancellationToken.None);
            }

            Assert.AreEqual(1, interrupted);

            await using (var verification = fixture.Database.CreateContext())
            {
                var states = await verification.OperationSubmissions.ToDictionaryAsync(
                    submission => submission.OperationId, submission => submission.State);
                Assert.AreEqual(OperationStates.Interrupted, states[firstId]);
                Assert.AreEqual(OperationStates.Pending, states[secondId]);
            }

            SettlementResult settled;
            using (var scope = fixture.Factory.Services.CreateScope())
            {
                var settlements = scope.ServiceProvider.GetRequiredService<OperationSettlementService>();
                settled = await settlements.SettleSuccessAsync(
                    user.Id, "translation", "Second deadline text", null, "ru", null, Guid.Parse(secondId), CancellationToken.None);
            }

            Assert.AreEqual(SettlementOutcome.Settled, settled.Outcome);

            var access = await fixture.SignInAsync("deadline-first@example.test");
            using var status = await fixture.GetAsync($"/api/operations/{firstId}", access);
            Assert.AreEqual(HttpStatusCode.OK, status.StatusCode);
            StringAssert.Contains(await status.Content.ReadAsStringAsync(), "\"status\":\"interrupted\"");
        }
    }

    [TestMethod]
    public async Task LateSettlementAfterInterruptIsFencedAndRepeatIsIdempotent()
    {
        var (fixture, access, userId, operationId, scalarCount) = await AdmitAsync(email: "fence-after-interrupt@example.test");
        await using (fixture)
        {
            var interrupted = await InterruptAsync(fixture, userId, operationId);
            Assert.AreEqual(RecoveryOutcome.Interrupted, interrupted.Outcome);

            SettlementResult lateSuccess;
            using (var scope = fixture.Factory.Services.CreateScope())
            {
                var settlements = scope.ServiceProvider.GetRequiredService<OperationSettlementService>();
                lateSuccess = await settlements.SettleSuccessAsync(
                    userId, "translation", "Recovery probe text", null, "ru", null, Guid.Parse(operationId), CancellationToken.None);
            }

            Assert.AreEqual(SettlementOutcome.Fenced, lateSuccess.Outcome);

            SettlementResult lateFailure;
            using (var scope = fixture.Factory.Services.CreateScope())
            {
                var settlements = scope.ServiceProvider.GetRequiredService<OperationSettlementService>();
                lateFailure = await settlements.SettleFailureAsync(
                    userId, "translation", "Recovery probe text", null, "ru", null, Guid.Parse(operationId), CancellationToken.None);
            }

            Assert.AreEqual(SettlementOutcome.Fenced, lateFailure.Outcome);

            var duplicate = await InterruptAsync(fixture, userId, operationId);
            Assert.AreEqual(RecoveryOutcome.DuplicateObserved, duplicate.Outcome);
            Assert.AreEqual(OperationStates.Interrupted, duplicate.Submission!.State);

            await using (var verification = fixture.Database.CreateContext())
            {
                var stored = await verification.OperationSubmissions.SingleAsync();
                Assert.AreEqual(OperationStates.Interrupted, stored.State);
                Assert.AreEqual(scalarCount, stored.ScalarCount);
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

            using var status = await fixture.GetAsync($"/api/operations/{operationId}", access);
            Assert.AreEqual(HttpStatusCode.OK, status.StatusCode);
            StringAssert.Contains(await status.Content.ReadAsStringAsync(), "\"status\":\"interrupted\"");
        }
    }

    [TestMethod]
    public async Task ChangedPayloadAgainstInterruptedIdentityConflicts()
    {
        var (fixture, access, userId, operationId, _) = await AdmitAsync(email: "interrupt-conflict@example.test");
        await using (fixture)
        {
            var interrupted = await InterruptAsync(fixture, userId, operationId);
            Assert.AreEqual(RecoveryOutcome.Interrupted, interrupted.Outcome);

            using var scope = fixture.Factory.Services.CreateScope();
            var recovery = scope.ServiceProvider.GetRequiredService<OperationRecoveryService>();
            var conflict = await recovery.InterruptAsync(
                userId, "translation", "Recovery probe text!", null, "ru", null, Guid.Parse(operationId), CancellationToken.None);
            Assert.AreEqual(RecoveryOutcome.IdentityConflict, conflict.Outcome);

            using var response = await fixture.PostRawAsync(
                "/api/operations",
                JsonSerializer.Serialize(new { operationId, family = "translation", source = "Recovery probe text!", target = "ru" }),
                access);
            var body = await response.Content.ReadAsStringAsync();
            Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode, body);

            await using (var verification = fixture.Database.CreateContext())
            {
                Assert.AreEqual(OperationStates.Interrupted, (await verification.OperationSubmissions.SingleAsync()).State);
                Assert.AreEqual(2L, (await verification.LedgerRevisions.SingleAsync()).Value);
            }
        }
    }

    [TestMethod]
    public async Task InterruptAfterSuccessIsFencedWithChargeIntact()
    {
        var (fixture, access, userId, operationId, scalarCount) = await AdmitAsync(email: "interrupt-after-success@example.test");
        await using (fixture)
        {
            using (var scope = fixture.Factory.Services.CreateScope())
            {
                var settlements = scope.ServiceProvider.GetRequiredService<OperationSettlementService>();
                var settled = await settlements.SettleSuccessAsync(
                    userId, "translation", "Recovery probe text", null, "ru", null, Guid.Parse(operationId), CancellationToken.None);
                Assert.AreEqual(SettlementOutcome.Settled, settled.Outcome);
            }

            var fenced = await InterruptAsync(fixture, userId, operationId);
            Assert.AreEqual(RecoveryOutcome.Fenced, fenced.Outcome);
            Assert.AreEqual(OperationStates.Succeeded, fenced.Submission!.State);

            await using (var verification = fixture.Database.CreateContext())
            {
                Assert.AreEqual(OperationStates.Succeeded, (await verification.OperationSubmissions.SingleAsync()).State);
                var userLedger = await verification.CharacterLedgerEntries
                    .SingleAsync(entry => entry.Scope == "user" && entry.Day == "2026-09-11");
                Assert.AreEqual(scalarCount, userLedger.ConsumedCharacters);
                Assert.AreEqual(0, userLedger.ReservedCharacters);
                Assert.AreEqual(2L, (await verification.LedgerRevisions.SingleAsync()).Value);
            }

            using var status = await fixture.GetAsync($"/api/operations/{operationId}", access);
            Assert.AreEqual(HttpStatusCode.OK, status.StatusCode);
            var statusBody = await status.Content.ReadAsStringAsync();
            StringAssert.Contains(statusBody, "\"status\":\"succeeded\"");
            StringAssert.Contains(statusBody, $"\"characterCount\":{scalarCount}");
        }
    }

    [TestMethod]
    public async Task AbortedWaitMayStillCommitSuccessWithOutputUnavailable()
    {
        var (fixture, access, userId, operationId, scalarCount) = await AdmitAsync(email: "abort-then-success@example.test");
        await using (fixture)
        {
            using (var scope = fixture.Factory.Services.CreateScope())
            {
                var settlements = scope.ServiceProvider.GetRequiredService<OperationSettlementService>();
                var settled = await settlements.SettleSuccessAsync(
                    userId, "translation", "Recovery probe text", null, "ru", null, Guid.Parse(operationId), CancellationToken.None);
                Assert.AreEqual(SettlementOutcome.Settled, settled.Outcome);
            }

            using var status = await fixture.GetAsync($"/api/operations/{operationId}", access);
            Assert.AreEqual(HttpStatusCode.OK, status.StatusCode);
            using var envelope = JsonDocument.Parse(await status.Content.ReadAsStringAsync());
            Assert.AreEqual("succeeded", envelope.RootElement.GetProperty("status").GetString());
            Assert.AreEqual(scalarCount, envelope.RootElement.GetProperty("characterCount").GetInt32());
            Assert.IsFalse(envelope.RootElement.GetProperty("outputAvailable").GetBoolean());
        }
    }

    [TestMethod]
    public async Task LateOutputNeverRevivesTerminalFailedRecords()
    {
        var (fixture, _, userId, operationId, _) = await AdmitAsync(email: "revive-failed@example.test");
        await using (fixture)
        {
            using (var scope = fixture.Factory.Services.CreateScope())
            {
                var settlements = scope.ServiceProvider.GetRequiredService<OperationSettlementService>();
                var settled = await settlements.SettleFailureAsync(
                    userId, "translation", "Recovery probe text", null, "ru", null, Guid.Parse(operationId), CancellationToken.None);
                Assert.AreEqual(SettlementOutcome.Settled, settled.Outcome);
            }

            var interrupt = await InterruptAsync(fixture, userId, operationId);
            Assert.AreEqual(RecoveryOutcome.Fenced, interrupt.Outcome);

            SettlementResult late;
            using (var scope = fixture.Factory.Services.CreateScope())
            {
                var settlements = scope.ServiceProvider.GetRequiredService<OperationSettlementService>();
                late = await settlements.SettleSuccessAsync(
                    userId, "translation", "Recovery probe text", null, "ru", null, Guid.Parse(operationId), CancellationToken.None);
            }

            Assert.AreEqual(SettlementOutcome.Fenced, late.Outcome);

            await using (var verification = fixture.Database.CreateContext())
            {
                Assert.AreEqual(OperationStates.Failed, (await verification.OperationSubmissions.SingleAsync()).State);
                Assert.AreEqual(2L, (await verification.LedgerRevisions.SingleAsync()).Value);
            }
        }
    }

    [TestMethod]
    public async Task UnknownExpiredAndMalformedStatusReads()
    {
        await using var fixture = await OperationFixture.CreateAsync();
        var user = await fixture.CreateAccountAsync("unknown-status@example.test", confirmed: true);
        var access = await fixture.SignInAsync("unknown-status@example.test");

        using (var unknown = await fixture.GetAsync($"/api/operations/{Guid.CreateVersion7():D}", access))
        {
            var body = await unknown.Content.ReadAsStringAsync();
            Assert.AreEqual(HttpStatusCode.NotFound, unknown.StatusCode, body);
            StringAssert.Contains(body, "unknownOperation");
            Assert.IsFalse(body.Contains("zero charge", StringComparison.OrdinalIgnoreCase), body);
        }

        var expiredId = OperationIdentity.CreateForTime(fixture.Time.GetUtcNow() - TimeSpan.FromHours(25)).ToString("D");
        using (var expired = await fixture.GetAsync($"/api/operations/{expiredId}", access))
        {
            var body = await expired.Content.ReadAsStringAsync();
            Assert.AreEqual(HttpStatusCode.Gone, expired.StatusCode, body);
            StringAssert.Contains(body, "identityExpired");
        }

        using (var malformed = await fixture.GetAsync("/api/operations/not-a-guid", access))
        {
            var body = await malformed.Content.ReadAsStringAsync();
            Assert.AreEqual(HttpStatusCode.BadRequest, malformed.StatusCode, body);
            StringAssert.Contains(body, "invalidIdentity");
            StringAssert.Contains(body, "serverTimeUtc");
        }

        var futureId = OperationIdentity.CreateForTime(fixture.Time.GetUtcNow() + TimeSpan.FromMinutes(10)).ToString("D");
        using (var future = await fixture.GetAsync($"/api/operations/{futureId}", access))
        {
            Assert.AreEqual(HttpStatusCode.BadRequest, future.StatusCode);
        }

        await using (var verification = fixture.Database.CreateContext())
        {
            Assert.AreEqual(0, await verification.OperationSubmissions.CountAsync());
            Assert.AreEqual(0, await verification.CharacterLedgerEntries.CountAsync());
            Assert.AreEqual(0L, (await verification.LedgerRevisions.SingleAsync()).Value);
        }

        Assert.IsNotNull(user.Id);
    }

    [TestMethod]
    public async Task InterruptedRetainsMonetaryExposureUntilOriginalMonthEvidence()
    {
        await using var fixture = await OperationFixture.CreateAsync(MonetaryConfig(100000));
        var user = await fixture.CreateAccountAsync("exposure-interrupt@example.test", confirmed: true);
        var access = await fixture.SignInAsync("exposure-interrupt@example.test");
        var operationId = Guid.CreateVersion7().ToString("D");
        using (var admitted = await fixture.PostRawAsync(
            "/api/operations",
            JsonSerializer.Serialize(new { operationId, family = "translation", source = "Exposure probe text", target = "ru" }),
            access))
        {
            Assert.AreEqual(HttpStatusCode.Accepted, admitted.StatusCode);
        }

        var attemptId = Guid.CreateVersion7().ToString("D");
        MonetaryAdmissionResult reservation;
        using (var scope = fixture.Factory.Services.CreateScope())
        {
            var monetary = scope.ServiceProvider.GetRequiredService<MonetaryAdmissionService>();
            reservation = await monetary.AdmitAsync(
                attemptId,
                operationId,
                1,
                new AttemptCostProfile(Currency, "test-tariff", "2026-09-01", 1_000_000m, 1_000_000m, 100, 50, 0),
                CancellationToken.None);
        }

        Assert.AreEqual(MonetaryAdmissionOutcome.Admitted, reservation.Outcome);
        Assert.AreEqual(150, reservation.BoundMinorUnits);
        Assert.AreEqual("2026-09", reservation.Reservation!.CostMonth);

        var interrupted = await InterruptAsync(fixture, user.Id, operationId, source: "Exposure probe text");
        Assert.AreEqual(RecoveryOutcome.Interrupted, interrupted.Outcome);

        await using (var verification = fixture.Database.CreateContext())
        {
            var held = await verification.MonetaryAttemptReservations.SingleAsync();
            Assert.AreEqual(MonetaryReservationStates.Reserved, held.State);
            Assert.AreEqual("2026-09", held.CostMonth);
            var ledger = await verification.MonetaryCostLedgers.SingleAsync();
            Assert.AreEqual(0, ledger.KnownSpendMinorUnits);
            Assert.AreEqual(150, ledger.UnresolvedExposureMinorUnits);
            var userLedger = await verification.CharacterLedgerEntries
                .SingleAsync(entry => entry.Scope == "user" && entry.Day == "2026-09-11");
            Assert.AreEqual(0, userLedger.ConsumedCharacters);
            Assert.AreEqual(0, userLedger.ReservedCharacters);
        }

        fixture.Time.Advance(TimeSpan.FromDays(30));

        ReconciliationResult settled;
        using (var scope = fixture.Factory.Services.CreateScope())
        {
            var monetary = scope.ServiceProvider.GetRequiredService<MonetaryAdmissionService>();
            settled = await monetary.ReconcileAsync(
                attemptId, new ReconciliationEvidence(120, false, "invoice-evidence"), CancellationToken.None);
        }

        Assert.AreEqual(ReconciliationOutcome.Settled, settled.Outcome);

        ReconciliationResult duplicate;
        using (var scope = fixture.Factory.Services.CreateScope())
        {
            var monetary = scope.ServiceProvider.GetRequiredService<MonetaryAdmissionService>();
            duplicate = await monetary.ReconcileAsync(
                attemptId, new ReconciliationEvidence(120, false, "invoice-evidence"), CancellationToken.None);
        }

        Assert.AreEqual(ReconciliationOutcome.DuplicateObserved, duplicate.Outcome);

        await using (var verification = fixture.Database.CreateContext())
        {
            Assert.AreEqual(1, await verification.MonetaryCostLedgers.CountAsync());
            var ledger = await verification.MonetaryCostLedgers.SingleAsync();
            Assert.AreEqual("2026-09", ledger.CostMonth);
            Assert.AreEqual(120, ledger.KnownSpendMinorUnits);
            Assert.AreEqual(0, ledger.UnresolvedExposureMinorUnits);
            Assert.AreEqual(OperationStates.Interrupted, (await verification.OperationSubmissions.SingleAsync()).State);
            var userLedger = await verification.CharacterLedgerEntries
                .SingleAsync(entry => entry.Scope == "user" && entry.Day == "2026-09-11");
            Assert.AreEqual(0, userLedger.ConsumedCharacters);
            Assert.AreEqual(0, userLedger.ReservedCharacters);
        }
    }

    [TestMethod]
    public async Task RestartPreservesInterruptedStateLedgersAndFencing()
    {
        var (fixture, _, userId, operationId, scalarCount) = await AdmitAsync(email: "restart-interrupted@example.test");
        var interrupted = await InterruptAsync(fixture, userId, operationId);
        Assert.AreEqual(RecoveryOutcome.Interrupted, interrupted.Outcome);

        await using var restarted = await fixture.RestartAsync();
        var access = await restarted.SignInAsync("restart-interrupted@example.test");

        using (var status = await restarted.GetAsync($"/api/operations/{operationId}", access))
        {
            Assert.AreEqual(HttpStatusCode.OK, status.StatusCode);
            var body = await status.Content.ReadAsStringAsync();
            StringAssert.Contains(body, "\"status\":\"interrupted\"");
            StringAssert.Contains(body, "\"characterCount\":0");
            StringAssert.Contains(body, "\"revision\":2");
        }

        RecoveryResult duplicate;
        using (var scope = restarted.Factory.Services.CreateScope())
        {
            var recovery = scope.ServiceProvider.GetRequiredService<OperationRecoveryService>();
            duplicate = await recovery.InterruptAsync(
                userId, "translation", "Recovery probe text", null, "ru", null, Guid.Parse(operationId), CancellationToken.None);
        }

        Assert.AreEqual(RecoveryOutcome.DuplicateObserved, duplicate.Outcome);

        SettlementResult late;
        using (var scope = restarted.Factory.Services.CreateScope())
        {
            var settlements = scope.ServiceProvider.GetRequiredService<OperationSettlementService>();
            late = await settlements.SettleSuccessAsync(
                userId, "translation", "Recovery probe text", null, "ru", null, Guid.Parse(operationId), CancellationToken.None);
        }

        Assert.AreEqual(SettlementOutcome.Fenced, late.Outcome);

        using (var reread = await restarted.GetAsync($"/api/operations/{operationId}", access))
        {
            Assert.AreEqual(HttpStatusCode.OK, reread.StatusCode);
            using var envelope = JsonDocument.Parse(await reread.Content.ReadAsStringAsync());
            Assert.AreEqual(2L, envelope.RootElement.GetProperty("usage").GetProperty("revision").GetInt64());
        }

        await using (var verification = restarted.Database.CreateContext())
        {
            Assert.AreEqual(OperationStates.Interrupted, (await verification.OperationSubmissions.SingleAsync()).State);
            Assert.AreEqual(scalarCount, (await verification.OperationSubmissions.SingleAsync()).ScalarCount);
            var userLedger = await verification.CharacterLedgerEntries
                .SingleAsync(entry => entry.Scope == "user" && entry.Day == "2026-09-11");
            Assert.AreEqual(0, userLedger.ConsumedCharacters);
            Assert.AreEqual(0, userLedger.ReservedCharacters);
            Assert.AreEqual(2L, (await verification.LedgerRevisions.SingleAsync()).Value);
        }
    }

    [TestMethod]
    public async Task InterruptedReadsEnforceOwnershipAndVerification()
    {
        var (fixture, _, userId, operationId, _) = await AdmitAsync(email: "interrupt-owner@example.test");
        await using (fixture)
        {
            var interrupted = await InterruptAsync(fixture, userId, operationId);
            Assert.AreEqual(RecoveryOutcome.Interrupted, interrupted.Outcome);

            await fixture.CreateAccountAsync("interrupt-stranger@example.test", confirmed: true);
            var strangerAccess = await fixture.SignInAsync("interrupt-stranger@example.test");
            using (var foreign = await fixture.GetAsync($"/api/operations/{operationId}", strangerAccess))
            {
                Assert.AreEqual(HttpStatusCode.NotFound, foreign.StatusCode);
            }

            using (var anonymous = await fixture.GetAsync($"/api/operations/{operationId}", null))
            {
                Assert.AreEqual(HttpStatusCode.Unauthorized, anonymous.StatusCode);
            }

            await fixture.CreateAccountAsync("interrupt-unverified@example.test", confirmed: false);
            var unverifiedAccess = await fixture.SignInAsync("interrupt-unverified@example.test");
            using (var unverified = await fixture.GetAsync($"/api/operations/{operationId}", unverifiedAccess))
            {
                Assert.AreEqual(HttpStatusCode.Forbidden, unverified.StatusCode);
            }
        }
    }

    [TestMethod]
    public async Task ConcurrentInterruptAndSuccessCommitAtMostOnce()
    {
        var (fixture, _, userId, operationId, scalarCount) = await AdmitAsync(email: "concurrent-interrupt@example.test");
        await using (fixture)
        {
            var attempts = Enumerable.Range(0, 8)
                .Select(index => Task.Run(async () =>
                {
                    using var scope = fixture.Factory.Services.CreateScope();
                    if (index % 2 == 0)
                    {
                        var recovery = scope.ServiceProvider.GetRequiredService<OperationRecoveryService>();
                        return (object)await recovery.InterruptAsync(
                            userId, "translation", "Recovery probe text", null, "ru", null, Guid.Parse(operationId), CancellationToken.None);
                    }

                    var settlements = scope.ServiceProvider.GetRequiredService<OperationSettlementService>();
                    return (object)await settlements.SettleSuccessAsync(
                        userId, "translation", "Recovery probe text", null, "ru", null, Guid.Parse(operationId), CancellationToken.None);
                }))
                .ToArray();
            var results = await Task.WhenAll(attempts);

            var won = results.Count(result =>
                (result is RecoveryResult recovery && recovery.Outcome == RecoveryOutcome.Interrupted)
                || (result is SettlementResult settlement && settlement.Outcome == SettlementOutcome.Settled));
            Assert.AreEqual(1, won);

            await using (var verification = fixture.Database.CreateContext())
            {
                var stored = await verification.OperationSubmissions.SingleAsync();
                var userLedger = await verification.CharacterLedgerEntries
                    .SingleAsync(entry => entry.Scope == "user" && entry.Day == "2026-09-11");
                var globalLedger = await verification.CharacterLedgerEntries
                    .SingleAsync(entry => entry.Scope == "global" && entry.Day == "2026-09-11");
                if (string.Equals(stored.State, OperationStates.Succeeded, StringComparison.Ordinal))
                {
                    Assert.AreEqual(scalarCount, userLedger.ConsumedCharacters);
                    Assert.AreEqual(0, userLedger.ReservedCharacters);
                    Assert.AreEqual(scalarCount, globalLedger.ConsumedCharacters);
                    Assert.AreEqual(0, globalLedger.ReservedCharacters);
                }
                else
                {
                    Assert.AreEqual(OperationStates.Interrupted, stored.State);
                    Assert.AreEqual(0, userLedger.ConsumedCharacters);
                    Assert.AreEqual(0, userLedger.ReservedCharacters);
                    Assert.AreEqual(0, globalLedger.ConsumedCharacters);
                    Assert.AreEqual(0, globalLedger.ReservedCharacters);
                }

                Assert.AreEqual(2L, (await verification.LedgerRevisions.SingleAsync()).Value);
            }
        }
    }

    [TestMethod]
    public async Task LogsAndTablesCarryNoSourceText()
    {
        await using var fixture = await OperationFixture.CreateAsync();
        var user = await fixture.CreateAccountAsync("interrupt-sentinel@example.test", confirmed: true);
        var access = await fixture.SignInAsync("interrupt-sentinel@example.test");
        var sourceSentinel = "interrupt-sentinel-source-" + Guid.NewGuid().ToString("N");
        var operationId = Guid.CreateVersion7().ToString("D");
        using (var admitted = await fixture.PostRawAsync(
            "/api/operations",
            JsonSerializer.Serialize(new { operationId, family = "translation", source = sourceSentinel, target = "ru" }),
            access))
        {
            Assert.AreEqual(HttpStatusCode.Accepted, admitted.StatusCode);
        }

        var logger = new CapturingRecoveryLogger();
        var options = Options.Create(new OperationAdmissionOptions
        {
            UserDailyAllowanceCharacters = 100000,
            GlobalDailyAllowanceCharacters = 1000000,
        });
        await using (var context = fixture.Database.CreateContext())
        {
            OperationFingerprintKeyProvider keys;
            using (var scope = fixture.Factory.Services.CreateScope())
            {
                keys = scope.ServiceProvider.GetRequiredService<OperationFingerprintKeyProvider>();
            }

            var service = new OperationRecoveryService(context, fixture.Time, options, keys, logger);
            var interrupted = await service.InterruptAsync(
                user.Id, "translation", sourceSentinel, null, "ru", null, Guid.Parse(operationId), CancellationToken.None);
            Assert.AreEqual(RecoveryOutcome.Interrupted, interrupted.Outcome);

            var duplicate = await service.InterruptAsync(
                user.Id, "translation", sourceSentinel, null, "ru", null, Guid.Parse(operationId), CancellationToken.None);
            Assert.AreEqual(RecoveryOutcome.DuplicateObserved, duplicate.Outcome);
        }

        foreach (var message in logger.Messages)
        {
            Assert.IsFalse(message.Contains(sourceSentinel, StringComparison.Ordinal), message);
        }

        using (var status = await fixture.GetAsync($"/api/operations/{operationId}", access))
        {
            Assert.IsFalse((await status.Content.ReadAsStringAsync()).Contains(sourceSentinel, StringComparison.Ordinal));
        }

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

    [TestMethod]
    public void RecoveryServiceHasNoDispatchDependencies()
    {
        var parameters = typeof(OperationRecoveryService).GetConstructors().Single().GetParameters();
        CollectionAssert.AreEquivalent(
            new[]
            {
                typeof(LinguaDeskDbContext),
                typeof(TimeProvider),
                typeof(IOptions<OperationAdmissionOptions>),
                typeof(OperationFingerprintKeyProvider),
                typeof(ILogger<OperationRecoveryService>),
            },
            parameters.Select(parameter => parameter.ParameterType).ToArray());
    }

    private sealed class CapturingRecoveryLogger : ILogger<OperationRecoveryService>
    {
        private readonly List<string> messages = new();

        public IReadOnlyList<string> Messages
        {
            get
            {
                lock (messages)
                {
                    return messages.ToArray();
                }
            }
        }

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull => new CapturingScope();

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            lock (messages)
            {
                messages.Add(formatter(state, exception));
            }
        }

        private sealed class CapturingScope : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}
