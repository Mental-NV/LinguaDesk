using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;

namespace LinguaDesk.Api.Features.Identity.Session;

public static class SessionEndpoints
{
    public static IEndpointRouteBuilder MapAccountSession(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/accounts/antiforgery", Bootstrap)
            .WithName("getAccountAntiforgeryToken").WithGroupName("linguadesk")
            .WithSummary("Obtain a browser antiforgery pair")
            .WithDescription("Returns a no-store request token and sets only the Secure, HttpOnly, SameSite=Strict __Host-LinguaDesk.Antiforgery cookie.")
            .Produces<AntiforgeryTokenResponse>().Produces<SessionProblemDetails>(400, "application/problem+json")
            .Produces(405)
            .AllowAnonymous();

        endpoints.MapPost("/api/accounts/sign-in", SignInAsync)
            .WithName("signInLocalAccount").WithGroupName("linguadesk")
            .WithSummary("Sign in with local credentials")
            .WithDescription("Requires the X-LinguaDesk-Antiforgery header and matching Strict cookie. Issues only a Secure, HttpOnly, SameSite=Lax, nonpersistent __Host-LinguaDesk.Session cookie.")
            .Accepts<SignInRequest>("application/json")
            .Produces<AccountSessionResponse>().Produces<SessionProblemDetails>(400, "application/problem+json")
            .Produces<SessionProblemDetails>(401, "application/problem+json")
            .Produces(405)
            .Produces<SessionProblemDetails>(415, "application/problem+json")
            .Produces<SessionProblemDetails>(503, "application/problem+json").AllowAnonymous();

        endpoints.MapGet("/api/accounts/session", SessionAsync)
            .WithName("getLocalAccountSession").WithGroupName("linguadesk")
            .WithSummary("Read the current browser session")
            .WithDescription("Authenticates the current account and security stamp on every request without renewing the fixed ticket.")
            .Produces<AccountSessionResponse>().Produces<SessionProblemDetails>(400, "application/problem+json")
            .Produces<SessionProblemDetails>(401, "application/problem+json")
            .Produces<SessionProblemDetails>(503, "application/problem+json").Produces(405);

        endpoints.MapPost("/api/accounts/sign-out", SignOutAsync)
            .WithName("signOutLocalAccount").WithGroupName("linguadesk")
            .WithSummary("End the caller browser session")
            .WithDescription("Requires the current antiforgery pair, clears only __Host-LinguaDesk.Session and does not change the account security stamp.")
            .Produces(204).Produces<SessionProblemDetails>(400, "application/problem+json").Produces(405).AllowAnonymous();

        MapMediaFallback(endpoints, "/api/accounts/sign-in");
        MapWrongMethods(endpoints, "/api/accounts/antiforgery", HttpMethods.Get);
        MapWrongMethods(endpoints, "/api/accounts/sign-in", HttpMethods.Post);
        MapWrongMethods(endpoints, "/api/accounts/session", HttpMethods.Get);
        MapWrongMethods(endpoints, "/api/accounts/sign-out", HttpMethods.Post);
        return endpoints;
    }

    private static IResult Bootstrap(HttpContext context, IAntiforgery antiforgery)
    {
        context.Response.Headers.CacheControl = "no-store";
        if (context.Request.QueryString.HasValue)
            return Problem(context, 400, "invalidRequest", "Antiforgery bootstrap accepts no query parameters.");
        var tokens = antiforgery.GetAndStoreTokens(context);
        return Results.Json(new AntiforgeryTokenResponse(tokens.RequestToken!, SessionAuthentication.AntiforgeryHeaderName));
    }

    private static async Task<IResult> SignInAsync(HttpContext context, IAntiforgery antiforgery,
        UserManager<IdentityUser> users, SignInManager<IdentityUser> signInManager, TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        if (!await ValidateAntiforgeryAsync(context, antiforgery)) return AntiforgeryProblem(context);
        if (context.Request.QueryString.HasValue)
            return Problem(context, 400, "invalidRequest", "Sign-in accepts no query parameters.");
        if (!IsUtf8Json(context.Request.ContentType))
            return Problem(context, 415, "invalidRequest", "The request must use UTF-8 application/json.");

        SignInRequest? request;
        Dictionary<string, string[]>? errors;
        try { (request, errors) = await ParseAsync(context.Request.Body, cancellationToken); }
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
            return Problem(context, 503, "availability", "Account sign-in is temporarily unavailable.");
        }
        if (!valid)
            return Problem(context, 401, "invalidCredentials", "The email or password is invalid.");

