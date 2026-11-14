using BrokerHub.Application.DTOs.CustomerAssignments;
using BrokerHub.Application.DTOs.FollowUp;

namespace BrokerHub.Application.Interfaces.Services;
public interface ICrmFollowUpService
{
    Task<CrmResult<CrmPage<CrmCustomerDto>>> QueueAsync(int page, int size, bool? hasRequirements, bool? hasNotes, CancellationToken ct);
    Task<CrmResult<CrmPage<SalesNoteDto>>> NotesAsync(Guid customerId, Guid? leadId, int page, int size, CancellationToken ct);
    Task<CrmResult<SalesNoteDto>> NoteAsync(Guid customerId, Guid noteId, CancellationToken ct);
    Task<CrmResult<CrmPage<CustomerNeedsChangeDto>>> NeedsHistoryAsync(Guid customerId, int page, int size, CancellationToken ct);
    Task<CrmResult<CustomerNeedsChangeDto>> NeedsChangeAsync(Guid customerId, Guid changeId, CancellationToken ct);
    Task<CrmResult<CrmMutationReply>> IntakeAsync(IntakeCustomerDto request, string key, CancellationToken ct);
    Task<CrmResult<CrmMutationReply>> ChangeNeedsAsync(Guid customerId, ChangeCustomerNeedsDto request, string key, CancellationToken ct);
    Task<CrmResult<CrmMutationReply>> AddNoteAsync(Guid customerId, Guid? correctsNoteId, AddSalesNoteDto request, string key, CancellationToken ct);
}
