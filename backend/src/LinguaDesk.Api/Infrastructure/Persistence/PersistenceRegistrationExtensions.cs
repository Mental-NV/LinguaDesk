using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LinguaDesk.Api.Infrastructure.Persistence;

public static class PersistenceRegistrationExtensions
{
    public static IServiceCollection AddLinguaDeskPersistence(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        services.AddOptions<StorageOptions>().BindConfiguration(StorageOptions.SectionName);
        services.AddSingleton(serviceProvider =>
        {
            var finalEnvironment = serviceProvider.GetRequiredService<IHostEnvironment>();
            return StoragePathPolicy.Resolve(
                serviceProvider.GetRequiredService<IOptions<StorageOptions>>().Value.DatabasePath,
                finalEnvironment.ContentRootPath);
        });
        services.AddSingleton(serviceProvider =>
            new StorageConnectionPolicy(serviceProvider.GetRequiredService<StorageDatabaseTarget>()));
        services.AddDbContext<LinguaDeskDbContext>((serviceProvider, options) =>
        {
            var policy = serviceProvider.GetRequiredService<StorageConnectionPolicy>();
            options.UseSqlite(
                policy.CreateRuntimeConnectionString(),
                sqlite => sqlite.MigrationsAssembly(typeof(LinguaDeskDbContext).Assembly.GetName().Name));
        });

        return services;
    }
}
