namespace LinguaDesk.Infrastructure.Ai;

public sealed record EffectiveSettings(
    double Temperature,
    double? TopP,
    bool HasTools,
    string Thinking,
    bool JsonResponseMode);

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
    public const string RequiredThinkingMode = "disabled";

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
            errors.Add("CredentialRef is required and provider-scoped; profiles never carry key material.");
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

            if (!string.Equals(Settings.Thinking, RequiredThinkingMode, StringComparison.Ordinal))
            {
                errors.Add("Thinking must be explicitly 'disabled'.");
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
