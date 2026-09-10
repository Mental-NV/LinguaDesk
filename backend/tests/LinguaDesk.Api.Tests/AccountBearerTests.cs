using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using LinguaDesk.Api.Features.Identity;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LinguaDesk.Api.Tests;

[TestClass]
public sealed class AccountBearerTests
{
    internal const string Password = " exact password 15 ";

    [TestMethod]
    public async Task ConfirmedAccountBearerSignInIssuesPairAndAuthenticatesMeAsBearer()
    {
        await using var fixture = await BearerFixture.CreateAsync();
        await fixture.CreateAccountAsync(confirmed: true);
        using var signIn = await fixture.PostJsonAsync("/api/accounts/bearer-sign-in",
            new { email = fixture.Email, password = Password });
        Assert.AreEqual(HttpStatusCode.OK, signIn.StatusCode);
        Assert.AreEqual("no-store", signIn.Headers.CacheControl?.ToString());
        Assert.IsFalse(signIn.Headers.Contains("Set-Cookie"));
        var body = await signIn.Content.ReadFromJsonAsync<JsonElement>();
        var access = body.GetProperty("accessToken").GetString()!;
        var refresh = body.GetProperty("refreshToken").GetString()!;
        Assert.IsGreaterThan(20, access.Length);
        Assert.IsGreaterThan(20, refresh.Length);
        Assert.AreNotEqual(access, refresh);
        Assert.AreEqual(900L, body.GetProperty("expiresIn").GetInt64());
        Assert.AreEqual("Bearer", body.GetProperty("tokenType").GetString());
        Assert.AreEqual("verified", body.GetProperty("verificationStatus").GetString());
        Assert.AreEqual(fixture.Time.GetUtcNow().AddMinutes(15), await fixture.AccessExpiryAsync(access));
        Assert.AreEqual(fixture.Time.GetUtcNow().AddDays(7), await fixture.RefreshExpiryAsync(refresh));

        using var me = await fixture.GetMeAsync(access);
        Assert.AreEqual(HttpStatusCode.OK, me.StatusCode);
        var meBody = await me.Content.ReadFromJsonAsync<JsonElement>();
        Assert.AreEqual(fixture.Email, meBody.GetProperty("email").GetString());
        Assert.AreEqual("verified", meBody.GetProperty("verificationStatus").GetString());
        Assert.AreEqual("bearer", meBody.GetProperty("authMode").GetString());
    }

