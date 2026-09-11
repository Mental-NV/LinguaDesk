using LinguaDesk.Api.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LinguaDesk.Api.Features.Operations;

public enum MonetaryAdmissionOutcome
{
    Admitted,
    DuplicateObserved,
    DeniedOverCap,
    Ineligible,
    ConfigurationInvalid,
}

public sealed record MonetaryAdmissionResult(
    MonetaryAdmissionOutcome Outcome,
    MonetaryAttemptReservation? Reservation,
    long BoundMinorUnits,
    DateTimeOffset ServerTime,
    string? Reason = null);

public enum ReconciliationOutcome
{
    Settled,
    Released,
    DuplicateObserved,
    RetainedMissingEvidence,
    UnknownAttempt,
}

public sealed record ReconciliationEvidence(
    long? ActualCostMinorUnits,
    bool AuthoritativeNoCharge,
    string? EvidenceReference);

public sealed record ReconciliationResult(
    ReconciliationOutcome Outcome,
    MonetaryAttemptReservation? Reservation,
    DateTimeOffset ServerTime);

public sealed partial class MonetaryAdmissionService(
    LinguaDeskDbContext database,
    TimeProvider timeProvider,
    IOptions<MonetaryAdmissionOptions> options,
    ILogger<MonetaryAdmissionService> logger)
{
    private const int MaxAdmissionAttempts = 5;

    public async Task<MonetaryAdmissionResult> AdmitAsync(
        string attemptId,
        string operationReference,
        int attemptNumber,
        AttemptCostProfile? cost,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(attemptId);
        ArgumentException.ThrowIfNullOrEmpty(operationReference);

        MonetaryAdmissionOptions limits;
        try
        {
            limits = options.Value;
        }
        catch (OptionsValidationException)
        {
            var observed = timeProvider.GetUtcNow();
            var invalidMonth = MonthString(observed);
            Log.ConfigurationInvalid(logger, invalidMonth);
            return new(MonetaryAdmissionOutcome.ConfigurationInvalid, null, 0, observed, "monetaryConfigurationInvalid");
        }

        var duplicate = await database.MonetaryAttemptReservations
            .AsNoTracking()
            .SingleOrDefaultAsync(reservation => reservation.AttemptId == attemptId, cancellationToken);
        if (duplicate is not null)
        {
            var observed = timeProvider.GetUtcNow();
            var duplicateMonth = MonthString(observed);
            Log.DuplicateObserved(logger, duplicateMonth);
            return new(MonetaryAdmissionOutcome.DuplicateObserved, duplicate, duplicate.BoundMinorUnits, observed);
        }

        if (!TryComputeUpperBound(cost, limits.Currency, out var bound, out var reason))
        {
            var observed = timeProvider.GetUtcNow();
            var ineligibleMonth = MonthString(observed);
            var ineligibleReason = reason ?? "unknown";
            Log.Ineligible(logger, ineligibleMonth, ineligibleReason);
            return new(MonetaryAdmissionOutcome.Ineligible, null, 0, observed, reason);
        }

        for (var attempt = 0; ; attempt++)
        {
            await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var transactionTime = timeProvider.GetUtcNow();
                var costMonth = MonthString(transactionTime);
                await database.Database.ExecuteSqlRawAsync(
                    "INSERT OR IGNORE INTO \"MonetaryCostLedgers\" (\"CostMonth\", \"KnownSpendMinorUnits\", \"UnresolvedExposureMinorUnits\") VALUES ({0}, 0, 0)",
                    [costMonth],
                    cancellationToken);

                var ledger = await database.MonetaryCostLedgers
                    .SingleAsync(entry => entry.CostMonth == costMonth, cancellationToken);
                var totalUnresolved = await database.MonetaryCostLedgers
                    .SumAsync(entry => entry.UnresolvedExposureMinorUnits, cancellationToken);

                if (ledger.KnownSpendMinorUnits + totalUnresolved + bound > limits.MonthlyCapMinorUnits)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    database.ChangeTracker.Clear();
                    Log.DeniedOverCap(logger, costMonth);
                    return new(MonetaryAdmissionOutcome.DeniedOverCap, null, bound, transactionTime, "monetarySuspended");
                }

                var reservation = new MonetaryAttemptReservation
                {
                    AttemptId = attemptId,
                    OperationReference = operationReference,
                    AttemptNumber = attemptNumber,
                    BoundMinorUnits = bound,
                    CostMonth = costMonth,
                    TariffReference = Truncate(TariffReference(cost!), 256),
                    State = MonetaryReservationStates.Reserved,
                    CreatedUtc = transactionTime,
                };
                database.MonetaryAttemptReservations.Add(reservation);
                ledger.UnresolvedExposureMinorUnits += bound;
                await database.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                Log.Admitted(logger, costMonth);
                return new(MonetaryAdmissionOutcome.Admitted, reservation, bound, transactionTime);
            }
            catch (DbUpdateException exception) when (IsUniqueViolation(exception))
            {
                await transaction.RollbackAsync(cancellationToken);
                database.ChangeTracker.Clear();
                var raced = await database.MonetaryAttemptReservations
                    .AsNoTracking()
                    .SingleOrDefaultAsync(reservation => reservation.AttemptId == attemptId, cancellationToken);
                if (raced is not null)
                {
                    var observed = timeProvider.GetUtcNow();
                    var racedMonth = MonthString(observed);
                    Log.DuplicateObserved(logger, racedMonth);
                    return new(MonetaryAdmissionOutcome.DuplicateObserved, raced, raced.BoundMinorUnits, observed);
                }

                if (attempt >= MaxAdmissionAttempts - 1)
                {
                    throw;
                }
            }
            catch (SqliteException exception) when ((exception.SqliteErrorCode == 5 || exception.SqliteErrorCode == 6) && attempt < MaxAdmissionAttempts - 1)
            {
                await transaction.RollbackAsync(cancellationToken);
                database.ChangeTracker.Clear();
            }
        }
    }

    public async Task<ReconciliationResult> ReconcileAsync(
        string attemptId,
        ReconciliationEvidence evidence,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(attemptId);
        ArgumentNullException.ThrowIfNull(evidence);

        var current = await database.MonetaryAttemptReservations
            .AsNoTracking()
            .SingleOrDefaultAsync(reservation => reservation.AttemptId == attemptId, cancellationToken);
        if (current is null)
        {
            return new(ReconciliationOutcome.UnknownAttempt, null, timeProvider.GetUtcNow());
        }

        if (!string.Equals(current.State, MonetaryReservationStates.Reserved, StringComparison.Ordinal))
        {
            return new(ReconciliationOutcome.DuplicateObserved, current, timeProvider.GetUtcNow());
        }

        if (!IsAuthoritative(evidence, out var settle))
        {
            return new(ReconciliationOutcome.RetainedMissingEvidence, current, timeProvider.GetUtcNow());
        }

        var actualCapped = settle
            ? Math.Min(evidence.ActualCostMinorUnits!.Value, current.BoundMinorUnits)
            : 0;
        var targetState = settle ? MonetaryReservationStates.Settled : MonetaryReservationStates.Released;
        var reconciledAt = timeProvider.GetUtcNow();

        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        var claimed = await database.MonetaryAttemptReservations
            .Where(reservation =>
                reservation.AttemptId == attemptId
                && reservation.State == MonetaryReservationStates.Reserved)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(reservation => reservation.State, targetState)
                    .SetProperty(reservation => reservation.SettledActualMinorUnits, actualCapped)
                    .SetProperty(reservation => reservation.EvidenceReference, Truncate(evidence.EvidenceReference ?? string.Empty, 128))
                    .SetProperty(reservation => reservation.ReconciledUtc, reconciledAt),
                cancellationToken);
        if (claimed == 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            database.ChangeTracker.Clear();
            var reread = await database.MonetaryAttemptReservations
                .AsNoTracking()
                .SingleOrDefaultAsync(reservation => reservation.AttemptId == attemptId, cancellationToken);
            return reread is null
                ? new(ReconciliationOutcome.UnknownAttempt, null, timeProvider.GetUtcNow())
                : new(ReconciliationOutcome.DuplicateObserved, reread, timeProvider.GetUtcNow());
        }

        var reservation = await database.MonetaryAttemptReservations
            .SingleAsync(item => item.AttemptId == attemptId, cancellationToken);
        var ledger = await GetOrCreateLedgerAsync(reservation.CostMonth, cancellationToken);
        ledger.UnresolvedExposureMinorUnits = Math.Max(0, ledger.UnresolvedExposureMinorUnits - reservation.BoundMinorUnits);
        if (settle)
        {
            checked
            {
                ledger.KnownSpendMinorUnits += actualCapped;
            }
        }

        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        Log.Reconciled(logger, reservation.CostMonth, targetState);
        return new(
            settle ? ReconciliationOutcome.Settled : ReconciliationOutcome.Released,
            reservation,
            timeProvider.GetUtcNow());
    }

    internal static bool TryComputeUpperBound(
        AttemptCostProfile? cost,
        string currency,
        out long bound,
        out string? reason)
    {
        bound = 0;
        reason = null;
        if (cost is null)
        {
            reason = "missingCostProfile";
            return false;
        }

        if (!string.Equals(cost.Currency, currency, StringComparison.Ordinal))
        {
            reason = "currencyMismatch";
            return false;
        }

        if (!(cost.PeakInputPerMillionTokens > 0) || !(cost.PeakOutputPerMillionTokens > 0))
        {
            reason = "unknownPrice";
            return false;
        }

        if (cost.MaxInputTokens <= 0 || cost.MaxOutputTokens <= 0)
        {
            reason = "unboundedScope";
            return false;
        }

        if (cost.OtherBillableMinorUnits < 0)
        {
            reason = "negativeOtherCharges";
            return false;
        }

        try
        {
            var input = CeilToMinorUnits(cost.MaxInputTokens, cost.PeakInputPerMillionTokens);
            var output = CeilToMinorUnits(cost.MaxOutputTokens, cost.PeakOutputPerMillionTokens);
            checked
            {
                bound = input + output + cost.OtherBillableMinorUnits;
            }

            return true;
        }
        catch (OverflowException)
        {
            bound = 0;
            reason = "boundOverflow";
            return false;
        }
    }

    private static long CeilToMinorUnits(int tokens, decimal ratePerMillionTokens)
    {
        var exact = (decimal)tokens * ratePerMillionTokens / 1_000_000m;
        return (long)Math.Ceiling(exact);
    }

    private static bool IsAuthoritative(ReconciliationEvidence evidence, out bool settle)
    {
        settle = false;
        if (evidence.AuthoritativeNoCharge && evidence.ActualCostMinorUnits.HasValue)
        {
            return false;
        }

        if (evidence.AuthoritativeNoCharge)
        {
            return true;
        }

        if (!evidence.ActualCostMinorUnits.HasValue || evidence.ActualCostMinorUnits.Value < 0)
        {
            return false;
        }

        settle = true;
        return true;
    }

    private async Task<MonetaryCostLedger> GetOrCreateLedgerAsync(string costMonth, CancellationToken cancellationToken)
    {
        var entry = await database.MonetaryCostLedgers
            .SingleOrDefaultAsync(item => item.CostMonth == costMonth, cancellationToken);
        if (entry is not null)
        {
            return entry;
        }

        entry = new MonetaryCostLedger { CostMonth = costMonth };
        database.MonetaryCostLedgers.Add(entry);
        return entry;
    }

    private static string TariffReference(AttemptCostProfile cost) =>
        string.Concat(cost.TariffSource, "@", cost.TariffCheckDate);

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value.Substring(0, maxLength);

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is SqliteException sqlite
        && sqlite.SqliteErrorCode == 19;

    internal static string MonthString(DateTimeOffset value) =>
        value.UtcDateTime.ToString("yyyy-MM", System.Globalization.CultureInfo.InvariantCulture);

    private static partial class Log
    {
        [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Monetary attempt admitted in cost month {CostMonth}.")]
        public static partial void Admitted(ILogger logger, string costMonth);

        [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Monetary attempt denied over cap in cost month {CostMonth}.")]
        public static partial void DeniedOverCap(ILogger logger, string costMonth);

        [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "Monetary attempt ineligible in cost month {CostMonth}: {Reason}.")]
        public static partial void Ineligible(ILogger logger, string costMonth, string reason);

        [LoggerMessage(EventId = 4, Level = LogLevel.Information, Message = "Monetary attempt duplicate observed in cost month {CostMonth}.")]
        public static partial void DuplicateObserved(ILogger logger, string costMonth);

        [LoggerMessage(EventId = 5, Level = LogLevel.Warning, Message = "Monetary admission denied: invalid monetary configuration for cost month {CostMonth}.")]
        public static partial void ConfigurationInvalid(ILogger logger, string costMonth);

        [LoggerMessage(EventId = 6, Level = LogLevel.Information, Message = "Monetary attempt reconciled in cost month {CostMonth}: {Outcome}.")]
        public static partial void Reconciled(ILogger logger, string costMonth, string outcome);
    }
}
