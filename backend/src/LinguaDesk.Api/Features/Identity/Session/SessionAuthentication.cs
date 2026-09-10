using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace LinguaDesk.Api.Features.Identity.Session;

internal static class SessionAuthentication
{
    public const string PolicyScheme = "LinguaDesk.Account";
    public const string UnsupportedBearerScheme = "LinguaDesk.UnsupportedBearer";
    public const string SessionCookieName = "__Host-LinguaDesk.Session";
    public const string AntiforgeryCookieName = "__Host-LinguaDesk.Antiforgery";
    public const string AntiforgeryHeaderName = "X-LinguaDesk-Antiforgery";
    public const string AvailabilityItem = "LinguaDesk.AccountSessionAvailabilityFailure";
    public static readonly TimeSpan CookieLifetime = TimeSpan.FromHours(8);
}

internal sealed class UnsupportedBearerAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync() =>
        Task.FromResult(AuthenticateResult.Fail("Bearer authentication is not available."));
}

internal sealed class CurrentAccountCookieEvents(UserManager<IdentityUser> users, TimeProvider timeProvider) : CookieAuthenticationEvents
{
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        if (context.Properties.ExpiresUtc is null || timeProvider.GetUtcNow() >= context.Properties.ExpiresUtc.Value)
        {
            context.RejectPrincipal();
            return;
        }

        var userId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        var ticketStamp = context.Principal?.FindFirstValue(users.Options.ClaimsIdentity.SecurityStampClaimType);
        if (userId is null || ticketStamp is null)
        {
            context.RejectPrincipal();
            return;
        }

        IdentityUser? current;
        try
        {
            current = await users.FindByIdAsync(userId);
        }
        catch
        {
            context.HttpContext.Items[SessionAuthentication.AvailabilityItem] = true;
            context.RejectPrincipal();
            return;
        }

        if (current is null || !string.Equals(current.SecurityStamp, ticketStamp, StringComparison.Ordinal))
        {
            context.RejectPrincipal();
        }
    }
}
