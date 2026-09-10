using System.Reflection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Routing;
using LinguaDesk.Api.Features.Capabilities;
using LinguaDesk.Api.Features.Identity;
using LinguaDesk.Api.Features.Identity.Registration;
using LinguaDesk.Api.Features.Identity.Verification;
using LinguaDesk.Api.Infrastructure.Persistence;
using LinguaDesk.Api.Infrastructure.Readiness;
using LinguaDesk.Api.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);
var isContractGeneration =
    string.Equals(
        Environment.GetEnvironmentVariable("LINGUADESK_OPENAPI_GENERATION"),
        "1",
        StringComparison.Ordinal)
    && string.Equals(
        Assembly.GetEntryAssembly()?.GetName().Name,
        "GetDocument.Insider",
        StringComparison.Ordinal);
builder.Logging.AddFilter(
    "Microsoft.AspNetCore.DataProtection.Repositories.FileSystemXmlRepository",
    LogLevel.Warning);

builder.Services.AddProblemDetails();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddLinguaDeskOpenApi();
builder.Services.AddLinguaDeskPersistence(
    builder.Configuration,
    builder.Environment,
    isContractGeneration);
builder.Services.AddLinguaDeskSecurity(builder.Configuration, isContractGeneration);
builder.Services.AddLinguaDeskAccounts();
builder.Services.AddLinguaDeskReadiness(builder.Environment, isContractGeneration);

var app = builder.Build();

app.UseStaticFiles();
app.UseAuthorization();

app.Use(static async (context, next) =>
{
    var path = context.Request.Path;
    var isUnknownHealthPath = path.StartsWithSegments("/health")
        && !path.Equals("/health/live", StringComparison.OrdinalIgnoreCase)
        && !path.Equals("/health/ready", StringComparison.OrdinalIgnoreCase);

    if (path.StartsWithSegments("/assets") || isUnknownHealthPath)
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    await next(context);
});

app.MapHealthChecks(
        "/health/live",
        new HealthCheckOptions
        {
            Predicate = static _ => false,
        })
    .WithMetadata(new HttpMethodMetadata([HttpMethods.Get]));
app.MapHealthChecks(
        "/health/ready",
        new HealthCheckOptions
        {
            Predicate = static check => check.Tags.Contains("ready"),
        })
    .WithMetadata(new HttpMethodMetadata([HttpMethods.Get]))
    .ExcludeFromDescription();

app.MapCapabilities();
app.MapRegistration();
app.MapAccountVerification();

static IResult ApiNotFound() => Results.Problem(
    detail: "The requested API endpoint does not exist.",
    statusCode: StatusCodes.Status404NotFound,
    title: "Not Found");

app.Map("/api", ApiNotFound);
app.Map("/api/{**path}", ApiNotFound);

app.MapFallbackToFile("{**path:nonfile}", "index.html");

app.Run();

public partial class Program;
