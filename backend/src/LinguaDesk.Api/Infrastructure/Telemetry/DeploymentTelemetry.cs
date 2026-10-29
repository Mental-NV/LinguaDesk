using System.Diagnostics;
using Azure.Monitor.OpenTelemetry.Exporter;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace LinguaDesk.Api.Infrastructure.Telemetry;

/// <summary>Only newly constructed allowlisted activities reach Azure. Raw HTTP/SQL/log activities are never subscribed.</summary>
public static class DeploymentTelemetry
{
    public const string SourceName = "LinguaDesk.SafeTelemetry";
    public static readonly ActivitySource Source = new(SourceName);

    public static bool CanExport(IConfiguration configuration, IHostEnvironment environment) =>
        configuration.GetValue<bool>("Telemetry:Enabled")
        && environment.EnvironmentName is "Staging" or "Production"
        && !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME"))
        && !string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase);

    public static IServiceCollection AddDeploymentTelemetry(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        if (!CanExport(configuration, environment)) return services;
        var connection = configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
        if (string.IsNullOrWhiteSpace(connection)) throw new InvalidOperationException("Hosted telemetry connection is missing.");
        var ratio = configuration.GetValue<double>("Telemetry:SamplingRatio");
        if (ratio is <= 0 or > 0.1) throw new InvalidOperationException("Telemetry sampling must be positive and at most 10 percent.");
        services.AddOpenTelemetry().ConfigureResource(r => r.Clear().AddService("LinguaDesk").AddAttributes(
            new Dictionary<string, object> { ["deployment.environment.name"] = environment.EnvironmentName }))
            .WithTracing(t => t.AddSource(SourceName).SetSampler(new DeploymentSampler(ratio))
                .AddAzureMonitorTraceExporter(o => { o.ConnectionString = connection; o.DisableOfflineStorage = true; }));
        services.AddHostedService<SafeDependencyObserver>();
        return services;
    }

    internal static void SanitizeContext(Activity? activity)
    {
        if (activity is null) return;
        activity.TraceStateString = null;
        foreach (var baggage in activity.Baggage.ToArray()) activity.SetBaggage(baggage.Key, null);
    }

    public static async Task ObserveRequest(HttpContext context, Func<Task> next)
    {
        var path = context.Request.Path;
        if (path.StartsWithSegments("/health") || path.StartsWithSegments("/assets") || !path.StartsWithSegments("/api"))
        {
            await next();
            return;
        }
        var parent = Activity.Current;
        var safeParent = parent is null ? default : new ActivityContext(parent.TraceId, parent.SpanId, parent.ActivityTraceFlags);
        using var span = Source.StartActivity("api.request", ActivityKind.Server, safeParent);
        SanitizeContext(span);
        try { await next(); }
        catch (Exception)
        {
            span?.SetStatus(ActivityStatusCode.Error);
            span?.SetTag("error.type", "application.failure");
            throw;
        }
        finally
        {
            // Route pattern is application metadata; never use raw paths or query values.
            var route = (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText;
            span?.SetTag("http.route", route ?? "/api/unknown");
            span?.SetTag("http.response.status_code", context.Response.StatusCode);
            if (context.Response.StatusCode >= 500) span?.SetStatus(ActivityStatusCode.Error);
        }
    }
}

internal sealed class SafeDependencyObserver(IHostEnvironment environment) : IHostedService, IObserver<DiagnosticListener>, IObserver<KeyValuePair<string, object?>>, IDisposable
{
    private readonly List<IDisposable> subscriptions = [];
    public Task StartAsync(CancellationToken cancellationToken)
    {
        subscriptions.Add(DiagnosticListener.AllListeners.Subscribe(this));
        // Exactly one sanitized startup marker, no separate unfiltered export path.
        using var marker = DeploymentTelemetry.Source.StartActivity("deployment.marker");
        DeploymentTelemetry.SanitizeContext(marker);
        marker?.SetTag("deployment.revision", Revision());
        marker?.SetTag("deployment.environment", environment.EnvironmentName);
        return Task.CompletedTask;
    }
    private static string Revision()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "revision.json");
        if (!File.Exists(path)) return "unknown";
        using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(path));
        var sha = doc.RootElement.GetProperty("sha").GetString() ?? "";
        return sha.Length == 40 && sha.All(Uri.IsHexDigit) ? sha : "unknown";
    }
    public void OnNext(DiagnosticListener value)
    {
        if (value.Name == "HttpHandlerDiagnosticListener") subscriptions.Add(value.Subscribe(this, name => name.StartsWith("System.Net.Http.HttpRequestOut", StringComparison.Ordinal)));
    }
    public void OnNext(KeyValuePair<string, object?> value)
    {
        if (value.Key != "System.Net.Http.HttpRequestOut.Stop") return;
        var original = Activity.Current;
        if (original?.Parent?.Source.Name != DeploymentTelemetry.SourceName) return;
        // Ignore request URI, headers, bodies, exception objects and provider identifiers.
        using var span = DeploymentTelemetry.Source.StartActivity("http.dependency", ActivityKind.Client,
            original.Parent.Context, startTime: original.StartTimeUtc);
        DeploymentTelemetry.SanitizeContext(span);
        var response = value.Value?.GetType().GetProperty("Response")?.GetValue(value.Value) as HttpResponseMessage;
        if (response is not null)
        {
            span?.SetTag("http.response.status_code", (int)response.StatusCode);
            if ((int)response.StatusCode >= 400) span?.SetStatus(ActivityStatusCode.Error);
        }
        else span?.SetStatus(ActivityStatusCode.Error);
        span?.SetTag("dependency.type", "http");
    }
    public void OnError(Exception error) { }
    public void OnCompleted() { }
    public Task StopAsync(CancellationToken cancellationToken) { Dispose(); return Task.CompletedTask; }
    public void Dispose() { foreach (var subscription in subscriptions) subscription.Dispose(); subscriptions.Clear(); }
}

internal sealed class DeploymentSampler(double ratio) : Sampler
{
    private readonly TraceIdRatioBasedSampler normal = new(ratio);
    public override SamplingResult ShouldSample(in SamplingParameters samplingParameters) =>
        samplingParameters.Name == "deployment.marker" ? new SamplingResult(SamplingDecision.RecordAndSample) : normal.ShouldSample(samplingParameters);
}
