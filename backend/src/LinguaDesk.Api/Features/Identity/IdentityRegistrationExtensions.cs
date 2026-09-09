using LinguaDesk.Api.Features.Identity.Registration;

namespace LinguaDesk.Api.Features.Identity;

public static class IdentityRegistrationExtensions
{
    public static IServiceCollection AddLinguaDeskAccounts(this IServiceCollection services)
    {
        services.AddScoped<RegistrationCoordinator>();
        services.AddSingleton<RegistrationGate>();
        services.AddSingleton<IAccountConfirmationSender, UnavailableAccountConfirmationSender>();
        services.AddVerifiedAccountAuthorization();
        return services;
    }
}
