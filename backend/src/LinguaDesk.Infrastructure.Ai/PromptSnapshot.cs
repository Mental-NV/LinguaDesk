using Microsoft.Extensions.AI;

namespace LinguaDesk.Infrastructure.Ai;

public sealed class PromptSnapshot
{
    private readonly PromptMessageSnapshot[] messages;

    internal PromptSnapshot(
        string promptId,
        string resourceSha256,
        IEnumerable<PromptMessageSnapshot> messages)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(promptId);
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceSha256);
        ArgumentNullException.ThrowIfNull(messages);

        PromptId = promptId;
        ResourceSha256 = resourceSha256;
        this.messages = [.. messages];
        Messages = Array.AsReadOnly(this.messages);
    }

    public string PromptId { get; }

    public string ResourceSha256 { get; }

    public IReadOnlyList<PromptMessageSnapshot> Messages { get; }

    internal IReadOnlyList<ChatMessage> CreateChatMessages()
    {
        var request = new ChatMessage[messages.Length];

        for (var index = 0; index < messages.Length; index++)
        {
            var message = messages[index];
            var role = message.Role switch
            {
                "system" => ChatRole.System,
                "user" => ChatRole.User,
                _ => throw new InvalidOperationException($"Unsupported prompt role '{message.Role}'."),
            };

            request[index] = new ChatMessage(role, message.Content);
        }

        return request;
    }
}
