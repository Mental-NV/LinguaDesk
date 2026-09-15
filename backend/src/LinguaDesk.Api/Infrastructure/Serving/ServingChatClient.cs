using LinguaDesk.Infrastructure.Ai;
using Microsoft.Extensions.AI;

namespace LinguaDesk.Api.Infrastructure.Serving;

/// <summary>
/// Bridges the serving <see cref="ChatCompletionsAdapter"/> to the
/// pipeline <see cref="IChatClient"/> contract. The per-operation
/// <see cref="EvaluationBudget"/> stays with the chain traversal (the
/// coordinators pass it to <c>ChainOrchestrator</c>), so the adapter runs
/// with a null budget here and never double-reserves. The last attempt
/// observation is reported back so the chain can settle its reservation
/// against authoritative usage.
/// </summary>
internal sealed class ServingChatClient(
    ChatCompletionsAdapter adapter,
    TransportCredential credential) : IChatClient, IChainAttemptReporter, IDisposable
{
    private bool disposed;

    public AttemptObservation? LastAttempt { get; private set; }

    public void Dispose()
    {
        if (!disposed)
        {
            disposed = true;
            credential.Dispose();
        }
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentNullException.ThrowIfNull(messages);

        var snapshots = messages
            .Select(static message => new PromptMessageSnapshot(message.Role.Value, message.Text))
            .ToArray();
        var observation = await adapter.SendAsync(snapshots, credential, budget: null, cancellationToken).ConfigureAwait(false);
        LastAttempt = observation;
        return new ChatResponse(new ChatMessage(ChatRole.Assistant, observation.ResponseText));
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("The serving execution path never streams.");
}
