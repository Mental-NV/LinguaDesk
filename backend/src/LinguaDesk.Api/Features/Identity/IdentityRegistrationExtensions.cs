using LinguaDesk.Api.Features.Identity.Registration;
using LinguaDesk.Api.Features.Identity.Verification;
using LinguaDesk.Api.Features.Identity.Recovery;
using LinguaDesk.Api.Features.Identity.Bearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.BearerToken;
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
                        ? IdentityConstants.BearerScheme
                        : IdentityConstants.ApplicationScheme;
            })
            .AddBearerToken(IdentityConstants.BearerScheme, options =>
            {
                options.BearerTokenExpiration = BearerAuthentication.AccessLifetime;
                options.RefreshTokenExpiration = BearerAuthentication.RefreshLifetime;
                options.EventsType = typeof(BearerStampValidationEvents);
            })
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
        services.AddOptions<BearerTokenOptions>(IdentityConstants.BearerScheme)
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
        services.AddScoped<BearerStampValidationEvents>();
        services.AddScoped<RegistrationCoordinator>();
        services.AddScoped<AccountConfirmationCoordinator>();
        services.AddScoped<AccountVerificationDeliveryCoordinator>();
        services.AddScoped<AccountPasswordResetCoordinator>();
        services.AddSingleton<RegistrationGate>();
        services.AddSingleton<IAccountConfirmationSender, UnavailableAccountConfirmationSender>();
        services.AddSingleton<IAccountPasswordResetSender, UnavailableAccountPasswordResetSender>();
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
        services.Configure<DataProtectionTokenProviderOptions>(options =>
        {
            options.TokenLifespan = AccountPasswordResetPolicy.TokenLifespan;
        });
        services.AddVerifiedAccountAuthorization();
        return services;
    }
}
