using System.Net;
using LinguaDesk.Api.Infrastructure.Persistence;
using LinguaDesk.Api.Infrastructure.Security;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace LinguaDesk.Api.Tests;

[TestClass]
public sealed class AccountReadinessTests
{
    private static readonly string[] InitialMigrationOnly = ["20260908221711_InitialStorage"];

    [TestMethod]
    public async Task MigratedDatabaseAndExistingKeyDirectoryAreReadyWhileLivenessStaysIndependent()
    {
        await using var database = StorageTestDatabase.Create();
        await database.MigrateAsync();
        var keysPath = Directory.CreateDirectory(Path.Combine(database.RootPath, "keys")).FullName;
        await using var factory = new AccountWebApplicationFactory(
            database.DatabasePath,
            keysPath,
            new CapturingConfirmationSender());
        using var client = factory.CreateClient();

        using var ready = await client.GetAsync("/health/ready");
        using var live = await client.GetAsync("/health/live");

        Assert.AreEqual(HttpStatusCode.OK, ready.StatusCode);
        Assert.AreEqual("Healthy", await ready.Content.ReadAsStringAsync());
        Assert.AreEqual(HttpStatusCode.OK, live.StatusCode);
        Assert.AreEqual("Healthy", await live.Content.ReadAsStringAsync());
    }

    [TestMethod]
    public async Task MissingDatabaseIsUnreadyWithoutCreatingIt()
    {
        await using var database = StorageTestDatabase.Create();
        var keysPath = Directory.CreateDirectory(Path.Combine(database.RootPath, "keys")).FullName;
        await using var factory = new AccountWebApplicationFactory(
            database.DatabasePath,
            keysPath,
            new CapturingConfirmationSender());
        using var client = factory.CreateClient();

        using var ready = await client.GetAsync("/health/ready");
        using var live = await client.GetAsync("/health/live");

        Assert.AreEqual(HttpStatusCode.ServiceUnavailable, ready.StatusCode);
        Assert.AreEqual("Unhealthy", await ready.Content.ReadAsStringAsync());
        Assert.AreEqual(HttpStatusCode.OK, live.StatusCode);
        Assert.IsFalse(File.Exists(database.DatabasePath));
    }

    [TestMethod]
    public async Task RemovingKeysAfterStartupMakesReadinessUnhealthyWithoutAffectingLiveness()
    {
        await using var database = StorageTestDatabase.Create();
        await database.MigrateAsync();
        var keysPath = Directory.CreateDirectory(Path.Combine(database.RootPath, "keys")).FullName;
        await using var factory = new AccountWebApplicationFactory(
            database.DatabasePath,
            keysPath,
            new CapturingConfirmationSender());
        using var client = factory.CreateClient();
        Assert.AreEqual(HttpStatusCode.OK, (await client.GetAsync("/health/ready")).StatusCode);

        Directory.Delete(keysPath, recursive: true);
        using var ready = await client.GetAsync("/health/ready");
        using var live = await client.GetAsync("/health/live");

        Assert.AreEqual(HttpStatusCode.ServiceUnavailable, ready.StatusCode);
        Assert.AreEqual(HttpStatusCode.OK, live.StatusCode);
    }

