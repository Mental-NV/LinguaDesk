using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;

namespace LinguaDesk.Api.Features.Identity.Recovery;

public static class RecoveryEndpoints
{
    private const string InvalidResetDetail = "The password-reset link is invalid or expired.";

    public static IEndpointRouteBuilder MapAccountRecovery(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/accounts/forgot-password", ForgotPasswordAsync)
            .WithName("requestLocalAccountPasswordReset")
            .WithGroupName("linguadesk")
            .WithSummary("Request a local-account password reset")
            .WithDescription("Returns one fixed acknowledgment for every syntactically valid email and requests delivery only for an existing local account outside its cooldown.")
            .Accepts<ForgotPasswordRequest>("application/json")
            .Produces<ForgotPasswordAccepted>(StatusCodes.Status202Accepted, "application/json")
            .Produces<RecoveryProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")
            .Produces(StatusCodes.Status405MethodNotAllowed)
            .Produces<RecoveryProblemDetails>(StatusCodes.Status415UnsupportedMediaType, "application/problem+json")
            .Produces<RecoveryProblemDetails>(StatusCodes.Status503ServiceUnavailable, "application/problem+json")
            .AllowAnonymous();
        MapFallbacks(endpoints, "/api/accounts/forgot-password");

        endpoints.MapPost("/api/accounts/reset-password", ResetPasswordAsync)
            .WithName("resetLocalAccountPassword")
            .WithGroupName("linguadesk")
            .WithSummary("Complete a local-account password reset")
            .WithDescription("Sets a new policy-compliant password from delivered opaque material without issuing authentication credentials or redirecting.")
            .Accepts<ResetPasswordRequest>("application/json")
            .Produces<ResetPasswordAccepted>(StatusCodes.Status200OK, "application/json")
            .Produces<RecoveryProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")
            .Produces(StatusCodes.Status405MethodNotAllowed)
            .Produces<RecoveryProblemDetails>(StatusCodes.Status415UnsupportedMediaType, "application/problem+json")
            .Produces<RecoveryProblemDetails>(StatusCodes.Status503ServiceUnavailable, "application/problem+json")
            .AllowAnonymous();
        MapFallbacks(endpoints, "/api/accounts/reset-password");

        return endpoints;
    }

    private static async Task<IResult> ForgotPasswordAsync(
        HttpContext context,
        AccountPasswordResetCoordinator coordinator,
        CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        var correlationId = context.TraceIdentifier;
        if (context.Request.QueryString.HasValue)
        {
            return InvalidRequest("Password reset requests accept no query parameters.", correlationId);
        }
        if (!IsUtf8Json(context.Request.ContentType))
        {
            return UnsupportedMediaType(correlationId);
        }

        ForgotPasswordRequest? request;
        try
        {
            request = await ParseForgotAsync(context.Request.Body, cancellationToken);
        }
        catch (JsonException)
        {
            return InvalidRequest("The JSON request body is malformed.", correlationId);
        }
        catch (DecoderFallbackException)
        {
            return InvalidRequest("The JSON request body is malformed.", correlationId);
        }
        catch (InvalidOperationException)
        {
            // Covers reader/object-disposed failures and unpaired-surrogate
            // materialization, which surfaces as InvalidOperationException
            // rather than JsonException. The body cannot be honored safely.
            return InvalidRequest("The JSON request body is malformed.", correlationId);
        }

        if (request is null)
        {
            return InvalidRequest("Only the required email field is accepted.", correlationId);
        }
        if (!IsValidEmail(request.Email))
        {
            return Problem(
                StatusCodes.Status400BadRequest,
                "Invalid request",
                "One or more account fields are invalid.",
                correlationId,
                "invalidRequest",
                new() { ["email"] = ["Email must be a valid address of at most 254 characters with no surrounding whitespace."] });
        }

        var outcome = await coordinator.RequestResetAsync(request.Email, correlationId, cancellationToken);
        return outcome switch
        {
            ForgotPasswordOutcome.Accepted => Results.Json(
                new ForgotPasswordAccepted(
                    ForgotPasswordStatus.PasswordResetRequested,
                    AccountPasswordResetCoordinator.RetryAfterSeconds),
                statusCode: StatusCodes.Status202Accepted),
            _ => Problem(StatusCodes.Status503ServiceUnavailable, "Service unavailable",
                "Account password reset is temporarily unavailable.", correlationId, "availability"),
        };
    }

