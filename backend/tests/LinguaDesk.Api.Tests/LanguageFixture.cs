using System.Text.Json;
using LinguaDesk.Core;
namespace LinguaDesk.Api.Tests;
internal static class LanguageFixture
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };
    internal static readonly LanguageDefinition[] Languages = Load();
    private static LanguageDefinition[] Load()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "languages.v1.json")));
        return JsonSerializer.Deserialize<LanguageDefinition[]>(document.RootElement.GetProperty("languages").GetRawText(), Options)!;
    }
    internal static string[] Directions => Languages.SelectMany(source => Languages.Where(target => target.Id != source.Id).Select(target => source.Id + ":" + target.Id)).ToArray();
}
