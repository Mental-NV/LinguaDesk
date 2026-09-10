using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using LinguaDesk.Api.Features.Identity.Recovery;
using LinguaDesk.Api.Features.Identity.Registration;
using LinguaDesk.Api.Features.Identity.Verification;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LinguaDesk.Api.Tests;

[TestClass]
public sealed class AccountRecoveryTests
{
    private const string DefaultPassword = "Maple!River2026";
    private const string SpacedPassword = " exact password 15 ";
    private const string UnicodePassword = "ПарольДляТеста2026";

    [TestMethod]
    public async Task VerifiedAccountResetCompletesWithOldRejectedAndNewAccepted()
    {
        await using var fixture = await RecoveryFixture.CreateAsync();
        await fixture.CreateAccountAsync(fixture.Email, confirmed: true);
        using var forgot = await fixture.ForgotAsync(fixture.Email);
        Assert.AreEqual(HttpStatusCode.Accepted, forgot.StatusCode);
        Assert.AreEqual("{\"status\":\"passwordResetRequested\",\"retryAfterSeconds\":60}", await forgot.Content.ReadAsStringAsync());
        Assert.AreEqual("no-store", forgot.Headers.CacheControl?.ToString());
        Assert.IsFalse(forgot.Headers.Contains("Set-Cookie"));
        Assert.IsFalse(forgot.Headers.Contains("Location"));
        var delivery = fixture.Sender.Deliveries.Single();
        Assert.AreEqual(fixture.Email, delivery.Destination);
        Assert.IsGreaterThan(0, delivery.UserId.Length);
        Assert.IsTrue(delivery.Code.All(IsBase64UrlCharacter));

        using var reset = await fixture.ResetAsync(delivery.UserId, delivery.Code, "NewMaple!River2027");
        Assert.AreEqual(HttpStatusCode.OK, reset.StatusCode);
        Assert.AreEqual("{\"status\":\"passwordReset\"}", await reset.Content.ReadAsStringAsync());
        Assert.AreEqual("no-store", reset.Headers.CacheControl?.ToString());
        Assert.IsFalse(reset.Headers.Contains("Set-Cookie"));
        Assert.IsFalse(reset.Headers.Contains("Location"));

        using var oldCookie = await fixture.CookieSignInAsync(fixture.Email, DefaultPassword);
        Assert.AreEqual(HttpStatusCode.Unauthorized, oldCookie.StatusCode);
        using var oldBearer = await fixture.BearerSignInAsync(fixture.Email, DefaultPassword);
        Assert.AreEqual(HttpStatusCode.Unauthorized, oldBearer.StatusCode);
        using var newCookie = await fixture.CookieSignInAsync(fixture.Email, "NewMaple!River2027");
        Assert.AreEqual(HttpStatusCode.OK, newCookie.StatusCode);
        StringAssert.Contains(await newCookie.Content.ReadAsStringAsync(), "verified");
        using var newBearer = await fixture.BearerSignInAsync(fixture.Email, "NewMaple!River2027");
        Assert.AreEqual(HttpStatusCode.OK, newBearer.StatusCode);
        var pair = await newBearer.Content.ReadFromJsonAsync<JsonElement>();
        using var me = await fixture.GetMeAsync(pair.GetProperty("accessToken").GetString()!);
        Assert.AreEqual(HttpStatusCode.OK, me.StatusCode);
        StringAssert.Contains(await me.Content.ReadAsStringAsync(), "verified");

        using var scope = fixture.Factory.Services.CreateScope();
        var account = await scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>().FindByEmailAsync(fixture.Email);
        Assert.IsNotNull(account);
        Assert.IsTrue(account.EmailConfirmed);
    }

