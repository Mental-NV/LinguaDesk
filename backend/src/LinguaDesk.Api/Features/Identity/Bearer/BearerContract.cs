using System.ComponentModel;
using System.Text.Json.Serialization;
using LinguaDesk.Api.Features.Identity.Session;

namespace LinguaDesk.Api.Features.Identity.Bearer;

public sealed record BearerSignInRequest(
    [property: Description("Local account email address. Maximum 254 Unicode scalar values; surrounding whitespace is invalid.")]
    string Email,
    [property: Description("Write-only password containing 15 to 128 well-formed Unicode scalar values.")]
    string Password);

public sealed record BearerRefreshRequest(
    [property: Description("Write-only currently held opaque refresh token. Both held pair members are required.")]
    string RefreshToken,
    [property: Description("Write-only currently held opaque access token. Both held pair members are required.")]
    string AccessToken);

public sealed record BearerTokenPairResponse(
    [property: Description("Opaque protected access token. Present it as `Authorization: Bearer ...`; it is never set as a cookie.")]
    string AccessToken,
    [property: Description("Opaque protected refresh token used only with POST /api/accounts/bearer-refresh.")]
    string RefreshToken,
    [property: Description("Access-token lifetime in seconds; always 900.")]
    long ExpiresIn,
    [property: Description("Always `Bearer`.")]
    string TokenType,
    [property: Description("Current server-side account verification state.")]
    SessionVerificationStatus VerificationStatus);

public sealed record CurrentAccountResponse(
    [property: Description("Local account email address.")]
    string Email,
    [property: Description("Current server-side account verification state.")]
    SessionVerificationStatus VerificationStatus,
    [property: Description("Credential mode that authenticated this call.")]
    AuthMode AuthMode);

[JsonConverter(typeof(JsonStringEnumConverter<AuthMode>))]
public enum AuthMode
{
    [JsonStringEnumMemberName("bearer")]
    Bearer,
    [JsonStringEnumMemberName("cookie")]
    Cookie,
}
