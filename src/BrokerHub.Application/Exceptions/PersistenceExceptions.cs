namespace BrokerHub.Application.Exceptions;

public sealed class UniqueConstraintViolationException(string message, Exception innerException)
    : Exception(message, innerException);

public sealed class OptimisticConcurrencyException(string message, Exception innerException)
    : Exception(message, innerException);
