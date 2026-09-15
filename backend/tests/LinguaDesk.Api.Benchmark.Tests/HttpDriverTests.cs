using System.Net;
using System.Text;
using System.Text.Json;
using LinguaDesk.Api.Benchmark;

namespace LinguaDesk.Api.Benchmark.Tests;

[TestClass]
public sealed class HttpDriverTests
{
    private static WorkItem TranslationItem(int sequence, string? selection = null) =>
        new(
            $"combo-{sequence}", "translation", "en->zh", "band-1-100",
            selection, "zh", null,
            "The M037 rehearsal schedule. ", SourceSynthesis.Sha256Hex("The M037 rehearsal schedule. "),
            SourceSynthesis.CountScalars("The M037 rehearsal schedule. "),
            IsRepeat: false, sequence);

    private static WorkItem RewritingItem(int sequence) =>
        new(
            $"rewrite-{sequence}", "rewriting", "en/correctionOnly", "band-1-100",
            null, null, "correctionOnly",
            "The M037 rehearsal note is ready. ", SourceSynthesis.Sha256Hex("The M037 rehearsal note is ready. "),
            SourceSynthesis.CountScalars("The M037 rehearsal note is ready. "),
            IsRepeat: false, sequence);

    private static string SuccessBody(string textField, string text) =>
        "{\"operationId\":\"00000000-0000-7000-8000-000000000000\",\"family\":\"translation\",\"status\":\"succeeded\"," +
        $"\"{textField}\":\"{text}\",\"characterCount\":28,\"admissionDay\":\"2026-09-11\"," +
        "\"usage\":{\"day\":\"2026-09-11\",\"consumedCharacters\":28,\"reservedCharacters\":0}}";

