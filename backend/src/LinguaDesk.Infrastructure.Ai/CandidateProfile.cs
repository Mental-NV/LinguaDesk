namespace LinguaDesk.Infrastructure.Ai;

public sealed record ReasoningConfiguration(
    string Mode,
    string? Effort);

public sealed record EffectiveSettings(
    double Temperature,
    double? TopP,
    bool HasTools,
    ReasoningConfiguration Reasoning,
    bool JsonResponseMode)
{
    public string Thinking =>
        string.Equals(Reasoning.Mode, CandidateProfile.DisabledReasoningMode, StringComparison.Ordinal)
            ? CandidateProfile.DisabledReasoningMode
            : Reasoning.Effort ?? Reasoning.Mode;
}

public sealed record ContextBounds(
    int MaxOutputTokens,
    int MaxResponseBytes,
    int MaxInputTokens,
    int ContextCapacityTokens,
    int AttemptTimeoutSeconds);

public sealed record BillingProfile(
    string Currency,
    string PriceSource,
    string PriceCheckDate,
    decimal PeakInputPerMillionTokens,
    decimal PeakOutputPerMillionTokens);

public sealed record CandidateProfile(
    string CandidateId,
    string AdapterId,
    string Endpoint,
    string Model,
    string CredentialRef,
    EffectiveSettings Settings,
    string PromptRevision,
    string ValidatorRevision,
    ContextBounds Bounds,
    BillingProfile Billing)
{
    public const string DisabledReasoningMode = "disabled";
    public const string EffortReasoningMode = "effort";
    public const string DefaultReasoningEffort = "low";

    public void Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(CandidateId))
        {
            errors.Add("CandidateId is required.");
        }

        if (string.IsNullOrWhiteSpace(AdapterId))
        {
            errors.Add("AdapterId is required.");
        }

        if (!Uri.TryCreate(Endpoint, UriKind.Absolute, out var endpointUri)
            || !string.Equals(endpointUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("Endpoint must be an absolute HTTPS URL.");
        }

        if (string.IsNullOrWhiteSpace(Model))
        {
            errors.Add("Model is required.");
        }

        if (string.IsNullOrWhiteSpace(CredentialRef))
        {
            errors.Add("CredentialRef is required and purpose-scoped; profiles never carry key material.");
        }

        if (Settings is null)
        {
            errors.Add("Settings are required.");
        }
        else
        {
            if (Settings.Temperature != 0)
            {
                errors.Add("Temperature must be explicitly 0.");
            }

            if (Settings.TopP.HasValue)
            {
                errors.Add("TopP must not be set; no top_p override is sent.");
            }

            if (Settings.HasTools)
            {
                errors.Add("Tools must not be requested.");
            }

            if (Settings.Reasoning is null)
            {
                errors.Add("Reasoning configuration is required.");
            }
            else if (string.Equals(Settings.Reasoning.Mode, DisabledReasoningMode, StringComparison.Ordinal))
            {
                if (Settings.Reasoning.Effort is not null)
                {
                    errors.Add("Disabled reasoning must not specify an effort.");
                }

                if (endpointUri is not null
                    && string.Equals(endpointUri.Host, "openrouter.ai", StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add("OpenRouter profiles must use an explicit reasoning effort.");
                }
            }
            else if (string.Equals(Settings.Reasoning.Mode, EffortReasoningMode, StringComparison.Ordinal))
            {
                if (!IsAllowedReasoningEffort(Settings.Reasoning.Effort))
                {
                    errors.Add("Reasoning effort must be one of: minimal, low, medium, high, xhigh, max.");
                }

                if (endpointUri is not null
                    && string.Equals(endpointUri.Host, "api.deepseek.com", StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add("DeepSeek profiles must keep reasoning disabled.");
                }
            }
            else
            {
                errors.Add("Reasoning mode must be exactly 'disabled' or 'effort'.");
            }

            if (!Settings.JsonResponseMode)
            {
                errors.Add("Bounded JSON response mode is required.");
            }
        }

        if (string.IsNullOrWhiteSpace(PromptRevision))
        {
            errors.Add("PromptRevision is required.");
        }

        if (string.IsNullOrWhiteSpace(ValidatorRevision))
        {
            errors.Add("ValidatorRevision is required.");
        }

        if (Bounds is null)
        {
            errors.Add("Bounds are required; unlimited defaults are rejected.");
        }
        else
        {
            if (Bounds.MaxOutputTokens <= 0)
            {
                errors.Add("Bounds.MaxOutputTokens must be positive and finite.");
            }

            if (Bounds.MaxResponseBytes <= 0)
            {
                errors.Add("Bounds.MaxResponseBytes must be positive and finite.");
            }

            if (Bounds.MaxInputTokens <= 0)
            {
                errors.Add("Bounds.MaxInputTokens must be positive and finite.");
            }

            if (Bounds.ContextCapacityTokens <= 0)
            {
                errors.Add("Bounds.ContextCapacityTokens must be positive and finite.");
            }

            if (Bounds.AttemptTimeoutSeconds <= 0)
            {
                errors.Add("Bounds.AttemptTimeoutSeconds must be positive and finite.");
            }
        }

        if (Billing is null)
        {
            errors.Add("Billing is required; every dispatch needs a finite conservative upper bound.");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(Billing.Currency))
            {
                errors.Add("Billing.Currency is required.");
            }

            if (string.IsNullOrWhiteSpace(Billing.PriceSource))
            {
                errors.Add("Billing.PriceSource is required.");
            }

            if (string.IsNullOrWhiteSpace(Billing.PriceCheckDate))
            {
                errors.Add("Billing.PriceCheckDate is required.");
            }

            if (!(Billing.PeakInputPerMillionTokens > 0))
            {
                errors.Add("Billing.PeakInputPerMillionTokens must be a positive finite rate.");
            }

            if (!(Billing.PeakOutputPerMillionTokens > 0))
            {
                errors.Add("Billing.PeakOutputPerMillionTokens must be a positive finite rate.");
            }
        }

        if (errors.Count > 0)
        {
            throw new CandidateProfileException(CandidateId, errors);
        }
    }

    private static bool IsAllowedReasoningEffort(string? effort) =>
        effort is "minimal" or "low" or "medium" or "high" or "xhigh" or "max";
}

public sealed class CandidateProfileException : InvalidOperationException
{
    public CandidateProfileException(string? candidateId, IEnumerable<string> errors)
        : base($"Candidate profile '{candidateId}' is invalid: {string.Join(" ", errors)}")
    {
        CandidateId = candidateId;
        Errors = [.. errors];
    }

    public string? CandidateId { get; }

    public IReadOnlyList<string> Errors { get; }
}
