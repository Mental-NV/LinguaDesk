using System.ComponentModel;
using LinguaDesk.Api.Features.Capabilities;
using System.Text.Json.Serialization;

namespace LinguaDesk.Api.Features.Operations;

[JsonConverter(typeof(JsonStringEnumConverter<OperationFamily>))]
public enum OperationFamily
{
    [JsonStringEnumMemberName("translation")]
    Translation,
    [JsonStringEnumMemberName("rewriting")]
    Rewriting,
}

[JsonConverter(typeof(JsonStringEnumConverter<OperationKind>))]
public enum OperationKind
{
    [JsonStringEnumMemberName("fullText")] FullText,
    [JsonStringEnumMemberName("sentenceAlternatives")] SentenceAlternatives,
}

/// <summary>Bounded exact current result and a complete sentence in scalar offsets. No caller-supplied charge.</summary>
public sealed record AlternativesRequest(
    [property: Description("Client-generated UUIDv7 identity unique within the account across all kinds.")]
    Guid OperationId,
    [property: Description("Exact current result context, at most 8000 Unicode scalars; never normalized or stored.")]
    string Context,
    [property: Description("Zero-based Unicode scalar offset of the selected complete sentence.")]
    int Start,
    [property: Description("Selected complete sentence length, from 1 to 2000 Unicode scalars.")]
    int Length,
    [property: Description("Opaque nonempty UUID correlating the in-memory result revision; never stored.")]
    Guid Association,
    [property: Description("Explicit canonical source language from capabilities.")]
    string Language,
    [property: Description("One effective rewriting mode; correctionOnly when omitted; explicit null is rejected.")]
    string Mode = "correctionOnly");

/// <summary>Original synchronous delivery only; status/replay never contains options.</summary>
public sealed record AlternativesSuccessResponse(
    [property: Description("Client-generated UUIDv7 identity unique within the account across all kinds.")]
    Guid OperationId,
    [property: Description("Existing routing family: rewriting.")]
    OperationFamily Family,
    [property: Description("Operation kind: sentenceAlternatives.")]
    OperationKind Kind,
    [property: Description("Successful settled outcome.")]
    OperationStatus Status,
    [property: Description("Exactly two distinct complete sentence alternatives in the resolved language.")]
    IReadOnlyList<string> Options,
    [property: Description("Opaque nonempty UUID correlating the in-memory result revision; never stored.")]
    Guid Association,
    [property: Description("Zero-based Unicode scalar offset of the selected complete sentence.")]
    int Start,
    [property: Description("Selected complete sentence length, from 1 to 2000 Unicode scalars.")]
    int Length,
    [property: Description("Validated source language from eligibility, never inferred from generated output.")]
    LanguageId ResolvedSourceLanguage,
    [property: Description("One effective rewriting mode; correctionOnly when omitted; explicit null is rejected.")]
    string Mode,
    [property: Description("Selected-sentence scalar count charged once on success.")]
    int CharacterCount,
    [property: Description("UTC admission day owning the charge, yyyy-MM-dd.")]
    string AdmissionDay,
    [property: Description("Original stored absolute server deadline, never reset by recovery.")]
    DateTimeOffset DeadlineUtc,
    [property: Description("Current server time in UTC.")]
    DateTimeOffset ServerTimeUtc,
    [property: Description("Authoritative current-day usage with durable ordering revision.")]
    UsageSnapshot Usage);

[JsonConverter(typeof(JsonStringEnumConverter<OperationStatus>))]
public enum OperationStatus
{
    [JsonStringEnumMemberName("pending")]
    Pending,
    [JsonStringEnumMemberName("succeeded")]
    Succeeded,
    [JsonStringEnumMemberName("failed")]
    Failed,
    [JsonStringEnumMemberName("interrupted")]
    Interrupted,
}

/// <summary>Client operation submission admitted as exactly one reserved logical operation.</summary>
public sealed record SubmitOperationRequest(
    [property: Description("Client-generated UUIDv7 operation identity, unique within the account across both families.")]
    Guid OperationId,
    [property: Description("Language operation family: `translation` or `rewriting`.")]
    OperationFamily Family,
    [property: Description("Exact decoded full source text; line endings are significant and preserved.")]
    string Source,
    [property: Description("Effective source selection; `auto` when omitted.")]
    string? SourceSelection = null,
    [property: Description("Required translation target language; absent for rewriting.")]
    string? Target = null,
    [property: Description("Rewriting mode; the catalog default when omitted; absent for translation.")]
    string? Mode = null);

