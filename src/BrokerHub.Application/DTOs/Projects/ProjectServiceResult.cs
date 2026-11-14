namespace BrokerHub.Application.DTOs.Projects;

public enum ProjectServiceStatus
{
    Success,
    ValidationFailed,
    NotFound,
    Conflict,
    Forbidden
}

public sealed record ProjectServiceResult<T>(
    ProjectServiceStatus Status,
    T? Value,
    IReadOnlyDictionary<string, string[]> Errors)
{
    public static ProjectServiceResult<T> Success(T value) =>
        new(ProjectServiceStatus.Success, value, EmptyErrors);

    public static ProjectServiceResult<T> Validation(
        IReadOnlyDictionary<string, string[]> errors) =>
        new(ProjectServiceStatus.ValidationFailed, default, errors);

    public static ProjectServiceResult<T> NotFound(string field, string message) =>
        Failure(ProjectServiceStatus.NotFound, field, message);

    public static ProjectServiceResult<T> Conflict(string field, string message) =>
        Failure(ProjectServiceStatus.Conflict, field, message);

    public static ProjectServiceResult<T> Forbidden() =>
        Failure(
            ProjectServiceStatus.Forbidden,
            "Authorization",
            "SystemAdministrator permission is required.");

    private static IReadOnlyDictionary<string, string[]> EmptyErrors { get; } =
        new Dictionary<string, string[]>();

    private static ProjectServiceResult<T> Failure(
        ProjectServiceStatus status,
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
