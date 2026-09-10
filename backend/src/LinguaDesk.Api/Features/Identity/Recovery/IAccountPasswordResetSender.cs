namespace LinguaDesk.Api.Features.Identity.Recovery;

public interface IAccountPasswordResetSender
{
    Task SendAsync(AccountPasswordResetDelivery delivery, CancellationToken cancellationToken);
}

public sealed record AccountPasswordResetDelivery(string Destination, string UserId, string Code);

internal sealed class UnavailableAccountPasswordResetSender : IAccountPasswordResetSender
{
    public Task SendAsync(AccountPasswordResetDelivery delivery, CancellationToken cancellationToken) =>
        throw new AccountPasswordResetDeliveryException();
}

public sealed class AccountPasswordResetDeliveryException : Exception;

public static class AccountPasswordResetPolicy
{
    public static readonly TimeSpan TokenLifespan = TimeSpan.FromMinutes(60);
}
