using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using LinguaDesk.Api.Features.Identity;
using LinguaDesk.Api.Features.Identity.Registration;
using LinguaDesk.Api.Features.Identity.Verification;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LinguaDesk.Api.Tests;

[TestClass]
public sealed class AccountVerificationTests
{
    private const string ValidPassword = "Maple!River2026";

    [TestMethod]
    public async Task CapturedRegistrationDeliveryConfirmsDurablyAndEnablesCurrentPolicy()
    {
        await using var database = StorageTestDatabase.Create();
        await database.MigrateAsync();
        var keysPath = Directory.CreateDirectory(Path.Combine(database.RootPath, "keys")).FullName;
        var sender = new CapturingConfirmationSender();
        await using var factory = new AccountWebApplicationFactory(database.DatabasePath, keysPath, sender);
        using var client = factory.CreateClient();

        using var registration = await RegisterAsync(client, "roundtrip@example.test");
        Assert.AreEqual(HttpStatusCode.Accepted, registration.StatusCode);
        var delivery = sender.Deliveries.Single();
        Assert.AreEqual("roundtrip@example.test", delivery.Destination);
        Assert.IsGreaterThan(0, delivery.UserId.Length);
        Assert.IsTrue(delivery.Code.All(IsBase64UrlCharacter));

        using var confirmation = await client.PostAsJsonAsync(
            "/api/accounts/confirm-email",
            new { userId = delivery.UserId, code = delivery.Code });
        Assert.AreEqual(HttpStatusCode.OK, confirmation.StatusCode);
        Assert.AreEqual("{\"status\":\"verified\"}", await confirmation.Content.ReadAsStringAsync());
        Assert.AreEqual("no-store", confirmation.Headers.CacheControl?.ToString());
        Assert.IsFalse(confirmation.Headers.Contains("Set-Cookie"));
        Assert.IsFalse(confirmation.Headers.Contains("Location"));

        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var account = await users.FindByIdAsync(delivery.UserId);
        Assert.IsNotNull(account);
        Assert.IsTrue(account.EmailConfirmed);
        var authorization = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();
        Assert.IsTrue((await authorization.AuthorizeAsync(
            Principal(account.Id),
            null,
            VerifiedAccountAuthorization.PolicyName)).Succeeded);
        using var absentLanguageRoute = await client.PostAsync("/api/translate", null);
        Assert.AreEqual(HttpStatusCode.NotFound, absentLanguageRoute.StatusCode);
    }

    [TestMethod]
    public async Task InvalidCorruptMismatchedUnknownAndBoundedMaterialUsesOneGenericCategory()
    {
        await using var database = StorageTestDatabase.Create();
        await database.MigrateAsync();
        var keysPath = Directory.CreateDirectory(Path.Combine(database.RootPath, "keys")).FullName;
        var sender = new CapturingConfirmationSender();
        await using var factory = new AccountWebApplicationFactory(database.DatabasePath, keysPath, sender);
        using var client = factory.CreateClient();
        using (await RegisterAsync(client, "first@example.test")) { }
        using (await RegisterAsync(client, "second@example.test")) { }
        var first = sender.Deliveries.First();
        var second = sender.Deliveries.Last();
        var corruptCode = first.Code[..^1] + (first.Code[^1] == 'A' ? "B" : "A");
        var cases = new[]
        {
            (UserId: first.UserId, Code: string.Empty),
            (UserId: string.Empty, Code: first.Code),
            (UserId: first.UserId, Code: "not+padded="),
            (UserId: first.UserId, Code: corruptCode),
            (UserId: second.UserId, Code: first.Code),
            (UserId: "unknown-account", Code: first.Code),
            (UserId: new string('u', 451), Code: first.Code),
            (UserId: first.UserId, Code: new string('A', 4097)),
        };

        foreach (var item in cases)
        {
            using var response = await client.PostAsJsonAsync(
                "/api/accounts/confirm-email",
                new { userId = item.UserId, code = item.Code });
            var body = await response.Content.ReadAsStringAsync();
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.AreEqual("no-store", response.Headers.CacheControl?.ToString());
            using var problem = JsonDocument.Parse(body);
            Assert.AreEqual("invalidOrExpiredVerification", problem.RootElement.GetProperty("category").GetString());
            Assert.AreEqual("The verification link is invalid or expired.", problem.RootElement.GetProperty("detail").GetString());
            Assert.IsFalse(body.Contains(first.UserId, StringComparison.Ordinal));
            Assert.IsFalse(body.Contains(first.Code, StringComparison.Ordinal));
        }

        await using var verification = database.CreateContext();
        Assert.IsTrue(await verification.Users.AllAsync(user => !user.EmailConfirmed));
    }

