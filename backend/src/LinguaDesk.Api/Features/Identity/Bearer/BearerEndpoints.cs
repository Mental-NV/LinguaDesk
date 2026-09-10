using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using LinguaDesk.Api.Features.Identity.Session;

namespace LinguaDesk.Api.Features.Identity.Bearer;

public static class BearerEndpoints
{
    public static IEndpointRouteBuilder MapAccountBearer(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/accounts/bearer-sign-in", SignInAsync)
            .WithName("signInBearerClient").WithGroupName("linguadesk")
            .WithSummary("Sign in an independent API client")
            .WithDescription("Anonymous. Accepts exactly email and password JSON. Returns only the opaque 15-minute access / 7-day refresh pair with expiresIn 900 and tokenType Bearer; tokens are never set as cookies.")
            .Accepts<BearerSignInRequest>("application/json")
            .Produces<BearerTokenPairResponse>().Produces<SessionProblemDetails>(400, "application/problem+json")
            .Produces<SessionProblemDetails>(401, "application/problem+json")
            .Produces(405)
            .Produces<SessionProblemDetails>(415, "application/problem+json")
            .Produces<SessionProblemDetails>(503, "application/problem+json").AllowAnonymous();

        endpoints.MapPost("/api/accounts/bearer-refresh", RefreshAsync)
            .WithName("refreshBearerClient").WithGroupName("linguadesk")
            .WithSummary("Refresh a held bearer pair")
            .WithDescription("Anonymous. Accepts exactly the currently held refreshToken and accessToken pair. Returns a replacement pair with fresh expiries; never replays language work.")
            .Accepts<BearerRefreshRequest>("application/json")
            .Produces<BearerTokenPairResponse>().Produces<SessionProblemDetails>(400, "application/problem+json")
            .Produces<SessionProblemDetails>(401, "application/problem+json")
            .Produces(405)
            .Produces<SessionProblemDetails>(415, "application/problem+json")
            .Produces<SessionProblemDetails>(503, "application/problem+json").AllowAnonymous();

        endpoints.MapGet("/api/accounts/me", MeAsync)
            .WithName("getCurrentAccount").WithGroupName("linguadesk")
            .WithSummary("Read the current account")
            .WithDescription("Accepts either a valid Authorization Bearer credential or a valid session cookie. Reports which mode authenticated the call; bearer header wins with no cookie fallback.")
            .Produces<CurrentAccountResponse>().Produces<SessionProblemDetails>(400, "application/problem+json")
            .Produces<SessionProblemDetails>(401, "application/problem+json")
            .Produces<SessionProblemDetails>(503, "application/problem+json").Produces(405);

        MapMediaFallback(endpoints, "/api/accounts/bearer-sign-in");
        MapMediaFallback(endpoints, "/api/accounts/bearer-refresh");
        MapWrongMethods(endpoints, "/api/accounts/bearer-sign-in", HttpMethods.Post);
        MapWrongMethods(endpoints, "/api/accounts/bearer-refresh", HttpMethods.Post);
        MapWrongMethods(endpoints, "/api/accounts/me", HttpMethods.Get);
        return endpoints;
    }

