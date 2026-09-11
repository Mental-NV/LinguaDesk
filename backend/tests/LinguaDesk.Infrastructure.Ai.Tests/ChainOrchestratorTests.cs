using System.Net;
using System.Text.Json;
using LinguaDesk.Core;
using LinguaDesk.Infrastructure.Ai;
using Microsoft.Extensions.AI;

namespace LinguaDesk.Infrastructure.Ai.Tests;

[TestClass]
public sealed class ChainOrchestratorTests
{
    private const string TranslatedText = "Translated synthetic text.";
    private const string RewrittenText = "Rewritten synthetic text.";
    private const string FallbackText = "Fallback synthetic text.";

    private static FamilyChain TranslationChain() => FamilyChain.EvaluationDefault(ChainFamily.Translation);

    private static FamilyChain RewritingChain() => FamilyChain.EvaluationDefault(ChainFamily.Rewriting);

    private static ChainPolicy PolicyFor(ManualChainClock clock) => new(
        TimeSpan.FromSeconds(30),
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(10),
        TimeSpan.FromSeconds(2),
        clock);

    private static decimal PerDispatchReservation() => EvaluationBudget.UpperBoundUsd(
        8192,
        256,
        0.30m,
        1.20m);

    private static Func<CandidateProfile, IChatClient> Router(SequenceClient primary, SequenceClient fallback) =>
        profile => string.Equals(profile.CandidateId, FamilyChain.EvaluationPrimaryId, StringComparison.Ordinal)
            ? primary
            : fallback;

    private static string SourceOfLastCall(SequenceClient client)
    {
        using var document = JsonDocument.Parse(client.UserTexts[^1]);
        return document.RootElement.GetProperty("source").GetString()!;
    }

    [TestMethod]
    public async Task TranslationPrimarySuccessNeverDispatchesTheFallback()
    {
        using var primary = new SequenceClient();
        primary.EnqueueResponse("{\"status\":\"eligible\",\"language\":\"en\"}");
        primary.EnqueueResponse($"{{\"status\":\"result\",\"text\":\"{TranslatedText}\"}}");
        using var fallback = new SequenceClient();

        var outcome = await ChainOrchestrator.ExecuteTranslationAsync(
            TranslationChain(),
            new TranslationInput("Please send the report tomorrow.", Target: "ru"),
            Router(primary, fallback),
            PolicyFor(new ManualChainClock(DateTimeOffset.UtcNow)));

        Assert.AreEqual(ChainDecision.Succeeded, outcome.Decision);
        Assert.AreEqual(TranslatedText, outcome.Text);
        Assert.IsNull(outcome.Category);
        Assert.AreEqual("en", outcome.ResolvedLanguage);
        Assert.AreEqual("ru", outcome.TargetOrMode);
        Assert.AreEqual(2, outcome.Dispatches);
        Assert.AreEqual(2, primary.CallCount);
        Assert.AreEqual(0, fallback.CallCount);
        Assert.IsFalse(outcome.ChargesCharacters);
    }

    [TestMethod]
    public async Task TranslationPathAPrimaryEligibilityFailsFallbackServesOriginalSource()
    {
        const string source = "Please send the report tomorrow.";
        using var primary = new SequenceClient();
        primary.EnqueueThrow(new HttpRequestException("synthetic provider failure"));
        using var fallback = new SequenceClient();
        fallback.EnqueueResponse("{\"status\":\"eligible\",\"language\":\"en\"}");
        fallback.EnqueueResponse($"{{\"status\":\"result\",\"text\":\"{FallbackText}\"}}");

        var outcome = await ChainOrchestrator.ExecuteTranslationAsync(
            TranslationChain(),
            new TranslationInput(source, Target: "ru"),
            Router(primary, fallback),
            PolicyFor(new ManualChainClock(DateTimeOffset.UtcNow)));

        Assert.AreEqual(ChainDecision.Succeeded, outcome.Decision);
        Assert.AreEqual(FallbackText, outcome.Text);
        Assert.IsNull(outcome.Category);
        Assert.AreEqual(3, outcome.Dispatches);
        Assert.AreEqual(1, primary.CallCount);
        Assert.AreEqual(2, fallback.CallCount);
        Assert.AreEqual(source, SourceOfLastCall(fallback));
        Assert.IsFalse(outcome.ChargesCharacters);
    }

