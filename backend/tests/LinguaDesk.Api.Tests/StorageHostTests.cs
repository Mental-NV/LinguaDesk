using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using LinguaDesk.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LinguaDesk.Api.Tests;

[TestClass]
public sealed class StorageHostTests
{
    private static readonly string[] ExpectedMigrationIds =
    [
        "20260908221711_InitialStorage",
        "20260909120834_LocalAccounts",
        "20260911024223_OperationAdmission",
        "20260911030052_OperationSettlement",
        "20260911032126_MonetaryAdmission",
    ];

    [TestMethod]
    public async Task HostScopesResolveTheConfiguredFileWithoutOpeningIt()
    {
        await using var database = StorageTestDatabase.Create();
        await using var factory = new StorageWebApplicationFactory(database.DatabasePath);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/health/live");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.IsFalse(File.Exists(database.DatabasePath));
        using var firstScope = factory.Services.CreateScope();
        using var secondScope = factory.Services.CreateScope();
        var firstContext = firstScope.ServiceProvider.GetRequiredService<LinguaDeskDbContext>();
        var secondContext = secondScope.ServiceProvider.GetRequiredService<LinguaDeskDbContext>();
        var firstConnection = new SqliteConnectionStringBuilder(firstContext.Database.GetConnectionString());
        Assert.AreEqual(database.DatabasePath, firstConnection.DataSource);
        Assert.AreNotSame(firstContext, secondContext);
        Assert.IsFalse(File.Exists(database.DatabasePath));
    }

    [TestMethod]
    [Timeout(30000, CooperativeCancellation = true)]
    public async Task CommittedDataSurvivesTwoDistinctRealHostProcesses()
    {
        await using var database = StorageTestDatabase.Create();
        await database.MigrateAsync();
        await database.CreateProbeSchemaAsync();
        await using (var seedContext = database.CreateContext())
        {
            await seedContext.Database.ExecuteSqlRawAsync(
                "INSERT INTO probe_value (id, value) VALUES (1, 'survives restart');");
        }

        var beforeStartup = await File.ReadAllBytesAsync(database.DatabasePath);
        var keysPath = Directory.CreateDirectory(Path.Combine(database.RootPath, "keys")).FullName;
        int firstProcessId;
        await using (var firstHost = await OwnedKestrelHost.StartAsync(database.DatabasePath, keysPath))
        {
            firstProcessId = firstHost.ProcessId;
            Assert.AreEqual("Healthy", await firstHost.Client.GetStringAsync("/health/live"));
        }

        await using (var secondHost = await OwnedKestrelHost.StartAsync(database.DatabasePath, keysPath))
        {
            Assert.AreNotEqual(firstProcessId, secondHost.ProcessId);
            Assert.AreEqual("Healthy", await secondHost.Client.GetStringAsync("/health/live"));
        }

        CollectionAssert.AreEqual(beforeStartup, await File.ReadAllBytesAsync(database.DatabasePath));
        await using var verification = database.CreateContext();
        Assert.AreEqual(
            "survives restart",
            await ExecuteScalarAsync<string>(verification, "SELECT value FROM probe_value WHERE id = 1;"));
        CollectionAssert.AreEqual(
            ExpectedMigrationIds,
            (await verification.Database.GetAppliedMigrationsAsync()).ToArray());
    }

