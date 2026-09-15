using System.Net;
using System.Text;
using System.Text.Json;
using LinguaDesk.Api.Infrastructure.Serving;
using LinguaDesk.Infrastructure.Ai;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace LinguaDesk.Api.Tests;

[TestClass]
[DoNotParallelize]
public sealed class ServingProviderTests
{
    private const string CredentialVariable =
        "LINGUADESK_AIEVALUATION__CREDENTIALS__DEEPSEEK__APIKEY";

    private static ServingOptions ValidOptions() => new()
    {
        Translation = new ServingFamilyOptions
        {
            CandidateId = "DeepSeek-V4.1-Flash",
            CredentialRef = "deepseek",
            MaxSpendUsdPerOperation = 0.05m,
        },
        Rewriting = new ServingFamilyOptions
        {
            CandidateId = "DeepSeek-V4.1-Flash",
            CredentialRef = "deepseek",
            MaxSpendUsdPerOperation = 0.05m,
        },
    };

    private static string ChatCompletionsJson(string content, HttpStatusCode status = HttpStatusCode.OK)
    {
        var usage = JsonSerializer.Serialize(new
        {
            prompt_tokens = 10,
            completion_tokens = 5,
            total_tokens = 15,
        });
        return JsonSerializer.Serialize(new
        {
            id = "chatcmpl-test",
            @object = "chat.completion",
            created = 0,
            model = "deepseek-flash",
            choices = new[]
            {
                new
                {
                    index = 0,
                    message = new { role = "assistant", content },
                    finish_reason = "stop",
                },
            },
            usage = JsonDocument.Parse(usage).RootElement,
            system_fingerprint = "fp-test",
        });
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(respond(request));
    }

    [TestMethod]
    public void UnconfiguredProvidersStayFailClosed()
    {
        var options = Options.Create(new ServingOptions());
        using var translation = new ServingTranslationClientProvider(options, new HttpClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK))));
        using var rewriting = new ServingRewritingClientProvider(options, new HttpClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK))));

        Assert.IsFalse(translation.TryGetClients(out _, out _));
        Assert.IsFalse(rewriting.TryGetClients(out _, out _));
    }

    [TestMethod]
    public async Task TranslationProviderServesPrimaryOnlyChain()
    {
        const string content = "{\"status\":\"eligible\",\"language\":\"en\"}";
        var httpClient = new HttpClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(ChatCompletionsJson(content), Encoding.UTF8, "application/json"),
        }));
        using var provider = new ServingTranslationClientProvider(Options.Create(ValidOptions()), httpClient);

        Assert.IsTrue(provider.TryGetClients(out var router, out var chain));
        Assert.AreEqual(ChainFamily.Translation, chain.Family);
        Assert.AreEqual("DeepSeek-V4.1-Flash", chain.Primary.CandidateId);
        Assert.IsNull(chain.Fallback);

        using var client = (IDisposable)router(chain.Primary);
        var response = await ((IChatClient)client).GetResponseAsync(
            [new ChatMessage(ChatRole.User, "probe")]);

        Assert.AreEqual(content, response.Text);
        var reporter = client as IChainAttemptReporter;
        Assert.IsNotNull(reporter);
        Assert.IsNotNull(reporter.LastAttempt);
        Assert.AreEqual("DeepSeek-V4.1-Flash", reporter.LastAttempt.CandidateId);
        Assert.AreEqual("deepseek", reporter.LastAttempt.CredentialRef);
    }

    [TestMethod]
    public async Task RewritingProviderServesPrimaryOnlyChain()
    {
        const string content = "{\"status\":\"eligible\",\"language\":\"en\"}";
        var httpClient = new HttpClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(ChatCompletionsJson(content), Encoding.UTF8, "application/json"),
        }));
        using var provider = new ServingRewritingClientProvider(Options.Create(ValidOptions()), httpClient);

        Assert.IsTrue(provider.TryGetClients(out var router, out var chain));
        Assert.AreEqual(ChainFamily.Rewriting, chain.Family);
        Assert.AreEqual("DeepSeek-V4.1-Flash", chain.Primary.CandidateId);
        Assert.IsNull(chain.Fallback);

        using var client = (IDisposable)router(chain.Primary);
        var response = await ((IChatClient)client).GetResponseAsync(
            [new ChatMessage(ChatRole.User, "probe")]);

        Assert.AreEqual(content, response.Text);
    }

    [TestMethod]
    public void MissingKeyBlocksInsteadOfFaking()
    {
        var previous = Environment.GetEnvironmentVariable(CredentialVariable);
        try
        {
            Environment.SetEnvironmentVariable(CredentialVariable, "  ");
            var httpClient = new HttpClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(ChatCompletionsJson("{}"), Encoding.UTF8, "application/json"),
            }));
            using var provider = new ServingTranslationClientProvider(Options.Create(ValidOptions()), httpClient);

            Assert.IsTrue(provider.TryGetClients(out var router, out var chain));
            var exception = Assert.ThrowsExactly<ChatCompletionsAdapterException>(() => router(chain.Primary));

            Assert.AreEqual(AttemptFailureKind.Blocked, exception.Kind);
        }
        finally
        {
            Environment.SetEnvironmentVariable(CredentialVariable, previous);
        }
    }

    [TestMethod]
    public async Task InvalidKeySurfacesProviderFailureHonestly()
    {
        var httpClient = new HttpClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized)));
        using var provider = new ServingTranslationClientProvider(Options.Create(ValidOptions()), httpClient);

        Assert.IsTrue(provider.TryGetClients(out var router, out var chain));
        using var client = (IDisposable)router(chain.Primary);
        var exception = await Assert.ThrowsExactlyAsync<ChatCompletionsAdapterException>(() =>
            ((IChatClient)client).GetResponseAsync([new ChatMessage(ChatRole.User, "probe")]));

        Assert.AreEqual(AttemptFailureKind.ProviderFailure, exception.Kind);
        Assert.AreEqual(401, exception.StatusCode);
    }
}
