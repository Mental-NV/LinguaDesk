using System.Security.Cryptography;
using System.Text.Json;
using LinguaDesk.Core;

namespace LinguaDesk.Infrastructure.Ai.Tests;

[TestClass]
public sealed class AlternativesCorpusTests
{
    private static JsonDocument Load(string name, string hash)
    {
        var bytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "alternatives", name));
        Assert.AreEqual(hash, Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant());
        return JsonDocument.Parse(bytes);
    }

    [TestMethod]
    public void FrozenDevelopmentAndQualificationCoverEveryCellWithSeparateReviewedMeanings()
    {
        using var development = Load("development.v1.json", "77e7e8d1b8486ad8735e333afb415469776e6e6c71f9c3df8c9ae193ca5f210d");
        using var qualification = Load("qualification.v2.json", "f795c315fd3c049f43a43c7669534b11d7e571b2a5f5cc45454be80d2ac84ba4");
        using var calibration = Load("calibration.v2.json", "c319a7b8ee9504b95bc3d8e9f02e24d52f67482a3eab4a7190607c1471e836fb");
        var dev = development.RootElement.GetProperty("cases").EnumerateArray().ToArray();
        var release = qualification.RootElement.GetProperty("cases").EnumerateArray().ToArray();
        var pool = calibration.RootElement.GetProperty("cases").EnumerateArray().ToArray();
        Assert.HasCount(36, dev); Assert.HasCount(360, release); Assert.HasCount(40, pool);
        foreach (var row in dev.Concat(release).Concat(pool))
        {
            Assert.IsTrue(AlternativesInputPolicy.Validate(row.GetProperty("context").GetString()!, row.GetProperty("start").GetInt32(), row.GetProperty("length").GetInt32(), Guid.NewGuid(), row.GetProperty("language").GetString()!, row.GetProperty("mode").GetString()).IsValid, row.GetProperty("id").GetString());
            Assert.IsGreaterThanOrEqualTo(4, row.GetProperty("meaningConstraints").GetArrayLength());
        }
        foreach (var language in ProductCatalog.Languages)
        {
            Assert.HasCount(10, pool.Where(row => row.GetProperty("language").GetString() == language.Id));
            foreach (var mode in ProductCatalog.RewritingModes)
            {
                Assert.HasCount(1, dev.Where(row => Cell(row, language.Id, mode.Id)));
                var cell = release.Where(row => Cell(row, language.Id, mode.Id)).ToArray();
                Assert.HasCount(10, cell);
                Assert.IsTrue(cell.Any(row => row.GetProperty("tags").EnumerateArray().Any(tag => tag.GetString() == "punctuation-free")));
                if (language.Id == "zh")
                {
                    Assert.HasCount(5, cell.Where(row => row.GetProperty("script").GetString() == "traditional"));
                    Assert.HasCount(5, cell.Where(row => row.GetProperty("script").GetString() == "simplified"));
                }
            }
        }
        var contexts = dev.Select(row => row.GetProperty("context").GetString()).ToHashSet(StringComparer.Ordinal);
        Assert.IsFalse(release.Any(row => contexts.Contains(row.GetProperty("context").GetString())));
    }

    private static bool Cell(JsonElement row, string language, string mode) => row.GetProperty("language").GetString() == language && row.GetProperty("mode").GetString() == mode;
}
