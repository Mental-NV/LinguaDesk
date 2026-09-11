using LinguaDesk.Api.Infrastructure.Persistence;
using LinguaDesk.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LinguaDesk.Api.Features.Operations;

public enum SettlementOutcome
{
    Settled,
    DuplicateObserved,
    Fenced,
    IdentityConflict,
    UnknownOperation,
}

public sealed record SettlementResult(
    SettlementOutcome Outcome,
    OperationSubmission? Submission,
    UsageSnapshotData Usage,
    DateTimeOffset ServerTime);

public sealed partial class OperationSettlementService(
    LinguaDeskDbContext database,
    TimeProvider timeProvider,
    IOptions<OperationAdmissionOptions> options,
    OperationFingerprintKeyProvider fingerprintKeys,
    ILogger<OperationSettlementService> logger)
{
    public Task<SettlementResult> SettleSuccessAsync(
        string accountId,
        string family,
        string source,
        string? sourceSelection,
        string? target,
        string? mode,
        Guid operationId,
        CancellationToken cancellationToken) =>
        SettleAsync(accountId, family, source, sourceSelection, target, mode, operationId, succeed: true, cancellationToken);

    public Task<SettlementResult> SettleFailureAsync(
        string accountId,
        string family,
        string source,
        string? sourceSelection,
        string? target,
        string? mode,
        Guid operationId,
        CancellationToken cancellationToken) =>
        SettleAsync(accountId, family, source, sourceSelection, target, mode, operationId, succeed: false, cancellationToken);

    private async Task<SettlementResult> SettleAsync(
        string accountId,
        string family,
        string source,
        string? sourceSelection,
        string? target,
        string? mode,
        Guid operationId,
        bool succeed,
        CancellationToken cancellationToken)
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
        var targetState = succeed ? OperationStates.Succeeded : OperationStates.Failed;

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
                SettlementOutcome.UnknownOperation,
                null,
                await SnapshotAsync(accountId, observed, allowances.UserDailyAllowanceCharacters, cancellationToken),
                observed);
        }

        if (!string.Equals(existing.Fingerprint, fingerprint, StringComparison.Ordinal))
        {
            await transaction.RollbackAsync(cancellationToken);
            return new(
                SettlementOutcome.IdentityConflict,
                null,
                await SnapshotAsync(accountId, observed, allowances.UserDailyAllowanceCharacters, cancellationToken),
                observed);
        }

        if (string.Equals(existing.State, targetState, StringComparison.Ordinal))
        {
            await transaction.RollbackAsync(cancellationToken);
            Log.Duplicate(logger, existing.ScalarCount, targetState);
            return new(
                SettlementOutcome.DuplicateObserved,
                existing,
                await SnapshotAsync(accountId, observed, allowances.UserDailyAllowanceCharacters, cancellationToken),
                observed);
        }

        if (!string.Equals(existing.State, OperationStates.Pending, StringComparison.Ordinal))
        {
            await transaction.RollbackAsync(cancellationToken);
            Log.Fenced(logger, existing.ScalarCount, existing.State, targetState);
            return new(
                SettlementOutcome.Fenced,
                existing,
                await SnapshotAsync(accountId, observed, allowances.UserDailyAllowanceCharacters, cancellationToken),
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
                    .SetProperty(submission => submission.State, targetState)
                    .SetProperty(submission => submission.SettledUtc, settledAt),
                cancellationToken);
        if (claimed == 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            database.ChangeTracker.Clear();
            return await RereadAfterRaceAsync(accountId, operationKey, fingerprint, targetState, allowances.UserDailyAllowanceCharacters, cancellationToken);
        }

        var scalarCount = existing.ScalarCount;
        var admissionDay = existing.AdmissionDay;
        var userLedger = await GetOrCreateLedgerAsync(accountId, CharacterLedgerScopes.User, admissionDay, cancellationToken);
        var globalLedger = await GetOrCreateLedgerAsync(CharacterLedgerScopes.GlobalAccountId, CharacterLedgerScopes.Global, admissionDay, cancellationToken);
        var revision = await GetOrCreateRevisionAsync(cancellationToken);
        if (succeed)
        {
            userLedger.ConsumedCharacters += scalarCount;
            userLedger.ReservedCharacters -= scalarCount;
            globalLedger.ConsumedCharacters += scalarCount;
            globalLedger.ReservedCharacters -= scalarCount;
        }
        else
        {
            userLedger.ReservedCharacters -= scalarCount;
            globalLedger.ReservedCharacters -= scalarCount;
        }

        revision.Value++;
        existing.State = targetState;
        existing.SettledUtc = settledAt;
        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var committed = timeProvider.GetUtcNow();
        Log.Settled(logger, scalarCount, admissionDay, targetState, revision.Value);
        return new(
            SettlementOutcome.Settled,
            existing,
            await SnapshotAsync(accountId, committed, allowances.UserDailyAllowanceCharacters, cancellationToken),
            committed);
    }

    private async Task<SettlementResult> RereadAfterRaceAsync(
        string accountId,
        string operationKey,
        string fingerprint,
        string targetState,
        int allowance,
        CancellationToken cancellationToken)
    {
        var reread = await database.OperationSubmissions
            .AsNoTracking()
            .SingleOrDefaultAsync(
                submission => submission.AccountId == accountId && submission.OperationId == operationKey,
                cancellationToken);
        var observed = timeProvider.GetUtcNow();
        var usage = await SnapshotAsync(accountId, observed, allowance, cancellationToken);
        if (reread is null)
        {
            return new(SettlementOutcome.UnknownOperation, null, usage, observed);
        }

        if (!string.Equals(reread.Fingerprint, fingerprint, StringComparison.Ordinal))
        {
            return new(SettlementOutcome.IdentityConflict, null, usage, observed);
        }

        return string.Equals(reread.State, targetState, StringComparison.Ordinal)
            ? new(SettlementOutcome.DuplicateObserved, reread, usage, observed)
            : new(SettlementOutcome.Fenced, reread, usage, observed);
    }

    private Task<UsageSnapshotData> SnapshotAsync(
        string accountId,
        DateTimeOffset serverTime,
        int allowance,
        CancellationToken cancellationToken) =>
        LedgerSnapshot.ReadAsync(database, accountId, OperationAdmissionService.DayString(serverTime), serverTime, allowance, cancellationToken);

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
        [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Operation settled: {ScalarCount} characters {TargetState} on {AdmissionDay} at revision {Revision}.")]
        public static partial void Settled(ILogger logger, int scalarCount, string admissionDay, string targetState, long revision);

        [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Duplicate settlement observed: {ScalarCount} characters already {TargetState}.")]
        public static partial void Duplicate(ILogger logger, int scalarCount, string targetState);

        [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "Late settlement fenced: {ScalarCount} characters already {CurrentState}, {TargetState} refused.")]
        public static partial void Fenced(ILogger logger, int scalarCount, string currentState, string targetState);
    }
}
