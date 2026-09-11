using LinguaDesk.Infrastructure.Ai;
using Microsoft.Extensions.AI;

namespace LinguaDesk.Api.Features.Operations;

/// <summary>
/// Supplies the translation provider clients and family chain for synchronous
/// translation execution. Production composition registers no provider, so
/// translation submissions remain pending reservations; the test harness
/// overrides this with a deterministic fake that is inaccessible in
/// production configuration.
/// </summary>
public interface ITranslationClientProvider
{
    bool TryGetClients(out Func<CandidateProfile, IChatClient> clients, out FamilyChain chain);
}

/// <summary>Production translation client provider: no serving configuration exists yet.</summary>
public sealed class UnavailableTranslationClientProvider : ITranslationClientProvider
{
    public bool TryGetClients(out Func<CandidateProfile, IChatClient> clients, out FamilyChain chain)
    {
        clients = null!;
        chain = null!;
        return false;
    }
}
