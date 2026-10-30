using System.IO.Compression;
using System.Text.Json;

namespace LinguaDesk.Api.Infrastructure.Persistence;

internal static class DeploymentRelease
{
    private const string Releases = "/home/linguadesk/releases";

    private static string Revision()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "revision.json")));
        var sha = document.RootElement.GetProperty("sha").GetString() ?? "";
        if (sha.Length != 40 || !sha.All(Uri.IsHexDigit)) throw new InvalidOperationException("Invalid deployment revision.");
        return sha;
    }

    internal static void Archive()
    {
        Directory.CreateDirectory(Releases);
        if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(Releases, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        var path = Path.Combine(Releases, Revision() + ".zip");
        if (File.Exists(path))
        {
            WriteChecksum(path);
            return;
        }
        var temporary = path + ".pending";
        if (File.Exists(temporary)) File.Delete(temporary);
        ZipFile.CreateFromDirectory(AppContext.BaseDirectory, temporary);
        File.Move(temporary, path);
        WriteChecksum(path);
        if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
    }

    private static void WriteChecksum(string path)
    {
        using var file = File.OpenRead(path);
        var hash = Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(file));
        var checksum = path + ".sha256";
        if (File.Exists(checksum) && !File.ReadAllText(checksum).StartsWith(hash + "  ", StringComparison.Ordinal))
            throw new InvalidOperationException("Retained release checksum mismatch.");
        File.WriteAllText(checksum, hash + "  " + Path.GetFileName(path) + "\n");
    }

    internal static void MarkReady()
    {
        File.WriteAllText(Path.Combine(Releases, Revision() + ".ready"), DateTime.UtcNow.ToString("O", System.Globalization.CultureInfo.InvariantCulture));
        // Retain current + previous ready artifacts. Unknown/failed artifacts remain recovery material.
        foreach (var old in new DirectoryInfo(Releases).GetFiles("*.ready").OrderByDescending(f => f.LastWriteTimeUtc).Skip(2))
        {
            var archive = Path.ChangeExtension(old.FullName, ".zip");
            if (File.Exists(archive)) File.Delete(archive);
            if (File.Exists(archive + ".sha256")) File.Delete(archive + ".sha256");
            old.Delete();
        }
    }
}
