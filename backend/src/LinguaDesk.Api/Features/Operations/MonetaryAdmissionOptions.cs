namespace LinguaDesk.Api.Features.Operations;

public sealed class MonetaryAdmissionOptions
{
    public const string SectionName = "MonetaryAdmission";

    public long MonthlyCapMinorUnits { get; set; }

    public string Currency { get; set; } = string.Empty;
}
