using LinguaDesk.Api.Benchmark;

namespace LinguaDesk.Api.Benchmark.Tests;

[TestClass]
public sealed class CompositionAuditorTests
{
    private static List<ExpectedCombo> TwoCombos() =>
    [
        new("combo-a", "translation", "en->zh", "band-1-100", "hash-a", 64, 1, 100),
        new("combo-b", "translation", "en->zh", "band-101-500", "hash-b", 320, 101, 500),
    ];

    private static List<ObservedRequest> PassingRun() =>
    [
        new("op-a1", "combo-a", IsRepeat: false, SubmittedFresh: true, HasTiming: true, "hash-a", 64),
        new("op-a2", "combo-a", IsRepeat: true, SubmittedFresh: true, HasTiming: true, "hash-a", 64),
        new("op-b1", "combo-b", IsRepeat: false, SubmittedFresh: true, HasTiming: true, "hash-b", 320),
        new("op-b2", "combo-b", IsRepeat: true, SubmittedFresh: true, HasTiming: true, "hash-b", 320),
    ];

    [TestMethod]
    public void BalancedOriginalPlusRepeatPasses()
    {
        var result = CompositionAuditor.Audit(TwoCombos(), PassingRun(), expectedTotal: 4);

        Assert.IsTrue(result.Passed);
        Assert.IsEmpty(result.Violations);
    }

    [TestMethod]
    public void MissingBandCellFailsTheRun()
    {
        var observed = PassingRun().Where(request => request.ComboId == "combo-a").ToList();

        var result = CompositionAuditor.Audit(TwoCombos(), observed, expectedTotal: 4);

        Assert.IsFalse(result.Passed);
        Assert.IsTrue(result.Violations.Any(violation => violation.Contains("combo-b", StringComparison.Ordinal)));
        Assert.IsTrue(result.Violations.Any(violation => violation.Contains('4')));
    }

    [TestMethod]
    public void RepeatedIdentityFailsTheRun()
    {
        var observed = PassingRun();
        observed[1] = observed[1] with { OperationId = "op-a1" };

        var result = CompositionAuditor.Audit(TwoCombos(), observed, expectedTotal: 4);

        Assert.IsFalse(result.Passed);
        Assert.IsTrue(result.Violations.Any(violation => violation.Contains("repeats", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void ReplayedCompletionFailsTheRun()
    {
        var freshReplay = PassingRun();
        freshReplay[1] = freshReplay[1] with { SubmittedFresh = false, HasTiming = false };

        var result = CompositionAuditor.Audit(TwoCombos(), freshReplay, expectedTotal: 4);

        Assert.IsFalse(result.Passed);
        Assert.IsTrue(result.Violations.Any(violation => violation.Contains("replayed", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void DroppedRequestFailsTheRun()
    {
        var observed = PassingRun()[..3];

        var result = CompositionAuditor.Audit(TwoCombos(), observed, expectedTotal: 4);

        Assert.IsFalse(result.Passed);
        Assert.IsTrue(result.Violations.Any(violation => violation.Contains("dropped", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void MismatchedSourceFailsTheRun()
    {
        var observed = PassingRun();
        observed[2] = observed[2] with { SourceSha256 = "hash-tampered", SourceLength = 321 };

        var result = CompositionAuditor.Audit(TwoCombos(), observed, expectedTotal: 4);

        Assert.IsFalse(result.Passed);
        Assert.IsTrue(result.Violations.Any(violation => violation.Contains("combo-b", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void UndeclaredComboFailsTheRun()
    {
        var observed = PassingRun();
        observed.Add(new ObservedRequest("op-extra", "combo-extra", false, true, true, "hash-x", 10));

        var result = CompositionAuditor.Audit(TwoCombos(), observed, expectedTotal: 4);

        Assert.IsFalse(result.Passed);
        Assert.IsTrue(result.Violations.Any(violation => violation.Contains("combo-extra", StringComparison.Ordinal)));
    }
}
