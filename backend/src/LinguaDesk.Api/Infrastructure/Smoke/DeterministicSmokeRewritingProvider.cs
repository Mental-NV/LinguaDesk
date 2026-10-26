using System.Text.Json;
using LinguaDesk.Api.Features.Operations;
using LinguaDesk.Infrastructure.Ai;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace LinguaDesk.Api.Infrastructure.Smoke;

/// <summary>
/// Smoke-environment-only deterministic rewriting provider for the published
/// browser journey (M029; #6 section 4.5). It proves transport and presentation
/// through the real cookie-auth, API, accounting and recovery boundaries with
/// repeatable text. Language quality is never inferred from its output.
/// Register only through <see cref="SmokeRewritingProviderRegistration"/>: any
/// other environment keeps the unavailable provider, so rewriting submissions
/// remain pending reservations and this slice is not serving.
/// </summary>
public sealed class DeterministicSmokeRewritingProvider : IRewritingClientProvider
{
    public bool TryGetClients(out Func<CandidateProfile, IChatClient> clients, out FamilyChain chain)
    {
        clients = static _ => DeterministicSmokeRewritingClient.Instance;
        chain = FamilyChain.EvaluationDefault(ChainFamily.Rewriting);
        return true;
    }
}

/// <summary>
/// Registers the deterministic Smoke rewriting provider. Fail-closed: outside
/// the Smoke environment the existing provider registration is left untouched.
/// </summary>
public static class SmokeRewritingProviderRegistration
{
    public static IServiceCollection AddSmokeDeterministicRewritingProvider(
        this IServiceCollection services,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(environment);
        if (environment.IsEnvironment("Smoke"))
        {
            services.RemoveAll<IRewritingClientProvider>();
            services.AddScoped<IRewritingClientProvider, DeterministicSmokeRewritingProvider>();
        }

        return services;
    }
}

/// <summary>
/// Single deterministic chat client shared by both Smoke chain candidates.
/// It distinguishes the eligibility call from the transformation call by the
/// shape of the caller-supplied prompt JSON (eligibility carries
/// <c>sourceHint</c>; transformation carries <c>sourceLanguage</c> and
/// <c>mode</c>), so staged behavior stays deterministic regardless of
/// candidate retries. Two source sentinels select controlled non-happy paths
/// for published failure/eligibility evidence; every other source is eligible
/// English with the fixed W-OK fixture result.
/// </summary>
public sealed class DeterministicSmokeRewritingClient : IChatClient
{
    public const string EligibilityRejectSentinel = "M029-ELIGIBILITY-REJECT";

    public const string ProcessingFailureSentinel = "M029-PROCESSING-FAILURE";

    public const string FixtureSource = "The report is really ready. We sends it today.";

    public const string FixtureResult = "The report is ready. We send it today.";

    public static readonly DeterministicSmokeRewritingClient Instance = new();

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private DeterministicSmokeRewritingClient()
    {
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var userText = messages.LastOrDefault()?.Text ?? string.Empty;
        if (userText.Contains("M039-DELAY", StringComparison.Ordinal))
        {
            // A bounded external-adapter fixture lets published browser tests
            // interrupt real admitted work without intercepting application APIs.
            await Task.Delay(TimeSpan.FromMilliseconds(750), cancellationToken);
        }

        return new ChatResponse(new ChatMessage(ChatRole.Assistant, Respond(userText)));
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
        if (root.TryGetProperty("selectedText", out var selected))
        {
            var text = selected.GetString();
            var context = root.GetProperty("context").GetString()!;
            if (ExpandedSmokeFixtures.Match(context) is { } fixture)
                return JsonSerializer.Serialize(new { status = "result", options = fixture.Options });
            var alternatives = text == "The report is ready."
                ? new[] { "The report is prepared.", "The report is complete." }
                : new[] { "We will send it today.", "We are sending it today." };
            return JsonSerializer.Serialize(new { status = "result", options = alternatives });
        }
        var isTransformation = root.TryGetProperty("mode", out _);
        if (!isTransformation)
        {
            return ExpandedSmokeFixtures.Classify(source, root) ?? EligibilityResponse(source);
        }

        if (source.Contains(ProcessingFailureSentinel, StringComparison.Ordinal))
        {
            throw new HttpRequestException("Smoke deterministic processing failure.");
        }

        if (ExpandedSmokeFixtures.Match(source) is { } expanded)
            return JsonSerializer.Serialize(new TransformationResult("result", expanded.Result), SerializerOptions);

        return JsonSerializer.Serialize(
            new TransformationResult("result", source.Contains("M045-DELAY-ALTERNATIVES", StringComparison.Ordinal) ? "M039-DELAY. The report is ready." : source.Contains("M045-EXPANDED-RESULT", StringComparison.Ordinal) ? "The report is ready. " + new string('a', 8000) : source.Contains("M045-DELETION", StringComparison.Ordinal) ? "The report is ready." : FixtureResult),
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

    private sealed record TransformationResult(string Status, string? Text);
}
