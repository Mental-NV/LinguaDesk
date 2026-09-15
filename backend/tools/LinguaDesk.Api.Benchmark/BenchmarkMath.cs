namespace LinguaDesk.Api.Benchmark;

/// <summary>
/// Full-denominator benchmark math: failures and timeouts are never timely
/// completions (infinite effective latency), percentiles use nearest rank over
/// every measured request, and families are assessed separately. A
/// successful-only percentile is never computed here.
/// </summary>
public static class BenchmarkMath
{
    public static (int WithinTarget, int Total, double Rate) SuccessWithinTarget(
        int successWithinTargetCount, int totalMeasured)
    {
        if (totalMeasured <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalMeasured), "The denominator must be positive.");
        }

        if (successWithinTargetCount < 0 || successWithinTargetCount > totalMeasured)
        {
            throw new ArgumentOutOfRangeException(
                nameof(successWithinTargetCount), "Timely successes cannot exceed measured requests.");
        }

        return (successWithinTargetCount, totalMeasured, (double)successWithinTargetCount / totalMeasured);
    }

    /// <summary>
    /// Nearest-rank percentile over ascending values. Callers map unsuccessful
    /// requests to <see cref="double.PositiveInfinity"/> first so the full
    /// denominator shapes the rank.
    /// </summary>
    public static double PercentileNearestRank(IReadOnlyList<double> ascendingValues, int percentile)
    {
        ArgumentNullException.ThrowIfNull(ascendingValues);
        if (ascendingValues.Count == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ascendingValues), "Percentiles need at least one value.");
        }

        if (percentile < 0 || percentile > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(percentile), "The percentile must sit between 0 and 100.");
        }

        for (var index = 1; index < ascendingValues.Count; index++)
        {
            if (ascendingValues[index] < ascendingValues[index - 1])
            {
                throw new ArgumentException("Percentile input must arrive in ascending order.", nameof(ascendingValues));
            }
        }

        var rank = (int)Math.Ceiling(percentile / 100.0 * ascendingValues.Count);
        var zeroBased = Math.Clamp(rank - 1, 0, ascendingValues.Count - 1);
        return ascendingValues[zeroBased];
    }

    public static double EffectiveLatencyMs(bool succeeded, double measuredMs) =>
        succeeded ? measuredMs : double.PositiveInfinity;
}
