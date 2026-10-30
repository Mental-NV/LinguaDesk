using System.Net;
using LinguaDesk.Api.Infrastructure.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LinguaDesk.Api.Tests;

[TestClass]
public sealed class DeploymentProxyTests
{
    [TestMethod]
    [DataRow("169.254.1.1", true)]
    [DataRow("198.51.100.7", false)]
    public async Task OnlyTheSpecificContainerGatewayCanForwardHttps(string remote, bool accepted)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLinguaDeskSecurity(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Security:ForwardedHeaderTrustedNetworks:0"] = "169.254.1.1/32",
        }).Build(), isContractGeneration: true);
        using var provider = services.BuildServiceProvider();
        var context = new DefaultHttpContext();
        context.Request.Scheme = "http";
        context.Connection.RemoteIpAddress = IPAddress.Parse(remote);
        context.Request.Headers["X-Forwarded-Proto"] = "https";
        context.Request.Headers["X-Forwarded-For"] = "203.0.113.12";
        var middleware = new ForwardedHeadersMiddleware(_ => Task.CompletedTask,
            provider.GetRequiredService<ILoggerFactory>(), provider.GetRequiredService<IOptions<ForwardedHeadersOptions>>());
        await middleware.Invoke(context);
        Assert.AreEqual(accepted ? "https" : "http", context.Request.Scheme);
        Assert.AreEqual(accepted ? "203.0.113.12" : remote, context.Connection.RemoteIpAddress!.ToString());
    }
}
