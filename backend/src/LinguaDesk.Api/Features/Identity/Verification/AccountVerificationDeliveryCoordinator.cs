using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using LinguaDesk.Api.Features.Identity.Registration;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LinguaDesk.Api.Features.Identity.Verification;

public sealed class AccountVerificationDeliveryCoordinator(
    UserManager<IdentityUser> users,
    IAccountConfirmationSender sender,
    RegistrationGate gate,
    TimeProvider timeProvider,
    ILogger<AccountVerificationDeliveryCoordinator> logger)
{
    public const int RetryAfterSeconds = 60;
    public const string CooldownTokenProvider = "LinguaDesk";
    public const string CooldownTokenName = "VerificationDeliveryAttemptUtc";
    private static readonly TimeSpan Cooldown = TimeSpan.FromSeconds(RetryAfterSeconds);

    private static readonly Action<ILogger, string, Exception?> LogDeliveryUnavailable =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(6010, "AccountVerificationDeliveryUnavailable"),
            "Account verification delivery is unavailable. Category=deliveryUnavailable CorrelationId={CorrelationId}");

    public Task RequestInitialDeliveryWhileGateHeldAsync(
        IdentityUser user,
        string destination,
        string correlationId,
        CancellationToken cancellationToken) =>
        TryDeliverWhileGateHeldAsync(user, destination, correlationId, cancellationToken);

    public async Task<ResendVerificationOutcome> RequestResendAsync(
        string email,
        string correlationId,
        CancellationToken cancellationToken)
    {
        await gate.EnterAsync(cancellationToken);
        try
        {
            var user = await users.FindByEmailAsync(email);
            if (user is null || await users.IsEmailConfirmedAsync(user))
            {
                return ResendVerificationOutcome.Accepted;
            }

            await TryDeliverWhileGateHeldAsync(user, email, correlationId, cancellationToken);
            return ResendVerificationOutcome.Accepted;
        }
        catch (Exception exception) when (exception is DbUpdateException or SqliteException or InvalidOperationException)
        {
            return ResendVerificationOutcome.Unavailable;
        }
        finally
        {
            gate.Exit();
        }
    }

    private async Task TryDeliverWhileGateHeldAsync(
        IdentityUser user,
        string destination,
        string correlationId,
        CancellationToken cancellationToken)
    {
        try
        {
            var now = timeProvider.GetUtcNow();
            var marker = await users.GetAuthenticationTokenAsync(user, CooldownTokenProvider, CooldownTokenName);
            if (DateTimeOffset.TryParseExact(
                    marker,
                    "O",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out var previous)
                && (previous > now || now - previous < Cooldown))
            {
                return;
            }

            var markerResult = await users.SetAuthenticationTokenAsync(
                user,
                CooldownTokenProvider,
                CooldownTokenName,
                now.ToString("O", CultureInfo.InvariantCulture));
            if (!markerResult.Succeeded)
            {
                LogDeliveryUnavailable(logger, correlationId, null);
                return;
            }

            var token = await users.GenerateEmailConfirmationTokenAsync(user);
            var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            await sender.SendAsync(new AccountConfirmationDelivery(destination, user.Id, code), cancellationToken);
        }
        catch (Exception exception) when (exception is AccountConfirmationDeliveryException
            or IOException
            or UnauthorizedAccessException
            or CryptographicException
            or DbUpdateException
            or SqliteException
            or InvalidOperationException)
        {
            LogDeliveryUnavailable(logger, correlationId, null);
        }
    }
}

public enum ResendVerificationOutcome
{
    Accepted,
    Unavailable,
}
