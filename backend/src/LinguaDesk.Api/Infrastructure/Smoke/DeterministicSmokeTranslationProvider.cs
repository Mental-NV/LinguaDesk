using System.Text.Json;
using LinguaDesk.Api.Features.Operations;
using LinguaDesk.Infrastructure.Ai;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace LinguaDesk.Api.Infrastructure.Smoke;

/// <summary>
/// Smoke-environment-only deterministic translation provider for the published
/// browser journey (M028; #6 section 4.5). It proves transport and presentation
/// through the real cookie-auth, API, accounting and recovery boundaries with
/// repeatable text. Language quality is never inferred from its output.
/// Register only through <see cref="SmokeTranslationProviderRegistration"/>: any
/// other environment keeps the unavailable provider, so translation submissions
/// remain pending reservations and this slice is not serving.
/// </summary>
public sealed class DeterministicSmokeTranslationProvider : ITranslationClientProvider
{
    public bool TryGetClients(out Func<CandidateProfile, IChatClient> clients, out FamilyChain chain)
    {
        clients = static _ => DeterministicSmokeTranslationClient.Instance;
        chain = FamilyChain.EvaluationDefault(ChainFamily.Translation);
        return true;
    }
}

/// <summary>
/// Registers the deterministic Smoke translation provider. Fail-closed: outside
/// the Smoke environment the existing provider registration is left untouched.
/// </summary>
public static class SmokeTranslationProviderRegistration
{
    public static IServiceCollection AddSmokeDeterministicTranslationProvider(
        this IServiceCollection services,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(environment);
        if (environment.IsEnvironment("Smoke"))
        {
            services.RemoveAll<ITranslationClientProvider>();
            services.AddScoped<ITranslationClientProvider, DeterministicSmokeTranslationProvider>();
        }

        return services;
    }
}

/// <summary>
/// Single deterministic chat client shared by both Smoke chain candidates.
/// It distinguishes the eligibility call from the transformation call by the
/// shape of the caller-supplied prompt JSON (eligibility carries
/// <c>sourceHint</c>; transformation carries <c>sourceLanguage</c> and
/// <c>targetLanguage</c>), so staged behavior stays deterministic regardless of
/// candidate retries. Two source sentinels select controlled non-happy paths
/// for published failure/eligibility evidence; every other source is eligible
/// English with a fixed per-target fixture result.
/// </summary>
public sealed class DeterministicSmokeTranslationClient : IChatClient
{
    public const string EligibilityRejectSentinel = "M028-ELIGIBILITY-REJECT";

    public const string ProcessingFailureSentinel = "M028-PROCESSING-FAILURE";

    public static readonly DeterministicSmokeTranslationClient Instance = new();

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private DeterministicSmokeTranslationClient()
    {
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var userText = messages.LastOrDefault()?.Text ?? string.Empty;
        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, Respond(userText))));
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("The Smoke deterministic client never streams.");

    private static string Respond(string userText)
    {
        using var document = ParseOrThrow(userText);
        var root = document.RootElement;
        var source = root.TryGetProperty("source", out var sourceElement) && sourceElement.ValueKind == JsonValueKind.String
            ? sourceElement.GetString() ?? string.Empty
            : string.Empty;
        var isTransformation = root.TryGetProperty("targetLanguage", out _);
        if (!isTransformation)
        {
            return EligibilityResponse(source);
        }

        if (source.Contains(ProcessingFailureSentinel, StringComparison.Ordinal))
        {
            throw new HttpRequestException("Smoke deterministic processing failure.");
        }

        var target = root.TryGetProperty("targetLanguage", out var targetElement) && targetElement.ValueKind == JsonValueKind.String
            ? targetElement.GetString()
            : null;
        return JsonSerializer.Serialize(
            new TransformationResult("result", FixtureText(target)),
            SerializerOptions);
    }

    private static string EligibilityResponse(string source)
    {
        if (source.Contains(EligibilityRejectSentinel, StringComparison.Ordinal))
        {
            return """{"status":"unsupported","language":null}""";
        }

        return """{"status":"eligible","language":"en"}""";
    }

    private static JsonDocument ParseOrThrow(string userText)
    {
        try
        {
            return JsonDocument.Parse(userText);
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException("The Smoke deterministic client received unexpected prompt JSON.", exception);
        }
    }

    internal static string FixtureText(string? target) =>
        target switch
        {
            "ro" => "Bună, întâlnirea începe la 14:30. Te rog să mergi.",
            "ru" => "Привет, встреча начинается в 14:30. Пожалуйста, приходи.",
            "zh" => "你好，会议在14:30开始，请参加。",
            _ => "Hello, the meeting starts at 14:30. Please go.",
        };

    private sealed record TransformationResult(string Status, string? Text);
}
