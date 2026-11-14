namespace BrokerHub.Application.Exceptions;

public sealed class DeveloperNotActiveException()
    : Exception("The Developer is no longer active at the persistence boundary.");
