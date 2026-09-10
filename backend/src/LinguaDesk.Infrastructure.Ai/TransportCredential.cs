using System.Diagnostics;

namespace LinguaDesk.Infrastructure.Ai;

[DebuggerDisplay("TransportCredential(<redacted>)")]
public sealed class TransportCredential : IDisposable
{
    private string? apiKey;
    private bool disposed;

    public TransportCredential(string apiKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        this.apiKey = apiKey;
    }

    public string GetApiKey()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return apiKey ?? throw new InvalidOperationException("The transport credential has been disposed.");
    }

    public override string ToString() => "TransportCredential(<redacted>)";

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        apiKey = null;
    }
}
