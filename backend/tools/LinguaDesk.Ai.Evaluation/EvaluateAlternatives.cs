using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using LinguaDesk.Core;
using LinguaDesk.Infrastructure.Ai;
using Microsoft.Extensions.AI;

namespace LinguaDesk.Ai.Evaluation;

internal static class EvaluateAlternatives
{
    private const string FrozenHash = "77e7e8d1b8486ad8735e333afb415469776e6e6c71f9c3df8c9ae193ca5f210d";
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true, Converters = { new JsonStringEnumConverter() } };
    internal static async Task<int> RunAsync(string[] args)
    {
        if (args.Contains("--suite", StringComparer.Ordinal)) return await M046Suite.RunAsync("alternatives", args).ConfigureAwait(false);
        var live = false;
        var offline = false;
        string? profileId = null, output = null;
        var dispatchCap = 0;
        var spendCap = 0m;
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == "--live") { live = true; continue; }
            if (args[i] == "--offline") { offline = true; continue; }
            if (i + 1 == args.Length) return 2;
            var name = args[i++]; var value = args[i];
            switch (name)
            {
                case "--profile": profileId = value; break;
                case "--output": output = value; break;
                case "--max-dispatches": if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out dispatchCap)) return 2; break;
                case "--max-spend-usd": if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out spendCap)) return 2; break;
                default: return 2;
            }
        }
        if (live && offline || profileId is null || dispatchCap <= 0 || spendCap <= 0) return 2;
        var profile = CandidateRegistry.Select(CandidateRegistry.Default, profileId);
        var corpusPath = Path.Combine(AppContext.BaseDirectory, "Corpus", "alternatives", "development.v1.json");
        var bytes = await File.ReadAllBytesAsync(corpusPath).ConfigureAwait(false);
        if (!string.Equals(Convert.ToHexString(SHA256.HashData(bytes)), FrozenHash, StringComparison.OrdinalIgnoreCase))
        { Console.Error.WriteLine("Alternatives development corpus differs from its reviewed frozen manifest."); return 3; }
        var corpus = JsonSerializer.Deserialize<Corpus>(bytes, Json)!;
        var variable = "LINGUADESK_AIEVALUATION__CREDENTIALS__" + profile.CredentialRef.ToUpperInvariant().Replace('-', '_') + "__APIKEY";
        var key = live ? Environment.GetEnvironmentVariable(variable) : null;
        if (live && string.IsNullOrWhiteSpace(key)) { Console.Error.WriteLine("Configured evaluation credential is missing; zero dispatches."); return 3; }
        using var credential = key is null ? null : new TransportCredential(key);
        using var http = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
        var budget = new EvaluationBudget(dispatchCap, spendCap);
        var rows = new List<object>();
        var failed = 0;
        var observations = new List<AttemptObservation>();
        foreach (var row in corpus.Cases)
        {
            var input = new AlternativesInput(row.Context, row.Start, row.Length, Guid.NewGuid(), row.Language, row.Mode);
            var outcome = await ChainOrchestrator.ExecuteAlternativesAsync(new(ChainFamily.Rewriting, profile, null), input,
                view => live ? new LiveClient(new ChatCompletionsAdapter(view, http), credential!, observations) : new OfflineClient(row.Language),
                budget: live ? budget : null).ConfigureAwait(false);
            if (outcome.Decision != ChainDecision.Succeeded) failed++;
            rows.Add(new { row.Id, row.Language, row.Mode, row.Script, row.Tags, row.MeaningConstraints,
                decision = outcome.Decision.ToString(), outcome.Category, outcome.Options, outcome.Dispatches, outcome.Attempts,
                candidateEnvelopes = observations.Select(o => o.ResponseText).ToArray(),
                usage = observations.Select(o => o.Usage).ToArray(), models = observations.Select(o => o.ReturnedModel).ToArray(),
                reviewDisposition = live ? "pending-both-option-semantic-review" : "scripted-offline-shape-only" });
            observations.Clear();
            Console.Error.WriteLine($"{row.Id}: {outcome.Decision}; {budget.DispatchesUsed}/{dispatchCap} dispatches; unresolved ${budget.UnresolvedUsd}");
        }
        var report = new { kind = "alternatives_development_report", revision = "alternatives-evaluator-v1", utc = DateTimeOffset.UtcNow,
            live, credentialPresent = credential is not null, profile.CandidateId, profile.CredentialRef, profile.Model,
            profile.Settings, profile.Billing, corpusSha256 = FrozenHash, segmentationRevision = SentenceBoundaryPolicy.Revision,
            prompt = AlternativesPrompt.Create(new(corpus.Cases[0].Context, corpus.Cases[0].Start, corpus.Cases[0].Length, Guid.NewGuid(), corpus.Cases[0].Language, corpus.Cases[0].Mode)).ResourceSha256,
            maxDispatches = dispatchCap, maxSpendUsd = spendCap, budget.DispatchesUsed, budget.ReservedUsd, budget.UnresolvedUsd,
            failed, status = failed == 0 ? live ? "awaiting-semantic-review" : "offline-pass" : "fail", rows };
        var json = JsonSerializer.Serialize(report, Json);
        if (output is not null) { Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!); await File.WriteAllTextAsync(output, json + "\n").ConfigureAwait(false); }
        else Console.WriteLine(json);
        return failed == 0 ? 0 : 1;
    }

    private sealed record Corpus(IReadOnlyList<Case> Cases);
    private sealed record Case(string Id, string Context, int Start, int Length, string Language, string Mode, string? Script, IReadOnlyList<string> Tags, IReadOnlyList<string> MeaningConstraints);
    private sealed class OfflineClient(string language) : IChatClient
    {
        public void Dispose() { }
        public object? GetService(Type serviceType, object? serviceKey = null) => null;
        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            var eligibility = messages.Last().Text.Contains("sourceHint", StringComparison.Ordinal);
            var text = eligibility ? JsonSerializer.Serialize(new { status = "eligible", language })
                : "{\"status\":\"result\",\"options\":[\"A different sentence.\",\"Another sentence.\"]}";
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, text)) { FinishReason = ChatFinishReason.Stop });
        }
        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
    private sealed class LiveClient(ChatCompletionsAdapter adapter, TransportCredential credential, List<AttemptObservation> observations) : IChatClient, IChainAttemptReporter
    {
        public AttemptObservation? LastAttempt { get; private set; }
        public void Dispose() { }
        public object? GetService(Type serviceType, object? serviceKey = null) => null;
        public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            var result = await adapter.SendAsync(messages.Select(m => new PromptMessageSnapshot(m.Role.Value, m.Text)).ToArray(), credential, null, cancellationToken).ConfigureAwait(false);
            LastAttempt = result;
            observations.Add(result);
            return new(new ChatMessage(ChatRole.Assistant, result.ResponseText)) { FinishReason = ChatFinishReason.Stop };
        }
        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
