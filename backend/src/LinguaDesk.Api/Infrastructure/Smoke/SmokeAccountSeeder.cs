using LinguaDesk.Api.Features.Operations;
using LinguaDesk.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

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
        var verified = await CreateAsync(users, verifiedEmail, password, emailConfirmed: true);
        await CreateAsync(users, unverifiedEmail, password, emailConfirmed: false);
        await SeedUsageBaselineAsync(scope.ServiceProvider, verified.Id);
    }

    private static string RequiredValue(string name) =>
        Environment.GetEnvironmentVariable(name) is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException("Required smoke seed configuration is missing.");

    private static async Task<IdentityUser> CreateAsync(
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

        return account;
    }

    /// <summary>
    /// Seeds the documented M028 published-suite baseline for the verified Smoke
    /// account: 7,500 consumed characters on the current UTC day (#6 section 4.1),
    /// composed of three settled translation submissions of 2,500 scalars each
    /// (every seed source respects the 5,000-character whole-input limit).
    /// Seed rows carry no source or result text: submissions persist counts and
    /// fingerprints only. Published cases exclude these rows from observed
    /// operation counts and read the live usage snapshot for deltas.
    /// </summary>
    private static async Task SeedUsageBaselineAsync(IServiceProvider services, string verifiedUserId)
    {
        const int seedOperationCount = 3;
        const int seedOperationScalars = 2500;
        var database = services.GetRequiredService<LinguaDeskDbContext>();
        var timeProvider = services.GetRequiredService<TimeProvider>();
        var observed = timeProvider.GetUtcNow();
        var day = observed.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

        for (var index = 0; index < seedOperationCount; index++)
        {
            database.OperationSubmissions.Add(new OperationSubmission
            {
                AccountId = verifiedUserId,
                OperationId = Guid.NewGuid().ToString("D"),
                Family = "translation",
                Fingerprint = $"smoke-seed-m028-{index}",
                ScalarCount = seedOperationScalars,
                AdmissionDay = day,
                DeadlineUtc = observed.AddSeconds(30),
                State = OperationStates.Succeeded,
                CreatedUtc = observed,
                SettledUtc = observed,
            });
        }

        var consumed = seedOperationCount * seedOperationScalars;
        database.CharacterLedgerEntries.Add(new CharacterLedgerEntry
        {
            Scope = CharacterLedgerScopes.User,
            AccountId = verifiedUserId,
            Day = day,
            ConsumedCharacters = consumed,
        });
        database.CharacterLedgerEntries.Add(new CharacterLedgerEntry
        {
            Scope = CharacterLedgerScopes.Global,
            AccountId = CharacterLedgerScopes.GlobalAccountId,
            Day = day,
            ConsumedCharacters = consumed,
        });

        var revision = await database.LedgerRevisions
            .SingleOrDefaultAsync(entry => entry.Id == LedgerRevision.SingletonId);
        if (revision is null)
        {
            database.LedgerRevisions.Add(new LedgerRevision { Id = LedgerRevision.SingletonId, Value = 1 });
        }

        await database.SaveChangesAsync();
    }
}
