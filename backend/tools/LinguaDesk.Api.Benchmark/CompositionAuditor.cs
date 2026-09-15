namespace LinguaDesk.Api.Benchmark;

/// <summary>
/// Expected composition distilled from the manifest expansion for auditing.
/// </summary>
public sealed record ExpectedCombo(
    string ComboId,
    string Family,
    string Route,
    string BandId,
    string SourceSha256,
    int SourceLength,
    int BandMin,
    int BandMax);

/// <summary>
/// One audited observation. Replays carry a repeated identity or no fresh
/// submission timing; both fail the run.
/// </summary>
public sealed record ObservedRequest(
    string OperationId,
    string ComboId,
    bool IsRepeat,
    bool SubmittedFresh,
    bool HasTiming,
    string SourceSha256,
    int SourceLength);

public sealed record AuditResult(bool Passed, IReadOnlyList<string> Violations);

/// <summary>
/// Fails the run on any missing band/cell, repeated identity, replayed
/// completion reused as a generation sample, or dropped request.
/// </summary>
public static class CompositionAuditor
{
    public static AuditResult Audit(
        IReadOnlyList<ExpectedCombo> expected,
        IReadOnlyList<ObservedRequest> observed,
        int expectedTotal)
    {
        ArgumentNullException.ThrowIfNull(expected);
        ArgumentNullException.ThrowIfNull(observed);
        var violations = new List<string>();

        if (observed.Count != expectedTotal)
        {
            violations.Add(
                $"Expected {expectedTotal} measured requests but observed {observed.Count}; dropped requests are retained, never hidden.");
        }

        var operationIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var request in observed)
        {
            if (string.IsNullOrWhiteSpace(request.OperationId))
            {
                violations.Add($"Combo '{request.ComboId}' carries no submission identity.");
            }
            else if (!operationIds.Add(request.OperationId))
            {
                violations.Add($"Operation identity '{request.OperationId}' repeats; every repeat needs a fresh identity.");
            }

            if (!request.SubmittedFresh || !request.HasTiming)
            {
                violations.Add(
                    $"Combo '{request.ComboId}' operation '{request.OperationId}' is a replayed completion, not a fresh submission.");
            }
        }

        var byCombo = expected.ToDictionary(combo => combo.ComboId, StringComparer.Ordinal);
        var observedByCombo = observed
            .GroupBy(request => request.ComboId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.Ordinal);

        foreach (var combo in expected)
        {
            if (!observedByCombo.TryGetValue(combo.ComboId, out var matches))
            {
                violations.Add($"Missing band/cell: combo '{combo.ComboId}' ({combo.Family} {combo.Route} {combo.BandId}) has no observation.");
                continue;
            }

            if (matches.Count != 2
                || matches.Count(match => !match.IsRepeat) != 1
                || matches.Count(match => match.IsRepeat) != 1)
            {
                violations.Add(
                    $"Combo '{combo.ComboId}' needs exactly one original and one fresh-identity repeat, found {matches.Count}.");
            }

            foreach (var match in matches)
            {
                if (!string.Equals(match.SourceSha256, combo.SourceSha256, StringComparison.Ordinal)
                    || match.SourceLength != combo.SourceLength)
                {
                    violations.Add($"Combo '{combo.ComboId}' observation source does not match the declared combination.");
                }
                else if (match.SourceLength < combo.BandMin || match.SourceLength > combo.BandMax)
                {
                    violations.Add($"Combo '{combo.ComboId}' source length {match.SourceLength} sits outside band '{combo.BandId}'.");
                }
            }
        }

        foreach (var comboId in observedByCombo.Keys)
        {
            if (!byCombo.ContainsKey(comboId))
            {
                violations.Add($"Observation combo '{comboId}' is not declared in the manifest.");
            }
        }

        return new AuditResult(violations.Count == 0, violations);
    }
}
