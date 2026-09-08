using LinguaDesk.Api.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LinguaDesk.Api.Tests;

[TestClass]
public sealed class StoragePolicyTests
{
    [TestMethod]
    public void MissingConfigurationUsesTheDocumentedProductionDefault()
    {
        var target = StoragePathPolicy.Resolve(configuredPath: null, GetContentRoot());

        Assert.AreEqual(StoragePathPolicy.DefaultDatabasePath, target.DatabasePath);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow("relative.db")]
    [DataRow(":memory:")]
    [DataRow("file:/private/tmp/linguadesk.db")]
    [DataRow("//server/share/linguadesk.db")]
    public void NonFileOrNonAbsoluteTargetsAreRejected(string candidate)
    {
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            StoragePathPolicy.Resolve(candidate, GetContentRoot()));
    }

    [TestMethod]
    public void GeneratedAndPublishedOutputTargetsAreRejected()
    {
        var repositoryRoot = GetRepositoryRoot();
        var candidates = new[]
        {
            Path.Combine(repositoryRoot, "artifacts", "unsafe.db"),
            Path.Combine(repositoryRoot, "frontend", "dist", "unsafe.db"),
            Path.Combine(GetContentRoot(), "wwwroot", "unsafe.db"),
            Path.Combine(GetContentRoot(), "bin", "unsafe.db"),
            Path.Combine(GetContentRoot(), "obj", "unsafe.db"),
        };

        foreach (var candidate in candidates)
        {
            Assert.ThrowsExactly<InvalidOperationException>(() =>
                StoragePathPolicy.Resolve(candidate, GetContentRoot()));
        }
    }

    [TestMethod]
    public void RuntimeAndMigrationConnectionsUseDistinctCreationModesAndSharedSafetySettings()
    {
        var target = StoragePathPolicy.Resolve(
            Path.Combine(Path.GetTempPath(), "linguadesk-policy.db"),
            GetContentRoot());
        var policy = new StorageConnectionPolicy(target);
        var runtime = new SqliteConnectionStringBuilder(policy.CreateRuntimeConnectionString());
        var migration = new SqliteConnectionStringBuilder(policy.CreateMigrationConnectionString());

        Assert.AreEqual(SqliteOpenMode.ReadWrite, runtime.Mode);
        Assert.AreEqual(SqliteOpenMode.ReadWriteCreate, migration.Mode);
        Assert.AreEqual(target.DatabasePath, runtime.DataSource);
        Assert.IsTrue(runtime.ForeignKeys);
        Assert.IsFalse(runtime.Pooling);
        Assert.AreEqual(StorageConnectionPolicy.DefaultTimeoutSeconds, runtime.DefaultTimeout);
        Assert.AreEqual(runtime.ForeignKeys, migration.ForeignKeys);
        Assert.AreEqual(runtime.Pooling, migration.Pooling);
        Assert.AreEqual(runtime.DefaultTimeout, migration.DefaultTimeout);
    }

    [TestMethod]
    public async Task RuntimeOpenOfMissingDatabaseFailsWithoutCreatingIt()
    {
        await using var database = StorageTestDatabase.Create();
        await using var context = database.CreateContext();

        await Assert.ThrowsAsync<SqliteException>(() => context.Database.OpenConnectionAsync());

        Assert.IsFalse(File.Exists(database.DatabasePath));
    }

    private static string GetContentRoot() =>
        Path.Combine(GetRepositoryRoot(), "backend", "src", "LinguaDesk.Api");

    private static string GetRepositoryRoot() =>
        StoragePathPolicy.FindRepositoryRoot(AppContext.BaseDirectory)
        ?? throw new InvalidOperationException("Could not locate repository root.");
}
