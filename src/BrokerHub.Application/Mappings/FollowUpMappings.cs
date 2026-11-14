using BrokerHub.Application.DTOs.FollowUp;
using BrokerHub.Domain.Entities;

namespace BrokerHub.Application.Mappings;
public static class FollowUpMappings
{
    public static SalesNoteDto ToDto(this SalesNote n) => new(n.Id, n.CustomerId, n.LeadId, n.AuthorAccountId, n.AuthorEmployeeId, n.Body, DateTime.SpecifyKind(n.CreatedAtUtc, DateTimeKind.Utc), n.CorrectsNoteId);
    public static CustomerNeedsChangeDto ToDto(this CustomerNeedsChange h) => new(h.Id, h.CustomerId, h.PreviousRequirements, h.Requirements, h.CustomerVersion, h.ActorAccountId, DateTime.SpecifyKind(h.AtUtc, DateTimeKind.Utc));
}