    [TestMethod]
    public async Task TranslationPathBFailedTransformationReusesEligibilityWithoutRefetch()
    {
        using var primary = new SequenceClient();
        primary.EnqueueResponse("{\"status\":\"eligible\",\"language\":\"en\"}");
        primary.EnqueueResponse("{\"status\":\"eligible\"}");
        using var fallback = new SequenceClient();
        fallback.EnqueueResponse($"{{\"status\":\"result\",\"text\":\"{FallbackText}\"}}");

        var outcome = await ChainOrchestrator.ExecuteTranslationAsync(
            TranslationChain(),
            new TranslationInput("Please send the report tomorrow.", Target: "ru"),
            Router(primary, fallback),
            PolicyFor(new ManualChainClock(DateTimeOffset.UtcNow)));

        Assert.AreEqual(ChainDecision.Succeeded, outcome.Decision);
        Assert.AreEqual(FallbackText, outcome.Text);
        Assert.AreEqual(3, outcome.Dispatches);
        Assert.AreEqual(2, primary.CallCount);
        Assert.AreEqual(1, fallback.CallCount);
        Assert.AreEqual(
            "fallback-transformation",
            outcome.Attempts[^1].Stage);
        Assert.IsFalse(outcome.Attempts.Any(static attempt => attempt.Stage == "fallback-eligibility"));
    }

    [TestMethod]
    public async Task RewritingBothThreeDispatchPathsServeWithEligibilityReuse()
    {
        foreach (var path in new[] { "A", "B" })
        {
            using var primary = new SequenceClient();
            using var fallback = new SequenceClient();
            if (path == "A")
            {
                primary.EnqueueThrow(new HttpRequestException("synthetic provider failure"));
                fallback.EnqueueResponse("{\"status\":\"eligible\",\"language\":\"en\"}");
                fallback.EnqueueResponse($"{{\"status\":\"result\",\"text\":\"{FallbackText}\"}}");
            }
            else
            {
                primary.EnqueueResponse("{\"status\":\"eligible\",\"language\":\"en\"}");
                primary.EnqueueResponse("{\"status\":\"refused\",\"text\":null}");
                fallback.EnqueueResponse($"{{\"status\":\"result\",\"text\":\"{FallbackText}\"}}");
            }

            var outcome = await ChainOrchestrator.ExecuteRewritingAsync(
                RewritingChain(),
                new RewritingInput("Please send the report tomorrow.", Mode: "simple"),
                Router(primary, fallback),
                PolicyFor(new ManualChainClock(DateTimeOffset.UtcNow)));

            Assert.AreEqual(ChainDecision.Succeeded, outcome.Decision, $"path {path}");
            Assert.AreEqual(FallbackText, outcome.Text, $"path {path}");
            Assert.AreEqual(3, outcome.Dispatches, $"path {path}");
            Assert.AreEqual("simple", outcome.TargetOrMode, $"path {path}");
            Assert.IsFalse(outcome.ChargesCharacters, $"path {path}");
            if (path == "B")
            {
                Assert.AreEqual(1, fallback.CallCount, "path B reuses accepted eligibility");
            }
        }
    }

    [TestMethod]
    public async Task ExhaustedCandidatesEndWithoutRetryOrReturn()
    {
        using var primary = new SequenceClient();
        primary.EnqueueResponse("{\"status\":\"eligible\",\"language\":\"en\"}");
        primary.EnqueueThrow(new HttpRequestException("synthetic provider failure"));
        using var fallback = new SequenceClient();
        fallback.EnqueueThrow(new HttpRequestException("synthetic fallback failure"));

        var outcome = await ChainOrchestrator.ExecuteTranslationAsync(
            TranslationChain(),
            new TranslationInput("Please send the report tomorrow.", Target: "ru"),
            Router(primary, fallback),
            PolicyFor(new ManualChainClock(DateTimeOffset.UtcNow)));

        Assert.AreEqual(ChainDecision.Failed, outcome.Decision);
        Assert.AreEqual("provider-failure", outcome.Category);
        Assert.IsNull(outcome.Text);
        Assert.AreEqual(3, outcome.Dispatches);
        Assert.AreEqual(2, primary.CallCount);
        Assert.AreEqual(1, fallback.CallCount);
        Assert.IsFalse(outcome.ChargesCharacters);
    }

