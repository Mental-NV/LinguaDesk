using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LinguaDesk.Api.Infrastructure.Persistence;

public sealed class LinguaDeskDesignTimeDbContextFactory : IDesignTimeDbContextFactory<LinguaDeskDbContext>
{
    public LinguaDeskDbContext CreateDbContext(string[] args)
    {
        var databasePath = ParseDatabasePath(args);
        var currentDirectory = Directory.GetCurrentDirectory();
        var repositoryRoot = StoragePathPolicy.FindRepositoryRoot(currentDirectory);
        var contentRoot = repositoryRoot is null
            ? currentDirectory
            : Path.Combine(repositoryRoot, "backend", "src", "LinguaDesk.Api");
        var target = StoragePathPolicy.Resolve(databasePath, contentRoot);
        var policy = new StorageConnectionPolicy(target);
        var options = new DbContextOptionsBuilder<LinguaDeskDbContext>()
            .UseSqlite(
                policy.CreateMigrationConnectionString(),
                sqlite => sqlite.MigrationsAssembly(typeof(LinguaDeskDbContext).Assembly.GetName().Name))
            .Options;

        return new LinguaDeskDbContext(options);
    }

    private static string ParseDatabasePath(string[] args)
    {
        if (args.Length != 2
            || !args[0].Equals("--database-path", StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(args[1]))
        {
            throw new InvalidOperationException(
                "Design-time storage commands require exactly: --database-path <absolute-path>.");
        }

        return args[1];
    }
}