    private static async Task<IResult> ResetPasswordAsync(
        HttpContext context,
        AccountPasswordResetCoordinator coordinator,
        CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        var correlationId = context.TraceIdentifier;
        if (context.Request.QueryString.HasValue)
        {
            return InvalidRequest("Password reset accepts no query parameters.", correlationId);
        }
        if (!IsUtf8Json(context.Request.ContentType))
        {
            return UnsupportedMediaType(correlationId);
        }

        ResetPasswordRequest? request;
        try
        {
            request = await ParseResetAsync(context.Request.Body, cancellationToken);
        }
        catch (JsonException)
        {
            return InvalidRequest("The JSON request body is malformed.", correlationId);
        }
        catch (DecoderFallbackException)
        {
            return InvalidRequest("The JSON request body is malformed.", correlationId);
        }
        catch (InvalidOperationException)
        {
            // Covers reader/object-disposed failures and unpaired-surrogate
            // materialization, which surfaces as InvalidOperationException
            // rather than JsonException. The body cannot be honored safely.
            return InvalidRequest("The JSON request body is malformed.", correlationId);
        }

        if (request is null)
        {
            return InvalidRequest("Only the required userId, code and newPassword fields are accepted.", correlationId);
        }
        if (!LinguaDeskPasswordValidator.TryCountScalars(request.NewPassword, out var passwordLength)
            || passwordLength < LinguaDeskPasswordValidator.MinimumScalarLength
            || passwordLength > LinguaDeskPasswordValidator.MaximumScalarLength)
        {
            return Problem(
                StatusCodes.Status400BadRequest,
                "Invalid request",
                "One or more account fields are invalid.",
                correlationId,
                "invalidRequest",
                new() { ["newPassword"] = ["Password must contain 15 to 128 well-formed Unicode characters."] });
        }
        if (request.UserId.Length is 0 or > 450
            || request.Code.Length is 0 or > 4096
            || !TryDecodeCode(request.Code, out var token))
        {
            return InvalidOrExpired(correlationId);
        }

        var outcome = await coordinator.ResetAsync(request.UserId, token, request.NewPassword);
        return outcome switch
        {
            ResetPasswordOutcome.PasswordReset => Results.Json(
                new ResetPasswordAccepted(ResetPasswordStatus.PasswordReset),
                statusCode: StatusCodes.Status200OK),
            ResetPasswordOutcome.InvalidOrExpired => InvalidOrExpired(correlationId),
            _ => Problem(StatusCodes.Status503ServiceUnavailable, "Service unavailable",
                "Account password reset is temporarily unavailable.", correlationId, "availability"),
        };
    }