    [TestMethod]
    public async Task FallbackSuccessIsIndistinguishableFromPrimarySuccess()
    {
        using var directPrimary = new SequenceClient();
        directPrimary.EnqueueResponse("{\"status\":\"eligible\",\"language\":\"en\"}");
        directPrimary.EnqueueResponse($"{{\"status\":\"result\",\"text\":\"{TranslatedText}\"}}");
        using var unusedFallback = new SequenceClient();
        var direct = await ChainOrchestrator.ExecuteTranslationAsync(
            TranslationChain(),
            new TranslationInput("Please send the report tomorrow.", Target: "ru"),
            Router(directPrimary, unusedFallback),
            PolicyFor(new ManualChainClock(DateTimeOffset.UtcNow)));

        using var failedPrimary = new SequenceClient();
        failedPrimary.EnqueueResponse("{\"status\":\"eligible\",\"language\":\"en\"}");
        failedPrimary.EnqueueResponse("{\"status\":\"eligible\"}");
        using var servingFallback = new SequenceClient();
        servingFallback.EnqueueResponse($"{{\"status\":\"result\",\"text\":\"{FallbackText}\"}}");
        var viaFallback = await ChainOrchestrator.ExecuteTranslationAsync(
            TranslationChain(),
            new TranslationInput("Please send the report tomorrow.", Target: "ru"),
            Router(failedPrimary, servingFallback),
            PolicyFor(new ManualChainClock(DateTimeOffset.UtcNow)));

        Assert.AreEqual(direct.Decision, viaFallback.Decision);
        Assert.AreEqual(direct.Category, viaFallback.Category);
        Assert.AreEqual(direct.ChargesCharacters, viaFallback.ChargesCharacters);
        Assert.IsNotNull(viaFallback.Text);
        Assert.IsNull(viaFallback.Category);
    }

    [TestMethod]
    [DataRow("transient")]
    [DataRow("invalid-envelope")]
    [DataRow("truncated")]
    [DataRow("refused")]
    public async Task EligibleTransformationFaultsAdvanceOnceToTheFallback(string fault)
    {
        using var primary = new SequenceClient();
        primary.EnqueueResponse("{\"status\":\"eligible\",\"language\":\"en\"}");
        switch (fault)
        {
            case "transient":
                primary.EnqueueThrow(new HttpRequestException("synthetic timeout"));
                break;
            case "invalid-envelope":
                primary.EnqueueResponse("{\"status\":\"eligible\"}");
                break;
            case "truncated":
                primary.EnqueueBehavior(_ => Task.FromResult(
                    new ChatResponse(new ChatMessage(ChatRole.Assistant, "{\"status\":\"result\","))
                    {
                        FinishReason = ChatFinishReason.Length,
                    }));
                break;
            default:
                primary.EnqueueResponse("{\"status\":\"refused\",\"text\":null}");
                break;
        }

        using var fallback = new SequenceClient();
        fallback.EnqueueResponse($"{{\"status\":\"result\",\"text\":\"{FallbackText}\"}}");

        var outcome = await ChainOrchestrator.ExecuteTranslationAsync(
            TranslationChain(),
            new TranslationInput("Please send the report tomorrow.", Target: "ru"),
            Router(primary, fallback),
            PolicyFor(new ManualChainClock(DateTimeOffset.UtcNow)));

        Assert.AreEqual(ChainDecision.Succeeded, outcome.Decision, fault);
        Assert.AreEqual(FallbackText, outcome.Text, fault);
        Assert.AreEqual(3, outcome.Dispatches, fault);
    }

    [TestMethod]
    [DataRow("provider-failure")]
    [DataRow("invalid-envelope")]
    public async Task FailedEligibilityAdvancesOnceToTheFallback(string fault)
    {
        using var primary = new SequenceClient();
        if (string.Equals(fault, "provider-failure", StringComparison.Ordinal))
        {
            primary.EnqueueThrow(new HttpRequestException("synthetic provider failure"));
        }
        else
        {
            primary.EnqueueResponse("not json at all {{{");
        }

        using var fallback = new SequenceClient();
        fallback.EnqueueResponse("{\"status\":\"eligible\",\"language\":\"en\"}");
        fallback.EnqueueResponse($"{{\"status\":\"result\",\"text\":\"{FallbackText}\"}}");

        var outcome = await ChainOrchestrator.ExecuteTranslationAsync(
            TranslationChain(),
            new TranslationInput("Please send the report tomorrow.", Target: "ru"),
            Router(primary, fallback),
            PolicyFor(new ManualChainClock(DateTimeOffset.UtcNow)));

        Assert.AreEqual(ChainDecision.Succeeded, outcome.Decision, fault);
        Assert.AreEqual(3, outcome.Dispatches, fault);
    }

