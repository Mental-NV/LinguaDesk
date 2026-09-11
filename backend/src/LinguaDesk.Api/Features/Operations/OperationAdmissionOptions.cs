namespace LinguaDesk.Api.Features.Operations;

public sealed class OperationAdmissionOptions
{
    public const string SectionName = "Operations";

    public int UserDailyAllowanceCharacters { get; set; } = 20000;

    public int GlobalDailyAllowanceCharacters { get; set; } = 2000000;
}
