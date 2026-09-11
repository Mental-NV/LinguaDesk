using LinguaDesk.Core;
using Microsoft.Extensions.AI;

namespace LinguaDesk.Infrastructure.Ai;

public enum EligibilityOperation
{
    Translation,
    Rewriting,
}

public enum EligibilityDecision
{
    Eligible,
    Uncertain,
    Unsupported,
    Mixed,
    SourceMismatch,
    Refused,
    LocallyRejected,
    Failed,
}

public sealed record EligibilityInput(
    string Source,
    EligibilityOperation Operation,
    string? SourceSelection = null,
    string? Target = null,
    string? Mode = null);

public sealed record EligibilityOutcome(
    EligibilityDecision Decision,
    string? ResolvedLanguage,
    string? Category,
    int ClassificationDispatches,
    bool ChargesCharacters,
    int? ExcessCharacters,
    string PromptId,
    string PromptResourceSha256)
{
    public bool IsTerminalRejection => Decision is EligibilityDecision.LocallyRejected
        or EligibilityDecision.Uncertain
        or EligibilityDecision.Unsupported
        or EligibilityDecision.Mixed
        or EligibilityDecision.SourceMismatch
        or EligibilityDecision.Refused
        or EligibilityDecision.Failed;
}

public static class EligibilityPipeline
{
    public static int MaximumSourceCharacters(EligibilityOperation operation) => operation switch
    {
        EligibilityOperation.Translation => ProductCatalog.TranslationMaximumSourceCharacters,
        EligibilityOperation.Rewriting => ProductCatalog.RewritingMaximumSourceCharacters,
        _ => throw new ArgumentOutOfRangeException(nameof(operation)),
    };

    public static async Task<EligibilityOutcome> EvaluateAsync(
        EligibilityInput input,
        IChatClient client,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(client);

        var gate = ApplyLocalGates(input);
        if (gate is not null)
        {
            return gate;
        }

        var hint = NormalizeHint(input.SourceSelection);
        var snapshot = EligibilityPrompt.Create(input.Source, hint);

        string responseText;
        try
        {
            var response = await CompleteResponseBoundary.GetResponseAsync(
                snapshot,
                client,
                cancellationToken).ConfigureAwait(false);
            responseText = response.Text ?? string.Empty;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return FailedOutcome("provider-failure");
        }

        if (string.IsNullOrWhiteSpace(responseText))
        {
            return FailedOutcome("invalid-envelope");
        }

        if (!EligibilityEnvelopeParser.TryParse(responseText, hint, out var classification, out _))
        {
            return FailedOutcome("invalid-envelope");
        }

        return classification!.Status switch
        {
            EligibilityStatus.Eligible => new EligibilityOutcome(
                EligibilityDecision.Eligible,
                classification.Language,
                null,
                1,
                ChargesCharacters: false,
                null,
                snapshot.PromptId,
                snapshot.ResourceSha256),
            EligibilityStatus.Uncertain => TerminalOutcome(EligibilityDecision.Uncertain, "uncertain", snapshot),
            EligibilityStatus.Unsupported => TerminalOutcome(EligibilityDecision.Unsupported, "unsupported", snapshot),
            EligibilityStatus.Mixed => TerminalOutcome(EligibilityDecision.Mixed, "mixed", snapshot),
            EligibilityStatus.SourceMismatch => TerminalOutcome(
                EligibilityDecision.SourceMismatch, "source-mismatch", snapshot),
            EligibilityStatus.Refused => TerminalOutcome(EligibilityDecision.Refused, "refused", snapshot),
            _ => FailedOutcome("invalid-envelope"),
        };

        static EligibilityOutcome FailedOutcome(string category)
        {
            var probe = EligibilityPrompt.Create(string.Empty, null);
            return new EligibilityOutcome(
                EligibilityDecision.Failed,
                null,
                category,
                1,
                ChargesCharacters: false,
                null,
                probe.PromptId,
                probe.ResourceSha256);
        }
    }

    private static EligibilityOutcome? ApplyLocalGates(EligibilityInput input)
    {
        ArgumentNullException.ThrowIfNull(input.Source);

        var limit = MaximumSourceCharacters(input.Operation);
        var analysis = ScalarInputPolicy.Analyze(input.Source, limit);

        if (!analysis.IsUnicodeValid)
        {
            return Gate("invalid-unicode", null);
        }

        if (analysis.IsEmptyOrWhitespace)
        {
            return Gate("empty", null);
        }

        if (analysis.IsOversized)
        {
            return Gate("oversize", analysis.Excess);
        }

        var effectiveSource = input.SourceSelection ?? ProductCatalog.AutomaticSource;
        if (!ProductCatalog.IsSourceSelection(effectiveSource))
        {
            return Gate("invalid-source-selection", null);
        }

        if (input.Operation == EligibilityOperation.Translation)
        {
            if (!ProductCatalog.IsLanguage(input.Target))
            {
                return Gate("invalid-target", null);
            }

            if (!string.Equals(effectiveSource, ProductCatalog.AutomaticSource, StringComparison.Ordinal)
                && string.Equals(effectiveSource, input.Target, StringComparison.Ordinal))
            {
                return Gate("equal-source-target", null);
            }
        }
        else
        {
            var effectiveMode = input.Mode ?? ProductCatalog.DefaultRewritingMode;
            if (!ProductCatalog.IsRewritingMode(effectiveMode))
            {
                return Gate("invalid-mode", null);
            }
        }

        return null;

        static EligibilityOutcome Gate(string category, int? excess)
        {
            var probe = EligibilityPrompt.Create(string.Empty, null);
            return new EligibilityOutcome(
                EligibilityDecision.LocallyRejected,
                null,
                category,
                0,
                ChargesCharacters: false,
                excess,
                probe.PromptId,
                probe.ResourceSha256);
        }
    }

    private static EligibilityOutcome TerminalOutcome(
        EligibilityDecision decision,
        string category,
        PromptSnapshot snapshot) =>
        new(
            decision,
            null,
            category,
            1,
            ChargesCharacters: false,
            null,
            snapshot.PromptId,
            snapshot.ResourceSha256);

    private static string? NormalizeHint(string? sourceSelection) =>
        EligibilityEnvelopeParser.IsSupportedLanguage(sourceSelection) ? sourceSelection : null;
}
