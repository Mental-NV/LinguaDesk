using Microsoft.Data.Sqlite;

namespace LinguaDesk.Api.Infrastructure.Persistence;

public sealed class StorageConnectionPolicy(StorageDatabaseTarget target)
{
    public const int DefaultTimeoutSeconds = 5;

    public StorageDatabaseTarget Target { get; } = target;

    public string CreateRuntimeConnectionString() => CreateConnectionString(SqliteOpenMode.ReadWrite);

    public string CreateMigrationConnectionString() => CreateConnectionString(SqliteOpenMode.ReadWriteCreate);

    private string CreateConnectionString(SqliteOpenMode mode) => new SqliteConnectionStringBuilder
    {
        DataSource = Target.DatabasePath,
        Mode = mode,
        ForeignKeys = true,
        DefaultTimeout = DefaultTimeoutSeconds,
        Pooling = false,
    }.ToString();
}
