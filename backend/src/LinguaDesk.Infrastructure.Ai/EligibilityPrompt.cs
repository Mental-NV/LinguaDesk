using System.Text.Json;
using System.Text.Json.Serialization;

namespace LinguaDesk.Infrastructure.Ai;

public static class EligibilityPrompt
{
    public const string PromptId = "eligibility.v1";

    private const string ResourceName =
        "LinguaDesk.Infrastructure.Ai.Prompts.eligibility.v1.txt";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private static readonly Lazy<(string Content, string Sha256)> Resource =
        new(() => PromptResource.Load(ResourceName));

    public static PromptSnapshot Create(string source, string? sourceHint = null)
    {
        ArgumentNullException.ThrowIfNull(source);

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
