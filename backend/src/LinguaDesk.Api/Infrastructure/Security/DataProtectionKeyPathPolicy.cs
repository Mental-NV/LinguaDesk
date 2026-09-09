namespace LinguaDesk.Api.Infrastructure.Security;

public static class DataProtectionKeyPathPolicy
{
    public const string DefaultPath = "/var/lib/linguadesk/keys";

    public static DataProtectionKeyDirectory Resolve(string? configuredPath, string contentRootPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentRootPath);
        var candidate = configuredPath is null ? DefaultPath : configuredPath;
        if (string.IsNullOrWhiteSpace(candidate)
            || candidate.Contains('\0')
            || !Path.IsPathFullyQualified(candidate))
        {
            throw new InvalidOperationException(
                "Security:DataProtectionKeysPath must be an absolute local directory path.");
        }

        var fullPath = Path.GetFullPath(candidate);
        var contentRoot = Path.GetFullPath(contentRootPath);
        var unsafeDirectories = new[]
        {
            contentRoot,
            Path.Combine(contentRoot, "wwwroot"),
            Path.Combine(contentRoot, "bin"),
            Path.Combine(contentRoot, "obj"),
        };
        if (unsafeDirectories.Any(directory => IsWithin(fullPath, directory)))
        {
            throw new InvalidOperationException(
                "Security:DataProtectionKeysPath must be outside source, static, generated, and published output.");
        }

        return new DataProtectionKeyDirectory(fullPath);
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

public sealed record DataProtectionKeyDirectory(string Path);
