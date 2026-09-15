using LinguaDesk.Api.Features.Operations;
using LinguaDesk.Infrastructure.Ai;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace LinguaDesk.Api.Infrastructure.Serving;

/// <summary>
/// Live translation provider over <see cref="ChatCompletionsAdapter"/> and the
/// candidate registry as a primary-only serving chain (no fallback per
/// DF-004). Returns false while the serving section is invalid so the
/// <c>Unavailable*</c> pending behavior is retained. A missing key at dispatch
/// time is a blocked failure, never a silent pending masquerade.
/// </summary>
public sealed class ServingTranslationClientProvider : ITranslationClientProvider, IDisposable
{
    private static readonly HttpClient SharedHttpClient = new()
    {
        Timeout = Timeout.InfiniteTimeSpan,
    };

    private readonly IOptions<ServingOptions> options;
    private readonly IConfiguration configuration;
    private readonly HttpClient httpClient;
    private bool disposed;

    public ServingTranslationClientProvider(IOptions<ServingOptions> options, IConfiguration configuration)
        : this(options, configuration, SharedHttpClient)
    {
    }

    internal ServingTranslationClientProvider(IOptions<ServingOptions> options, IConfiguration configuration, HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(httpClient);
        this.options = options;
        this.configuration = configuration;
        this.httpClient = httpClient;
    }

    public void Dispose()
    {
        disposed = true;
    }

    public bool TryGetClients(out Func<CandidateProfile, IChatClient> clients, out FamilyChain chain)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        var selected = options.Value?.Translation;
        if (selected is null
            || string.IsNullOrWhiteSpace(selected.CandidateId)
            || string.IsNullOrWhiteSpace(selected.CredentialRef)
            || !(selected.MaxSpendUsdPerOperation > 0))
        {
            clients = null!;
            chain = null!;
            return false;
        }

        FamilyChain serving;
        try
        {
            serving = FamilyChain.Create(ChainFamily.Translation, CandidateRegistry.Default, selected.CandidateId);
        }
        catch (CandidateProfileException)
        {
            clients = null!;
            chain = null!;
            return false;
        }

        if (!string.Equals(selected.CredentialRef, serving.Primary.CredentialRef, StringComparison.Ordinal))
        {
            clients = null!;
            chain = null!;
            return false;
        }

        var transport = httpClient;
        chain = serving;
        clients = profile =>
        {
            ArgumentNullException.ThrowIfNull(profile);
            if (!ServingCredential.TryResolve(configuration, profile.CredentialRef, out var credential) || credential is null)
            {
                throw new ChatCompletionsAdapterException(
                    AttemptFailureKind.Blocked,
                    null,
                    $"Candidate '{profile.CandidateId}' has no transport credential; a requested live run without a credential is blocked, never faked.");
            }

            var adapter = new ChatCompletionsAdapter(profile, transport);
            return new ServingChatClient(adapter, credential);
        };
        return true;
    }
}