    [TestMethod]
    public async Task ForgotAcknowledgmentEquivalentAcrossKnownUnknownVerifiedUnverified()
    {
        await using var fixture = await RecoveryFixture.CreateAsync();
        await fixture.CreateAccountAsync("verified@example.test", confirmed: true);
        await fixture.CreateAccountAsync("unverified@example.test", confirmed: false);

        using var verified = await fixture.ForgotAsync("verified@example.test");
        var verifiedBody = await verified.Content.ReadAsStringAsync();
        using var unverified = await fixture.ForgotAsync("unverified@example.test");
        using var unknown = await fixture.ForgotAsync("absent@example.test");
        AssertEquivalentAcknowledgment(verified, verifiedBody, unverified, await unverified.Content.ReadAsStringAsync());
        AssertEquivalentAcknowledgment(verified, verifiedBody, unknown, await unknown.Content.ReadAsStringAsync());
        Assert.AreEqual("{\"status\":\"passwordResetRequested\",\"retryAfterSeconds\":60}", verifiedBody);
        Assert.HasCount(2, fixture.Sender.Deliveries);

        using var malformed = await fixture.ForgotAsync("not-an-email");
        Assert.AreEqual(HttpStatusCode.BadRequest, malformed.StatusCode);
        var malformedBody = await malformed.Content.ReadAsStringAsync();
        using var malformedProblem = JsonDocument.Parse(malformedBody);
        Assert.AreEqual("invalidRequest", malformedProblem.RootElement.GetProperty("category").GetString());
        Assert.IsTrue(malformedProblem.RootElement.TryGetProperty("errors", out var errors));
        Assert.IsTrue(errors.TryGetProperty("email", out _));
        Assert.IsFalse(malformedBody.Contains("not-an-email", StringComparison.Ordinal));
        Assert.HasCount(2, fixture.Sender.Deliveries);

        using var surrogateContent = new StringContent(
            "{\"email\":\"x-\\ud800-y@example.test\"}",
            Encoding.UTF8,
            "application/json");
        using var surrogate = await fixture.PlainClient.PostAsync("/api/accounts/forgot-password", surrogateContent);
        var surrogateBody = await surrogate.Content.ReadAsStringAsync();
        Assert.AreEqual(HttpStatusCode.BadRequest, surrogate.StatusCode);
        using var surrogateProblem = JsonDocument.Parse(surrogateBody);
        Assert.AreEqual("invalidRequest", surrogateProblem.RootElement.GetProperty("category").GetString());
        StringAssert.Contains(surrogateProblem.RootElement.GetProperty("errors").GetRawText(), "email");
        Assert.HasCount(2, fixture.Sender.Deliveries);
    }

    [TestMethod]
    public async Task ResetRejectsInvalidExpiredReplayedMaterialGenerically()
    {
        await using var fixture = await RecoveryFixture.CreateAsync();
        await fixture.CreateAccountAsync("first@example.test", confirmed: true);
        await fixture.CreateAccountAsync("second@example.test", confirmed: true);
        using (await fixture.ForgotAsync("first@example.test")) { }
        using (await fixture.ForgotAsync("second@example.test")) { }
        var first = fixture.Sender.Deliveries.First();
        var second = fixture.Sender.Deliveries.Last();
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
            using var response = await fixture.ResetAsync(item.UserId, item.Code, "AnotherValidPass99");
            var body = await response.Content.ReadAsStringAsync();
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.AreEqual("no-store", response.Headers.CacheControl?.ToString());
            Assert.IsFalse(response.Headers.Contains("Set-Cookie"));
            using var problem = JsonDocument.Parse(body);
            Assert.AreEqual("invalidOrExpiredPasswordReset", problem.RootElement.GetProperty("category").GetString());
            Assert.AreEqual("The password-reset link is invalid or expired.", problem.RootElement.GetProperty("detail").GetString());
            Assert.IsFalse(body.Contains(first.UserId, StringComparison.Ordinal));
            Assert.IsFalse(body.Contains(first.Code, StringComparison.Ordinal));
            Assert.IsFalse(body.Contains("AnotherValidPass99", StringComparison.Ordinal));
        }

        using var surrogateContent = new StringContent(
            $"{{\"userId\":\"x-\\ud800-y\",\"code\":\"{first.Code}\",\"newPassword\":\"AnotherValidPass99\"}}",
            Encoding.UTF8,
            "application/json");
        using var surrogate = await fixture.PlainClient.PostAsync("/api/accounts/reset-password", surrogateContent);
        using var surrogateProblem = JsonDocument.Parse(await surrogate.Content.ReadAsStringAsync());
        Assert.AreEqual(HttpStatusCode.BadRequest, surrogate.StatusCode);
        Assert.AreEqual("invalidOrExpiredPasswordReset", surrogateProblem.RootElement.GetProperty("category").GetString());

        using var intact = await fixture.BearerSignInAsync("first@example.test", DefaultPassword);
        Assert.AreEqual(HttpStatusCode.OK, intact.StatusCode);

