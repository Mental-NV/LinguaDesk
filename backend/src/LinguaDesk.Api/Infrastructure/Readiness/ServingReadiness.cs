using System.Data.Common;
using LinguaDesk.Api.Infrastructure.Persistence;
using LinguaDesk.Api.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LinguaDesk.Api.Infrastructure.Readiness;

public sealed class ServingReadiness(
    IServiceScopeFactory scopeFactory,
    StorageDatabaseTarget databaseTarget,
    DataProtectionKeyDirectory keyDirectory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(databaseTarget.DatabasePath))
            {
                return HealthCheckResult.Unhealthy();
            }

            await using var scope = scopeFactory.CreateAsyncScope();
            var database = scope.ServiceProvider.GetRequiredService<LinguaDeskDbContext>();
            if ((await database.Database.GetPendingMigrationsAsync(cancellationToken)).Any())
            {
                return HealthCheckResult.Unhealthy();
            }

            await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
            await database.Database.ExecuteSqlRawAsync(
                "CREATE TABLE __linguadesk_readiness_probe (id INTEGER);",
                cancellationToken);
            await transaction.RollbackAsync(cancellationToken);

            if (!Directory.Exists(keyDirectory.Path))
            {
                return HealthCheckResult.Unhealthy();
            }

            var probePath = Path.Combine(keyDirectory.Path, $".readiness-{Guid.NewGuid():N}");
            await using (var probe = new FileStream(
                probePath,
                FileMode.CreateNew,
                FileAccess.ReadWrite,
                FileShare.None,
                1,
                FileOptions.Asynchronous | FileOptions.DeleteOnClose))
            {
                await probe.WriteAsync(new byte[] { 1 }, cancellationToken);
                probe.Position = 0;
                if (probe.ReadByte() != 1)
                {
                    return HealthCheckResult.Unhealthy();
                }
            }

            return HealthCheckResult.Healthy();
        }
        catch (Exception exception) when (exception is IOException
            or UnauthorizedAccessException
            or InvalidOperationException
            or DbException
            or DbUpdateException)
        {
            return HealthCheckResult.Unhealthy();
        }
    }
}

public sealed class ServingReadinessStartupValidator(ServingReadiness readiness) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var result = await readiness.CheckHealthAsync(new HealthCheckContext(), cancellationToken);
        if (result.Status != HealthStatus.Healthy)
        {
            throw new InvalidOperationException("Account-serving dependencies are not ready.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
