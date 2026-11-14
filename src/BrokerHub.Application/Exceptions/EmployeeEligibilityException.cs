namespace BrokerHub.Application.Exceptions;

public sealed class EmployeeEligibilityException(bool actorDenied) : Exception("Current staff eligibility changed.")
{
    public bool ActorDenied { get; } = actorDenied;
}
