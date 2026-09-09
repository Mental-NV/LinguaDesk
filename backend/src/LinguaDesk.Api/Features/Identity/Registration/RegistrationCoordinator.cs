using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LinguaDesk.Api.Features.Identity.Registration;

public sealed class RegistrationCoordinator(
    UserManager<IdentityUser> users,
    IAccountConfirmationSender sender,
    RegistrationGate gate,
    ILogger<RegistrationCoordinator> logger)
{
    private static readonly Action<ILogger, string, Exception?> LogDeliveryUnavailable =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(6001, "AccountConfirmationDeliveryUnavailable"),
            "Account confirmation delivery failed. Category=deliveryUnavailable CorrelationId={CorrelationId}");

    private static readonly Action<ILogger, string, Exception?> LogStorageUnavailable =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(6002, "RegistrationStorageUnavailable"),
            "Registration storage is unavailable. Category=availability CorrelationId={CorrelationId}");

    public async Task<RegistrationOutcome> RegisterAsync(
        RegistrationRequest request,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = users.NormalizeEmail(request.Email);
        await gate.EnterAsync(cancellationToken);
        try
        {
            if (await users.FindByEmailAsync(request.Email) is not null)
            {
                return RegistrationOutcome.Accepted;
            }

            var user = new IdentityUser { UserName = request.Email, Email = request.Email, EmailConfirmed = false };
            var result = await users.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                if (result.Errors.All(error => IsDuplicate(error.Code))
                    || await NormalizedEmailExistsAsync(normalizedEmail))
                {
                    return RegistrationOutcome.Accepted;
                }

                return RegistrationOutcome.InvalidPassword;
            }

            try
            {
                var material = await users.GenerateEmailConfirmationTokenAsync(user);
                await sender.SendAsync(request.Email, material, cancellationToken);
            }
            catch (Exception exception) when (exception is AccountConfirmationDeliveryException
                or IOException
                or UnauthorizedAccessException
                or CryptographicException)
            {
                LogDeliveryUnavailable(logger, correlationId, null);
            }

            return RegistrationOutcome.Accepted;
        }
        catch (Exception exception) when (exception is DbUpdateException or SqliteException or InvalidOperationException)
        {
            if (await NormalizedEmailExistsAsync(normalizedEmail))
            {
                return RegistrationOutcome.Accepted;
            }

            LogStorageUnavailable(logger, correlationId, null);
            return RegistrationOutcome.Unavailable;
        }
        finally
        {
            gate.Exit();
        }
    }

    private async Task<bool> NormalizedEmailExistsAsync(string? normalizedEmail)
    {
        if (normalizedEmail is null)
        {
            return false;
        }

        try
        {
            return await users.Users.AnyAsync(user => user.NormalizedEmail == normalizedEmail);
        }
        catch (Exception exception) when (exception is DbUpdateException or SqliteException or InvalidOperationException)
        {
            return false;
        }
    }

    private static bool IsDuplicate(string code) =>
        code is "DuplicateEmail" or "DuplicateUserName";
}

public sealed class RegistrationGate : IDisposable
{
    private readonly SemaphoreSlim semaphore = new(1, 1);

    public Task EnterAsync(CancellationToken cancellationToken) => semaphore.WaitAsync(cancellationToken);

    public void Exit() => semaphore.Release();

    public void Dispose() => semaphore.Dispose();
}

public enum RegistrationOutcome
{
    Accepted,
    InvalidPassword,
    Unavailable,
}
