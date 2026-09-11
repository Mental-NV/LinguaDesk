namespace LinguaDesk.Infrastructure.Ai;

public interface IChainClock
{
    DateTimeOffset UtcNow { get; }
}

public sealed class SystemChainClock : IChainClock
{
    public static readonly SystemChainClock Instance = new();

    private SystemChainClock()
    {
    }

    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

public sealed record ChainPolicy(
    TimeSpan OverallDeadline,
    TimeSpan EligibilityTimeout,
    TimeSpan TransformationTimeout,
    TimeSpan FinalizationReserve,
    IChainClock Clock)
{
    public static ChainPolicy Default { get; } = new(
        TimeSpan.FromSeconds(30),
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(10),
        TimeSpan.FromSeconds(2),
        SystemChainClock.Instance);

    public ChainPolicy WithClock(IChainClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        return this with { Clock = clock };
    }

    public void Validate()
    {
        var errors = new List<string>();

        if (OverallDeadline <= TimeSpan.Zero)
        {
            errors.Add("OverallDeadline must be positive.");
        }

        if (EligibilityTimeout <= TimeSpan.Zero)
        {
            errors.Add("EligibilityTimeout must be positive.");
        }

        if (TransformationTimeout <= TimeSpan.Zero)
        {
            errors.Add("TransformationTimeout must be positive.");
        }

        if (FinalizationReserve < TimeSpan.Zero)
        {
            errors.Add("FinalizationReserve must not be negative.");
        }

        if (errors.Count == 0 && FinalizationReserve >= OverallDeadline)
        {
            errors.Add("FinalizationReserve must leave a positive overall budget.");
        }

        if (errors.Count == 0
            && (EligibilityTimeout + FinalizationReserve > OverallDeadline
                || TransformationTimeout + FinalizationReserve > OverallDeadline))
        {
            errors.Add("No complete stage allowance fits inside the overall deadline minus the finalization reserve.");
        }

        if (Clock is null)
        {
            errors.Add("Clock is required.");
        }

        if (errors.Count > 0)
        {
            throw new InvalidOperationException($"The chain deadline policy is invalid: {string.Join(" ", errors)}");
        }
    }
}
