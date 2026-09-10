using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using LinguaDesk.Api.Features.Identity;
using LinguaDesk.Api.Features.Identity.Registration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace LinguaDesk.Api.Tests;

[TestClass]
public sealed class AccountSessionTests
{
    internal const string Password = " exact password 15 ";

    [TestMethod]
    public async Task BootstrapUsesOnlyTheStrictSecureHostCookieAndNoStoreBody()
    {
        await using var fixture = await SessionFixture.CreateAsync();
        using var response = await fixture.Client.GetAsync("/api/accounts/antiforgery");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        StringAssert.Contains(response.Headers.CacheControl?.ToString(), "no-store");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.AreEqual("X-LinguaDesk-Antiforgery", body.GetProperty("headerName").GetString());
        Assert.IsGreaterThan(20, body.GetProperty("requestToken").GetString()!.Length);
        var cookies = response.Headers.GetValues("Set-Cookie").ToArray();
        Assert.HasCount(1, cookies);
        StringAssert.StartsWith(cookies[0], "__Host-LinguaDesk.Antiforgery=");
        StringAssert.Contains(cookies[0], "path=/", StringComparison.OrdinalIgnoreCase);
        StringAssert.Contains(cookies[0], "secure", StringComparison.OrdinalIgnoreCase);
        StringAssert.Contains(cookies[0], "httponly", StringComparison.OrdinalIgnoreCase);
        StringAssert.Contains(cookies[0], "samesite=strict", StringComparison.OrdinalIgnoreCase);
    }

