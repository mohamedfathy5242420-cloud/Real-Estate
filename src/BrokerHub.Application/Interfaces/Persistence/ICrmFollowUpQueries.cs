using BrokerHub.Application.DTOs.CustomerAssignments;
using BrokerHub.Application.DTOs.FollowUp;

namespace BrokerHub.Application.Interfaces.Persistence;
public interface ICrmFollowUpQueries
{
    Task<CrmResult<CrmPage<CrmCustomerDto>>> QueueAsync(Guid actor, int page, int size, bool? hasRequirements, bool? hasNotes, CancellationToken ct);
    Task<CrmResult<CrmPage<SalesNoteDto>>> NotesAsync(Guid actor, Guid customerId, Guid? leadId, int page, int size, CancellationToken ct);
    Task<CrmResult<SalesNoteDto>> NoteAsync(Guid actor, Guid customerId, Guid noteId, CancellationToken ct);
    Task<CrmResult<CrmPage<CustomerNeedsChangeDto>>> NeedsHistoryAsync(Guid actor, Guid customerId, int page, int size, CancellationToken ct);
    Task<CrmResult<CustomerNeedsChangeDto>> NeedsChangeAsync(Guid actor, Guid customerId, Guid changeId, CancellationToken ct);
}
