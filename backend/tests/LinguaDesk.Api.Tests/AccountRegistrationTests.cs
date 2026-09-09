using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using LinguaDesk.Api.Features.Identity;
using LinguaDesk.Api.Features.Identity.Registration;
using LinguaDesk.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LinguaDesk.Api.Tests;

[TestClass]
public sealed class AccountRegistrationTests
{
    private const string ValidPassword = "Maple!River2026";

    [TestMethod]
    public async Task UniqueRegistrationCreatesOneDurableUnconfirmedAccountAndDeliveryIntent()
    {
        await using var database = StorageTestDatabase.Create();
        await database.MigrateAsync();
        var keysPath = Directory.CreateDirectory(Path.Combine(database.RootPath, "keys")).FullName;
        var sender = new CapturingConfirmationSender();

        await using var factory = new AccountWebApplicationFactory(database.DatabasePath, keysPath, sender);
        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync(
            "/api/accounts/register",
            new { email = "new.account@example.test", password = ValidPassword });
        var body = await response.Content.ReadAsStringAsync();

        Assert.AreEqual(HttpStatusCode.Accepted, response.StatusCode);
        Assert.AreEqual("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.AreEqual("utf-8", response.Content.Headers.ContentType?.CharSet);
        Assert.AreEqual("no-store", response.Headers.CacheControl?.ToString());
        Assert.IsFalse(response.Headers.Contains("Set-Cookie"));
        Assert.IsFalse(response.Headers.Contains("WWW-Authenticate"));
        Assert.IsFalse(response.Headers.Contains("Location"));
        Assert.AreEqual("{\"status\":\"verificationRequired\"}", body);
        Assert.HasCount(1, sender.Deliveries);

        await using var verification = database.CreateContext();
        var account = await verification.Users.SingleAsync();
        Assert.IsFalse(account.EmailConfirmed);
        Assert.IsNotNull(account.PasswordHash);
        var hasher = new PasswordHasher<IdentityUser>();
        Assert.AreNotEqual(
            PasswordVerificationResult.Failed,
            hasher.VerifyHashedPassword(account, account.PasswordHash, ValidPassword));
        Assert.IsFalse(body.Contains("new.account@example.test", StringComparison.Ordinal));
        Assert.IsFalse(body.Contains(ValidPassword, StringComparison.Ordinal));
        Assert.IsFalse(body.Contains(account.Id, StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task StrictShapeMediaAndMalformedJsonFailWithoutEffects()
    {
        await using var database = StorageTestDatabase.Create();
        await database.MigrateAsync();
        var keysPath = Directory.CreateDirectory(Path.Combine(database.RootPath, "keys")).FullName;
        var sender = new CapturingConfirmationSender();
        await using var factory = new AccountWebApplicationFactory(database.DatabasePath, keysPath, sender);
        using var client = factory.CreateClient();

        var cases = new[]
        {
            ("{\"email\":\"shape@example.test\",\"password\":\"Maple!River2026\",\"role\":\"admin\"}", "application/json", HttpStatusCode.BadRequest),
            ("{\"email\":\"shape@example.test\",\"email\":\"other@example.test\",\"password\":\"Maple!River2026\"}", "application/json", HttpStatusCode.BadRequest),
            ("{\"email\":\"shape@example.test\",\"password\":", "application/json", HttpStatusCode.BadRequest),
            ("{\"email\":\"shape@example.test\",\"password\":\"Maple!River2026\"}", "text/plain", HttpStatusCode.UnsupportedMediaType),
            ("{\"email\":\"shape@example.test\",\"password\":\"Maple!River2026\"}", "application/json; charset=iso-8859-1", HttpStatusCode.UnsupportedMediaType),
        };

        foreach (var (json, mediaType, expectedStatus) in cases)
        {
            using var content = new StringContent(json, Encoding.UTF8);
            content.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(mediaType);
            using var response = await client.PostAsync("/api/accounts/register", content);
            var responseBody = await response.Content.ReadAsStringAsync();
            Assert.AreEqual(expectedStatus, response.StatusCode, mediaType);
            Assert.AreEqual("application/problem+json", response.Content.Headers.ContentType?.MediaType);
            Assert.AreEqual("no-store", response.Headers.CacheControl?.ToString());
            using var problem = JsonDocument.Parse(responseBody);
            Assert.AreEqual("invalidRequest", problem.RootElement.GetProperty("category").GetString());
            Assert.IsGreaterThan(0, problem.RootElement.GetProperty("correlationId").GetString()?.Length ?? 0);
            Assert.IsFalse(responseBody.Contains(ValidPassword, StringComparison.Ordinal));
            Assert.IsFalse(responseBody.Contains("shape@example.test", StringComparison.Ordinal));
        }

        using (var queryResponse = await client.PostAsJsonAsync(
            "/api/accounts/register?role=admin",
            new { email = "shape@example.test", password = ValidPassword }))
        {
            Assert.AreEqual(HttpStatusCode.BadRequest, queryResponse.StatusCode);
            Assert.AreEqual("no-store", queryResponse.Headers.CacheControl?.ToString());
        }

        var invalidUtf8 = Encoding.UTF8.GetBytes(
            "{\"email\":\"shape@example.test\",\"password\":\"Maple!River2026\"}");
        invalidUtf8[12] = 0xff;
        using (var invalidEncodingContent = new ByteArrayContent(invalidUtf8))
        {
            invalidEncodingContent.Headers.ContentType =
                System.Net.Http.Headers.MediaTypeHeaderValue.Parse("application/json; charset=utf-8");
            using var invalidEncodingResponse = await client.PostAsync(
                "/api/accounts/register",
                invalidEncodingContent);
            Assert.AreEqual(HttpStatusCode.BadRequest, invalidEncodingResponse.StatusCode);
            Assert.AreEqual("no-store", invalidEncodingResponse.Headers.CacheControl?.ToString());
        }

        await using var verification = database.CreateContext();
        Assert.AreEqual(0, await verification.Users.CountAsync());
        Assert.HasCount(0, sender.Deliveries);
    }

    [TestMethod]
    public async Task PasswordScalarBoundariesAndExactStringsFollowTheSelectedPolicy()
    {
        await using var database = StorageTestDatabase.Create();
        await database.MigrateAsync();
        var keysPath = Directory.CreateDirectory(Path.Combine(database.RootPath, "keys")).FullName;
        var sender = new CapturingConfirmationSender();
        await using var factory = new AccountWebApplicationFactory(database.DatabasePath, keysPath, sender);
        using var client = factory.CreateClient();

        var invalidPasswords = new[] { new string('a', 14), new string('a', 129) };
        foreach (var password in invalidPasswords)
        {
            using var response = await client.PostAsJsonAsync(
                "/api/accounts/register",
                new { email = $"invalid{password.Length}@example.test", password });
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        var validCases = new[]
        {
            (Email: "fifteen@example.test", Password: new string('a', 15)),
            (Email: "spaces@example.test", Password: new string(' ', 15)),
            (Email: "unicode@example.test", Password: string.Concat(Enumerable.Repeat("😀", 128))),
            (Email: "decomposed@example.test", Password: string.Concat(Enumerable.Repeat("e\u0301", 8))),
        };
        foreach (var item in validCases)
        {
            using var response = await client.PostAsJsonAsync("/api/accounts/register", new
            {
                email = item.Email,
                password = item.Password,
            });
            Assert.AreEqual(HttpStatusCode.Accepted, response.StatusCode, item.Email);
        }

        await using var verification = database.CreateContext();
        Assert.AreEqual(validCases.Length, await verification.Users.CountAsync());
        Assert.HasCount(validCases.Length, sender.Deliveries);
        foreach (var item in validCases)
        {
            var account = await verification.Users.SingleAsync(user => user.Email == item.Email);
            var hasher = new PasswordHasher<IdentityUser>();
            Assert.AreNotEqual(
                PasswordVerificationResult.Failed,
                hasher.VerifyHashedPassword(account, account.PasswordHash!, item.Password));
        }
    }

    [TestMethod]
    public async Task SequentialCaseVariantAndConcurrentDuplicatesHaveOneEffectAndIdenticalResponses()
    {
        await using var database = StorageTestDatabase.Create();
        await database.MigrateAsync();
        var keysPath = Directory.CreateDirectory(Path.Combine(database.RootPath, "keys")).FullName;
        var sender = new CapturingConfirmationSender();
        await using var factory = new AccountWebApplicationFactory(database.DatabasePath, keysPath, sender);
        using var client = factory.CreateClient();

        using var first = await client.PostAsJsonAsync(
            "/api/accounts/register",
            new { email = "Duplicate@example.test", password = ValidPassword });
        var firstBody = await first.Content.ReadAsStringAsync();
        using var sequential = await client.PostAsJsonAsync(
            "/api/accounts/register",
            new { email = "duplicate@EXAMPLE.TEST", password = ValidPassword });
        AssertResponseMatches(first, firstBody, sequential, await sequential.Content.ReadAsStringAsync());

        var concurrent = Enumerable.Range(0, 8)
            .Select(_ => client.PostAsJsonAsync(
                "/api/accounts/register",
                new { email = "DUPLICATE@example.test", password = ValidPassword }))
            .ToArray();
        await Task.WhenAll(concurrent);
        foreach (var response in concurrent.Select(task => task.Result))
        {
            using (response)
            {
                AssertResponseMatches(first, firstBody, response, await response.Content.ReadAsStringAsync());
            }
        }

        await using var verification = database.CreateContext();
        Assert.AreEqual(1, await verification.Users.CountAsync());
        Assert.HasCount(1, sender.Deliveries);
    }

    [TestMethod]
    public async Task DeliveryFailureKeepsTheGenericAcknowledgmentAndUnconfirmedAccount()
    {
        await using var database = StorageTestDatabase.Create();
        await database.MigrateAsync();
        var keysPath = Directory.CreateDirectory(Path.Combine(database.RootPath, "keys")).FullName;
        var sender = new FailingConfirmationSender();
        await using var factory = new AccountWebApplicationFactory(database.DatabasePath, keysPath, sender);
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/accounts/register",
            new { email = "delivery.failure@example.test", password = ValidPassword });

        Assert.AreEqual(HttpStatusCode.Accepted, response.StatusCode);
        Assert.AreEqual("{\"status\":\"verificationRequired\"}", await response.Content.ReadAsStringAsync());
        Assert.AreEqual(1, sender.Attempts);
        await using var verification = database.CreateContext();
        Assert.IsFalse((await verification.Users.SingleAsync()).EmailConfirmed);
    }

    [TestMethod]
    public async Task VerifiedAccountPolicyLoadsCurrentDurableState()
    {
        await using var database = StorageTestDatabase.Create();
        await database.MigrateAsync();
        var keysPath = Directory.CreateDirectory(Path.Combine(database.RootPath, "keys")).FullName;
        var sender = new CapturingConfirmationSender();
        await using var factory = new AccountWebApplicationFactory(database.DatabasePath, keysPath, sender);
        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync(
            "/api/accounts/register",
            new { email = "policy@example.test", password = ValidPassword });
        Assert.AreEqual(HttpStatusCode.Accepted, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var authorization = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();
        var account = await users.FindByEmailAsync("policy@example.test");
        Assert.IsNotNull(account);
        var principal = Principal(account.Id);
        Assert.IsFalse((await authorization.AuthorizeAsync(principal, null, VerifiedAccountAuthorization.PolicyName)).Succeeded);
        Assert.IsFalse((await authorization.AuthorizeAsync(Principal("missing"), null, VerifiedAccountAuthorization.PolicyName)).Succeeded);

        account.EmailConfirmed = true;
        Assert.IsTrue((await users.UpdateAsync(account)).Succeeded);
        Assert.IsTrue((await authorization.AuthorizeAsync(principal, null, VerifiedAccountAuthorization.PolicyName)).Succeeded);
        using var absentLanguageRoute = await client.PostAsync("/api/translate", content: null);
        Assert.AreEqual(HttpStatusCode.NotFound, absentLanguageRoute.StatusCode);
    }

    [TestMethod]
    public async Task WrongMethodsReturn405AndNoStore()
    {
        await using var database = StorageTestDatabase.Create();
        await database.MigrateAsync();
        var keysPath = Directory.CreateDirectory(Path.Combine(database.RootPath, "keys")).FullName;
        await using var factory = new AccountWebApplicationFactory(
            database.DatabasePath,
            keysPath,
            new CapturingConfirmationSender());
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/accounts/register");

        Assert.AreEqual(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        Assert.AreEqual("POST", response.Content.Headers.Allow.Single());
        Assert.AreEqual("no-store", response.Headers.CacheControl?.ToString());
    }

    [TestMethod]
    public async Task EmailScalarWhitespaceAndSyntaxBoundariesHaveNoInvalidEffects()
    {
        await using var database = StorageTestDatabase.Create();
        await database.MigrateAsync();
        var keysPath = Directory.CreateDirectory(Path.Combine(database.RootPath, "keys")).FullName;
        var sender = new CapturingConfirmationSender();
        await using var factory = new AccountWebApplicationFactory(database.DatabasePath, keysPath, sender);
        using var client = factory.CreateClient();
        var maximumEmail = $"{new string('a', 247)}@x.test";
        Assert.AreEqual(254, maximumEmail.Length);

        using (var accepted = await client.PostAsJsonAsync(
            "/api/accounts/register",
            new { email = maximumEmail, password = ValidPassword }))
        {
            Assert.AreEqual(HttpStatusCode.Accepted, accepted.StatusCode);
        }

        var invalidEmails = new[]
        {
            " invalid@example.test",
            "invalid@example.test ",
            "not-an-address",
            $"{new string('b', 248)}@x.test",
        };
        foreach (var email in invalidEmails)
        {
            using var response = await client.PostAsJsonAsync(
                "/api/accounts/register",
                new { email, password = ValidPassword });
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.IsFalse(body.Contains(email, StringComparison.Ordinal));
        }

        await using var verification = database.CreateContext();
        Assert.AreEqual(1, await verification.Users.CountAsync());
        Assert.HasCount(1, sender.Deliveries);
    }

    [TestMethod]
    public async Task ConfirmationMaterialRemainsValidAcrossAHostRestartWithTheSameKeys()
    {
        await using var database = StorageTestDatabase.Create();
        await database.MigrateAsync();
        var keysPath = Directory.CreateDirectory(Path.Combine(database.RootPath, "keys")).FullName;
        var sender = new CapturingConfirmationSender();
        await using (var firstFactory = new AccountWebApplicationFactory(database.DatabasePath, keysPath, sender))
        {
            using var client = firstFactory.CreateClient();
            using var response = await client.PostAsJsonAsync(
                "/api/accounts/register",
                new { email = "restart@example.test", password = ValidPassword });
            Assert.AreEqual(HttpStatusCode.Accepted, response.StatusCode);
        }

        var delivery = sender.Deliveries.Single();
        await using var secondFactory = new AccountWebApplicationFactory(
            database.DatabasePath,
            keysPath,
            new CapturingConfirmationSender());
        using var scope = secondFactory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var account = await users.FindByEmailAsync("restart@example.test");
        Assert.IsNotNull(account);
        Assert.IsTrue(await users.VerifyUserTokenAsync(
            account,
            TokenOptions.DefaultProvider,
            UserManager<IdentityUser>.ConfirmEmailTokenPurpose,
            delivery.Material));
    }

    [TestMethod]
    public async Task PostStartStorageLossReturnsSanitizedAvailabilityProblem()
    {
        await using var database = StorageTestDatabase.Create();
        await database.MigrateAsync();
        var keysPath = Directory.CreateDirectory(Path.Combine(database.RootPath, "keys")).FullName;
        await using var factory = new AccountWebApplicationFactory(
            database.DatabasePath,
            keysPath,
            new CapturingConfirmationSender());
        using var client = factory.CreateClient();
        File.Delete(database.DatabasePath);

        using var response = await client.PostAsJsonAsync(
            "/api/accounts/register",
            new { email = "unavailable@example.test", password = ValidPassword });
        var body = await response.Content.ReadAsStringAsync();

        Assert.AreEqual(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.AreEqual("no-store", response.Headers.CacheControl?.ToString());
        using var problem = JsonDocument.Parse(body);
        Assert.AreEqual("availability", problem.RootElement.GetProperty("category").GetString());
        Assert.IsFalse(body.Contains(database.DatabasePath, StringComparison.Ordinal));
        Assert.IsFalse(body.Contains("unavailable@example.test", StringComparison.Ordinal));
        Assert.IsFalse(body.Contains(ValidPassword, StringComparison.Ordinal));
    }

    private static ClaimsPrincipal Principal(string accountId) => new(
        new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, accountId)], "test"));

    private static void AssertResponseMatches(
        HttpResponseMessage expected,
        string expectedBody,
        HttpResponseMessage actual,
        string actualBody)
    {
        Assert.AreEqual(expected.StatusCode, actual.StatusCode);
        Assert.AreEqual(expectedBody, actualBody);
        Assert.AreEqual(expected.Headers.CacheControl?.ToString(), actual.Headers.CacheControl?.ToString());
        Assert.AreEqual(expected.Headers.Contains("Set-Cookie"), actual.Headers.Contains("Set-Cookie"));
        Assert.AreEqual(expected.Headers.Contains("WWW-Authenticate"), actual.Headers.Contains("WWW-Authenticate"));
    }
}

internal sealed class AccountWebApplicationFactory(
    string databasePath,
    string keysPath,
    IAccountConfirmationSender sender,
    string environment = "Testing",
    IReadOnlyDictionary<string, string?>? configurationOverrides = null) : WebApplicationFactory<Program>
{
    private readonly string webRoot = Directory.CreateTempSubdirectory("linguadesk-account-webroot-").FullName;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(environment);
        builder.UseWebRoot(webRoot);
        builder.ConfigureAppConfiguration(configuration =>
        {
            var settings = new Dictionary<string, string?>
            {
                ["Storage:DatabasePath"] = databasePath,
                ["Security:DataProtectionKeysPath"] = keysPath,
            };
            if (configurationOverrides is not null)
            {
                foreach (var setting in configurationOverrides)
                {
                    settings[setting.Key] = setting.Value;
                }
            }
            configuration.AddInMemoryCollection(settings);
        });
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IAccountConfirmationSender>();
            services.AddSingleton(sender);
            services.AddSingleton<IAccountConfirmationSender>(sender);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && Directory.Exists(webRoot))
        {
            Directory.Delete(webRoot, recursive: true);
        }
    }
}

internal sealed class CapturingConfirmationSender : IAccountConfirmationSender
{
    private readonly ConcurrentQueue<(string Destination, string Material)> deliveries = new();

    public IReadOnlyCollection<(string Destination, string Material)> Deliveries => deliveries.ToArray();

    public Task SendAsync(string destination, string verificationMaterial, CancellationToken cancellationToken)
    {
        deliveries.Enqueue((destination, verificationMaterial));
        return Task.CompletedTask;
    }
}

internal sealed class FailingConfirmationSender : IAccountConfirmationSender
{
    private int attempts;

    public int Attempts => Volatile.Read(ref attempts);

    public Task SendAsync(string destination, string verificationMaterial, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref attempts);
        throw new AccountConfirmationDeliveryException();
    }
}
