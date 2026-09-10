using Microsoft.AspNetCore.Identity;

namespace LinguaDesk.Api.Infrastructure.Smoke;

internal static class SmokeAccountSeeder
{
    private const string SeedFlag = "LINGUADESK_SMOKE_SEED_ACCOUNTS";
    private const string VerifiedEmailVariable = "LINGUADESK_SMOKE_VERIFIED_EMAIL";
    private const string UnverifiedEmailVariable = "LINGUADESK_SMOKE_UNVERIFIED_EMAIL";
    private const string PasswordVariable = "LINGUADESK_SMOKE_ACCOUNT_PASSWORD";

    public static bool IsRequested() =>
        string.Equals(Environment.GetEnvironmentVariable(SeedFlag), "1", StringComparison.Ordinal);

    public static async Task SeedAsync(WebApplication app)
    {
        if (!app.Environment.IsEnvironment("Smoke"))
        {
            throw new InvalidOperationException("Smoke account seeding is available only in the Smoke environment.");
        }

        var verifiedEmail = RequiredValue(VerifiedEmailVariable);
        var unverifiedEmail = RequiredValue(UnverifiedEmailVariable);
        var password = RequiredValue(PasswordVariable);

        await using var scope = app.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        await CreateAsync(users, verifiedEmail, password, emailConfirmed: true);
        await CreateAsync(users, unverifiedEmail, password, emailConfirmed: false);
    }

    private static string RequiredValue(string name) =>
        Environment.GetEnvironmentVariable(name) is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException("Required smoke seed configuration is missing.");

    private static async Task CreateAsync(
        UserManager<IdentityUser> users,
        string email,
        string password,
        bool emailConfirmed)
    {
        var account = new IdentityUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = emailConfirmed,
        };
        var result = await users.CreateAsync(account, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException("A synthetic smoke account could not be created.");
        }
    }
}
