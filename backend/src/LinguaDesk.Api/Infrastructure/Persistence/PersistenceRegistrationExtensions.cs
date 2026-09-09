using LinguaDesk.Api.Features.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace LinguaDesk.Api.Infrastructure.Persistence;

public static class PersistenceRegistrationExtensions
{
    public static IServiceCollection AddLinguaDeskPersistence(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment,
        bool isContractGeneration = false)
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
        var identity = services
            .AddIdentityCore<IdentityUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.User.AllowedUserNameCharacters = null!;
                options.Password.RequiredLength = 1;
                options.Password.RequiredUniqueChars = 0;
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
            })
            .AddEntityFrameworkStores<LinguaDeskDbContext>();
        if (!isContractGeneration)
        {
            identity.AddDefaultTokenProviders();
        }
        services.RemoveAll<IPasswordValidator<IdentityUser>>();
        services.AddSingleton<IPasswordValidator<IdentityUser>, LinguaDeskPasswordValidator>();

        return services;
    }
}
