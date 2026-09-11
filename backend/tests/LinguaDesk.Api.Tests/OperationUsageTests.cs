using System.Net;
using System.Text.Json;
using LinguaDesk.Api.Features.Operations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LinguaDesk.Api.Tests;

[TestClass]
public sealed class OperationUsageTests
{
    private static Dictionary<string, string?> MonetaryConfig(long cap) => new()
    {
        ["MonetaryAdmission:MonthlyCapMinorUnits"] = cap.ToString(System.Globalization.CultureInfo.InvariantCulture),
        ["MonetaryAdmission:Currency"] = "USD",
    };

    private static AttemptCostProfile Cost(int maxInputTokens, int maxOutputTokens) =>
        new("USD", "test-tariff", "2026-09-01", 1_000_000m, 1_000_000m, maxInputTokens, maxOutputTokens, 0);

    private static string NewIdentity(OperationFixture fixture) =>
        OperationIdentity.CreateForTime(fixture.Time.GetUtcNow()).ToString("D");

    private static string DayString(DateTimeOffset value) =>
        value.UtcDateTime.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

    private static async Task<(string OperationId, int ScalarCount, string Body)> SubmitAsync(
        OperationFixture fixture,
        string access,
        string source,
        string? operationId = null,
        string family = "translation",
        string? target = "ru")
    {
        operationId ??= NewIdentity(fixture);
        var body = target is null
            ? JsonSerializer.Serialize(new { operationId, family, source })
            : JsonSerializer.Serialize(new { operationId, family, source, target });
        using var response = await fixture.PostRawAsync("/api/operations", body, access);
        var responseBody = await response.Content.ReadAsStringAsync();
        Assert.AreEqual(HttpStatusCode.Accepted, response.StatusCode, responseBody);
        var scalarCount = JsonDocument.Parse(responseBody).RootElement.GetProperty("characterCount").GetInt32();
        return (operationId, scalarCount, responseBody);
    }

