using Microsoft.Extensions.AI;

namespace LinguaDesk.Infrastructure.Ai;

public static class CompleteResponseBoundary
{
    public static Task<ChatResponse> GetResponseAsync(
        PromptSnapshot snapshot,
        IChatClient client,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(client);

        var messages = snapshot.CreateChatMessages();
        var options = new ChatOptions
        {
            ResponseFormat = ChatResponseFormat.Json,
        };

        return client.GetResponseAsync(messages, options, cancellationToken);
    }
}
