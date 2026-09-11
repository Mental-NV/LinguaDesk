using LinguaDesk.Api.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LinguaDesk.Api.Features.Operations;

internal static class LedgerSnapshot
{
    internal static async Task<UsageSnapshotData> ReadAsync(
        LinguaDeskDbContext database,
        string accountId,
        string day,
        string costMonth,
        DateTimeOffset serverTime,
        int userAllowance,
        int globalAllowance,
        long monthlyCapMinorUnits,
        CancellationToken cancellationToken)
    {
        var resetAtUtc = OperationAdmissionService.NextMidnightUtc(serverTime);
        try
        {
            var userEntry = await database.CharacterLedgerEntries
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item => item.Scope == CharacterLedgerScopes.User && item.AccountId == accountId && item.Day == day,
                    cancellationToken);
            var globalEntry = await database.CharacterLedgerEntries
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item => item.Scope == CharacterLedgerScopes.Global
                        && item.AccountId == CharacterLedgerScopes.GlobalAccountId
                        && item.Day == day,
                    cancellationToken);
            var revision = await database.LedgerRevisions
                .AsNoTracking()
                .SingleOrDefaultAsync(item => item.Id == LedgerRevision.SingletonId, cancellationToken);
            var costLedgers = await database.MonetaryCostLedgers
                .AsNoTracking()
                .ToListAsync(cancellationToken);
            var consumed = userEntry?.ConsumedCharacters ?? 0;
            var reserved = userEntry?.ReservedCharacters ?? 0;
            var globalConsumed = globalEntry?.ConsumedCharacters ?? 0;
            var globalReserved = globalEntry?.ReservedCharacters ?? 0;
            var currentKnownSpend = costLedgers
                .Where(entry => string.Equals(entry.CostMonth, costMonth, StringComparison.Ordinal))
                .Sum(entry => entry.KnownSpendMinorUnits);
            var totalUnresolved = costLedgers.Sum(entry => entry.UnresolvedExposureMinorUnits);
            return new(
                day,
                resetAtUtc,
                consumed,
                reserved,
                userAllowance,
                Math.Max(0, userAllowance - consumed - reserved),
                revision?.Value ?? 0,
                Classify(
                    consumed,
                    reserved,
                    userAllowance,
                    globalConsumed,
                    globalReserved,
                    globalAllowance,
                    currentKnownSpend,
                    totalUnresolved,
                    monthlyCapMinorUnits));
        }
        catch (Exception exception) when (exception is SqliteException or DbUpdateException)
        {
            return new(day, resetAtUtc, 0, 0, userAllowance, 0, 0, UsageAvailability.Unavailable);
        }
    }

    internal static UsageAvailability Classify(
        int consumed,
        int reserved,
        int userAllowance,
        int globalConsumed,
        int globalReserved,
        int globalAllowance,
        long currentKnownSpendMinorUnits,
        long totalUnresolvedMinorUnits,
        long monthlyCapMinorUnits)
    {
        if ((long)consumed + reserved >= userAllowance)
        {
            return UsageAvailability.UserExhausted;
        }

        if ((long)globalConsumed + globalReserved >= globalAllowance)
        {
            return UsageAvailability.ServiceExhausted;
        }

        if (monthlyCapMinorUnits > 0
            && currentKnownSpendMinorUnits + totalUnresolvedMinorUnits >= monthlyCapMinorUnits)
        {
            return UsageAvailability.MonetarySuspended;
        }

        return UsageAvailability.Available;
    }

    internal static long ResolveCapOrZero(IOptions<MonetaryAdmissionOptions> monetaryOptions)
    {
        try
        {
            var cap = monetaryOptions.Value.MonthlyCapMinorUnits;
            return cap > 0 ? cap : 0;
        }
        catch (OptionsValidationException)
        {
            return 0;
        }
    }
}