    [TestMethod]
    public async Task UnverifiedAccountReceivesPairButFreshVerifiedPolicyDeniesIt()
    {
        await using var fixture = await BearerFixture.CreateAsync();
        var user = await fixture.CreateAccountAsync(confirmed: false);
        using var signIn = await fixture.PostJsonAsync("/api/accounts/bearer-sign-in",
            new { email = fixture.Email, password = Password });
        Assert.AreEqual(HttpStatusCode.OK, signIn.StatusCode);
        var body = await signIn.Content.ReadFromJsonAsync<JsonElement>();
        Assert.AreEqual("verificationRequired", body.GetProperty("verificationStatus").GetString());
        var access = body.GetProperty("accessToken").GetString()!;
        using var me = await fixture.GetMeAsync(access);
        Assert.AreEqual(HttpStatusCode.OK, me.StatusCode);
        StringAssert.Contains(await me.Content.ReadAsStringAsync(), "verificationRequired");
        using var scope = fixture.Factory.Services.CreateScope();
        var authorization = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, user.Id)], "test"));
        Assert.IsFalse((await authorization.AuthorizeAsync(principal, VerifiedAccountAuthorization.PolicyName)).Succeeded);
    }

    [TestMethod]
    public async Task UnknownAndWrongPasswordsAreEquivalentWithNoTokenOrDetail()
    {
        await using var fixture = await BearerFixture.CreateAsync();
        await fixture.CreateAccountAsync(confirmed: true);
        using var wrong = await fixture.PostJsonAsync("/api/accounts/bearer-sign-in",
            new { email = fixture.Email, password = "wrong password 15" });
        using var unknown = await fixture.PostJsonAsync("/api/accounts/bearer-sign-in",
            new { email = "absent@example.test", password = "wrong password 15" });
        using var trimmed = await fixture.PostJsonAsync("/api/accounts/bearer-sign-in",
            new { email = fixture.Email, password = Password.Trim() });
        Assert.AreEqual(HttpStatusCode.Unauthorized, wrong.StatusCode);
        Assert.AreEqual(HttpStatusCode.Unauthorized, unknown.StatusCode);
        Assert.AreEqual(HttpStatusCode.Unauthorized, trimmed.StatusCode);
        var wrongBody = await wrong.Content.ReadFromJsonAsync<JsonElement>();
        var unknownBody = await unknown.Content.ReadFromJsonAsync<JsonElement>();
        Assert.AreEqual("invalidCredentials", wrongBody.GetProperty("category").GetString());
        Assert.AreEqual(wrongBody.GetProperty("category").GetString(), unknownBody.GetProperty("category").GetString());
        Assert.AreEqual(wrongBody.GetProperty("detail").GetString(), unknownBody.GetProperty("detail").GetString());
        foreach (var response in new[] { wrong, unknown, trimmed })
        {
            Assert.AreEqual("no-store", response.Headers.CacheControl?.ToString());
            Assert.IsFalse(response.Headers.Contains("Set-Cookie"));
            var raw = await response.Content.ReadAsStringAsync();
            Assert.IsFalse(raw.Contains(fixture.Email, StringComparison.Ordinal));
            Assert.IsFalse(raw.Contains(Password, StringComparison.Ordinal));
        }
    }

    [TestMethod]
    public async Task ValidPairRefreshesIntoReplacementThatAuthenticatesMe()
    {
        await using var fixture = await BearerFixture.CreateAsync();
        await fixture.CreateAccountAsync(confirmed: true);
        var pair = await fixture.SignInPairAsync();
        var before = fixture.Time.GetUtcNow();
        fixture.Time.Advance(TimeSpan.FromMinutes(1));
        using var refresh = await fixture.PostJsonAsync("/api/accounts/bearer-refresh",
            new { refreshToken = pair.Refresh, accessToken = pair.Access });
        Assert.AreEqual(HttpStatusCode.OK, refresh.StatusCode);
        Assert.IsFalse(refresh.Headers.Contains("Set-Cookie"));
        var body = await refresh.Content.ReadFromJsonAsync<JsonElement>();
        var newAccess = body.GetProperty("accessToken").GetString()!;
        var newRefresh = body.GetProperty("refreshToken").GetString()!;
        Assert.IsFalse(string.IsNullOrEmpty(newAccess));
        Assert.IsFalse(string.IsNullOrEmpty(newRefresh));
        Assert.AreEqual(900L, body.GetProperty("expiresIn").GetInt64());
        Assert.AreEqual("Bearer", body.GetProperty("tokenType").GetString());
        Assert.AreEqual(fixture.Time.GetUtcNow().AddMinutes(15), await fixture.AccessExpiryAsync(newAccess));
        Assert.AreEqual(fixture.Time.GetUtcNow().AddDays(7), await fixture.RefreshExpiryAsync(newRefresh));
        Assert.IsTrue(fixture.Time.GetUtcNow() > before);
        using var me = await fixture.GetMeAsync(newAccess);
        Assert.AreEqual(HttpStatusCode.OK, me.StatusCode);
    }

    [TestMethod]
    public async Task InvalidExpiredAndCrossAccountRefreshAreSecretFreeAuthenticationRequired()
    {
        await using var fixture = await BearerFixture.CreateAsync();
        await fixture.CreateAccountAsync(confirmed: true);
        var pair = await fixture.SignInPairAsync();
        using var malformed = await fixture.PostJsonAsync("/api/accounts/bearer-refresh",
            new { refreshToken = "not-a-token", accessToken = pair.Access });
        fixture.Time.Advance(TimeSpan.FromDays(7));
        using var expired = await fixture.PostJsonAsync("/api/accounts/bearer-refresh",
            new { refreshToken = pair.Refresh, accessToken = pair.Access });
        foreach (var response in new[] { malformed, expired })
        {
            Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
            StringAssert.Contains(await response.Content.ReadAsStringAsync(), "authenticationRequired");
            Assert.IsFalse(response.Headers.Contains("Set-Cookie"));
        }

        await using var other = await BearerFixture.CreateAsync("other@example.test");
        await other.CreateAccountAsync(confirmed: true);
        var otherPair = await other.SignInPairAsync();
        using var crossed = await fixture.PostJsonAsync("/api/accounts/bearer-refresh",
            new { refreshToken = otherPair.Refresh, accessToken = pair.Access });
        Assert.AreEqual(HttpStatusCode.Unauthorized, crossed.StatusCode);
        StringAssert.Contains(await crossed.Content.ReadAsStringAsync(), "authenticationRequired");
    }

    [TestMethod]
    public async Task SchemeSelectionHoldsBothWaysWithoutAntiforgery()
    {
        await using var fixture = await BearerFixture.CreateAsync();
        await fixture.CreateAccountAsync(confirmed: true);
        using (await fixture.CookieSignInAsync()) { }
        var pair = await fixture.SignInPairAsync(withoutCookies: true);

        using var invalidBearer = new HttpRequestMessage(HttpMethod.Get, "/api/accounts/me");
        invalidBearer.Headers.Authorization = new("Bearer", "unsupported-opaque-value");
        using var noFallback = await fixture.Client.SendAsync(invalidBearer);
        Assert.AreEqual(HttpStatusCode.Unauthorized, noFallback.StatusCode);
        StringAssert.Contains(await noFallback.Content.ReadAsStringAsync(), "authenticationRequired");

        using var bearerOnly = await fixture.GetMeAsync(pair.Access, withoutCookies: true);
        Assert.AreEqual(HttpStatusCode.OK, bearerOnly.StatusCode);
        StringAssert.Contains(await bearerOnly.Content.ReadAsStringAsync(), "\"authMode\":\"bearer\"");

        using var cookieOnly = await fixture.Client.GetAsync("/api/accounts/me");
        Assert.AreEqual(HttpStatusCode.OK, cookieOnly.StatusCode);
        StringAssert.Contains(await cookieOnly.Content.ReadAsStringAsync(), "\"authMode\":\"cookie\"");
    }

    [TestMethod]
    public async Task AccessExpiresAtFifteenMinutesAndRefreshAtSevenDays()
    {
        await using var fixture = await BearerFixture.CreateAsync();
        await fixture.CreateAccountAsync(confirmed: true);
        var pair = await fixture.SignInPairAsync();
        fixture.Time.Advance(TimeSpan.FromMinutes(15) - TimeSpan.FromTicks(1));
        using var beforeAccess = await fixture.GetMeAsync(pair.Access);
        Assert.AreEqual(HttpStatusCode.OK, beforeAccess.StatusCode);
        fixture.Time.Advance(TimeSpan.FromTicks(1));
        using var atAccess = await fixture.GetMeAsync(pair.Access);
        Assert.AreEqual(HttpStatusCode.Unauthorized, atAccess.StatusCode);

        fixture.Time.Advance(TimeSpan.FromDays(7) - TimeSpan.FromMinutes(15) - TimeSpan.FromTicks(1));
        using var beforeRefresh = await fixture.PostJsonAsync("/api/accounts/bearer-refresh",
            new { refreshToken = pair.Refresh, accessToken = pair.Access });
        Assert.AreEqual(HttpStatusCode.OK, beforeRefresh.StatusCode);
        fixture.Time.Advance(TimeSpan.FromTicks(1));
        using var atRefresh = await fixture.PostJsonAsync("/api/accounts/bearer-refresh",
            new { refreshToken = pair.Refresh, accessToken = pair.Access });
        Assert.AreEqual(HttpStatusCode.Unauthorized, atRefresh.StatusCode);
    }

    [TestMethod]
    public async Task StampChangeAndDeletionInvalidateAccessAndRefresh()
    {
        await AssertBearerMutationInvalidatesAsync(delete: false);
        await AssertBearerMutationInvalidatesAsync(delete: true);
    }

    [TestMethod]
    public async Task StrictQueryJsonMediaEncodingAndMethodBoundariesAreNoStore()
    {
        await using var fixture = await BearerFixture.CreateAsync();
        await fixture.CreateAccountAsync(confirmed: true);
        var pair = await fixture.SignInPairAsync();
        using var query = await fixture.PostJsonAsync("/api/accounts/bearer-sign-in?mode=forbidden",
            new { email = fixture.Email, password = Password });
        using var duplicateContent = new StringContent(
            $"{{\"email\":\"{fixture.Email}\",\"email\":\"{fixture.Email}\",\"password\":\"{Password}\"}}",
            Encoding.UTF8, "application/json");
        using var duplicate = await fixture.SendRawAsync("/api/accounts/bearer-sign-in", duplicateContent);
        using var unknownShape = await fixture.PostJsonAsync("/api/accounts/bearer-sign-in",
            new { email = fixture.Email, password = Password, unexpected = true });
        using var media = await fixture.SendRawAsync("/api/accounts/bearer-sign-in",
            new StringContent("{}", Encoding.UTF8, "text/plain"));
        using var invalidEncodingContent = new ByteArrayContent([0x7B, 0x22, 0x65, 0x6D, 0x61, 0x69, 0x6C, 0x22, 0x3A, 0x22, 0xC3, 0x28, 0x22, 0x7D]);
        invalidEncodingContent.Headers.ContentType = new("application/json") { CharSet = "utf-8" };
        using var invalidEncoding = await fixture.SendRawAsync("/api/accounts/bearer-sign-in", invalidEncodingContent);
        using var meQuery = new HttpRequestMessage(HttpMethod.Get, "/api/accounts/me?mode=forbidden");
        meQuery.Headers.Authorization = new("Bearer", pair.Access);
        using var meQueryResponse = await fixture.PlainClient.SendAsync(meQuery);
        using var refreshMethod = await fixture.PlainClient.DeleteAsync("/api/accounts/bearer-refresh");
        using var meMethod = await fixture.PlainClient.PostAsync("/api/accounts/me", content: null);
        using var refreshUnknown = await fixture.PostJsonAsync("/api/accounts/bearer-refresh",
            new { refreshToken = pair.Refresh, accessToken = pair.Access, unexpected = true });
        using var refreshMissing = await fixture.PostJsonAsync("/api/accounts/bearer-refresh",
            new { refreshToken = pair.Refresh });
        Assert.AreEqual(HttpStatusCode.BadRequest, query.StatusCode);
        Assert.AreEqual(HttpStatusCode.BadRequest, duplicate.StatusCode);
        Assert.AreEqual(HttpStatusCode.BadRequest, unknownShape.StatusCode);
        Assert.AreEqual(HttpStatusCode.UnsupportedMediaType, media.StatusCode);
        Assert.AreEqual(HttpStatusCode.BadRequest, invalidEncoding.StatusCode);
        Assert.AreEqual(HttpStatusCode.BadRequest, meQueryResponse.StatusCode);
        Assert.AreEqual(HttpStatusCode.MethodNotAllowed, refreshMethod.StatusCode);
        Assert.AreEqual(HttpMethod.Post.Method, refreshMethod.Content.Headers.Allow.Single());
        Assert.AreEqual(HttpStatusCode.MethodNotAllowed, meMethod.StatusCode);
        Assert.AreEqual(HttpMethod.Get.Method, meMethod.Content.Headers.Allow.Single());
        Assert.AreEqual(HttpStatusCode.BadRequest, refreshUnknown.StatusCode);
        Assert.AreEqual(HttpStatusCode.BadRequest, refreshMissing.StatusCode);
        foreach (var response in new[] { query, duplicate, unknownShape, media, invalidEncoding, meQueryResponse, refreshMethod, meMethod, refreshUnknown, refreshMissing })
            Assert.AreEqual("no-store", response.Headers.CacheControl?.ToString());
    }

    [TestMethod]
    public async Task MissingStorageReturnsSanitizedAvailabilityWithoutCredentials()
    {
        await using var fixture = await BearerFixture.CreateAsync();
        await fixture.CreateAccountAsync(confirmed: true);
        var pair = await fixture.SignInPairAsync();
        File.Delete(fixture.Database.DatabasePath);
        using var me = await fixture.GetMeAsync(pair.Access);
        Assert.AreEqual(HttpStatusCode.ServiceUnavailable, me.StatusCode);
        StringAssert.Contains(await me.Content.ReadAsStringAsync(), "availability");

        await using var signInFixture = await BearerFixture.CreateAsync("signin@example.test");
        await signInFixture.CreateAccountAsync(confirmed: true);
        File.Delete(signInFixture.Database.DatabasePath);
        using var response = await signInFixture.PostJsonAsync("/api/accounts/bearer-sign-in",
            new { email = signInFixture.Email, password = Password });
        Assert.AreEqual(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.IsFalse(response.Headers.Contains("Set-Cookie"));
        var body = await response.Content.ReadAsStringAsync();
        StringAssert.Contains(body, "availability");
        Assert.IsFalse(body.Contains(signInFixture.Email, StringComparison.Ordinal));
        Assert.IsFalse(body.Contains(Password, StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task SecretSentinelsAreAbsentFromResponsesAndLogs()
    {
        await using var fixture = await BearerFixture.CreateAsync();
        await fixture.CreateAccountAsync(confirmed: true);
        var pair = await fixture.SignInPairAsync();
        using var me = await fixture.GetMeAsync(pair.Access);
        var meBody = await me.Content.ReadAsStringAsync();
        Assert.IsFalse(meBody.Contains(pair.Access, StringComparison.Ordinal));
        Assert.IsFalse(meBody.Contains(pair.Refresh, StringComparison.Ordinal));
        using var badRefresh = await fixture.PostJsonAsync("/api/accounts/bearer-refresh",
            new { refreshToken = "not-a-token", accessToken = pair.Access });
        var badBody = await badRefresh.Content.ReadAsStringAsync();
        Assert.IsFalse(badBody.Contains(pair.Access, StringComparison.Ordinal));
        Assert.IsFalse(badBody.Contains(pair.Refresh, StringComparison.Ordinal));
        Assert.IsFalse(badBody.Contains("not-a-token", StringComparison.Ordinal));
    }

    private static async Task AssertBearerMutationInvalidatesAsync(bool delete)
    {
        await using var fixture = await BearerFixture.CreateAsync();
        var user = await fixture.CreateAccountAsync(confirmed: true);
        var pair = await fixture.SignInPairAsync();
        using (var scope = fixture.Factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var current = (await users.FindByIdAsync(user.Id))!;
            var result = delete ? await users.DeleteAsync(current) : await users.UpdateSecurityStampAsync(current);
            Assert.IsTrue(result.Succeeded);
        }
        using var me = await fixture.GetMeAsync(pair.Access);
        Assert.AreEqual(HttpStatusCode.Unauthorized, me.StatusCode);
        using var refresh = await fixture.PostJsonAsync("/api/accounts/bearer-refresh",
            new { refreshToken = pair.Refresh, accessToken = pair.Access });
        Assert.AreEqual(HttpStatusCode.Unauthorized, refresh.StatusCode);
        StringAssert.Contains(await refresh.Content.ReadAsStringAsync(), "authenticationRequired");
    }
}

internal sealed class BearerFixture : IAsyncDisposable
{
    private BearerFixture(string email, StorageTestDatabase database, string keysPath, AdjustableTimeProvider time)
    {
        Email = email;
        Database = database;
        Time = time;
        Factory = new AccountWebApplicationFactory(database.DatabasePath, keysPath, new CapturingConfirmationSender(), timeProvider: time);
        Client = Factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = true, AllowAutoRedirect = false });
        PlainClient = Factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = false, AllowAutoRedirect = false });
    }

    public string Email { get; }
    public StorageTestDatabase Database { get; }
    public AdjustableTimeProvider Time { get; }
    public AccountWebApplicationFactory Factory { get; }
    public HttpClient Client { get; }
    public HttpClient PlainClient { get; }

    public static async Task<BearerFixture> CreateAsync(string? email = null)
    {
        var database = StorageTestDatabase.Create();
        await database.MigrateAsync();
        var keysPath = Directory.CreateDirectory(Path.Combine(database.RootPath, "keys")).FullName;
        return new(email ?? "bearer@example.test", database, keysPath,
            new AdjustableTimeProvider(new DateTimeOffset(2026, 9, 10, 8, 0, 0, TimeSpan.Zero)));
    }

    public async Task<IdentityUser> CreateAccountAsync(bool confirmed)
    {
        using var scope = Factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var user = new IdentityUser { UserName = Email, Email = Email, EmailConfirmed = confirmed };
        var result = await users.CreateAsync(user, AccountBearerTests.Password);
        Assert.IsTrue(result.Succeeded, string.Join(',', result.Errors.Select(error => error.Code)));
        return user;
    }

    public Task<HttpResponseMessage> PostJsonAsync(string path, object body)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(body) };
        return PlainClient.SendAsync(request);
    }

    public Task<HttpResponseMessage> SendRawAsync(string path, HttpContent content)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = content };
        return PlainClient.SendAsync(request);
    }

    public Task<HttpResponseMessage> GetMeAsync(string accessToken, bool withoutCookies = true)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/accounts/me");
        request.Headers.Authorization = new("Bearer", accessToken);
        return (withoutCookies ? PlainClient : Client).SendAsync(request);
    }

    public async Task<(string Access, string Refresh)> SignInPairAsync(bool withoutCookies = false)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/accounts/bearer-sign-in")
        {
            Content = JsonContent.Create(new { email = Email, password = AccountBearerTests.Password }),
        };
        using var response = await (withoutCookies ? PlainClient : Client).SendAsync(request);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return (body.GetProperty("accessToken").GetString()!, body.GetProperty("refreshToken").GetString()!);
    }

    public async Task<HttpResponseMessage> CookieSignInAsync()
    {
        using var bootstrap = await Client.GetAsync("/api/accounts/antiforgery");
        bootstrap.EnsureSuccessStatusCode();
        var token = (await bootstrap.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("requestToken").GetString()!;
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/accounts/sign-in")
        {
            Content = JsonContent.Create(new { email = Email, password = AccountBearerTests.Password }),
        };
        request.Headers.Add("X-LinguaDesk-Antiforgery", token);
        return await Client.SendAsync(request);
    }

    public async Task<DateTimeOffset> AccessExpiryAsync(string accessToken)
    {
        using var scope = Factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<IOptionsMonitor<BearerTokenOptions>>();
        var ticket = options.Get(IdentityConstants.BearerScheme).BearerTokenProtector.Unprotect(accessToken);
        return ticket!.Properties!.ExpiresUtc!.Value;
    }

    public async Task<DateTimeOffset> RefreshExpiryAsync(string refreshToken)
    {
        using var scope = Factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<IOptionsMonitor<BearerTokenOptions>>();
        var ticket = options.Get(IdentityConstants.BearerScheme).RefreshTokenProtector.Unprotect(refreshToken);
        return ticket!.Properties!.ExpiresUtc!.Value;
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        PlainClient.Dispose();
        await Factory.DisposeAsync();
        await Database.DisposeAsync();
    }
}
