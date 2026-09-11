using LinguaDesk.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LinguaDesk.Api.Features.Operations;

internal static class LedgerSnapshot
{
    internal static async Task<UsageSnapshotData> ReadAsync(
        LinguaDeskDbContext database,
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
            OperationAdmissionService.NextMidnightUtc(serverTime),
            consumed,
            reserved,
            allowance,
            Math.Max(0, allowance - consumed - reserved),
            revision?.Value ?? 0);
    }
}
