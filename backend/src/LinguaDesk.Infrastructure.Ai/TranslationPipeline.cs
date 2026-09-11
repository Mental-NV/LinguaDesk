using LinguaDesk.Core;
using Microsoft.Extensions.AI;

namespace LinguaDesk.Infrastructure.Ai;

public enum TranslationDecision
{
    Succeeded,
    Ineligible,
    Refused,
    Failed,
}

public sealed record TranslationInput(
    string Source,
    string? SourceSelection = null,
    string? Target = null);

public sealed record TranslationOutcome(
    TranslationDecision Decision,
    string? ResolvedSourceLanguage,
    string? Target,
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
    public bool IsTerminalRejection => Decision is TranslationDecision.Ineligible
        or TranslationDecision.Refused
        or TranslationDecision.Failed;
}

public static class TranslationPipeline
{
    public static async Task<TranslationOutcome> TranslateAsync(
        TranslationInput input,
        IChatClient client,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(client);

        var eligibility = await EligibilityPipeline.EvaluateAsync(
            new EligibilityInput(input.Source, EligibilityOperation.Translation, input.SourceSelection, input.Target),
            client,
            cancellationToken).ConfigureAwait(false);

        if (eligibility.Decision != EligibilityDecision.Eligible)
        {
            var probe = TranslationPrompt.Create(
                string.Empty,
                eligibility.ResolvedLanguage ?? "en",
                input.Target ?? "en");
            return new TranslationOutcome(
                eligibility.Decision == EligibilityDecision.Failed
                    ? TranslationDecision.Failed
                    : TranslationDecision.Ineligible,
                null,
                input.Target,
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
        var target = input.Target!;
        if (string.Equals(resolved, target, StringComparison.Ordinal))
        {
            return IneligibleOutcome("equal-source-target", eligibility, input, resolved, target);
        }

        var snapshot = TranslationPrompt.Create(input.Source, resolved, target);

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
            return FailedOutcome("provider-failure", eligibility, snapshot, input);
        }

        if (response.FinishReason == ChatFinishReason.Length)
        {
            return FailedOutcome("truncated", eligibility, snapshot, input);
        }

        if (CarriesToolCall(response))
        {
            return FailedOutcome("invalid-envelope", eligibility, snapshot, input);
        }

        var responseText = response.Text;
        if (string.IsNullOrWhiteSpace(responseText))
        {
            return FailedOutcome("invalid-envelope", eligibility, snapshot, input);
        }

        if (!TranslationEnvelopeParser.TryParse(responseText, out var parsed, out _))
        {
            return FailedOutcome("invalid-envelope", eligibility, snapshot, input);
        }

        return parsed!.Status switch
        {
            TranslationStatus.Result => new TranslationOutcome(
                TranslationDecision.Succeeded,
                resolved,
                target,
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
            TranslationStatus.Refused => new TranslationOutcome(
                TranslationDecision.Refused,
                null,
                target,
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
            _ => FailedOutcome("invalid-envelope", eligibility, snapshot, input),
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

    private static TranslationOutcome IneligibleOutcome(
        string category,
        EligibilityOutcome eligibility,
        TranslationInput input,
        string resolved,
        string target)
    {
        var probe = TranslationPrompt.Create(string.Empty, resolved, target);
        return new TranslationOutcome(
            TranslationDecision.Ineligible,
            null,
            input.Target,
            null,
            category,
            eligibility.ClassificationDispatches,
            0,
            eligibility.ClassificationDispatches,
            ChargesCharacters: false,
            null,
            eligibility.PromptId,
            eligibility.PromptResourceSha256,
            probe.PromptId,
            probe.ResourceSha256);
    }

    private static TranslationOutcome FailedOutcome(
        string category,
        EligibilityOutcome eligibility,
        PromptSnapshot snapshot,
        TranslationInput input) =>
        new(
            TranslationDecision.Failed,
            null,
            input.Target,
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
