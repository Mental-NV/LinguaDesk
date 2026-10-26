using LinguaDesk.Core;
using System.Text.Json;
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

    internal static readonly string CatalogSnapshot = JsonSerializer.Serialize(ProductCatalog.Languages);
    internal static readonly string CatalogSha256 = ComputeSha256(StrictUtf8.GetBytes(CatalogSnapshot));

    internal static (string Content, string Sha256) Compose(string resourceName, string metadata)
    {
        var resource = Load(resourceName);
        var content = resource.Content + "\nCatalog " + ProductCatalog.LanguageRevision + " sha256=" + CatalogSha256 + "\n" + metadata;
        return (content, ComputeSha256(StrictUtf8.GetBytes(content)));
    }

    internal static string Descriptor(string id) => JsonSerializer.Serialize(ProductCatalog.GetLanguage(id));

    internal static string EligibilityCatalog => string.Join("\n", ProductCatalog.Languages.GroupBy(language => language.Script).Select(group =>
        "Writing system " + group.Key + ": " + string.Join("; ", group.Select(language => language.Id + "=" + language.Name))))
        + "\nzh: Simplified or Traditional input, Simplified output. ar: Modern Standard Arabic. "
        + "sr: Cyrillic or Latin input; preserve script in Write, default Cyrillic translation. "
        + "ku: Latin Kurmanji. ckb: Arabic Sorani. pa: Gurmukhi only. mn: Cyrillic only. "
        + "az/uz/tk/ha: Latin only. fil: no tl alias. Other writing systems as listed.";

    internal static string ComputeSha256(ReadOnlySpan<byte> bytes) =>
        Convert.ToHexStringLower(SHA256.HashData(bytes));
}
