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
        services.AddScoped<OperationAdmissionService>();
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