/// <summary>Pending reservation envelope with operation attribution and a fresh current-day usage snapshot.</summary>
public sealed record OperationPendingResponse(
    [property: Description("Operation identity echoed from the submission.")]
    Guid OperationId,
    [property: Description("Language operation family.")]
    OperationFamily Family,
    [property: Description("Observed reservation state: `pending`.")]
    OperationStatus Status,
    [property: Description("Reserved Unicode scalar count charged on a later successful settlement.")]
    int CharacterCount,
    [property: Description("UTC day owning the reservation, in yyyy-MM-dd form.")]
    string AdmissionDay,
    [property: Description("Server-controlled overall deadline for the operation.")]
    DateTimeOffset DeadlineUtc,
    [property: Description("Current server time in UTC.")]
    DateTimeOffset ServerTimeUtc,
    [property: Description("Fresh current-day user usage snapshot.")]
    UsageSnapshot Usage)
{
    [Description("Operation kind; fullText for existing operations, sentenceAlternatives for selected-sentence requests.")]
    public OperationKind Kind { get; init; } = OperationKind.FullText;
}

/// <summary>Terminal operation metadata: pending reservation, settled success charge with output unavailable, settled failure with zero charge, or recovered interruption with zero charge, plus a fresh current-day usage snapshot. Reads never dispatch work.</summary>
public sealed record OperationStatusResponse(
    [property: Description("Operation identity echoed from the submission.")]
    Guid OperationId,
    [property: Description("Language operation family.")]
    OperationFamily Family,
    [property: Description("Observed operation state: `pending`, `succeeded`, `failed` or `interrupted`.")]
    OperationStatus Status,
    [property: Description("Reserved count while pending, committed charge when succeeded, zero when failed or interrupted.")]
    int CharacterCount,
    [property: Description("UTC day owning the reservation or charge, in yyyy-MM-dd form.")]
    string AdmissionDay,
    [property: Description("Server-controlled overall deadline for the operation.")]
    DateTimeOffset DeadlineUtc,
    [property: Description("Always false in this slice: status reads never carry result text.")]
    bool OutputAvailable,
    [property: Description("Current server time in UTC.")]
    DateTimeOffset ServerTimeUtc,
    [property: Description("Fresh current-day user usage snapshot.")]
    UsageSnapshot Usage)
{
    [Description("Operation kind; fullText for existing operations, sentenceAlternatives for selected-sentence requests.")]
    public OperationKind Kind { get; init; } = OperationKind.FullText;
}

/// <summary>Synchronous translation success: complete validated translated text with its committed charge and a fresh current-day usage snapshot. Text-bearing responses always use `Cache-Control: no-store`.</summary>
public sealed record TranslationSuccessResponse(
    [property: Description("Operation identity echoed from the submission.")]
    Guid OperationId,
    [property: Description("Language operation family.")]
    OperationFamily Family,
    [property: Description("Observed operation state: `pending`, `succeeded`, `failed` or `interrupted`.")]
    OperationStatus Status,
    [property: Description("Complete validated translated text. Never logged, traced or persisted.")]
    string TranslatedText,
    [property: Description("Committed Unicode scalar charge for the full submitted source.")]
    int CharacterCount,
    [property: Description("UTC day owning the charge (the stored admission day), in yyyy-MM-dd form.")]
    string AdmissionDay,
    [property: Description("Server-controlled overall deadline for the operation.")]
    DateTimeOffset DeadlineUtc,
    [property: Description("Current server time in UTC.")]
    DateTimeOffset ServerTimeUtc,
    [property: Description("Fresh current-day user usage snapshot.")]
    UsageSnapshot Usage)
{
    [Description("Operation kind; fullText for existing operations, sentenceAlternatives for selected-sentence requests.")]
    public OperationKind Kind { get; init; } = OperationKind.FullText;
    [Description("Validated canonical source language from capabilities; never inferred from output.")]
    public required LanguageId ResolvedSourceLanguage { get; init; }
}

