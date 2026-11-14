namespace BrokerHub.Application.Abstractions.Identity;

public interface IAccountEmailSender
{
    Task SendConfirmationLinkAsync(
        string email,
        string confirmationLink,
        CancellationToken cancellationToken);

    Task SendPasswordResetLinkAsync(
        string email,
        string resetLink,
        CancellationToken cancellationToken);
}
