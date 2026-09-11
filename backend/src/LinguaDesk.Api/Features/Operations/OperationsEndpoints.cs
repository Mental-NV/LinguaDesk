using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using LinguaDesk.Api.Features.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LinguaDesk.Api.Features.Operations;

public static class OperationsEndpoints
{
    public static IEndpointRouteBuilder MapOperations(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/operations", SubmitAsync)
            .WithName("submitLanguageOperation")
            .WithGroupName("linguadesk")
            .WithSummary("Submit one logical language operation")
            .WithDescription(
                "Validates, fingerprint-matches and atomically reserves exactly one logical operation per verified " +
                "account identity against both daily character allowances. Identical identities observe the pending " +
                "reservation or the settled success/failure/interrupted metadata; changed payloads conflict. " +
                "Translation submissions execute synchronously through the configured provider behind the stored deadline " +
                "and return complete text or a classified failure; rewriting submissions remain pending reservations until M027.")
            .Accepts<SubmitOperationRequest>("application/json")
            .Produces<TranslationSuccessResponse>(StatusCodes.Status201Created, "application/json")
            .Produces<OperationPendingResponse>(StatusCodes.Status202Accepted, "application/json")
            .Produces<OperationStatusResponse>(StatusCodes.Status200OK, "application/json")
            .Produces<OperationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")
            .Produces<OperationProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")
            .Produces<OperationProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")
            .Produces(StatusCodes.Status405MethodNotAllowed)
            .Produces<OperationProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")
            .Produces<OperationProblemDetails>(StatusCodes.Status410Gone, "application/problem+json")
            .Produces<OperationProblemDetails>(StatusCodes.Status415UnsupportedMediaType, "application/problem+json")
            .Produces<OperationProblemDetails>(StatusCodes.Status422UnprocessableEntity, "application/problem+json")
            .Produces<OperationProblemDetails>(StatusCodes.Status429TooManyRequests, "application/problem+json")
            .Produces<OperationProblemDetails>(StatusCodes.Status503ServiceUnavailable, "application/problem+json")
            .Produces<OperationProblemDetails>(StatusCodes.Status504GatewayTimeout, "application/problem+json")
            .RequireAuthorization(VerifiedAccountAuthorization.PolicyName);
        endpoints.MapPost("/api/operations", static (HttpContext context) =>
            {
                context.Response.Headers.CacheControl = "no-store";
                return Results.Json(
                    new OperationProblemDetails
                    {
                        Type = "about:blank",
                        Title = "Unsupported Media Type",
                        Status = StatusCodes.Status415UnsupportedMediaType,
                        Detail = "The request must use UTF-8 application/json.",
                        Category = "invalidRequest",
                        CorrelationId = context.TraceIdentifier,
                    },
                    contentType: "application/problem+json",
                    statusCode: StatusCodes.Status415UnsupportedMediaType);
            })
            .ExcludeFromDescription()
            .RequireAuthorization(VerifiedAccountAuthorization.PolicyName);
        endpoints.MapMethods("/api/operations", [
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

        endpoints.MapGet("/api/operations/{operationId}", GetStatusAsync)
            .WithName("getLanguageOperationStatus")
            .WithGroupName("linguadesk")
            .WithSummary("Read one submitted operation")
            .WithDescription(
                "Returns the pending reservation or settled success/failure/interrupted metadata for an account-owned operation " +
                "identity with a fresh current-day usage snapshot. Status reads never dispatch work.")
            .Produces<OperationStatusResponse>(StatusCodes.Status200OK, "application/json")
            .Produces<OperationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")
            .Produces<OperationProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")
            .Produces<OperationProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")
            .Produces<OperationProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")
            .Produces(StatusCodes.Status405MethodNotAllowed)
            .Produces<OperationProblemDetails>(StatusCodes.Status410Gone, "application/problem+json")
            .Produces<OperationProblemDetails>(StatusCodes.Status503ServiceUnavailable, "application/problem+json")
            .RequireAuthorization(VerifiedAccountAuthorization.PolicyName);
        endpoints.MapMethods("/api/operations/{operationId}", [
            HttpMethods.Post,
            HttpMethods.Head,
            HttpMethods.Put,
            HttpMethods.Patch,
            HttpMethods.Delete,
            HttpMethods.Options,
        ], static (HttpContext context) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            context.Response.Headers.Allow = HttpMethods.Get;
            return Results.StatusCode(StatusCodes.Status405MethodNotAllowed);
        }).ExcludeFromDescription();

        endpoints.MapGet("/api/usage", GetUsageAsync)
            .WithName("getCurrentUsage")
            .WithGroupName("linguadesk")
            .WithSummary("Read authoritative current-day usage")
            .WithDescription(
                "Returns the authoritative current-day user usage snapshot with a categorical availability signal. " +
                "Usage reads never charge allowances, reserve exposure or dispatch work.")
            .Produces<UsageSnapshot>(StatusCodes.Status200OK, "application/json")
            .Produces<OperationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")
            .Produces<OperationProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")
            .Produces<OperationProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")
            .Produces(StatusCodes.Status405MethodNotAllowed)
            .Produces<OperationProblemDetails>(StatusCodes.Status503ServiceUnavailable, "application/problem+json")
            .RequireAuthorization(VerifiedAccountAuthorization.PolicyName);
        endpoints.MapMethods("/api/usage", [
            HttpMethods.Post,
            HttpMethods.Head,
            HttpMethods.Put,
            HttpMethods.Patch,
            HttpMethods.Delete,
            HttpMethods.Options,
        ], static (HttpContext context) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            context.Response.Headers.Allow = HttpMethods.Get;
            return Results.StatusCode(StatusCodes.Status405MethodNotAllowed);
        }).ExcludeFromDescription();

        return endpoints;
    }