/// <summary>Synchronous rewriting success: complete validated same-language rewritten text with its committed charge and a fresh current-day usage snapshot. Text-bearing responses always use `Cache-Control: no-store`.</summary>
public sealed record RewritingSuccessResponse(
    [property: Description("Operation identity echoed from the submission.")]
    Guid OperationId,
    [property: Description("Language operation family.")]
    OperationFamily Family,
    [property: Description("Observed operation state: `pending`, `succeeded`, `failed` or `interrupted`.")]
    OperationStatus Status,
    [property: Description("Complete validated rewritten text in the resolved source language. Never logged, traced or persisted.")]
    string RewrittenText,
    [property: Description("Committed Unicode scalar charge for the full submitted source.")]
    int CharacterCount,
    [property: Description("UTC day owning the charge (the stored admission day), in yyyy-MM-dd form.")]
    string AdmissionDay,
    [property: Description("Server-controlled overall deadline for the operation.")]
    DateTimeOffset DeadlineUtc,
    [property: Description("Current server time in UTC.")]
    DateTimeOffset ServerTimeUtc,
    [property: Description("Fresh current-day user usage snapshot.")]
    UsageSnapshot Usage)
{
    [Description("Operation kind; fullText for existing operations, sentenceAlternatives for selected-sentence requests.")]
    public OperationKind Kind { get; init; } = OperationKind.FullText;
    [Description("Validated canonical source language from capabilities; never inferred from output.")]
    public required LanguageId ResolvedSourceLanguage { get; init; }
}

/// <summary>Wire-fixed Problem Details category names for the translation execution path (API §8).</summary>
public static class TranslationProblemCategories
{
    /// <summary>Classified provider/refusal/invalid-output exhaustion: 503 with zero character charge.</summary>
    public const string ProcessingFailure = "processingFailure";

    /// <summary>Server-established terminal deadline failure: 504 with zero character charge.</summary>
    public const string DeadlineExceeded = "deadlineExceeded";
}

/// <summary>Wire-fixed Problem Details category names for the rewriting execution path (API §8).</summary>
public static class RewritingProblemCategories
{
    /// <summary>Classified provider/refusal/invalid-output exhaustion: 503 with zero character charge.</summary>
    public const string ProcessingFailure = "processingFailure";

    /// <summary>Server-established terminal deadline failure: 504 with zero character charge.</summary>
    public const string DeadlineExceeded = "deadlineExceeded";
}

/// <summary>Categorical current-day availability computed from the same consistent read as the snapshot.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<UsageAvailability>))]
public enum UsageAvailability
{
    [JsonStringEnumMemberName("available")]
    Available,
    [JsonStringEnumMemberName("user-exhausted")]
    UserExhausted,
    [JsonStringEnumMemberName("service-exhausted")]
    ServiceExhausted,
    [JsonStringEnumMemberName("monetary-suspended")]
    MonetarySuspended,
    [JsonStringEnumMemberName("unavailable")]
    Unavailable,
}

/// <summary>Authoritative current-day user availability with a durable ordering revision.</summary>
public sealed record UsageSnapshot(
    [property: Description("UTC day described by this snapshot, in yyyy-MM-dd form.")]
    string Day,
    [property: Description("Next UTC midnight resetting both daily allowances.")]
    DateTimeOffset ResetAtUtc,
    [property: Description("Characters consumed by settled successes on this day.")]
    int ConsumedCharacters,
    [property: Description("Characters currently reserved by pending operations on this day.")]
    int ReservedCharacters,
    [property: Description("Configured user daily character allowance.")]
    int AllowanceCharacters,
    [property: Description("Nonnegative remaining reservable characters.")]
    int AvailableCharacters,
    [property: Description("Durable monotonically increasing snapshot revision.")]
    long Revision,
    [property: Description("Categorical availability: `available`, `user-exhausted`, `service-exhausted`, `monetary-suspended` or `unavailable`.")]
    UsageAvailability Availability);

public sealed class OperationProblemDetails
{
    [Description("RFC 9457 problem type reference.")]
    public string? Type { get; init; }
    [Description("Short, stable problem summary.")]
    public required string Title { get; init; }
    [Description("HTTP status code for this occurrence.")]
    public required int Status { get; init; }
    [Description("Safe explanation that never echoes submitted text.")]
    public required string Detail { get; init; }
    [Description("LinguaDesk error category.")]
    public required string Category { get; init; }
    [Description("Opaque request correlation identifier.")]
    public required string CorrelationId { get; init; }
    [Description("Current server time in UTC for identity and allowance recovery.")]
    public DateTimeOffset? ServerTimeUtc { get; init; }
    [Description("Next UTC midnight resetting both daily allowances; present for allowance denials.")]
    public DateTimeOffset? ResetAtUtc { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Description("Machine-readable eligibility reason; present for input eligibility failures.")]
    public string? Reason { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Description("Submitted scalar count; present when safely countable.")]
    public int? CharacterCount { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Description("Applicable character limit; present for oversize and allowance failures.")]
    public int? Limit { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Description("Field messages for structural request failures.")]
    public Dictionary<string, string[]>? Errors { get; init; }
}
