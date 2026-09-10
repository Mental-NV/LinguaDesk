using System.Net;
using System.Text;
using System.Text.Json;
using LinguaDesk.Infrastructure.Ai;

namespace LinguaDesk.Infrastructure.Ai.Tests;

[TestClass]
public sealed class CredentialAndBudgetTests
{
    private const string SyntheticKey = "test-synthetic-key-plainly-fake-002";
    private const string CredentialVariable =
        "LINGUADESK_AIEVALUATION__CREDENTIALS__DEEPSEEK__APIKEY";

    [TestMethod]
    public void TransportCredentialNeverRendersItsValue()
    {
        using var credential = new TransportCredential(SyntheticKey);

        Assert.AreEqual(SyntheticKey, credential.GetApiKey());
        Assert.AreEqual("TransportCredential(<redacted>)", credential.ToString());
        Assert.IsFalse(credential.ToString().Contains(SyntheticKey, StringComparison.Ordinal));
    }

    [TestMethod]
    public void DisposedCredentialReleasesItsValue()
    {
        var credential = new TransportCredential(SyntheticKey);
        credential.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() => credential.GetApiKey());
    }

    [TestMethod]
    public void BlankCredentialMaterialIsRejected()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new TransportCredential("  "));
    }

    [TestMethod]
    public void SerializedProfilesAndDiagnosticsExposeOnlyReferences()
    {
        var profiles = JsonSerializer.Serialize(CandidateRegistry.Default);
        var diagnostics = JsonSerializer.Serialize(new
        {
            candidateId = "DeepSeek-V4.1-Flash",
            credentialRef = "deepseek",
            credentialPresent = true,
            dispatchCount = 1,
        });

        Assert.IsFalse(profiles.Contains(SyntheticKey, StringComparison.Ordinal));
        Assert.IsFalse(diagnostics.Contains(SyntheticKey, StringComparison.Ordinal));
        Assert.IsFalse(profiles.Contains("apiKey", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public async Task AdapterUsesOnlyTheExplicitCredentialNeverAmbientVariables()
    {
        var previous = Environment.GetEnvironmentVariable(CredentialVariable);
        try
        {
            Environment.SetEnvironmentVariable(CredentialVariable, "ambient-key-must-be-ignored-001");
            var observed = await SendWithExplicitCredentialAsync();

            Assert.AreEqual(SyntheticKey, observed);

            Environment.SetEnvironmentVariable(CredentialVariable, null);
            observed = await SendWithExplicitCredentialAsync();

            Assert.AreEqual(SyntheticKey, observed);
        }
        finally
        {
            Environment.SetEnvironmentVariable(CredentialVariable, previous);
        }
    }

    [TestMethod]
    public void UpperBoundReservationUsesPeakRatesRoundedConservatively()
    {
        var bound = EvaluationBudget.UpperBoundUsd(8192, 256, 0.30m, 1.20m);

        Assert.AreEqual(0.002765m, bound);
    }

    [TestMethod]
    public void DispatchCapDeniesFurtherReservations()
    {
        var budget = new EvaluationBudget(1, 10.00m);

        Assert.IsTrue(budget.TryReserve(0.01m, out var first));
        Assert.IsFalse(budget.TryReserve(0.01m, out _));
        Assert.AreEqual(1, budget.DispatchesUsed);

        budget.Settle(first, 0.005m);
        Assert.AreEqual(0.005m, budget.ReservedUsd);
    }

    [TestMethod]
    public void MissingOrInconsistentUsageRetainsTheFullReservation()
    {
        var budget = new EvaluationBudget(4, 10.00m);
        Assert.IsTrue(budget.TryReserve(0.05m, out var unknown));
        Assert.IsTrue(budget.TryReserve(0.05m, out var inconsistent));

        budget.Settle(unknown, null);
        budget.Settle(inconsistent, 99.00m);

        Assert.AreEqual(0.10m, budget.UnresolvedUsd);
    }

    [TestMethod]
    public void SettledActualsBelowTheReservationReleaseExposure()
    {
        var budget = new EvaluationBudget(4, 10.00m);
        Assert.IsTrue(budget.TryReserve(0.05m, out var reservation));

        budget.Settle(reservation, 0.002m);

        Assert.AreEqual(0.002m, budget.ReservedUsd);
        Assert.AreEqual(0m, budget.UnresolvedUsd);
    }

    [TestMethod]
    public void DoubleSettlementIsRejected()
    {
        var budget = new EvaluationBudget(4, 10.00m);
        Assert.IsTrue(budget.TryReserve(0.01m, out var reservation));
        budget.Settle(reservation, 0.001m);

        Assert.ThrowsExactly<InvalidOperationException>(() => budget.Settle(reservation, 0.001m));
    }

    [TestMethod]
    public void NonPositiveBudgetCapsAreRejected()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new EvaluationBudget(0, 1.00m));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new EvaluationBudget(2, 0m));
    }

    private static async Task<string?> SendWithExplicitCredentialAsync()
    {
        string? observed = null;
        var handler = new ObservingHandler(request =>
        {
            observed = request.Headers.Authorization?.Parameter;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    "{\"choices\":[{\"message\":{\"role\":\"assistant\",\"content\":\"{\\\"status\\\":\\\"eligible\\\"}\"},\"finish_reason\":\"stop\"}]," +
                    "\"usage\":{\"prompt_tokens\":10,\"completion_tokens\":2,\"total_tokens\":12}}",
                    Encoding.UTF8,
                    "application/json"),
            };
        });

        var adapter = new ChatCompletionsAdapter(
            CandidateRegistry.Default[0],
            new HttpClient(handler));
        using var credential = new TransportCredential(SyntheticKey);
        await adapter.SendAsync(
            [new PromptMessageSnapshot("system", "Return JSON only.")],
            credential,
            null);

        return observed;
    }

    private sealed class ObservingHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> responder = responder;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(responder(request));
    }
}
