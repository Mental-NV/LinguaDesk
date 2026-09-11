using System.Text.Json;
using System.Text.Json.Serialization;

namespace LinguaDesk.Infrastructure.Ai;

public static class RewritingPrompt
{
    public const string PromptId = "rewriting.v1";

    private const string ResourceName =
        "LinguaDesk.Infrastructure.Ai.Prompts.rewriting.v1.txt";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private static readonly Lazy<(string Content, string Sha256)> Resource =
        new(() => PromptResource.Load(ResourceName));

    public static PromptSnapshot Create(string source, string sourceLanguage, string mode)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceLanguage);
        ArgumentException.ThrowIfNullOrWhiteSpace(mode);

        var userData = JsonSerializer.Serialize(
            new RewritingUserData(source, sourceLanguage, mode),
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

    private sealed record RewritingUserData(
        [property: JsonPropertyOrder(0)] string Source,
        [property: JsonPropertyOrder(1)] string SourceLanguage,
        [property: JsonPropertyOrder(2)] string Mode);
}
