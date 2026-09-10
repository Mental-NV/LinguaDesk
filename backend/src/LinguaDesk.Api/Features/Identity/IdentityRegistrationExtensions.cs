using LinguaDesk.Api.Features.Identity.Registration;
using LinguaDesk.Api.Features.Identity.Verification;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using LinguaDesk.Api.Features.Identity.Session;

namespace LinguaDesk.Api.Features.Identity;

public static class IdentityRegistrationExtensions
{
    public static IServiceCollection AddLinguaDeskAccounts(this IServiceCollection services)
    {
        services.AddAuthentication(options =>
            {
                options.DefaultScheme = SessionAuthentication.PolicyScheme;
                options.DefaultAuthenticateScheme = SessionAuthentication.PolicyScheme;
                options.DefaultChallengeScheme = SessionAuthentication.PolicyScheme;
            })
            .AddPolicyScheme(SessionAuthentication.PolicyScheme, null, options =>
            {
                options.ForwardDefaultSelector = context =>
                    context.Request.Headers.Authorization.Any(static value =>
                        value?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) == true)
                        ? SessionAuthentication.UnsupportedBearerScheme
                        : IdentityConstants.ApplicationScheme;
            })
            .AddScheme<AuthenticationSchemeOptions, UnsupportedBearerAuthenticationHandler>(
                SessionAuthentication.UnsupportedBearerScheme, null)
            .AddCookie(IdentityConstants.ApplicationScheme, options =>
            {
                options.Cookie.Name = SessionAuthentication.SessionCookieName;
                options.Cookie.Path = "/";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.ExpireTimeSpan = SessionAuthentication.CookieLifetime;
                options.SlidingExpiration = false;
                options.EventsType = typeof(CurrentAccountCookieEvents);
            });
        services.AddOptions<CookieAuthenticationOptions>(IdentityConstants.ApplicationScheme)
            .Configure<TimeProvider>((options, timeProvider) => options.TimeProvider = timeProvider);
        services.AddAntiforgery(options =>
        {
            options.Cookie.Name = SessionAuthentication.AntiforgeryCookieName;
            options.Cookie.Path = "/";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.HeaderName = SessionAuthentication.AntiforgeryHeaderName;
        });
        services.AddScoped<CurrentAccountCookieEvents>();
        services.AddScoped<RegistrationCoordinator>();
        services.AddScoped<AccountConfirmationCoordinator>();
        services.AddScoped<AccountVerificationDeliveryCoordinator>();
        services.AddSingleton<RegistrationGate>();
        services.AddSingleton<IAccountConfirmationSender, UnavailableAccountConfirmationSender>();
        services.AddTransient<LinguaDeskEmailConfirmationTokenProvider<IdentityUser>>();
        services.Configure<LinguaDeskEmailConfirmationTokenProviderOptions>(options =>
        {
            options.Name = LinguaDeskEmailConfirmationTokenPolicy.ProviderName;
            options.TokenLifespan = LinguaDeskEmailConfirmationTokenPolicy.TokenLifespan;
        });
        services.Configure<IdentityOptions>(options =>
        {
            options.Tokens.ProviderMap[LinguaDeskEmailConfirmationTokenPolicy.ProviderName] =
                new TokenProviderDescriptor(typeof(LinguaDeskEmailConfirmationTokenProvider<IdentityUser>));
            options.Tokens.EmailConfirmationTokenProvider = LinguaDeskEmailConfirmationTokenPolicy.ProviderName;
        });
        services.AddVerifiedAccountAuthorization();
        return services;
    }
}
