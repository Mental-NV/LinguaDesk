using LinguaDesk.Api.Infrastructure.Serving;
using LinguaDesk.Core;
using LinguaDesk.Infrastructure.Ai;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace LinguaDesk.Api.Features.Operations;

public sealed record AlternativesExecutionResult(ChainDecision Decision, SettlementResult Settlement, IReadOnlyList<string>? Options, string? Category);

public sealed class AlternativesOperationCoordinator(
    OperationSettlementService settlement,
    OperationRecoveryService recovery,
    MonetaryAdmissionService monetary,
    IRewritingClientProvider clients,
    IOptions<MonetaryAdmissionOptions> monetaryOptions,
    IOptions<ServingOptions> servingOptions,
    TimeProvider timeProvider)
{
    public async Task<AlternativesExecutionResult> ExecuteAsync(string accountId, AlternativesRequest request, OperationSubmission submission, CancellationToken cancellationToken)
    {
        var cap = LedgerSnapshot.ResolveCapOrZero(monetaryOptions);
        async Task<SettlementResult> Settle(bool success, CancellationToken token) => success
            ? await settlement.SettleSuccessAsync(accountId, OperationAdmissionService.FamilyRewriting, request.Context, request.Language, null, request.Mode, request.OperationId, token, cap, request).ConfigureAwait(false)
            : await settlement.SettleFailureAsync(accountId, OperationAdmissionService.FamilyRewriting, request.Context, request.Language, null, request.Mode, request.OperationId, token, cap, request).ConfigureAwait(false);
        if (!clients.TryGetClients(out var router, out var chain))
            return new(ChainDecision.Failed, await Settle(false, cancellationToken).ConfigureAwait(false), null, "provider-unavailable");
        var input = new AlternativesInput(request.Context, request.Start, request.Length, request.Association, request.Language, request.Mode);
        var remaining = submission.DeadlineUtc - timeProvider.GetUtcNow();
        if (remaining <= TimeSpan.FromSeconds(5))
            return new(ChainDecision.DeadlineExceeded, await Settle(false, cancellationToken).ConfigureAwait(false), null, "deadline-exceeded");
        var policy = new ChainPolicy(remaining, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(2), new Clock(timeProvider));
        var gate = new GatingRouter(router, monetary, request.OperationId.ToString("D"));
        ChainOutcome outcome;
        try
        {
            outcome = await ChainOrchestrator.ExecuteAlternativesAsync(chain, input, gate.Resolve, policy,
                ServingOperationBudget.ForAlternatives(servingOptions), cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            var interrupted = await recovery.InterruptAsync(accountId, OperationAdmissionService.FamilyRewriting,
                request.Context, request.Language, null, request.Mode, request.OperationId, CancellationToken.None, cap, request).ConfigureAwait(false);
            return new(ChainDecision.Cancelled, new(SettlementOutcome.Fenced, interrupted.Submission, interrupted.Usage, interrupted.ServerTime), null, "cancelled");
        }
        catch (InvalidOperationException)
        {
            return new(ChainDecision.Failed, await Settle(false, CancellationToken.None).ConfigureAwait(false), null, "profile-ineligible");
        }
        var success = outcome.Decision == ChainDecision.Succeeded && outcome.Options?.Count == 2 && timeProvider.GetUtcNow() < submission.DeadlineUtc;
        var settled = await Settle(success, CancellationToken.None).ConfigureAwait(false);
        if (gate.Denied) return new(ChainDecision.BudgetDenied, settled, null, "budget-denied");
        if (success && settled.Outcome == SettlementOutcome.Settled && settled.Submission?.State == OperationStates.Succeeded)
            return new(ChainDecision.Succeeded, settled, outcome.Options, null);
        return new(outcome.Decision == ChainDecision.Succeeded ? ChainDecision.DeadlineExceeded : outcome.Decision, settled, null, outcome.Category);
    }

    private sealed class Clock(TimeProvider time) : IChainClock { public DateTimeOffset UtcNow => time.GetUtcNow(); }

    private sealed class GatingRouter(Func<CandidateProfile, IChatClient> router, MonetaryAdmissionService monetary, string operation)
    {
        private int dispatches;
        private readonly MonetaryAdmissionService admission = monetary;
        private readonly string operationId = operation;
        public bool Denied { get; private set; }
        public IChatClient Resolve(CandidateProfile profile) => new Client(this, profile, router(profile));
        private sealed class Client(GatingRouter owner, CandidateProfile profile, IChatClient inner) : IChatClient, IChainAttemptReporter
        {
            public AttemptObservation? LastAttempt => (inner as IChainAttemptReporter)?.LastAttempt;
            public void Dispose() => inner.Dispose();
            public object? GetService(Type serviceType, object? serviceKey = null) => inner.GetService(serviceType, serviceKey);
            public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
            {
                var number = ++owner.dispatches;
                if (number > 3) throw new InvalidOperationException("Alternatives dispatch bound reached.");
                var snapshots = messages.Select(m => new PromptMessageSnapshot(m.Role.Value, m.Text)).ToArray();
                var inputBound = ChatCompletionsAdapter.ConservativeInputTokenBound(profile, snapshots);
                if (inputBound > profile.Bounds.MaxInputTokens) throw new InvalidOperationException("Alternatives prompt exceeds its bound.");
                var attemptId = $"{owner.operationId}:alternatives-attempt-{number}";
                var admitted = await owner.admission.AdmitAsync(attemptId, owner.operationId, number, new AttemptCostProfile(
                    profile.Billing.Currency, profile.Billing.PriceSource, profile.Billing.PriceCheckDate,
                    profile.Billing.PeakInputPerMillionTokens, profile.Billing.PeakOutputPerMillionTokens,
                    inputBound, profile.Bounds.MaxOutputTokens, 0), cancellationToken).ConfigureAwait(false);
                if (admitted.Outcome != MonetaryAdmissionOutcome.Admitted)
                {
                    owner.Denied = true;
                    throw new InvalidOperationException("Alternatives monetary admission denied.");
                }
                var response = await inner.GetResponseAsync(snapshots.Select(m => new ChatMessage(new ChatRole(m.Role), m.Content)), options, cancellationToken).ConfigureAwait(false);
                // Missing billing evidence retains the complete reservation. Smoke is explicitly nonbillable.
                var smoke = inner is Infrastructure.Smoke.DeterministicSmokeRewritingClient;
                await owner.admission.ReconcileAsync(attemptId, new(null, smoke, smoke ? "smoke-alternatives-v1" : null), CancellationToken.None).ConfigureAwait(false);
                return response;
            }
            public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) => throw new NotSupportedException("Alternatives never stream.");
        }
    }
}
