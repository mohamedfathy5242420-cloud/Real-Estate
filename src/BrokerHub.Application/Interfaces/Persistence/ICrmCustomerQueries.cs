using BrokerHub.Application.DTOs.CustomerAssignments;

namespace BrokerHub.Application.Interfaces.Persistence;

public interface ICrmCustomerQueries
{
    Task<CrmResult<CrmPage<CrmCustomerDto>>> GetPageAsync(Guid actor, int page, int size, bool unassignedOnly, CancellationToken ct);
    Task<CrmResult<CrmCustomerDto>> GetAsync(Guid actor, Guid id, CancellationToken ct);
    Task<CrmResult<CrmPage<CustomerAssignmentDto>>> GetHistoryAsync(Guid actor, Guid customerId, int page, int size, CancellationToken ct);
    Task<CrmResult<CustomerAssignmentDto>> GetAssignmentAsync(Guid actor, Guid customerId, Guid assignmentId, CancellationToken ct);
    Task<CrmResult<CrmPage<AssignmentCandidateDto>>> GetCandidatesAsync(Guid actor, int page, int size, CancellationToken ct);
}
