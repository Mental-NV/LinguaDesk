using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace LinguaDesk.Api.Features.Identity.Bearer;

internal static class BearerAuthentication
{
    public const string AvailabilityItem = "LinguaDesk.AccountBearerAvailabilityFailure";
    public static readonly TimeSpan AccessLifetime = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan RefreshLifetime = TimeSpan.FromDays(7);
    public const long AccessLifetimeSeconds = 900;
    public const string TokenType = "Bearer";
}

internal sealed class BearerStampValidationEvents(
    IOptionsMonitor<BearerTokenOptions> bearerOptions,
    IServiceScopeFactory scopeFactory) : BearerTokenEvents
{
    public override async Task MessageReceivedAsync(MessageReceivedContext context)
    {
        var token = context.Token;
        if (string.IsNullOrEmpty(token))
        {
            var authorization = context.HttpContext.Request.Headers.Authorization.ToString();
            token = authorization.StartsWith("Bearer ", StringComparison.Ordinal)
                ? authorization["Bearer ".Length..]
                : null;
        }

        if (string.IsNullOrEmpty(token))
        {
            return;
        }

        AuthenticationTicket? ticket;
        try
        {
            ticket = bearerOptions.Get(IdentityConstants.BearerScheme).BearerTokenProtector.Unprotect(token);
        }
        catch
        {
            context.HttpContext.Items[BearerAuthentication.AvailabilityItem] = true;
            context.Fail("Bearer authentication is temporarily unavailable.");
            return;
        }

        if (ticket?.Principal is null)
        {
            return;
        }

        IdentityUser? current;
        try
        {
            using var scope = scopeFactory.CreateScope();
            var signInManager = scope.ServiceProvider.GetRequiredService<SignInManager<IdentityUser>>();
            current = await signInManager.ValidateSecurityStampAsync(ticket.Principal);
        }
        catch
        {
            context.HttpContext.Items[BearerAuthentication.AvailabilityItem] = true;
            context.Fail("Bearer authentication is temporarily unavailable.");
            return;
        }

        if (current is null)
        {
            context.Fail("Bearer credential is no longer valid.");
            return;
        }

        var ticketUserId = ticket.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.Equals(ticketUserId, current.Id, StringComparison.Ordinal))
        {
            context.Fail("Bearer credential is no longer valid.");
        }
    }
}
