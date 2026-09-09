using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace LinguaDesk.Api.Features.Identity;

public static class VerifiedAccountAuthorization
{
    public const string PolicyName = "VerifiedAccount";

    public static IServiceCollection AddVerifiedAccountAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options => options.AddPolicy(
            PolicyName,
            policy => policy.AddRequirements(new VerifiedAccountRequirement())));
        services.AddScoped<IAuthorizationHandler, VerifiedAccountHandler>();
        return services;
    }
}

public sealed class VerifiedAccountRequirement : IAuthorizationRequirement;

public sealed class VerifiedAccountHandler(UserManager<IdentityUser> users)
    : AuthorizationHandler<VerifiedAccountRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        VerifiedAccountRequirement requirement)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return;
        }

        var current = await users.FindByIdAsync(userId);
        if (current?.EmailConfirmed == true)
        {
            context.Succeed(requirement);
        }
    }
}