    private static async Task<IResult> SubmitAsync(
        HttpContext context,
        OperationAdmissionService admissions,
        TranslationOperationCoordinator translator,
        IOptions<MonetaryAdmissionOptions> monetaryOptions,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        var correlationId = context.TraceIdentifier;
        if (context.Request.QueryString.HasValue)
        {
            return Problem(
                StatusCodes.Status400BadRequest,
                "Invalid request",
                "Operation submission accepts no query parameters.",
                correlationId,
                "invalidRequest");
        }

        if (!IsUtf8Json(context.Request.ContentType))
        {
            return Problem(
                StatusCodes.Status415UnsupportedMediaType,
                "Unsupported Media Type",
                "The request must use UTF-8 application/json.",
                correlationId,
                "invalidRequest");
        }

        ParsedSubmission? parsed;
        try
        {
            parsed = await ParseSubmissionAsync(context.Request.Body, cancellationToken);
        }
        catch (JsonException)
        {
            return Problem(StatusCodes.Status400BadRequest, "Invalid request", "The JSON request body is malformed.", correlationId, "invalidRequest");
        }
        catch (DecoderFallbackException)
        {
            return Problem(StatusCodes.Status400BadRequest, "Invalid request", "The JSON request body is malformed.", correlationId, "invalidRequest");
        }
        catch (InvalidOperationException exception) when (exception.InnerException is DecoderFallbackException)
        {
            return Problem(StatusCodes.Status400BadRequest, "Invalid request", "The JSON request body is malformed.", correlationId, "invalidRequest");
        }

        if (parsed is null)
        {
            return Problem(
                StatusCodes.Status400BadRequest,
                "Invalid request",
                "Only the operationId, family, source, sourceSelection, target and mode string fields are accepted; operationId, family and source are required.",
                correlationId,
                "invalidRequest");
        }

        if (parsed.Family is not (OperationAdmissionService.FamilyTranslation or OperationAdmissionService.FamilyRewriting))
        {
            return Problem(
                StatusCodes.Status400BadRequest,
                "Invalid request",
                "The operation family must be translation or rewriting.",
                correlationId,
                "invalidRequest");
        }

        var serverTime = timeProvider.GetUtcNow();
        var identity = OperationIdentity.Assess(parsed.OperationId, serverTime);
        if (identity.Status is OperationIdentityStatus.Malformed or OperationIdentityStatus.FutureSkew)
        {
            return Problem(
                StatusCodes.Status400BadRequest,
                "Invalid operation identity",
                "The operation identity must be a UUIDv7 created within the accepted clock skew.",
                correlationId,
                "invalidIdentity",
                serverTimeUtc: serverTime);
        }

        if (identity.Status == OperationIdentityStatus.Expired)
        {
            return Problem(
                StatusCodes.Status410Gone,
                "Operation identity expired",
                "The operation identity validity window has ended; create a new submission identity.",
                correlationId,
                "identityExpired",
                serverTimeUtc: serverTime);
        }

        var accountId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(accountId))
        {
            return Problem(
                StatusCodes.Status401Unauthorized,
                "Authentication required",
                "Authentication is required to submit a language operation.",
                correlationId,
                "authenticationRequired");
        }

