using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Routing;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();
builder.Services.AddProblemDetails();

var app = builder.Build();

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

app.Run();

public partial class Program;
