using System.Security.Cryptography;
using System.Text;
using LinguaDesk.Api.Infrastructure.Security;
using Microsoft.AspNetCore.DataProtection;

namespace LinguaDesk.Api.Features.Operations;

public sealed class OperationFingerprintKeyProvider(
    IDataProtectionProvider dataProtection,
    DataProtectionKeyDirectory keyDirectory) : IDisposable
{
    public const string KeyPurpose = "LinguaDesk.Operations.FingerprintKey/v1";

    private const string KeyFileName = "operation-fingerprint-key.protected";

    private readonly SemaphoreSlim gate = new(1, 1);
    private bool disposed;
    private byte[]? key;

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        gate.Dispose();
    }

    public async Task<byte[]> GetKeyAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (key is not null)
        {
            return key;
        }

        await gate.WaitAsync(cancellationToken);
        try
        {
            key ??= await LoadOrCreateAsync(cancellationToken);
            return key;
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task<byte[]> LoadOrCreateAsync(CancellationToken cancellationToken)
    {
        var protector = dataProtection.CreateProtector(KeyPurpose);
        var path = Path.Combine(keyDirectory.Path, KeyFileName);
        if (File.Exists(path))
        {
            var stored = await File.ReadAllTextAsync(path, cancellationToken);
            var protectedPayload = Encoding.UTF8.GetString(Convert.FromBase64String(stored.Trim()));
            return Convert.FromBase64String(protector.Unprotect(protectedPayload));
        }

        var created = RandomNumberGenerator.GetBytes(32);
        var serialized = Convert.ToBase64String(
            Encoding.UTF8.GetBytes(protector.Protect(Convert.ToBase64String(created))));
        await File.WriteAllTextAsync(path, serialized + "\n", cancellationToken);
        return created;
    }
}

public static class OperationFingerprint
{
    public const string PolicyRevision = "unicode-scalar-v1";

    private const string CanonicalPrefix = "linguadesk-operation-v1";

    public static string Compute(
        byte[] key,
        string family,
        string source,
        string effectiveSourceSelection,
        string? target,
        string? mode)
    {
        ArgumentNullException.ThrowIfNull(key);
        var canonical = string.Join(
            "\0",
            CanonicalPrefix,
            family,
            source,
            effectiveSourceSelection,
            target ?? string.Empty,
            mode ?? string.Empty,
            PolicyRevision);
        return Convert.ToHexString(HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(canonical)));
    }
}