        using var completed = await fixture.ResetAsync(first.UserId, first.Code, "AnotherValidPass99");
        Assert.AreEqual(HttpStatusCode.OK, completed.StatusCode);
        using var replay = await fixture.ResetAsync(first.UserId, first.Code, "YetAnotherValid88");
        Assert.AreEqual(HttpStatusCode.BadRequest, replay.StatusCode);
        using var replayProblem = JsonDocument.Parse(await replay.Content.ReadAsStringAsync());
        Assert.AreEqual("invalidOrExpiredPasswordReset", replayProblem.RootElement.GetProperty("category").GetString());
        using var afterReplay = await fixture.BearerSignInAsync("first@example.test", "AnotherValidPass99");
        Assert.AreEqual(HttpStatusCode.OK, afterReplay.StatusCode);
    }

    [TestMethod]
    public async Task ResetMaterialSurvivesRestartAndValidReplayAfterConfirmIsRejected()
    {
        await using var database = StorageTestDatabase.Create();
        await database.MigrateAsync();
        var keysPath = Directory.CreateDirectory(Path.Combine(database.RootPath, "keys")).FullName;
        var sender = new CapturingResetSender();
        await using (var firstFactory = new AccountWebApplicationFactory(
            database.DatabasePath, keysPath, new CapturingConfirmationSender(), resetSender: sender))
        {
            using var scope = firstFactory.Services.CreateScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var created = await users.CreateAsync(
                new IdentityUser { UserName = "restart@example.test", Email = "restart@example.test", EmailConfirmed = true },
                DefaultPassword);
            Assert.IsTrue(created.Succeeded);
            using var client = firstFactory.CreateClient();
            using var forgot = await client.PostAsJsonAsync("/api/accounts/forgot-password", new { email = "restart@example.test" });
            Assert.AreEqual(HttpStatusCode.Accepted, forgot.StatusCode);
        }

        var delivery = sender.Deliveries.Single();
        await using var secondFactory = new AccountWebApplicationFactory(
            database.DatabasePath, keysPath, new CapturingConfirmationSender(), resetSender: new CapturingResetSender());
        using var secondClient = secondFactory.CreateClient();
        using var reset = await secondClient.PostAsJsonAsync(
            "/api/accounts/reset-password",
            new { userId = delivery.UserId, code = delivery.Code, newPassword = "RestartValidPass77" });
        Assert.AreEqual(HttpStatusCode.OK, reset.StatusCode);
        Assert.AreEqual("{\"status\":\"passwordReset\"}", await reset.Content.ReadAsStringAsync());
    }

    [TestMethod]
    public async Task ZeroLifetimePinRejectsOtherwiseValidMaterial()
    {
        await using var database = StorageTestDatabase.Create();
        await database.MigrateAsync();
        var keysPath = Directory.CreateDirectory(Path.Combine(database.RootPath, "keys")).FullName;
        var sender = new CapturingResetSender();
        await using var factory = new AccountWebApplicationFactory(
            database.DatabasePath, keysPath, new CapturingConfirmationSender(),
            resetSender: sender, passwordResetTokenLifespan: TimeSpan.Zero);
        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var created = await users.CreateAsync(
            new IdentityUser { UserName = "expired@example.test", Email = "expired@example.test", EmailConfirmed = true },
            DefaultPassword);
        Assert.IsTrue(created.Succeeded);
        using var client = factory.CreateClient();
        using (await client.PostAsJsonAsync("/api/accounts/forgot-password", new { email = "expired@example.test" })) { }
        var delivery = sender.Deliveries.Single();

        using var response = await client.PostAsJsonAsync(
            "/api/accounts/reset-password",
            new { userId = delivery.UserId, code = delivery.Code, newPassword = "AnotherValidPass99" });
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.AreEqual("invalidOrExpiredPasswordReset", problem.RootElement.GetProperty("category").GetString());
    }

    [TestMethod]
    public async Task DefaultResetLifespanPinnedWithoutChangingConfirmationProvider()
    {
        await using var fixture = await RecoveryFixture.CreateAsync();
        using var scope = fixture.Factory.Services.CreateScope();
        var identity = scope.ServiceProvider.GetRequiredService<IOptions<IdentityOptions>>().Value;
        var reset = scope.ServiceProvider.GetRequiredService<IOptions<DataProtectionTokenProviderOptions>>().Value;
        var email = scope.ServiceProvider
            .GetRequiredService<IOptions<LinguaDeskEmailConfirmationTokenProviderOptions>>()
            .Value;

        Assert.AreEqual(TokenOptions.DefaultProvider, identity.Tokens.PasswordResetTokenProvider);
        Assert.AreEqual(TimeSpan.FromMinutes(60), reset.TokenLifespan);
        Assert.AreEqual(AccountPasswordResetPolicy.TokenLifespan, reset.TokenLifespan);
        Assert.AreEqual(TimeSpan.FromHours(24), email.TokenLifespan);
    }

    [TestMethod]
    public async Task PreResetSessionAndBearerInvalidatedWhilePostResetWork()
    {
        await using var fixture = await RecoveryFixture.CreateAsync();
        await fixture.CreateAccountAsync(fixture.Email, confirmed: true);
        using (await fixture.CookieSignInAsync(fixture.Email, DefaultPassword)) { }
        using var sessionBefore = await fixture.Client.GetAsync("/api/accounts/session");
        Assert.AreEqual(HttpStatusCode.OK, sessionBefore.StatusCode);
        var pair = await fixture.BearerPairAsync(fixture.Email, DefaultPassword);
        using var meBefore = await fixture.GetMeAsync(pair.Access);
        Assert.AreEqual(HttpStatusCode.OK, meBefore.StatusCode);

        using (await fixture.ForgotAsync(fixture.Email)) { }
        var delivery = fixture.Sender.Deliveries.Single();
        using var reset = await fixture.ResetAsync(delivery.UserId, delivery.Code, "RotatedValidPass66");
        Assert.AreEqual(HttpStatusCode.OK, reset.StatusCode);

        using var sessionAfter = await fixture.Client.GetAsync("/api/accounts/session");
        Assert.AreEqual(HttpStatusCode.Unauthorized, sessionAfter.StatusCode);
        StringAssert.Contains(await sessionAfter.Content.ReadAsStringAsync(), "authenticationRequired");
        using var meAfter = await fixture.GetMeAsync(pair.Access);
        Assert.AreEqual(HttpStatusCode.Unauthorized, meAfter.StatusCode);
        using var refreshAfter = await fixture.PlainClient.PostAsJsonAsync(
            "/api/accounts/bearer-refresh",
            new { refreshToken = pair.Refresh, accessToken = pair.Access });
        Assert.AreEqual(HttpStatusCode.Unauthorized, refreshAfter.StatusCode);
        StringAssert.Contains(await refreshAfter.Content.ReadAsStringAsync(), "authenticationRequired");

        using (await fixture.CookieSignInAsync(fixture.Email, "RotatedValidPass66")) { }
        using var sessionRenewed = await fixture.Client.GetAsync("/api/accounts/session");
        Assert.AreEqual(HttpStatusCode.OK, sessionRenewed.StatusCode);
        var renewed = await fixture.BearerPairAsync(fixture.Email, "RotatedValidPass66");
        using var meRenewed = await fixture.GetMeAsync(renewed.Access);
        Assert.AreEqual(HttpStatusCode.OK, meRenewed.StatusCode);
    }

    [TestMethod]
    public async Task PasswordPolicyAppliesEquallyToKnownAndUnknownAccounts()
    {
        await using var fixture = await RecoveryFixture.CreateAsync();
        await fixture.CreateAccountAsync("known@example.test", confirmed: true);
        using (await fixture.ForgotAsync("known@example.test")) { }
        var delivery = fixture.Sender.Deliveries.Single();
        var unknownCode = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes("unknown-reset-token"));
        var invalidPasswords = new[] { "short", new string('p', 129), string.Empty };

        foreach (var password in invalidPasswords)
        {
            using var known = await fixture.ResetAsync(delivery.UserId, delivery.Code, password);
            var knownBody = await known.Content.ReadAsStringAsync();
            using var unknown = await fixture.ResetAsync("unknown-account", unknownCode, password);
            var unknownBody = await unknown.Content.ReadAsStringAsync();
            Assert.AreEqual(HttpStatusCode.BadRequest, known.StatusCode);
            Assert.AreEqual(HttpStatusCode.BadRequest, unknown.StatusCode);
            var knownErrors = ErrorPayload(knownBody);
            var unknownErrors = ErrorPayload(unknownBody);
            Assert.AreEqual(knownErrors, unknownErrors);
            Assert.AreEqual("invalidRequest", JsonDocument.Parse(knownBody).RootElement.GetProperty("category").GetString());
            Assert.IsFalse(password.Length > 0 && knownBody.Contains(password, StringComparison.Ordinal));
        }

        // A lone surrogate cannot survive PostAsJsonAsync (the client substitutes
        // U+FFFD before sending), so it is delivered as a raw JSON escape instead.
        foreach (var (userId, code) in new[] { (delivery.UserId, delivery.Code), ("unknown-account", unknownCode) })
        {
            using var content = new StringContent(
                $"{{\"userId\":\"{userId}\",\"code\":\"{code}\",\"newPassword\":\"ok-but-\\ud800-broken\"}}",
                Encoding.UTF8,
                "application/json");
            using var response = await fixture.PlainClient.PostAsync("/api/accounts/reset-password", content);
            var body = await response.Content.ReadAsStringAsync();
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
            using var problem = JsonDocument.Parse(body);
            Assert.AreEqual("invalidRequest", problem.RootElement.GetProperty("category").GetString());
            StringAssert.Contains(problem.RootElement.GetProperty("errors").GetRawText(), "newPassword");
        }

        using var unknownValid = await fixture.ResetAsync("unknown-account", unknownCode, "AnotherValidPass99");
        Assert.AreEqual(HttpStatusCode.BadRequest, unknownValid.StatusCode);
        using var unknownValidProblem = JsonDocument.Parse(await unknownValid.Content.ReadAsStringAsync());
        Assert.AreEqual("invalidOrExpiredPasswordReset", unknownValidProblem.RootElement.GetProperty("category").GetString());

        using var intact = await fixture.BearerSignInAsync("known@example.test", DefaultPassword);
        Assert.AreEqual(HttpStatusCode.OK, intact.StatusCode);
    }

    [TestMethod]
    public async Task ExactSpacedAndUnicodePasswordsHashVerbatimAndUnverifiedStaysUnverified()
    {
        await using var fixture = await RecoveryFixture.CreateAsync();
        await fixture.CreateAccountAsync("spaced@example.test", confirmed: true);
        await fixture.CreateAccountAsync("unicode@example.test", confirmed: false);
        using (await fixture.ForgotAsync("spaced@example.test")) { }
        using (await fixture.ForgotAsync("unicode@example.test")) { }
        var spaced = fixture.Sender.Deliveries.First();
        var unicode = fixture.Sender.Deliveries.Last();

        using var spacedReset = await fixture.ResetAsync(spaced.UserId, spaced.Code, SpacedPassword);
        Assert.AreEqual(HttpStatusCode.OK, spacedReset.StatusCode);
        using var exact = await fixture.CookieSignInAsync("spaced@example.test", SpacedPassword);
        Assert.AreEqual(HttpStatusCode.OK, exact.StatusCode);
        using var trimmed = await fixture.BearerSignInAsync("spaced@example.test", SpacedPassword.Trim());
        Assert.AreEqual(HttpStatusCode.Unauthorized, trimmed.StatusCode);

        using var unicodeReset = await fixture.ResetAsync(unicode.UserId, unicode.Code, UnicodePassword);
        Assert.AreEqual(HttpStatusCode.OK, unicodeReset.StatusCode);
        using var unicodeSignIn = await fixture.BearerSignInAsync("unicode@example.test", UnicodePassword);
        Assert.AreEqual(HttpStatusCode.OK, unicodeSignIn.StatusCode);
        StringAssert.Contains(await unicodeSignIn.Content.ReadAsStringAsync(), "verificationRequired");

        using var scope = fixture.Factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        Assert.IsFalse((await users.FindByEmailAsync("unicode@example.test"))!.EmailConfirmed);
    }

    [TestMethod]
    public async Task StrictBoundariesHaveNoRecoveryEffects()
    {
        await using var fixture = await RecoveryFixture.CreateAsync();
        await fixture.CreateAccountAsync(fixture.Email, confirmed: true);
        using (await fixture.ForgotAsync(fixture.Email)) { }
        var delivery = fixture.Sender.Deliveries.Single();

        using var duplicate = new StringContent(
            $"{{\"email\":\"{fixture.Email}\",\"email\":\"{fixture.Email}\"}}",
            Encoding.UTF8,
            "application/json");
        using var duplicateResponse = await fixture.PlainClient.PostAsync("/api/accounts/forgot-password", duplicate);
        Assert.AreEqual(HttpStatusCode.BadRequest, duplicateResponse.StatusCode);
        using var queryResponse = await fixture.PlainClient.PostAsJsonAsync(
            "/api/accounts/forgot-password?email=forbidden",
            new { email = fixture.Email });
        Assert.AreEqual(HttpStatusCode.BadRequest, queryResponse.StatusCode);
        using var mediaResponse = await fixture.PlainClient.PostAsync(
            "/api/accounts/reset-password",
            new StringContent("{}", Encoding.UTF8, "text/plain"));
        Assert.AreEqual(HttpStatusCode.UnsupportedMediaType, mediaResponse.StatusCode);
        using var invalidEncodingContent = new ByteArrayContent([0x7B, 0x22, 0x63, 0x6F, 0x64, 0x65, 0x22, 0x3A, 0x22, 0xC3, 0x28, 0x22, 0x7D]);
        invalidEncodingContent.Headers.ContentType = new("application/json") { CharSet = "utf-8" };
        using var invalidEncodingResponse = await fixture.PlainClient.PostAsync(
            "/api/accounts/reset-password",
            invalidEncodingContent);
        Assert.AreEqual(HttpStatusCode.BadRequest, invalidEncodingResponse.StatusCode);
        using var resetShapeResponse = await fixture.PlainClient.PostAsJsonAsync(
            "/api/accounts/reset-password",
            new { userId = delivery.UserId, code = delivery.Code });
        Assert.AreEqual(HttpStatusCode.BadRequest, resetShapeResponse.StatusCode);
        using var nonStringResponse = await fixture.PlainClient.PostAsJsonAsync(
            "/api/accounts/forgot-password",
            new { email = 42 });
        Assert.AreEqual(HttpStatusCode.BadRequest, nonStringResponse.StatusCode);
        foreach (var response in new[] { duplicateResponse, queryResponse, mediaResponse, invalidEncodingResponse, resetShapeResponse, nonStringResponse })
            Assert.AreEqual("no-store", response.Headers.CacheControl?.ToString());
        using var forgotMethod = await fixture.PlainClient.GetAsync("/api/accounts/forgot-password");
        Assert.AreEqual(HttpStatusCode.MethodNotAllowed, forgotMethod.StatusCode);
        Assert.AreEqual("POST", forgotMethod.Content.Headers.Allow.Single());
        Assert.AreEqual("no-store", forgotMethod.Headers.CacheControl?.ToString());
        using var resetMethod = await fixture.PlainClient.DeleteAsync("/api/accounts/reset-password");
        Assert.AreEqual(HttpStatusCode.MethodNotAllowed, resetMethod.StatusCode);
        Assert.AreEqual("POST", resetMethod.Content.Headers.Allow.Single());
        Assert.AreEqual("no-store", resetMethod.Headers.CacheControl?.ToString());

        Assert.HasCount(1, fixture.Sender.Deliveries);
        using var intact = await fixture.BearerSignInAsync(fixture.Email, DefaultPassword);
        Assert.AreEqual(HttpStatusCode.OK, intact.StatusCode);
    }

    [TestMethod]
    public async Task StorageLossReturnsSanitizedAvailability()
    {
        await using var fixture = await RecoveryFixture.CreateAsync();
        await fixture.CreateAccountAsync(fixture.Email, confirmed: true);
        File.Delete(fixture.Database.DatabasePath);

        using var forgot = await fixture.ForgotAsync(fixture.Email);
        using var reset = await fixture.ResetAsync(
            "opaque-account",
            WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes("opaque-token")),
            "AnotherValidPass99");
        foreach (var response in new[] { forgot, reset })
        {
            var body = await response.Content.ReadAsStringAsync();
            Assert.AreEqual(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            Assert.AreEqual("no-store", response.Headers.CacheControl?.ToString());
            using var problem = JsonDocument.Parse(body);
            Assert.AreEqual("availability", problem.RootElement.GetProperty("category").GetString());
            Assert.IsFalse(body.Contains(fixture.Database.DatabasePath, StringComparison.Ordinal));
            Assert.IsFalse(body.Contains("opaque-token", StringComparison.Ordinal));
            Assert.IsFalse(body.Contains(fixture.Email, StringComparison.Ordinal));
            Assert.IsFalse(body.Contains("AnotherValidPass99", StringComparison.Ordinal));
        }
    }

    [TestMethod]
    public async Task CooldownBoundaryRestartAndControlledConcurrencyAllowOneNewDelivery()
    {
        await using var database = StorageTestDatabase.Create();
        await database.MigrateAsync();
        var keysPath = Directory.CreateDirectory(Path.Combine(database.RootPath, "keys")).FullName;
        var time = new AdjustableTimeProvider(new DateTimeOffset(2026, 9, 10, 12, 0, 0, TimeSpan.Zero));
        var sender = new BlockingResetSender();
        await using (var firstFactory = CreateFactory(database.DatabasePath, keysPath, sender, time))
        {
            await CreateAccountAsync(firstFactory, "cooldown@example.test", confirmed: true);
            using var firstClient = firstFactory.CreateClient();
            using (await firstClient.PostAsJsonAsync("/api/accounts/forgot-password", new { email = "cooldown@example.test" })) { }
            using (await firstClient.PostAsJsonAsync("/api/accounts/forgot-password", new { email = "cooldown@example.test" })) { }
            time.Advance(TimeSpan.FromSeconds(59));
            using (await firstClient.PostAsJsonAsync("/api/accounts/forgot-password", new { email = "cooldown@example.test" })) { }
            Assert.HasCount(1, sender.Deliveries);
        }

        await using var secondFactory = CreateFactory(database.DatabasePath, keysPath, sender, time);
        using var secondClient = secondFactory.CreateClient();
        using (await secondClient.PostAsJsonAsync("/api/accounts/forgot-password", new { email = "cooldown@example.test" })) { }
        Assert.HasCount(1, sender.Deliveries);
        time.Advance(TimeSpan.FromSeconds(1));
        sender.BlockNextDelivery();
        var first = secondClient.PostAsJsonAsync("/api/accounts/forgot-password", new { email = "cooldown@example.test" });
        await sender.WaitUntilBlockedAsync();
        var concurrent = Enumerable.Range(0, 7)
            .Select(_ => secondClient.PostAsJsonAsync("/api/accounts/forgot-password", new { email = "cooldown@example.test" }))
            .ToArray();
        sender.ReleaseBlockedDelivery();
        using (await first) { }
        foreach (var response in await Task.WhenAll(concurrent)) response.Dispose();
        Assert.HasCount(2, sender.Deliveries);

        await using var verification = database.CreateContext();
        var marker = await verification.UserTokens.SingleAsync();
        Assert.AreEqual(AccountPasswordResetCoordinator.CooldownTokenProvider, marker.LoginProvider);
        Assert.AreEqual(AccountPasswordResetCoordinator.CooldownTokenName, marker.Name);
        Assert.AreEqual(time.GetUtcNow().ToString("O"), marker.Value);
    }

    [TestMethod]
    public async Task FailedDeliveryKeepsAcknowledgmentAndRecoversAfterCooldown()
    {
        await using var database = StorageTestDatabase.Create();
        await database.MigrateAsync();
        var keysPath = Directory.CreateDirectory(Path.Combine(database.RootPath, "keys")).FullName;
        var time = new AdjustableTimeProvider(new DateTimeOffset(2026, 9, 10, 13, 0, 0, TimeSpan.Zero));
        var sender = new RecoveringResetSender(failures: 1);
        await using var factory = CreateFactory(database.DatabasePath, keysPath, sender, time);
        await CreateAccountAsync(factory, "recover@example.test", confirmed: true);
        using var client = factory.CreateClient();
        using var failed = await client.PostAsJsonAsync("/api/accounts/forgot-password", new { email = "recover@example.test" });
        var failedBody = await failed.Content.ReadAsStringAsync();
        Assert.AreEqual(HttpStatusCode.Accepted, failed.StatusCode);
        Assert.AreEqual("{\"status\":\"passwordResetRequested\",\"retryAfterSeconds\":60}", failedBody);
        Assert.AreEqual(1, sender.Attempts);
        Assert.HasCount(0, sender.Deliveries);
        using var unknown = await client.PostAsJsonAsync("/api/accounts/forgot-password", new { email = "absent@example.test" });
        Assert.AreEqual(failedBody, await unknown.Content.ReadAsStringAsync());

        time.Advance(TimeSpan.FromSeconds(60));
        using var recovered = await client.PostAsJsonAsync("/api/accounts/forgot-password", new { email = "recover@example.test" });
        Assert.AreEqual(HttpStatusCode.Accepted, recovered.StatusCode);
        var delivery = sender.Deliveries.Single();
        using var reset = await client.PostAsJsonAsync(
            "/api/accounts/reset-password",
            new { userId = delivery.UserId, code = delivery.Code, newPassword = "RecoveredValidPass8" });
        Assert.AreEqual(HttpStatusCode.OK, reset.StatusCode);
    }

    [TestMethod]
    public async Task SecretSentinelsAbsentFromResponsesAndStoredMarkers()
    {
        await using var fixture = await RecoveryFixture.CreateAsync();
        const string sentinelPassword = "Sentinel Mountain 99";
        await fixture.CreateAccountAsync("sentinel@example.test", confirmed: true, password: sentinelPassword);
        using var forgot = await fixture.ForgotAsync("sentinel@example.test");
        var forgotBody = await forgot.Content.ReadAsStringAsync();
        Assert.IsFalse(forgotBody.Contains("sentinel@example.test", StringComparison.Ordinal));
        var delivery = fixture.Sender.Deliveries.Single();
        using var reset = await fixture.ResetAsync(delivery.UserId, delivery.Code, "Sentinel Valley 88");
        var resetBody = await reset.Content.ReadAsStringAsync();
        Assert.IsFalse(resetBody.Contains(delivery.UserId, StringComparison.Ordinal));
        Assert.IsFalse(resetBody.Contains(delivery.Code, StringComparison.Ordinal));
        Assert.IsFalse(resetBody.Contains("Sentinel Valley 88", StringComparison.Ordinal));

        await using var inspection = fixture.Database.CreateContext();
        var account = await inspection.Users.SingleAsync();
        Assert.IsFalse(account.PasswordHash!.Contains(sentinelPassword, StringComparison.Ordinal));
        Assert.IsFalse(account.PasswordHash.Contains("Sentinel Valley 88", StringComparison.Ordinal));
        var marker = await inspection.UserTokens.SingleAsync();
        Assert.IsFalse(marker.Value!.Contains(delivery.Code, StringComparison.Ordinal));
    }

    private static AccountWebApplicationFactory CreateFactory(
        string databasePath,
        string keysPath,
        IAccountPasswordResetSender sender,
        TimeProvider? time = null) => new(
            databasePath, keysPath, new CapturingConfirmationSender(), resetSender: sender, timeProvider: time);

    private static async Task CreateAccountAsync(AccountWebApplicationFactory factory, string email, bool confirmed, string password = DefaultPassword)
    {
        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var result = await users.CreateAsync(
            new IdentityUser { UserName = email, Email = email, EmailConfirmed = confirmed },
            password);
        Assert.IsTrue(result.Succeeded, string.Join(',', result.Errors.Select(error => error.Code)));
    }

    private static string ErrorPayload(string body)
    {
        using var problem = JsonDocument.Parse(body);
        return problem.RootElement.GetProperty("errors").GetRawText();
    }

    private static bool IsBase64UrlCharacter(char character) => character is >= 'A' and <= 'Z'
        or >= 'a' and <= 'z'
        or >= '0' and <= '9'
        or '-'
        or '_';

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