    [TestMethod]
    [DataRow("empty", "   ")]
    [DataRow("uncertain", "12345")]
    public async Task TerminalInputEndsWithNoFallback(string name, string source)
    {
        using var primary = new SequenceClient();
        if (!string.Equals(name, "empty", StringComparison.Ordinal))
        {
            primary.EnqueueResponse("{\"status\":\"uncertain\",\"language\":null}");
        }

        using var fallback = new SequenceClient();

        var outcome = await ChainOrchestrator.ExecuteTranslationAsync(
            TranslationChain(),
            new TranslationInput(source, Target: "ru"),
            Router(primary, fallback),
            PolicyFor(new ManualChainClock(DateTimeOffset.UtcNow)));

        Assert.AreEqual(ChainDecision.Ineligible, outcome.Decision, name);
        Assert.IsNull(outcome.Text, name);
        Assert.AreEqual(0, fallback.CallCount, name);
        Assert.IsFalse(outcome.ChargesCharacters, name);
    }

    [TestMethod]
    public async Task OversizedInputEndsWithNoFallback()
    {
        using var primary = new SequenceClient();
        using var fallback = new SequenceClient();

        var outcome = await ChainOrchestrator.ExecuteTranslationAsync(
            TranslationChain(),
            new TranslationInput(new string('a', ProductCatalog.TranslationMaximumSourceCharacters + 1), Target: "ru"),
            Router(primary, fallback),
            PolicyFor(new ManualChainClock(DateTimeOffset.UtcNow)));

        Assert.AreEqual(ChainDecision.Ineligible, outcome.Decision);
        Assert.AreEqual("oversize", outcome.Category);
        Assert.AreEqual(0, outcome.Dispatches);
        Assert.AreEqual(0, primary.CallCount);
        Assert.AreEqual(0, fallback.CallCount);
    }

    [TestMethod]
    public async Task EligibilityRefusalStaysTerminalPerSelectedMapping()
    {
        using var primary = new SequenceClient();
        primary.EnqueueResponse("{\"status\":\"refused\",\"language\":null}");
        using var fallback = new SequenceClient();

        var outcome = await ChainOrchestrator.ExecuteTranslationAsync(
            TranslationChain(),
            new TranslationInput("Please send the report tomorrow.", Target: "ru"),
            Router(primary, fallback),
            PolicyFor(new ManualChainClock(DateTimeOffset.UtcNow)));

        Assert.AreEqual(ChainDecision.Ineligible, outcome.Decision);
        Assert.AreEqual("refused", outcome.Category);
        Assert.AreEqual(1, outcome.Dispatches);
        Assert.AreEqual(0, fallback.CallCount);
    }

    [TestMethod]
    public async Task InvalidRewritingModeEndsWithZeroDispatches()
    {
        using var primary = new SequenceClient();
        using var fallback = new SequenceClient();

        var outcome = await ChainOrchestrator.ExecuteRewritingAsync(
            RewritingChain(),
            new RewritingInput("Please send the report tomorrow.", Mode: "no-such-mode"),
            Router(primary, fallback),
            PolicyFor(new ManualChainClock(DateTimeOffset.UtcNow)));

        Assert.AreEqual(ChainDecision.Ineligible, outcome.Decision);
        Assert.AreEqual("invalid-mode", outcome.Category);
        Assert.AreEqual(0, outcome.Dispatches);
        Assert.AreEqual(0, primary.CallCount);
        Assert.AreEqual(0, fallback.CallCount);
    }

