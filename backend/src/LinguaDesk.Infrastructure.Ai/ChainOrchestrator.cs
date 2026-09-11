using LinguaDesk.Core;
using Microsoft.Extensions.AI;

namespace LinguaDesk.Infrastructure.Ai;

public enum ChainDecision
{
    Succeeded,
    Ineligible,
    Refused,
    Failed,
    BudgetDenied,
    DeadlineExceeded,
    Cancelled,
}

public sealed record ChainAttempt(
    string CandidateId,
    string CredentialRef,
    string Stage,
    string FinishCategory,
    int StageDispatches,
    decimal ReservedUsd,
    decimal? ActualUsd,
    long ElapsedMs);

public sealed record ChainOutcome(
    ChainFamily Family,
    ChainDecision Decision,
    string? Text,
    string? Category,
    string? ResolvedLanguage,
    string? TargetOrMode,
    int Dispatches,
    IReadOnlyList<ChainAttempt> Attempts,
    bool ChargesCharacters,
    long ElapsedMs,
    string PrimaryCandidateId,
    string? FallbackCandidateId);

public interface IChainAttemptReporter
{
    AttemptObservation? LastAttempt { get; }
}

public static class ChainOrchestrator
{
    public static Task<ChainOutcome> ExecuteTranslationAsync(
        FamilyChain chain,
        TranslationInput input,
        Func<CandidateProfile, IChatClient> clients,
        ChainPolicy? policy = null,
        EvaluationBudget? budget = null,
        IReadOnlySet<string>? blockedCredentialRefs = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(chain);
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(clients);

        if (chain.Family != ChainFamily.Translation)
        {
            throw new ArgumentException("The translation traversal requires a translation family chain.", nameof(chain));
        }

        var eligibilityInput = new EligibilityInput(
            input.Source, EligibilityOperation.Translation, input.SourceSelection, input.Target);
        return ExecuteAsync<EligibilityOutcome, TranslationOutcome>(
            chain,
            input.Target,
            clients,
            policy,
            budget,
            blockedCredentialRefs,
            (client, stageToken) => EligibilityPipeline.EvaluateAsync(eligibilityInput, client, stageToken),
            static outcome => NormalizeEligibility(outcome),
            (accepted, client, stageToken) => TranslationPipeline.TransformAsync(input, accepted, client, stageToken),
            static outcome => NormalizeTranslation(outcome),
            cancellationToken);
    }

    public static Task<ChainOutcome> ExecuteRewritingAsync(
        FamilyChain chain,
        RewritingInput input,
        Func<CandidateProfile, IChatClient> clients,
        ChainPolicy? policy = null,
        EvaluationBudget? budget = null,
        IReadOnlySet<string>? blockedCredentialRefs = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(chain);
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(clients);

        if (chain.Family != ChainFamily.Rewriting)
        {
            throw new ArgumentException("The rewriting traversal requires a rewriting family chain.", nameof(chain));
        }

        var mode = input.Mode ?? ProductCatalog.DefaultRewritingMode;
        if (!ProductCatalog.IsRewritingMode(mode))
        {
            return Task.FromResult(new ChainOutcome(
                chain.Family,
                ChainDecision.Ineligible,
                null,
                "invalid-mode",
                null,
                input.Mode,
                0,
                [],
                ChargesCharacters: false,
                0,
                chain.Primary.CandidateId,
                chain.Fallback?.CandidateId));
        }

        var eligibilityInput = new EligibilityInput(
            input.Source, EligibilityOperation.Rewriting, input.SourceSelection, Mode: mode);
        return ExecuteAsync<EligibilityOutcome, RewritingOutcome>(
            chain,
            mode,
            clients,
            policy,
            budget,
            blockedCredentialRefs,
            (client, stageToken) => EligibilityPipeline.EvaluateAsync(eligibilityInput, client, stageToken),
            static outcome => NormalizeEligibility(outcome),
            (accepted, client, stageToken) => RewritingPipeline.TransformAsync(input, mode, accepted, client, stageToken),
            static outcome => NormalizeRewriting(outcome),
            cancellationToken);
    }

    private sealed record NormalizedStage(
        bool Succeeded,
        bool Terminal,
        string? Text,
        string? Category,
        string? ResolvedLanguage,
        int Dispatches,
        string FinishCategory,
        string PromptId,
        string PromptResourceSha256);

