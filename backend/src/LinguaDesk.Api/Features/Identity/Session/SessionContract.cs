using System.ComponentModel;
using System.Text.Json.Serialization;

namespace LinguaDesk.Api.Features.Identity.Session;

public sealed record AntiforgeryTokenResponse(
    [property: Description("Opaque request token. Send it only in the X-LinguaDesk-Antiforgery header with the matching antiforgery cookie.")]
    string RequestToken,
    [property: Description("Always `X-LinguaDesk-Antiforgery`.")]
    string HeaderName);

public sealed record SignInRequest(
    [property: Description("Local account email address. Maximum 254 Unicode scalar values; surrounding whitespace is invalid.")]
    string Email,
    [property: Description("Write-only password containing 15 to 128 well-formed Unicode scalar values.")]
    string Password);

public sealed record AccountSessionResponse(
    [property: Description("Always `signedIn` for a current browser session.")]
    SessionStatus Status,
    [property: Description("Current server-side account verification state.")]
    SessionVerificationStatus VerificationStatus,
    [property: Description("UTC expiry of the fixed eight-hour protected server ticket; the cookie itself is nonpersistent.")]
    DateTimeOffset ExpiresAtUtc);

[JsonConverter(typeof(JsonStringEnumConverter<SessionStatus>))]
public enum SessionStatus
{
    [JsonStringEnumMemberName("signedIn")]
    SignedIn,
}

[JsonConverter(typeof(JsonStringEnumConverter<SessionVerificationStatus>))]
public enum SessionVerificationStatus
{
    [JsonStringEnumMemberName("verified")]
    Verified,
    [JsonStringEnumMemberName("verificationRequired")]
    VerificationRequired,
}

public sealed class SessionProblemDetails
{
    [Description("RFC 9457 problem type reference.")]
    public string? Type { get; init; }
    [Description("Short, stable problem summary.")]
    public required string Title { get; init; }
    [Description("HTTP status code for this occurrence.")]
    public required int Status { get; init; }
    [Description("Safe explanation that never echoes account or authentication values.")]
    public required string Detail { get; init; }
    [Description("LinguaDesk error category.")]
    public required string Category { get; init; }
    [Description("Opaque request correlation identifier.")]
    public required string CorrelationId { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Description("Field messages keyed only by `email` or `password`.")]
    public Dictionary<string, string[]>? Errors { get; init; }
}
