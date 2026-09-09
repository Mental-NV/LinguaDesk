using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace LinguaDesk.Infrastructure.Ai;

internal static class PromptResource
{
    private static readonly UTF8Encoding StrictUtf8 = new(
        encoderShouldEmitUTF8Identifier: false,
        throwOnInvalidBytes: true);

    internal static (string Content, string Sha256) Load(string resourceName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceName);

        var assembly = typeof(PromptResource).Assembly;
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Embedded prompt resource '{resourceName}' was not found in {assembly.GetName().Name}.");
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        var bytes = buffer.ToArray();

        return (StrictUtf8.GetString(bytes), ComputeSha256(bytes));
    }

    internal static string ComputeSha256(ReadOnlySpan<byte> bytes) =>
        Convert.ToHexStringLower(SHA256.HashData(bytes));
}
