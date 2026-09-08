using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Routing;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseStaticFiles();

app.Use(static async (context, next) =>
{
    var path = context.Request.Path;
    var isUnknownHealthPath = path.StartsWithSegments("/health")
        && !path.Equals("/health/live", StringComparison.OrdinalIgnoreCase);

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

static IResult ApiNotFound() => Results.Problem(
    detail: "The requested API endpoint does not exist.",
    statusCode: StatusCodes.Status404NotFound,
    title: "Not Found");

app.Map("/api", ApiNotFound);
app.Map("/api/{**path}", ApiNotFound);

app.MapFallbackToFile("{**path:nonfile}", "index.html");

app.Run();

public partial class Program;
