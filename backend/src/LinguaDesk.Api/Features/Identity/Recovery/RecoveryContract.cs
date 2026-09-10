using System.ComponentModel;
using System.Text.Json.Serialization;

namespace LinguaDesk.Api.Features.Identity.Recovery;

public sealed record ForgotPasswordRequest(
    [property: Description("Local account email address. Maximum 254 Unicode scalar values; surrounding whitespace is invalid.")]
    string Email);

public sealed record ForgotPasswordAccepted(
    [property: Description("Always `passwordResetRequested` for a syntactically valid request.")]
    ForgotPasswordStatus Status,
    [property: Description("Fixed reset-delivery interval in seconds; it does not disclose account or cooldown state.")]
    int RetryAfterSeconds);

[JsonConverter(typeof(JsonStringEnumConverter<ForgotPasswordStatus>))]
public enum ForgotPasswordStatus
{
    [JsonStringEnumMemberName("passwordResetRequested")]
    PasswordResetRequested,
}

public sealed record ResetPasswordRequest(
    [property: Description("Opaque local-account identifier delivered with the reset code. Maximum 450 characters.")]
    string UserId,
    [property: Description("Unpadded base64url encoding of the delivered password-reset token. Maximum 4096 characters.")]
    string Code,
    [property: Description("Write-only replacement password containing 15 to 128 well-formed Unicode scalar values.")]
    string NewPassword);

public sealed record ResetPasswordAccepted(
    [property: Description("Always `passwordReset` after a successful password reset.")]
    ResetPasswordStatus Status);

[JsonConverter(typeof(JsonStringEnumConverter<ResetPasswordStatus>))]
public enum ResetPasswordStatus
{
    [JsonStringEnumMemberName("passwordReset")]
    PasswordReset,
}

public sealed class RecoveryProblemDetails
{
    [Description("RFC 9457 problem type reference.")]
    public string? Type { get; init; }
    [Description("Short, stable problem summary.")]
    public required string Title { get; init; }
    [Description("HTTP status code for this occurrence.")]
    public required int Status { get; init; }
    [Description("Safe explanation that never echoes submitted account, token or password values.")]
    public required string Detail { get; init; }
    [Description("LinguaDesk error category: `invalidRequest`, `invalidOrExpiredPasswordReset`, or `availability`.")]
    public required string Category { get; init; }
    [Description("Opaque request correlation identifier.")]
    public required string CorrelationId { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Description("Field messages keyed only by `email` or `newPassword`; present for field validation failures.")]
    public Dictionary<string, string[]>? Errors { get; init; }
}
