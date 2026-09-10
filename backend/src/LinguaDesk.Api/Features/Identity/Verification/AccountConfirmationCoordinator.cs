using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LinguaDesk.Api.Features.Identity.Verification;

public sealed class AccountConfirmationCoordinator(UserManager<IdentityUser> users)
{
    public async Task<AccountConfirmationOutcome> ConfirmAsync(string userId, string token)
    {
        try
        {
            var user = await users.FindByIdAsync(userId);
            if (user is null)
            {
                return AccountConfirmationOutcome.InvalidOrExpired;
            }

            var result = await users.ConfirmEmailAsync(user, token);
            return result.Succeeded
                ? AccountConfirmationOutcome.Verified
                : AccountConfirmationOutcome.InvalidOrExpired;
        }
        catch (Exception exception) when (exception is DbUpdateException
            or SqliteException
            or InvalidOperationException
            or IOException
            or UnauthorizedAccessException
            or CryptographicException)
        {
            return AccountConfirmationOutcome.Unavailable;
        }
    }
}

public enum AccountConfirmationOutcome
{
    Verified,
    InvalidOrExpired,
    Unavailable,
}
