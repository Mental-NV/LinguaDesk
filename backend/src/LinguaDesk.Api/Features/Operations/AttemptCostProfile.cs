namespace LinguaDesk.Api.Features.Operations;

public sealed record AttemptCostProfile(
    string Currency,
    string TariffSource,
    string TariffCheckDate,
    decimal PeakInputPerMillionTokens,
    decimal PeakOutputPerMillionTokens,
    int MaxInputTokens,
    int MaxOutputTokens,
    long OtherBillableMinorUnits);