    private static async Task<T> ExecuteScalarAsync<T>(LinguaDeskDbContext context, string commandText)
    {
        var connection = context.Database.GetDbConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = commandText;
        var result = await command.ExecuteScalarAsync();
        if (result is null || result is DBNull)
        {
            throw new InvalidOperationException($"Scalar command returned no value: {commandText}");
        }

        return (T)Convert.ChangeType(result, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
    }

    private sealed class StorageWebApplicationFactory(string databasePath) : WebApplicationFactory<Program>
    {
        private readonly string webRoot = Directory.CreateTempSubdirectory("linguadesk-storage-webroot-").FullName;
        private readonly string keysPath = Directory.CreateTempSubdirectory("linguadesk-storage-keys-").FullName;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseWebRoot(webRoot);
            builder.ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Storage:DatabasePath"] = databasePath,
                    ["Security:DataProtectionKeysPath"] = keysPath,
                }));
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing && Directory.Exists(webRoot))
            {
                Directory.Delete(webRoot, recursive: true);
            }
            if (disposing && Directory.Exists(keysPath))
            {
                Directory.Delete(keysPath, recursive: true);
            }
        }
    }

    private sealed class OwnedKestrelHost : IAsyncDisposable
    {
        private readonly Process process;
        private readonly ConcurrentQueue<string> output;

        private OwnedKestrelHost(Process process, Uri baseAddress, ConcurrentQueue<string> output)
        {
            this.process = process;
            this.output = output;
            Client = new HttpClient { BaseAddress = baseAddress };
        }

        public HttpClient Client { get; }

        public int ProcessId => process.Id;

        public static async Task<OwnedKestrelHost> StartAsync(string databasePath, string keysPath)
        {
            var repositoryRoot = StoragePathPolicy.FindRepositoryRoot(AppContext.BaseDirectory)
                ?? throw new InvalidOperationException("Could not locate repository root.");
            var projectPath = Path.Combine(
                repositoryRoot,
                "backend",
                "src",
                "LinguaDesk.Api",
                "LinguaDesk.Api.csproj");
            var startInfo = new ProcessStartInfo("dotnet")
            {
                WorkingDirectory = repositoryRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            startInfo.ArgumentList.Add("run");
            startInfo.ArgumentList.Add("--project");
            startInfo.ArgumentList.Add(projectPath);
            startInfo.ArgumentList.Add("--configuration");
            startInfo.ArgumentList.Add("Release");
            startInfo.ArgumentList.Add("--no-build");
            startInfo.ArgumentList.Add("--no-restore");
            startInfo.ArgumentList.Add("--");
            startInfo.ArgumentList.Add("--urls");
            startInfo.ArgumentList.Add("http://127.0.0.1:0");
            startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Testing";
            startInfo.Environment["Storage__DatabasePath"] = databasePath;
            startInfo.Environment["Security__DataProtectionKeysPath"] = keysPath;

            var output = new ConcurrentQueue<string>();
            var ready = new TaskCompletionSource<Uri>(TaskCreationOptions.RunContinuationsAsynchronously);
            var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
            DataReceivedEventHandler handleOutput = (_, eventArgs) =>
            {
                if (eventArgs.Data is null)
                {
                    return;
                }

                output.Enqueue(eventArgs.Data);
                const string prefix = "http://127.0.0.1:";
                var start = eventArgs.Data.IndexOf(prefix, StringComparison.Ordinal);
                if (start < 0)
                {
                    return;
                }

                var address = eventArgs.Data[start..].Trim();
                if (Uri.TryCreate(address, UriKind.Absolute, out var baseAddress))
                {
                    ready.TrySetResult(baseAddress);
                }
            };
            process.OutputDataReceived += handleOutput;
            process.ErrorDataReceived += handleOutput;
            process.Exited += (_, _) => ready.TrySetException(new InvalidOperationException(
                $"Host exited before readiness. Output:{Environment.NewLine}{string.Join(Environment.NewLine, output)}"));

            try
            {
                if (!process.Start())
                {
                    throw new InvalidOperationException("Could not start the owned Kestrel process.");
                }

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                var baseAddress = await ready.Task.WaitAsync(TimeSpan.FromSeconds(15));
                return new OwnedKestrelHost(process, baseAddress, output);
            }
            catch
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }

                process.Dispose();
                throw;
            }
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            if (!process.HasExited)
            {
                using var terminate = Process.Start("/bin/kill", ["-TERM", process.Id.ToString(System.Globalization.CultureInfo.InvariantCulture)]);
                if (terminate is not null)
                {
                    await terminate.WaitForExitAsync();
                }

                try
                {
                    await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
                }
                catch (TimeoutException)
                {
                    process.Kill(entireProcessTree: true);
                    await process.WaitForExitAsync();
                }
            }

            process.Dispose();
        }
    }
}