    private static NormalizedStage NormalizeEligibility(EligibilityOutcome outcome) =>
        outcome.Decision switch
        {
            EligibilityDecision.Eligible => new NormalizedStage(
                true, false, null, null, outcome.ResolvedLanguage,
                outcome.ClassificationDispatches, "eligible",
                outcome.PromptId, outcome.PromptResourceSha256),
            EligibilityDecision.Failed => new NormalizedStage(
                false, false, null, outcome.Category ?? "provider-failure", null,
                outcome.ClassificationDispatches, outcome.Category ?? "provider-failure",
                outcome.PromptId, outcome.PromptResourceSha256),
            _ => new NormalizedStage(
                false, true, null, outcome.Category ?? "rejected", null,
                outcome.ClassificationDispatches, outcome.Category ?? "rejected",
                outcome.PromptId, outcome.PromptResourceSha256),
        };

    private static NormalizedStage NormalizeTranslation(TranslationOutcome outcome) =>
        outcome.Decision switch
        {
            TranslationDecision.Succeeded => new NormalizedStage(
                true, false, outcome.Text, null, outcome.ResolvedSourceLanguage,
                outcome.TransformationDispatches, "success",
                outcome.EligibilityPromptId, outcome.EligibilityPromptResourceSha256),
            TranslationDecision.Ineligible => new NormalizedStage(
                false, true, null, outcome.Category ?? "ineligible", null,
                outcome.TransformationDispatches, outcome.Category ?? "ineligible",
                outcome.EligibilityPromptId, outcome.EligibilityPromptResourceSha256),
            TranslationDecision.Refused => new NormalizedStage(
                false, false, null, "refused", null,
                outcome.TransformationDispatches, "refused",
                outcome.EligibilityPromptId, outcome.EligibilityPromptResourceSha256),
            _ => new NormalizedStage(
                false, false, null, outcome.Category ?? "provider-failure", null,
                outcome.TransformationDispatches, outcome.Category ?? "provider-failure",
                outcome.EligibilityPromptId, outcome.EligibilityPromptResourceSha256),
        };

    private static NormalizedStage NormalizeRewriting(RewritingOutcome outcome) =>
        outcome.Decision switch
        {
            RewritingDecision.Succeeded => new NormalizedStage(
                true, false, outcome.Text, null, outcome.ResolvedSourceLanguage,
                outcome.TransformationDispatches, "success",
                outcome.EligibilityPromptId, outcome.EligibilityPromptResourceSha256),
            RewritingDecision.Ineligible => new NormalizedStage(
                false, true, null, outcome.Category ?? "ineligible", null,
                outcome.TransformationDispatches, outcome.Category ?? "ineligible",
                outcome.EligibilityPromptId, outcome.EligibilityPromptResourceSha256),
            RewritingDecision.Refused => new NormalizedStage(
                false, false, null, "refused", null,
                outcome.TransformationDispatches, "refused",
                outcome.EligibilityPromptId, outcome.EligibilityPromptResourceSha256),
            _ => new NormalizedStage(
                false, false, null, outcome.Category ?? "provider-failure", null,
                outcome.TransformationDispatches, outcome.Category ?? "provider-failure",
                outcome.EligibilityPromptId, outcome.EligibilityPromptResourceSha256),
        };

