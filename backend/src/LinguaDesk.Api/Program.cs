using System.Reflection;
using LinguaDesk.Api.Infrastructure.Telemetry;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Routing;
using LinguaDesk.Api.Features.Capabilities;
using LinguaDesk.Api.Features.Identity;
using LinguaDesk.Api.Features.Operations;
using LinguaDesk.Api.Features.Identity.Registration;
using LinguaDesk.Api.Features.Identity.Bearer;
using LinguaDesk.Api.Features.Identity.Session;
using LinguaDesk.Api.Features.Identity.Recovery;
using LinguaDesk.Api.Features.Identity.Verification;
using LinguaDesk.Api.Infrastructure.Persistence;
using LinguaDesk.Api.Infrastructure.Readiness;
using LinguaDesk.Api.Infrastructure.Security;
using LinguaDesk.Api.Infrastructure.Serving;
using LinguaDesk.Api.Infrastructure.Smoke;

if (args is ["--prepare-deployment"])
{
    var preparation = WebApplication.CreateBuilder(Array.Empty<string>());
    HostedDeploymentGuard.Validate(preparation.Configuration, preparation.Environment, false);
    await DeploymentPreparation.PrepareAsync(preparation.Configuration, preparation.Environment.ContentRootPath);
    if (preparation.Configuration.GetValue<bool>("Deployment:AzureAppService")) DeploymentRelease.Archive();
    return;
}

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
builder.Logging.AddFilter("Microsoft.AspNetCore.Hosting.Diagnostics", LogLevel.Warning);

HostedDeploymentGuard.Validate(builder.Configuration, builder.Environment, isContractGeneration);
builder.Services.AddDeploymentTelemetry(builder.Configuration, builder.Environment);
builder.Services.AddProblemDetails();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddLinguaDeskOpenApi();
builder.Services.AddLinguaDeskPersistence(
    builder.Configuration,
    builder.Environment,
    isContractGeneration);
builder.Services.AddLinguaDeskAccounts();
builder.Services.AddLinguaDeskOperations(builder.Configuration);
builder.Services.AddSmokeDeterministicTranslationProvider(builder.Environment);
builder.Services.AddSmokeDeterministicRewritingProvider(builder.Environment);
builder.Services.AddLinguaDeskSecurity(builder.Configuration, isContractGeneration);
builder.Services.AddLinguaDeskReadiness(builder.Environment, isContractGeneration);

using var deploymentLease = !isContractGeneration && builder.Configuration.GetValue<bool>("Deployment:AzureAppService")
    ? new FileStream(Path.Combine(Path.GetDirectoryName(builder.Configuration["Storage:DatabasePath"])!, ".runtime.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None)
    : null;
var app = builder.Build();
if (deploymentLease is not null) app.Lifetime.ApplicationStarted.Register(DeploymentRelease.MarkReady);

if (SmokeAccountSeeder.IsRequested())
{
    await SmokeAccountSeeder.SeedAsync(app);
    await app.DisposeAsync();
    return;
}

app.Use(DeploymentTelemetry.ObserveRequest);
app.UseStaticFiles();
app.UseForwardedHeaders();
app.UseAuthentication();
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
app.MapOperations();
app.MapRegistration();
app.MapAccountVerification();
app.MapAccountRecovery();
app.MapAccountSession();
app.MapAccountBearer();

static IResult ApiNotFound() => Results.Problem(
    detail: "The requested API endpoint does not exist.",
    statusCode: StatusCodes.Status404NotFound,
    title: "Not Found");

app.Map("/api", ApiNotFound);
app.Map("/api/{**path}", ApiNotFound);

app.MapFallbackToFile("{**path:nonfile}", "index.html");

ServingStartupGuard.ThrowWhenServingUnconfigured(app.Configuration, app.Environment, isContractGeneration);
MonetaryAdmissionStartupGuard.ThrowWhenMonetaryUnconfigured(app.Configuration, app.Environment, isContractGeneration);

app.Run();

public partial class Program;