internal sealed class RecoveryFixture : IAsyncDisposable
{
    private RecoveryFixture(string email, StorageTestDatabase database, string keysPath, AdjustableTimeProvider time, CapturingResetSender sender)
    {
        Email = email;
        Database = database;
        Time = time;
        Sender = sender;
        Factory = new AccountWebApplicationFactory(
            database.DatabasePath, keysPath, new CapturingConfirmationSender(), resetSender: sender, timeProvider: time);
        Client = Factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = true, AllowAutoRedirect = false });
        PlainClient = Factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = false, AllowAutoRedirect = false });
    }

    public string Email { get; }
    public StorageTestDatabase Database { get; }
    public AdjustableTimeProvider Time { get; }
    public CapturingResetSender Sender { get; }
    public AccountWebApplicationFactory Factory { get; }
    public HttpClient Client { get; }
    public HttpClient PlainClient { get; }

    public static async Task<RecoveryFixture> CreateAsync(string? email = null)
    {
        var database = StorageTestDatabase.Create();
        await database.MigrateAsync();
        var keysPath = Directory.CreateDirectory(Path.Combine(database.RootPath, "keys")).FullName;
        return new(email ?? "recovery@example.test", database, keysPath,
            new AdjustableTimeProvider(new DateTimeOffset(2026, 9, 10, 8, 0, 0, TimeSpan.Zero)),
            new CapturingResetSender());
    }

    public async Task<IdentityUser> CreateAccountAsync(string email, bool confirmed, string password = "Maple!River2026")
    {
        using var scope = Factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = confirmed };
        var result = await users.CreateAsync(user, password);
        Assert.IsTrue(result.Succeeded, string.Join(',', result.Errors.Select(error => error.Code)));
        return user;
    }

    public Task<HttpResponseMessage> ForgotAsync(string email) =>
        PlainClient.PostAsJsonAsync("/api/accounts/forgot-password", new { email });

    public Task<HttpResponseMessage> ResetAsync(string userId, string code, string newPassword) =>
        PlainClient.PostAsJsonAsync("/api/accounts/reset-password", new { userId, code, newPassword });

    public async Task<HttpResponseMessage> CookieSignInAsync(string email, string password)
    {
        using var bootstrap = await Client.GetAsync("/api/accounts/antiforgery");
        bootstrap.EnsureSuccessStatusCode();
        var token = (await bootstrap.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("requestToken").GetString()!;
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/accounts/sign-in")
        {
            Content = JsonContent.Create(new { email, password }),
        };
        request.Headers.Add("X-LinguaDesk-Antiforgery", token);
        return await Client.SendAsync(request);
    }

    public async Task<HttpResponseMessage> BearerSignInAsync(string email, string password) =>
        await PlainClient.PostAsJsonAsync("/api/accounts/bearer-sign-in", new { email, password });

    public async Task<(string Access, string Refresh)> BearerPairAsync(string email, string password)
    {
        using var response = await BearerSignInAsync(email, password);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return (body.GetProperty("accessToken").GetString()!, body.GetProperty("refreshToken").GetString()!);
    }

    public Task<HttpResponseMessage> GetMeAsync(string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/accounts/me");
        request.Headers.Authorization = new("Bearer", accessToken);
        return PlainClient.SendAsync(request);
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        PlainClient.Dispose();
        await Factory.DisposeAsync();
        await Database.DisposeAsync();
    }
}