    private static async Task<ChainOutcome> ExecuteAsync<TEligibility, TTransformation>(
        FamilyChain chain,
        string? targetOrMode,
        Func<CandidateProfile, IChatClient> clients,
        ChainPolicy? policy,
        EvaluationBudget? budget,
        IReadOnlySet<string>? blockedCredentialRefs,
        Func<IChatClient, CancellationToken, Task<TEligibility>> runEligibility,
        Func<TEligibility, NormalizedStage> normalizeEligibility,
        Func<EligibilityOutcome, IChatClient, CancellationToken, Task<TTransformation>> runTransformation,
        Func<TTransformation, NormalizedStage> normalizeTransformation,
        CancellationToken hostCancellationToken)
    {
        var effective = policy ?? ChainPolicy.Default;
        effective.Validate();

        var clock = effective.Clock;
        var start = clock.UtcNow;
        var deadline = start + effective.OverallDeadline;
        var attempts = new List<ChainAttempt>();
        var dispatches = 0;
        var candidateIndex = 0;

        EligibilityOutcome? accepted = null;
        string? lastCategory = null;
        var lastWasRefused = false;

        long ElapsedMs() => (long)(clock.UtcNow - start).TotalMilliseconds;

        ChainOutcome Terminal(ChainDecision decision, string? category, string? text = null) => new(
            chain.Family,
            decision,
            text,
            category,
            null,
            targetOrMode,
            dispatches,
            attempts.AsReadOnly(),
            ChargesCharacters: false,
            ElapsedMs(),
            chain.Primary.CandidateId,
            chain.Fallback?.CandidateId);

        try
        {
            hostCancellationToken.ThrowIfCancellationRequested();
        }
        catch (OperationCanceledException)
        {
            return Terminal(ChainDecision.Cancelled, "cancelled");
        }

        foreach (var profile in chain.CandidatesInOrder())
        {
            var isFallback = candidateIndex > 0;
            candidateIndex++;

            if (blockedCredentialRefs is not null
                && blockedCredentialRefs.Contains(profile.CredentialRef))
            {
                attempts.Add(new ChainAttempt(
                    profile.CandidateId,
                    profile.CredentialRef,
                    isFallback ? "fallback-skipped" : "primary-skipped",
                    "credential-scope-skipped",
                    0,
                    0m,
                    null,
                    ElapsedMs()));
                lastCategory = "credential-scope-blocked";
                lastWasRefused = false;
                continue;
            }

            IChatClient client;
            try
            {
                client = clients(profile);
                ArgumentNullException.ThrowIfNull(client);
            }
            catch (OperationCanceledException)
            {
                return hostCancellationToken.IsCancellationRequested
                    ? Terminal(ChainDecision.Cancelled, "cancelled")
                    : Terminal(ChainDecision.DeadlineExceeded, "deadline-exceeded");
            }
            catch (Exception)
            {
                return Terminal(ChainDecision.Failed, "internal-failure");
            }

            if (accepted is null)
            {
                var eligibility = await DispatchStageAsync(
                    profile,
                    isFallback ? "fallback-eligibility" : "eligibility",
                    effective.EligibilityTimeout,
                    effective,
                    deadline,
                    budget,
                    client,
                    async stageToken => normalizeEligibility(await runEligibility(client, stageToken).ConfigureAwait(false)),
                    hostCancellationToken).ConfigureAwait(false);

                dispatches += eligibility.Dispatches;
                attempts.AddRange(eligibility.Attempts);

                if (eligibility.TerminalDecision.HasValue)
                {
                    return Terminal(eligibility.TerminalDecision.Value, eligibility.TerminalCategory);
                }

                var stage = eligibility.Stage!;

                if (stage.Terminal)
                {
                    return Terminal(ChainDecision.Ineligible, stage.Category);
                }

                if (!stage.Succeeded)
                {
                    lastCategory = stage.Category;
                    lastWasRefused = false;
                    continue;
                }

                accepted = new EligibilityOutcome(
                    EligibilityDecision.Eligible,
                    stage.ResolvedLanguage,
                    null,
                    stage.Dispatches,
                    ChargesCharacters: false,
                    null,
                    stage.PromptId,
                    stage.PromptResourceSha256);
            }

            var transformation = await DispatchStageAsync(
                profile,
                isFallback ? "fallback-transformation" : "transformation",
                effective.TransformationTimeout,
                effective,
                deadline,
                budget,
                client,
                async stageToken => normalizeTransformation(
                    await runTransformation(accepted, client, stageToken).ConfigureAwait(false)),
                hostCancellationToken).ConfigureAwait(false);

            dispatches += transformation.Dispatches;
            attempts.AddRange(transformation.Attempts);

            if (transformation.TerminalDecision.HasValue)
            {
                return Terminal(transformation.TerminalDecision.Value, transformation.TerminalCategory);
            }

            var finished = transformation.Stage!;

            if (finished.Terminal)
            {
                return Terminal(ChainDecision.Ineligible, finished.Category);
            }

            if (finished.Succeeded)
            {
                return new ChainOutcome(
                    chain.Family,
                    ChainDecision.Succeeded,
                    finished.Text,
                    null,
                    finished.ResolvedLanguage,
                    targetOrMode,
                    dispatches,
                    attempts.AsReadOnly(),
                    ChargesCharacters: false,
                    ElapsedMs(),
                    chain.Primary.CandidateId,
                    chain.Fallback?.CandidateId);
            }

            lastCategory = finished.Category;
            lastWasRefused = string.Equals(finished.Category, "refused", StringComparison.Ordinal);
        }

        if (lastCategory is null)
        {
            return Terminal(ChainDecision.Failed, "credential-scope-blocked");
        }

        return Terminal(
            lastWasRefused ? ChainDecision.Refused : ChainDecision.Failed,
            lastCategory);
    }

