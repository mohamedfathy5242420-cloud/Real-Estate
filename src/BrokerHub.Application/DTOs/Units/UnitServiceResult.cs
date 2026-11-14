namespace BrokerHub.Application.DTOs.Units;

public enum UnitServiceStatus
{
    Success,
    ValidationFailed,
    NotFound,
    Conflict,
    Forbidden
}

public sealed record UnitServiceResult<T>(
    UnitServiceStatus Status,
    T? Value,
    IReadOnlyDictionary<string, string[]> Errors)
{
    public static UnitServiceResult<T> Success(T value) =>
        new(UnitServiceStatus.Success, value, EmptyErrors);

    public static UnitServiceResult<T> Validation(
        IReadOnlyDictionary<string, string[]> errors) =>
        new(UnitServiceStatus.ValidationFailed, default, errors);

    public static UnitServiceResult<T> NotFound(string field, string message) =>
        Failure(UnitServiceStatus.NotFound, field, message);

    public static UnitServiceResult<T> Conflict(string field, string message) =>
        Failure(UnitServiceStatus.Conflict, field, message);

    public static UnitServiceResult<T> Forbidden() =>
        Failure(
            UnitServiceStatus.Forbidden,
            "Authorization",
            "SystemAdministrator permission is required.");

    private static IReadOnlyDictionary<string, string[]> EmptyErrors { get; } =
        new Dictionary<string, string[]>();

    private static UnitServiceResult<T> Failure(
        UnitServiceStatus status,
        string field,
        string message) =>
        new(
            status,
            default,
            new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                [field] = [message]
            });
}
