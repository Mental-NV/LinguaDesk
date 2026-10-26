using System.Collections.Concurrent;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using LinguaDesk.Core;
using LinguaDesk.Infrastructure.Ai;
using Microsoft.Extensions.AI;

namespace LinguaDesk.Ai.Evaluation;

/// <summary>Allowlisted development inputs; never accepts caller text, provider URLs or prompt overrides.</summary>
internal static class M046Suite
{
    internal const string FrozenHash = "550777c025e5577b45fdc742dc04edeb4b35b063f44bfb12ee785a1214574142";
    internal const string ReducedFrozenHash = "3af8e839293f913358818da7cec9e14868f9a3f5126aef49dc4f0c702d435879";
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true, Converters = { new JsonStringEnumConverter() } };
    private static readonly JsonSerializerOptions CompactJson = new(Json) { WriteIndented = false };
    internal sealed record Case(string Id, string Operation, string? Language, string? Source, string? SourceSelection, string? Target, string? Mode, string? Context, int Start, int Length, string? Status, string[] Tags, string[] MeaningConstraints);
    internal sealed record Manifest(string Revision, string CatalogRevision, string CatalogSha256, string SegmentationRevision, Dictionary<string, int> Counts, string[] RepresentativeDirections, Case[] Cases);

    internal static Manifest Load(bool reduced = false)
    {
        var bytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Corpus", "m046", reduced ? "reduced-development.v1.json" : "development.v1.json"));
        if (Convert.ToHexStringLower(SHA256.HashData(bytes)) != (reduced ? ReducedFrozenHash : FrozenHash)) throw new InvalidOperationException("M046 corpus differs from its frozen reference.");
        var manifest = JsonSerializer.Deserialize<Manifest>(bytes, Json)!;
        var catalogBytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Corpus", "m046", "languages.v1.json"));
        using var catalogDocument = JsonDocument.Parse(catalogBytes);
        var catalog = JsonSerializer.Deserialize<LanguageDefinition[]>(catalogDocument.RootElement.GetProperty("languages").GetRawText(), Json)!;
        if (Convert.ToHexStringLower(SHA256.HashData(catalogBytes)) != manifest.CatalogSha256 || !catalog.SequenceEqual(ProductCatalog.Languages))
            throw new InvalidOperationException("The frozen language metadata differs from Core.");
        var codes = ProductCatalog.Languages.Select(l => l.Id).ToHashSet(StringComparer.Ordinal);
        if (manifest.CatalogRevision != ProductCatalog.LanguageRevision || manifest.SegmentationRevision != SentenceBoundaryPolicy.Revision
            || manifest.Cases.Select(c => c.Id).Distinct(StringComparer.Ordinal).Count() != manifest.Cases.Length)
            throw new InvalidOperationException("M046 catalog, segmentation or case identity mismatch.");
        if (reduced) ValidateReduced(manifest);
        else
        {
            foreach (var (operation, count) in new Dictionary<string, int> { ["translation"] = 264, ["rewriting"] = 1116, ["alternatives"] = 1116, ["eligibility"] = 12 })
                if (manifest.Counts.GetValueOrDefault(operation) != count || manifest.Cases.Count(c => c.Operation == operation) != count)
                    throw new InvalidOperationException("Missing or duplicate M046 operation coverage: " + operation);
            foreach (var operation in new[] { "rewriting", "alternatives" })
            {
                var cells = manifest.Cases.Where(c => c.Operation == operation).Select(c => c.Language + ":" + c.Mode).ToArray();
                if (cells.Distinct(StringComparer.Ordinal).Count() != 1116 || codes.Any(code => ProductCatalog.RewritingModes.Any(mode => !cells.Contains(code + ":" + mode.Id, StringComparer.Ordinal))))
                    throw new InvalidOperationException("Missing or duplicate M046 language/mode cell.");
            }
            var original = new[] { "en", "ru", "ro", "zh" };
            var representatives = original.SelectMany(a => original.Where(b => b != a).Select(b => a + ":" + b)).ToHashSet(StringComparer.Ordinal);
            foreach (var (a, b) in new[] { ("de", "fr"), ("ru", "ar"), ("hi", "bn"), ("ja", "ko"), ("th", "vi"), ("am", "he") }) { representatives.Add(a + ":" + b); representatives.Add(b + ":" + a); }
            if (!representatives.SetEquals(manifest.RepresentativeDirections)) throw new InvalidOperationException("Wrong representative directions.");
            var directions = manifest.Cases.Where(c => c.Operation == "translation").Select(c => c.Language + ":" + c.Target).ToHashSet(StringComparer.Ordinal);
            foreach (var code in codes.Except(original)) { representatives.Add(code + ":en"); representatives.Add("en:" + code); }
            if (directions.Count != 264 || !directions.SetEquals(representatives)) throw new InvalidOperationException("Missing Translation template/anchor coverage.");
        }
        foreach (var row in manifest.Cases)
        {
            if (row.MeaningConstraints.Length == 0 || row.Tags.Length == 0) throw new InvalidOperationException("Missing reviewed expectations.");
            if (row.Operation == "eligibility") continue;
            if (row.Language is null || !codes.Contains(row.Language)) throw new InvalidOperationException("Unknown language.");
            if (row.Operation == "alternatives")
            {
                if (!new AlternativesInput(row.Context!, row.Start, row.Length, Guid.NewGuid(), row.Language, row.Mode).Validate().IsValid)
                    throw new InvalidOperationException("Invalid frozen alternatives span: " + row.Id);
            }
            else if (!ScalarInputPolicy.Analyze(row.Source!, row.Operation == "translation" ? 5000 : 2000).IsValid)
                throw new InvalidOperationException("Invalid frozen source: " + row.Id);
        }
        return manifest;
    }

    private static void ValidateReduced(Manifest manifest)
    {
        var expectedCounts = new Dictionary<string, int> { ["translation"] = 16, ["rewriting"] = 40, ["alternatives"] = 36, ["eligibility"] = 8 };
        if (manifest.Cases.Length != 100) throw new InvalidOperationException("The reduced live scope must contain exactly 100 cases.");
        foreach (var (operation, count) in expectedCounts)
            if (manifest.Counts.GetValueOrDefault(operation) != count || manifest.Cases.Count(c => c.Operation == operation) != count)
                throw new InvalidOperationException("Incorrect reduced operation coverage: " + operation);
        var excluded = new[] { "Latin", "Cyrillic", "Chinese", "Japanese" };
        var selected = ProductCatalog.Languages.Where(l => !excluded.Contains(l.Script, StringComparer.Ordinal)).ToArray();
        var codes = selected.Select(l => l.Id).ToHashSet(StringComparer.Ordinal);
        var measured = manifest.Cases.Where(c => c.Operation != "eligibility").Select(c => c.Language!).ToHashSet(StringComparer.Ordinal);
        if (codes.Count != 33 || selected.Select(l => l.Script).Distinct(StringComparer.Ordinal).Count() != 21 || !codes.SetEquals(measured))
            throw new InvalidOperationException("Reduced scope must cover all 33 selected languages and 21 writing systems.");
        foreach (var operation in new[] { "rewriting", "alternatives" })
            if (!ProductCatalog.RewritingModes.Select(m => m.Id).ToHashSet(StringComparer.Ordinal).SetEquals(manifest.Cases.Where(c => c.Operation == operation).Select(c => c.Mode!)))
                throw new InvalidOperationException("Reduced scope omits a writing mode.");
        foreach (var row in manifest.Cases)
        {
            if (row.Language is not null && !codes.Contains(row.Language) || row.Target is not null && !codes.Contains(row.Target)
                || row.SourceSelection is not null && !codes.Contains(row.SourceSelection))
                throw new InvalidOperationException("Excluded writing system in case metadata: " + row.Id);
            foreach (var scalar in ((row.Source ?? "") + (row.Context ?? "")).EnumerateRunes())
                if (scalar.Value is >= 0x41 and <= 0x5a or >= 0x61 and <= 0x7a or >= 0xc0 and <= 0x24f
                    or >= 0x400 and <= 0x52f or >= 0x1e00 and <= 0x1eff or >= 0x3400 and <= 0x9fff
                    or >= 0xa640 and <= 0xa69f or >= 0xf900 and <= 0xfaff or >= 0x20000 and <= 0x323af)
                    throw new InvalidOperationException("Excluded script glyph in case payload: " + row.Id);
        }
        var directions = manifest.Cases.Where(c => c.Operation == "translation").Select(c => c.Language + ":" + c.Target).ToHashSet(StringComparer.Ordinal);
        if (directions.Count != 16 || !directions.SetEquals(manifest.RepresentativeDirections))
            throw new InvalidOperationException("Reduced Translation directions do not match the frozen plan.");
    }

    internal static async Task<int> RunAsync(string operation, string[] args)
    {
        try { return await RunCheckedAsync(operation, args).ConfigureAwait(false); }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or IOException or JsonException)
        { Console.Error.WriteLine("M046 preflight/run failed: " + exception.Message); return 3; }
    }

    private static async Task<int> RunCheckedAsync(string operation, string[] args)
    {
        bool live = false, offline = false;
        string? suite = null, profileId = null, output = null;
        var cap = 0; var spend = 0m; var selectedIds = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == "--live") { live = true; continue; }
            if (args[i] == "--offline") { offline = true; continue; }
            if (i + 1 == args.Length) return 2;
            var name = args[i++]; var value = args[i];
            switch (name)
            {
                case "--suite": suite = value; break;
                case "--profile": profileId = value; break;
                case "--output": output = value; break;
                case "--case-id": selectedIds.Add(value); break;
                case "--max-dispatches": if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out cap)) return 2; break;
                case "--max-spend-usd": if (!decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out spend)) return 2; break;
                default: return 2;
            }
        }
        var reduced = suite == "m046-reduced";
        if (suite is not ("m046" or "m046-reduced") || live && offline || cap <= 0 || spend <= 0 || profileId is null || live && output is null || selectedIds.Count > 5 || selectedIds.Distinct(StringComparer.Ordinal).Count() != selectedIds.Count) return 2;
        if (live && !reduced) { Console.Error.WriteLine("The full M046 matrix is retained offline only; live scope is m046-reduced."); return 2; }
        var manifest = Load(reduced); // All cells and spans are checked before credential access or network activity.
        var rows = manifest.Cases.Where(c => c.Operation == operation).ToArray();
        if (selectedIds.Count > 0)
        {
            if (selectedIds.Any(id => !rows.Any(c => c.Id == id))) return 2;
            rows = rows.Where(c => selectedIds.Contains(c.Id, StringComparer.Ordinal)).ToArray();
        }
        var required = rows.Length * (operation == "eligibility" ? 1 : 2);
        if (cap < required) { Console.Error.WriteLine($"M046 requires {required} dispatches for this slice; zero dispatches."); return 3; }
        var profile = CandidateRegistry.Select(CandidateRegistry.Default, profileId);
        var variable = "LINGUADESK_AIEVALUATION__CREDENTIALS__" + profile.CredentialRef.ToUpperInvariant().Replace('-', '_') + "__APIKEY";
        var key = live ? Environment.GetEnvironmentVariable(variable) : null;
        if (live && string.IsNullOrWhiteSpace(key)) { Console.Error.WriteLine("M046 credential absent; zero dispatches."); return 3; }
        using var credential = key is null ? null : new TransportCredential(key);
        using var http = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
        var budget = new EvaluationBudget(cap, spend);
        var results = new ConcurrentDictionary<string, object>(StringComparer.Ordinal);
        var writeGate = new SemaphoreSlim(1, 1);
        if (output is not null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
            // Never overwrite a preserved attempt, even if the prior run was incomplete.
            if (File.Exists(output) || File.Exists(output + ".ndjson")) throw new IOException("The report or journal already exists; use a fresh output path.");
            using var reservedOutput = new FileStream(output, FileMode.CreateNew, FileAccess.Write);
            using var reservedJournal = new FileStream(output + ".ndjson", FileMode.CreateNew, FileAccess.Write);
        }
        var failures = 0;
        await Parallel.ForEachAsync(rows, new ParallelOptions { MaxDegreeOfParallelism = live && !reduced ? 4 : 1 }, async (row, token) =>
        {
            var observations = new List<AttemptObservation>();
            var trustedPromptHashes = new List<string>();
            IChatClient Client(CandidateProfile view) => live ? new LiveClient(new(view, http), credential!, observations, trustedPromptHashes, operation == "eligibility" ? budget : null) : new OfflineClient(row);
            object result;
            var matched = true;
            if (operation == "eligibility")
            {
                using var client = Client(profile);
                using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
                deadline.CancelAfter(TimeSpan.FromSeconds(5));
                EligibilityOutcome? outcome = null;
                try { outcome = await EligibilityPipeline.EvaluateAsync(new(row.Source!, EligibilityOperation.Rewriting, row.SourceSelection), client, deadline.Token).ConfigureAwait(false); }
                catch (OperationCanceledException) { matched = false; }
                matched &= outcome?.Category == (row.Status == "eligible" ? null : row.Status == "source_mismatch" ? "source-mismatch" : row.Status) && outcome?.ResolvedLanguage == (row.Status == "eligible" ? row.Language : null);
                if (live && observations.Count > 0)
                    matched &= EligibilityEnvelopeParser.TryParse(observations[^1].ResponseText, row.SourceSelection, out var classification, out _) && classification!.Language == row.Language;
                result = new { row.Id, outcome, matched, observations, trustedPromptHashes };
            }
            else
            {
                var chain = new FamilyChain(operation == "translation" ? ChainFamily.Translation : ChainFamily.Rewriting, profile, null);
                var outcome = operation switch
                {
                    "translation" => await ChainOrchestrator.ExecuteTranslationAsync(chain, new(row.Source!, row.SourceSelection, row.Target), Client, budget: live ? budget : null, cancellationToken: token).ConfigureAwait(false),
                    "rewriting" => await ChainOrchestrator.ExecuteRewritingAsync(chain, new(row.Source!, row.SourceSelection, row.Mode), Client, budget: live ? budget : null, cancellationToken: token).ConfigureAwait(false),
                    _ => await ChainOrchestrator.ExecuteAlternativesAsync(chain, new(row.Context!, row.Start, row.Length, Guid.NewGuid(), row.Language!, row.Mode), Client, budget: live ? budget : null, cancellationToken: token).ConfigureAwait(false),
                };
                matched = outcome.Decision == ChainDecision.Succeeded;
                result = new { row.Id, outcome, matched, observations, trustedPromptHashes, reviewDisposition = live ? "pending-semantic-review" : "synthetic-shape-only" };
            }
            if (!matched) Interlocked.Increment(ref failures);
            results[row.Id] = result;
            if (output is not null)
            {
                await writeGate.WaitAsync(token).ConfigureAwait(false);
                try { await File.AppendAllTextAsync(output + ".ndjson", JsonSerializer.Serialize(result, CompactJson) + "\n", token).ConfigureAwait(false); }
                finally { writeGate.Release(); }
            }
            if (live) Console.Error.WriteLine($"{row.Id}: {(matched ? "shape-ok" : "failed")}; {budget.DispatchesUsed}/{cap}; exposure USD {budget.ReservedUsd}; unresolved {budget.UnresolvedUsd}");
        }).ConfigureAwait(false);
        var report = new { kind = "m046-development", utc = DateTimeOffset.UtcNow, live, operation, suite, manifest.Revision, corpusSha256 = reduced ? ReducedFrozenHash : FrozenHash, manifest.CatalogRevision, manifest.SegmentationRevision,
            profile.CandidateId, profile.CredentialRef, profile.Model, profile.Settings, profile.Bounds, profile.Billing, fullTextViewRevision = FullTextProfile.Revision, fullTextMaximumInputTokens = FullTextProfile.MaximumInputTokens,
            eligibility = EligibilityPrompt.Create("").ResourceSha256, eligibilityRevision = EligibilityPrompt.PromptId, modelDataEncoding = "native-unicode-json-v1", translationRevision = TranslationPrompt.PromptId, rewritingRevision = RewritingPrompt.PromptId, alternativesRevision = AlternativesPrompt.PromptId, alternativesValidatorRevision = AlternativesEnvelopeParser.Revision,
            cap, spend, budget.DispatchesUsed, budget.ReservedUsd, budget.UnresolvedUsd, failures,
            status = failures > 0 ? "failed" : live ? "awaiting-semantic-review" : "offline-shape-pass", results = rows.Select(row => results[row.Id]).ToArray() };
        var json = JsonSerializer.Serialize(report, Json);
        if (output is null) Console.WriteLine(json); else await File.WriteAllTextAsync(output, json + "\n").ConfigureAwait(false);
        writeGate.Dispose();
        if (!live) Console.Error.WriteLine($"M046 {operation}: {rows.Length} synthetic shape cases, {failures} failures, zero provider dispatches.");
        return failures == 0 ? 0 : 1;
    }

    private sealed class OfflineClient(Case row) : IChatClient
    {
        public void Dispose() { }
        public object? GetService(Type serviceType, object? serviceKey = null) => null;
        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            using var data = JsonDocument.Parse(messages.Last().Text);
            var classification = data.RootElement.TryGetProperty("sourceHint", out _);
            var text = classification ? JsonSerializer.Serialize(new { status = row.Status ?? "eligible", language = row.Language })
                : row.Operation == "alternatives" ? """{"status":"result","options":["An explicit synthetic option.","A second synthetic option."]}"""
                : """{"status":"result","text":"An explicit synthetic result."}""";
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, text)) { FinishReason = ChatFinishReason.Stop });
        }
        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
    private sealed class LiveClient(ChatCompletionsAdapter adapter, TransportCredential credential, List<AttemptObservation> observations, List<string> trustedPromptHashes, EvaluationBudget? budget) : IChatClient, IChainAttemptReporter
    {
        public AttemptObservation? LastAttempt { get; private set; }
        public void Dispose() { }
        public object? GetService(Type serviceType, object? serviceKey = null) => null;
        public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            var snapshots = messages.Select(m => new PromptMessageSnapshot(m.Role.Value, m.Text)).ToArray();
            trustedPromptHashes.Add(Convert.ToHexStringLower(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(snapshots[0].Content))));
            var result = await adapter.SendAsync(snapshots, credential, budget, cancellationToken).ConfigureAwait(false);
            LastAttempt = result; observations.Add(result);
            return new(new ChatMessage(ChatRole.Assistant, result.ResponseText)) { FinishReason = result.FinishCategory == "completed" ? ChatFinishReason.Stop : new ChatFinishReason(result.FinishCategory) };
        }
        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