    [TestMethod]
    public async Task BlockedPrimaryScopeIsSkippedWithoutDispatch()
    {
        using var primary = new SequenceClient();
        using var fallback = new SequenceClient();
        fallback.EnqueueResponse("{\"status\":\"eligible\",\"language\":\"en\"}");
        fallback.EnqueueResponse($"{{\"status\":\"result\",\"text\":\"{FallbackText}\"}}");

        var outcome = await ChainOrchestrator.ExecuteTranslationAsync(
            TranslationChain(),
            new TranslationInput("Please send the report tomorrow.", Target: "ru"),
            Router(primary, fallback),
            PolicyFor(new ManualChainClock(DateTimeOffset.UtcNow)),
            blockedCredentialRefs: new HashSet<string>(StringComparer.Ordinal) { "deepseek" });

        Assert.AreEqual(ChainDecision.Succeeded, outcome.Decision);
        Assert.AreEqual(FallbackText, outcome.Text);
        Assert.AreEqual(0, primary.CallCount);
        Assert.AreEqual(2, fallback.CallCount);
        Assert.AreEqual("primary-skipped", outcome.Attempts[0].Stage);
        Assert.AreEqual("credential-scope-skipped", outcome.Attempts[0].FinishCategory);
        Assert.AreEqual(0, outcome.Attempts[0].StageDispatches);
    }

    [TestMethod]
    public async Task FullyBlockedCredentialScopeEndsUnavailable()
    {
        using var primary = new SequenceClient();
        using var fallback = new SequenceClient();

        var outcome = await ChainOrchestrator.ExecuteTranslationAsync(
            TranslationChain(),
            new TranslationInput("Please send the report tomorrow.", Target: "ru"),
            Router(primary, fallback),
            PolicyFor(new ManualChainClock(DateTimeOffset.UtcNow)),
            blockedCredentialRefs: new HashSet<string>(StringComparer.Ordinal) { "deepseek", "deepseek-secondary" });

        Assert.AreEqual(ChainDecision.Failed, outcome.Decision);
        Assert.AreEqual("credential-scope-blocked", outcome.Category);
        Assert.AreEqual(0, outcome.Dispatches);
        Assert.AreEqual(0, primary.CallCount);
        Assert.AreEqual(0, fallback.CallCount);
    }

    [TestMethod]
    public async Task EveryDispatchReservesConservativeExposureBeforeLaunch()
    {
        using var primary = new SequenceClient();
        primary.EnqueueResponse("{\"status\":\"eligible\",\"language\":\"en\"}");
        primary.EnqueueResponse("{\"status\":\"eligible\"}");
        using var fallback = new SequenceClient();
        fallback.EnqueueResponse($"{{\"status\":\"result\",\"text\":\"{FallbackText}\"}}");

        var budget = new EvaluationBudget(10, 100m);
        var outcome = await ChainOrchestrator.ExecuteTranslationAsync(
            TranslationChain(),
            new TranslationInput("Please send the report tomorrow.", Target: "ru"),
            Router(primary, fallback),
            PolicyFor(new ManualChainClock(DateTimeOffset.UtcNow)),
            budget);

        var expected = 3 * PerDispatchReservation();
        Assert.AreEqual(ChainDecision.Succeeded, outcome.Decision);
        Assert.AreEqual(3, budget.DispatchesUsed);
        Assert.AreEqual(expected, budget.ReservedUsd);
        Assert.AreEqual(expected, budget.UnresolvedUsd);
        Assert.IsTrue(outcome.Attempts.All(static attempt => attempt.ReservedUsd == PerDispatchReservation()));
    }

    [TestMethod]
    public async Task DeniedBudgetStopsTraversalWithNoFurtherDispatch()
    {
        using var primary = new SequenceClient();
        primary.EnqueueResponse("{\"status\":\"eligible\",\"language\":\"en\"}");
        using var fallback = new SequenceClient();

        var budget = new EvaluationBudget(1, 100m);
        var outcome = await ChainOrchestrator.ExecuteTranslationAsync(
            TranslationChain(),
            new TranslationInput("Please send the report tomorrow.", Target: "ru"),
            Router(primary, fallback),
            PolicyFor(new ManualChainClock(DateTimeOffset.UtcNow)),
            budget);

        Assert.AreEqual(ChainDecision.BudgetDenied, outcome.Decision);
        Assert.AreEqual("budget-denied", outcome.Category);
        Assert.AreEqual(1, outcome.Dispatches);
        Assert.AreEqual(1, primary.CallCount);
        Assert.AreEqual(0, fallback.CallCount);
        Assert.IsNull(outcome.Text);
    }