    [TestMethod]
    public async Task ConfirmationSurvivesRestartAndValidReplayIsIdempotent()
    {
        await using var database = StorageTestDatabase.Create();
        await database.MigrateAsync();
        var keysPath = Directory.CreateDirectory(Path.Combine(database.RootPath, "keys")).FullName;
        var sender = new CapturingConfirmationSender();
        await using (var firstFactory = new AccountWebApplicationFactory(database.DatabasePath, keysPath, sender))
        {
            using var firstClient = firstFactory.CreateClient();
            using (await RegisterAsync(firstClient, "restart-confirm@example.test")) { }
        }

        var delivery = sender.Deliveries.Single();
        await using var secondFactory = new AccountWebApplicationFactory(
            database.DatabasePath,
            keysPath,
            new CapturingConfirmationSender());
        using var secondClient = secondFactory.CreateClient();
        for (var attempt = 0; attempt < 2; attempt++)
        {
            using var response = await secondClient.PostAsJsonAsync(
                "/api/accounts/confirm-email",
                new { userId = delivery.UserId, code = delivery.Code });
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual("{\"status\":\"verified\"}", await response.Content.ReadAsStringAsync());
        }

        await using var verification = database.CreateContext();
        Assert.IsTrue((await verification.Users.SingleAsync()).EmailConfirmed);
    }

    [TestMethod]
    public async Task DedicatedProviderPinsEmailLifetimeWithoutChangingResetProvider()
    {
        await using var database = StorageTestDatabase.Create();
        await database.MigrateAsync();
        var keysPath = Directory.CreateDirectory(Path.Combine(database.RootPath, "keys")).FullName;
        await using var factory = new AccountWebApplicationFactory(
            database.DatabasePath,
            keysPath,
            new CapturingConfirmationSender());
        _ = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var identity = scope.ServiceProvider.GetRequiredService<IOptions<IdentityOptions>>().Value;
        var email = scope.ServiceProvider
            .GetRequiredService<IOptions<LinguaDeskEmailConfirmationTokenProviderOptions>>()
            .Value;

        Assert.AreEqual(LinguaDeskEmailConfirmationTokenPolicy.ProviderName, identity.Tokens.EmailConfirmationTokenProvider);
        Assert.AreEqual(TokenOptions.DefaultProvider, identity.Tokens.PasswordResetTokenProvider);
        Assert.AreEqual(TimeSpan.FromHours(24), email.TokenLifespan);
        Assert.AreEqual(LinguaDeskEmailConfirmationTokenPolicy.ProviderName, email.Name);
    }

    [TestMethod]
    public async Task ZeroLifetimeProviderRejectsOtherwiseValidMaterial()
    {
        await using var database = StorageTestDatabase.Create();
        await database.MigrateAsync();
        var keysPath = Directory.CreateDirectory(Path.Combine(database.RootPath, "keys")).FullName;
        var sender = new CapturingConfirmationSender();
        await using var factory = new AccountWebApplicationFactory(
            database.DatabasePath,
            keysPath,
            sender,
            emailConfirmationTokenLifespan: TimeSpan.Zero);
        using var client = factory.CreateClient();
        using (await RegisterAsync(client, "expired@example.test")) { }
        var delivery = sender.Deliveries.Single();

        using var response = await client.PostAsJsonAsync(
            "/api/accounts/confirm-email",
            new { userId = delivery.UserId, code = delivery.Code });
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.AreEqual("invalidOrExpiredVerification", problem.RootElement.GetProperty("category").GetString());
    }

