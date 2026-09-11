namespace LinguaDesk.Api.Features.Operations;

public sealed partial class OperationRecoveryHostedService(
    IServiceProvider services,
    ILogger<OperationRecoveryHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = services.CreateScope();
            var recovery = scope.ServiceProvider.GetRequiredService<OperationRecoveryService>();
            var interrupted = await recovery.ReconcileOrphansAsync(stoppingToken);
            Log.ScanComplete(logger, interrupted);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception)
        {
            Log.ScanUnavailable(logger);
        }
    }

    private static partial class Log
    {
        [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Operation recovery scan complete: {InterruptedCount} pending operations finalized as interrupted.")]
        public static partial void ScanComplete(ILogger logger, int interruptedCount);

        [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Operation recovery scan unavailable at startup; pending operations remain pending.")]
        public static partial void ScanUnavailable(ILogger logger);
    }
}
