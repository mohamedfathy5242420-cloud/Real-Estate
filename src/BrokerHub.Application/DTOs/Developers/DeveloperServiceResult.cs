namespace BrokerHub.Application.DTOs.Developers;

public enum DeveloperServiceStatus
{
    Success,
    ValidationFailed,
    NotFound,
    Conflict,
    Forbidden
}

public sealed record DeveloperServiceResult<T>(
    DeveloperServiceStatus Status,
    T? Value,
    IReadOnlyDictionary<string, string[]> Errors)
{
    public static DeveloperServiceResult<T> Success(T value) =>
        new(DeveloperServiceStatus.Success, value, EmptyErrors);

    public static DeveloperServiceResult<T> Validation(
        IReadOnlyDictionary<string, string[]> errors) =>
        new(DeveloperServiceStatus.ValidationFailed, default, errors);

    public static DeveloperServiceResult<T> NotFound(string message) =>
        Failure(DeveloperServiceStatus.NotFound, "Developer", message);

    public static DeveloperServiceResult<T> Conflict(string field, string message) =>
        Failure(DeveloperServiceStatus.Conflict, field, message);

    public static DeveloperServiceResult<T> Forbidden() =>
        Failure(
            DeveloperServiceStatus.Forbidden,
            "Authorization",
            "SystemAdministrator permission is required.");

    private static IReadOnlyDictionary<string, string[]> EmptyErrors { get; } =
        new Dictionary<string, string[]>();

    private static DeveloperServiceResult<T> Failure(
        DeveloperServiceStatus status,
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