    [TestMethod]
    public async Task ResendAcknowledgmentIsEquivalentForKnownConfirmedAndUnknownAccounts()
    {
        await using var database = StorageTestDatabase.Create();
        await database.MigrateAsync();
        var keysPath = Directory.CreateDirectory(Path.Combine(database.RootPath, "keys")).FullName;
        var sender = new CapturingConfirmationSender();
        await using var factory = new AccountWebApplicationFactory(database.DatabasePath, keysPath, sender);
        using var client = factory.CreateClient();
        using (await RegisterAsync(client, "known@example.test")) { }
        using (await RegisterAsync(client, "confirmed@example.test")) { }
        var confirmed = sender.Deliveries.Last();
        using (var response = await client.PostAsJsonAsync(
            "/api/accounts/confirm-email",
            new { userId = confirmed.UserId, code = confirmed.Code }))
        {
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        using var known = await ResendAsync(client, "known@example.test");
        var knownBody = await known.Content.ReadAsStringAsync();
        using var alreadyConfirmed = await ResendAsync(client, "confirmed@example.test");
        using var unknown = await ResendAsync(client, "unknown@example.test");
        AssertEquivalentAcknowledgment(known, knownBody, alreadyConfirmed, await alreadyConfirmed.Content.ReadAsStringAsync());
        AssertEquivalentAcknowledgment(known, knownBody, unknown, await unknown.Content.ReadAsStringAsync());
        Assert.AreEqual("{\"status\":\"verificationRequested\",\"retryAfterSeconds\":60}", knownBody);
        Assert.HasCount(2, sender.Deliveries);
    }

    [TestMethod]
    public async Task CooldownBoundaryRestartAndControlledConcurrencyAllowOneNewDelivery()
    {
        await using var database = StorageTestDatabase.Create();
        await database.MigrateAsync();
        var keysPath = Directory.CreateDirectory(Path.Combine(database.RootPath, "keys")).FullName;
        var time = new AdjustableTimeProvider(new DateTimeOffset(2026, 9, 9, 12, 0, 0, TimeSpan.Zero));
        var sender = new BlockingConfirmationSender();
        await using (var firstFactory = new AccountWebApplicationFactory(
            database.DatabasePath, keysPath, sender, timeProvider: time))
        {
            using var firstClient = firstFactory.CreateClient();
            using (await RegisterAsync(firstClient, "cooldown@example.test")) { }
            using (await ResendAsync(firstClient, "cooldown@example.test")) { }
            time.Advance(TimeSpan.FromSeconds(59));
            using (await ResendAsync(firstClient, "cooldown@example.test")) { }
            Assert.HasCount(1, sender.Deliveries);
        }

        await using var secondFactory = new AccountWebApplicationFactory(
            database.DatabasePath, keysPath, sender, timeProvider: time);
        using var secondClient = secondFactory.CreateClient();
        using (await ResendAsync(secondClient, "cooldown@example.test")) { }
        Assert.HasCount(1, sender.Deliveries);
        time.Advance(TimeSpan.FromSeconds(1));
        sender.BlockNextDelivery();
        var first = ResendAsync(secondClient, "cooldown@example.test");
        await sender.WaitUntilBlockedAsync();
        var concurrent = Enumerable.Range(0, 7)
            .Select(_ => ResendAsync(secondClient, "cooldown@example.test"))
            .ToArray();
        sender.ReleaseBlockedDelivery();
        using (await first) { }
        foreach (var response in await Task.WhenAll(concurrent)) response.Dispose();
        Assert.HasCount(2, sender.Deliveries);

        await using var verification = database.CreateContext();
        var marker = await verification.UserTokens.SingleAsync();
        Assert.AreEqual(AccountVerificationDeliveryCoordinator.CooldownTokenProvider, marker.LoginProvider);
        Assert.AreEqual(AccountVerificationDeliveryCoordinator.CooldownTokenName, marker.Name);
        Assert.AreEqual(time.GetUtcNow().ToString("O"), marker.Value);
    }

    [TestMethod]
    public async Task FailedRegistrationDeliveryRecoversAfterCooldownAndConfirms()
    {
        await using var database = StorageTestDatabase.Create();
        await database.MigrateAsync();
        var keysPath = Directory.CreateDirectory(Path.Combine(database.RootPath, "keys")).FullName;
        var time = new AdjustableTimeProvider(new DateTimeOffset(2026, 9, 9, 13, 0, 0, TimeSpan.Zero));
        var sender = new RecoveringConfirmationSender(failures: 1);
        await using var factory = new AccountWebApplicationFactory(
            database.DatabasePath, keysPath, sender, timeProvider: time);
        using var client = factory.CreateClient();
        using var registration = await RegisterAsync(client, "recover@example.test");
        Assert.AreEqual(HttpStatusCode.Accepted, registration.StatusCode);
        Assert.AreEqual(1, sender.Attempts);
        Assert.HasCount(0, sender.Deliveries);

        time.Advance(TimeSpan.FromSeconds(60));
        using var resend = await ResendAsync(client, "recover@example.test");
        Assert.AreEqual(HttpStatusCode.Accepted, resend.StatusCode);
        var delivery = sender.Deliveries.Single();
        using var confirmation = await client.PostAsJsonAsync(
            "/api/accounts/confirm-email",
            new { userId = delivery.UserId, code = delivery.Code });
        Assert.AreEqual(HttpStatusCode.OK, confirmation.StatusCode);
    }

    [TestMethod]
    public async Task ThrowingResendRetainsGenericAcknowledgmentAndDoesNotRetryAutomatically()
    {
        await using var database = StorageTestDatabase.Create();
        await database.MigrateAsync();
        var keysPath = Directory.CreateDirectory(Path.Combine(database.RootPath, "keys")).FullName;
        var time = new AdjustableTimeProvider(new DateTimeOffset(2026, 9, 9, 14, 0, 0, TimeSpan.Zero));
        var sender = new FailingConfirmationSender();
        await using var factory = new AccountWebApplicationFactory(
            database.DatabasePath, keysPath, sender, timeProvider: time);
        using var client = factory.CreateClient();
        using (await RegisterAsync(client, "throwing@example.test")) { }
        time.Advance(TimeSpan.FromSeconds(60));
        using var known = await ResendAsync(client, "throwing@example.test");
        var knownBody = await known.Content.ReadAsStringAsync();
        using var unknown = await ResendAsync(client, "absent@example.test");
        AssertEquivalentAcknowledgment(known, knownBody, unknown, await unknown.Content.ReadAsStringAsync());
        Assert.AreEqual(2, sender.Attempts);
        await using var verification = database.CreateContext();
        Assert.IsFalse((await verification.Users.SingleAsync()).EmailConfirmed);
    }

    [TestMethod]
    public async Task StrictJsonMediaQueryAndMethodBoundariesHaveNoVerificationEffects()
    {
        await using var database = StorageTestDatabase.Create();
        await database.MigrateAsync();
        var keysPath = Directory.CreateDirectory(Path.Combine(database.RootPath, "keys")).FullName;
        var sender = new CapturingConfirmationSender();
        await using var factory = new AccountWebApplicationFactory(database.DatabasePath, keysPath, sender);
        using var client = factory.CreateClient();
        using (await RegisterAsync(client, "boundary@example.test")) { }
        var delivery = sender.Deliveries.Single();

        using var duplicate = new StringContent(
            $"{{\"userId\":\"{delivery.UserId}\",\"userId\":\"{delivery.UserId}\",\"code\":\"{delivery.Code}\"}}",
            Encoding.UTF8,
            "application/json");
        using var duplicateResponse = await client.PostAsync("/api/accounts/confirm-email", duplicate);
        Assert.AreEqual(HttpStatusCode.BadRequest, duplicateResponse.StatusCode);
        using var queryResponse = await client.PostAsJsonAsync(
            "/api/accounts/confirm-email?code=forbidden",
            new { userId = delivery.UserId, code = delivery.Code });
        Assert.AreEqual(HttpStatusCode.BadRequest, queryResponse.StatusCode);
        using var mediaResponse = await client.PostAsync(
            "/api/accounts/resend-verification",
            new StringContent("{}", Encoding.UTF8, "text/plain"));
        Assert.AreEqual(HttpStatusCode.UnsupportedMediaType, mediaResponse.StatusCode);
        using var invalidEncodingContent = new ByteArrayContent([0x7B, 0x22, 0x63, 0x6F, 0x64, 0x65, 0x22, 0x3A, 0x22, 0xC3, 0x28, 0x22, 0x7D]);
        invalidEncodingContent.Headers.ContentType = new("application/json") { CharSet = "utf-8" };
        using var invalidEncodingResponse = await client.PostAsync(
            "/api/accounts/confirm-email",
            invalidEncodingContent);
        Assert.AreEqual(HttpStatusCode.BadRequest, invalidEncodingResponse.StatusCode);
        Assert.AreEqual("no-store", invalidEncodingResponse.Headers.CacheControl?.ToString());
        using var resendShapeResponse = await client.PostAsJsonAsync(
            "/api/accounts/resend-verification",
            new { email = "boundary@example.test", unexpected = true });
        Assert.AreEqual(HttpStatusCode.BadRequest, resendShapeResponse.StatusCode);
        Assert.AreEqual("no-store", resendShapeResponse.Headers.CacheControl?.ToString());
        using var methodResponse = await client.GetAsync("/api/accounts/confirm-email");
        Assert.AreEqual(HttpStatusCode.MethodNotAllowed, methodResponse.StatusCode);
        Assert.AreEqual("POST", methodResponse.Content.Headers.Allow.Single());
        Assert.AreEqual("no-store", methodResponse.Headers.CacheControl?.ToString());
        using var resendMethodResponse = await client.DeleteAsync("/api/accounts/resend-verification");
        Assert.AreEqual(HttpStatusCode.MethodNotAllowed, resendMethodResponse.StatusCode);
        Assert.AreEqual("POST", resendMethodResponse.Content.Headers.Allow.Single());
        Assert.AreEqual("no-store", resendMethodResponse.Headers.CacheControl?.ToString());

        await using var verification = database.CreateContext();
        Assert.IsFalse((await verification.Users.SingleAsync()).EmailConfirmed);
        Assert.HasCount(1, sender.Deliveries);
    }

    [TestMethod]
    public async Task PostStartStorageLossReturnsSanitizedAvailability()
    {
        await using var database = StorageTestDatabase.Create();
        await database.MigrateAsync();
        var keysPath = Directory.CreateDirectory(Path.Combine(database.RootPath, "keys")).FullName;
        await using var factory = new AccountWebApplicationFactory(
            database.DatabasePath, keysPath, new CapturingConfirmationSender());
        using var client = factory.CreateClient();
        File.Delete(database.DatabasePath);

        using var confirmation = await client.PostAsJsonAsync(
            "/api/accounts/confirm-email",
            new { userId = "opaque-account", code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes("opaque-token")) });
        using var resend = await ResendAsync(client, "storage@example.test");
        foreach (var response in new[] { confirmation, resend })
        {
            var body = await response.Content.ReadAsStringAsync();
            Assert.AreEqual(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            Assert.AreEqual("no-store", response.Headers.CacheControl?.ToString());
            using var problem = JsonDocument.Parse(body);
            Assert.AreEqual("availability", problem.RootElement.GetProperty("category").GetString());
            Assert.IsFalse(body.Contains(database.DatabasePath, StringComparison.Ordinal));
            Assert.IsFalse(body.Contains("opaque-token", StringComparison.Ordinal));
            Assert.IsFalse(body.Contains("storage@example.test", StringComparison.Ordinal));
        }
    }

    private static Task<HttpResponseMessage> RegisterAsync(HttpClient client, string email) =>
        client.PostAsJsonAsync("/api/accounts/register", new { email, password = ValidPassword });

    private static Task<HttpResponseMessage> ResendAsync(HttpClient client, string email) =>
        client.PostAsJsonAsync("/api/accounts/resend-verification", new { email });

    private static bool IsBase64UrlCharacter(char character) => character is >= 'A' and <= 'Z'
        or >= 'a' and <= 'z'
        or >= '0' and <= '9'
        or '-'
        or '_';

    private static ClaimsPrincipal Principal(string accountId) => new(
        new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, accountId)], "test"));

    private static void AssertEquivalentAcknowledgment(
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
        Assert.AreEqual(expected.Headers.Contains("Location"), actual.Headers.Contains("Location"));
    }
}

