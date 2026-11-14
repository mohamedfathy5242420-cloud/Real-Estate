using BrokerHub.Application.Abstractions.Identity;

namespace BrokerHub.Infrastructure.Identity;

internal sealed class UnconfiguredAccountEmailSender : IAccountEmailSender
{
    private const string ErrorMessage =
        "A production account-email sender is not configured. Configure the P08 email adapter.";

    public Task SendConfirmationLinkAsync(
        string email,
        string confirmationLink,
        CancellationToken cancellationToken)
    {
        throw new InvalidOperationException(ErrorMessage);
    }

    public Task SendPasswordResetLinkAsync(
        string email,
        string resetLink,
        CancellationToken cancellationToken)
    {
        throw new InvalidOperationException(ErrorMessage);
    }
}
