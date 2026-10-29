using System.Diagnostics;
using LinguaDesk.Api.Infrastructure.Telemetry;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using OpenTelemetry.Trace;

namespace LinguaDesk.Api.Tests;

[TestClass]
[DoNotParallelize]
public sealed class DeploymentTelemetryTests
{
    [TestMethod]
    public async Task OnlyRouteStatusAndSanitizedErrorReachInMemoryExporter()
    {
        const string sentinel = "PRIVATE-TEXT-COOKIE-SECRET-EMAIL";
        using var exporter = new CaptureExporter();
        using var provider = Sdk.CreateTracerProviderBuilder().AddSource(DeploymentTelemetry.SourceName)
            .SetSampler(new AlwaysOnSampler()).AddProcessor(new SimpleActivityExportProcessor(exporter)).Build();
        using var parent = new Activity("unexported-parent").AddBaggage("private", sentinel).Start();
        parent.TraceStateString = sentinel;
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/operations/" + sentinel;
        context.Request.QueryString = new QueryString("?prompt=" + sentinel);
        context.Request.Headers.Authorization = sentinel;
        context.Request.Headers.Cookie = sentinel;
        context.SetEndpoint(new RouteEndpoint(_ => Task.CompletedTask, RoutePatternFactory.Parse("/api/operations/{id}"), 0, EndpointMetadataCollection.Empty, "operation"));
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => DeploymentTelemetry.ObserveRequest(context, () => throw new InvalidOperationException(sentinel)));
        Assert.IsTrue(exporter.Serialized.Any(s => s.Contains("/api/operations/{id}", StringComparison.Ordinal)));
        Assert.IsFalse(exporter.Serialized.Any(s => s.Contains(sentinel, StringComparison.Ordinal)));
        Assert.IsTrue(exporter.Serialized.Any(s => s.Contains("application.failure", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task HealthAndStaticRequestsExportNothing()
    {
        using var exporter = new CaptureExporter();
        using var provider = Sdk.CreateTracerProviderBuilder().AddSource(DeploymentTelemetry.SourceName)
            .AddProcessor(new SimpleActivityExportProcessor(exporter)).Build();
        foreach (var path in new[] { "/health/ready", "/assets/app.js", "/" })
        {
            var context = new DefaultHttpContext();
            context.Request.Path = path;
            await DeploymentTelemetry.ObserveRequest(context, () => Task.CompletedTask);
        }
        Assert.IsEmpty(exporter.Serialized);
    }

    [TestMethod]
    public void OfflineHostCannotEnableExporterWithAConnectionString()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Telemetry:Enabled"] = "true",
            ["APPLICATIONINSIGHTS_CONNECTION_STRING"] = "InstrumentationKey=private-secret",
        }).Build();
        Assert.IsFalse(DeploymentTelemetry.CanExport(config, new TestEnvironment()));
    }

    [TestMethod]
    public void MarkerIsBoundedAndIndependentOfNormalSampling()
    {
        using var exporter = new CaptureExporter();
        using var provider = Sdk.CreateTracerProviderBuilder().AddSource(DeploymentTelemetry.SourceName)
            .SetSampler(new DeploymentSampler(0)).AddProcessor(new SimpleActivityExportProcessor(exporter)).Build();
        using (DeploymentTelemetry.Source.StartActivity("api.request")) { }
        using (DeploymentTelemetry.Source.StartActivity("deployment.marker")) { }
        Assert.HasCount(1, exporter.Serialized);
        StringAssert.StartsWith(exporter.Serialized[0], "deployment.marker");
    }

    [TestMethod]
    public async Task ExportFailureDoesNotFailARequest()
    {
        using var exporter = new FailedExporter();
        using var provider = Sdk.CreateTracerProviderBuilder().AddSource(DeploymentTelemetry.SourceName)
            .AddProcessor(new SimpleActivityExportProcessor(exporter)).Build();
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/capabilities";
        await DeploymentTelemetry.ObserveRequest(context, () => { context.Response.StatusCode = 200; return Task.CompletedTask; });
        Assert.AreEqual(200, context.Response.StatusCode);
    }

    [TestMethod]
    public void HostedCiCannotExportAndMissingConnectionFailsPreflight()
    {
        var website = Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME");
        var ci = Environment.GetEnvironmentVariable("CI");
        try
        {
            Environment.SetEnvironmentVariable("WEBSITE_SITE_NAME", "isolated-test-only");
            var environment = new TestEnvironment { EnvironmentName = "Production" };
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Telemetry:Enabled"] = "true" }).Build();
            Environment.SetEnvironmentVariable("CI", "true");
            Assert.IsFalse(DeploymentTelemetry.CanExport(config, environment));
            Environment.SetEnvironmentVariable("CI", "false");
            Assert.ThrowsExactly<InvalidOperationException>(() => new ServiceCollection().AddDeploymentTelemetry(config, environment));
        }
        finally
        {
            Environment.SetEnvironmentVariable("WEBSITE_SITE_NAME", website);
            Environment.SetEnvironmentVariable("CI", ci);
        }
    }

    private sealed class FailedExporter : BaseExporter<Activity>
    {
        public override ExportResult Export(in Batch<Activity> batch) => ExportResult.Failure;
    }

    private sealed class CaptureExporter : BaseExporter<Activity>
    {
        public List<string> Serialized { get; } = [];
        public override ExportResult Export(in Batch<Activity> batch)
        {
            foreach (var activity in batch)
                Serialized.Add(activity.DisplayName + " " + activity.StatusDescription + " " + activity.TraceStateString + " " + string.Join(" ", activity.Baggage.Select(b => b.Value)) + " " + string.Join(" ", activity.Tags.Select(t => t.Key + "=" + t.Value)) + " " + string.Join(" ", activity.Events.Select(e => e.Name)));
            return ExportResult.Success;
        }
    }
    private sealed class TestEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Testing";
        public string ApplicationName { get; set; } = "LinguaDesk.Api";
        public string ContentRootPath { get; set; } = "/tmp";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