    private static async Task<IResult> SignInAsync(HttpContext context, UserManager<IdentityUser> users,
        SignInManager<IdentityUser> signInManager, IOptionsMonitor<BearerTokenOptions> bearerOptions,
        TimeProvider timeProvider, CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        if (context.Request.QueryString.HasValue)
            return Problem(context, 400, "invalidRequest", "Bearer sign-in accepts no query parameters.");
        if (!IsUtf8Json(context.Request.ContentType))
            return Problem(context, 415, "invalidRequest", "The request must use UTF-8 application/json.");

        BearerSignInRequest? request;
        Dictionary<string, string[]>? errors;
        try { (request, errors) = await ParseSignInAsync(context.Request.Body, cancellationToken); }
        catch (Exception ex) when (ex is JsonException or DecoderFallbackException
            || ex is InvalidOperationException { InnerException: DecoderFallbackException })
        { return Problem(context, 400, "invalidRequest", "The JSON request body is malformed."); }
        if (errors is not null)
            return Problem(context, 400, "invalidRequest", "One or more account fields are invalid.", errors);

        IdentityUser? user;
        bool valid;
        try
        {
            user = await users.FindByEmailAsync(request!.Email);
            valid = user is not null && await users.CheckPasswordAsync(user, request.Password);
        }
        catch
        {
            return Problem(context, 503, "availability", "Bearer sign-in is temporarily unavailable.");
        }
        if (!valid)
            return Problem(context, 401, "invalidCredentials", "The email or password is invalid.");

        BearerTokenPairResponse pair;
        try
        {
            var principal = await signInManager.CreateUserPrincipalAsync(user!);
            pair = IssuePair(bearerOptions, timeProvider, principal,
                user!.EmailConfirmed ? SessionVerificationStatus.Verified : SessionVerificationStatus.VerificationRequired);
        }
        catch
        {
            return Problem(context, 503, "availability", "Bearer sign-in is temporarily unavailable.");
        }
        return Results.Json(pair);
    }

    private static async Task<IResult> RefreshAsync(HttpContext context, SignInManager<IdentityUser> signInManager,
        IOptionsMonitor<BearerTokenOptions> bearerOptions, TimeProvider timeProvider, CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        if (context.Request.QueryString.HasValue)
            return Problem(context, 400, "invalidRequest", "Bearer refresh accepts no query parameters.");
        if (!IsUtf8Json(context.Request.ContentType))
            return Problem(context, 415, "invalidRequest", "The request must use UTF-8 application/json.");

        BearerRefreshRequest? request;
        Dictionary<string, string[]>? errors;
        try { (request, errors) = await ParseRefreshAsync(context.Request.Body, cancellationToken); }
        catch (Exception ex) when (ex is JsonException or DecoderFallbackException
            || ex is InvalidOperationException { InnerException: DecoderFallbackException })
        { return Problem(context, 400, "invalidRequest", "The JSON request body is malformed."); }
        if (errors is not null)
            return Problem(context, 400, "invalidRequest", "One or more account fields are invalid.", errors);

        AuthenticationTicket? accessTicket;
        AuthenticationTicket? refreshTicket;
        try
        {
            var options = bearerOptions.Get(IdentityConstants.BearerScheme);
            accessTicket = options.BearerTokenProtector.Unprotect(request!.AccessToken);
            refreshTicket = options.RefreshTokenProtector.Unprotect(request.RefreshToken);
        }
        catch
        {
            return Problem(context, 503, "availability", "Bearer refresh is temporarily unavailable.");
        }

        if (accessTicket?.Principal is null || refreshTicket?.Principal is null)
            return Problem(context, 401, "authenticationRequired", "Authentication is required.");

        var accessUserId = accessTicket.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var refreshUserId = refreshTicket.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (accessUserId is null || refreshUserId is null
            || !string.Equals(accessUserId, refreshUserId, StringComparison.Ordinal))
            return Problem(context, 401, "authenticationRequired", "Authentication is required.");

        var refreshExpiry = refreshTicket.Properties?.ExpiresUtc;
        if (refreshExpiry is null || timeProvider.GetUtcNow() >= refreshExpiry.Value)
            return Problem(context, 401, "authenticationRequired", "Authentication is required.");

        IdentityUser? current;
        try
        {
            current = await signInManager.ValidateSecurityStampAsync(refreshTicket.Principal);
        }
        catch
        {
            return Problem(context, 503, "availability", "Bearer refresh is temporarily unavailable.");
        }
        if (current is null || !string.Equals(current.Id, refreshUserId, StringComparison.Ordinal))
            return Problem(context, 401, "authenticationRequired", "Authentication is required.");

        BearerTokenPairResponse pair;
        try
        {
            var principal = await signInManager.CreateUserPrincipalAsync(current);
            pair = IssuePair(bearerOptions, timeProvider, principal,
                current.EmailConfirmed ? SessionVerificationStatus.Verified : SessionVerificationStatus.VerificationRequired);
        }
        catch
        {
            return Problem(context, 503, "availability", "Bearer refresh is temporarily unavailable.");
        }
        return Results.Json(pair);
    }

