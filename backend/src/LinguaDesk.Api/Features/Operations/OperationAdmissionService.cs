using LinguaDesk.Api.Infrastructure.Persistence;
using LinguaDesk.Core;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LinguaDesk.Api.Features.Operations;

public enum AdmissionOutcome
{
    Admitted,
    DuplicateObserved,
    IdentityConflict,
    IdentityExpired,
    InputIneligible,
    InsufficientUserCapacity,
    InsufficientGlobalCapacity,
}

public sealed record UsageSnapshotData(
    string Day,
    DateTimeOffset ResetAtUtc,
    int ConsumedCharacters,
    int ReservedCharacters,
    int AllowanceCharacters,
    int AvailableCharacters,
    long Revision);

public sealed record AdmissionResult(
    AdmissionOutcome Outcome,
    OperationSubmission? Submission,
    UsageSnapshotData Usage,
    DateTimeOffset ServerTime,
    string? EligibilityReason = null,
    int? CharacterCount = null,
    int? SourceLimit = null);

public sealed partial class OperationAdmissionService(
    LinguaDeskDbContext database,
    TimeProvider timeProvider,
    IOptions<OperationAdmissionOptions> options,
    OperationFingerprintKeyProvider fingerprintKeys,
    ILogger<OperationAdmissionService> logger)
{
    public const string FamilyTranslation = "translation";
    public const string FamilyRewriting = "rewriting";

    public async Task<AdmissionResult> AdmitAsync(
        string accountId,
        string family,
        string source,
        string? sourceSelection,
        string? target,
        string? mode,
        Guid operationId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(accountId);
        var allowances = options.Value;
        if (allowances.UserDailyAllowanceCharacters <= 0 || allowances.GlobalDailyAllowanceCharacters <= 0)
        {
            throw new InvalidOperationException("Operation allowance configuration must use positive character limits.");
        }

        var now = timeProvider.GetUtcNow();
        var identityTime = OperationIdentity.ToIdentityTime(operationId);
        var effectiveSourceSelection = sourceSelection ?? ProductCatalog.AutomaticSource;
        var maximumSourceCharacters = string.Equals(family, FamilyRewriting, StringComparison.Ordinal)
            ? ProductCatalog.RewritingMaximumSourceCharacters
            : ProductCatalog.TranslationMaximumSourceCharacters;

        var ineligible = AssessEligibility(family, source, effectiveSourceSelection, target, mode, maximumSourceCharacters);
        if (ineligible is not null)
        {
            return new(
                AdmissionOutcome.InputIneligible,
                null,
                await SnapshotAsync(accountId, DayString(now), now, allowances.UserDailyAllowanceCharacters, cancellationToken),
                now,
                ineligible.Value.Reason,
                ineligible.Value.CharacterCount,
                maximumSourceCharacters);
        }

        var effectiveTarget = string.Equals(family, FamilyTranslation, StringComparison.Ordinal) ? target : null;
        var effectiveMode = string.Equals(family, FamilyRewriting, StringComparison.Ordinal)
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

        var existing = await database.OperationSubmissions
            .AsNoTracking()
            .SingleOrDefaultAsync(
                submission => submission.AccountId == accountId && submission.OperationId == operationKey,
                cancellationToken);
        if (existing is not null)
        {
            return string.Equals(existing.Fingerprint, fingerprint, StringComparison.Ordinal)
                ? new(
                    AdmissionOutcome.DuplicateObserved,
                    existing,
                    await SnapshotAsync(accountId, DayString(now), now, allowances.UserDailyAllowanceCharacters, cancellationToken),
                    now)
                : new(
                    AdmissionOutcome.IdentityConflict,
                    null,
                    await SnapshotAsync(accountId, DayString(now), now, allowances.UserDailyAllowanceCharacters, cancellationToken),
                    now);
        }

        var analysis = ScalarInputPolicy.Analyze(source, maximumSourceCharacters);
        var scalarCount = analysis.ScalarCount ?? 0;

        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var transactionTime = timeProvider.GetUtcNow();
            if (transactionTime >= identityTime.AddSeconds(ProductCatalog.OperationIdentityValidForSeconds))
            {
                await transaction.RollbackAsync(cancellationToken);
                return new(
                    AdmissionOutcome.IdentityExpired,
                    null,
                    await SnapshotAsync(accountId, DayString(transactionTime), transactionTime, allowances.UserDailyAllowanceCharacters, cancellationToken),
                    transactionTime);
            }

            var admissionDay = DayString(transactionTime);
            var userLedger = await GetOrCreateLedgerAsync(accountId, CharacterLedgerScopes.User, admissionDay, cancellationToken);
            var globalLedger = await GetOrCreateLedgerAsync(CharacterLedgerScopes.GlobalAccountId, CharacterLedgerScopes.Global, admissionDay, cancellationToken);
            var revision = await GetOrCreateRevisionAsync(cancellationToken);

            if ((long)userLedger.ConsumedCharacters + userLedger.ReservedCharacters + scalarCount > allowances.UserDailyAllowanceCharacters)
            {
                await transaction.RollbackAsync(cancellationToken);
                Log.UserCapacityDenied(logger, scalarCount);
                return new(
                    AdmissionOutcome.InsufficientUserCapacity,
                    null,
                    await SnapshotAsync(accountId, admissionDay, transactionTime, allowances.UserDailyAllowanceCharacters, cancellationToken),
                    transactionTime,
                    CharacterCount: scalarCount,
                    SourceLimit: allowances.UserDailyAllowanceCharacters);
            }

            if ((long)globalLedger.ConsumedCharacters + globalLedger.ReservedCharacters + scalarCount > allowances.GlobalDailyAllowanceCharacters)
            {
                await transaction.RollbackAsync(cancellationToken);
                Log.GlobalCapacityDenied(logger, scalarCount);
                return new(
                    AdmissionOutcome.InsufficientGlobalCapacity,
                    null,
                    await SnapshotAsync(accountId, admissionDay, transactionTime, allowances.UserDailyAllowanceCharacters, cancellationToken),
                    transactionTime,
                    CharacterCount: scalarCount,
                    SourceLimit: allowances.GlobalDailyAllowanceCharacters);
            }

            var submission = new OperationSubmission
            {
                AccountId = accountId,
                OperationId = operationKey,
                Family = family,
                Fingerprint = fingerprint,
                ScalarCount = scalarCount,
                AdmissionDay = admissionDay,
                DeadlineUtc = transactionTime.AddSeconds(ProductCatalog.OverallDeadlineSeconds),
                State = OperationStates.Pending,
                CreatedUtc = transactionTime,
            };
            database.OperationSubmissions.Add(submission);
            userLedger.ReservedCharacters += scalarCount;
            globalLedger.ReservedCharacters += scalarCount;
            revision.Value++;
            await database.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            Log.Admitted(logger, scalarCount, admissionDay, revision.Value);
            return new(
                AdmissionOutcome.Admitted,
                submission,
                await SnapshotAsync(accountId, admissionDay, transactionTime, allowances.UserDailyAllowanceCharacters, cancellationToken),
                transactionTime,
                CharacterCount: scalarCount);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            await transaction.RollbackAsync(cancellationToken);
            database.ChangeTracker.Clear();
            var raced = await database.OperationSubmissions
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    submission => submission.AccountId == accountId && submission.OperationId == operationKey,
                    cancellationToken);
            var rereadTime = timeProvider.GetUtcNow();
            if (raced is not null && string.Equals(raced.Fingerprint, fingerprint, StringComparison.Ordinal))
            {
                return new(
                    AdmissionOutcome.DuplicateObserved,
                    raced,
                    await SnapshotAsync(accountId, DayString(rereadTime), rereadTime, allowances.UserDailyAllowanceCharacters, cancellationToken),
                    rereadTime);
            }

            return new(
                AdmissionOutcome.IdentityConflict,
                null,
                await SnapshotAsync(accountId, DayString(rereadTime), rereadTime, allowances.UserDailyAllowanceCharacters, cancellationToken),
                rereadTime);
        }
    }

    public async Task<(OperationSubmission? Submission, UsageSnapshotData Usage, DateTimeOffset ServerTime)> GetAsync(
        string accountId,
        Guid operationId,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var submission = await database.OperationSubmissions
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.AccountId == accountId && item.OperationId == operationId.ToString("D"),
                cancellationToken);
        return (
            submission,
            await SnapshotAsync(accountId, DayString(now), now, options.Value.UserDailyAllowanceCharacters, cancellationToken),
            now);
    }

    private static (string Reason, int? CharacterCount)? AssessEligibility(
        string family,
        string source,
        string effectiveSourceSelection,
        string? target,
        string? mode,
        int maximumSourceCharacters)
    {
        if (string.Equals(family, FamilyTranslation, StringComparison.Ordinal))
        {
            var validation = ScalarInputPolicy.ValidateTranslation(
                source,
                maximumSourceCharacters,
                effectiveSourceSelection,
                target);
            return ToReason(validation.Input, validation.IsSourceSelectionValid, validation.IsTargetValid, validation.IsDistinctSelection, target is null, null);
        }

        if (string.Equals(family, FamilyRewriting, StringComparison.Ordinal))
        {
            var validation = ScalarInputPolicy.ValidateRewriting(
                source,
                maximumSourceCharacters,
                effectiveSourceSelection,
                mode);
            return ToReason(validation.Input, validation.IsSourceSelectionValid, true, true, false, validation.IsModeValid ? null : "invalidMode");
        }

        return null;
    }

    private static (string Reason, int? CharacterCount)? ToReason(
        InputAnalysis input,
        bool isSourceSelectionValid,
        bool isTargetValid,
        bool isDistinctSelection,
        bool isTargetMissing,
        string? modeReason)
    {
        if (!input.IsUnicodeValid)
        {
            return ("invalidUnicode", null);
        }

        if (input.IsEmptyOrWhitespace)
        {
            return ("emptySource", 0);
        }

        if (input.IsOversized)
        {
            return ("oversizedSource", input.ScalarCount);
        }

        if (!isSourceSelectionValid)
        {
            return ("invalidSourceSelection", input.ScalarCount);
        }

        if (isTargetMissing)
        {
            return ("targetRequired", input.ScalarCount);
        }

        if (!isTargetValid)
        {
            return ("invalidTarget", input.ScalarCount);
        }

        if (!isDistinctSelection)
        {
            return ("sameLanguage", input.ScalarCount);
        }

        return modeReason is null ? null : (modeReason, input.ScalarCount);
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

    private async Task<UsageSnapshotData> SnapshotAsync(
        string accountId,
        string day,
        DateTimeOffset serverTime,
        int allowance,
        CancellationToken cancellationToken)
    {
        var entry = await database.CharacterLedgerEntries
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.Scope == CharacterLedgerScopes.User && item.AccountId == accountId && item.Day == day,
                cancellationToken);
        var revision = await database.LedgerRevisions
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == LedgerRevision.SingletonId, cancellationToken);
        var consumed = entry?.ConsumedCharacters ?? 0;
        var reserved = entry?.ReservedCharacters ?? 0;
        return new(
            day,
            NextMidnightUtc(serverTime),
            consumed,
            reserved,
            allowance,
            Math.Max(0, allowance - consumed - reserved),
            revision?.Value ?? 0);
    }

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is SqliteException sqlite
        && sqlite.SqliteErrorCode == 19;

    internal static string DayString(DateTimeOffset value) =>
        value.UtcDateTime.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

    private static partial class Log
    {
        [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Operation admission denied for user capacity: {ScalarCount} characters requested.")]
        public static partial void UserCapacityDenied(ILogger logger, int scalarCount);

        [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Operation admission denied for global capacity: {ScalarCount} characters requested.")]
        public static partial void GlobalCapacityDenied(ILogger logger, int scalarCount);

        [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "Operation admitted: {ScalarCount} characters reserved on {AdmissionDay} at revision {Revision}.")]
        public static partial void Admitted(ILogger logger, int scalarCount, string admissionDay, long revision);
    }

    internal static DateTimeOffset NextMidnightUtc(DateTimeOffset value) =>
        new(new DateOnly(value.UtcDateTime.Year, value.UtcDateTime.Month, value.UtcDateTime.Day)
            .ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
            .AddDays(1));
}
