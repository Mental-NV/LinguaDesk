using LinguaDesk.Infrastructure.Ai;
using Microsoft.Extensions.AI;

namespace LinguaDesk.Api.Features.Operations;

/// <summary>
/// Supplies the rewriting provider clients and family chain for synchronous
/// rewriting execution. Production composition registers no provider, so
/// rewriting submissions remain pending reservations; the test harness
/// overrides this with a deterministic fake that is inaccessible in
/// production configuration.
/// </summary>
public interface IRewritingClientProvider
{
    bool TryGetClients(out Func<CandidateProfile, IChatClient> clients, out FamilyChain chain);
}

/// <summary>Production rewriting client provider: no serving configuration exists yet.</summary>
public sealed class UnavailableRewritingClientProvider : IRewritingClientProvider
{
    public bool TryGetClients(out Func<CandidateProfile, IChatClient> clients, out FamilyChain chain)
    {
        clients = null!;
        chain = null!;
        return false;
    }
}