    private static async Task<JsonElement> GetUsageAsync(OperationFixture fixture, string access)
    {
        using var response = await fixture.GetAsync("/api/usage", access);
        var body = await response.Content.ReadAsStringAsync();
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, body);
        Assert.AreEqual("no-store", response.Headers.CacheControl?.ToString());
        using var document = JsonDocument.Parse(body);
        return document.RootElement.Clone();
    }

    private static void AssertSnapshotShape(JsonElement usage, int allowance, string day)
    {
        Assert.AreEqual(day, usage.GetProperty("day").GetString());
        var consumed = usage.GetProperty("consumedCharacters").GetInt32();
        var reserved = usage.GetProperty("reservedCharacters").GetInt32();
        Assert.AreEqual(allowance, usage.GetProperty("allowanceCharacters").GetInt32());
        Assert.AreEqual(Math.Max(0, allowance - consumed - reserved), usage.GetProperty("availableCharacters").GetInt32());
        Assert.IsTrue(usage.TryGetProperty("resetAtUtc", out _));
        Assert.IsTrue(usage.TryGetProperty("revision", out _));
        Assert.IsTrue(usage.TryGetProperty("availability", out _));
    }

    private static int CompareSnapshots(
        string currentAccount,
        string currentDay,
        long currentRevision,
        string incomingAccount,
        string incomingDay,
        long incomingRevision)
    {
        if (!string.Equals(currentAccount, incomingAccount, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Usage snapshots are never compared across accounts.");
        }

        var dayOrder = string.CompareOrdinal(incomingDay, currentDay);
        return dayOrder != 0 ? dayOrder : incomingRevision.CompareTo(currentRevision);
    }

    [TestMethod]
    public async Task UsageReadAndOperationResponsesCarryAuthoritativeSnapshot()
    {
        await using var fixture = await OperationFixture.CreateAsync();
        await fixture.CreateAccountAsync("usage@example.test", confirmed: true);
        var access = await fixture.SignInAsync("usage@example.test");

        var (operationId, scalarCount, submitBody) = await SubmitAsync(fixture, access, "Usage probe text");
        using (var submitDocument = JsonDocument.Parse(submitBody))
        {
            var submitUsage = submitDocument.RootElement.GetProperty("usage");
            AssertSnapshotShape(submitUsage, 20000, "2026-09-11");
            Assert.AreEqual("available", submitUsage.GetProperty("availability").GetString());
            Assert.AreEqual(scalarCount, submitUsage.GetProperty("reservedCharacters").GetInt32());
        }

        var usage = await GetUsageAsync(fixture, access);
        AssertSnapshotShape(usage, 20000, "2026-09-11");
        Assert.AreEqual("available", usage.GetProperty("availability").GetString());
        Assert.AreEqual(scalarCount, usage.GetProperty("reservedCharacters").GetInt32());
        Assert.AreEqual(1L, usage.GetProperty("revision").GetInt64());

        using var status = await fixture.GetAsync($"/api/operations/{operationId}", access);
        Assert.AreEqual(HttpStatusCode.OK, status.StatusCode);
        Assert.AreEqual("no-store", status.Headers.CacheControl?.ToString());
        var statusBody = await status.Content.ReadAsStringAsync();
        using (var statusDocument = JsonDocument.Parse(statusBody))
        {
            var statusUsage = statusDocument.RootElement.GetProperty("usage");
            AssertSnapshotShape(statusUsage, 20000, "2026-09-11");
            Assert.AreEqual("available", statusUsage.GetProperty("availability").GetString());
        }

        foreach (var body in new[] { submitBody, statusBody, usage.GetRawText() })
        {
            Assert.IsFalse(body.Contains("global", StringComparison.OrdinalIgnoreCase), body);
            Assert.IsFalse(body.Contains("MinorUnits", StringComparison.Ordinal), body);
            Assert.IsFalse(body.Contains("KnownSpend", StringComparison.Ordinal), body);
            Assert.IsFalse(body.Contains("UnresolvedExposure", StringComparison.Ordinal), body);
            Assert.IsFalse(body.Contains("Usage probe text", StringComparison.Ordinal), body);
        }
    }

    [TestMethod]
    public async Task AvailabilityMatrixReportsEachExhaustionStateWithDenialPairing()
    {
        await using var userExhausted = await OperationFixture.CreateAsync(new Dictionary<string, string?>
        {
            ["Operations:UserDailyAllowanceCharacters"] = "100",
            ["Operations:GlobalDailyAllowanceCharacters"] = "1000000",
        });
        await userExhausted.CreateAccountAsync("user-cap@example.test", confirmed: true);
        var userAccess = await userExhausted.SignInAsync("user-cap@example.test");
        await SubmitAsync(userExhausted, userAccess, new string('a', 60));
        await SubmitAsync(userExhausted, userAccess, new string('b', 40));
        var userUsage = await GetUsageAsync(userExhausted, userAccess);
        Assert.AreEqual("user-exhausted", userUsage.GetProperty("availability").GetString());
        Assert.AreEqual(0, userUsage.GetProperty("availableCharacters").GetInt32());
        using (var denied = await userExhausted.PostRawAsync(
            "/api/operations",
            JsonSerializer.Serialize(new { operationId = NewIdentity(userExhausted), family = "translation", source = "c", target = "ru" }),
            userAccess))
        {
            var deniedBody = await denied.Content.ReadAsStringAsync();
            Assert.AreEqual((HttpStatusCode)429, denied.StatusCode, deniedBody);
            Assert.AreEqual("userAllowance", JsonDocument.Parse(deniedBody).RootElement.GetProperty("category").GetString());
        }

        await using var globallyExhausted = await OperationFixture.CreateAsync(new Dictionary<string, string?>
        {
            ["Operations:UserDailyAllowanceCharacters"] = "1000000",
            ["Operations:GlobalDailyAllowanceCharacters"] = "100",
        });
        await globallyExhausted.CreateAccountAsync("global-a@example.test", confirmed: true);
        await globallyExhausted.CreateAccountAsync("global-b@example.test", confirmed: true);
        var firstAccess = await globallyExhausted.SignInAsync("global-a@example.test");
        var secondAccess = await globallyExhausted.SignInAsync("global-b@example.test");
        await SubmitAsync(globallyExhausted, firstAccess, new string('a', 60));
        await SubmitAsync(globallyExhausted, secondAccess, new string('b', 40));
        var globalUsage = await GetUsageAsync(globallyExhausted, firstAccess);
        Assert.AreEqual("service-exhausted", globalUsage.GetProperty("availability").GetString());
        await globallyExhausted.CreateAccountAsync("global-c@example.test", confirmed: true);
        var thirdAccess = await globallyExhausted.SignInAsync("global-c@example.test");
        using (var denied = await globallyExhausted.PostRawAsync(
            "/api/operations",
            JsonSerializer.Serialize(new { operationId = NewIdentity(globallyExhausted), family = "translation", source = "c", target = "ru" }),
            thirdAccess))
        {
            var deniedBody = await denied.Content.ReadAsStringAsync();
            Assert.AreEqual((HttpStatusCode)429, denied.StatusCode, deniedBody);
            Assert.AreEqual("globalAllowance", JsonDocument.Parse(deniedBody).RootElement.GetProperty("category").GetString());
        }

        await using var suspended = await OperationFixture.CreateAsync(MonetaryConfig(1000));
        await suspended.CreateAccountAsync("suspended@example.test", confirmed: true);
        var suspendedAccess = await suspended.SignInAsync("suspended@example.test");
        MonetaryAdmissionResult admitted;
        using (var scope = suspended.Factory.Services.CreateScope())
        {
            var monetary = scope.ServiceProvider.GetRequiredService<MonetaryAdmissionService>();
            admitted = await monetary.AdmitAsync(Guid.CreateVersion7().ToString("D"), "usage-suspension-probe", 1, Cost(600, 400), CancellationToken.None);
        }

        Assert.AreEqual(MonetaryAdmissionOutcome.Admitted, admitted.Outcome);
        var suspendedUsage = await GetUsageAsync(suspended, suspendedAccess);
        Assert.AreEqual("monetary-suspended", suspendedUsage.GetProperty("availability").GetString());
        var suspendedBody = suspendedUsage.GetRawText();
        Assert.IsFalse(suspendedBody.Contains("MinorUnits", StringComparison.Ordinal), suspendedBody);
        var (_, _, suspendedSubmit) = await SubmitAsync(suspended, suspendedAccess, "Still admittable text");
        Assert.AreEqual("monetary-suspended", JsonDocument.Parse(suspendedSubmit).RootElement.GetProperty("usage").GetProperty("availability").GetString());

        await using var prioritized = await OperationFixture.CreateAsync(new Dictionary<string, string?>
        {
            ["Operations:UserDailyAllowanceCharacters"] = "100",
            ["Operations:GlobalDailyAllowanceCharacters"] = "100",
        });
        await prioritized.CreateAccountAsync("priority@example.test", confirmed: true);
        var priorityAccess = await prioritized.SignInAsync("priority@example.test");
        await SubmitAsync(prioritized, priorityAccess, new string('a', 60));
        await SubmitAsync(prioritized, priorityAccess, new string('b', 40));
        var priorityUsage = await GetUsageAsync(prioritized, priorityAccess);
        Assert.AreEqual("user-exhausted", priorityUsage.GetProperty("availability").GetString());
    }

    [TestMethod]
    public async Task StorageFailureReportsUnavailableWithoutUsageValues()
    {
        await using var fixture = await OperationFixture.CreateAsync();
        await fixture.CreateAccountAsync("unavailable@example.test", confirmed: true);
        var access = await fixture.SignInAsync("unavailable@example.test");

        await using (var sabotage = fixture.Database.CreateContext())
        {
            await sabotage.Database.ExecuteSqlRawAsync("DROP TABLE \"CharacterLedgerEntries\";");
        }

        using var response = await fixture.GetAsync("/api/usage", access);
        var body = await response.Content.ReadAsStringAsync();
        Assert.AreEqual(HttpStatusCode.ServiceUnavailable, response.StatusCode, body);
        Assert.AreEqual("no-store", response.Headers.CacheControl?.ToString());
        using var problem = JsonDocument.Parse(body);
        Assert.AreEqual("availability", problem.RootElement.GetProperty("category").GetString());
        Assert.IsFalse(body.Contains("consumedCharacters", StringComparison.Ordinal), body);
        Assert.IsFalse(body.Contains("reservedCharacters", StringComparison.Ordinal), body);
        Assert.IsFalse(body.Contains("availableCharacters", StringComparison.Ordinal), body);
    }

    [TestMethod]
    public async Task CrossMidnightSettlementChargesAdmissionDayWithFreshSnapshot()
    {
        await using var fixture = await OperationFixture.CreateAsync();
        var user = await fixture.CreateAccountAsync("midnight@example.test", confirmed: true);
        var access = await fixture.SignInAsync("midnight@example.test");
        var (operationId, scalarCount, _) = await SubmitAsync(fixture, access, "Cross midnight text");

        fixture.Time.Advance(TimeSpan.FromHours(16));
        var admissionDay = "2026-09-11";
        var currentDay = DayString(fixture.Time.GetUtcNow());
        Assert.AreEqual("2026-09-12", currentDay);

        SettlementResult settled;
        using (var scope = fixture.Factory.Services.CreateScope())
        {
            var settlements = scope.ServiceProvider.GetRequiredService<OperationSettlementService>();
            settled = await settlements.SettleSuccessAsync(
                user.Id, "translation", "Cross midnight text", null, "ru", null, Guid.Parse(operationId), CancellationToken.None);
        }

        Assert.AreEqual(SettlementOutcome.Settled, settled.Outcome);

        await using (var verification = fixture.Database.CreateContext())
        {
            var oldUser = await verification.CharacterLedgerEntries
                .SingleAsync(entry => entry.Scope == "user" && entry.Day == admissionDay);
            Assert.AreEqual(scalarCount, oldUser.ConsumedCharacters);
            Assert.AreEqual(0, oldUser.ReservedCharacters);
            var oldGlobal = await verification.CharacterLedgerEntries
                .SingleAsync(entry => entry.Scope == "global" && entry.Day == admissionDay);
            Assert.AreEqual(scalarCount, oldGlobal.ConsumedCharacters);
            Assert.AreEqual(
                0,
                await verification.CharacterLedgerEntries.CountAsync(entry => entry.Day == currentDay));
            Assert.AreEqual(2L, (await verification.LedgerRevisions.SingleAsync()).Value);
        }

        access = await fixture.SignInAsync("midnight@example.test");
        using var status = await fixture.GetAsync($"/api/operations/{operationId}", access);
        Assert.AreEqual(HttpStatusCode.OK, status.StatusCode);
        var statusBody = await status.Content.ReadAsStringAsync();
        using var statusDocument = JsonDocument.Parse(statusBody);
        var statusRoot = statusDocument.RootElement;
        Assert.AreEqual("succeeded", statusRoot.GetProperty("status").GetString());
        Assert.AreEqual(admissionDay, statusRoot.GetProperty("admissionDay").GetString());
        Assert.AreEqual(scalarCount, statusRoot.GetProperty("characterCount").GetInt32());
        var snapshot = statusRoot.GetProperty("usage");
        Assert.AreEqual(currentDay, snapshot.GetProperty("day").GetString());
        Assert.AreEqual(0, snapshot.GetProperty("consumedCharacters").GetInt32());
        Assert.AreEqual(0, snapshot.GetProperty("reservedCharacters").GetInt32());
        Assert.AreEqual("available", snapshot.GetProperty("availability").GetString());
        Assert.AreEqual(2L, snapshot.GetProperty("revision").GetInt64());
    }

    [TestMethod]
    public async Task PostMidnightDuplicateAndStatusReuseOriginalPeriod()
    {
        await using var fixture = await OperationFixture.CreateAsync();
        await fixture.CreateAccountAsync("duplicate@example.test", confirmed: true);
        var access = await fixture.SignInAsync("duplicate@example.test");
        var (operationId, scalarCount, submitBody) = await SubmitAsync(fixture, access, "Duplicate period text");
        var admissionDay = JsonDocument.Parse(submitBody).RootElement.GetProperty("admissionDay").GetString()!;

        fixture.Time.Advance(TimeSpan.FromHours(16));
        var currentDay = DayString(fixture.Time.GetUtcNow());
        Assert.AreNotEqual(admissionDay, currentDay);
        access = await fixture.SignInAsync("duplicate@example.test");

        var before = await GetUsageAsync(fixture, access);
        using (var duplicate = await fixture.PostRawAsync(
            "/api/operations",
            JsonSerializer.Serialize(new { operationId, family = "translation", source = "Duplicate period text", target = "ru" }),
            access))
        {
            var duplicateBody = await duplicate.Content.ReadAsStringAsync();
            Assert.AreEqual(HttpStatusCode.Accepted, duplicate.StatusCode, duplicateBody);
            using var duplicateDocument = JsonDocument.Parse(duplicateBody);
            Assert.AreEqual(admissionDay, duplicateDocument.RootElement.GetProperty("admissionDay").GetString());
            Assert.AreEqual("pending", duplicateDocument.RootElement.GetProperty("status").GetString());
            var duplicateUsage = duplicateDocument.RootElement.GetProperty("usage");
            Assert.AreEqual(currentDay, duplicateUsage.GetProperty("day").GetString());
            Assert.AreEqual(before.GetProperty("revision").GetInt64(), duplicateUsage.GetProperty("revision").GetInt64());
        }

        using (var conflict = await fixture.PostRawAsync(
            "/api/operations",
            JsonSerializer.Serialize(new { operationId, family = "translation", source = "Changed period text", target = "ru" }),
            access))
        {
            var conflictBody = await conflict.Content.ReadAsStringAsync();
            Assert.AreEqual(HttpStatusCode.Conflict, conflict.StatusCode, conflictBody);
            Assert.AreEqual("identityConflict", JsonDocument.Parse(conflictBody).RootElement.GetProperty("category").GetString());
        }

        using var status = await fixture.GetAsync($"/api/operations/{operationId}", access);
        var statusBody = await status.Content.ReadAsStringAsync();
        Assert.AreEqual(HttpStatusCode.OK, status.StatusCode, statusBody);
        using var statusDocument = JsonDocument.Parse(statusBody);
        Assert.AreEqual(admissionDay, statusDocument.RootElement.GetProperty("admissionDay").GetString());
        Assert.AreEqual(scalarCount, statusDocument.RootElement.GetProperty("characterCount").GetInt32());
        Assert.AreEqual(currentDay, statusDocument.RootElement.GetProperty("usage").GetProperty("day").GetString());

        var after = await GetUsageAsync(fixture, access);
        Assert.AreEqual(before.GetProperty("revision").GetInt64(), after.GetProperty("revision").GetInt64());
        Assert.AreEqual(0, after.GetProperty("reservedCharacters").GetInt32());
        Assert.AreEqual(0, after.GetProperty("consumedCharacters").GetInt32());

        await using (var verification = fixture.Database.CreateContext())
        {
            Assert.AreEqual(
                scalarCount,
                await verification.CharacterLedgerEntries
                    .Where(entry => entry.Scope == "user" && entry.Day == admissionDay)
                    .Select(entry => entry.ReservedCharacters)
                    .SingleAsync());
            Assert.AreEqual(1L, (await verification.LedgerRevisions.SingleAsync()).Value);
        }
    }

    [TestMethod]
    public async Task InterruptedAndFailedAcrossMidnightReportZeroCharge()
    {
        await using var fixture = await OperationFixture.CreateAsync();
        var user = await fixture.CreateAccountAsync("terminal@example.test", confirmed: true);
        var access = await fixture.SignInAsync("terminal@example.test");
        var (interruptedId, _, _) = await SubmitAsync(fixture, access, "Interrupted across midnight");
        var (failedId, _, _) = await SubmitAsync(fixture, access, "Failed across midnight");

        using (var scope = fixture.Factory.Services.CreateScope())
        {
            var recovery = scope.ServiceProvider.GetRequiredService<OperationRecoveryService>();
            var interrupted = await recovery.InterruptAsync(
                user.Id, "translation", "Interrupted across midnight", null, "ru", null, Guid.Parse(interruptedId), CancellationToken.None);
            Assert.AreEqual(RecoveryOutcome.Interrupted, interrupted.Outcome);
            var settlements = scope.ServiceProvider.GetRequiredService<OperationSettlementService>();
            var failed = await settlements.SettleFailureAsync(
                user.Id, "translation", "Failed across midnight", null, "ru", null, Guid.Parse(failedId), CancellationToken.None);
            Assert.AreEqual(SettlementOutcome.Settled, failed.Outcome);
        }

        fixture.Time.Advance(TimeSpan.FromHours(16));
        var currentDay = DayString(fixture.Time.GetUtcNow());
        access = await fixture.SignInAsync("terminal@example.test");

        foreach (var (operationId, expectedStatus) in new[] { (interruptedId, "interrupted"), (failedId, "failed") })
        {
            using var status = await fixture.GetAsync($"/api/operations/{operationId}", access);
            var statusBody = await status.Content.ReadAsStringAsync();
            Assert.AreEqual(HttpStatusCode.OK, status.StatusCode, statusBody);
            using var statusDocument = JsonDocument.Parse(statusBody);
            Assert.AreEqual(expectedStatus, statusDocument.RootElement.GetProperty("status").GetString());
            Assert.AreEqual(0, statusDocument.RootElement.GetProperty("characterCount").GetInt32());
            Assert.AreEqual("2026-09-11", statusDocument.RootElement.GetProperty("admissionDay").GetString());
            var snapshot = statusDocument.RootElement.GetProperty("usage");
            Assert.AreEqual(currentDay, snapshot.GetProperty("day").GetString());
            Assert.AreEqual("available", snapshot.GetProperty("availability").GetString());
        }

        using (var unknown = await fixture.GetAsync($"/api/operations/{NewIdentity(fixture)}", access))
        {
            var unknownBody = await unknown.Content.ReadAsStringAsync();
            Assert.AreEqual(HttpStatusCode.NotFound, unknown.StatusCode, unknownBody);
            Assert.AreEqual("unknownOperation", JsonDocument.Parse(unknownBody).RootElement.GetProperty("category").GetString());
            Assert.IsFalse(unknownBody.Contains("characterCount", StringComparison.Ordinal), unknownBody);
        }

        fixture.Time.Advance(TimeSpan.FromHours(10));
        access = await fixture.SignInAsync("terminal@example.test");
        using (var expired = await fixture.GetAsync($"/api/operations/{interruptedId}", access))
        {
            var expiredBody = await expired.Content.ReadAsStringAsync();
            Assert.AreEqual(HttpStatusCode.Gone, expired.StatusCode, expiredBody);
            Assert.AreEqual("identityExpired", JsonDocument.Parse(expiredBody).RootElement.GetProperty("category").GetString());
        }
    }

    [TestMethod]
    public async Task CrossMonthCarryoverSuspendsUntilAuthoritativeSettlement()
    {
        await using var fixture = await OperationFixture.CreateAsync(MonetaryConfig(1000));
        await fixture.CreateAccountAsync("carryover@example.test", confirmed: true);

        string attemptId = Guid.CreateVersion7().ToString("D");
        MonetaryAdmissionResult first;
        using (var scope = fixture.Factory.Services.CreateScope())
        {
            var monetary = scope.ServiceProvider.GetRequiredService<MonetaryAdmissionService>();
            first = await monetary.AdmitAsync(attemptId, "carryover-probe", 1, Cost(600, 400), CancellationToken.None);
        }

        Assert.AreEqual(MonetaryAdmissionOutcome.Admitted, first.Outcome);
        Assert.AreEqual(1000, first.BoundMinorUnits);
        Assert.AreEqual("2026-09", first.Reservation!.CostMonth);

        fixture.Time.Advance(TimeSpan.FromDays(21));
        Assert.AreEqual("2026-10-02", DayString(fixture.Time.GetUtcNow()));
        var access = await fixture.SignInAsync("carryover@example.test");

        MonetaryAdmissionResult denied;
        using (var scope = fixture.Factory.Services.CreateScope())
        {
            var monetary = scope.ServiceProvider.GetRequiredService<MonetaryAdmissionService>();
            denied = await monetary.AdmitAsync(Guid.CreateVersion7().ToString("D"), "carryover-probe", 2, Cost(300, 200), CancellationToken.None);
        }

        Assert.AreEqual(MonetaryAdmissionOutcome.DeniedOverCap, denied.Outcome);
        Assert.AreEqual("monetarySuspended", denied.Reason);

        var octoberUsage = await GetUsageAsync(fixture, access);
        Assert.AreEqual("2026-10-02", octoberUsage.GetProperty("day").GetString());
        Assert.AreEqual(0, octoberUsage.GetProperty("consumedCharacters").GetInt32());
        Assert.AreEqual("monetary-suspended", octoberUsage.GetProperty("availability").GetString());

        ReconciliationResult settled;
        using (var scope = fixture.Factory.Services.CreateScope())
        {
            var monetary = scope.ServiceProvider.GetRequiredService<MonetaryAdmissionService>();
            settled = await monetary.ReconcileAsync(attemptId, new ReconciliationEvidence(400, false, "october-settlement"), CancellationToken.None);
        }

        Assert.AreEqual(ReconciliationOutcome.Settled, settled.Outcome);
        Assert.AreEqual("2026-09", settled.Reservation!.CostMonth);

        ReconciliationResult repeated;
        using (var scope = fixture.Factory.Services.CreateScope())
        {
            var monetary = scope.ServiceProvider.GetRequiredService<MonetaryAdmissionService>();
            repeated = await monetary.ReconcileAsync(attemptId, new ReconciliationEvidence(400, false, "october-settlement"), CancellationToken.None);
        }

        Assert.AreEqual(ReconciliationOutcome.DuplicateObserved, repeated.Outcome);

        MonetaryAdmissionResult readmitted;
        using (var scope = fixture.Factory.Services.CreateScope())
        {
            var monetary = scope.ServiceProvider.GetRequiredService<MonetaryAdmissionService>();
            readmitted = await monetary.AdmitAsync(Guid.CreateVersion7().ToString("D"), "carryover-probe", 2, Cost(300, 200), CancellationToken.None);
        }

        Assert.AreEqual(MonetaryAdmissionOutcome.Admitted, readmitted.Outcome);
        Assert.AreEqual("2026-10", readmitted.Reservation!.CostMonth);

        await using (var verification = fixture.Database.CreateContext())
        {
            var september = await verification.MonetaryCostLedgers.SingleAsync(entry => entry.CostMonth == "2026-09");
            Assert.AreEqual(400, september.KnownSpendMinorUnits);
            Assert.AreEqual(0, september.UnresolvedExposureMinorUnits);
            var october = await verification.MonetaryCostLedgers.SingleAsync(entry => entry.CostMonth == "2026-10");
            Assert.AreEqual(0, october.KnownSpendMinorUnits);
            Assert.AreEqual(500, october.UnresolvedExposureMinorUnits);
            var knownTotal = await verification.MonetaryCostLedgers.SumAsync(entry => entry.KnownSpendMinorUnits);
            var unresolvedTotal = await verification.MonetaryCostLedgers.SumAsync(entry => entry.UnresolvedExposureMinorUnits);
            Assert.AreEqual(900, knownTotal + unresolvedTotal);
        }

        var (_, _, octoberSubmit) = await SubmitAsync(fixture, access, "October character text");
        Assert.AreEqual("2026-10-02", JsonDocument.Parse(octoberSubmit).RootElement.GetProperty("admissionDay").GetString());
        var rolledUsage = await GetUsageAsync(fixture, access);
        Assert.AreEqual("2026-10-02", rolledUsage.GetProperty("day").GetString());
        Assert.AreEqual(0, rolledUsage.GetProperty("consumedCharacters").GetInt32());
    }

    [TestMethod]
    public async Task SnapshotOrderingIsDayFirstRevisionSecond()
    {
        await using var fixture = await OperationFixture.CreateAsync();
        var user = await fixture.CreateAccountAsync("ordering@example.test", confirmed: true);
        var access = await fixture.SignInAsync("ordering@example.test");
        await fixture.CreateAccountAsync("ordering-other@example.test", confirmed: true);
        var otherAccess = await fixture.SignInAsync("ordering-other@example.test");

        var (firstId, firstCount, _) = await SubmitAsync(fixture, access, "Ordering first text");
        var first = await GetUsageAsync(fixture, access);
        var (secondId, _, _) = await SubmitAsync(fixture, access, "Ordering second text");
        var second = await GetUsageAsync(fixture, access);
        Assert.IsGreaterThan(first.GetProperty("revision").GetInt64(), second.GetProperty("revision").GetInt64());

        using (var scope = fixture.Factory.Services.CreateScope())
        {
            var settlements = scope.ServiceProvider.GetRequiredService<OperationSettlementService>();
            var settled = await settlements.SettleSuccessAsync(
                user.Id, "translation", "Ordering first text", null, "ru", null, Guid.Parse(firstId), CancellationToken.None);
            Assert.AreEqual(SettlementOutcome.Settled, settled.Outcome);
        }

        var settledDay = await GetUsageAsync(fixture, access);
        fixture.Time.Advance(TimeSpan.FromHours(16));
        access = await fixture.SignInAsync("ordering@example.test");
        otherAccess = await fixture.SignInAsync("ordering-other@example.test");
        var nextDay = await GetUsageAsync(fixture, access);
        Assert.AreEqual(DayString(fixture.Time.GetUtcNow()), nextDay.GetProperty("day").GetString());

        Assert.AreEqual(-1, Math.Sign(CompareSnapshots("ordering@example.test",
            settledDay.GetProperty("day").GetString()!, settledDay.GetProperty("revision").GetInt64(),
            "ordering@example.test",
            first.GetProperty("day").GetString()!, first.GetProperty("revision").GetInt64())));
        Assert.AreEqual(-1, Math.Sign(CompareSnapshots("ordering@example.test",
            second.GetProperty("day").GetString()!, second.GetProperty("revision").GetInt64(),
            "ordering@example.test",
            first.GetProperty("day").GetString()!, first.GetProperty("revision").GetInt64())));
        Assert.AreEqual(1, Math.Sign(CompareSnapshots("ordering@example.test",
            settledDay.GetProperty("day").GetString()!, settledDay.GetProperty("revision").GetInt64(),
            "ordering@example.test",
            nextDay.GetProperty("day").GetString()!, nextDay.GetProperty("revision").GetInt64())));
        Assert.IsLessThan(settledDay.GetProperty("consumedCharacters").GetInt32(), nextDay.GetProperty("consumedCharacters").GetInt32());

        using (var scope = fixture.Factory.Services.CreateScope())
        {
            var recovery = scope.ServiceProvider.GetRequiredService<OperationRecoveryService>();
            var interrupted = await recovery.InterruptAsync(
                user.Id, "translation", "Ordering second text", null, "ru", null, Guid.Parse(secondId), CancellationToken.None);
            Assert.AreEqual(RecoveryOutcome.Interrupted, interrupted.Outcome);
        }

        var afterRecovery = await GetUsageAsync(fixture, access);
        Assert.IsGreaterThan(nextDay.GetProperty("revision").GetInt64(), afterRecovery.GetProperty("revision").GetInt64());

        var revisionText = nextDay.GetProperty("revision").GetRawText();
        Assert.IsFalse(revisionText.Contains('.', StringComparison.Ordinal), revisionText);
        Assert.IsFalse(revisionText.Contains('e', StringComparison.OrdinalIgnoreCase), revisionText);
        Assert.AreEqual(nextDay.GetProperty("revision").GetInt64(), long.Parse(revisionText, System.Globalization.CultureInfo.InvariantCulture));
        using var roundTrip = JsonDocument.Parse(nextDay.GetRawText());
        Assert.AreEqual(nextDay.GetProperty("day").GetString(), roundTrip.RootElement.GetProperty("day").GetString());
        Assert.AreEqual(nextDay.GetProperty("revision").GetInt64(), roundTrip.RootElement.GetProperty("revision").GetInt64());
        Assert.AreEqual(nextDay.GetProperty("availability").GetString(), roundTrip.RootElement.GetProperty("availability").GetString());

        var otherUsage = await GetUsageAsync(fixture, otherAccess);
        Assert.AreEqual(afterRecovery.GetProperty("revision").GetInt64(), otherUsage.GetProperty("revision").GetInt64());
        Assert.AreNotEqual(nextDay.GetProperty("consumedCharacters").GetInt32(), settledDay.GetProperty("consumedCharacters").GetInt32());
        Assert.AreEqual(0, otherUsage.GetProperty("consumedCharacters").GetInt32());
        Assert.Throws<InvalidOperationException>(() => CompareSnapshots(
            "ordering@example.test", nextDay.GetProperty("day").GetString()!, nextDay.GetProperty("revision").GetInt64(),
            "ordering-other@example.test", otherUsage.GetProperty("day").GetString()!, otherUsage.GetProperty("revision").GetInt64()));

        using var foreign = await fixture.GetAsync($"/api/operations/{firstId}", otherAccess);
        Assert.AreEqual(HttpStatusCode.NotFound, foreign.StatusCode);
        Assert.AreEqual(firstCount, settledDay.GetProperty("consumedCharacters").GetInt32());
    }

    [TestMethod]
    public async Task RestartPreservesLedgersRevisionAndAvailability()
    {
        await using var fixture = await OperationFixture.CreateAsync();
        var user = await fixture.CreateAccountAsync("usage-restart@example.test", confirmed: true);
        var access = await fixture.SignInAsync("usage-restart@example.test");
        var (firstId, firstCount, _) = await SubmitAsync(fixture, access, "Restart usage first");
        using (var scope = fixture.Factory.Services.CreateScope())
        {
            var settlements = scope.ServiceProvider.GetRequiredService<OperationSettlementService>();
            var settled = await settlements.SettleSuccessAsync(
                user.Id, "translation", "Restart usage first", null, "ru", null, Guid.Parse(firstId), CancellationToken.None);
            Assert.AreEqual(SettlementOutcome.Settled, settled.Outcome);
        }

        fixture.Time.Advance(TimeSpan.FromHours(16));
        access = await fixture.SignInAsync("usage-restart@example.test");
        var currentDay = DayString(fixture.Time.GetUtcNow());
        var (secondId, secondCount, _) = await SubmitAsync(fixture, access, "Restart usage second");

        var beforeUsage = await GetUsageAsync(fixture, access);
        Assert.AreEqual(currentDay, beforeUsage.GetProperty("day").GetString());
        Assert.AreEqual(3L, beforeUsage.GetProperty("revision").GetInt64());

        string beforeStatusBody;
        using (var beforeStatus = await fixture.GetAsync($"/api/operations/{firstId}", access))
        {
            Assert.AreEqual(HttpStatusCode.OK, beforeStatus.StatusCode);
            beforeStatusBody = await beforeStatus.Content.ReadAsStringAsync();
        }

        await using var restarted = await fixture.RestartAsync();
        var restartedAccess = await restarted.SignInAsync("usage-restart@example.test");

        var afterUsage = await GetUsageAsync(restarted, restartedAccess);
        Assert.AreEqual(beforeUsage.GetRawText(), afterUsage.GetRawText());

        using (var afterStatus = await restarted.GetAsync($"/api/operations/{firstId}", restartedAccess))
        {
            Assert.AreEqual(HttpStatusCode.OK, afterStatus.StatusCode);
            var afterStatusBody = await afterStatus.Content.ReadAsStringAsync();
            Assert.AreEqual(
                JsonDocument.Parse(beforeStatusBody).RootElement.GetProperty("admissionDay").GetString(),
                JsonDocument.Parse(afterStatusBody).RootElement.GetProperty("admissionDay").GetString());
            Assert.AreEqual(
                JsonDocument.Parse(beforeStatusBody).RootElement.GetProperty("characterCount").GetInt32(),
                JsonDocument.Parse(afterStatusBody).RootElement.GetProperty("characterCount").GetInt32());
            StringAssert.Contains(afterStatusBody, "\"status\":\"succeeded\"");
            StringAssert.Contains(afterStatusBody, "\"outputAvailable\":false");
        }

        _ = await GetUsageAsync(restarted, restartedAccess);
        using (await restarted.GetAsync($"/api/operations/{secondId}", restartedAccess))
        {
        }

        await using (var verification = restarted.Database.CreateContext())
        {
            var oldUser = await verification.CharacterLedgerEntries
                .SingleAsync(entry => entry.Scope == "user" && entry.Day == "2026-09-11");
            Assert.AreEqual(firstCount, oldUser.ConsumedCharacters);
            Assert.AreEqual(0, oldUser.ReservedCharacters);
            var newUser = await verification.CharacterLedgerEntries
                .SingleAsync(entry => entry.Scope == "user" && entry.Day == currentDay);
            Assert.AreEqual(0, newUser.ConsumedCharacters);
            Assert.AreEqual(secondCount, newUser.ReservedCharacters);
            Assert.AreEqual(3L, (await verification.LedgerRevisions.SingleAsync()).Value);
        }
    }

    [TestMethod]
    public async Task UsageMetadataHoldsNoSourceTextOrSecrets()
    {
        await using var fixture = await OperationFixture.CreateAsync();
        await fixture.CreateAccountAsync("usage-sentinel@example.test", confirmed: true);
        var access = await fixture.SignInAsync("usage-sentinel@example.test");
        var sourceSentinel = "usage-sentinel-source-" + Guid.NewGuid().ToString("N");
        var secretSentinel = "usage-sentinel-secret-" + Guid.NewGuid().ToString("N");
        var (operationId, _, submitBody) = await SubmitAsync(fixture, access, sourceSentinel);
        Assert.IsFalse(submitBody.Contains(sourceSentinel, StringComparison.Ordinal));
        Assert.IsFalse(submitBody.Contains(secretSentinel, StringComparison.Ordinal));

        using var status = await fixture.GetAsync($"/api/operations/{operationId}", access);
        var statusBody = await status.Content.ReadAsStringAsync();
        Assert.IsFalse(statusBody.Contains(sourceSentinel, StringComparison.Ordinal));
        Assert.IsFalse(statusBody.Contains(secretSentinel, StringComparison.Ordinal));

        var usage = await GetUsageAsync(fixture, access);
        var usageBody = usage.GetRawText();
        Assert.IsFalse(usageBody.Contains(sourceSentinel, StringComparison.Ordinal));
        Assert.IsFalse(usageBody.Contains(secretSentinel, StringComparison.Ordinal));

        await using var verification = fixture.Database.CreateContext();
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
}