        AdmissionResult outcome;
        try
        {
            outcome = await admissions.AdmitAsync(
                accountId,
                parsed.Family,
                parsed.Source,
                parsed.SourceSelection,
                parsed.Target,
                parsed.Mode,
                identity.OperationId,
                cancellationToken,
                LedgerSnapshot.ResolveCapOrZero(monetaryOptions));
        }
        catch (Exception exception) when (exception is SqliteException or DbUpdateException)
        {
            return Problem(
                StatusCodes.Status503ServiceUnavailable,
                "Service unavailable",
                "Operation submission is temporarily unavailable.",
                correlationId,
                "availability");
        }

        if ((outcome.Outcome is AdmissionOutcome.Admitted or AdmissionOutcome.DuplicateObserved)
            && outcome.Usage.Availability == UsageAvailability.Unavailable)
        {
            return Problem(
                StatusCodes.Status503ServiceUnavailable,
                "Service unavailable",
                "Operation submission is temporarily unavailable.",
                correlationId,
                "availability");
        }

        if (outcome.Outcome == AdmissionOutcome.Admitted
            && string.Equals(parsed.Family, OperationAdmissionService.FamilyTranslation, StringComparison.Ordinal))
        {
            var execution = await translator.ExecuteAdmittedAsync(
                accountId,
                parsed.Source,
                parsed.SourceSelection,
                parsed.Target,
                identity.OperationId,
                outcome.Submission!,
                outcome.Usage,
                outcome.ServerTime,
                cancellationToken);
            return MapTranslationExecution(execution, correlationId);
        }

