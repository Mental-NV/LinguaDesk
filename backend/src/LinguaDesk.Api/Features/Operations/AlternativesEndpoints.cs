using System.Security.Claims;
using System.Text.Json;
using LinguaDesk.Api.Features.Identity;
using LinguaDesk.Core;
using LinguaDesk.Infrastructure.Ai;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LinguaDesk.Api.Features.Operations;

public static partial class OperationsEndpoints
{
    private static readonly string[] RequiredAlternativesFields = ["operationId", "context", "start", "length", "association", "language"];

    private static void MapAlternatives(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/rewriting/alternatives", SubmitAlternativesAsync)
            .WithName("submitSentenceAlternatives").WithGroupName("linguadesk")
            .WithSummary("Generate two contextual alternatives for one complete sentence")
            .WithDescription("Exact result context and Unicode scalar span; selected sentence charged once on success. Status/replay are metadata-only. Deadline 15 seconds; at most three provider dispatches.")
            .Accepts<AlternativesRequest>("application/json")
            .Produces<AlternativesSuccessResponse>(201).Produces<OperationStatusResponse>(200).Produces<OperationPendingResponse>(202)
            .Produces<OperationProblemDetails>(400, "application/problem+json")
            .Produces<OperationProblemDetails>(401, "application/problem+json")
            .Produces<OperationProblemDetails>(403, "application/problem+json")
            .Produces<OperationProblemDetails>(409, "application/problem+json")
            .Produces<OperationProblemDetails>(410, "application/problem+json")
            .Produces<OperationProblemDetails>(413, "application/problem+json")
            .Produces<OperationProblemDetails>(415, "application/problem+json")
            .Produces<OperationProblemDetails>(422, "application/problem+json")
            .Produces<OperationProblemDetails>(429, "application/problem+json")
            .Produces<OperationProblemDetails>(503, "application/problem+json")
            .Produces<OperationProblemDetails>(504, "application/problem+json")
            .RequireAuthorization(VerifiedAccountAuthorization.PolicyName);
        endpoints.MapMethods("/api/rewriting/alternatives", ["GET", "HEAD", "PUT", "PATCH", "DELETE", "OPTIONS"], (HttpContext context) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            context.Response.Headers.Allow = "POST";
            return Results.StatusCode(405);
        }).ExcludeFromDescription();
    }

    private static async Task<IResult> SubmitAlternativesAsync(HttpContext context, OperationAdmissionService admissions,
        AlternativesOperationCoordinator coordinator, IOptions<MonetaryAdmissionOptions> monetaryOptions,
        TimeProvider timeProvider, CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        var correlation = context.TraceIdentifier;
        if (context.Request.QueryString.HasValue) return Problem(400, "Invalid request", "No query parameters are accepted.", correlation, "invalidRequest");
        if (!IsUtf8Json(context.Request.ContentType)) return Problem(415, "Unsupported Media Type", "Use UTF-8 application/json.", correlation, "invalidRequest");
        AlternativesRequest? request;
        try
        {
            using var body = new MemoryStream();
            var buffer = new byte[8192];
            int read;
            while ((read = await context.Request.Body.ReadAsync(buffer, cancellationToken)) > 0)
            {
                if (body.Length + read > AlternativesInputPolicy.RequestBodyBytes)
                    return Problem(413, "Request too large", "The request exceeds 128 KiB.", correlation, "invalidRequest");
                body.Write(buffer, 0, read);
            }
            using var document = JsonDocument.Parse(body.ToArray(), new JsonDocumentOptions { MaxDepth = 4 });
            request = ParseAlternatives(document.RootElement);
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or ArgumentException or FormatException or OverflowException)
        {
            return Problem(400, "Invalid request", "The request body is malformed.", correlation, "invalidRequest");
        }
        if (request is null) return Problem(400, "Invalid request", "Only operationId, context, start, length, association, language and optional mode are accepted; all other fields are required.", correlation, "invalidRequest");
        var identity = OperationIdentity.Assess(request.OperationId.ToString("D"), timeProvider.GetUtcNow());
        if (identity.Status is OperationIdentityStatus.Malformed or OperationIdentityStatus.FutureSkew)
            return Problem(400, "Invalid operation identity", "Use a current UUIDv7.", correlation, "invalidIdentity", timeProvider.GetUtcNow());
        if (identity.Status == OperationIdentityStatus.Expired)
            return Problem(410, "Operation identity expired", "The identity recovery window has ended.", correlation, "identityExpired", timeProvider.GetUtcNow());
        var accountId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (accountId is null) return Problem(401, "Authentication required", "Sign in to continue.", correlation, "authenticationRequired");
        try
        {
            var admitted = await admissions.AdmitAsync(accountId, OperationAdmissionService.FamilyRewriting, request.Context,
                request.Language, null, request.Mode, request.OperationId, cancellationToken,
                LedgerSnapshot.ResolveCapOrZero(monetaryOptions), request);
            if (admitted.Outcome != AdmissionOutcome.Admitted) return MapAdmission(admitted, correlation);
            if (admitted.Usage.Availability == UsageAvailability.Unavailable)
                return Problem(503, "Service unavailable", "Processing availability could not be established.", correlation, "availability");
            var result = await coordinator.ExecuteAsync(accountId, request, admitted.Submission!, cancellationToken);
            var settled = result.Settlement;
            if (result.Decision == ChainDecision.Succeeded)
                return Results.Json(new AlternativesSuccessResponse(request.OperationId, OperationFamily.Rewriting, OperationKind.SentenceAlternatives,
                    OperationStatus.Succeeded, result.Options!, request.Association, request.Start, request.Length, LinguaDesk.Api.Features.Capabilities.CapabilitiesDocument.ToLanguageId(request.Language),
                    request.Mode ?? ProductCatalog.DefaultRewritingMode, settled.Submission!.ScalarCount, settled.Submission.AdmissionDay,
                    settled.Submission.DeadlineUtc, settled.ServerTime, ToUsage(settled.Usage)), statusCode: 201);
            if (result.Decision == ChainDecision.Ineligible)
                return Problem(422, "Input not eligible", "The context is not eligible; no character charge was made.", correlation, "inputEligibility", reason: result.Category);
            var deadline = result.Decision is ChainDecision.DeadlineExceeded or ChainDecision.Cancelled;
            return Problem(deadline ? 504 : 503, deadline ? "Deadline exceeded" : "Alternatives unavailable",
                "The alternatives could not be completed; no character charge was made.", correlation,
                result.Decision == ChainDecision.BudgetDenied ? "monetarySuspension" : deadline ? "deadlineExceeded" : "processingFailure");
        }
        catch (Exception exception) when (exception is SqliteException or DbUpdateException)
        {
            return Problem(503, "Service unavailable", "Operation submission is temporarily unavailable.", correlation, "availability");
        }
    }

    private static AlternativesRequest? ParseAlternatives(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object) return null;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in root.EnumerateObject())
        {
            if (!seen.Add(property.Name)) return null;
            if (property.Name is "start" or "length")
            {
                if (property.Value.ValueKind != JsonValueKind.Number || !property.Value.TryGetInt32(out _)) return null;
            }
            else if (property.Name is not ("operationId" or "context" or "association" or "language" or "mode") || property.Value.ValueKind != JsonValueKind.String) return null;
        }
        if (!RequiredAlternativesFields.All(seen.Contains)) return null;
        return new(root.GetProperty("operationId").GetGuid(), root.GetProperty("context").GetString()!,
            root.GetProperty("start").GetInt32(), root.GetProperty("length").GetInt32(), root.GetProperty("association").GetGuid(),
            root.GetProperty("language").GetString()!, seen.Contains("mode") ? root.GetProperty("mode").GetString()! : ProductCatalog.DefaultRewritingMode);
    }
}
