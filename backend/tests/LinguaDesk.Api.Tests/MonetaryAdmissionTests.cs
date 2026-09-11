using System.Text.Json;
using LinguaDesk.Api.Features.Operations;
using LinguaDesk.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LinguaDesk.Api.Tests;

[TestClass]
public sealed class MonetaryAdmissionTests
{
    private const string Currency = "USD";

    private const string CostMonth = "2026-09";

    private static AttemptCostProfile Cost(
        int maxInputTokens,
        int maxOutputTokens,
        decimal inputPerMillion = 1_000_000m,
        decimal outputPerMillion = 1_000_000m,
        long other = 0,
        string currency = Currency) =>
        new(currency, "test-tariff", "2026-09-01", inputPerMillion, outputPerMillion, maxInputTokens, maxOutputTokens, other);

    private static Dictionary<string, string?> MonetaryConfig(long cap, string currency = Currency) => new()
    {
        ["MonetaryAdmission:MonthlyCapMinorUnits"] = cap.ToString(System.Globalization.CultureInfo.InvariantCulture),
        ["MonetaryAdmission:Currency"] = currency,
    };

    private static async Task<MonetaryAdmissionResult> AdmitAsync(
        OperationFixture fixture,
        string attemptId,
        AttemptCostProfile? cost,
        string operationReference = "opaque-operation-ref",
        int attemptNumber = 1)
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<MonetaryAdmissionService>();
        return await service.AdmitAsync(attemptId, operationReference, attemptNumber, cost, CancellationToken.None);
    }

    private static async Task<ReconciliationResult> ReconcileAsync(
        OperationFixture fixture,
        string attemptId,
        ReconciliationEvidence evidence)
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<MonetaryAdmissionService>();
        return await service.ReconcileAsync(attemptId, evidence, CancellationToken.None);
    }

    [TestMethod]
    public async Task FitAdmitsAndOverCapDeniesWithoutWrites()
    {
        await using var fixture = await OperationFixture.CreateAsync(MonetaryConfig(1000));

        var first = await AdmitAsync(fixture, Guid.CreateVersion7().ToString("D"), Cost(100, 50));
        Assert.AreEqual(MonetaryAdmissionOutcome.Admitted, first.Outcome);
        Assert.AreEqual(150, first.BoundMinorUnits);
        Assert.AreEqual(CostMonth, first.Reservation!.CostMonth);
        Assert.AreEqual(MonetaryReservationStates.Reserved, first.Reservation.State);

        var overCap = await AdmitAsync(fixture, Guid.CreateVersion7().ToString("D"), Cost(600, 300));
        Assert.AreEqual(MonetaryAdmissionOutcome.DeniedOverCap, overCap.Outcome);
        Assert.AreEqual(900, overCap.BoundMinorUnits);
        Assert.AreEqual("monetarySuspended", overCap.Reason);

        await using var verification = fixture.Database.CreateContext();
        Assert.AreEqual(1, await verification.MonetaryAttemptReservations.CountAsync());
        var ledger = await verification.MonetaryCostLedgers.SingleAsync();
        Assert.AreEqual(CostMonth, ledger.CostMonth);
        Assert.AreEqual(0, ledger.KnownSpendMinorUnits);
        Assert.AreEqual(150, ledger.UnresolvedExposureMinorUnits);
    }

    [TestMethod]
    public async Task UnknownPriceAndUnboundedScopeAreIneligible()
    {
        await using var fixture = await OperationFixture.CreateAsync(MonetaryConfig(1000));

        var cases = new (string Name, AttemptCostProfile? Cost, string Reason)[]
        {
            ("missingProfile", null, "missingCostProfile"),
            ("unknownInputPrice", Cost(100, 50, inputPerMillion: 0m), "unknownPrice"),
            ("unknownOutputPrice", Cost(100, 50, outputPerMillion: 0m), "unknownPrice"),
            ("unboundedInput", Cost(0, 50), "unboundedScope"),
            ("unboundedOutput", Cost(100, 0), "unboundedScope"),
            ("currencyMismatch", Cost(100, 50, currency: "EUR"), "currencyMismatch"),
            ("negativeOtherCharges", Cost(100, 50, other: -1), "negativeOtherCharges"),
        };
        foreach (var item in cases)
        {
            var result = await AdmitAsync(fixture, Guid.CreateVersion7().ToString("D"), item.Cost);
            Assert.AreEqual(MonetaryAdmissionOutcome.Ineligible, result.Outcome, item.Name);
            Assert.AreEqual(item.Reason, result.Reason, item.Name);
        }

        await using var verification = fixture.Database.CreateContext();
        Assert.AreEqual(0, await verification.MonetaryAttemptReservations.CountAsync());
        Assert.AreEqual(0, await verification.MonetaryCostLedgers.CountAsync());
    }

    [TestMethod]
    public async Task UpperBoundUsesCeilingRounding()
    {
        await using var fixture = await OperationFixture.CreateAsync(MonetaryConfig(100000));

        var fractional = await AdmitAsync(fixture, Guid.CreateVersion7().ToString("D"), Cost(1, 1, 500_000m, 500_000m));
        Assert.AreEqual(MonetaryAdmissionOutcome.Admitted, fractional.Outcome);
        Assert.AreEqual(2, fractional.BoundMinorUnits);

        var dust = await AdmitAsync(fixture, Guid.CreateVersion7().ToString("D"), Cost(1, 1, 1m, 1m));
        Assert.AreEqual(MonetaryAdmissionOutcome.Admitted, dust.Outcome);
        Assert.AreEqual(2, dust.BoundMinorUnits);

        var withOther = await AdmitAsync(fixture, Guid.CreateVersion7().ToString("D"), Cost(10, 10, other: 5));
        Assert.AreEqual(MonetaryAdmissionOutcome.Admitted, withOther.Outcome);
        Assert.AreEqual(25, withOther.BoundMinorUnits);

        await using var verification = fixture.Database.CreateContext();
        Assert.AreEqual(2 + 2 + 25, await verification.MonetaryCostLedgers.SumAsync(entry => entry.UnresolvedExposureMinorUnits));
    }

    [TestMethod]
    public async Task InvalidConfigurationDeniesPaidAdmission()
    {
        await using var fixture = await OperationFixture.CreateAsync(MonetaryConfig(0));

        using (var scope = fixture.Factory.Services.CreateScope())
        {
            var options = scope.ServiceProvider.GetRequiredService<IOptions<MonetaryAdmissionOptions>>();
            Assert.Throws<OptionsValidationException>(() => _ = options.Value);
        }

        var denied = await AdmitAsync(fixture, Guid.CreateVersion7().ToString("D"), Cost(100, 50));
        Assert.AreEqual(MonetaryAdmissionOutcome.ConfigurationInvalid, denied.Outcome);
        Assert.AreEqual("monetaryConfigurationInvalid", denied.Reason);

        await using var verification = fixture.Database.CreateContext();
        Assert.AreEqual(0, await verification.MonetaryAttemptReservations.CountAsync());
        Assert.AreEqual(0, await verification.MonetaryCostLedgers.CountAsync());

        await using var configured = await OperationFixture.CreateAsync(MonetaryConfig(1000));
        using (var scope = configured.Factory.Services.CreateScope())
        {
            var options = scope.ServiceProvider.GetRequiredService<IOptions<MonetaryAdmissionOptions>>().Value;
            Assert.AreEqual(1000, options.MonthlyCapMinorUnits);
            Assert.AreEqual(Currency, options.Currency);
        }
    }

    [TestMethod]
    public async Task ConcurrentAdmissionsHoldCeiling()
    {
        await using var fixture = await OperationFixture.CreateAsync(MonetaryConfig(1000));

        var attempts = Enumerable.Range(0, 8)
            .Select(_ => Task.Run(async () =>
            {
                using var scope = fixture.Factory.Services.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<MonetaryAdmissionService>();
                return await service.AdmitAsync(
                    Guid.CreateVersion7().ToString("D"), "opaque-operation-ref", 1, Cost(200, 100), CancellationToken.None);
            }))
            .ToArray();
        var results = await Task.WhenAll(attempts);

        var admitted = results.Where(result => result.Outcome == MonetaryAdmissionOutcome.Admitted).ToArray();
        var denied = results.Where(result => result.Outcome == MonetaryAdmissionOutcome.DeniedOverCap).ToArray();
        Assert.HasCount(8, results);
        Assert.HasCount(3, admitted);
        Assert.HasCount(5, denied);
        foreach (var denial in denied)
        {
            Assert.AreEqual("monetarySuspended", denial.Reason);
        }

        await using var verification = fixture.Database.CreateContext();
        var stored = await verification.MonetaryAttemptReservations.ToListAsync();
        Assert.HasCount(3, stored);
        Assert.IsTrue(stored.All(reservation => reservation.BoundMinorUnits == 300));
        var ledger = await verification.MonetaryCostLedgers.SingleAsync();
        Assert.AreEqual(0, ledger.KnownSpendMinorUnits);
        Assert.AreEqual(900, ledger.UnresolvedExposureMinorUnits);
    }

    [TestMethod]
    public async Task CrossMonthCarryoverBlocksNewMonth()
    {
        await using var fixture = await OperationFixture.CreateAsync(MonetaryConfig(1000));

        var september = await AdmitAsync(fixture, Guid.CreateVersion7().ToString("D"), Cost(500, 300));
        Assert.AreEqual(MonetaryAdmissionOutcome.Admitted, september.Outcome);
        Assert.AreEqual("2026-09", september.Reservation!.CostMonth);

        fixture.Time.Advance(TimeSpan.FromDays(30));

        var blocked = await AdmitAsync(fixture, Guid.CreateVersion7().ToString("D"), Cost(200, 100));
        Assert.AreEqual(MonetaryAdmissionOutcome.DeniedOverCap, blocked.Outcome);

        var october = await AdmitAsync(fixture, Guid.CreateVersion7().ToString("D"), Cost(100, 100));
        Assert.AreEqual(MonetaryAdmissionOutcome.Admitted, october.Outcome);
        Assert.AreEqual("2026-10", october.Reservation!.CostMonth);

        await using var verification = fixture.Database.CreateContext();
        var ledgers = await verification.MonetaryCostLedgers.OrderBy(entry => entry.CostMonth).ToListAsync();
        Assert.HasCount(2, ledgers);
        Assert.AreEqual("2026-09", ledgers[0].CostMonth);
        Assert.AreEqual(0, ledgers[0].KnownSpendMinorUnits);
        Assert.AreEqual(800, ledgers[0].UnresolvedExposureMinorUnits);
        Assert.AreEqual("2026-10", ledgers[1].CostMonth);
        Assert.AreEqual(0, ledgers[1].KnownSpendMinorUnits);
        Assert.AreEqual(200, ledgers[1].UnresolvedExposureMinorUnits);
    }

    [TestMethod]
    public async Task SettlementMovesExposureOnceAndReleaseClears()
    {
        await using var fixture = await OperationFixture.CreateAsync(MonetaryConfig(10000));

        var attemptA = Guid.CreateVersion7().ToString("D");
        var admitted = await AdmitAsync(fixture, attemptA, Cost(500, 300));
        Assert.AreEqual(MonetaryAdmissionOutcome.Admitted, admitted.Outcome);

        var settled = await ReconcileAsync(fixture, attemptA, new ReconciliationEvidence(500, false, "invoice-sep-1"));
        Assert.AreEqual(ReconciliationOutcome.Settled, settled.Outcome);
        Assert.AreEqual(MonetaryReservationStates.Settled, settled.Reservation!.State);
        Assert.AreEqual(500, settled.Reservation.SettledActualMinorUnits);
        Assert.AreEqual("invoice-sep-1", settled.Reservation.EvidenceReference);

        await using (var verification = fixture.Database.CreateContext())
        {
            var ledger = await verification.MonetaryCostLedgers.SingleAsync();
            Assert.AreEqual(500, ledger.KnownSpendMinorUnits);
            Assert.AreEqual(0, ledger.UnresolvedExposureMinorUnits);
        }

        var repeated = await ReconcileAsync(fixture, attemptA, new ReconciliationEvidence(500, false, "invoice-sep-1"));
        Assert.AreEqual(ReconciliationOutcome.DuplicateObserved, repeated.Outcome);

        await using (var verification = fixture.Database.CreateContext())
        {
            var ledger = await verification.MonetaryCostLedgers.SingleAsync();
            Assert.AreEqual(500, ledger.KnownSpendMinorUnits);
            Assert.AreEqual(0, ledger.UnresolvedExposureMinorUnits);
            Assert.AreEqual(1, await verification.MonetaryAttemptReservations.CountAsync());
        }

        var attemptB = Guid.CreateVersion7().ToString("D");
        await AdmitAsync(fixture, attemptB, Cost(100, 100));
        var released = await ReconcileAsync(fixture, attemptB, new ReconciliationEvidence(null, true, "void-1"));
        Assert.AreEqual(ReconciliationOutcome.Released, released.Outcome);
        Assert.AreEqual(MonetaryReservationStates.Released, released.Reservation!.State);

        await using (var verification = fixture.Database.CreateContext())
        {
            var ledger = await verification.MonetaryCostLedgers.SingleAsync();
            Assert.AreEqual(500, ledger.KnownSpendMinorUnits);
            Assert.AreEqual(0, ledger.UnresolvedExposureMinorUnits);
        }

        var unknown = await ReconcileAsync(fixture, Guid.CreateVersion7().ToString("D"), new ReconciliationEvidence(10, false, "invoice-x"));
        Assert.AreEqual(ReconciliationOutcome.UnknownAttempt, unknown.Outcome);
    }

    [TestMethod]
    public async Task MissingEvidenceRetainsReservation()
    {
        await using var fixture = await OperationFixture.CreateAsync(MonetaryConfig(10000));

        var attemptId = Guid.CreateVersion7().ToString("D");
        await AdmitAsync(fixture, attemptId, Cost(200, 100));

        var missing = await ReconcileAsync(fixture, attemptId, new ReconciliationEvidence(null, false, "no-evidence"));
        Assert.AreEqual(ReconciliationOutcome.RetainedMissingEvidence, missing.Outcome);

        var conflicting = await ReconcileAsync(fixture, attemptId, new ReconciliationEvidence(100, true, "conflicting"));
        Assert.AreEqual(ReconciliationOutcome.RetainedMissingEvidence, conflicting.Outcome);

        var negative = await ReconcileAsync(fixture, attemptId, new ReconciliationEvidence(-5, false, "negative"));
        Assert.AreEqual(ReconciliationOutcome.RetainedMissingEvidence, negative.Outcome);

        await using var verification = fixture.Database.CreateContext();
        var reservation = await verification.MonetaryAttemptReservations.SingleAsync();
        Assert.AreEqual(MonetaryReservationStates.Reserved, reservation.State);
        var ledger = await verification.MonetaryCostLedgers.SingleAsync();
        Assert.AreEqual(0, ledger.KnownSpendMinorUnits);
        Assert.AreEqual(300, ledger.UnresolvedExposureMinorUnits);
    }

    [TestMethod]
    public async Task SettledAttributionStaysInOriginalMonth()
    {
        await using var fixture = await OperationFixture.CreateAsync(MonetaryConfig(10000));

        var attemptId = Guid.CreateVersion7().ToString("D");
        await AdmitAsync(fixture, attemptId, Cost(500, 300));

        fixture.Time.Advance(TimeSpan.FromDays(30));

        var settled = await ReconcileAsync(fixture, attemptId, new ReconciliationEvidence(800, false, "invoice-late"));
        Assert.AreEqual(ReconciliationOutcome.Settled, settled.Outcome);
        Assert.AreEqual("2026-09", settled.Reservation!.CostMonth);

        await using var verification = fixture.Database.CreateContext();
        var september = await verification.MonetaryCostLedgers.SingleAsync(entry => entry.CostMonth == "2026-09");
        Assert.AreEqual(800, september.KnownSpendMinorUnits);
        Assert.AreEqual(0, september.UnresolvedExposureMinorUnits);
        Assert.IsFalse(await verification.MonetaryCostLedgers.AnyAsync(entry => entry.CostMonth == "2026-10"));
    }

    [TestMethod]
    public async Task RestartPreservesLedgersAndObservesDuplicate()
    {
        await using var database = StorageTestDatabase.Create();
        await database.MigrateAsync();
        var time = new AdjustableTimeProvider(new DateTimeOffset(2026, 9, 11, 8, 30, 0, TimeSpan.Zero));
        var options = Options.Create(new MonetaryAdmissionOptions { MonthlyCapMinorUnits = 1000, Currency = Currency });
        var attemptId = Guid.CreateVersion7().ToString("D");

        await using (var context = database.CreateContext())
        {
            var service = new MonetaryAdmissionService(context, time, options, new CapturingLogger());
            var admitted = await service.AdmitAsync(attemptId, "opaque-operation-ref", 1, Cost(100, 50), CancellationToken.None);
            Assert.AreEqual(MonetaryAdmissionOutcome.Admitted, admitted.Outcome);
        }

        await using (var restarted = database.CreateContext())
        {
            await restarted.Database.MigrateAsync();
            var service = new MonetaryAdmissionService(restarted, time, options, new CapturingLogger());
            var duplicate = await service.AdmitAsync(attemptId, "opaque-operation-ref", 1, Cost(100, 50), CancellationToken.None);
            Assert.AreEqual(MonetaryAdmissionOutcome.DuplicateObserved, duplicate.Outcome);
            Assert.AreEqual(150, duplicate.BoundMinorUnits);
        }

        await using var verification = database.CreateContext();
        Assert.AreEqual(1, await verification.MonetaryAttemptReservations.CountAsync());
        var ledger = await verification.MonetaryCostLedgers.SingleAsync();
        Assert.AreEqual(150, ledger.UnresolvedExposureMinorUnits);
        Assert.AreEqual(0, ledger.KnownSpendMinorUnits);
    }

    [TestMethod]
    public void ProblemMappingUsesSuspensionShapesWithoutAmounts()
    {
        var denied = MonetaryAdmissionProblem.Describe(MonetaryAdmissionOutcome.DeniedOverCap);
        Assert.AreEqual((503, "monetarySuspension"), (denied.Status, denied.Category));

        var unconfigured = MonetaryAdmissionProblem.Describe(MonetaryAdmissionOutcome.ConfigurationInvalid);
        Assert.AreEqual((503, "monetarySuspension"), (unconfigured.Status, unconfigured.Category));

        var ineligible = MonetaryAdmissionProblem.Describe(MonetaryAdmissionOutcome.Ineligible);
        Assert.AreEqual((422, "monetaryIneligible"), (ineligible.Status, ineligible.Category));

        foreach (var mapped in new[] { denied, unconfigured, ineligible })
        {
            var payload = JsonSerializer.Serialize(new
            {
                status = mapped.Status,
                title = mapped.Title,
                detail = mapped.Detail,
                category = mapped.Category,
            });
            Assert.IsFalse(payload.Contains("7777777", StringComparison.Ordinal));
            Assert.IsFalse(payload.Contains("50000000", StringComparison.Ordinal));
        }

        Assert.Throws<ArgumentOutOfRangeException>(() => MonetaryAdmissionProblem.Describe(MonetaryAdmissionOutcome.Admitted));
        Assert.Throws<ArgumentOutOfRangeException>(() => MonetaryAdmissionProblem.Describe(MonetaryAdmissionOutcome.DuplicateObserved));
    }

    [TestMethod]
    public void AdmissionServiceHasNoDispatchDependencies()
    {
        var parameters = typeof(MonetaryAdmissionService).GetConstructors().Single().GetParameters();
        CollectionAssert.AreEquivalent(
            new[]
            {
                typeof(LinguaDeskDbContext),
                typeof(TimeProvider),
                typeof(IOptions<MonetaryAdmissionOptions>),
                typeof(ILogger<MonetaryAdmissionService>),
            },
            parameters.Select(parameter => parameter.ParameterType).ToArray());
    }

    [TestMethod]
    public async Task LogsAndTablesCarryNoAmountsOrUnrelatedText()
    {
        await using var fixture = await OperationFixture.CreateAsync(MonetaryConfig(50000000));
        var logger = new CapturingLogger();
        var options = Options.Create(new MonetaryAdmissionOptions { MonthlyCapMinorUnits = 50000000, Currency = Currency });

        var attemptSentinel = "attempt-sentinel-" + Guid.NewGuid().ToString("N");
        var operationSentinel = "operation-sentinel-" + Guid.NewGuid().ToString("N");
        var tariffSentinel = "tariff-sentinel-" + Guid.NewGuid().ToString("N");
        var cost = new AttemptCostProfile(Currency, tariffSentinel, "2026-09-01", 1_000_000m, 1_000_000m, 5000000, 2777777, 0);

        await using (var context = fixture.Database.CreateContext())
        {
            var service = new MonetaryAdmissionService(context, fixture.Time, options, logger);
            var admitted = await service.AdmitAsync(attemptSentinel, operationSentinel, 1, cost, CancellationToken.None);
            Assert.AreEqual(MonetaryAdmissionOutcome.Admitted, admitted.Outcome);
            Assert.AreEqual(7777777, admitted.BoundMinorUnits);

            var denied = await service.AdmitAsync(
                "attempt-" + Guid.NewGuid().ToString("N"), operationSentinel, 1,
                new AttemptCostProfile(Currency, tariffSentinel, "2026-09-01", 1_000_000m, 1_000_000m, 40000000, 40000000, 0),
                CancellationToken.None);
            Assert.AreEqual(MonetaryAdmissionOutcome.DeniedOverCap, denied.Outcome);

            var settled = await service.ReconcileAsync(attemptSentinel, new ReconciliationEvidence(7777777, false, "invoice-sentinel"), CancellationToken.None);
            Assert.AreEqual(ReconciliationOutcome.Settled, settled.Outcome);
        }

        foreach (var message in logger.Messages)
        {
            Assert.IsFalse(message.Contains("7777777", StringComparison.Ordinal), message);
            Assert.IsFalse(message.Contains("50000000", StringComparison.Ordinal), message);
            Assert.IsFalse(message.Contains(attemptSentinel, StringComparison.Ordinal), message);
            Assert.IsFalse(message.Contains(operationSentinel, StringComparison.Ordinal), message);
            Assert.IsFalse(message.Contains(tariffSentinel, StringComparison.Ordinal), message);
        }

        await using var verification = fixture.Database.CreateContext();
        var tables = await verification.Database.SqlQueryRaw<string>(
            "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%';").ToListAsync();
        var identityHits = new List<string>();
        foreach (var table in tables.Where(name => name is not ("__EFMigrationsHistory" or "AspNetUsers" or "AspNetUserClaims" or "AspNetUserLogins" or "AspNetUserTokens")))
        {
            var safeTable = table.Replace("'", "''", StringComparison.Ordinal);
            var columns = await verification.Database.SqlQueryRaw<string>(
                string.Concat("SELECT name || '|' || type FROM pragma_table_info('", safeTable, "');")).ToListAsync();
            var textColumns = columns
                .Select(entry => entry.Split('|'))
                .Where(parts => parts.Length == 2 && parts[1].StartsWith("TEXT", StringComparison.OrdinalIgnoreCase))
                .Select(parts => parts[0])
                .ToList();
            foreach (var column in textColumns)
            {
                var safeColumn = column.Replace("\"", "\"\"", StringComparison.Ordinal);
                var safeFrom = table.Replace("\"", "\"\"", StringComparison.Ordinal);
                var values = await verification.Database.SqlQueryRaw<string>(
                    string.Concat("SELECT COALESCE(CAST(\"", safeColumn, "\" AS TEXT), '') FROM \"", safeFrom, "\";")).ToListAsync();
                foreach (var value in values)
                {
                    Assert.IsFalse(value.Contains("7777777", StringComparison.Ordinal), $"{table}.{column}");
                    Assert.IsFalse(value.Contains("50000000", StringComparison.Ordinal), $"{table}.{column}");
                    if (value.Contains(attemptSentinel, StringComparison.Ordinal)
                        || value.Contains(operationSentinel, StringComparison.Ordinal)
                        || value.Contains(tariffSentinel, StringComparison.Ordinal))
                    {
                        identityHits.Add($"{table}.{column}");
                    }
                }
            }
        }

        Assert.IsGreaterThanOrEqualTo(1, identityHits.Count);
        Assert.IsTrue(identityHits.All(location => location.StartsWith("MonetaryAttemptReservations.", StringComparison.Ordinal)), string.Join(",", identityHits));
    }

    private sealed class CapturingLogger : ILogger<MonetaryAdmissionService>
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