        var issued = timeProvider.GetUtcNow();
        try
        {
            await signInManager.SignInAsync(user!, new AuthenticationProperties
            {
                IsPersistent = false,
                AllowRefresh = false,
                IssuedUtc = issued,
                ExpiresUtc = issued + SessionAuthentication.CookieLifetime,
            });
        }
        catch
        {
            return Problem(context, 503, "availability", "Account sign-in is temporarily unavailable.");
        }
        return Results.Json(ToResponse(user!, issued + SessionAuthentication.CookieLifetime));
    }

    private static async Task<IResult> SessionAsync(HttpContext context, UserManager<IdentityUser> users)
    {
        context.Response.Headers.CacheControl = "no-store";
        if (context.Request.QueryString.HasValue)
            return Problem(context, 400, "invalidRequest", "Session accepts no query parameters.");
        var authentication = await context.AuthenticateAsync(SessionAuthentication.PolicyScheme);
        if (context.Items.ContainsKey(SessionAuthentication.AvailabilityItem))
            return Problem(context, 503, "availability", "Account session is temporarily unavailable.");
        if (!authentication.Succeeded || authentication.Principal is null || authentication.Properties?.ExpiresUtc is null)
            return Problem(context, 401, "authenticationRequired", "Authentication is required.");
        var userId = authentication.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
        IdentityUser? user;
        try { user = userId is null ? null : await users.FindByIdAsync(userId); }
        catch { return Problem(context, 503, "availability", "Account session is temporarily unavailable."); }
        return user is null
            ? Problem(context, 401, "authenticationRequired", "Authentication is required.")
            : Results.Json(ToResponse(user, authentication.Properties.ExpiresUtc.Value));
    }

    private static async Task<IResult> SignOutAsync(HttpContext context, IAntiforgery antiforgery,
        SignInManager<IdentityUser> signInManager)
    {
        context.Response.Headers.CacheControl = "no-store";
        if (!await ValidateAntiforgeryAsync(context, antiforgery)) return AntiforgeryProblem(context);
        if (context.Request.QueryString.HasValue)
            return Problem(context, 400, "invalidRequest", "Sign-out accepts no query parameters.");
        await signInManager.SignOutAsync();
        return Results.NoContent();
    }

    private static AccountSessionResponse ToResponse(IdentityUser user, DateTimeOffset expiry) =>
        new(SessionStatus.SignedIn, user.EmailConfirmed ? SessionVerificationStatus.Verified : SessionVerificationStatus.VerificationRequired, expiry);

    private static async Task<bool> ValidateAntiforgeryAsync(HttpContext context, IAntiforgery antiforgery)
    {
        try { await antiforgery.ValidateRequestAsync(context); return true; }
        catch (AntiforgeryValidationException) { return false; }
    }

    private static IResult AntiforgeryProblem(HttpContext context) =>
        Problem(context, 400, "invalidAntiforgery", "The antiforgery token is invalid.");

    private static async Task<(SignInRequest?, Dictionary<string, string[]>?)> ParseAsync(Stream body, CancellationToken ct)
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
        return errors.Count == 0 ? (new SignInRequest(email!, password!), null) : (null, errors);
    }

    private static bool IsValidEmail(string? value) => value is not null && value.Length > 0 && value == value.Trim()
        && LinguaDeskPasswordValidator.TryCountScalars(value, out var count) && count <= 254 && new EmailAddressAttribute().IsValid(value);
    private static bool IsUtf8Json(string? contentType) => MediaTypeHeaderValue.TryParse(contentType, out var parsed)
        && string.Equals(parsed.MediaType, "application/json", StringComparison.OrdinalIgnoreCase)
        && (string.IsNullOrEmpty(parsed.CharSet) || string.Equals(parsed.CharSet, "utf-8", StringComparison.OrdinalIgnoreCase));
    private static Dictionary<string, string[]> ShapeError() => new(StringComparer.Ordinal)
        { ["email"] = ["Only the required email and password fields are accepted."] };

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