    [TestMethod]
    public async Task ConfirmedAccountSignsInAndReadsFixedNonpersistentSession()
    {
        await using var fixture = await SessionFixture.CreateAsync();
        await fixture.CreateAccountAsync(confirmed: true);
        var token = await fixture.BootstrapAsync();
        using var signIn = await fixture.PostAsync("/api/accounts/sign-in", new { email = fixture.Email, password = Password }, token);
        Assert.AreEqual(HttpStatusCode.OK, signIn.StatusCode);
        var signInBody = await signIn.Content.ReadFromJsonAsync<JsonElement>();
        Assert.AreEqual("signedIn", signInBody.GetProperty("status").GetString());
        Assert.AreEqual("verified", signInBody.GetProperty("verificationStatus").GetString());
        Assert.AreEqual(fixture.Time.GetUtcNow().AddHours(8), signInBody.GetProperty("expiresAtUtc").GetDateTimeOffset());
        var sessionCookie = signIn.Headers.GetValues("Set-Cookie").Single(value => value.StartsWith("__Host-LinguaDesk.Session=", StringComparison.Ordinal));
        StringAssert.Contains(sessionCookie, "path=/", StringComparison.OrdinalIgnoreCase);
        StringAssert.Contains(sessionCookie, "secure", StringComparison.OrdinalIgnoreCase);
        StringAssert.Contains(sessionCookie, "httponly", StringComparison.OrdinalIgnoreCase);
        StringAssert.Contains(sessionCookie, "samesite=lax", StringComparison.OrdinalIgnoreCase);
        Assert.IsFalse(sessionCookie.Contains("expires=", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(sessionCookie.Contains("max-age", StringComparison.OrdinalIgnoreCase));

        using var session = await fixture.Client.GetAsync("/api/accounts/session");
        Assert.AreEqual(HttpStatusCode.OK, session.StatusCode);
        Assert.IsFalse(session.Headers.Contains("Set-Cookie"));
        Assert.AreEqual(await signIn.Content.ReadAsStringAsync(), await session.Content.ReadAsStringAsync());
    }

    [TestMethod]
    public async Task UnverifiedAccountSignsInButFreshVerifiedPolicyDeniesIt()
    {
        await using var fixture = await SessionFixture.CreateAsync();
        var user = await fixture.CreateAccountAsync(confirmed: false);
        using var signIn = await fixture.SignInAsync();
        Assert.AreEqual(HttpStatusCode.OK, signIn.StatusCode);
        StringAssert.Contains(await signIn.Content.ReadAsStringAsync(), "verificationRequired");
        using var scope = fixture.Factory.Services.CreateScope();
        var authorization = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, user.Id)], "test"));
        Assert.IsFalse((await authorization.AuthorizeAsync(principal, VerifiedAccountAuthorization.PolicyName)).Succeeded);
    }

    [TestMethod]
    public async Task UnknownAndWrongPasswordsAreEquivalentAndIssueNoCookie()
    {
        await using var fixture = await SessionFixture.CreateAsync();
        await fixture.CreateAccountAsync(confirmed: true);
        var firstToken = await fixture.BootstrapAsync();
        using var wrong = await fixture.PostAsync("/api/accounts/sign-in", new { email = fixture.Email, password = "wrong password 15" }, firstToken);
        var secondToken = await fixture.BootstrapAsync();
        using var unknown = await fixture.PostAsync("/api/accounts/sign-in", new { email = "absent@example.test", password = "wrong password 15" }, secondToken);
        var thirdToken = await fixture.BootstrapAsync();
        using var trimmed = await fixture.PostAsync("/api/accounts/sign-in", new { email = fixture.Email, password = Password.Trim() }, thirdToken);
        Assert.AreEqual(HttpStatusCode.Unauthorized, wrong.StatusCode);
        Assert.AreEqual(HttpStatusCode.Unauthorized, trimmed.StatusCode);
        var wrongBody = await wrong.Content.ReadFromJsonAsync<JsonElement>();
        var unknownBody = await unknown.Content.ReadFromJsonAsync<JsonElement>();
        Assert.AreEqual(wrongBody.GetProperty("category").GetString(), unknownBody.GetProperty("category").GetString());
        Assert.AreEqual(wrongBody.GetProperty("detail").GetString(), unknownBody.GetProperty("detail").GetString());
        Assert.IsFalse(wrong.Headers.Contains("Set-Cookie"));
        Assert.IsFalse(unknown.Headers.Contains("Set-Cookie"));
        Assert.IsFalse(trimmed.Headers.Contains("Set-Cookie"));
        StringAssert.Contains(await wrong.Content.ReadAsStringAsync(), "invalidCredentials");
    }

    [TestMethod]
    public async Task MissingMismatchedAndIdentityStaleAntiforgeryPairsAreEquivalent()
    {
        await using var fixture = await SessionFixture.CreateAsync();
        await fixture.CreateAccountAsync(confirmed: true);
        using var missing = await fixture.PostAsync("/api/accounts/sign-in", new { email = fixture.Email, password = Password }, null);
        var token = await fixture.BootstrapAsync();
        using var mismatched = await fixture.PostAsync("/api/accounts/sign-in", new { email = fixture.Email, password = Password }, token + "x");
        token = await fixture.BootstrapAsync();
        using var success = await fixture.PostAsync("/api/accounts/sign-in", new { email = fixture.Email, password = Password }, token);
        Assert.AreEqual(HttpStatusCode.OK, success.StatusCode);
        using var stale = await fixture.PostAsync("/api/accounts/sign-out", null, token);
        foreach (var failure in new[] { missing, mismatched, stale })
        {
            Assert.AreEqual(HttpStatusCode.BadRequest, failure.StatusCode);
            Assert.AreEqual("no-store", failure.Headers.CacheControl?.ToString());
            StringAssert.Contains(await failure.Content.ReadAsStringAsync(), "invalidAntiforgery");
            Assert.IsFalse(failure.Headers.Contains("Set-Cookie"));
        }
    }

    [TestMethod]
    public async Task FixedTicketExpiresAtEightHoursWithoutRenewal()
    {
        await using var fixture = await SessionFixture.CreateAsync();
        await fixture.CreateAccountAsync(confirmed: true);
        using (await fixture.SignInAsync()) { }
        fixture.Time.Advance(TimeSpan.FromHours(8) - TimeSpan.FromTicks(1));
        using var before = await fixture.Client.GetAsync("/api/accounts/session");
        Assert.AreEqual(HttpStatusCode.OK, before.StatusCode);
        Assert.IsFalse(before.Headers.Contains("Set-Cookie"));
        fixture.Time.Advance(TimeSpan.FromTicks(1));
        using var at = await fixture.Client.GetAsync("/api/accounts/session");
        Assert.AreEqual(HttpStatusCode.Unauthorized, at.StatusCode);
        StringAssert.Contains(await at.Content.ReadAsStringAsync(), "authenticationRequired");
    }

    [TestMethod]
    public async Task StampChangeAndDeletionInvalidateTheNextSession()
    {
        await AssertMutationInvalidatesAsync(delete: false);
        await AssertMutationInvalidatesAsync(delete: true);
    }

    [TestMethod]
    public async Task BearerHeaderNeverFallsBackToAmbientCookie()
    {
        await using var fixture = await SessionFixture.CreateAsync();
        await fixture.CreateAccountAsync(confirmed: true);
        using (await fixture.SignInAsync()) { }
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/accounts/session");
        request.Headers.Authorization = new("Bearer", "unsupported-opaque-value");
        using var response = await fixture.Client.SendAsync(request);
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        StringAssert.Contains(await response.Content.ReadAsStringAsync(), "authenticationRequired");
    }

    [TestMethod]
    public async Task SignOutClearsOnlyCallerCookieAndIsIdempotentWithoutStampChange()
    {
        await using var fixture = await SessionFixture.CreateAsync();
        var user = await fixture.CreateAccountAsync(confirmed: true);
        using (await fixture.SignInAsync()) { }
        using var secondClient = fixture.CreateClient();
        var secondToken = await fixture.BootstrapAsync(secondClient);
        using var secondSignIn = await fixture.PostAsync(
            "/api/accounts/sign-in", new { email = fixture.Email, password = Password }, secondToken, secondClient);
        Assert.AreEqual(HttpStatusCode.OK, secondSignIn.StatusCode);
        var token = await fixture.BootstrapAsync();
        using var response = await fixture.PostAsync("/api/accounts/sign-out", null, token);
        Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);
        var expired = response.Headers.GetValues("Set-Cookie").Single(value => value.StartsWith("__Host-LinguaDesk.Session=", StringComparison.Ordinal));
        StringAssert.Contains(expired, "expires=", StringComparison.OrdinalIgnoreCase);
        using var session = await fixture.Client.GetAsync("/api/accounts/session");
        Assert.AreEqual(HttpStatusCode.Unauthorized, session.StatusCode);
        using var secondSession = await secondClient.GetAsync("/api/accounts/session");
        Assert.AreEqual(HttpStatusCode.OK, secondSession.StatusCode);
        token = await fixture.BootstrapAsync();
        using var repeated = await fixture.PostAsync("/api/accounts/sign-out", null, token);
        Assert.AreEqual(HttpStatusCode.NoContent, repeated.StatusCode);
        using var scope = fixture.Factory.Services.CreateScope();
        Assert.AreEqual(user.SecurityStamp, (await scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>().FindByIdAsync(user.Id))!.SecurityStamp);
    }

    [TestMethod]
    public async Task StrictQueryJsonMediaEncodingAndMethodBoundariesAreNoStore()
    {
        await using var fixture = await SessionFixture.CreateAsync();
        await fixture.CreateAccountAsync(confirmed: true);
        var token = await fixture.BootstrapAsync();
        using var query = await fixture.PostAsync("/api/accounts/sign-in?mode=forbidden", new { email = fixture.Email, password = Password }, token);
        token = await fixture.BootstrapAsync();
        using var duplicateContent = new StringContent($"{{\"email\":\"{fixture.Email}\",\"email\":\"{fixture.Email}\",\"password\":\"{Password}\"}}", Encoding.UTF8, "application/json");
        using var duplicate = await fixture.SendAsync("/api/accounts/sign-in", duplicateContent, token);
        token = await fixture.BootstrapAsync();
        using var unknownShape = await fixture.PostAsync("/api/accounts/sign-in", new { email = fixture.Email, password = Password, unexpected = true }, token);
        token = await fixture.BootstrapAsync();
        using var media = await fixture.SendAsync("/api/accounts/sign-in", new StringContent("{}", Encoding.UTF8, "text/plain"), token);
        token = await fixture.BootstrapAsync();
        using var invalidEncodingContent = new ByteArrayContent([0x7B, 0x22, 0x65, 0x6D, 0x61, 0x69, 0x6C, 0x22, 0x3A, 0x22, 0xC3, 0x28, 0x22, 0x7D]);
        invalidEncodingContent.Headers.ContentType = new("application/json") { CharSet = "utf-8" };
        using var invalidEncoding = await fixture.SendAsync("/api/accounts/sign-in", invalidEncodingContent, token);
        using var method = await fixture.Client.DeleteAsync("/api/accounts/session");
        Assert.AreEqual(HttpStatusCode.BadRequest, query.StatusCode);
        Assert.AreEqual(HttpStatusCode.BadRequest, duplicate.StatusCode);
        Assert.AreEqual(HttpStatusCode.BadRequest, unknownShape.StatusCode);
        Assert.AreEqual(HttpStatusCode.UnsupportedMediaType, media.StatusCode);
        Assert.AreEqual(HttpStatusCode.BadRequest, invalidEncoding.StatusCode);
        Assert.AreEqual(HttpStatusCode.MethodNotAllowed, method.StatusCode);
        Assert.AreEqual("GET", method.Content.Headers.Allow.Single());
        foreach (var response in new[] { query, duplicate, unknownShape, media, invalidEncoding, method })
            Assert.AreEqual("no-store", response.Headers.CacheControl?.ToString());
    }

    [TestMethod]
    public async Task MissingStorageReturnsSanitizedAvailabilityWithoutSessionCookie()
    {
        await using var fixture = await SessionFixture.CreateAsync();
        await fixture.CreateAccountAsync(confirmed: true);
        using (await fixture.SignInAsync()) { }
        File.Delete(fixture.Database.DatabasePath);
        using var session = await fixture.Client.GetAsync("/api/accounts/session");
        Assert.AreEqual(HttpStatusCode.ServiceUnavailable, session.StatusCode);
        StringAssert.Contains(await session.Content.ReadAsStringAsync(), "availability");

        await using var signInFixture = await SessionFixture.CreateAsync();
        await signInFixture.CreateAccountAsync(confirmed: true);
        var token = await signInFixture.BootstrapAsync();
        File.Delete(signInFixture.Database.DatabasePath);
        using var response = await signInFixture.PostAsync("/api/accounts/sign-in", new { email = signInFixture.Email, password = Password }, token);
        Assert.AreEqual(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.IsFalse(response.Headers.Contains("Set-Cookie"));
        var body = await response.Content.ReadAsStringAsync();
        StringAssert.Contains(body, "availability");
        Assert.IsFalse(body.Contains(signInFixture.Email, StringComparison.Ordinal));
        Assert.IsFalse(body.Contains(Password, StringComparison.Ordinal));
    }

    private static async Task AssertMutationInvalidatesAsync(bool delete)
    {
        await using var fixture = await SessionFixture.CreateAsync();
        var user = await fixture.CreateAccountAsync(confirmed: true);
        using (await fixture.SignInAsync()) { }
        using (var scope = fixture.Factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var current = (await users.FindByIdAsync(user.Id))!;
            var result = delete ? await users.DeleteAsync(current) : await users.UpdateSecurityStampAsync(current);
            Assert.IsTrue(result.Succeeded);
        }
        using var response = await fixture.Client.GetAsync("/api/accounts/session");
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

internal sealed class SessionFixture : IAsyncDisposable
{
    private SessionFixture(StorageTestDatabase database, string keysPath, AdjustableTimeProvider time)
    {
        Database = database;
        Time = time;
        Factory = new AccountWebApplicationFactory(database.DatabasePath, keysPath, new CapturingConfirmationSender(), timeProvider: time);
        Client = Factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), HandleCookies = true, AllowAutoRedirect = false });
    }

    public string Email { get; } = "session@example.test";
    public StorageTestDatabase Database { get; }
    public AdjustableTimeProvider Time { get; }
    public AccountWebApplicationFactory Factory { get; }
    public HttpClient Client { get; }

    public HttpClient CreateClient() => Factory.CreateClient(new()
        { BaseAddress = new Uri("https://localhost"), HandleCookies = true, AllowAutoRedirect = false });

    public static async Task<SessionFixture> CreateAsync()
    {
        var database = StorageTestDatabase.Create();
        await database.MigrateAsync();
        var keysPath = Directory.CreateDirectory(Path.Combine(database.RootPath, "keys")).FullName;
        return new(database, keysPath, new AdjustableTimeProvider(new DateTimeOffset(2026, 9, 10, 8, 0, 0, TimeSpan.Zero)));
    }

    public async Task<IdentityUser> CreateAccountAsync(bool confirmed)
    {
        using var scope = Factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var user = new IdentityUser { UserName = Email, Email = Email, EmailConfirmed = confirmed };
        var result = await users.CreateAsync(user, AccountSessionTests.Password);
        Assert.IsTrue(result.Succeeded, string.Join(',', result.Errors.Select(error => error.Code)));
        return user;
    }

    public async Task<string> BootstrapAsync(HttpClient? client = null)
    {
        using var response = await (client ?? Client).GetAsync("/api/accounts/antiforgery");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("requestToken").GetString()!;
    }

    public async Task<HttpResponseMessage> SignInAsync()
    {
        var token = await BootstrapAsync();
        return await PostAsync("/api/accounts/sign-in", new { email = Email, password = AccountSessionTests.Password }, token);
    }

    public Task<HttpResponseMessage> PostAsync(string path, object? body, string? token, HttpClient? client = null)
    {
        HttpContent content = body is null ? new ByteArrayContent([]) : JsonContent.Create(body);
        if (body is null) content.Headers.ContentType = null;
        return SendAsync(path, content, token, client);
    }

    public Task<HttpResponseMessage> SendAsync(string path, HttpContent content, string? token, HttpClient? client = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = content };
        if (token is not null) request.Headers.Add("X-LinguaDesk-Antiforgery", token);
        return (client ?? Client).SendAsync(request);
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await Factory.DisposeAsync();
        await Database.DisposeAsync();
    }
}
