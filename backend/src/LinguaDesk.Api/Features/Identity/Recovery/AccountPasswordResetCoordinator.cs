using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using LinguaDesk.Api.Features.Identity.Registration;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LinguaDesk.Api.Features.Identity.Recovery;

public sealed class AccountPasswordResetCoordinator(
    UserManager<IdentityUser> users,
    IAccountPasswordResetSender sender,
    RegistrationGate gate,
    TimeProvider timeProvider,
    ILogger<AccountPasswordResetCoordinator> logger)
{
    public const int RetryAfterSeconds = 60;
    public const string CooldownTokenProvider = "LinguaDesk";
    public const string CooldownTokenName = "PasswordResetDeliveryAttemptUtc";
    private static readonly TimeSpan Cooldown = TimeSpan.FromSeconds(RetryAfterSeconds);

    private static readonly Action<ILogger, string, Exception?> LogDeliveryUnavailable =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(6020, "AccountPasswordResetDeliveryUnavailable"),
            "Account password-reset delivery is unavailable. Category=deliveryUnavailable CorrelationId={CorrelationId}");

    public async Task<ForgotPasswordOutcome> RequestResetAsync(
        string email,
        string correlationId,
        CancellationToken cancellationToken)
    {
        await gate.EnterAsync(cancellationToken);
        try
        {
            var user = await users.FindByEmailAsync(email);
            if (user is null)
            {
                return ForgotPasswordOutcome.Accepted;
            }

            await TryDeliverWhileGateHeldAsync(user, email, correlationId, cancellationToken);
            return ForgotPasswordOutcome.Accepted;
        }
        catch (Exception exception) when (exception is DbUpdateException or SqliteException or InvalidOperationException)
        {
            return ForgotPasswordOutcome.Unavailable;
        }
        finally
        {
            gate.Exit();
        }
    }

    public async Task<ResetPasswordOutcome> ResetAsync(string userId, string token, string newPassword)
    {
        try
        {
            var user = await users.FindByIdAsync(userId);
            if (user is null)
            {
                return ResetPasswordOutcome.InvalidOrExpired;
            }

            var result = await users.ResetPasswordAsync(user, token, newPassword);
            return result.Succeeded
                ? ResetPasswordOutcome.PasswordReset
                : ResetPasswordOutcome.InvalidOrExpired;
        }
        catch (Exception exception) when (exception is DbUpdateException
            or SqliteException
            or InvalidOperationException
            or IOException
            or UnauthorizedAccessException
            or CryptographicException)
        {
            return ResetPasswordOutcome.Unavailable;
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

            var token = await users.GeneratePasswordResetTokenAsync(user);
            var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            await sender.SendAsync(new AccountPasswordResetDelivery(destination, user.Id, code), cancellationToken);
        }
        catch (Exception exception) when (exception is AccountPasswordResetDeliveryException
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

public enum ForgotPasswordOutcome
{
    Accepted,
    Unavailable,
}

public enum ResetPasswordOutcome
{
    PasswordReset,
    InvalidOrExpired,
    Unavailable,
}