internal sealed class CapturingResetSender : IAccountPasswordResetSender
{
    private readonly ConcurrentQueue<AccountPasswordResetDelivery> deliveries = new();

    public IReadOnlyCollection<AccountPasswordResetDelivery> Deliveries => deliveries.ToArray();

    public Task SendAsync(AccountPasswordResetDelivery delivery, CancellationToken cancellationToken)
    {
        deliveries.Enqueue(delivery);
        return Task.CompletedTask;
    }
}

internal sealed class FailingResetSender : IAccountPasswordResetSender
{
    private int attempts;

    public int Attempts => Volatile.Read(ref attempts);

    public Task SendAsync(AccountPasswordResetDelivery delivery, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref attempts);
        throw new AccountPasswordResetDeliveryException();
    }
}

internal sealed class RecoveringResetSender(int failures) : IAccountPasswordResetSender
{
    private readonly ConcurrentQueue<AccountPasswordResetDelivery> deliveries = new();
    private int failuresRemaining = failures;
    private int attempts;

    public int Attempts => Volatile.Read(ref attempts);
    public IReadOnlyCollection<AccountPasswordResetDelivery> Deliveries => deliveries.ToArray();

    public Task SendAsync(AccountPasswordResetDelivery delivery, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref attempts);
        if (Interlocked.Decrement(ref failuresRemaining) >= 0)
        {
            throw new AccountPasswordResetDeliveryException();
        }
        deliveries.Enqueue(delivery);
        return Task.CompletedTask;
    }
}

internal sealed class BlockingResetSender : IAccountPasswordResetSender
{
    private readonly ConcurrentQueue<AccountPasswordResetDelivery> deliveries = new();
    private TaskCompletionSource? blocked;
    private TaskCompletionSource? release;

    public IReadOnlyCollection<AccountPasswordResetDelivery> Deliveries => deliveries.ToArray();

    public void BlockNextDelivery()
    {
        blocked = new(TaskCreationOptions.RunContinuationsAsynchronously);
        release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public Task WaitUntilBlockedAsync() => blocked?.Task ?? Task.CompletedTask;

    public void ReleaseBlockedDelivery() => release?.TrySetResult();

    public async Task SendAsync(AccountPasswordResetDelivery delivery, CancellationToken cancellationToken)
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
