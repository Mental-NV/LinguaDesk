using LinguaDesk.Api.Features.Identity.Registration;
using LinguaDesk.Api.Features.Identity.Verification;
using Microsoft.AspNetCore.Identity;

namespace LinguaDesk.Api.Features.Identity;

public static class IdentityRegistrationExtensions
{
    public static IServiceCollection AddLinguaDeskAccounts(this IServiceCollection services)
    {
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
