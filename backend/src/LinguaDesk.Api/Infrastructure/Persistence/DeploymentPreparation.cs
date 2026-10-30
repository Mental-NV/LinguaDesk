using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LinguaDesk.Api.Infrastructure.Persistence;

/// <summary>Run only while the serving process is stopped. Never restores or downgrades automatically.</summary>
public static class DeploymentPreparation
{
    public static async Task PrepareAsync(IConfiguration configuration, string contentRoot)
    {
        var storage = configuration.GetSection("Storage").Get<StorageOptions>() ?? new();
        _ = new StorageJournalInterceptor(storage.JournalMode);
        var target = StoragePathPolicy.Resolve(storage.DatabasePath, contentRoot);
        var parent = Path.GetDirectoryName(target.DatabasePath)!;
        var keys = configuration["Security:DataProtectionKeysPath"]
            ?? throw new InvalidOperationException("A durable key directory is required.");
        var backups = configuration["Storage:BackupPath"]
            ?? throw new InvalidOperationException("A durable backup directory is required.");
        foreach (var path in new[] { parent, keys, backups })
        {
            if (!Path.IsPathFullyQualified(path) || Path.GetFullPath(path).StartsWith(Path.GetFullPath(contentRoot) + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                throw new InvalidOperationException("Deployment directories must be absolute and outside the package.");
            Directory.CreateDirectory(path);
            if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
        await using var runtimeLease = new FileStream(Path.Combine(parent, ".runtime.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        // OS releases this exclusive lock on a crash; no stale lock-file deletion is needed.
        await using var lease = new FileStream(Path.Combine(parent, ".prepare.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var marker = Path.Combine(parent, ".initialized");
        if (File.Exists(marker) && !File.Exists(target.DatabasePath))
            throw new InvalidOperationException("An initialized database is missing. Explicit recovery is required.");
        var backup = Path.Combine(backups, $"pre-migration-{DateTime.UtcNow:yyyyMMddTHHmmssfffffffZ}-{Guid.NewGuid():N}.db");
        if (File.Exists(target.DatabasePath))
        {
            await using var source = new SqliteConnection(new StorageConnectionPolicy(target).CreateRuntimeConnectionString());
            await source.OpenAsync();
            await using var destination = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = backup, Pooling = false }.ToString());
            await destination.OpenAsync();
            source.BackupDatabase(destination);
            var keyBackup = Directory.CreateDirectory(backup + ".keys");
            if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(keyBackup.FullName, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            foreach (var key in Directory.GetFiles(keys, "key-*.xml"))
            {
                var copy = Path.Combine(keyBackup.FullName, Path.GetFileName(key));
                File.Copy(key, copy);
                if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(copy, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            }
            if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(backup, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
        var options = new DbContextOptionsBuilder<LinguaDeskDbContext>()
            .UseSqlite(new StorageConnectionPolicy(target).CreateMigrationConnectionString()).Options;
        await using (var context = new LinguaDeskDbContext(options))
        {
            var known = context.Database.GetMigrations().ToHashSet(StringComparer.Ordinal);
            var applied = await context.Database.GetAppliedMigrationsAsync();
            if (applied.Any(migration => !known.Contains(migration)))
                throw new InvalidOperationException("The store contains migrations unknown to this artifact. Schema rollback is forbidden.");
            await context.Database.MigrateAsync();
        }
        await using (var connection = new SqliteConnection(new StorageConnectionPolicy(target).CreateRuntimeConnectionString()))
        {
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA wal_checkpoint(TRUNCATE);";
            await command.ExecuteNonQueryAsync();
            command.CommandText = $"PRAGMA journal_mode={storage.JournalMode.ToUpperInvariant()};";
            if (!string.Equals((await command.ExecuteScalarAsync())?.ToString(), storage.JournalMode, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Could not establish the required journal mode.");
            command.CommandText = "PRAGMA synchronous=FULL;";
            await command.ExecuteNonQueryAsync();
            command.CommandText = "PRAGMA integrity_check;";
            if (!Equals(await command.ExecuteScalarAsync(), "ok")) throw new InvalidOperationException("Database integrity check failed.");
            command.CommandText = "PRAGMA foreign_key_check;";
            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync()) throw new InvalidOperationException("Database foreign key check failed.");
        }
        if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(target.DatabasePath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        await File.WriteAllTextAsync(marker, "LinguaDesk initialized\n");
        // Keep the original recovery point even after repeated failed migration attempts.
        // A failed preparation never prunes; successful cleanup retains original + four newest.
        var recoveryPoints = new DirectoryInfo(backups).GetFiles("pre-migration-*.db").OrderByDescending(f => f.Name, StringComparer.Ordinal).ToArray();
        var originalRecoveryPoint = recoveryPoints.LastOrDefault()?.FullName;
        foreach (var old in recoveryPoints.Skip(4).Where(f => f.FullName != originalRecoveryPoint))
        {
            await using var check = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = old.FullName, Mode = SqliteOpenMode.ReadOnly, Pooling = false }.ToString());
            await check.OpenAsync();
            using var command = check.CreateCommand();
            command.CommandText = "PRAGMA integrity_check;";
            if (Equals(await command.ExecuteScalarAsync(), "ok"))
            {
                old.Delete();
                if (Directory.Exists(old.FullName + ".keys")) Directory.Delete(old.FullName + ".keys", recursive: true);
            }
        }
    }
}
