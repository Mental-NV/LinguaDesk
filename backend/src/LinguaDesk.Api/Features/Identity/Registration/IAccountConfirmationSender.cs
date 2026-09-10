namespace LinguaDesk.Api.Features.Identity.Registration;

public interface IAccountConfirmationSender
{
    Task SendAsync(AccountConfirmationDelivery delivery, CancellationToken cancellationToken);
}

public sealed record AccountConfirmationDelivery(string Destination, string UserId, string Code);

internal sealed class UnavailableAccountConfirmationSender : IAccountConfirmationSender
{
    public Task SendAsync(AccountConfirmationDelivery delivery, CancellationToken cancellationToken) =>
        throw new AccountConfirmationDeliveryException();
}

public sealed class AccountConfirmationDeliveryException : Exception;
