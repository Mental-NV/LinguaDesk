namespace LinguaDesk.Api.Features.Operations;

public sealed class MonetaryCostLedger
{
    public string CostMonth { get; set; } = string.Empty;

    public long KnownSpendMinorUnits { get; set; }

    public long UnresolvedExposureMinorUnits { get; set; }
}

public static class MonetaryReservationStates
{
    public const string Reserved = "reserved";

    public const string Settled = "settled";

    public const string Released = "released";
}

public sealed class MonetaryAttemptReservation
{
    public int Id { get; set; }

    public string AttemptId { get; set; } = string.Empty;

    public string OperationReference { get; set; } = string.Empty;

    public int AttemptNumber { get; set; }

    public long BoundMinorUnits { get; set; }

    public string CostMonth { get; set; } = string.Empty;

    public string TariffReference { get; set; } = string.Empty;

    public string State { get; set; } = MonetaryReservationStates.Reserved;

    public long SettledActualMinorUnits { get; set; }

    public string? EvidenceReference { get; set; }

    public DateTimeOffset CreatedUtc { get; set; }

    public DateTimeOffset? ReconciledUtc { get; set; }
}
