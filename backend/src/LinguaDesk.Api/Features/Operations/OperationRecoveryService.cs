using LinguaDesk.Api.Infrastructure.Persistence;
using LinguaDesk.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LinguaDesk.Api.Features.Operations;

public enum RecoveryOutcome
{
    Interrupted,
    DuplicateObserved,
    Fenced,
    IdentityConflict,
    UnknownOperation,
}

public sealed record RecoveryResult(
    RecoveryOutcome Outcome,
    OperationSubmission? Submission,
    UsageSnapshotData Usage,
    DateTimeOffset ServerTime);

public sealed partial class OperationRecoveryService(
    LinguaDeskDbContext database,
    TimeProvider timeProvider,
    IOptions<OperationAdmissionOptions> options,
    OperationFingerprintKeyProvider fingerprintKeys,
    ILogger<OperationRecoveryService> logger)
{
    private const int MaxOrphanScanRecords = 100;

    public async Task<RecoveryResult> InterruptAsync(
        string accountId,
        string family,
        string source,
        string? sourceSelection,
        string? target,
        string? mode,
        Guid operationId,
        CancellationToken cancellationToken,
        long? monthlyCapMinorUnits = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(accountId);
        var allowances = options.Value;
        if (allowances.UserDailyAllowanceCharacters <= 0 || allowances.GlobalDailyAllowanceCharacters <= 0)
        {
            throw new InvalidOperationException("Operation allowance configuration must use positive character limits.");
        }

        var effectiveSourceSelection = sourceSelection ?? ProductCatalog.AutomaticSource;
        var effectiveTarget = string.Equals(family, OperationAdmissionService.FamilyTranslation, StringComparison.Ordinal)
            ? target
            : null;
        var effectiveMode = string.Equals(family, OperationAdmissionService.FamilyRewriting, StringComparison.Ordinal)
            ? mode ?? ProductCatalog.DefaultRewritingMode
            : null;
        var fingerprint = OperationFingerprint.Compute(
            await fingerprintKeys.GetKeyAsync(cancellationToken),
            family,
            source,
            effectiveSourceSelection,
            effectiveTarget,
            effectiveMode);
        var operationKey = operationId.ToString("D");

        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        var existing = await database.OperationSubmissions
            .SingleOrDefaultAsync(
                submission => submission.AccountId == accountId && submission.OperationId == operationKey,
                cancellationToken);
        var observed = timeProvider.GetUtcNow();
        if (existing is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return new(
                RecoveryOutcome.UnknownOperation,
                null,
                await SnapshotAsync(accountId, observed, allowances, monthlyCapMinorUnits, cancellationToken),
                observed);
        }

        if (!string.Equals(existing.Fingerprint, fingerprint, StringComparison.Ordinal))
        {
            await transaction.RollbackAsync(cancellationToken);
            return new(
                RecoveryOutcome.IdentityConflict,
                null,
                await SnapshotAsync(accountId, observed, allowances, monthlyCapMinorUnits, cancellationToken),
                observed);
        }

        if (string.Equals(existing.State, OperationStates.Interrupted, StringComparison.Ordinal))
        {
            await transaction.RollbackAsync(cancellationToken);
            Log.Duplicate(logger, existing.ScalarCount);
            return new(
                RecoveryOutcome.DuplicateObserved,
                existing,
                await SnapshotAsync(accountId, observed, allowances, monthlyCapMinorUnits, cancellationToken),
                observed);
        }

        if (!string.Equals(existing.State, OperationStates.Pending, StringComparison.Ordinal))
        {
            await transaction.RollbackAsync(cancellationToken);
            Log.Fenced(logger, existing.ScalarCount, existing.State);
            return new(
                RecoveryOutcome.Fenced,
                existing,
                await SnapshotAsync(accountId, observed, allowances, monthlyCapMinorUnits, cancellationToken),
                observed);
        }

        var settledAt = timeProvider.GetUtcNow();
        var claimed = await database.OperationSubmissions
            .Where(submission =>
                submission.AccountId == accountId
                && submission.OperationId == operationKey
                && submission.State == OperationStates.Pending)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(submission => submission.State, OperationStates.Interrupted)
                    .SetProperty(submission => submission.SettledUtc, settledAt),
                cancellationToken);
        if (claimed == 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            database.ChangeTracker.Clear();
            return await RereadAfterRaceAsync(accountId, operationKey, fingerprint, allowances, monthlyCapMinorUnits, cancellationToken);
        }

        await ReleaseReservationAsync(existing, cancellationToken);
        existing.State = OperationStates.Interrupted;
        existing.SettledUtc = settledAt;
        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var committed = timeProvider.GetUtcNow();
        var revision = await CurrentRevisionAsync(cancellationToken).ConfigureAwait(false);
        Log.Interrupted(logger, existing.ScalarCount, existing.AdmissionDay, revision);
        return new(
            RecoveryOutcome.Interrupted,
            existing,
            await SnapshotAsync(accountId, committed, allowances, monthlyCapMinorUnits, cancellationToken),
            committed);
    }

    public async Task<int> ReconcileOrphansAsync(CancellationToken cancellationToken, int maxRecords = MaxOrphanScanRecords)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxRecords);
        // SQLite stores deadlines as text and rejects DateTimeOffset in WHERE and
        // ORDER BY, so the scan pages pending rows in insertion order and applies
        // the ascending-deadline expiry filter client-side; overflow waits for a
        // later scan.
        var now = timeProvider.GetUtcNow();
        var candidates = await database.OperationSubmissions
            .AsNoTracking()
            .Where(submission => submission.State == OperationStates.Pending)
            .OrderBy(submission => submission.Id)
            .Take(maxRecords)
            .Select(submission => new { submission.AccountId, submission.OperationId, submission.DeadlineUtc })
            .ToListAsync(cancellationToken);
        var interrupted = 0;
        foreach (var candidate in candidates
            .Where(candidate => candidate.DeadlineUtc <= now)
            .OrderBy(candidate => candidate.DeadlineUtc))
        {
            if (await InterruptRecordAsync(candidate.AccountId, candidate.OperationId, cancellationToken))
            {
                interrupted++;
            }
        }

        return interrupted;
    }

    private async Task<bool> InterruptRecordAsync(string accountId, string operationKey, CancellationToken cancellationToken)
    {
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        var existing = await database.OperationSubmissions
            .SingleOrDefaultAsync(
                submission => submission.AccountId == accountId && submission.OperationId == operationKey,
                cancellationToken);
        if (existing is null || !string.Equals(existing.State, OperationStates.Pending, StringComparison.Ordinal))
        {
            await transaction.RollbackAsync(cancellationToken);
            database.ChangeTracker.Clear();
            return false;
        }

        var settledAt = timeProvider.GetUtcNow();
        var claimed = await database.OperationSubmissions
            .Where(submission =>
                submission.AccountId == accountId
                && submission.OperationId == operationKey
                && submission.State == OperationStates.Pending)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(submission => submission.State, OperationStates.Interrupted)
                    .SetProperty(submission => submission.SettledUtc, settledAt),
                cancellationToken);
        if (claimed == 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            database.ChangeTracker.Clear();
            return false;
        }

        await ReleaseReservationAsync(existing, cancellationToken);
        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        database.ChangeTracker.Clear();

        var revision = await CurrentRevisionAsync(cancellationToken).ConfigureAwait(false);
        Log.Interrupted(logger, existing.ScalarCount, existing.AdmissionDay, revision);
        return true;
    }

    private async Task ReleaseReservationAsync(OperationSubmission existing, CancellationToken cancellationToken)
    {
        var scalarCount = existing.ScalarCount;
        var admissionDay = existing.AdmissionDay;
        var userLedger = await GetOrCreateLedgerAsync(existing.AccountId, CharacterLedgerScopes.User, admissionDay, cancellationToken);
        var globalLedger = await GetOrCreateLedgerAsync(CharacterLedgerScopes.GlobalAccountId, CharacterLedgerScopes.Global, admissionDay, cancellationToken);
        var revision = await GetOrCreateRevisionAsync(cancellationToken);
        userLedger.ReservedCharacters -= scalarCount;
        globalLedger.ReservedCharacters -= scalarCount;
        revision.Value++;
    }

    private async Task<RecoveryResult> RereadAfterRaceAsync(
        string accountId,
        string operationKey,
        string fingerprint,
        OperationAdmissionOptions allowances,
        long? monthlyCapMinorUnits,
        CancellationToken cancellationToken)
    {
        var reread = await database.OperationSubmissions
            .AsNoTracking()
            .SingleOrDefaultAsync(
                submission => submission.AccountId == accountId && submission.OperationId == operationKey,
                cancellationToken);
        var observed = timeProvider.GetUtcNow();
        var usage = await SnapshotAsync(accountId, observed, allowances, monthlyCapMinorUnits, cancellationToken);
        if (reread is null)
        {
            return new(RecoveryOutcome.UnknownOperation, null, usage, observed);
        }

        if (!string.Equals(reread.Fingerprint, fingerprint, StringComparison.Ordinal))
        {
            return new(RecoveryOutcome.IdentityConflict, null, usage, observed);
        }

        return string.Equals(reread.State, OperationStates.Interrupted, StringComparison.Ordinal)
            ? new(RecoveryOutcome.DuplicateObserved, reread, usage, observed)
            : new(RecoveryOutcome.Fenced, reread, usage, observed);
    }

    private Task<UsageSnapshotData> SnapshotAsync(
        string accountId,
        DateTimeOffset serverTime,
        OperationAdmissionOptions allowances,
        long? monthlyCapMinorUnits,
        CancellationToken cancellationToken) =>
        LedgerSnapshot.ReadAsync(
            database,
            accountId,
            OperationAdmissionService.DayString(serverTime),
            MonetaryAdmissionService.MonthString(serverTime),
            serverTime,
            allowances.UserDailyAllowanceCharacters,
            allowances.GlobalDailyAllowanceCharacters,
            monthlyCapMinorUnits ?? 0,
            cancellationToken);

    private async Task<long> CurrentRevisionAsync(CancellationToken cancellationToken)
    {
        var revision = await database.LedgerRevisions
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == LedgerRevision.SingletonId, cancellationToken);
        return revision?.Value ?? 0;
    }

    private async Task<CharacterLedgerEntry> GetOrCreateLedgerAsync(
        string accountId,
        string scope,
        string day,
        CancellationToken cancellationToken)
    {
        var entry = await database.CharacterLedgerEntries
            .SingleOrDefaultAsync(item => item.Scope == scope && item.AccountId == accountId && item.Day == day, cancellationToken);
        if (entry is not null)
        {
            return entry;
        }

        entry = new CharacterLedgerEntry { Scope = scope, AccountId = accountId, Day = day };
        database.CharacterLedgerEntries.Add(entry);
        return entry;
    }

    private async Task<LedgerRevision> GetOrCreateRevisionAsync(CancellationToken cancellationToken)
    {
        var revision = await database.LedgerRevisions
            .SingleOrDefaultAsync(item => item.Id == LedgerRevision.SingletonId, cancellationToken);
        if (revision is not null)
        {
            return revision;
        }

        revision = new LedgerRevision { Id = LedgerRevision.SingletonId };
        database.LedgerRevisions.Add(revision);
        return revision;
    }

    private static partial class Log
    {
        [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Operation interrupted: {ScalarCount} characters released on {AdmissionDay} at revision {Revision}.")]
        public static partial void Interrupted(ILogger logger, int scalarCount, string admissionDay, long revision);

        [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Duplicate interrupt observed: {ScalarCount} characters already interrupted.")]
        public static partial void Duplicate(ILogger logger, int scalarCount);

        [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "Interrupt fenced: {ScalarCount} characters already {CurrentState}, interrupt refused.")]
        public static partial void Fenced(ILogger logger, int scalarCount, string currentState);
    }
}
