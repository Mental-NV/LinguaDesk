using Microsoft.AspNetCore.Http.HttpResults;

namespace LinguaDesk.Api.Features.Capabilities;

public static class CapabilitiesEndpoints
{
    public const string OpenApiDocumentName = "linguadesk";

    public static IEndpointRouteBuilder MapCapabilities(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/capabilities", GetCapabilities)
            .WithName("getCapabilities")
            .WithGroupName(OpenApiDocumentName)
            .WithSummary("Get accepted language and input capabilities")
            .WithDescription(
                "Returns public catalog, input-counting, whole-input limit, deadline, and retry-identity metadata. " +
                "The catalog does not imply that language-operation, account, usage, or status endpoints are implemented.")
            .Produces<CapabilitiesResponse>(StatusCodes.Status200OK, "application/json");
        endpoints.MapPost("/api/capabilities", RejectUnsupportedMethod)
            .ExcludeFromDescription();

        return endpoints;
    }

    private static Ok<CapabilitiesResponse> GetCapabilities(HttpContext context, TimeProvider timeProvider)
    {
        context.Response.Headers.CacheControl = "no-store";
        return TypedResults.Ok(CapabilitiesDocument.Create(timeProvider.GetUtcNow()));
    }

    private static StatusCodeHttpResult RejectUnsupportedMethod(HttpContext context)
    {
        context.Response.Headers.Allow = HttpMethods.Get;
        return TypedResults.StatusCode(StatusCodes.Status405MethodNotAllowed);
    }
}
