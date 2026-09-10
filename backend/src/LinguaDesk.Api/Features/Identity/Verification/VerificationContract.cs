using System.ComponentModel;
using System.Text.Json.Serialization;

namespace LinguaDesk.Api.Features.Identity.Verification;

public sealed record ConfirmEmailRequest(
    [property: Description("Opaque local-account identifier delivered with the verification code. Maximum 450 characters.")]
    string UserId,
    [property: Description("Unpadded base64url encoding of the delivered email-confirmation token. Maximum 4096 characters.")]
    string Code);

public sealed record ConfirmEmailAccepted(
    [property: Description("Always `verified` after a successful or idempotently repeated confirmation.")]
    ConfirmationStatus Status);

[JsonConverter(typeof(JsonStringEnumConverter<ConfirmationStatus>))]
public enum ConfirmationStatus
{
    [JsonStringEnumMemberName("verified")]
    Verified,
}

public sealed record ResendVerificationRequest(
    [property: Description("Local account email address. Maximum 254 Unicode scalar values; surrounding whitespace is invalid.")]
    string Email);

public sealed record ResendVerificationAccepted(
    [property: Description("Always `verificationRequested` for a syntactically valid request.")]
    ResendVerificationStatus Status,
    [property: Description("Fixed resend interval in seconds; it does not disclose account or cooldown state.")]
    int RetryAfterSeconds);

[JsonConverter(typeof(JsonStringEnumConverter<ResendVerificationStatus>))]
public enum ResendVerificationStatus
{
    [JsonStringEnumMemberName("verificationRequested")]
    VerificationRequested,
}

public sealed class VerificationProblemDetails
{
    [Description("RFC 9457 problem type reference.")]
    public string? Type { get; init; }
    [Description("Short, stable problem summary.")]
    public required string Title { get; init; }
    [Description("HTTP status code for this occurrence.")]
    public required int Status { get; init; }
    [Description("Safe explanation that never echoes submitted account or verification values.")]
    public required string Detail { get; init; }
    [Description("LinguaDesk error category: `invalidRequest`, `invalidOrExpiredVerification`, or `availability`.")]
    public required string Category { get; init; }
    [Description("Opaque request correlation identifier.")]
    public required string CorrelationId { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Description("Field messages keyed only by `email`, `userId`, or `code`; present for field validation failures.")]
    public Dictionary<string, string[]>? Errors { get; init; }
}
