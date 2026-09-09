using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Metadata;

namespace LinguaDesk.Api.Features.Identity.Registration;

public static class RegistrationEndpoints
{
    public static IEndpointRouteBuilder MapRegistration(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/accounts/register", RegisterAsync)
            .WithName("registerLocalAccount")
            .WithGroupName("linguadesk")
            .WithSummary("Register a local account")
            .WithDescription(
                "Creates an unverified local account when the normalized email is new and always returns the same verification-required acknowledgment for a valid duplicate. No authentication credential is issued.")
            .Accepts<RegistrationRequest>("application/json")
            .Produces<RegistrationAccepted>(StatusCodes.Status202Accepted, "application/json")
            .Produces<RegistrationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")
            .Produces<RegistrationProblemDetails>(StatusCodes.Status415UnsupportedMediaType, "application/problem+json")
            .Produces<RegistrationProblemDetails>(StatusCodes.Status503ServiceUnavailable, "application/problem+json")
            .AllowAnonymous();
        endpoints.MapPost("/api/accounts/register", static (HttpContext context) =>
            {
                context.Response.Headers.CacheControl = "no-store";
                return Problem(
                    StatusCodes.Status415UnsupportedMediaType,
                    "Unsupported Media Type",
                    "The request must use UTF-8 application/json.",
                    context.TraceIdentifier);
            })
            .ExcludeFromDescription()
            .AllowAnonymous();
        endpoints.MapMethods("/api/accounts/register", [
            HttpMethods.Get,
            HttpMethods.Head,
            HttpMethods.Put,
            HttpMethods.Patch,
            HttpMethods.Delete,
            HttpMethods.Options,
        ],
            static (HttpContext context) =>
            {
                context.Response.Headers.CacheControl = "no-store";
                context.Response.Headers.Allow = HttpMethods.Post;
                return Results.StatusCode(StatusCodes.Status405MethodNotAllowed);
            }).ExcludeFromDescription();
        return endpoints;
    }

    private static async Task<IResult> RegisterAsync(
        HttpContext context,
        RegistrationCoordinator coordinator,
        CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        var correlationId = context.TraceIdentifier;
        if (context.Request.QueryString.HasValue)
        {
            return Problem(
                StatusCodes.Status400BadRequest,
                "Invalid request",
                "Registration accepts no query parameters.",
                correlationId,
                ShapeError());
        }

        if (!IsUtf8Json(context.Request.ContentType))
        {
            return Problem(StatusCodes.Status415UnsupportedMediaType, "Unsupported Media Type",
                "The request must use UTF-8 application/json.", correlationId);
        }

        RegistrationRequest? request;
        Dictionary<string, string[]>? errors;
        try
        {
            (request, errors) = await ParseAndValidateAsync(context.Request.Body, cancellationToken);
        }
        catch (JsonException)
        {
            return Problem(StatusCodes.Status400BadRequest, "Invalid request",
                "The JSON request body is malformed.", correlationId);
        }
        catch (DecoderFallbackException)
        {
            return Problem(StatusCodes.Status400BadRequest, "Invalid request",
                "The JSON request body is malformed.", correlationId);
        }
        catch (InvalidOperationException exception) when (exception.InnerException is DecoderFallbackException)
        {
            return Problem(StatusCodes.Status400BadRequest, "Invalid request",
                "The JSON request body is malformed.", correlationId);
        }

        if (errors is not null)
        {
            return Problem(StatusCodes.Status400BadRequest, "Invalid request",
                "One or more account fields are invalid.", correlationId, errors);
        }

        var outcome = await coordinator.RegisterAsync(request!, correlationId, cancellationToken);
        return outcome switch
        {
            RegistrationOutcome.Accepted => Results.Json(
                new RegistrationAccepted(RegistrationStatus.VerificationRequired),
                statusCode: StatusCodes.Status202Accepted),
            RegistrationOutcome.InvalidPassword => Problem(StatusCodes.Status400BadRequest, "Invalid request",
                "One or more account fields are invalid.", correlationId,
                new() { ["password"] = ["Password must contain 15 to 128 well-formed Unicode characters."] }),
            _ => Problem(StatusCodes.Status503ServiceUnavailable, "Service unavailable",
                "Account registration is temporarily unavailable.", correlationId, category: "availability"),
        };
    }

    private static async Task<(RegistrationRequest? Request, Dictionary<string, string[]>? Errors)> ParseAndValidateAsync(
        Stream body,
        CancellationToken cancellationToken)
    {
        using var document = await JsonDocument.ParseAsync(body, new JsonDocumentOptions { MaxDepth = 4 }, cancellationToken);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            return (null, ShapeError());
        }

        string? email = null;
        string? password = null;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in document.RootElement.EnumerateObject())
        {
            if (!seen.Add(property.Name) || property.Name is not ("email" or "password")
                || property.Value.ValueKind != JsonValueKind.String)
            {
                return (null, ShapeError());
            }

            if (property.Name == "email") email = property.Value.GetString();
            else password = property.Value.GetString();
        }

        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        if (!IsValidEmail(email))
        {
            errors["email"] = ["Email must be a valid address of at most 254 characters with no surrounding whitespace."];
        }
        if (!LinguaDeskPasswordValidator.TryCountScalars(password, out var passwordLength)
            || passwordLength < LinguaDeskPasswordValidator.MinimumScalarLength
            || passwordLength > LinguaDeskPasswordValidator.MaximumScalarLength)
        {
            errors["password"] = ["Password must contain 15 to 128 well-formed Unicode characters."];
        }

        return errors.Count == 0
            ? (new RegistrationRequest(email!, password!), null)
            : (null, errors);
    }

    private static bool IsValidEmail(string? value) =>
        value is not null
        && value.Length > 0
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

    private static Dictionary<string, string[]> ShapeError() =>
        new(StringComparer.Ordinal) { ["email"] = ["Only the required email and password fields are accepted."] };

    private static IResult Problem(
        int status,
        string title,
        string detail,
        string correlationId,
        Dictionary<string, string[]>? errors = null,
        string category = "invalidRequest") => Results.Json(
            new RegistrationProblemDetails
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
