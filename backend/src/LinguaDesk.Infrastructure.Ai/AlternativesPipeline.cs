using Microsoft.Extensions.AI;

namespace LinguaDesk.Infrastructure.Ai;

public sealed record AlternativesOutcome(RewritingDecision Decision, IReadOnlyList<string>? Options, string? Category, string? ResolvedLanguage);

public static class AlternativesPipeline
{
    public static async Task<AlternativesOutcome> TransformAsync(AlternativesInput input, EligibilityOutcome eligibility, IChatClient client, CancellationToken cancellationToken = default)
    {
        var validation = input.Validate();
        if (!validation.IsValid || eligibility.Decision != EligibilityDecision.Eligible || eligibility.ResolvedLanguage != input.Language)
            return new(RewritingDecision.Ineligible, null, "invalid-input", null);
        try
        {
            var response = await CompleteResponseBoundary.GetResponseAsync(AlternativesPrompt.Create(input), client, cancellationToken).ConfigureAwait(false);
            if (response.FinishReason is not null && response.FinishReason != ChatFinishReason.Stop)
                return new(RewritingDecision.Failed, null, "truncated", null);
            if (response.Messages.Any(m => m.Contents.Any(c => c is FunctionCallContent))
                || !AlternativesEnvelopeParser.TryParse(response.Text, validation.SelectedText!, out var parsed, input.Language))
                return new(RewritingDecision.Failed, null, "invalid-envelope", null);
            return parsed!.Refused ? new(RewritingDecision.Refused, null, "refused", null)
                : new(RewritingDecision.Succeeded, parsed.Options, null, input.Language);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception) { return new(RewritingDecision.Failed, null, ChatCompletionsAdapter.FailureCategory(exception), null); }
    }
}
