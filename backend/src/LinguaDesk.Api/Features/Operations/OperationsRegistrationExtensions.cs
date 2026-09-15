using LinguaDesk.Api.Infrastructure.Serving;
using LinguaDesk.Api.Infrastructure.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;

namespace LinguaDesk.Api.Features.Operations;

public static class OperationsRegistrationExtensions
{
    public static IServiceCollection AddLinguaDeskOperations(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<OperationAdmissionOptions>().BindConfiguration(OperationAdmissionOptions.SectionName);
        services.AddSingleton<IValidateOptions<OperationAdmissionOptions>, OperationAdmissionOptionsValidator>();
        services.AddOptions<MonetaryAdmissionOptions>().BindConfiguration(MonetaryAdmissionOptions.SectionName);
        services.AddSingleton<IValidateOptions<MonetaryAdmissionOptions>, MonetaryAdmissionOptionsValidator>();
        // Note: no IValidateOptions<ServingOptions> registration. The serving
        // section is intentionally fail-closed by default, and options
        // validation on read would turn the pending path into a 500. Section
        // validity gates provider registration (below) and startup
        // (ServingStartupGuard); the per-operation ceiling degrades to the
        // owner-locked default when unconfigured.
        services.AddOptions<ServingOptions>().BindConfiguration(ServingOptions.SectionName);
        services.AddScoped<OperationAdmissionService>();
        RegisterServingOrUnavailableProviders(services, configuration);
        services.AddScoped<TranslationOperationCoordinator>();
        services.AddScoped<RewritingOperationCoordinator>();
        services.AddScoped<OperationSettlementService>();
        services.AddScoped<OperationRecoveryService>();
        services.AddHostedService<OperationRecoveryHostedService>();
        services.AddScoped<MonetaryAdmissionService>();
        services.AddSingleton<OperationFingerprintKeyProvider>(serviceProvider =>
            new OperationFingerprintKeyProvider(
                serviceProvider.GetRequiredService<IDataProtectionProvider>(),
                serviceProvider.GetRequiredService<DataProtectionKeyDirectory>()));
        return services;
    }

    /// <summary>
    /// Selects the primary-only serving providers only when the serving
    /// section validates; otherwise the fail-closed <c>Unavailable*</c>
    /// providers stay so submissions remain pending reservations.
    /// </summary>
    private static void RegisterServingOrUnavailableProviders(IServiceCollection services, IConfiguration configuration)
    {
        var serving = configuration.GetSection(ServingOptions.SectionName).Get<ServingOptions>();
        if (ServingOptionsValidator.IsSectionValid(serving ?? new ServingOptions()))
        {
            services.AddScoped<ITranslationClientProvider, ServingTranslationClientProvider>();
            services.AddScoped<IRewritingClientProvider, ServingRewritingClientProvider>();
            return;
        }

        services.AddScoped<ITranslationClientProvider, UnavailableTranslationClientProvider>();
        services.AddScoped<IRewritingClientProvider, UnavailableRewritingClientProvider>();
    }
}

internal sealed class OperationAdmissionOptionsValidator : IValidateOptions<OperationAdmissionOptions>
{
    public ValidateOptionsResult Validate(string? name, OperationAdmissionOptions options) =>
        options.UserDailyAllowanceCharacters > 0 && options.GlobalDailyAllowanceCharacters > 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail("Operation allowance configuration must use positive character limits.");
}

internal sealed class MonetaryAdmissionOptionsValidator : IValidateOptions<MonetaryAdmissionOptions>
{
    public ValidateOptionsResult Validate(string? name, MonetaryAdmissionOptions options) =>
        options.MonthlyCapMinorUnits > 0 && !string.IsNullOrWhiteSpace(options.Currency)
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail("Monetary admission configuration must set a positive monthly cap and a non-empty currency.");
}
