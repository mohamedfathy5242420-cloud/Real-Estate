using BrokerHub.Application.DTOs.CustomerAssignments;

namespace BrokerHub.Application.Interfaces.Services;

public interface ICustomerAssignmentService
{
    Task<CrmResult<CrmPage<CrmCustomerDto>>> GetPageAsync(int page, int size, bool unassignedOnly, CancellationToken ct);
    Task<CrmResult<CrmCustomerDto>> GetAsync(Guid id, CancellationToken ct);
    Task<CrmResult<CrmPage<CustomerAssignmentDto>>> GetHistoryAsync(Guid customerId, int page, int size, CancellationToken ct);
    Task<CrmResult<CustomerAssignmentDto>> GetAssignmentAsync(Guid customerId, Guid assignmentId, CancellationToken ct);
    Task<CrmResult<CrmPage<AssignmentCandidateDto>>> GetCandidatesAsync(int page, int size, CancellationToken ct);
    Task<CrmResult<CustomerAssignmentReply>> ChangeAsync(Guid customerId, bool transfer, ChangeCustomerAssignmentDto command, string key, CancellationToken ct);
}