    [TestMethod]
    public async Task NoDispatchStartsWhenTheStageAllowanceNoLongerFits()
    {
        var clock = new ManualChainClock(DateTimeOffset.UtcNow);
        using var primary = new SequenceClient();
        primary.EnqueueBehavior(_ =>
        {
            clock.Advance(TimeSpan.FromSeconds(24));
            return Task.FromResult(new ChatResponse(
                new ChatMessage(ChatRole.Assistant, "{\"status\":\"eligible\",\"language\":\"en\"}")));
        });
        using var fallback = new SequenceClient();

        var outcome = await ChainOrchestrator.ExecuteTranslationAsync(
            TranslationChain(),
            new TranslationInput("Please send the report tomorrow.", Target: "ru"),
            Router(primary, fallback),
            PolicyFor(clock));

        Assert.AreEqual(ChainDecision.DeadlineExceeded, outcome.Decision);
        Assert.AreEqual("deadline-exceeded", outcome.Category);
        Assert.AreEqual(1, outcome.Dispatches);
        Assert.IsNull(outcome.Text);
        Assert.AreEqual(1, primary.CallCount);
        Assert.AreEqual(0, fallback.CallCount);
    }

    [TestMethod]
    public async Task FakeTimeExpiryDuringEligibilityEndsWithoutPostDeadlineDispatch()
    {
        var clock = new ManualChainClock(DateTimeOffset.UtcNow);
        using var primary = new SequenceClient();
        primary.EnqueueBehavior(_ =>
        {
            clock.Advance(TimeSpan.FromSeconds(26));
            return Task.FromResult(new ChatResponse(
                new ChatMessage(ChatRole.Assistant, "{\"status\":\"eligible\",\"language\":\"en\"}")));
        });
        using var fallback = new SequenceClient();

        var outcome = await ChainOrchestrator.ExecuteTranslationAsync(
            TranslationChain(),
            new TranslationInput("Please send the report tomorrow.", Target: "ru"),
            Router(primary, fallback),
            PolicyFor(clock));

        Assert.AreEqual(ChainDecision.DeadlineExceeded, outcome.Decision);
        Assert.IsNull(outcome.Text);
        Assert.AreEqual(1, outcome.Dispatches);
        Assert.AreEqual(1, primary.CallCount);
        Assert.AreEqual(0, fallback.CallCount);
    }

    [TestMethod]
    public async Task LateTransformationSuccessIsFencedAndNeverWins()
    {
        var clock = new ManualChainClock(DateTimeOffset.UtcNow);
        using var primary = new SequenceClient();
        primary.EnqueueResponse("{\"status\":\"eligible\",\"language\":\"en\"}");
        primary.EnqueueBehavior(_ =>
        {
            clock.Advance(TimeSpan.FromSeconds(40));
            return Task.FromResult(new ChatResponse(
                new ChatMessage(ChatRole.Assistant, $"{{\"status\":\"result\",\"text\":\"{TranslatedText}\"}}")));
        });
        using var fallback = new SequenceClient();

        var outcome = await ChainOrchestrator.ExecuteTranslationAsync(
            TranslationChain(),
            new TranslationInput("Please send the report tomorrow.", Target: "ru"),
            Router(primary, fallback),
            PolicyFor(clock));

        Assert.AreEqual(ChainDecision.DeadlineExceeded, outcome.Decision);
        Assert.AreEqual("deadline-exceeded", outcome.Category);
        Assert.IsNull(outcome.Text);
        Assert.AreEqual("deadline-fenced", outcome.Attempts[^1].FinishCategory);
        Assert.AreEqual(0, fallback.CallCount);
    }

    [TestMethod]
    public async Task FakeTimeExpiryDuringFallbackEndsBeforeTheFinalStage()
    {
        var clock = new ManualChainClock(DateTimeOffset.UtcNow);
        using var primary = new SequenceClient();
        primary.EnqueueResponse("{\"status\":\"eligible\",\"language\":\"en\"}");
        primary.EnqueueBehavior(_ =>
        {
            clock.Advance(TimeSpan.FromSeconds(20));
            throw new HttpRequestException("synthetic provider failure");
        });
        using var fallback = new SequenceClient();

        var outcome = await ChainOrchestrator.ExecuteTranslationAsync(
            TranslationChain(),
            new TranslationInput("Please send the report tomorrow.", Target: "ru"),
            Router(primary, fallback),
            PolicyFor(clock));

        Assert.AreEqual(ChainDecision.DeadlineExceeded, outcome.Decision);
        Assert.AreEqual("deadline-exceeded", outcome.Category);
        Assert.IsNull(outcome.Text);
        Assert.AreEqual(2, outcome.Dispatches);
        Assert.AreEqual(2, primary.CallCount);
        Assert.AreEqual(0, fallback.CallCount);
    }

