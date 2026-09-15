using LinguaDesk.Api.Benchmark;

namespace LinguaDesk.Api.Benchmark.Tests;

[TestClass]
public sealed class BenchmarkMathTests
{
    [TestMethod]
    public void FullDenominatorCountsOnlyTimelySuccesses()
    {
        var (within, total, rate) = BenchmarkMath.SuccessWithinTarget(21, 24);

        Assert.AreEqual(21, within);
        Assert.AreEqual(24, total);
        Assert.AreEqual(21.0 / 24.0, rate);
    }

    [TestMethod]
    public void AllFailuresYieldZeroRateOverTheFullDenominator()
    {
        var (within, total, rate) = BenchmarkMath.SuccessWithinTarget(0, 24);

        Assert.AreEqual(0, within);
        Assert.AreEqual(24, total);
        Assert.AreEqual(0.0, rate);
    }

    [TestMethod]
    public void TimelyCountCanNeverExceedMeasuredRequests()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => BenchmarkMath.SuccessWithinTarget(25, 24));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => BenchmarkMath.SuccessWithinTarget(1, 0));
    }

    [TestMethod]
    public void FailuresMapToInfiniteEffectiveLatency()
    {
        Assert.AreEqual(12.0, BenchmarkMath.EffectiveLatencyMs(succeeded: true, measuredMs: 12.0));
        Assert.AreEqual(double.PositiveInfinity, BenchmarkMath.EffectiveLatencyMs(succeeded: false, measuredMs: 12.0));
        Assert.AreEqual(double.PositiveInfinity, BenchmarkMath.EffectiveLatencyMs(succeeded: false, measuredMs: 60000.0));
    }

    [TestMethod]
    public void NearestRankUsesTheFullDenominator()
    {
        // 23 fast successes plus 1 failure at infinite latency: p95 ranks
        // ceil(0.95 * 24) = 23, still a success value, while p100 is infinite.
        var values = Enumerable.Repeat(40.0, 23)
            .Concat([double.PositiveInfinity])
            .ToList();

        Assert.AreEqual(40.0, BenchmarkMath.PercentileNearestRank(values, 50));
        Assert.AreEqual(40.0, BenchmarkMath.PercentileNearestRank(values, 95));
        Assert.AreEqual(double.PositiveInfinity, BenchmarkMath.PercentileNearestRank(values, 100));
    }

    [TestMethod]
    public void PercentileFallsOnInfinityWhenFailuresOutweighTheRank()
    {
        // 21 successes plus 3 failures: p95 ranks 23rd, which is infinite.
        var values = Enumerable.Repeat(40.0, 21)
            .Concat([double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity])
            .ToList();

        Assert.AreEqual(40.0, BenchmarkMath.PercentileNearestRank(values, 50));
        Assert.AreEqual(double.PositiveInfinity, BenchmarkMath.PercentileNearestRank(values, 95));
    }

    [TestMethod]
    public void SuccessfulOnlyPercentileWouldHideFailures()
    {
        // A successful-only p95 over the 21 successes claims 40ms while the
        // full-denominator p95 is infinite: this contrast is why the harness
        // never computes successful-only percentiles.
        var full = Enumerable.Repeat(40.0, 21)
            .Concat([double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity])
            .ToList();
        var successfulOnly = Enumerable.Repeat(40.0, 21).ToList();

        Assert.AreEqual(40.0, BenchmarkMath.PercentileNearestRank(successfulOnly, 95));
        Assert.AreEqual(double.PositiveInfinity, BenchmarkMath.PercentileNearestRank(full, 95));
    }

    [TestMethod]
    public void NearestRankHandlesEdges()
    {
        Assert.AreEqual(7.0, BenchmarkMath.PercentileNearestRank([7.0], 95));
        Assert.AreEqual(1.0, BenchmarkMath.PercentileNearestRank([1.0, 2.0, 3.0, 4.0], 25));
        Assert.AreEqual(2.0, BenchmarkMath.PercentileNearestRank([1.0, 2.0, 3.0, 4.0], 50));
        Assert.AreEqual(4.0, BenchmarkMath.PercentileNearestRank([1.0, 2.0, 3.0, 4.0], 100));
        Assert.ThrowsExactly<ArgumentException>(() => BenchmarkMath.PercentileNearestRank([2.0, 1.0], 50));
    }
}
