using LinguaDesk.Api.Infrastructure.Persistence;
using LinguaDesk.Api.Features.Operations;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace LinguaDesk.Api.Tests;

[TestClass]
public sealed class DeploymentPreparationTests
{
    private static IConfiguration Configuration(StorageTestDatabase database) => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["Storage:DatabasePath"] = database.DatabasePath,
        ["Storage:JournalMode"] = "Delete",
        ["Storage:BackupPath"] = Path.Combine(database.RootPath, "backups"),
        ["Security:DataProtectionKeysPath"] = Path.Combine(database.RootPath, "keys"),
    }).Build();

    [TestMethod]
    public async Task FreshPrepareAndRedeploymentPreserveAccountLedgerAndCookieKeys()
    {
        await using var database = StorageTestDatabase.Create();
        var configuration = Configuration(database);
        await DeploymentPreparation.PrepareAsync(configuration, database.ContentRootPath);
        var keys = new DirectoryInfo(Path.Combine(database.RootPath, "keys"));
        var cookie = DataProtectionProvider.Create(keys, b => b.SetApplicationName("LinguaDesk")).CreateProtector("test-cookie").Protect("session");
        await using (var context = database.CreateContext())
        {
            context.Users.Add(new IdentityUser { Id = "synthetic-account", UserName = "synthetic", EmailConfirmed = true });
            await context.SaveChangesAsync();
            await context.Database.ExecuteSqlRawAsync("UPDATE LedgerRevisions SET Value=17;");
        }
        await DeploymentPreparation.PrepareAsync(configuration, database.ContentRootPath);
        await using var verification = database.CreateContext();
        Assert.AreEqual(1, await verification.Users.CountAsync());
        Assert.AreEqual(17L, (await verification.LedgerRevisions.SingleAsync()).Value);
        Assert.AreEqual("session", DataProtectionProvider.Create(keys, b => b.SetApplicationName("LinguaDesk")).CreateProtector("test-cookie").Unprotect(cookie));
        await using var connection = new SqliteConnection(database.ConnectionPolicy.CreateRuntimeConnectionString());
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA journal_mode;";
        Assert.AreEqual("delete", await command.ExecuteScalarAsync());
        command.CommandText = "PRAGMA integrity_check;";
        Assert.AreEqual("ok", await command.ExecuteScalarAsync());
        Assert.HasCount(1, Directory.GetFiles(Path.Combine(database.RootPath, "backups"), "*.db"));
    }

    [TestMethod]
    public async Task LegacyWalUpgradeAndBackupRestoreRetainData()
    {
        await using var database = StorageTestDatabase.Create();
        await database.MigrateAsync();
        await using (var context = database.CreateContext())
        {
            await context.Database.ExecuteSqlRawAsync("UPDATE LedgerRevisions SET Value=23;");
            context.MonetaryCostLedgers.Add(new MonetaryCostLedger { CostMonth = "2026-10", KnownSpendMinorUnits = 123, UnresolvedExposureMinorUnits = 45 });
            context.MonetaryAttemptReservations.Add(new MonetaryAttemptReservation { AttemptId = "retained-attempt", OperationReference = "opaque-operation", CostMonth = "2026-10", TariffReference = "fixture", BoundMinorUnits = 45 });
            context.CharacterLedgerEntries.Add(new CharacterLedgerEntry { Scope = "user", AccountId = "synthetic-account", Day = "2026-10-26", ConsumedCharacters = 46 });
            await context.SaveChangesAsync();
        }
        await DeploymentPreparation.PrepareAsync(Configuration(database), database.ContentRootPath);
        var backup = Directory.GetFiles(Path.Combine(database.RootPath, "backups"), "*.db").Single();
        File.Copy(backup, database.DatabasePath, overwrite: true);
        await DeploymentPreparation.PrepareAsync(Configuration(database), database.ContentRootPath);
        await using var restored = database.CreateContext();
        Assert.AreEqual(23L, (await restored.LedgerRevisions.SingleAsync()).Value);
        var monetary = await restored.MonetaryCostLedgers.SingleAsync();
        Assert.AreEqual(123L, monetary.KnownSpendMinorUnits);
        Assert.AreEqual(45L, monetary.UnresolvedExposureMinorUnits);
        Assert.AreEqual("retained-attempt", (await restored.MonetaryAttemptReservations.SingleAsync()).AttemptId);
        Assert.AreEqual(46, (await restored.CharacterLedgerEntries.SingleAsync()).ConsumedCharacters);
        Assert.IsFalse((await restored.Database.GetPendingMigrationsAsync()).Any());
    }

    [TestMethod]
    public async Task MissingEstablishedStoreFailsWithoutCreatingAReplacement()
    {
        await using var database = StorageTestDatabase.Create();
        var config = Configuration(database);
        await DeploymentPreparation.PrepareAsync(config, database.ContentRootPath);
        File.Delete(database.DatabasePath);
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => DeploymentPreparation.PrepareAsync(config, database.ContentRootPath));
        Assert.IsFalse(File.Exists(database.DatabasePath));
    }

    [TestMethod]
    public async Task FailedMigrationRetainsRecoveryAndDoesNotMarkInitialized()
    {
        await using var database = StorageTestDatabase.Create();
        await using (var connection = new SqliteConnection(database.ConnectionPolicy.CreateMigrationConnectionString()))
        {
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            command.CommandText = "CREATE TABLE AspNetUsers (Id TEXT);";
            await command.ExecuteNonQueryAsync();
        }
        await Assert.ThrowsExactlyAsync<SqliteException>(() => DeploymentPreparation.PrepareAsync(Configuration(database), database.ContentRootPath));
        Assert.IsFalse(File.Exists(Path.Combine(database.RootPath, ".initialized")));
        Assert.HasCount(1, Directory.GetFiles(Path.Combine(database.RootPath, "backups"), "*.db"));
    }

    [TestMethod]
    public async Task ExclusivePreparationLeaseFailsClosed()
    {
        await using var database = StorageTestDatabase.Create();
        await using var lease = new FileStream(Path.Combine(database.RootPath, ".prepare.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        await Assert.ThrowsExactlyAsync<IOException>(() => DeploymentPreparation.PrepareAsync(Configuration(database), database.ContentRootPath));
        Assert.IsFalse(File.Exists(database.DatabasePath));
    }

    [TestMethod]
    public async Task ServingLeaseBlocksPreparationAndNewerSchemaBlocksRollback()
    {
        await using var database = StorageTestDatabase.Create();
        var config = Configuration(database);
        await using (var runtime = new FileStream(Path.Combine(database.RootPath, ".runtime.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
        {
            await Assert.ThrowsExactlyAsync<IOException>(() => DeploymentPreparation.PrepareAsync(config, database.ContentRootPath));
        }
        await DeploymentPreparation.PrepareAsync(config, database.ContentRootPath);
        await using (var context = database.CreateContext())
        {
            await context.Database.ExecuteSqlRawAsync("INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('20990101000000_FutureSchema', '10.0.10');");
        }
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => DeploymentPreparation.PrepareAsync(config, database.ContentRootPath));
        Assert.HasCount(1, Directory.GetFiles(Path.Combine(database.RootPath, "backups"), "*.db"));
    }

    [TestMethod]
    public async Task RuntimeModeMismatchFailsWithoutChangingTheStore()
    {
        await using var database = StorageTestDatabase.Create();
        await database.MigrateAsync();
        var options = new DbContextOptionsBuilder<LinguaDeskDbContext>()
            .UseSqlite(database.ConnectionPolicy.CreateRuntimeConnectionString())
            .AddInterceptors(new StorageJournalInterceptor("Delete")).Options;
        await using (var mismatched = new LinguaDeskDbContext(options))
        {
            await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => mismatched.Database.OpenConnectionAsync());
        }
        await DeploymentPreparation.PrepareAsync(Configuration(database), database.ContentRootPath);
        await using var ready = new LinguaDeskDbContext(options);
        await ready.Database.OpenConnectionAsync();
        await using (var transaction = await ready.Database.BeginTransactionAsync())
        {
            await ready.Database.ExecuteSqlRawAsync("UPDATE LedgerRevisions SET Value=99;");
            // Closing an uncommitted writer must not charge a durable ledger.
        }
        Assert.AreEqual(0L, (await ready.LedgerRevisions.SingleAsync()).Value);
    }

    [TestMethod]
    [Timeout(30000, CooperativeCancellation = true)]
    public async Task DeleteProfileSurvivesTwoServingProcessesAndRedeployment()
    {
        await using var database = StorageTestDatabase.Create();
        var config = Configuration(database);
        await DeploymentPreparation.PrepareAsync(config, database.ContentRootPath);
        await using (var context = database.CreateContext())
        {
            await context.Database.ExecuteSqlRawAsync("UPDATE LedgerRevisions SET Value=31;");
        }
        var keys = Path.Combine(database.RootPath, "keys");
        int firstId;
        await using (var first = await StorageHostTests.OwnedKestrelHost.StartAsync(database.DatabasePath, keys, "Delete"))
        {
            firstId = first.ProcessId;
            Assert.AreEqual("Healthy", await first.Client.GetStringAsync("/health/ready"));
        }
        await DeploymentPreparation.PrepareAsync(config, database.ContentRootPath);
        await using (var second = await StorageHostTests.OwnedKestrelHost.StartAsync(database.DatabasePath, keys, "Delete"))
        {
            Assert.AreNotEqual(firstId, second.ProcessId);
            Assert.AreEqual("Healthy", await second.Client.GetStringAsync("/health/ready"));
        }
        await using var verification = database.CreateContext();
        Assert.AreEqual(31L, (await verification.LedgerRevisions.SingleAsync()).Value);
    }

    [TestMethod]
    public async Task BackupRetentionNeverDiscardsTheOriginalRecoveryPoint()
    {
        await using var database = StorageTestDatabase.Create();
        var config = Configuration(database);
        await DeploymentPreparation.PrepareAsync(config, database.ContentRootPath);
        await DeploymentPreparation.PrepareAsync(config, database.ContentRootPath);
        var original = Directory.GetFiles(Path.Combine(database.RootPath, "backups"), "*.db").Single();
        for (var i = 0; i < 7; i++) await DeploymentPreparation.PrepareAsync(config, database.ContentRootPath);
        Assert.IsTrue(File.Exists(original));
        Assert.HasCount(5, Directory.GetFiles(Path.Combine(database.RootPath, "backups"), "*.db"));
    }

    [TestMethod]
    public void UnknownJournalProfileRejected() => Assert.ThrowsExactly<InvalidOperationException>(() => new StorageJournalInterceptor("Memory"));
}
