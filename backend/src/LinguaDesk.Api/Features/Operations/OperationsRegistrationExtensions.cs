using LinguaDesk.Api.Infrastructure.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;

namespace LinguaDesk.Api.Features.Operations;

public static class OperationsRegistrationExtensions
{
    public static IServiceCollection AddLinguaDeskOperations(this IServiceCollection services)
    {
        services.AddOptions<OperationAdmissionOptions>().BindConfiguration(OperationAdmissionOptions.SectionName);
        services.AddSingleton<IValidateOptions<OperationAdmissionOptions>, OperationAdmissionOptionsValidator>();
        services.AddOptions<MonetaryAdmissionOptions>().BindConfiguration(MonetaryAdmissionOptions.SectionName);
        services.AddSingleton<IValidateOptions<MonetaryAdmissionOptions>, MonetaryAdmissionOptionsValidator>();
        services.AddScoped<OperationAdmissionService>();
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
