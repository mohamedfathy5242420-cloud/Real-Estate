namespace BrokerHub.Application.Exceptions;

public sealed class CustomerLinkEligibilityException(bool actorDenied) : Exception("Current account or link eligibility changed.")
{
    public bool ActorDenied { get; } = actorDenied;
}