    [TestMethod]
    public void DataProtectionPathPolicyRejectsRelativeAndApplicationOwnedPaths()
    {
        var contentRoot = Path.Combine(Path.GetTempPath(), "linguadesk-content");
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            DataProtectionKeyPathPolicy.Resolve("relative/keys", contentRoot));
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            DataProtectionKeyPathPolicy.Resolve(Path.Combine(contentRoot, "wwwroot", "keys"), contentRoot));
    }

    [TestMethod]
    public async Task ProductionLikeStartupAcceptsOnlyCurrentStorageAndExistingKeys()
    {
        await using var database = StorageTestDatabase.Create();
        await database.MigrateAsync();
        var keysPath = Directory.CreateDirectory(Path.Combine(database.RootPath, "keys")).FullName;
        await using var factory = new AccountWebApplicationFactory(
            database.DatabasePath,
            keysPath,
            new CapturingConfirmationSender(),
            environment: "M006Startup");
        using var client = factory.CreateClient();

        using var ready = await client.GetAsync("/health/ready");

        Assert.AreEqual(HttpStatusCode.OK, ready.StatusCode);
    }

    [TestMethod]
    public async Task ProductionLikeStartupCannotBeConfiguredToCreateOrSkipMissingStorage()
    {
        await using var database = StorageTestDatabase.Create();
        var keysPath = Directory.CreateDirectory(Path.Combine(database.RootPath, "keys")).FullName;
        await using var factory = new AccountWebApplicationFactory(
            database.DatabasePath,
            keysPath,
            new CapturingConfirmationSender(),
            environment: "M006Startup",
            configurationOverrides: new Dictionary<string, string?>
            {
                ["SkipReadiness"] = "true",
                ["AutoMigrate"] = "true",
            });

        Assert.Throws<InvalidOperationException>(() => factory.CreateClient());
        Assert.IsFalse(File.Exists(database.DatabasePath));
    }

    [TestMethod]
    public async Task ProductionLikeStartupDoesNotCreateAnAbsentKeyDirectory()
    {
        await using var database = StorageTestDatabase.Create();
        await database.MigrateAsync();
        var missingKeysPath = Path.Combine(database.RootPath, "missing-keys");
        await using var factory = new AccountWebApplicationFactory(
            database.DatabasePath,
            missingKeysPath,
            new CapturingConfirmationSender(),
            environment: "M006Startup");

        Assert.Throws<InvalidOperationException>(() => factory.CreateClient());
        Assert.IsFalse(Directory.Exists(missingKeysPath));
    }

    [TestMethod]
    public async Task ProductionLikeStartupRejectsAStaleDatabaseWithoutMigratingIt()
    {
        await using var database = StorageTestDatabase.Create();
        await using (var initial = database.CreateContext(allowCreate: true))
        {
            await initial.GetService<IMigrator>().MigrateAsync("20260908221711_InitialStorage");
        }
        var before = await File.ReadAllBytesAsync(database.DatabasePath);
        var keysPath = Directory.CreateDirectory(Path.Combine(database.RootPath, "keys")).FullName;
        await using var factory = new AccountWebApplicationFactory(
            database.DatabasePath,
            keysPath,
            new CapturingConfirmationSender(),
            environment: "M006Startup");

        Assert.Throws<InvalidOperationException>(() => factory.CreateClient());
        CollectionAssert.AreEqual(before, await File.ReadAllBytesAsync(database.DatabasePath));
        await using var verification = database.CreateContext();
        CollectionAssert.AreEqual(
            InitialMigrationOnly,
            (await verification.Database.GetAppliedMigrationsAsync()).ToArray());
    }

    [TestMethod]
    [Timeout(12000, CooperativeCancellation = true)]
    public async Task ACompetingWriteLockMakesReadinessUnhealthy()
    {
        await using var database = StorageTestDatabase.Create();
        await database.MigrateAsync();
        var keysPath = Directory.CreateDirectory(Path.Combine(database.RootPath, "keys")).FullName;
        await using var factory = new AccountWebApplicationFactory(
            database.DatabasePath,
            keysPath,
            new CapturingConfirmationSender());
        using var client = factory.CreateClient();
        await using var lockConnection = new SqliteConnection(database.ConnectionPolicy.CreateRuntimeConnectionString());
        await lockConnection.OpenAsync();
        await using var transaction = (SqliteTransaction)await lockConnection.BeginTransactionAsync();
        await using (var command = lockConnection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = "CREATE TABLE __readiness_lock_owner (id INTEGER);";
            await command.ExecuteNonQueryAsync();
        }

        using var ready = await client.GetAsync("/health/ready");

        Assert.AreEqual(HttpStatusCode.ServiceUnavailable, ready.StatusCode);
        await transaction.RollbackAsync();
    }
}