    private static async Task<IResult> MeAsync(HttpContext context, UserManager<IdentityUser> users)
    {
        context.Response.Headers.CacheControl = "no-store";
        if (context.Request.QueryString.HasValue)
            return Problem(context, 400, "invalidRequest", "Current account accepts no query parameters.");
        var authentication = await context.AuthenticateAsync(Session.SessionAuthentication.PolicyScheme);
        if (context.Items.ContainsKey(BearerAuthentication.AvailabilityItem)
            || context.Items.ContainsKey(Session.SessionAuthentication.AvailabilityItem))
            return Problem(context, 503, "availability", "Account access is temporarily unavailable.");
        if (!authentication.Succeeded || authentication.Principal is null)
            return Problem(context, 401, "authenticationRequired", "Authentication is required.");
        var userId = authentication.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
        IdentityUser? user;
        try { user = userId is null ? null : await users.FindByIdAsync(userId); }
        catch { return Problem(context, 503, "availability", "Account access is temporarily unavailable."); }
        if (user is null)
            return Problem(context, 401, "authenticationRequired", "Authentication is required.");

        var hasBearerHeader = context.Request.Headers.Authorization.Any(static value =>
            value?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) == true);
        if (hasBearerHeader && !string.Equals(authentication.Ticket?.AuthenticationScheme, $"{IdentityConstants.BearerScheme}:AccessToken", StringComparison.Ordinal))
            return Problem(context, 401, "authenticationRequired", "Authentication is required.");

