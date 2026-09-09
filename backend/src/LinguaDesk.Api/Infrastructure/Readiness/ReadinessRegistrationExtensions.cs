namespace LinguaDesk.Api.Infrastructure.Readiness;

public static class ReadinessRegistrationExtensions
{
    public static IServiceCollection AddLinguaDeskReadiness(
        this IServiceCollection services,
        IHostEnvironment environment,
        bool isContractGeneration)
    {
        services.AddSingleton<ServingReadiness>();
        services.AddHealthChecks().AddCheck<ServingReadiness>("serving", tags: ["ready"]);
        if (!isContractGeneration && !environment.IsEnvironment("Testing"))
        {
            services.AddHostedService<ServingReadinessStartupValidator>();
        }

        return services;
    }
}
