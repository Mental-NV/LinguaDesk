using LinguaDesk.Api.Benchmark;

namespace LinguaDesk.Api.Benchmark.Tests;

[TestClass]
public sealed class ManifestLoaderTests
{
    private static string FixturePath(string name) =>
        Path.Combine(AppContext.BaseDirectory, "Fixtures", name);

    [TestMethod]
    public void ShippedManifestExpandsToTwentyFourRequests()
    {
        var manifest = ManifestLoader.Load(FixturePath("m037-rehearsal-24.json"));

        Assert.AreEqual("benchmark-manifest.v1", manifest.SchemaRevision);
        Assert.AreEqual(4, manifest.Concurrency);
        Assert.AreEqual(37037, manifest.InterleaveSeed);

        var items = ManifestLoader.Expand(manifest);

        Assert.HasCount(24, items);
        Assert.AreEqual(12, items.Count(item => !item.IsRepeat));
        Assert.AreEqual(12, items.Count(item => item.IsRepeat));
        Assert.AreEqual(12, items.Select(item => item.ComboId).Distinct().Count());
        Assert.AreEqual(24, items.Select(item => item.SequenceIndex).Distinct().Count());
    }

    [TestMethod]
    public void ShippedManifestMirrorsSection61Structurally()
    {
        var manifest = ManifestLoader.Load(FixturePath("m037-rehearsal-24.json"));
        var items = ManifestLoader.Expand(manifest);

        var translation = items.Where(item => item.Family == "translation").ToList();
        var rewriting = items.Where(item => item.Family == "rewriting").ToList();
        Assert.HasCount(12, translation);
        Assert.HasCount(12, rewriting);

        // Two translation directions x three bands x original-plus-repeat.
        Assert.AreEqual(2, translation.Select(item => item.Route).Distinct().Count());
        foreach (var band in new[] { "band-1-100", "band-101-500", "band-501-1000" })
        {
            Assert.AreEqual(4, translation.Count(item => item.BandId == band));
            Assert.AreEqual(4, rewriting.Count(item => item.BandId == band));
        }

        // Two rewriting cells x three bands x original-plus-repeat.
        Assert.AreEqual(2, rewriting.Select(item => item.Route).Distinct().Count());

        // Every repeat shares its combo source with a fresh identity minted at run time.
        foreach (var group in items.GroupBy(item => item.ComboId))
        {
            Assert.AreEqual(2, group.Count());
            Assert.AreEqual(1, group.Count(item => !item.IsRepeat));
            Assert.AreEqual(1, group.Count(item => item.IsRepeat));
            Assert.AreEqual(1, group.Select(item => item.SourceSha256).Distinct().Count());
        }
    }

    [TestMethod]
    public void ShippedManifestBalancesSelectionAndScripts()
    {
        var manifest = ManifestLoader.Load(FixturePath("m037-rehearsal-24.json"));
        var items = ManifestLoader.Expand(manifest).Where(item => !item.IsRepeat).ToList();

        Assert.IsGreaterThan(0, items.Count(item => item.SourceSelection is null), "automatic selection is represented");
        Assert.IsGreaterThan(0, items.Count(item => item.SourceSelection is not null), "manual selection is represented");

        var simplified = items.Count(item => item.SourceText.Contains("简体中文", StringComparison.Ordinal));
        var traditional = items.Count(item => item.SourceText.Contains("繁體中文", StringComparison.Ordinal));
        Assert.IsGreaterThan(0, simplified, "simplified Chinese input is represented");
        Assert.IsGreaterThan(0, traditional, "traditional Chinese input is represented");
    }

    [TestMethod]
    public void InterleaveOrderIsDeterministicForTheRecordedSeed()
    {
        var manifest = ManifestLoader.Load(FixturePath("m037-rehearsal-24.json"));

        var first = ManifestLoader.Expand(manifest).Select(item => item.ComboId + (item.IsRepeat ? "#r" : "#o"));
        var second = ManifestLoader.Expand(manifest).Select(item => item.ComboId + (item.IsRepeat ? "#r" : "#o"));

        CollectionAssert.AreEqual(first.ToList(), second.ToList());
    }

    [TestMethod]
    public void SuccessorMappingCoversEveryFullRule()
    {
        var manifest = ManifestLoader.Load(FixturePath("m037-rehearsal-24.json"));
        var rules = ReportWriter.LoadRules(FixturePath("full-360-rules.json"));

        Assert.HasCount(12, rules.Rules);
        foreach (var rule in rules.Rules)
        {
            var entry = manifest.SuccessorMapping.FirstOrDefault(item => item.RuleId == rule.Id);
            Assert.IsNotNull(entry, $"Rule {rule.Id} has no rehearsal analogue or deferral.");
            Assert.IsTrue(
                entry.RehearsedAs is not null || entry.SuccessorOnly is not null,
                $"Rule {rule.Id} names neither an analogue nor a deferral.");
        }
    }

    [TestMethod]
    public void LoaderRejectsUnknownBandsAndSelections()
    {
        var manifest = ManifestLoader.Load(FixturePath("m037-rehearsal-24.json"));
        var broken = manifest with
        {
            TranslationDirections =
            [
                new TranslationDirection("en", "xx", manifest.TranslationDirections[0].Combos),
            ],
        };

        Assert.ThrowsExactly<InvalidOperationException>(() => ManifestLoader.Expand(broken));
    }
}
