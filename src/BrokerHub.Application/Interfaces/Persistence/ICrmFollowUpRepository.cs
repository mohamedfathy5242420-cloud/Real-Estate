using BrokerHub.Domain.Entities;

namespace BrokerHub.Application.Interfaces.Persistence;
public interface ICrmFollowUpRepository
{
    Task<bool> LockManagerAsync(Guid actor, CancellationToken ct);
    Task<SalesNote?> LockNoteAsync(Guid customerId, Guid noteId, CancellationToken ct);
    Task<Guid?> FindAuthorEmployeeAsync(Guid actor, CancellationToken ct);
    void AddCustomer(Customer customer);
    void AddNote(SalesNote note);
    void AddNeedsChange(CustomerNeedsChange change);
}
