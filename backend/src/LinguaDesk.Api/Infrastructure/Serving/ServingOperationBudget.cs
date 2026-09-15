using LinguaDesk.Infrastructure.Ai;
using Microsoft.Extensions.Options;

namespace LinguaDesk.Api.Infrastructure.Serving;

/// <summary>
/// Builds the per-operation <c>EvaluationBudget</c> that enforces the
/// owner-locked per-operation spend ceiling on the serving path. The chain
/// traversal reserves each stage's conservative upper bound against it, so a
/// dispatch that would breach the ceiling is denied before any provider call
/// and the operation settles as monetary suspension with zero charge and no
/// fallback. An exactly-at-cap bound is admitted. Monthly-cap admission stays
/// with <c>MonetaryAdmissionService</c> independently.
/// </summary>
internal static class ServingOperationBudget
{
    internal const int MaxStagesPerOperation = 4;

    internal const decimal DefaultMaxSpendUsdPerOperation = 0.05m;

    internal static EvaluationBudget ForTranslation(IOptions<ServingOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return new EvaluationBudget(MaxStagesPerOperation, CeilingOrDefault(options.Value?.Translation?.MaxSpendUsdPerOperation));
    }

    internal static EvaluationBudget ForRewriting(IOptions<ServingOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return new EvaluationBudget(MaxStagesPerOperation, CeilingOrDefault(options.Value?.Rewriting?.MaxSpendUsdPerOperation));
    }

    private static decimal CeilingOrDefault(decimal? configured) =>
        configured is decimal value && value > 0 ? value : DefaultMaxSpendUsdPerOperation;
}
