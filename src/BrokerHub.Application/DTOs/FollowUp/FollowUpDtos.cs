namespace BrokerHub.Application.DTOs.FollowUp;
public sealed record IntakeCustomerDto(string FullName, string Phone, string? Email, string? PreferredContactMethod, string? Requirements);
public sealed record ChangeCustomerNeedsDto(long Version, string? Requirements);
public sealed record AddSalesNoteDto(string Body, Guid? LeadId = null);
public sealed record CrmMutationReply(Guid Id, Guid CustomerId, long Version, bool IsReplay);
public sealed record SalesNoteDto(Guid Id, Guid CustomerId, Guid? LeadId, Guid AuthorAccountId, Guid? AuthorEmployeeId, string Body, DateTime CreatedAtUtc, Guid? CorrectsNoteId);
public sealed record CustomerNeedsChangeDto(Guid Id, Guid CustomerId, string? PreviousRequirements, string? Requirements, long CustomerVersion, Guid ActorAccountId, DateTime AtUtc);
