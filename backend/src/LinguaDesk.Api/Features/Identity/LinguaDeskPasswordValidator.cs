using System.Buffers;
using System.Text;
using Microsoft.AspNetCore.Identity;

namespace LinguaDesk.Api.Features.Identity;

public sealed class LinguaDeskPasswordValidator : IPasswordValidator<IdentityUser>
{
    public const int MinimumScalarLength = 15;
    public const int MaximumScalarLength = 128;

    public Task<IdentityResult> ValidateAsync(
        UserManager<IdentityUser> manager,
        IdentityUser user,
        string? password)
    {
        if (!TryCountScalars(password, out var count)
            || count < MinimumScalarLength
            || count > MaximumScalarLength)
        {
            return Task.FromResult(IdentityResult.Failed(new IdentityError
            {
                Code = "PasswordPolicy",
                Description = "Password must contain 15 to 128 well-formed Unicode characters.",
            }));
        }

        return Task.FromResult(IdentityResult.Success);
    }

    public static bool TryCountScalars(string? value, out int count)
    {
        count = 0;
        if (value is null)
        {
            return false;
        }

        var span = value.AsSpan();
        while (!span.IsEmpty)
        {
            var status = Rune.DecodeFromUtf16(span, out _, out var consumed);
            if (status != OperationStatus.Done)
            {
                return false;
            }

            count++;
            span = span[consumed..];
        }

        return true;
    }
}