        return outcome.Outcome switch
        {
            AdmissionOutcome.Admitted => Results.Json(
                ToPending(outcome.Submission!, outcome.Usage, outcome.ServerTime),
                statusCode: StatusCodes.Status202Accepted),
            AdmissionOutcome.DuplicateObserved => ToDuplicate(outcome.Submission!, outcome.Usage, outcome.ServerTime),
            AdmissionOutcome.IdentityConflict => Problem(
                StatusCodes.Status409Conflict,
                "Operation identity conflict",
                "The operation identity is already claimed by a different payload; reuse the original payload or create a new identity.",
                correlationId,
                "identityConflict",
                serverTimeUtc: outcome.ServerTime),
            AdmissionOutcome.IdentityExpired => Problem(
                StatusCodes.Status410Gone,
                "Operation identity expired",
                "The operation identity validity window has ended; create a new submission identity.",
                correlationId,
                "identityExpired",
                serverTimeUtc: outcome.ServerTime),
            AdmissionOutcome.InputIneligible => Problem(
                StatusCodes.Status422UnprocessableEntity,
                "Input not eligible",
                "The operation source or settings are not eligible for admission; no operation was admitted.",
                correlationId,
                "inputEligibility",
                reason: outcome.EligibilityReason,
                characterCount: outcome.CharacterCount,
                limit: outcome.SourceLimit),
            AdmissionOutcome.InsufficientUserCapacity => Problem(
                StatusCodes.Status429TooManyRequests,
                "User allowance exhausted",
                "The user daily character allowance has insufficient remaining capacity; no operation was admitted.",
                correlationId,
                "userAllowance",
                serverTimeUtc: outcome.ServerTime,
                resetAtUtc: OperationAdmissionService.NextMidnightUtc(outcome.ServerTime),
                characterCount: outcome.CharacterCount,
                limit: outcome.SourceLimit),
            AdmissionOutcome.InsufficientGlobalCapacity => Problem(
                StatusCodes.Status429TooManyRequests,
                "Service allowance exhausted",
                "The service is temporarily out of daily character capacity; no operation was admitted.",
                correlationId,
                "globalAllowance",
                serverTimeUtc: outcome.ServerTime,
                resetAtUtc: OperationAdmissionService.NextMidnightUtc(outcome.ServerTime),
                characterCount: outcome.CharacterCount,
                limit: outcome.SourceLimit),
            _ => Problem(
                StatusCodes.Status503ServiceUnavailable,
                "Service unavailable",
                "Operation submission is temporarily unavailable.",
                correlationId,
                "availability"),
        };
    }

    private static async Task<IResult> GetStatusAsync(
        HttpContext context,
        string operationId,
        OperationAdmissionService admissions,
        IOptions<MonetaryAdmissionOptions> monetaryOptions,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        var correlationId = context.TraceIdentifier;
        if (context.Request.QueryString.HasValue)
        {
            return Problem(
                StatusCodes.Status400BadRequest,
                "Invalid request",
                "Operation status reads accept no query parameters.",
                correlationId,
                "invalidRequest");
        }

        var serverTime = timeProvider.GetUtcNow();
        var identity = OperationIdentity.Assess(operationId, serverTime);
        if (identity.Status is OperationIdentityStatus.Malformed or OperationIdentityStatus.FutureSkew)
        {
            return Problem(
                StatusCodes.Status400BadRequest,
                "Invalid operation identity",
                "The operation identity must be a UUIDv7 created within the accepted clock skew.",
                correlationId,
                "invalidIdentity",
                serverTimeUtc: serverTime);
        }

        if (identity.Status == OperationIdentityStatus.Expired)
        {
            return Problem(
                StatusCodes.Status410Gone,
                "Operation identity expired",
                "The operation identity validity window has ended; status is no longer available.",
                correlationId,
                "identityExpired",
                serverTimeUtc: serverTime);
        }

        var accountId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(accountId))
        {
            return Problem(
                StatusCodes.Status401Unauthorized,
                "Authentication required",
                "Authentication is required to read a language operation.",
                correlationId,
                "authenticationRequired");
        }

        try
        {
            var (submission, usage, observedTime) =
                await admissions.GetAsync(
                    accountId,
                    identity.OperationId,
                    cancellationToken,
                    LedgerSnapshot.ResolveCapOrZero(monetaryOptions));
            if (submission is null)
            {
                return Problem(
                    StatusCodes.Status404NotFound,
                    "Operation not found",
                    "No account-owned operation uses this identity.",
                    correlationId,
                    "unknownOperation");
            }

            if (usage.Availability == UsageAvailability.Unavailable)
            {
                return Problem(
                    StatusCodes.Status503ServiceUnavailable,
                    "Service unavailable",
                    "Operation status is temporarily unavailable.",
                    correlationId,
                    "availability");
            }

            return ToTerminal(submission, usage, observedTime);
        }
        catch (Exception exception) when (exception is SqliteException or DbUpdateException)
        {
            return Problem(
                StatusCodes.Status503ServiceUnavailable,
                "Service unavailable",
                "Operation status is temporarily unavailable.",
                correlationId,
                "availability");
        }
    }

    private static async Task<IResult> GetUsageAsync(
        HttpContext context,
        OperationAdmissionService admissions,
        IOptions<MonetaryAdmissionOptions> monetaryOptions,
        CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        var correlationId = context.TraceIdentifier;
        if (context.Request.QueryString.HasValue)
        {
            return Problem(
                StatusCodes.Status400BadRequest,
                "Invalid request",
                "Usage reads accept no query parameters.",
                correlationId,
                "invalidRequest");
        }

        var accountId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(accountId))
        {
            return Problem(
                StatusCodes.Status401Unauthorized,
                "Authentication required",
                "Authentication is required to read current usage.",
                correlationId,
                "authenticationRequired");
        }

        try
        {
            var (usage, _) = await admissions.GetUsageAsync(
                accountId,
                cancellationToken,
                LedgerSnapshot.ResolveCapOrZero(monetaryOptions));
            if (usage.Availability == UsageAvailability.Unavailable)
            {
                return Problem(
                    StatusCodes.Status503ServiceUnavailable,
                    "Service unavailable",
                    "Current usage is temporarily unavailable.",
                    correlationId,
                    "availability");
            }

            return Results.Json(ToUsage(usage), statusCode: StatusCodes.Status200OK);
        }
        catch (Exception exception) when (exception is SqliteException or DbUpdateException)
        {
            return Problem(
                StatusCodes.Status503ServiceUnavailable,
                "Service unavailable",
                "Current usage is temporarily unavailable.",
                correlationId,
                "availability");
        }
    }

    internal static IResult MapTranslationExecution(
        TranslationExecutionResult execution,
        string correlationId) =>
        execution.Outcome switch
        {
            TranslationExecutionOutcome.Succeeded => Results.Json(
                ToSuccess(execution),
                statusCode: StatusCodes.Status201Created),
            TranslationExecutionOutcome.ProviderUnavailable => Results.Json(
                ToPending(execution.Submission!, execution.Usage, execution.ServerTime),
                statusCode: StatusCodes.Status202Accepted),
            TranslationExecutionOutcome.InputEligibilityRejected => Problem(
                StatusCodes.Status422UnprocessableEntity,
                "Input not eligible",
                "The translation provider classified the source as not eligible for transformation; no character charge was made.",
                correlationId,
                "inputEligibility",
                reason: execution.EligibilityReason,
                characterCount: execution.Submission?.ScalarCount),
            TranslationExecutionOutcome.MonetarySuspended => Problem(
                MonetaryAdmissionProblem.SuspensionStatus,
                MonetaryAdmissionProblem.Describe(MonetaryAdmissionOutcome.DeniedOverCap).Title,
                MonetaryAdmissionProblem.Describe(MonetaryAdmissionOutcome.DeniedOverCap).Detail,
                correlationId,
                MonetaryAdmissionProblem.SuspensionCategory),
            TranslationExecutionOutcome.DeadlineExceeded or TranslationExecutionOutcome.Cancelled => Problem(
                StatusCodes.Status504GatewayTimeout,
                "Translation deadline exceeded",
                "The translation did not complete within the server-established deadline; no character charge was made.",
                correlationId,
                TranslationProblemCategories.DeadlineExceeded),
            _ => Problem(
                StatusCodes.Status503ServiceUnavailable,
                "Translation unavailable",
                "The translation could not be completed; no character charge was made.",
                correlationId,
                TranslationProblemCategories.ProcessingFailure),
        };

    internal static TranslationSuccessResponse ToSuccess(TranslationExecutionResult execution) => new(
        Guid.Parse(execution.Submission!.OperationId),
        ToFamily(execution.Submission),
        OperationStatus.Succeeded,
        execution.TranslatedText!,
        execution.Submission.ScalarCount,
        execution.Submission.AdmissionDay,
        execution.Submission.DeadlineUtc,
        execution.ServerTime,
        ToUsage(execution.Usage));

    internal static IResult ToDuplicate(
        OperationSubmission submission,
        UsageSnapshotData usage,
        DateTimeOffset serverTime) =>
        string.Equals(submission.State, OperationStates.Pending, StringComparison.Ordinal)
            ? Results.Json(ToPending(submission, usage, serverTime), statusCode: StatusCodes.Status202Accepted)
            : ToTerminal(submission, usage, serverTime);

    internal static IResult ToTerminal(
        OperationSubmission submission,
        UsageSnapshotData usage,
        DateTimeOffset serverTime) =>
        Results.Json(ToStatus(submission, usage, serverTime), statusCode: StatusCodes.Status200OK);

    internal static OperationPendingResponse ToPending(
        OperationSubmission submission,
        UsageSnapshotData usage,
        DateTimeOffset serverTime) => new(
            Guid.Parse(submission.OperationId),
            ToFamily(submission),
            OperationStatus.Pending,
            submission.ScalarCount,
            submission.AdmissionDay,
            submission.DeadlineUtc,
            serverTime,
            ToUsage(usage));

    internal static OperationStatusResponse ToStatus(
        OperationSubmission submission,
        UsageSnapshotData usage,
        DateTimeOffset serverTime) => new(
            Guid.Parse(submission.OperationId),
            ToFamily(submission),
            ToOperationStatus(submission),
            string.Equals(submission.State, OperationStates.Succeeded, StringComparison.Ordinal)
                || string.Equals(submission.State, OperationStates.Pending, StringComparison.Ordinal)
                ? submission.ScalarCount
                : 0,
            submission.AdmissionDay,
            submission.DeadlineUtc,
            false,
            serverTime,
            ToUsage(usage));

    private static OperationStatus ToOperationStatus(OperationSubmission submission)
    {
        if (string.Equals(submission.State, OperationStates.Succeeded, StringComparison.Ordinal))
        {
            return OperationStatus.Succeeded;
        }

        if (string.Equals(submission.State, OperationStates.Failed, StringComparison.Ordinal))
        {
            return OperationStatus.Failed;
        }

        return string.Equals(submission.State, OperationStates.Interrupted, StringComparison.Ordinal)
            ? OperationStatus.Interrupted
            : OperationStatus.Pending;
    }

    private static OperationFamily ToFamily(OperationSubmission submission) =>
        string.Equals(submission.Family, OperationAdmissionService.FamilyRewriting, StringComparison.Ordinal)
            ? OperationFamily.Rewriting
            : OperationFamily.Translation;

    private static UsageSnapshot ToUsage(UsageSnapshotData usage) => new(
        usage.Day,
        usage.ResetAtUtc,
        usage.ConsumedCharacters,
        usage.ReservedCharacters,
        usage.AllowanceCharacters,
        usage.AvailableCharacters,
        usage.Revision,
        usage.Availability);

    private static async Task<ParsedSubmission?> ParseSubmissionAsync(Stream body, CancellationToken cancellationToken)
    {
        using var document = await JsonDocument.ParseAsync(body, new JsonDocumentOptions { MaxDepth = 4 }, cancellationToken);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        string? operationId = null;
        string? family = null;
        string? source = null;
        string? sourceSelection = null;
        string? target = null;
        string? mode = null;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in document.RootElement.EnumerateObject())
        {
            if (!seen.Add(property.Name)
                || property.Name is not ("operationId" or "family" or "source" or "sourceSelection" or "target" or "mode")
                || property.Value.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            var value = property.Value.GetString();
            switch (property.Name)
            {
                case "operationId": operationId = value; break;
                case "family": family = value; break;
                case "source": source = value; break;
                case "sourceSelection": sourceSelection = value; break;
                case "target": target = value; break;
                default: mode = value; break;
            }
        }

        return operationId is null || family is null || source is null
            ? null
            : new ParsedSubmission(operationId, family, source, sourceSelection, target, mode);
    }

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

    private static IResult Problem(
        int status,
        string title,
        string detail,
        string correlationId,
        string category,
        DateTimeOffset? serverTimeUtc = null,
        DateTimeOffset? resetAtUtc = null,
        string? reason = null,
        int? characterCount = null,
        int? limit = null) => Results.Json(
            new OperationProblemDetails
            {
                Type = "about:blank",
                Title = title,
                Status = status,
                Detail = detail,
                Category = category,
                CorrelationId = correlationId,
                ServerTimeUtc = serverTimeUtc,
                ResetAtUtc = resetAtUtc,
                Reason = reason,
                CharacterCount = characterCount,
                Limit = limit,
            },
            contentType: "application/problem+json",
            statusCode: status);

    private sealed record ParsedSubmission(
        string OperationId,
        string Family,
        string Source,
        string? SourceSelection,
        string? Target,
        string? Mode);
}
