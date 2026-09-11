namespace LinguaDesk.Api.Features.Operations;

public sealed class OperationSubmission
{
    public int Id { get; set; }

    public string AccountId { get; set; } = string.Empty;

    public string OperationId { get; set; } = string.Empty;

    public string Family { get; set; } = string.Empty;

    public string Fingerprint { get; set; } = string.Empty;

    public int ScalarCount { get; set; }

    public string AdmissionDay { get; set; } = string.Empty;

    public DateTimeOffset DeadlineUtc { get; set; }

    public string State { get; set; } = OperationStates.Pending;

    public DateTimeOffset CreatedUtc { get; set; }
}

public static class OperationStates
{
    public const string Pending = "pending";
}

public sealed class CharacterLedgerEntry
{
    public string Scope { get; set; } = string.Empty;

    public string AccountId { get; set; } = string.Empty;

    public string Day { get; set; } = string.Empty;

    public int ConsumedCharacters { get; set; }

    public int ReservedCharacters { get; set; }
}

public static class CharacterLedgerScopes
{
    public const string User = "user";

    public const string Global = "global";

    public const string GlobalAccountId = "";
}

public sealed class LedgerRevision
{
    public const int SingletonId = 1;

    public int Id { get; set; }

    public long Value { get; set; }
}
