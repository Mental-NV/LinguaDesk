using System.Text.Json;
using System.Text.Json.Serialization;

namespace LinguaDesk.Infrastructure.Ai;

public static class EligibilityPrompt
{
    public const string PromptId = "eligibility.v2";

    private const string ResourceName =
        "LinguaDesk.Infrastructure.Ai.Prompts.eligibility.v2.txt";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private static readonly Lazy<(string Content, string Sha256)> Resource =
        new(() => PromptResource.Compose(ResourceName, PromptResource.EligibilityCatalog));

    public static PromptSnapshot Create(string source, string? sourceHint = null)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (sourceHint is not null && !LinguaDesk.Core.ProductCatalog.IsLanguage(sourceHint))
            throw new ArgumentException("Unknown canonical source hint.", nameof(sourceHint));
        var userData = JsonSerializer.Serialize(
            new EligibilityUserData(source, sourceHint),
            SerializerOptions);
        var resource = Resource.Value;

        return new PromptSnapshot(
            PromptId,
            resource.Sha256,
            [
                new PromptMessageSnapshot("system", resource.Content),
                new PromptMessageSnapshot("user", userData),
            ]);
    }

    private sealed record EligibilityUserData(
        [property: JsonPropertyOrder(0)] string Source,
        [property: JsonPropertyOrder(1)] string? SourceHint);
}
