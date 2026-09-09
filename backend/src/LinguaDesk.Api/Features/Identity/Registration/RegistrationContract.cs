using System.ComponentModel;
using System.Text.Json.Serialization;

namespace LinguaDesk.Api.Features.Identity.Registration;

public sealed record RegistrationRequest(
    [property: Description("Local account email address. Maximum 254 Unicode scalar values; surrounding whitespace is invalid.")]
    string Email,
    [property: Description("Write-only password containing 15 to 128 well-formed Unicode scalar values.")]
    string Password);

public sealed record RegistrationAccepted(
    [property: Description("Always `verificationRequired` for an accepted new or existing normalized email.")]
    RegistrationStatus Status);

[JsonConverter(typeof(JsonStringEnumConverter<RegistrationStatus>))]
public enum RegistrationStatus
{
    [JsonStringEnumMemberName("verificationRequired")]
    VerificationRequired,
}

public sealed class RegistrationProblemDetails
{
    [Description("RFC 9457 problem type reference.")]
    public string? Type { get; init; }
    [Description("Short, stable problem summary.")]
    public required string Title { get; init; }
    [Description("HTTP status code for this occurrence.")]
    public required int Status { get; init; }
    [Description("Safe explanation that never echoes submitted account values.")]
    public required string Detail { get; init; }
    [Description("LinguaDesk error category: `invalidRequest` or `availability`.")]
    public required string Category { get; init; }
    [Description("Opaque request correlation identifier.")]
    public required string CorrelationId { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Description("Field messages keyed only by `email` or `password`; present for field validation failures.")]
    public Dictionary<string, string[]>? Errors { get; init; }
}
