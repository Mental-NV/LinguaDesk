using LinguaDesk.Infrastructure.Ai;
using Microsoft.Extensions.AI;

namespace LinguaDesk.Infrastructure.Ai.Tests;

[TestClass]
public sealed class CompleteResponseBoundaryTests
{
    private const string ScriptedResponse = "{\"status\":\"eligible\",\"language\":\"en\"}";

    [TestMethod]
    public async Task CompleteResponseCallsClientOnceAndReturnsRawResponse()
    {
        var snapshot = EligibilityPrompt.Create("A fixed test sentence.");
        var expected = new ChatResponse(new ChatMessage(ChatRole.Assistant, ScriptedResponse));
        using var client = new CapturingChatClient(expected);

        var actual = await CompleteResponseBoundary.GetResponseAsync(snapshot, client);

        Assert.AreSame(expected, actual);
        Assert.AreEqual(1, client.CallCount);
        Assert.IsNotNull(client.LastMessages);
        Assert.HasCount(2, client.LastMessages);
        Assert.AreEqual(ChatRole.System, client.LastMessages[0].Role);
        Assert.AreEqual(snapshot.Messages[0].Content, client.LastMessages[0].Text);
        Assert.AreEqual(ChatRole.User, client.LastMessages[1].Role);
        Assert.AreEqual(snapshot.Messages[1].Content, client.LastMessages[1].Text);
        Assert.IsNotNull(client.LastOptions);
        Assert.AreSame(ChatResponseFormat.Json, client.LastOptions.ResponseFormat);
        Assert.IsNull(client.LastOptions.Tools);
    }

    [TestMethod]
    public async Task EveryBoundaryInvocationUsesFreshMessagesAndOptions()
    {
        var snapshot = EligibilityPrompt.Create("A fixed test sentence.");
        using var client = new CapturingChatClient(
            new ChatResponse(new ChatMessage(ChatRole.Assistant, ScriptedResponse)));

        await CompleteResponseBoundary.GetResponseAsync(snapshot, client);
        var firstEnumerable = client.MessageEnumerables[0];
        var firstMessages = client.MessageCalls[0];
        var firstOptions = client.OptionsCalls[0];

        await CompleteResponseBoundary.GetResponseAsync(snapshot, client);

        Assert.AreEqual(2, client.CallCount);
        Assert.AreNotSame(firstEnumerable, client.MessageEnumerables[1]);
        Assert.AreNotSame(firstMessages[0], client.MessageCalls[1][0]);
        Assert.AreNotSame(firstMessages[1], client.MessageCalls[1][1]);
        Assert.AreNotSame(firstOptions, client.OptionsCalls[1]);
    }

    [TestMethod]
    public async Task SuppliedCancellationTokenReachesClientUnchanged()
    {
        using var cancellation = new CancellationTokenSource();
        var snapshot = EligibilityPrompt.Create("A fixed test sentence.");
        using var client = new CapturingChatClient(
            new ChatResponse(new ChatMessage(ChatRole.Assistant, ScriptedResponse)));

        await CompleteResponseBoundary.GetResponseAsync(snapshot, client, cancellation.Token);

        Assert.AreEqual(cancellation.Token, client.LastCancellationToken);
    }

    [TestMethod]
    public async Task ScriptedClientExceptionPropagatesWithoutRetry()
    {
        var snapshot = EligibilityPrompt.Create("A fixed test sentence.");
        var expected = new SyntheticClientException();
        using var client = new CapturingChatClient(expected);

        var actual = await Assert.ThrowsExactlyAsync<SyntheticClientException>(
            () => CompleteResponseBoundary.GetResponseAsync(snapshot, client));

        Assert.AreSame(expected, actual);
        Assert.AreEqual(1, client.CallCount);
    }

    private sealed class SyntheticClientException : Exception;

    private sealed class CapturingChatClient : IChatClient
    {
        private readonly ChatResponse? response;
        private readonly Exception? exception;

        public CapturingChatClient(ChatResponse response)
        {
            this.response = response;
        }

        public CapturingChatClient(Exception exception)
        {
            this.exception = exception;
        }

        public int CallCount { get; private set; }

        public ChatMessage[]? LastMessages { get; private set; }

        public ChatOptions? LastOptions { get; private set; }

        public CancellationToken LastCancellationToken { get; private set; }

        public List<IEnumerable<ChatMessage>> MessageEnumerables { get; } = [];

        public List<ChatMessage[]> MessageCalls { get; } = [];

        public List<ChatOptions?> OptionsCalls { get; } = [];

        public void Dispose()
        {
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            MessageEnumerables.Add(messages);
            LastMessages = messages.ToArray();
            MessageCalls.Add(LastMessages);
            LastOptions = options;
            OptionsCalls.Add(options);
            LastCancellationToken = cancellationToken;

            if (exception is not null)
            {
                return Task.FromException<ChatResponse>(exception);
            }

            return Task.FromResult(response!);
        }

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Streaming is outside M004.");
    }
}
