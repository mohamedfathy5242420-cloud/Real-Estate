namespace BrokerHub.Application.DTOs.Customers;

public sealed record OwnCustomerLinkDto(Guid Id, string Claim, string Status, DateTime CreatedAtUtc, DateTime? DecidedAtUtc, long Version);
public sealed record CustomerLinkEventDto(Guid Id, string Kind, Guid ActorAccountId, DateTime AtUtc, string? Reason, Guid? CustomerId);
public sealed record CustomerLinkReviewDto(Guid Id, Guid AccountId, string Claim, string Status, DateTime CreatedAtUtc,
    DateTime? DecidedAtUtc, long Version, string? DecisionReason, Guid? DecidedByAccountId, Guid? CustomerId,
    IReadOnlyList<CustomerLinkEventDto> History);
public sealed record CustomerLinkCandidateDto(Guid Id, string FullName, string Phone, string? Email, long Version);
public sealed record CustomerLinkPage<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total);
public sealed record ReviewCustomerLinkDto(long Version, string Reason, Guid? CustomerId = null, long? CustomerVersion = null);
public enum CustomerLinkStatus { Success, Invalid, NotFound, Conflict, Forbidden }
public sealed record CustomerLinkResult<T>(CustomerLinkStatus Status, T? Value = default);
