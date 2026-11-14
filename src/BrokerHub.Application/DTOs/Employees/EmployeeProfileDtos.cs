namespace BrokerHub.Application.DTOs.Employees;

public sealed record CreateEmployeeDto(Guid AccountId, string EmployeeNumber, string DisplayName);
public sealed record UpdateEmployeeDto(string DisplayName, bool IsActive, long Version);
public sealed record EmployeeProfileDto(Guid Id, Guid AccountId, string EmployeeNumber, string DisplayName,
    bool IsActive, long Version, DateTime CreatedAtUtc, Guid CreatedByAccountId, DateTime? UpdatedAtUtc, Guid? UpdatedByAccountId);
public sealed record EmployeeRevisionDto(Guid Id, long Version, string DisplayName, bool IsActive, DateTime AtUtc, Guid ActorAccountId);
public sealed record EmployeePage<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total);
public enum EmployeeProfileStatus { Success, Invalid, Forbidden, NotFound, Conflict }
public sealed record EmployeeProfileResult<T>(EmployeeProfileStatus Status, T? Value = default);
