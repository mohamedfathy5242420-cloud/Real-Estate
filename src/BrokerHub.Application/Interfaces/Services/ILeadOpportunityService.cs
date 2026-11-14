using BrokerHub.Application.DTOs.Commands;
using BrokerHub.Application.DTOs.Leads;

namespace BrokerHub.Application.Interfaces.Services;
// Same command scope/transaction and already authorized locked Customer are caller preconditions.
// Stages only; this port neither authorizes callers nor starts/saves/commits a transaction.
public interface ILeadOpportunityService
{
    Task<LeadOpenResult> EnsureOpenAsync(Guid customerId, Guid projectId, string source, CommandExecutionContext context, CancellationToken ct);
}
