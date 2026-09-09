using System.Diagnostics;
using LinguaDesk.Api.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace LinguaDesk.Api.Tests;

[TestClass]
public sealed class StorageMigrationTests
{
    private const string InitialMigrationId = "20260908221711_InitialStorage";
    private const string LocalAccountsMigrationId = "20260909120834_LocalAccounts";

    [TestMethod]
    public async Task ModelMatchesSnapshotWithoutOpeningOrCreatingDatabase()
    {
        await using var database = StorageTestDatabase.Create();
        await using var context = database.CreateContext(allowCreate: true);

        Assert.IsFalse(context.Database.HasPendingModelChanges());
        Assert.IsFalse(File.Exists(database.DatabasePath));
    }

    [TestMethod]
    public async Task FreshAndRepeatedMigrationsPreserveHistoryAndSyntheticData()
    {
        await using var database = StorageTestDatabase.Create();
        await database.MigrateAsync();

        await using (var context = database.CreateContext())
        {
            var applied = await context.Database.GetAppliedMigrationsAsync();
            CollectionAssert.AreEqual(new[] { InitialMigrationId, LocalAccountsMigrationId }, applied.ToArray());
            Assert.AreEqual(
                4L,
                await ExecuteScalarAsync<long>(
                    context,
                    "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name LIKE 'AspNet%';"));
            Assert.AreEqual(
                0L,
                await ExecuteScalarAsync<long>(
                    context,
                    "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name LIKE 'AspNetRole%';"));
            await context.Database.ExecuteSqlRawAsync(
                "CREATE TABLE test_fixture (id INTEGER PRIMARY KEY, value TEXT NOT NULL);");
            await context.Database.ExecuteSqlRawAsync(
                "INSERT INTO test_fixture (id, value) VALUES (1, 'preserved');");
        }

        await database.MigrateAsync();

        await using var verification = database.CreateContext();
        var repeatedHistory = await verification.Database.GetAppliedMigrationsAsync();
        CollectionAssert.AreEqual(new[] { InitialMigrationId, LocalAccountsMigrationId }, repeatedHistory.ToArray());
        Assert.AreEqual("preserved", await ExecuteScalarAsync<string>(verification, "SELECT value FROM test_fixture WHERE id = 1;"));
    }

    [TestMethod]
    public async Task UpgradeFromInitialStoragePreservesExistingDataAndAddsOnlyUserIdentityTables()
    {
        await using var database = StorageTestDatabase.Create();
        await using (var initial = database.CreateContext(allowCreate: true))
        {
            await initial.GetService<IMigrator>().MigrateAsync(InitialMigrationId);
            await initial.Database.ExecuteSqlRawAsync(
                "CREATE TABLE upgrade_fixture (id INTEGER PRIMARY KEY, value TEXT NOT NULL);");
            await initial.Database.ExecuteSqlRawAsync(
                "INSERT INTO upgrade_fixture (id, value) VALUES (1, 'preserved');");
        }

        await database.MigrateAsync();

        await using var verification = database.CreateContext();
        CollectionAssert.AreEqual(
            new[] { InitialMigrationId, LocalAccountsMigrationId },
            (await verification.Database.GetAppliedMigrationsAsync()).ToArray());
        Assert.AreEqual(
            "preserved",
            await ExecuteScalarAsync<string>(verification, "SELECT value FROM upgrade_fixture WHERE id = 1;"));
        Assert.AreEqual(
            4L,
            await ExecuteScalarAsync<long>(
                verification,
                "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name LIKE 'AspNet%';"));
        Assert.AreEqual(
            0L,
            await ExecuteScalarAsync<long>(
                verification,
                "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name LIKE 'AspNetRole%';"));
    }

    [TestMethod]
    public async Task InitializedConnectionsUseWalAndEnforceForeignKeys()
    {
        await using var database = StorageTestDatabase.Create();
        await database.MigrateAsync();
        await database.CreateProbeSchemaAsync();

        await using var context = database.CreateContext();
        Assert.AreEqual("wal", await ExecuteScalarAsync<string>(context, "PRAGMA journal_mode;"));
        Assert.AreEqual(1L, await ExecuteScalarAsync<long>(context, "PRAGMA foreign_keys;"));
        await Assert.ThrowsAsync<SqliteException>(() => context.Database.ExecuteSqlRawAsync(
            "INSERT INTO probe_child (id, parent_id, value) VALUES (1, 999, 'invalid');"));
    }

