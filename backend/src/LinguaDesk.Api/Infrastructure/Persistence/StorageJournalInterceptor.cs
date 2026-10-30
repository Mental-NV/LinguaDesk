using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace LinguaDesk.Api.Infrastructure.Persistence;

public sealed class StorageJournalInterceptor : DbConnectionInterceptor
{
    private readonly string expected;

    public StorageJournalInterceptor(string mode)
    {
        expected = mode.ToUpperInvariant() switch
        {
            "WAL" => "wal",
            "DELETE" => "delete",
            _ => throw new InvalidOperationException("Storage:JournalMode must be Wal or Delete."),
        };
    }

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData) => Validate(connection);

    public override Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        Validate(connection);
        return Task.CompletedTask;
    }

    private void Validate(DbConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA journal_mode;";
        if (!string.Equals(command.ExecuteScalar()?.ToString(), expected, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Database journal mode differs from the configured storage profile. Run deployment preparation while stopped.");
        }
        command.CommandText = "PRAGMA synchronous=FULL; PRAGMA busy_timeout=5000; PRAGMA foreign_keys=ON;";
        command.ExecuteNonQuery();
    }
}
