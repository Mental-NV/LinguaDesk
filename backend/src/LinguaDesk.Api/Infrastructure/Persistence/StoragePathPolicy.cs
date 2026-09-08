namespace LinguaDesk.Api.Infrastructure.Persistence;

public static class StoragePathPolicy
{
    public const string DefaultDatabasePath = "/var/lib/linguadesk/linguadesk.db";

    public static StorageDatabaseTarget Resolve(string? configuredPath, string contentRootPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentRootPath);

        var candidate = configuredPath is null ? DefaultDatabasePath : configuredPath;
        if (string.IsNullOrWhiteSpace(candidate))
        {
            throw new InvalidOperationException("Storage:DatabasePath must not be empty.");
        }

        if (candidate.Equals(":memory:", StringComparison.OrdinalIgnoreCase)
            || candidate.StartsWith("file:", StringComparison.OrdinalIgnoreCase)
            || candidate.StartsWith("//", StringComparison.Ordinal)
            || candidate.StartsWith("\\\\", StringComparison.Ordinal)
            || candidate.Contains('\0')
            || !Path.IsPathFullyQualified(candidate))
        {
            throw new InvalidOperationException(
                "Storage:DatabasePath must be an absolute local file path; relative, URI, and in-memory targets are not allowed.");
        }

        var fullPath = Path.GetFullPath(candidate);
        if (string.IsNullOrEmpty(Path.GetFileName(fullPath)))
        {
            throw new InvalidOperationException("Storage:DatabasePath must identify a database file, not a directory root.");
        }

        foreach (var unsafeDirectory in GetUnsafeDirectories(contentRootPath))
        {
            if (IsWithin(fullPath, unsafeDirectory))
            {
                throw new InvalidOperationException(
                    $"Storage:DatabasePath must be outside generated, static, and published output. Unsafe location: '{unsafeDirectory}'.");
            }
        }

        return new StorageDatabaseTarget(fullPath);
    }

    public static string? FindRepositoryRoot(string startPath)
    {
        var current = new DirectoryInfo(Path.GetFullPath(startPath));
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "global.json"))
                && Directory.Exists(Path.Combine(current.FullName, "backend")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return null;
    }

    private static IEnumerable<string> GetUnsafeDirectories(string contentRootPath)
    {
        var contentRoot = Path.GetFullPath(contentRootPath);
        if (!File.Exists(Path.Combine(contentRoot, "LinguaDesk.Api.csproj")))
        {
            yield return contentRoot;
            yield break;
        }

        yield return Path.Combine(contentRoot, "wwwroot");
        yield return Path.Combine(contentRoot, "bin");
        yield return Path.Combine(contentRoot, "obj");

        var repositoryRoot = FindRepositoryRoot(contentRoot);
        if (repositoryRoot is null)
        {
            yield break;
        }

        yield return Path.Combine(repositoryRoot, "frontend", "dist");
        yield return Path.Combine(repositoryRoot, "artifacts");
    }

    private static bool IsWithin(string path, string directory)
    {
        var comparison = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        var normalizedDirectory = Path.GetFullPath(directory)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return path.Equals(normalizedDirectory, comparison)
            || path.StartsWith($"{normalizedDirectory}{Path.DirectorySeparatorChar}", comparison)
            || path.StartsWith($"{normalizedDirectory}{Path.AltDirectorySeparatorChar}", comparison);
    }
}

public sealed record StorageDatabaseTarget(string DatabasePath);
