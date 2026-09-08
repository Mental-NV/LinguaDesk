using LinguaDesk.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LinguaDesk.Api.Tests;

internal sealed class StorageTestDatabase : IAsyncDisposable
{
    private StorageTestDatabase(string rootPath)
    {
        RootPath = rootPath;
        DatabasePath = Path.Combine(rootPath, "database with spaces.db");
        var repositoryRoot = StoragePathPolicy.FindRepositoryRoot(AppContext.BaseDirectory)
            ?? throw new InvalidOperationException("Could not locate the repository root for storage tests.");
        ContentRootPath = Path.Combine(repositoryRoot, "backend", "src", "LinguaDesk.Api");
        Target = StoragePathPolicy.Resolve(DatabasePath, ContentRootPath);
        ConnectionPolicy = new StorageConnectionPolicy(Target);
    }

    public string ContentRootPath { get; }

    public StorageConnectionPolicy ConnectionPolicy { get; }

    public string DatabasePath { get; }

    public string RootPath { get; }

    public StorageDatabaseTarget Target { get; }

    public static StorageTestDatabase Create() =>
        new(Directory.CreateTempSubdirectory("linguadesk-storage-").FullName);

    public LinguaDeskDbContext CreateContext(bool allowCreate = false)
    {
        var connectionString = allowCreate
            ? ConnectionPolicy.CreateMigrationConnectionString()
            : ConnectionPolicy.CreateRuntimeConnectionString();
        var options = new DbContextOptionsBuilder<LinguaDeskDbContext>()
            .UseSqlite(
                connectionString,
                sqlite => sqlite.MigrationsAssembly(typeof(LinguaDeskDbContext).Assembly.GetName().Name))
            .Options;
        return new LinguaDeskDbContext(options);
    }

    public async Task MigrateAsync()
    {
        await using var context = CreateContext(allowCreate: true);
        await context.Database.MigrateAsync();
    }

    public async Task CreateProbeSchemaAsync()
    {
        await using var context = CreateContext();
        await context.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE probe_parent (id INTEGER PRIMARY KEY);
            CREATE TABLE probe_child (
                id INTEGER PRIMARY KEY,
                parent_id INTEGER NOT NULL,
                value TEXT NOT NULL,
                FOREIGN KEY (parent_id) REFERENCES probe_parent(id));
            CREATE TABLE probe_value (id INTEGER PRIMARY KEY, value TEXT NOT NULL);
            INSERT INTO probe_parent (id) VALUES (1);
            """);
    }

    public ValueTask DisposeAsync()
    {
        if (Directory.Exists(RootPath))
        {
            Directory.Delete(RootPath, recursive: true);
        }

        return ValueTask.CompletedTask;
    }
}
