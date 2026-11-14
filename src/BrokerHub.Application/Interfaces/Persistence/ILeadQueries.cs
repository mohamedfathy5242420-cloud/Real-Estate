using BrokerHub.Application.DTOs.CustomerAssignments;
using BrokerHub.Application.DTOs.Leads;

namespace BrokerHub.Application.Interfaces.Persistence;
public interface ILeadQueries
{
    Task<CrmResult<CrmPage<LeadDto>>> ListAsync(Guid actor, Guid customerId, int page, int size, string? status, CancellationToken ct);
    Task<CrmResult<LeadDto>> GetAsync(Guid actor, Guid customerId, Guid id, CancellationToken ct);
    Task<CrmResult<CrmPage<LeadEventDto>>> HistoryAsync(Guid actor, Guid customerId, Guid id, int page, int size, CancellationToken ct);
    Task<CrmResult<LeadEventDto>> EventAsync(Guid actor, Guid customerId, Guid id, Guid eventId, CancellationToken ct);
}
