using System.Text.Json;
using BrokerHub.Application.Abstractions.Identity;
using BrokerHub.Application.Authorization;
using BrokerHub.Application.DTOs.Commands;
using BrokerHub.Application.DTOs.CustomerAssignments;
using BrokerHub.Application.Interfaces.Persistence;
using BrokerHub.Application.Interfaces.Services;
using BrokerHub.Domain.Entities;

namespace BrokerHub.Application.Services;

public sealed class CustomerAssignmentService(ICrmCustomerRepository repository, ICrmCustomerQueries queries,
    ICommandExecutor executor, ICurrentUser currentUser, TimeProvider clock) : ICustomerAssignmentService
{
    public const string AssignOperation = "Crm.Customer.Assign";
    public const string TransferOperation = "Crm.Customer.Transfer";
    private Guid? Actor(bool managerOnly = false) => currentUser.IsAuthenticated && currentUser.AccountId is { } id && id != Guid.Empty &&
        (currentUser.IsInRole(AppRoles.SalesManager) || (!managerOnly && currentUser.IsInRole(AppRoles.SalesEmployee))) ? id : null;
    private static bool PageValid(int page, int size) => page is > 0 and <= 1000000 && size is > 0 and <= 100;
    public Task<CrmResult<CrmPage<CrmCustomerDto>>> GetPageAsync(int page, int size, bool unassignedOnly, CancellationToken ct) =>
        Actor(unassignedOnly) is not { } actor ? Task.FromResult(new CrmResult<CrmPage<CrmCustomerDto>>(CrmStatus.Forbidden)) :
        !PageValid(page, size) ? Task.FromResult(new CrmResult<CrmPage<CrmCustomerDto>>(CrmStatus.Invalid)) : queries.GetPageAsync(actor, page, size, unassignedOnly, ct);
    public Task<CrmResult<CrmCustomerDto>> GetAsync(Guid id, CancellationToken ct) => Actor() is not { } actor
        ? Task.FromResult(new CrmResult<CrmCustomerDto>(CrmStatus.Forbidden)) : queries.GetAsync(actor, id, ct);
    public Task<CrmResult<CrmPage<CustomerAssignmentDto>>> GetHistoryAsync(Guid id, int page, int size, CancellationToken ct) =>
        Actor() is not { } actor ? Task.FromResult(new CrmResult<CrmPage<CustomerAssignmentDto>>(CrmStatus.Forbidden)) :
        !PageValid(page, size) ? Task.FromResult(new CrmResult<CrmPage<CustomerAssignmentDto>>(CrmStatus.Invalid)) : queries.GetHistoryAsync(actor, id, page, size, ct);
    public Task<CrmResult<CustomerAssignmentDto>> GetAssignmentAsync(Guid id, Guid assignment, CancellationToken ct) => Actor() is not { } actor
        ? Task.FromResult(new CrmResult<CustomerAssignmentDto>(CrmStatus.Forbidden)) : queries.GetAssignmentAsync(actor, id, assignment, ct);
    public Task<CrmResult<CrmPage<AssignmentCandidateDto>>> GetCandidatesAsync(int page, int size, CancellationToken ct) =>
        Actor(true) is not { } actor ? Task.FromResult(new CrmResult<CrmPage<AssignmentCandidateDto>>(CrmStatus.Forbidden)) :
        !PageValid(page, size) ? Task.FromResult(new CrmResult<CrmPage<AssignmentCandidateDto>>(CrmStatus.Invalid)) : queries.GetCandidatesAsync(actor, page, size, ct);

    public async Task<CrmResult<CustomerAssignmentReply>> ChangeAsync(Guid customerId, bool transfer, ChangeCustomerAssignmentDto command, string key, CancellationToken ct)
    {
        if (Actor(true) is not { } actor) return new(CrmStatus.Forbidden);
        var reason = string.IsNullOrWhiteSpace(command.Reason) ? null : command.Reason.Trim();
        if (customerId == Guid.Empty || command.EmployeeId == Guid.Empty || command.Version <= 0 || reason?.Length > CustomerAssignment.ReasonMaxLength || (transfer && reason is null))
            return new(CrmStatus.Invalid);
        Customer? customer = null;
        var intent = JsonSerializer.SerializeToElement(new { customerId, employeeId = command.EmployeeId, version = command.Version, reason });
        var result = await executor.ExecuteAsync(transfer ? TransferOperation : AssignOperation, key, intent,
            async token =>
            {
                // Fresh on every strategy attempt/replay, before receipt lookup. No Version/target check here.
                customer = null;
                var access = await repository.LockForSalesAsync(customerId, actor, true, token);
                customer = access.Customer; return access.Permission;
            },
            async (context, token) =>
            {
                var c = customer ?? throw new InvalidOperationException("Authorized Customer root required.");
                if (c.Version != command.Version || c.Version == long.MaxValue || (transfer ? !c.AssignedEmployeeId.HasValue : c.AssignedEmployeeId.HasValue) || c.AssignedEmployeeId == command.EmployeeId)
                    return new CommandEffect(CommandStatus.Conflict);
                if (!await repository.LockEligibleAssigneeAsync(command.EmployeeId, token)) return new CommandEffect(CommandStatus.Conflict);
                var previous = c.AssignedEmployeeId; var now = clock.GetUtcNow().UtcDateTime;
                CustomerAssignment history;
                try
                {
                    c.AssignTo(command.EmployeeId, command.Version, now, context.ActorAccountId);
                    history = CustomerAssignment.Record(context.CommandId, c.Id, previous, command.EmployeeId, context.ActorAccountId, now, reason, c.Version);
                }
                catch (ArgumentException) { return new CommandEffect(CommandStatus.Invalid); }
                repository.AddAssignment(history);
                context.Emit(transfer ? "Crm.CustomerTransferred" : "Crm.CustomerAssigned", 1, c.Id,
                    JsonSerializer.SerializeToElement(new { customerId = c.Id, assignmentId = history.Id, previousEmployeeId = previous, newEmployeeId = command.EmployeeId, customerVersion = c.Version }));
                return new CommandEffect(CommandStatus.Success, new("CustomerAssignmentChange", history.Id, c.Version));
            }, ct);
        return result.Status switch
        {
            CommandStatus.Success => new(CrmStatus.Success, new(result.Outcome!.ResourceId, customerId, result.Outcome.ResourceVersion, result.IsReplay)),
            CommandStatus.Forbidden => new(CrmStatus.Forbidden),
            CommandStatus.NotFound => new(CrmStatus.NotFound),
            CommandStatus.Conflict => new(CrmStatus.Conflict),
            _ => new(CrmStatus.Invalid)
        };
    }
}
