using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.Options;

namespace LinguaDesk.Api.Infrastructure.Security;

public static class SecurityRegistrationExtensions
{
    public static IServiceCollection AddLinguaDeskSecurity(
        this IServiceCollection services,
        IConfiguration configuration,
        bool isContractGeneration)
    {
        services.AddOptions<SecurityOptions>().BindConfiguration(SecurityOptions.SectionName);
        services.AddSingleton(serviceProvider => DataProtectionKeyPathPolicy.Resolve(
            serviceProvider.GetRequiredService<IOptions<SecurityOptions>>().Value.DataProtectionKeysPath,
            serviceProvider.GetRequiredService<IHostEnvironment>().ContentRootPath));
        if (isContractGeneration)
        {
            return services;
        }

        services.AddDataProtection()
            .SetApplicationName("LinguaDesk");
        services.AddSingleton<IConfigureOptions<KeyManagementOptions>, DataProtectionKeyOptionsSetup>();
        return services;
    }
}

internal sealed class DataProtectionKeyOptionsSetup(
    DataProtectionKeyDirectory directory,
    ILoggerFactory loggerFactory) : IConfigureOptions<KeyManagementOptions>
{
    public void Configure(KeyManagementOptions options)
    {
        if (!Directory.Exists(directory.Path))
        {
            throw new InvalidOperationException("The configured Data Protection key directory does not exist.");
        }

        options.XmlRepository = new FileSystemXmlRepository(
            new DirectoryInfo(directory.Path),
            loggerFactory);
    }
}