    [TestMethod]
    public async Task SeparateContextsPreserveCommitsAndExcludeRollbacks()
    {
        await using var database = StorageTestDatabase.Create();
        await database.MigrateAsync();
        await database.CreateProbeSchemaAsync();

        await using (var committedContext = database.CreateContext())
        await using (var transaction = await committedContext.Database.BeginTransactionAsync())
        {
            await committedContext.Database.ExecuteSqlRawAsync(
                "INSERT INTO probe_value (id, value) VALUES (1, 'committed');");
            await transaction.CommitAsync();
        }

        await using (var rolledBackContext = database.CreateContext())
        await using (var transaction = await rolledBackContext.Database.BeginTransactionAsync())
        {
            await rolledBackContext.Database.ExecuteSqlRawAsync(
                "INSERT INTO probe_value (id, value) VALUES (2, 'rolled back');");
            await transaction.RollbackAsync();
        }

        await using var verification = database.CreateContext();
        Assert.AreEqual(1L, await ExecuteScalarAsync<long>(verification, "SELECT COUNT(*) FROM probe_value;"));
        Assert.AreEqual("committed", await ExecuteScalarAsync<string>(verification, "SELECT value FROM probe_value WHERE id = 1;"));
    }

    [TestMethod]
    [Timeout(12000, CooperativeCancellation = true)]
    public async Task CompetingWriterStopsAtTheConfiguredFiniteTimeout()
    {
        await using var database = StorageTestDatabase.Create();
        await database.MigrateAsync();
        await database.CreateProbeSchemaAsync();
        await using var first = new SqliteConnection(database.ConnectionPolicy.CreateRuntimeConnectionString());
        await using var second = new SqliteConnection(database.ConnectionPolicy.CreateRuntimeConnectionString());
        await first.OpenAsync();
        await second.OpenAsync();
        await using var firstTransaction = (SqliteTransaction)await first.BeginTransactionAsync();
        await using (var firstCommand = first.CreateCommand())
        {
            firstCommand.Transaction = firstTransaction;
            firstCommand.CommandText = "INSERT INTO probe_value (id, value) VALUES (1, 'lock owner');";
            await firstCommand.ExecuteNonQueryAsync();
        }

        await using var competingCommand = second.CreateCommand();
        competingCommand.CommandText = "INSERT INTO probe_value (id, value) VALUES (2, 'competitor');";
        var elapsed = Stopwatch.StartNew();

        var exception = await Assert.ThrowsAsync<SqliteException>(() => competingCommand.ExecuteNonQueryAsync());

        elapsed.Stop();
        Assert.AreEqual(5, exception.SqliteErrorCode);
        Assert.IsGreaterThanOrEqualTo(TimeSpan.FromSeconds(3), elapsed.Elapsed);
        Assert.IsLessThan(TimeSpan.FromSeconds(9), elapsed.Elapsed);
        await firstTransaction.RollbackAsync();
    }

    [TestMethod]
    public async Task MigrationAgainstDirectoryFailsWithoutDeletingIt()
    {
        await using var database = StorageTestDatabase.Create();
        var directoryTarget = Directory.CreateDirectory(Path.Combine(database.RootPath, "not-a-file"));
        var target = StoragePathPolicy.Resolve(directoryTarget.FullName, database.ContentRootPath);
        var policy = new StorageConnectionPolicy(target);
        var options = new DbContextOptionsBuilder<LinguaDeskDbContext>()
            .UseSqlite(policy.CreateMigrationConnectionString())
            .Options;
        await using var context = new LinguaDeskDbContext(options);

        await Assert.ThrowsAsync<SqliteException>(() => context.Database.MigrateAsync());

        Assert.IsTrue(Directory.Exists(directoryTarget.FullName));
        Assert.IsFalse(File.Exists(database.DatabasePath));
    }

    private static async Task<T> ExecuteScalarAsync<T>(LinguaDeskDbContext context, string commandText)
    {
        var connection = context.Database.GetDbConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = commandText;
        var result = await command.ExecuteScalarAsync();
        if (result is null || result is DBNull)
        {
            throw new InvalidOperationException($"Scalar command returned no value: {commandText}");
        }

        return (T)Convert.ChangeType(result, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
    }
}
