using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace LinguaDesk.Infrastructure.Ai;

/// <summary>Model-facing JSON data keeps native script letters readable while the serializer escapes JSON syntax and controls.</summary>
internal static class PromptDataJson
{
    internal static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
    };
}
