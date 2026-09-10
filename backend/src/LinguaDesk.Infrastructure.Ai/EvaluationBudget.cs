namespace LinguaDesk.Infrastructure.Ai;

public sealed class EvaluationBudget
{
    private readonly object gate = new();
    private int dispatchesUsed;
    private decimal reservedUsd;
    private decimal unresolvedUsd;

    public EvaluationBudget(int maxDispatches, decimal maxSpendUsd)
    {
        if (maxDispatches <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxDispatches), "The evaluation budget needs a positive finite dispatch cap.");
        }

        if (!(maxSpendUsd > 0))
        {
            throw new ArgumentOutOfRangeException(nameof(maxSpendUsd), "The evaluation budget needs a positive finite spend cap.");
        }

        MaxDispatches = maxDispatches;
        MaxSpendUsd = maxSpendUsd;
    }

    public int MaxDispatches { get; }

    public decimal MaxSpendUsd { get; }

    public int DispatchesUsed
    {
        get
        {
            lock (gate)
            {
                return dispatchesUsed;
            }
        }
    }

    public decimal ReservedUsd
    {
        get
        {
            lock (gate)
            {
                return reservedUsd;
            }
        }
    }

    public decimal UnresolvedUsd
    {
        get
        {
            lock (gate)
            {
                return unresolvedUsd;
            }
        }
    }

    public static decimal UpperBoundUsd(
        long inputTokensUpperBound,
        int maxOutputTokens,
        decimal peakInputPerMillionTokens,
        decimal peakOutputPerMillionTokens)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(inputTokensUpperBound);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxOutputTokens);

        var raw = ((decimal)inputTokensUpperBound * peakInputPerMillionTokens
            + (decimal)maxOutputTokens * peakOutputPerMillionTokens) / 1_000_000m;
        return Math.Ceiling(raw * 1_000_000m) / 1_000_000m;
    }

    public bool TryReserve(decimal upperBoundUsd, out BudgetReservation reservation)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(upperBoundUsd);

        lock (gate)
        {
            if (dispatchesUsed >= MaxDispatches || reservedUsd + upperBoundUsd > MaxSpendUsd)
            {
                reservation = null!;
                return false;
            }

            dispatchesUsed++;
            reservedUsd += upperBoundUsd;
            reservation = new BudgetReservation(this, upperBoundUsd);
            return true;
        }
    }

    public void Settle(BudgetReservation reservation, decimal? actualUsd)
    {
        ArgumentNullException.ThrowIfNull(reservation);

        lock (gate)
        {
            if (reservation.Settled)
            {
                throw new InvalidOperationException("The reservation was already settled.");
            }

            reservation.MarkSettled();
            if (actualUsd.HasValue && actualUsd.Value >= 0 && actualUsd.Value <= reservation.AmountUsd)
            {
                reservedUsd -= reservation.AmountUsd - actualUsd.Value;
            }
            else
            {
                unresolvedUsd += reservation.AmountUsd;
            }
        }
    }
}

public sealed class BudgetReservation
{
    internal BudgetReservation(EvaluationBudget budget, decimal amountUsd)
    {
        Budget = budget;
        AmountUsd = amountUsd;
    }

    public EvaluationBudget Budget { get; }

    public decimal AmountUsd { get; }

    public bool Settled { get; private set; }

    internal void MarkSettled() => Settled = true;
}
