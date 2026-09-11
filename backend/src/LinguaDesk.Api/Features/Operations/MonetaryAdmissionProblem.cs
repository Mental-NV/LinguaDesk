namespace LinguaDesk.Api.Features.Operations;

public static class MonetaryAdmissionProblem
{
    public const string SuspensionCategory = "monetarySuspension";

    public const string IneligibleCategory = "monetaryIneligible";

    public const int SuspensionStatus = StatusCodes.Status503ServiceUnavailable;

    public const int IneligibleStatus = StatusCodes.Status422UnprocessableEntity;

    public static (int Status, string Category, string Title, string Detail) Describe(MonetaryAdmissionOutcome outcome) =>
        outcome switch
        {
            MonetaryAdmissionOutcome.DeniedOverCap => (
                SuspensionStatus,
                SuspensionCategory,
                "Paid processing suspended",
                "The configured monthly monetary ceiling has no remaining capacity for another paid attempt; no reservation was made. Status and usage reads remain available."),
            MonetaryAdmissionOutcome.ConfigurationInvalid => (
                SuspensionStatus,
                SuspensionCategory,
                "Paid processing suspended",
                "Paid admission is not configured; no reservation was made and nothing was dispatched."),
            MonetaryAdmissionOutcome.Ineligible => (
                IneligibleStatus,
                IneligibleCategory,
                "Attempt not eligible for paid dispatch",
                "The attempt has no usable price or bounded billable scope; nothing was reserved or dispatched."),
            _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, "Admitted and duplicate outcomes are not problem responses."),
        };
}