    private sealed record CollectedStage(
        NormalizedStage? Stage,
        ChainDecision? TerminalDecision,
        string? TerminalCategory,
        int Dispatches,
        IReadOnlyList<ChainAttempt> Attempts);

    private static async Task<CollectedStage> DispatchStageAsync(
        CandidateProfile profile,
        string stage,
        TimeSpan stageTimeout,
        ChainPolicy policy,
        DateTimeOffset deadline,
        EvaluationBudget? budget,
        IChatClient client,
        Func<CancellationToken, Task<NormalizedStage>> run,
        CancellationToken hostCancellationToken)
    {
        var clock = policy.Clock;
        long ElapsedSince(DateTimeOffset moment) => (long)(clock.UtcNow - moment).TotalMilliseconds;

        var remaining = deadline - clock.UtcNow;
        if (remaining < stageTimeout + policy.FinalizationReserve)
        {
            return new CollectedStage(null, ChainDecision.DeadlineExceeded, "deadline-exceeded", 0, []);
        }

        BudgetReservation? reservation = null;
        decimal reserved = 0m;
        if (budget is not null)
        {
            reserved = EvaluationBudget.UpperBoundUsd(
                profile.Bounds.MaxInputTokens,
                profile.Bounds.MaxOutputTokens,
                profile.Billing.PeakInputPerMillionTokens,
                profile.Billing.PeakOutputPerMillionTokens);
            if (!budget.TryReserve(reserved, out reservation))
            {
                return new CollectedStage(null, ChainDecision.BudgetDenied, "budget-denied", 0, []);
            }
        }

        void Settle(decimal? actualUsd)
        {
            if (budget is not null && reservation is not null)
            {
                budget.Settle(reservation, actualUsd);
            }
        }

        var effectiveTimeout = TimeoutOf(remaining, stageTimeout, policy.FinalizationReserve);
        using var stageSource = CancellationTokenSource.CreateLinkedTokenSource(hostCancellationToken);
        stageSource.CancelAfter(effectiveTimeout);

        var stageStart = clock.UtcNow;
        NormalizedStage finished;
        try
        {
            finished = await run(stageSource.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (hostCancellationToken.IsCancellationRequested)
        {
            Settle(null);
            return new CollectedStage(null, ChainDecision.Cancelled, "cancelled", 0, []);
        }
        catch (OperationCanceledException)
        {
            Settle(null);
            return new CollectedStage(null, ChainDecision.DeadlineExceeded, "deadline-exceeded", 0, []);
        }
        catch (Exception)
        {
            Settle(null);
            return new CollectedStage(null, ChainDecision.Failed, "internal-failure", 0, []);
        }

        if (clock.UtcNow >= deadline)
        {
            Settle(null);
            var fenced = new ChainAttempt(
                profile.CandidateId,
                profile.CredentialRef,
                stage,
                "deadline-fenced",
                finished.Dispatches,
                reserved,
                null,
                ElapsedSince(stageStart));
            return new CollectedStage(
                null, ChainDecision.DeadlineExceeded, "deadline-exceeded", finished.Dispatches, [fenced]);
        }

        var actual = (client as IChainAttemptReporter)?.LastAttempt?.ActualUsd;
        if (finished.Dispatches == 0)
        {
            Settle(0m);
            reserved = 0m;
        }
        else
        {
            Settle(actual);
        }

        var attempt = new ChainAttempt(
            profile.CandidateId,
            profile.CredentialRef,
            stage,
            finished.FinishCategory,
            finished.Dispatches,
            reserved,
            finished.Dispatches == 0 ? 0m : actual,
            ElapsedSince(stageStart));
        return new CollectedStage(finished, null, null, finished.Dispatches, [attempt]);
    }

    private static TimeSpan TimeoutOf(TimeSpan remaining, TimeSpan stageTimeout, TimeSpan reserve)
    {
        var budget = remaining - reserve;
        if (budget <= TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        return budget < stageTimeout ? budget : stageTimeout;
    }
}