internal sealed class AdjustableTimeProvider(DateTimeOffset initial) : TimeProvider
{
    private DateTimeOffset utcNow = initial;

    public override DateTimeOffset GetUtcNow() => utcNow;

    public void Advance(TimeSpan duration) => utcNow = utcNow.Add(duration);
}

internal sealed class BlockingConfirmationSender : IAccountConfirmationSender
{
    private readonly ConcurrentQueue<AccountConfirmationDelivery> deliveries = new();
    private TaskCompletionSource? blocked;
    private TaskCompletionSource? release;

    public IReadOnlyCollection<AccountConfirmationDelivery> Deliveries => deliveries.ToArray();

    public void BlockNextDelivery()
    {
        blocked = new(TaskCreationOptions.RunContinuationsAsynchronously);
        release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public Task WaitUntilBlockedAsync() => blocked?.Task ?? Task.CompletedTask;

    public void ReleaseBlockedDelivery() => release?.TrySetResult();

    public async Task SendAsync(AccountConfirmationDelivery delivery, CancellationToken cancellationToken)
    {
        deliveries.Enqueue(delivery);
        if (blocked is not null && release is not null)
        {
            blocked.TrySetResult();
            await release.Task.WaitAsync(cancellationToken);
            blocked = null;
            release = null;
        }
    }
}

internal sealed class RecoveringConfirmationSender(int failures) : IAccountConfirmationSender
{
    private readonly ConcurrentQueue<AccountConfirmationDelivery> deliveries = new();
    private int failuresRemaining = failures;
    private int attempts;

    public int Attempts => Volatile.Read(ref attempts);
    public IReadOnlyCollection<AccountConfirmationDelivery> Deliveries => deliveries.ToArray();

    public Task SendAsync(AccountConfirmationDelivery delivery, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref attempts);
        if (Interlocked.Decrement(ref failuresRemaining) >= 0)
        {
            throw new AccountConfirmationDeliveryException();
        }
        deliveries.Enqueue(delivery);
        return Task.CompletedTask;
    }
}
