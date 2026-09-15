using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using LinguaDesk.Api.Benchmark;

internal static class Program
{
    private static readonly string[] BlockedCredentialVariables =
    [
        "OPENAI_API_KEY",
        "AZURE_OPENAI_API_KEY",
        "ANTHROPIC_API_KEY",
        "DEEPSEEK_API_KEY",
        "GOOGLE_API_KEY",
        "GEMINI_API_KEY",
        "MISTRAL_API_KEY",
        "COHERE_API_KEY",
    ];

    public static async Task<int> Main(string[] args)
    {
        if (args.Length == 0 || args[0] != "rehearse")
        {
            WriteUsage();
            return 2;
        }

        if (!RehearseOptions.TryParse(args[1..], out var options, out var error))
        {
            Console.Error.WriteLine(error);
            WriteUsage();
            return 2;
        }

        if (FindPresentCredential() is { } credential)
        {
            Console.Error.WriteLine(
                $"Blocked: credential environment '{credential}' is present; the unpaid rehearsal never runs with provider credentials.");
            return 3;
        }

        try
        {
            return await RehearseAsync(options!).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Rehearsal failed: {exception.Message}");
            return 1;
        }
    }

    private static string? FindPresentCredential()
    {
        foreach (System.Collections.DictionaryEntry entry in Environment.GetEnvironmentVariables())
        {
            var name = entry.Key?.ToString() ?? string.Empty;
            if (name.StartsWith("LINGUADESK_AIEVALUATION__CREDENTIALS__", StringComparison.Ordinal)
                && !string.IsNullOrWhiteSpace(entry.Value?.ToString()))
            {
                return name;
            }
        }

        foreach (var name in BlockedCredentialVariables)
        {
            if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(name)))
            {
                return name;
            }
        }

        return null;
    }

    private static async Task<int> RehearseAsync(RehearseOptions options)
    {
        var manifest = ManifestLoader.Load(options.ManifestPath);
        var rules = ReportWriter.LoadRules(options.RulesPath);
        var concurrency = options.Concurrency ?? manifest.Concurrency;
        var targetMs = options.TargetMs ?? manifest.TargetMs;
        if (concurrency != manifest.Concurrency)
        {
            throw new InvalidOperationException(
                $"Requested concurrency {concurrency} differs from the manifest ({manifest.Concurrency}); rescaling needs a reviewed manifest.");
        }

        if (targetMs != manifest.TargetMs)
        {
            throw new InvalidOperationException(
                $"Requested target {targetMs}ms differs from the manifest ({manifest.TargetMs}ms); retargeting needs a reviewed manifest.");
        }

        var items = ManifestLoader.Expand(manifest);
        using var handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
        };
        using var http = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(Math.Max(10, options.TimeoutSeconds)),
        };
        var driver = new BenchmarkHttpDriver(http, options.BaseUrl);

        using var runCancellation = new CancellationTokenSource(TimeSpan.FromMinutes(10));
        await driver.RegisterAsync(options.Email, options.Password, runCancellation.Token).ConfigureAwait(false);
        var accessToken = await driver.SignInAsync(options.Email, options.Password, runCancellation.Token).ConfigureAwait(false);
        var run = await driver.RunAsync(items, accessToken, concurrency, runCancellation.Token).ConfigureAwait(false);

        var bands = manifest.LengthBands.ToDictionary(band => band.Id, StringComparer.Ordinal);
        var expected = items
            .Where(item => !item.IsRepeat)
            .Select(item => new ExpectedCombo(
                item.ComboId, item.Family, item.Route, item.BandId,
                item.SourceSha256, item.SourceLength,
                bands[item.BandId].MinCanonicalChars, bands[item.BandId].MaxCanonicalChars))
            .ToList();
        var observed = run.Observations
            .Select(item => new ObservedRequest(
                item.OperationId, item.ComboId, item.IsRepeat,
                item.SubmittedFresh, item.TotalMs >= 0,
                item.SourceSha256, item.SourceLength))
            .ToList();
        var audit = CompositionAuditor.Audit(expected, observed, items.Count);

        var report = ReportWriter.Build(
            manifest,
            rules,
            run.Observations,
            run,
            audit,
            options.CodeRevision,
            options.DotnetSdk,
            ShaFile(options.ManifestPath),
            ShaFile(options.RulesPath),
            options.ReadinessProbes,
            options.BaseUrl,
            $"{RuntimeInformation.OSDescription} / {RuntimeInformation.FrameworkDescription}",
            DateTimeOffset.UtcNow);

        ReportWriter.Write(report, options.OutputPath);
        Console.Out.Write(ReportWriter.Serialize(report));
        Console.Out.Write('\n');
        Console.Error.WriteLine($"Benchmark rehearsal report: {options.OutputPath}");

        if (!audit.Passed)
        {
            Console.Error.WriteLine("Composition audit failed:");
            foreach (var violation in audit.Violations)
            {
                Console.Error.WriteLine($"- {violation}");
            }

            return 1;
        }

        if (!string.Equals(report.Status, "complete", StringComparison.Ordinal))
        {
            Console.Error.WriteLine("Rehearsal recorded no measured requests.");
            return 1;
        }

        Console.Error.WriteLine(
            $"Rehearsal complete: {run.Observations.Count}/{items.Count} measured at concurrency {concurrency} " +
            $"(max in flight {run.MaxInFlight}, drain {run.DrainMs:F0}ms), zero paid dispatches.");
        return 0;
    }

    private static string ShaFile(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private static void WriteUsage() =>
        Console.Error.WriteLine(
            "Usage: dotnet LinguaDesk.Api.Benchmark.dll rehearse --base-url <url> --email <address> --password <secret> " +
            "--manifest <path> --rules <path> --output <path> --code-revision <sha> --sdk <version> " +
            "--readiness-probes <n> [--concurrency <n>] [--target-ms <n>] [--timeout-s <n>]");

    private sealed record RehearseOptions(
        string BaseUrl,
        string Email,
        string Password,
        string ManifestPath,
        string RulesPath,
        string OutputPath,
        string CodeRevision,
        string DotnetSdk,
        int ReadinessProbes,
        int? Concurrency,
        int? TargetMs,
        int TimeoutSeconds)
    {
        public static bool TryParse(string[] options, out RehearseOptions? selected, out string error)
        {
            string? baseUrl = null;
            string? email = null;
            string? password = null;
            string? manifest = null;
            string? rules = null;
            string? output = null;
            string? revision = null;
            string? sdk = null;
            int readinessProbes = -1;
            int? concurrency = null;
            int? targetMs = null;
            var timeoutSeconds = 60;

            for (var index = 0; index < options.Length; index++)
            {
                string? TakeValue(string flag)
                {
                    if (index + 1 >= options.Length)
                    {
                        return null;
                    }

                    index++;
                    return options[index];
                }

                switch (options[index])
                {
                    case "--base-url": baseUrl = TakeValue("--base-url"); break;
                    case "--email": email = TakeValue("--email"); break;
                    case "--password": password = TakeValue("--password"); break;
                    case "--manifest": manifest = TakeValue("--manifest"); break;
                    case "--rules": rules = TakeValue("--rules"); break;
                    case "--output": output = TakeValue("--output"); break;
                    case "--code-revision": revision = TakeValue("--code-revision"); break;
                    case "--sdk": sdk = TakeValue("--sdk"); break;
                    case "--readiness-probes":
                        if (TakeValue("--readiness-probes") is { } probesText
                            && int.TryParse(probesText, out var probes) && probes >= 0)
                        {
                            readinessProbes = probes;
                        }
                        else
                        {
                            selected = null;
                            error = "rehearse requires --readiness-probes <nonnegative-count>.";
                            return false;
                        }

                        break;
                    case "--concurrency":
                        if (TakeValue("--concurrency") is { } concurrencyText
                            && int.TryParse(concurrencyText, out var parsedConcurrency) && parsedConcurrency > 0)
                        {
                            concurrency = parsedConcurrency;
                        }
                        else
                        {
                            selected = null;
                            error = "rehearse requires --concurrency <positive-count>.";
                            return false;
                        }

                        break;
                    case "--target-ms":
                        if (TakeValue("--target-ms") is { } targetText
                            && int.TryParse(targetText, out var parsedTarget) && parsedTarget > 0)
                        {
                            targetMs = parsedTarget;
                        }
                        else
                        {
                            selected = null;
                            error = "rehearse requires --target-ms <positive-ms>.";
                            return false;
                        }

                        break;
                    case "--timeout-s":
                        if (TakeValue("--timeout-s") is { } timeoutText
                            && int.TryParse(timeoutText, out var parsedTimeout) && parsedTimeout > 0)
                        {
                            timeoutSeconds = parsedTimeout;
                        }
                        else
                        {
                            selected = null;
                            error = "rehearse requires --timeout-s <positive-seconds>.";
                            return false;
                        }

                        break;
                    default:
                        selected = null;
                        error = $"Unknown or incomplete rehearse option '{options[index]}'.";
                        return false;
                }
            }

            var missing = new StringBuilder();
            void Require(string? value, string flag)
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    missing.Append(missing.Length == 0 ? flag : $" {flag}");
                }
            }

            Require(baseUrl, "--base-url");
            Require(email, "--email");
            Require(password, "--password");
            Require(manifest, "--manifest");
            Require(rules, "--rules");
            Require(output, "--output");
            Require(revision, "--code-revision");
            Require(sdk, "--sdk");
            if (missing.Length > 0 || readinessProbes < 0)
            {
                selected = null;
                error = $"rehearse is missing required options: {missing} --readiness-probes.";
                return false;
            }

            selected = new RehearseOptions(
                baseUrl!, email!, password!, manifest!, rules!, output!, revision!, sdk!,
                readinessProbes, concurrency, targetMs, timeoutSeconds);
            error = string.Empty;
            return true;
        }
    }
}