        var mode = string.Equals(authentication.Ticket?.AuthenticationScheme, $"{IdentityConstants.BearerScheme}:AccessToken", StringComparison.Ordinal)
            ? AuthMode.Bearer : AuthMode.Cookie;
        return Results.Json(new CurrentAccountResponse(
            user.Email ?? string.Empty,
            user.EmailConfirmed ? SessionVerificationStatus.Verified : SessionVerificationStatus.VerificationRequired,
            mode));
    }

    private static BearerTokenPairResponse IssuePair(IOptionsMonitor<BearerTokenOptions> bearerOptions,
        TimeProvider timeProvider, ClaimsPrincipal principal, SessionVerificationStatus verificationStatus)
    {
        var options = bearerOptions.Get(IdentityConstants.BearerScheme);
        var issued = (options.TimeProvider ?? timeProvider).GetUtcNow();
        var accessProperties = new AuthenticationProperties { IssuedUtc = issued, ExpiresUtc = issued + options.BearerTokenExpiration };
        var refreshProperties = new AuthenticationProperties { IssuedUtc = issued, ExpiresUtc = issued + options.RefreshTokenExpiration };
        var accessTicket = new AuthenticationTicket(principal, accessProperties, $"{IdentityConstants.BearerScheme}:AccessToken");
        var refreshTicket = new AuthenticationTicket(principal, refreshProperties, $"{IdentityConstants.BearerScheme}:RefreshToken");
        return new BearerTokenPairResponse(
            options.BearerTokenProtector.Protect(accessTicket),
            options.RefreshTokenProtector.Protect(refreshTicket),
            BearerAuthentication.AccessLifetimeSeconds,
            BearerAuthentication.TokenType,
            verificationStatus);
    }

    private static async Task<(BearerSignInRequest?, Dictionary<string, string[]>?)> ParseSignInAsync(Stream body, CancellationToken ct)
    {
        using var document = await JsonDocument.ParseAsync(body, new JsonDocumentOptions { MaxDepth = 4 }, ct);
        if (document.RootElement.ValueKind != JsonValueKind.Object) return (null, ShapeError());
        string? email = null, password = null;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in document.RootElement.EnumerateObject())
        {
            if (!seen.Add(property.Name) || property.Name is not ("email" or "password") || property.Value.ValueKind != JsonValueKind.String)
                return (null, ShapeError());
            if (property.Name == "email") email = property.Value.GetString(); else password = property.Value.GetString();
        }
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        if (!IsValidEmail(email)) errors["email"] = ["Email must be a valid address of at most 254 characters with no surrounding whitespace."];
        if (!LinguaDeskPasswordValidator.TryCountScalars(password, out var length) || length < 15 || length > 128)
            errors["password"] = ["Password must contain 15 to 128 well-formed Unicode characters."];
        return errors.Count == 0 ? (new BearerSignInRequest(email!, password!), null) : (null, errors);
    }

    private static async Task<(BearerRefreshRequest?, Dictionary<string, string[]>?)> ParseRefreshAsync(Stream body, CancellationToken ct)
    {
        using var document = await JsonDocument.ParseAsync(body, new JsonDocumentOptions { MaxDepth = 4 }, ct);
        if (document.RootElement.ValueKind != JsonValueKind.Object) return (null, RefreshShapeError());
        string? refreshToken = null, accessToken = null;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in document.RootElement.EnumerateObject())
        {
            if (!seen.Add(property.Name) || property.Name is not ("refreshToken" or "accessToken") || property.Value.ValueKind != JsonValueKind.String)
                return (null, RefreshShapeError());
            if (property.Name == "refreshToken") refreshToken = property.Value.GetString(); else accessToken = property.Value.GetString();
        }
        if (string.IsNullOrEmpty(refreshToken) || string.IsNullOrEmpty(accessToken))
            return (null, RefreshShapeError());
        return (new BearerRefreshRequest(refreshToken, accessToken), null);
    }

    private static bool IsValidEmail(string? value) => value is not null && value.Length > 0 && value == value.Trim()
        && LinguaDeskPasswordValidator.TryCountScalars(value, out var count) && count <= 254 && new EmailAddressAttribute().IsValid(value);
    private static bool IsUtf8Json(string? contentType) => MediaTypeHeaderValue.TryParse(contentType, out var parsed)
        && string.Equals(parsed.MediaType, "application/json", StringComparison.OrdinalIgnoreCase)
        && (string.IsNullOrEmpty(parsed.CharSet) || string.Equals(parsed.CharSet, "utf-8", StringComparison.OrdinalIgnoreCase));
    private static Dictionary<string, string[]> ShapeError() => new(StringComparer.Ordinal)
        { ["email"] = ["Only the required email and password fields are accepted."] };
    private static Dictionary<string, string[]> RefreshShapeError() => new(StringComparer.Ordinal)
        { ["refreshToken"] = ["Only the required refreshToken and accessToken fields are accepted."] };

    private static IResult Problem(HttpContext context, int status, string category, string detail,
        Dictionary<string, string[]>? errors = null) => Results.Json(new SessionProblemDetails
        {
            Type = "about:blank", Title = status switch { 401 => "Unauthorized", 415 => "Unsupported Media Type", 503 => "Service unavailable", _ => "Invalid request" },
            Status = status, Detail = detail, Category = category, CorrelationId = context.TraceIdentifier, Errors = errors,
        }, contentType: "application/problem+json", statusCode: status);

    private static void MapMediaFallback(IEndpointRouteBuilder endpoints, string path) =>
        endpoints.MapPost(path, (HttpContext context) => { context.Response.Headers.CacheControl = "no-store"; return Problem(context, 415, "invalidRequest", "The request must use UTF-8 application/json."); })
            .ExcludeFromDescription().AllowAnonymous();

    private static void MapWrongMethods(IEndpointRouteBuilder endpoints, string path, string allowed)
    {
        var methods = new[] { HttpMethods.Get, HttpMethods.Head, HttpMethods.Post, HttpMethods.Put, HttpMethods.Patch, HttpMethods.Delete, HttpMethods.Options }
            .Where(method => !string.Equals(method, allowed, StringComparison.Ordinal)).ToArray();
        endpoints.MapMethods(path, methods, (HttpContext context) => { context.Response.Headers.CacheControl = "no-store"; context.Response.Headers.Allow = allowed; return Results.StatusCode(405); })
            .ExcludeFromDescription().AllowAnonymous();
    }
}