    [TestMethod]
    public async Task CancelledHostOperationEndsWithoutSuccess()
    {
        using var primary = new SequenceClient();
        using var fallback = new SequenceClient();
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();

        var outcome = await ChainOrchestrator.ExecuteTranslationAsync(
            TranslationChain(),
            new TranslationInput("Please send the report tomorrow.", Target: "ru"),
            Router(primary, fallback),
            PolicyFor(new ManualChainClock(DateTimeOffset.UtcNow)),
            cancellationToken: cancelled.Token);

        Assert.AreEqual(ChainDecision.Cancelled, outcome.Decision);
        Assert.AreEqual("cancelled", outcome.Category);
        Assert.IsNull(outcome.Text);
        Assert.AreEqual(0, outcome.Dispatches);
        Assert.AreEqual(0, primary.CallCount);
    }

    [TestMethod]
    public async Task MissingCandidateClientEndsAsInternalFailureWithoutFallback()
    {
        using var fallback = new SequenceClient();

        var outcome = await ChainOrchestrator.ExecuteTranslationAsync(
            TranslationChain(),
            new TranslationInput("Please send the report tomorrow.", Target: "ru"),
            _ => null!,
            PolicyFor(new ManualChainClock(DateTimeOffset.UtcNow)));

        Assert.AreEqual(ChainDecision.Failed, outcome.Decision);
        Assert.AreEqual("internal-failure", outcome.Category);
        Assert.AreEqual(0, outcome.Dispatches);
        Assert.AreEqual(0, fallback.CallCount);
    }

    [TestMethod]
    public async Task InvalidDeadlinePolicyIsRejected()
    {
        var clock = new ManualChainClock(DateTimeOffset.UtcNow);
        var policy = new ChainPolicy(
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(2),
            clock);
        using var primary = new SequenceClient();
        using var fallback = new SequenceClient();

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => ChainOrchestrator.ExecuteTranslationAsync(
            TranslationChain(),
            new TranslationInput("Please send the report tomorrow.", Target: "ru"),
            Router(primary, fallback),
            policy));
    }

    [TestMethod]
    public async Task EqualTranslationLanguagesEndWithNoFallback()
    {
        using var primary = new SequenceClient();
        primary.EnqueueResponse("{\"status\":\"eligible\",\"language\":\"en\"}");
        using var fallback = new SequenceClient();

        var outcome = await ChainOrchestrator.ExecuteTranslationAsync(
            TranslationChain(),
            new TranslationInput("Please send the report tomorrow.", Target: "en"),
            Router(primary, fallback),
            PolicyFor(new ManualChainClock(DateTimeOffset.UtcNow)));

        Assert.AreEqual(ChainDecision.Ineligible, outcome.Decision);
        Assert.AreEqual("equal-source-target", outcome.Category);
        Assert.AreEqual(1, outcome.Dispatches);
        Assert.AreEqual(0, fallback.CallCount);
    }

    private sealed class ManualChainClock(DateTimeOffset start) : IChainClock
    {
        public DateTimeOffset Current = start;

        public DateTimeOffset UtcNow => Current;

        public void Advance(TimeSpan span) => Current += span;
    }

    private sealed class SequenceClient : IChatClient
    {
        private readonly Queue<Func<CancellationToken, Task<ChatResponse>>> behaviors = new();

        public int CallCount { get; private set; }

        public List<string> UserTexts { get; } = [];

        public void EnqueueResponse(string json) =>
            behaviors.Enqueue(_ => Task.FromResult(
                new ChatResponse(new ChatMessage(ChatRole.Assistant, json))));

        public void EnqueueThrow(Exception exception) =>
            behaviors.Enqueue(_ => Task.FromException<ChatResponse>(exception));

        public void EnqueueBehavior(Func<CancellationToken, Task<ChatResponse>> behavior) =>
            behaviors.Enqueue(behavior);

        public void Dispose()
        {
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            var materialized = messages.ToArray();
            UserTexts.Add(materialized[^1].Text);
            return behaviors.Dequeue()(cancellationToken);
        }

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("The chain tests never stream.");
    }
}
