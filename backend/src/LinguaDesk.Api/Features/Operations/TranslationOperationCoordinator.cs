using LinguaDesk.Infrastructure.Ai;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace LinguaDesk.Api.Features.Operations;

public enum TranslationExecutionOutcome
{
    Succeeded,
    ProviderUnavailable,
    InputEligibilityRejected,
    ProcessingFailed,
    DeadlineExceeded,
    MonetarySuspended,
    Cancelled,
}

public sealed record TranslationExecutionResult(
    TranslationExecutionOutcome Outcome,
    OperationSubmission? Submission,
    UsageSnapshotData Usage,
    DateTimeOffset ServerTime,
    string? TranslatedText = null,
    string? EligibilityReason = null,
    int Dispatches = 0);

/// <summary>
/// Executes one admitted translation operation synchronously: per-dispatch
/// monetary admission, M016 translation pipeline plus M018 explicit chain
/// traversal behind the stored absolute deadline, result validation, and
/// settle-once commitment through the existing settlement/recovery services.
/// Late output never resurrects a terminal state; every failure path carries
/// zero character charge.
/// </summary>
public sealed partial class TranslationOperationCoordinator(
    OperationSettlementService settlement,
    OperationRecoveryService recovery,
    MonetaryAdmissionService monetary,
    ITranslationClientProvider clients,
    IOptions<MonetaryAdmissionOptions> monetaryOptions,
    TimeProvider timeProvider,
    ILogger<TranslationOperationCoordinator> logger)
{
    internal const string NoChargeEvidenceReference = "fake-translation-harness-v1";

    public async Task<TranslationExecutionResult> ExecuteAdmittedAsync(
        string accountId,
        string source,
        string? sourceSelection,
        string? target,
        Guid operationId,
        OperationSubmission submission,
        UsageSnapshotData admittedUsage,
        DateTimeOffset admittedTime,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(accountId);
        ArgumentNullException.ThrowIfNull(submission);
        ArgumentNullException.ThrowIfNull(admittedUsage);

        if (!clients.TryGetClients(out var router, out var chain) || router is null || chain is null)
        {
            Log.ProviderUnavailable(logger, submission.ScalarCount);
            return new(
                TranslationExecutionOutcome.ProviderUnavailable,
                submission,
                admittedUsage,
                admittedTime,
                Dispatches: 0);
        }

        var gate = new MonetaryGatingClient(
            router,
            operationId.ToString("D"),
            monetary);
        var remaining = submission.DeadlineUtc - timeProvider.GetUtcNow();
        ChainOutcome outcome;
        try
        {
            var policy = new ChainPolicy(
                remaining <= TimeSpan.Zero ? TimeSpan.Zero : remaining,
                ChainPolicy.Default.EligibilityTimeout,
                ChainPolicy.Default.TransformationTimeout,
                ChainPolicy.Default.FinalizationReserve,
                new TimeProviderChainClock(timeProvider));
            policy.Validate();
            outcome = await ChainOrchestrator.ExecuteTranslationAsync(
                chain,
                new TranslationInput(source, sourceSelection, target),
                gate.Resolve,
                policy,
                budget: null,
                blockedCredentialRefs: null,
                cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidOperationException)
        {
            return await FailAsync(
                accountId, source, sourceSelection, target, operationId, submission, gate,
                TranslationExecutionOutcome.DeadlineExceeded, eligibilityReason: null, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            await gate.ReconcileAllAsync(CancellationToken.None).ConfigureAwait(false);
            var interrupted = await recovery.InterruptAsync(
                accountId,
                OperationAdmissionService.FamilyTranslation,
                source,
                sourceSelection,
                target,
                mode: null,
                operationId,
                CancellationToken.None,
                LedgerSnapshot.ResolveCapOrZero(monetaryOptions)).ConfigureAwait(false);
            Log.Cancelled(logger, submission.ScalarCount, gate.Dispatches);
            return new(
                TranslationExecutionOutcome.Cancelled,
                interrupted.Submission,
                interrupted.Usage,
                interrupted.ServerTime,
                Dispatches: gate.Dispatches);
        }

        await gate.ReconcileAllAsync(cancellationToken).ConfigureAwait(false);

        if (gate.DeniedOutcome is not null)
        {
            return await FailAsync(
                accountId, source, sourceSelection, target, operationId, submission, gate,
                TranslationExecutionOutcome.MonetarySuspended, eligibilityReason: null, cancellationToken).ConfigureAwait(false);
        }

        switch (outcome.Decision)
        {
            case ChainDecision.Succeeded when !string.IsNullOrWhiteSpace(outcome.Text):
                if (timeProvider.GetUtcNow() >= submission.DeadlineUtc)
                {
                    Log.LateSuccessFenced(logger, submission.ScalarCount, gate.Dispatches);
                    return await FailAsync(
                        accountId, source, sourceSelection, target, operationId, submission, gate,
                        TranslationExecutionOutcome.DeadlineExceeded, eligibilityReason: null, cancellationToken).ConfigureAwait(false);
                }

                var settled = await settlement.SettleSuccessAsync(
                    accountId,
                    OperationAdmissionService.FamilyTranslation,
                    source,
                    sourceSelection,
                    target,
                    mode: null,
                    operationId,
                    cancellationToken,
                    LedgerSnapshot.ResolveCapOrZero(monetaryOptions)).ConfigureAwait(false);
                Log.Succeeded(logger, submission.ScalarCount, submission.AdmissionDay, gate.Dispatches);
                return new(
                    TranslationExecutionOutcome.Succeeded,
                    settled.Submission,
                    settled.Usage,
                    settled.ServerTime,
                    TranslatedText: outcome.Text,
                    Dispatches: gate.Dispatches);
            case ChainDecision.Succeeded:
                return await FailAsync(
                    accountId, source, sourceSelection, target, operationId, submission, gate,
                    TranslationExecutionOutcome.ProcessingFailed, eligibilityReason: null, cancellationToken).ConfigureAwait(false);
            case ChainDecision.Ineligible:
                return await FailAsync(
                    accountId, source, sourceSelection, target, operationId, submission, gate,
                    TranslationExecutionOutcome.InputEligibilityRejected,
                    MapEligibilityReason(outcome.Category),
                    cancellationToken).ConfigureAwait(false);
            case ChainDecision.DeadlineExceeded:
                return await FailAsync(
                    accountId, source, sourceSelection, target, operationId, submission, gate,
                    TranslationExecutionOutcome.DeadlineExceeded, eligibilityReason: null, cancellationToken).ConfigureAwait(false);
            case ChainDecision.Cancelled:
                return await FailAsync(
                    accountId, source, sourceSelection, target, operationId, submission, gate,
                    TranslationExecutionOutcome.DeadlineExceeded, eligibilityReason: null, cancellationToken).ConfigureAwait(false);
            default:
                return await FailAsync(
                    accountId, source, sourceSelection, target, operationId, submission, gate,
                    TranslationExecutionOutcome.ProcessingFailed, eligibilityReason: null, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<TranslationExecutionResult> FailAsync(
        string accountId,
        string source,
        string? sourceSelection,
        string? target,
        Guid operationId,
        OperationSubmission submission,
        MonetaryGatingClient gate,
        TranslationExecutionOutcome outcome,
        string? eligibilityReason,
        CancellationToken cancellationToken)
    {
        var failed = await settlement.SettleFailureAsync(
            accountId,
            OperationAdmissionService.FamilyTranslation,
            source,
            sourceSelection,
            target,
            mode: null,
            operationId,
            cancellationToken,
            LedgerSnapshot.ResolveCapOrZero(monetaryOptions)).ConfigureAwait(false);
        var outcomeName = outcome switch
        {
            TranslationExecutionOutcome.InputEligibilityRejected => nameof(TranslationExecutionOutcome.InputEligibilityRejected),
            TranslationExecutionOutcome.ProcessingFailed => nameof(TranslationExecutionOutcome.ProcessingFailed),
            TranslationExecutionOutcome.DeadlineExceeded => nameof(TranslationExecutionOutcome.DeadlineExceeded),
            TranslationExecutionOutcome.MonetarySuspended => nameof(TranslationExecutionOutcome.MonetarySuspended),
            _ => nameof(TranslationExecutionOutcome.ProcessingFailed),
        };
        Log.SettledFailure(logger, submission.ScalarCount, outcomeName, gate.Dispatches);
        return new(
            outcome,
            failed.Submission,
            failed.Usage,
            failed.ServerTime,
            EligibilityReason: eligibilityReason,
            Dispatches: gate.Dispatches);
    }

    private static string MapEligibilityReason(string? category) =>
        category switch
        {
            "source-mismatch" => "sourceMismatch",
            "equal-source-target" => "sameLanguage",
            "uncertain" => "uncertain",
            "unsupported" => "unsupported",
            "mixed" => "mixed",
            _ => "uncertain",
        };

    private static partial class Log
    {
        [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Translation execution unavailable: {ScalarCount} characters remain pending with no provider dispatch.")]
        public static partial void ProviderUnavailable(ILogger logger, int scalarCount);

        [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Translation succeeded: {ScalarCount} characters charged on {AdmissionDay} after {Dispatches} dispatches.")]
        public static partial void Succeeded(ILogger logger, int scalarCount, string admissionDay, int dispatches);

        [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "Translation settled as failure: {ScalarCount} characters {Outcome} after {Dispatches} dispatches with zero charge.")]
        public static partial void SettledFailure(ILogger logger, int scalarCount, string outcome, int dispatches);

        [LoggerMessage(EventId = 4, Level = LogLevel.Information, Message = "Translation success arrived after the stored deadline and was fenced: {ScalarCount} characters with zero charge after {Dispatches} dispatches.")]
        public static partial void LateSuccessFenced(ILogger logger, int scalarCount, int dispatches);

        [LoggerMessage(EventId = 5, Level = LogLevel.Information, Message = "Translation execution cancelled: {ScalarCount} characters fenced after {Dispatches} dispatches.")]
        public static partial void Cancelled(ILogger logger, int scalarCount, int dispatches);
    }

    /// <summary>
    /// Admits monetary exposure per dispatch before delegating to the
    /// provider client. A denial throws before any provider work, so the
    /// chain observes exhaustion without dispatching further attempts.
    /// </summary>
    private sealed class MonetaryGatingClient(
        Func<CandidateProfile, IChatClient> router,
        string operationReference,
        MonetaryAdmissionService monetary)
    {
        private readonly List<string> admittedAttemptIds = [];

        public int Dispatches { get; private set; }

        public MonetaryAdmissionOutcome? DeniedOutcome { get; private set; }

        public IChatClient Resolve(CandidateProfile profile)
        {
            ArgumentNullException.ThrowIfNull(profile);
            var inner = router(profile);
            ArgumentNullException.ThrowIfNull(inner);
            return new GatedClient(this, profile, inner);
        }

        public async Task ReconcileAllAsync(CancellationToken cancellationToken)
        {
            foreach (var attemptId in admittedAttemptIds)
            {
                await monetary.ReconcileAsync(
                    attemptId,
                    new ReconciliationEvidence(null, true, NoChargeEvidenceReference),
                    cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task<MonetaryAdmissionResult> AdmitNextAsync(
            CandidateProfile profile,
            CancellationToken cancellationToken)
        {
            Dispatches++;
            var result = await monetary.AdmitAsync(
                $"{operationReference}:translation-attempt-{Dispatches}",
                operationReference,
                Dispatches,
                new AttemptCostProfile(
                    profile.Billing.Currency,
                    profile.Billing.PriceSource,
                    profile.Billing.PriceCheckDate,
                    profile.Billing.PeakInputPerMillionTokens,
                    profile.Billing.PeakOutputPerMillionTokens,
                    profile.Bounds.MaxInputTokens,
                    profile.Bounds.MaxOutputTokens,
                    0),
                cancellationToken).ConfigureAwait(false);
            if (result.Outcome is MonetaryAdmissionOutcome.Admitted or MonetaryAdmissionOutcome.DuplicateObserved)
            {
                if (result.Reservation is not null)
                {
                    admittedAttemptIds.Add(result.Reservation.AttemptId);
                }

                return result;
            }

            DeniedOutcome = result.Outcome;
            throw new MonetaryDeniedException(result.Outcome);
        }

        private sealed class GatedClient(
            MonetaryGatingClient gate,
            CandidateProfile profile,
            IChatClient inner) : IChatClient
        {
            public void Dispose() => inner.Dispose();

            public object? GetService(Type serviceType, object? serviceKey = null) =>
                inner.GetService(serviceType, serviceKey);

            public async Task<ChatResponse> GetResponseAsync(
                IEnumerable<ChatMessage> messages,
                ChatOptions? options = null,
                CancellationToken cancellationToken = default)
            {
                await gate.AdmitNextAsync(profile, cancellationToken).ConfigureAwait(false);
                return await inner.GetResponseAsync(messages, options, cancellationToken).ConfigureAwait(false);
            }

            public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
                IEnumerable<ChatMessage> messages,
                ChatOptions? options = null,
                CancellationToken cancellationToken = default) =>
                throw new NotSupportedException("The translation execution path never streams.");
        }
    }

    private sealed class MonetaryDeniedException(MonetaryAdmissionOutcome outcome) : InvalidOperationException(
        $"Monetary admission denied translation dispatch: {outcome}.")
    {
    }

    private sealed class TimeProviderChainClock(TimeProvider timeProvider) : IChainClock
    {
        public DateTimeOffset UtcNow => timeProvider.GetUtcNow();
    }
}
