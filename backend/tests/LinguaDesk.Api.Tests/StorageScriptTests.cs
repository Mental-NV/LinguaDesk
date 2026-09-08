using System.Diagnostics;
using LinguaDesk.Api.Infrastructure.Persistence;

namespace LinguaDesk.Api.Tests;

[TestClass]
public sealed class StorageScriptTests
{
    [TestMethod]
    public async Task MissingMigrationTargetReturnsUsageFailure()
    {
        var result = await RunStorageScriptAsync("migrate");

        Assert.AreEqual(2, result.ExitCode);
        StringAssert.Contains(result.Error, "Usage:");
    }

    [TestMethod]
    public async Task RelativeMigrationTargetFailsWithoutCreatingAFile()
    {
        var repositoryRoot = GetRepositoryRoot();
        const string relativePath = "should-not-exist.db";
        var unexpectedPath = Path.Combine(repositoryRoot, relativePath);

        var result = await RunStorageScriptAsync("migrate", relativePath);

        Assert.AreEqual(2, result.ExitCode);
        Assert.IsFalse(File.Exists(unexpectedPath));
    }

    [TestMethod]
    public async Task MissingParentMigrationTargetFailsWithoutFallback()
    {
        await using var database = StorageTestDatabase.Create();
        var missingTarget = Path.Combine(database.RootPath, "missing", "target.db");

        var result = await RunStorageScriptAsync("migrate", missingTarget);

        Assert.AreEqual(2, result.ExitCode);
        Assert.IsFalse(File.Exists(missingTarget));
        Assert.IsFalse(File.Exists(database.DatabasePath));
    }

    [TestMethod]
    public async Task DirectoryMigrationTargetFailsWithoutDeletingTheDirectory()
    {
        await using var database = StorageTestDatabase.Create();
        var directoryTarget = Directory.CreateDirectory(Path.Combine(database.RootPath, "directory-target"));

        var result = await RunStorageScriptAsync("migrate", directoryTarget.FullName);

        Assert.AreEqual(2, result.ExitCode);
        Assert.IsTrue(Directory.Exists(directoryTarget.FullName));
        Assert.IsFalse(File.Exists(database.DatabasePath));
    }

    private static string GetRepositoryRoot() =>
        StoragePathPolicy.FindRepositoryRoot(AppContext.BaseDirectory)
        ?? throw new InvalidOperationException("Could not locate repository root.");

    private static async Task<ScriptResult> RunStorageScriptAsync(params string[] arguments)
    {
        var repositoryRoot = GetRepositoryRoot();
        var startInfo = new ProcessStartInfo("bash")
        {
            WorkingDirectory = repositoryRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        startInfo.ArgumentList.Add(Path.Combine(repositoryRoot, "scripts", "storage.sh"));
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Could not start storage script.");
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
        return new ScriptResult(process.ExitCode, await outputTask, await errorTask);
    }

    private sealed record ScriptResult(int ExitCode, string Output, string Error);
}
