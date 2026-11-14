namespace BrokerHub.Application.Exceptions;

public sealed class CatalogParentNotActiveException(string field)
    : Exception("The catalog parent is missing, inactive or changed at the persistence boundary.")
{
    public string Field { get; } = field;
}
