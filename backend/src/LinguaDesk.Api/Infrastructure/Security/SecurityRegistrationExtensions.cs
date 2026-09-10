using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.AspNetCore.HttpOverrides;
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
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.ForwardLimit = 1;
            options.RequireHeaderSymmetry = true;

            var configuredNetworks = configuration
                .GetSection($"{SecurityOptions.SectionName}:ForwardedHeaderTrustedNetworks")
                .Get<string[]>() ?? [];
            if (configuredNetworks.Length == 0)
            {
                return;
            }

            options.KnownIPNetworks.Clear();
            foreach (var configuredNetwork in configuredNetworks)
            {
                if (!System.Net.IPNetwork.TryParse(configuredNetwork, out var network))
                {
                    throw new InvalidOperationException("A trusted forwarded-header network is invalid.");
                }
                options.KnownIPNetworks.Add(network);
            }
        });
        services.AddSingleton(serviceProvider => DataProtectionKeyPathPolicy.Resolve(
            serviceProvider.GetRequiredService<IOptions<SecurityOptions>>().Value.DataProtectionKeysPath,
            serviceProvider.GetRequiredService<IHostEnvironment>().ContentRootPath));
        if (isContractGeneration)
        {
            services.AddDataProtection().UseEphemeralDataProtectionProvider();
            foreach (var hostedService in services
                .Where(static descriptor => descriptor.ServiceType == typeof(IHostedService)
                    && string.Equals(
                        descriptor.ImplementationType?.FullName,
                        "Microsoft.AspNetCore.DataProtection.Internal.DataProtectionHostedService",
                        StringComparison.Ordinal))
                .ToArray())
            {
                services.Remove(hostedService);
            }
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