    private static async Task<ForgotPasswordRequest?> ParseForgotAsync(Stream body, CancellationToken cancellationToken)
    {
        using var document = await JsonDocument.ParseAsync(body, new JsonDocumentOptions { MaxDepth = 4 }, cancellationToken);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        string? email = null;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in document.RootElement.EnumerateObject())
        {
            if (!seen.Add(property.Name) || property.Name != "email" || property.Value.ValueKind != JsonValueKind.String)
            {
                return null;
            }
            email = ReadLenient(property.Value);
        }
        return email is null ? null : new ForgotPasswordRequest(email);
    }

    private static async Task<ResetPasswordRequest?> ParseResetAsync(Stream body, CancellationToken cancellationToken)
    {
        using var document = await JsonDocument.ParseAsync(body, new JsonDocumentOptions { MaxDepth = 4 }, cancellationToken);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        string? userId = null;
        string? code = null;
        string? newPassword = null;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in document.RootElement.EnumerateObject())
        {
            if (!seen.Add(property.Name)
                || property.Name is not ("userId" or "code" or "newPassword")
                || property.Value.ValueKind != JsonValueKind.String)
            {
                return null;
            }
            if (property.Name == "userId") userId = ReadLenient(property.Value);
            else if (property.Name == "code") code = ReadLenient(property.Value);
            else newPassword = ReadLenient(property.Value);
        }
        return userId is null || code is null || newPassword is null
            ? null
            : new ResetPasswordRequest(userId, code, newPassword);
    }

    private static string ReadLenient(JsonElement element)
    {
        try
        {
            // An unpaired surrogate cannot be materialized as a string and
            // surfaces as InvalidOperationException. Map it to empty so the
            // field falls into its semantic validation: unknown-user/generic
            // reset material for userId/code, field errors for email and the
            // exact-password policy for newPassword.
            return element.GetString() ?? string.Empty;
        }
        catch (InvalidOperationException)
        {
            return string.Empty;
        }
    }

    private static bool TryDecodeCode(string code, out string token)
    {
        token = string.Empty;
        if (code.Any(character => !(character is >= 'A' and <= 'Z'
                or >= 'a' and <= 'z'
                or >= '0' and <= '9'
                or '-'
                or '_')))
        {
            return false;
        }

        try
        {
            token = new UTF8Encoding(false, true).GetString(WebEncoders.Base64UrlDecode(code));
            return token.Length > 0;
        }
        catch (Exception exception) when (exception is FormatException or DecoderFallbackException)
        {
            return false;
        }
    }

    private static bool IsValidEmail(string value) =>
        value.Length > 0
        && value == value.Trim()
        && LinguaDeskPasswordValidator.TryCountScalars(value, out var count)
        && count <= 254
        && new EmailAddressAttribute().IsValid(value);

    private static bool IsUtf8Json(string? contentType)
    {
        if (!MediaTypeHeaderValue.TryParse(contentType, out var parsed)
            || !string.Equals(parsed.MediaType, "application/json", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
        return string.IsNullOrEmpty(parsed.CharSet)
            || string.Equals(parsed.CharSet, "utf-8", StringComparison.OrdinalIgnoreCase);
    }

    private static void MapFallbacks(IEndpointRouteBuilder endpoints, string path)
    {
        endpoints.MapPost(path, static (HttpContext context) =>
            {
                context.Response.Headers.CacheControl = "no-store";
                return UnsupportedMediaType(context.TraceIdentifier);
            })
            .ExcludeFromDescription()
            .AllowAnonymous();
        endpoints.MapMethods(path, [
            HttpMethods.Get,
            HttpMethods.Head,
            HttpMethods.Put,
            HttpMethods.Patch,
            HttpMethods.Delete,
            HttpMethods.Options,
        ], static (HttpContext context) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            context.Response.Headers.Allow = HttpMethods.Post;
            return Results.StatusCode(StatusCodes.Status405MethodNotAllowed);
        }).ExcludeFromDescription();
    }

    private static IResult InvalidRequest(string detail, string correlationId) => Problem(
        StatusCodes.Status400BadRequest, "Invalid request", detail, correlationId, "invalidRequest");

    private static IResult InvalidOrExpired(string correlationId) => Problem(
        StatusCodes.Status400BadRequest,
        "Invalid or expired password reset",
        InvalidResetDetail,
        correlationId,
        "invalidOrExpiredPasswordReset");

    private static IResult UnsupportedMediaType(string correlationId) => Problem(
        StatusCodes.Status415UnsupportedMediaType,
        "Unsupported Media Type",
        "The request must use UTF-8 application/json.",
        correlationId,
        "invalidRequest");

    private static IResult Problem(
        int status,
        string title,
        string detail,
        string correlationId,
        string category,
        Dictionary<string, string[]>? errors = null) => Results.Json(
            new RecoveryProblemDetails
            {
                Type = "about:blank",
                Title = title,
                Status = status,
                Detail = detail,
                Category = category,
                CorrelationId = correlationId,
                Errors = errors,
            },
            contentType: "application/problem+json",
            statusCode: status);
}
