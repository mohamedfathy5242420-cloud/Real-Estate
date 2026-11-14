using BrokerHub.Application.DTOs.CustomerAssignments;
using BrokerHub.Application.DTOs.Leads;

namespace BrokerHub.Application.Interfaces.Services;
public interface ILeadService
{
    Task<CrmResult<CrmPage<LeadDto>>> ListAsync(Guid customerId, int page, int size, string? status, CancellationToken ct);
    Task<CrmResult<LeadDto>> GetAsync(Guid customerId, Guid id, CancellationToken ct);
    Task<CrmResult<CrmPage<LeadEventDto>>> HistoryAsync(Guid customerId, Guid id, int page, int size, CancellationToken ct);
    Task<CrmResult<LeadEventDto>> EventAsync(Guid customerId, Guid id, Guid eventId, CancellationToken ct);
    Task<CrmResult<LeadReply>> OpenAsync(Guid customerId, OpenLeadDto request, string key, CancellationToken ct);
    Task<CrmResult<LeadReply>> CloseAsync(Guid customerId, Guid id, CloseLeadDto request, string key, CancellationToken ct);
}
