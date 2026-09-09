namespace LinguaDesk.Api.Features.Identity.Registration;

public interface IAccountConfirmationSender
{
    Task SendAsync(string destination, string verificationMaterial, CancellationToken cancellationToken);
}

internal sealed class UnavailableAccountConfirmationSender : IAccountConfirmationSender
{
    public Task SendAsync(string destination, string verificationMaterial, CancellationToken cancellationToken) =>
        throw new AccountConfirmationDeliveryException();
}

public sealed class AccountConfirmationDeliveryException : Exception;
