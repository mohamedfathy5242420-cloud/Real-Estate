using BrokerHub.Application.DTOs.Commands;
using BrokerHub.Domain.Entities;

namespace BrokerHub.Application.DTOs.CustomerAssignments;

public enum CrmStatus { Success, Invalid, Forbidden, NotFound, Conflict }
public sealed record CrmResult<T>(CrmStatus Status, T? Value = default);
public sealed record CrmPage<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total);
public sealed record CrmCustomerDto(Guid Id, string FullName, string Phone, string? Email, string? PreferredContactMethod,
    string? Requirements, Guid? AssignedEmployeeId, long Version, DateTime CreatedAtUtc, DateTime? UpdatedAtUtc);
public sealed record AssignmentCandidateDto(Guid Id, string EmployeeNumber, string DisplayName);
public sealed record CustomerAssignmentDto(Guid Id, Guid CustomerId, Guid? PreviousEmployeeId, Guid NewEmployeeId,
    Guid ActorAccountId, DateTime ChangedAtUtc, string? Reason, long CustomerVersion);
public sealed record ChangeCustomerAssignmentDto(Guid EmployeeId, long Version, string? Reason);
public sealed record CustomerAssignmentReply(Guid Id, Guid CustomerId, long CustomerVersion, bool IsReplay);
public sealed record CustomerSalesLock(CommandPermission Permission, Customer? Customer = null);