    [TestMethod]
    public async Task RegisterAndSignInUseTheRealAuthBoundary()
    {
        var handler = new RecordingHandler(request =>
        {
            var path = request.RequestUri!.AbsolutePath;
            if (path == "/api/accounts/register")
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted)
                {
                    Content = new StringContent("""{"status":"signInRequired"}""", Encoding.UTF8, "application/json"),
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"accessToken":"synthetic-token"}""", Encoding.UTF8, "application/json"),
            });
        });
        var driver = new BenchmarkHttpDriver(new HttpClient(handler), "http://127.0.0.1:9");

        await driver.RegisterAsync("bench@example.test", "m037-benchmark-pass-01", CancellationToken.None);
        var token = await driver.SignInAsync("bench@example.test", "m037-benchmark-pass-01", CancellationToken.None);

        Assert.AreEqual("synthetic-token", token);
        Assert.HasCount(2, handler.Requests);
    }

    [TestMethod]
    public async Task SubmissionsCarryFreshIdentitiesAndDeclaredSettings()
    {
        var handler = new RecordingHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent(SuccessBody("translatedText", "ZH fixture"), Encoding.UTF8, "application/json"),
        }));
        var driver = new BenchmarkHttpDriver(new HttpClient(handler), "http://127.0.0.1:9");
        var items = new[] { TranslationItem(0), TranslationItem(1, "en"), RewritingItem(2) };

        var run = await driver.RunAsync(items, "synthetic-token", concurrency: 2, CancellationToken.None);

        Assert.HasCount(3, run.Observations);
        Assert.IsTrue(run.Observations.All(item => item.Outcome == "success"));
        Assert.AreEqual(3, run.Observations.Select(item => item.OperationId).Distinct().Count());
        Assert.IsTrue(run.Observations.All(item => Guid.TryParse(item.OperationId, out _)));

        var bodies = handler.Requests
            .Where(request => request.RequestUri!.AbsolutePath == "/api/operations")
            .Select(request => JsonDocument.Parse(request.Body!).RootElement)
            .ToList();
        Assert.HasCount(3, bodies);
        var translations = bodies.Where(body => body.GetProperty("family").GetString() == "translation").ToList();
        var rewrites = bodies.Where(body => body.GetProperty("family").GetString() == "rewriting").ToList();
        Assert.HasCount(2, translations);
        Assert.HasCount(1, rewrites);
        Assert.AreEqual(1, translations.Count(body => !body.TryGetProperty("sourceSelection", out _)));
        Assert.AreEqual("en", translations.Single(body => body.TryGetProperty("sourceSelection", out _)).GetProperty("sourceSelection").GetString());
        Assert.IsTrue(translations.All(body => body.GetProperty("target").GetString() == "zh"));
        Assert.AreEqual("correctionOnly", rewrites.Single().GetProperty("mode").GetString());
        Assert.IsTrue(handler.Requests.All(request =>
            request.Authorization == "Bearer synthetic-token"));
        Assert.IsTrue(run.Observations.All(item => item.SubmittedFresh && item.TotalMs >= 0));
    }

    [TestMethod]
    public async Task ConcurrencyNeverExceedsTheDeclaredLimit()
    {
        var handler = new RecordingHandler(async _ =>
        {
            await Task.Delay(200, CancellationToken.None);
            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent(SuccessBody("translatedText", "ZH fixture"), Encoding.UTF8, "application/json"),
            };
        });
        var driver = new BenchmarkHttpDriver(new HttpClient(handler), "http://127.0.0.1:9");
        var items = Enumerable.Range(0, 8).Select(sequence => TranslationItem(sequence)).ToList();

        var run = await driver.RunAsync(items, "synthetic-token", concurrency: 4, CancellationToken.None);

        Assert.HasCount(8, run.Observations);
        Assert.IsLessThanOrEqualTo(4, handler.MaxObservedConcurrency);
        Assert.IsGreaterThanOrEqualTo(2, handler.MaxObservedConcurrency);
        Assert.AreEqual(4, run.MaxInFlight);
        Assert.IsTrue(run.Observations.All(item => item.TotalMs >= 150));
    }

    [TestMethod]
    public async Task ClassifiedFailuresAreRetainedWithCategory()
    {
        var handler = new RecordingHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
        {
            Content = new StringContent(
                """{"title":"Service unavailable","status":503,"detail":"synthetic","category":"processingFailure","correlationId":"x"}""",
                Encoding.UTF8,
                "application/problem+json"),
        }));
        var driver = new BenchmarkHttpDriver(new HttpClient(handler), "http://127.0.0.1:9");

        var run = await driver.RunAsync([TranslationItem(0)], "synthetic-token", concurrency: 4, CancellationToken.None);

        var observation = run.Observations.Single();
        Assert.AreEqual("failure", observation.Outcome);
        Assert.AreEqual("processingFailure", observation.FailureCategory);
        Assert.AreEqual(503, observation.HttpStatus);
        Assert.IsGreaterThanOrEqualTo(0.0, observation.TotalMs);
        Assert.IsNull(observation.OutputSha256);
    }

    [TestMethod]
    public async Task SuccessRecordsCountsWithoutText()
    {
        var handler = new RecordingHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent(SuccessBody("rewrittenText", "EN fixture"), Encoding.UTF8, "application/json"),
        }));
        var driver = new BenchmarkHttpDriver(new HttpClient(handler), "http://127.0.0.1:9");

        var run = await driver.RunAsync([RewritingItem(0)], "synthetic-token", concurrency: 1, CancellationToken.None);

        var observation = run.Observations.Single();
        Assert.AreEqual("success", observation.Outcome);
        Assert.AreEqual(28, observation.CharacterCount);
        Assert.AreEqual("2026-09-11", observation.AdmissionDay);
        Assert.IsNotNull(observation.UsageJson);
        Assert.AreEqual(10, observation.OutputLength);
        Assert.AreEqual(SourceSynthesis.Sha256Hex("EN fixture"), observation.OutputSha256);
        Assert.AreEqual("unknown", observation.CacheCohort);
        Assert.IsNull(observation.Stages.AdmissionMs);
    }

    private sealed class RecordingHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> responder) : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> responder = responder;
        private int inFlight;

        public List<RecordedRequest> Requests { get; } = [];

        public int MaxObservedConcurrency { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var current = Interlocked.Increment(ref inFlight);
            lock (Requests)
            {
                MaxObservedConcurrency = Math.Max(MaxObservedConcurrency, current);
            }

            try
            {
                var body = request.Content is null
                    ? null
                    : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                var recorded = new RecordedRequest(
                    request.RequestUri!,
                    body,
                    request.Headers.Authorization?.ToString());
                lock (Requests)
                {
                    Requests.Add(recorded);
                }

                return await responder(request).ConfigureAwait(false);
            }
            finally
            {
                Interlocked.Decrement(ref inFlight);
            }
        }
    }

    private sealed record RecordedRequest(Uri RequestUri, string? Body, string? Authorization);
}
