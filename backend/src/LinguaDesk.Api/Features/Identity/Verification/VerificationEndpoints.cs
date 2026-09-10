using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;

namespace LinguaDesk.Api.Features.Identity.Verification;

public static class VerificationEndpoints
{
    private const string InvalidVerificationDetail = "The verification link is invalid or expired.";

    public static IEndpointRouteBuilder MapAccountVerification(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/accounts/confirm-email", ConfirmEmailAsync)
            .WithName("confirmLocalAccountEmail")
            .WithGroupName("linguadesk")
            .WithSummary("Confirm a local account email")
            .WithDescription("Confirms an unverified local account from delivered opaque material without issuing authentication credentials or redirecting.")
            .Accepts<ConfirmEmailRequest>("application/json")
            .Produces<ConfirmEmailAccepted>(StatusCodes.Status200OK, "application/json")
            .Produces<VerificationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")
            .Produces(StatusCodes.Status405MethodNotAllowed)
            .Produces<VerificationProblemDetails>(StatusCodes.Status415UnsupportedMediaType, "application/problem+json")
            .Produces<VerificationProblemDetails>(StatusCodes.Status503ServiceUnavailable, "application/problem+json")
            .AllowAnonymous();
        MapFallbacks(endpoints, "/api/accounts/confirm-email");

        endpoints.MapPost("/api/accounts/resend-verification", ResendVerificationAsync)
            .WithName("resendLocalAccountVerification")
            .WithGroupName("linguadesk")
            .WithSummary("Request another local-account verification delivery")
            .WithDescription("Returns one fixed acknowledgment for every syntactically valid email and requests delivery only for an eligible unverified account outside its cooldown.")
            .Accepts<ResendVerificationRequest>("application/json")
            .Produces<ResendVerificationAccepted>(StatusCodes.Status202Accepted, "application/json")
            .Produces<VerificationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")
            .Produces(StatusCodes.Status405MethodNotAllowed)
            .Produces<VerificationProblemDetails>(StatusCodes.Status415UnsupportedMediaType, "application/problem+json")
            .Produces<VerificationProblemDetails>(StatusCodes.Status503ServiceUnavailable, "application/problem+json")
            .AllowAnonymous();
        MapFallbacks(endpoints, "/api/accounts/resend-verification");

        return endpoints;
    }

    private static async Task<IResult> ConfirmEmailAsync(
        HttpContext context,
        AccountConfirmationCoordinator coordinator,
        CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        var correlationId = context.TraceIdentifier;
        if (context.Request.QueryString.HasValue)
        {
            return InvalidRequest("Email confirmation accepts no query parameters.", correlationId);
        }
        if (!IsUtf8Json(context.Request.ContentType))
        {
            return UnsupportedMediaType(correlationId);
        }

        ConfirmEmailRequest? request;
        try
        {
            request = await ParseConfirmationAsync(context.Request.Body, cancellationToken);
        }
        catch (JsonException)
        {
            return InvalidRequest("The JSON request body is malformed.", correlationId);
        }
        catch (DecoderFallbackException)
        {
            return InvalidRequest("The JSON request body is malformed.", correlationId);
        }
        catch (InvalidOperationException exception) when (exception.InnerException is DecoderFallbackException)
        {
            return InvalidRequest("The JSON request body is malformed.", correlationId);
        }

        if (request is null)
        {
            return InvalidRequest("Only the required userId and code fields are accepted.", correlationId);
        }
        if (request.UserId.Length is 0 or > 450
            || request.Code.Length is 0 or > 4096
            || !TryDecodeCode(request.Code, out var token))
        {
            return InvalidOrExpired(correlationId);
        }

        var outcome = await coordinator.ConfirmAsync(request.UserId, token);
        return outcome switch
        {
            AccountConfirmationOutcome.Verified => Results.Json(
                new ConfirmEmailAccepted(ConfirmationStatus.Verified),
                statusCode: StatusCodes.Status200OK),
            AccountConfirmationOutcome.InvalidOrExpired => InvalidOrExpired(correlationId),
            _ => Problem(StatusCodes.Status503ServiceUnavailable, "Service unavailable",
                "Account verification is temporarily unavailable.", correlationId, "availability"),
        };
    }

    private static async Task<IResult> ResendVerificationAsync(
        HttpContext context,
        AccountVerificationDeliveryCoordinator coordinator,
        CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        var correlationId = context.TraceIdentifier;
        if (context.Request.QueryString.HasValue)
        {
            return InvalidRequest("Verification resend accepts no query parameters.", correlationId);
        }
        if (!IsUtf8Json(context.Request.ContentType))
        {
            return UnsupportedMediaType(correlationId);
        }

        ResendVerificationRequest? request;
        try
        {
            request = await ParseResendAsync(context.Request.Body, cancellationToken);
        }
        catch (JsonException)
        {
            return InvalidRequest("The JSON request body is malformed.", correlationId);
        }
        catch (DecoderFallbackException)
        {
            return InvalidRequest("The JSON request body is malformed.", correlationId);
        }
        catch (InvalidOperationException exception) when (exception.InnerException is DecoderFallbackException)
        {
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

        var outcome = await coordinator.RequestResendAsync(request.Email, correlationId, cancellationToken);
        return outcome switch
        {
            ResendVerificationOutcome.Accepted => Results.Json(
                new ResendVerificationAccepted(
                    ResendVerificationStatus.VerificationRequested,
                    AccountVerificationDeliveryCoordinator.RetryAfterSeconds),
                statusCode: StatusCodes.Status202Accepted),
            _ => Problem(StatusCodes.Status503ServiceUnavailable, "Service unavailable",
                "Account verification is temporarily unavailable.", correlationId, "availability"),
        };
    }

    private static async Task<ConfirmEmailRequest?> ParseConfirmationAsync(Stream body, CancellationToken cancellationToken)
    {
        using var document = await JsonDocument.ParseAsync(body, new JsonDocumentOptions { MaxDepth = 4 }, cancellationToken);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        string? userId = null;
        string? code = null;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in document.RootElement.EnumerateObject())
        {
            if (!seen.Add(property.Name)
                || property.Name is not ("userId" or "code")
                || property.Value.ValueKind != JsonValueKind.String)
            {
                return null;
            }
            if (property.Name == "userId") userId = property.Value.GetString();
            else code = property.Value.GetString();
        }
        return userId is null || code is null ? null : new ConfirmEmailRequest(userId, code);
    }

    private static async Task<ResendVerificationRequest?> ParseResendAsync(Stream body, CancellationToken cancellationToken)
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
            email = property.Value.GetString();
        }
        return email is null ? null : new ResendVerificationRequest(email);
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
        "Invalid or expired verification",
        InvalidVerificationDetail,
        correlationId,
        "invalidOrExpiredVerification");

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
            new VerificationProblemDetails
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
