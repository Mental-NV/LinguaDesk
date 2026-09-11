using LinguaDesk.Core;
using Microsoft.Extensions.AI;

namespace LinguaDesk.Infrastructure.Ai;

public enum RewritingDecision
{
    Succeeded,
    Ineligible,
    Refused,
    Failed,
}

public sealed record RewritingInput(
    string Source,
    string? SourceSelection = null,
    string? Mode = null);

public sealed record RewritingOutcome(
    RewritingDecision Decision,
    string? ResolvedSourceLanguage,
    string? Mode,
    string? Text,
    string? Category,
    int EligibilityDispatches,
    int TransformationDispatches,
    int TotalDispatches,
    bool ChargesCharacters,
    int? ExcessCharacters,
    string EligibilityPromptId,
    string EligibilityPromptResourceSha256,
    string PromptId,
    string PromptResourceSha256)
{
    public bool IsTerminalRejection => Decision is RewritingDecision.Ineligible
        or RewritingDecision.Refused
        or RewritingDecision.Failed;
}

public static class RewritingPipeline
{
    public static async Task<RewritingOutcome> RewriteAsync(
        RewritingInput input,
        IChatClient client,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(client);

        var mode = input.Mode ?? ProductCatalog.DefaultRewritingMode;
        if (!ProductCatalog.IsRewritingMode(mode))
        {
            var modeProbe = RewritingPrompt.Create(
                string.Empty,
                "en",
                ProductCatalog.DefaultRewritingMode);
            var eligibilityProbe = EligibilityPrompt.Create(string.Empty, null);
            return new RewritingOutcome(
                RewritingDecision.Ineligible,
                null,
                input.Mode,
                null,
                "invalid-mode",
                0,
                0,
                0,
                ChargesCharacters: false,
                null,
                eligibilityProbe.PromptId,
                eligibilityProbe.ResourceSha256,
                modeProbe.PromptId,
                modeProbe.ResourceSha256);
        }

        var eligibility = await EligibilityPipeline.EvaluateAsync(
            new EligibilityInput(input.Source, EligibilityOperation.Rewriting, input.SourceSelection, Mode: mode),
            client,
            cancellationToken).ConfigureAwait(false);

        if (eligibility.Decision != EligibilityDecision.Eligible)
        {
            var probe = RewritingPrompt.Create(
                string.Empty,
                eligibility.ResolvedLanguage ?? "en",
                mode);
            return new RewritingOutcome(
                eligibility.Decision == EligibilityDecision.Failed
                    ? RewritingDecision.Failed
                    : RewritingDecision.Ineligible,
                null,
                mode,
                null,
                eligibility.Category,
                eligibility.ClassificationDispatches,
                0,
                eligibility.ClassificationDispatches,
                ChargesCharacters: false,
                eligibility.ExcessCharacters,
                eligibility.PromptId,
                eligibility.PromptResourceSha256,
                probe.PromptId,
                probe.ResourceSha256);
        }

        var resolved = eligibility.ResolvedLanguage!;
        var snapshot = RewritingPrompt.Create(input.Source, resolved, mode);

        ChatResponse response;
        try
        {
            response = await CompleteResponseBoundary.GetResponseAsync(
                snapshot,
                client,
                cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return FailedOutcome("provider-failure", eligibility, snapshot, mode);
        }

        if (response.FinishReason == ChatFinishReason.Length)
        {
            return FailedOutcome("truncated", eligibility, snapshot, mode);
        }

        if (CarriesToolCall(response))
        {
            return FailedOutcome("invalid-envelope", eligibility, snapshot, mode);
        }

        var responseText = response.Text;
        if (string.IsNullOrWhiteSpace(responseText))
        {
            return FailedOutcome("invalid-envelope", eligibility, snapshot, mode);
        }

        if (!RewritingEnvelopeParser.TryParse(responseText, out var parsed, out _))
        {
            return FailedOutcome("invalid-envelope", eligibility, snapshot, mode);
        }

        return parsed!.Status switch
        {
            RewritingStatus.Result => new RewritingOutcome(
                RewritingDecision.Succeeded,
                resolved,
                mode,
                parsed.Text,
                null,
                eligibility.ClassificationDispatches,
                1,
                eligibility.ClassificationDispatches + 1,
                ChargesCharacters: false,
                null,
                eligibility.PromptId,
                eligibility.PromptResourceSha256,
                snapshot.PromptId,
                snapshot.ResourceSha256),
            RewritingStatus.Refused => new RewritingOutcome(
                RewritingDecision.Refused,
                null,
                mode,
                null,
                "refused",
                eligibility.ClassificationDispatches,
                1,
                eligibility.ClassificationDispatches + 1,
                ChargesCharacters: false,
                null,
                eligibility.PromptId,
                eligibility.PromptResourceSha256,
                snapshot.PromptId,
                snapshot.ResourceSha256),
            _ => FailedOutcome("invalid-envelope", eligibility, snapshot, mode),
        };
    }

    private static bool CarriesToolCall(ChatResponse response)
    {
        foreach (var message in response.Messages)
        {
            foreach (var content in message.Contents)
            {
                if (content is FunctionCallContent)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static RewritingOutcome FailedOutcome(
        string category,
        EligibilityOutcome eligibility,
        PromptSnapshot snapshot,
        string mode) =>
        new(
            RewritingDecision.Failed,
            null,
            mode,
            null,
            category,
            eligibility.ClassificationDispatches,
            1,
            eligibility.ClassificationDispatches + 1,
            ChargesCharacters: false,
            null,
            eligibility.PromptId,
            eligibility.PromptResourceSha256,
            snapshot.PromptId,
            snapshot.ResourceSha256);
}
