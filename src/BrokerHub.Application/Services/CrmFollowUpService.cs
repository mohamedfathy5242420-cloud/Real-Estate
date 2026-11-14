using System.Text.Json;
using BrokerHub.Application.Abstractions.Identity;
using BrokerHub.Application.Authorization;
using BrokerHub.Application.DTOs.Commands;
using BrokerHub.Application.DTOs.CustomerAssignments;
using BrokerHub.Application.DTOs.FollowUp;
using BrokerHub.Application.Interfaces.Persistence;
using BrokerHub.Application.Interfaces.Services;
using BrokerHub.Domain.Entities;

namespace BrokerHub.Application.Services;
public sealed class CrmFollowUpService(ICrmFollowUpRepository repository, ICrmFollowUpQueries queries,
    ICrmCustomerRepository scope, ILeadRepository leads, ICommandExecutor executor, ICurrentUser user, TimeProvider clock) : ICrmFollowUpService
{
    private Guid? Actor(bool manager = false) => user.IsAuthenticated && user.AccountId is { } id && id != Guid.Empty &&
        (user.IsInRole(AppRoles.SalesManager) || (!manager && user.IsInRole(AppRoles.SalesEmployee))) ? id : null;
    private static bool Page(int page, int size) => page is > 0 and <= 1000000 && size is > 0 and <= 100;
    private static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    public Task<CrmResult<CrmPage<CrmCustomerDto>>> QueueAsync(int page, int size, bool? hasRequirements, bool? hasNotes, CancellationToken ct) =>
        Actor() is not { } actor ? Task.FromResult(new CrmResult<CrmPage<CrmCustomerDto>>(CrmStatus.Forbidden)) : !Page(page, size) ? Task.FromResult(new CrmResult<CrmPage<CrmCustomerDto>>(CrmStatus.Invalid)) : queries.QueueAsync(actor, page, size, hasRequirements, hasNotes, ct);
    public Task<CrmResult<CrmPage<SalesNoteDto>>> NotesAsync(Guid customerId, Guid? leadId, int page, int size, CancellationToken ct) =>
        Actor() is not { } actor ? Task.FromResult(new CrmResult<CrmPage<SalesNoteDto>>(CrmStatus.Forbidden)) : !Page(page, size) || leadId == Guid.Empty ? Task.FromResult(new CrmResult<CrmPage<SalesNoteDto>>(CrmStatus.Invalid)) : queries.NotesAsync(actor, customerId, leadId, page, size, ct);
    public Task<CrmResult<SalesNoteDto>> NoteAsync(Guid customerId, Guid noteId, CancellationToken ct) => Actor() is not { } actor
        ? Task.FromResult(new CrmResult<SalesNoteDto>(CrmStatus.Forbidden)) : queries.NoteAsync(actor, customerId, noteId, ct);
    public Task<CrmResult<CrmPage<CustomerNeedsChangeDto>>> NeedsHistoryAsync(Guid customerId, int page, int size, CancellationToken ct) =>
        Actor() is not { } actor ? Task.FromResult(new CrmResult<CrmPage<CustomerNeedsChangeDto>>(CrmStatus.Forbidden)) : !Page(page, size) ? Task.FromResult(new CrmResult<CrmPage<CustomerNeedsChangeDto>>(CrmStatus.Invalid)) : queries.NeedsHistoryAsync(actor, customerId, page, size, ct);
    public Task<CrmResult<CustomerNeedsChangeDto>> NeedsChangeAsync(Guid customerId, Guid changeId, CancellationToken ct) => Actor() is not { } actor
        ? Task.FromResult(new CrmResult<CustomerNeedsChangeDto>(CrmStatus.Forbidden)) : queries.NeedsChangeAsync(actor, customerId, changeId, ct);

    public async Task<CrmResult<CrmMutationReply>> IntakeAsync(IntakeCustomerDto request, string key, CancellationToken ct)
    {
        if (Actor(true) is not { } actor) return new(CrmStatus.Forbidden);
        var normalized = new IntakeCustomerDto(request.FullName?.Trim() ?? "", request.Phone?.Trim() ?? "", Optional(request.Email), Optional(request.PreferredContactMethod), Optional(request.Requirements));
        var result = await executor.ExecuteAsync("Crm.Customer.Intake", key, JsonSerializer.SerializeToElement(normalized),
            async token => await repository.LockManagerAsync(actor, token) ? CommandPermission.Allowed : CommandPermission.Forbidden,
            (context, _) =>
            {
                Customer c;
                try { c = Customer.Create(context.CommandId, null, normalized.FullName, normalized.Phone, normalized.Email, normalized.PreferredContactMethod, normalized.Requirements, clock.GetUtcNow().UtcDateTime, context.ActorAccountId); }
                catch (ArgumentException) { return Task.FromResult(new CommandEffect(CommandStatus.Invalid)); }
                repository.AddCustomer(c);
                context.Emit("Crm.CustomerCreated", 1, c.Id, JsonSerializer.SerializeToElement(new { customerId = c.Id, version = c.Version }));
                return Task.FromResult(new CommandEffect(CommandStatus.Success, new CommandOutcome("CrmCustomer", c.Id, c.Version)));
            }, ct);
        return Map(result, result.Outcome?.ResourceId ?? Guid.Empty);
    }
    public async Task<CrmResult<CrmMutationReply>> ChangeNeedsAsync(Guid customerId, ChangeCustomerNeedsDto request, string key, CancellationToken ct)
    {
        if (Actor() is not { } actor) return new(CrmStatus.Forbidden);
        var needs = Optional(request.Requirements);
        if (customerId == Guid.Empty || request.Version <= 0 || needs?.Length > Customer.RequirementsMaxLength) return new(CrmStatus.Invalid);
        Customer? customer = null;
        var result = await executor.ExecuteAsync("Crm.Customer.Requirements", key, JsonSerializer.SerializeToElement(new { customerId, version = request.Version, requirements = needs }),
            async token => { customer = null; var access = await scope.LockForSalesAsync(customerId, actor, false, token); customer = access.Customer; return access.Permission; },
            (context, _) =>
            {
                var c = customer ?? throw new InvalidOperationException("Authorized Customer required.");
                if (c.Version != request.Version || c.Version == long.MaxValue) return Task.FromResult(new CommandEffect(CommandStatus.Conflict));
                var now = clock.GetUtcNow().UtcDateTime;
                if (now < c.CreatedAtUtc || (c.UpdatedAtUtc is { } updated && now < updated)) return Task.FromResult(new CommandEffect(CommandStatus.Conflict));
                var before = c.Requirements;
                c.UpdateProfile(c.FullName, c.Phone, c.Email, c.PreferredContactMethod, needs, request.Version, now, context.ActorAccountId);
                var change = CustomerNeedsChange.Record(context.CommandId, c, before, now, context.ActorAccountId); repository.AddNeedsChange(change);
                context.Emit("Crm.CustomerRequirementsChanged", 1, c.Id, JsonSerializer.SerializeToElement(new { customerId = c.Id, changeId = change.Id, customerVersion = c.Version }));
                return Task.FromResult(new CommandEffect(CommandStatus.Success, new CommandOutcome("CustomerNeedsChange", change.Id, c.Version)));
            }, ct);
        return Map(result, customerId);
    }
    public async Task<CrmResult<CrmMutationReply>> AddNoteAsync(Guid customerId, Guid? correctsNoteId, AddSalesNoteDto request, string key, CancellationToken ct)
    {
        if (Actor() is not { } actor) return new(CrmStatus.Forbidden);
        if (customerId == Guid.Empty || correctsNoteId == Guid.Empty || request.LeadId == Guid.Empty || string.IsNullOrWhiteSpace(request.Body) || request.Body.Trim().Length > SalesNote.BodyMaxLength || (correctsNoteId.HasValue && request.LeadId.HasValue)) return new(CrmStatus.Invalid);
        SalesNote? original = null; Customer? customer = null; var body = request.Body.Trim();
        var result = await executor.ExecuteAsync(correctsNoteId.HasValue ? "Crm.Note.Correct" : "Crm.Note.Add", key,
            JsonSerializer.SerializeToElement(new { customerId, correctsNoteId, leadId = request.LeadId, body }),
            async token =>
            {
                original = null; customer = null; var access = await scope.LockForSalesAsync(customerId, actor, false, token); customer = access.Customer;
                if (access.Permission != CommandPermission.Allowed) return access.Permission;
                if (correctsNoteId is { } previous) { original = await repository.LockNoteAsync(customerId, previous, token); if (original is null) return CommandPermission.NotFound; }
                if (request.LeadId is { } lead && await leads.LockByIdAsync(customerId, lead, token) is null) return CommandPermission.NotFound;
                return CommandPermission.Allowed;
            },
            async (context, token) =>
            {
                var now = clock.GetUtcNow().UtcDateTime;
                if (now < customer!.CreatedAtUtc || (original is not null && now < original.CreatedAtUtc)) return new CommandEffect(CommandStatus.Conflict);
                var employee = await repository.FindAuthorEmployeeAsync(context.ActorAccountId, token);
                var note = SalesNote.Create(context.CommandId, customerId, original?.LeadId ?? request.LeadId, context.ActorAccountId, employee, body, now, correctsNoteId);
                repository.AddNote(note);
                context.Emit(correctsNoteId.HasValue ? "Crm.NoteCorrected" : "Crm.NoteAdded", 1, customerId,
                    JsonSerializer.SerializeToElement(new { customerId, noteId = note.Id, leadId = note.LeadId, correctsNoteId }));
                return new CommandEffect(CommandStatus.Success, new("SalesNote", note.Id, 1));
            }, ct);
        return Map(result, customerId);
    }
    private static CrmResult<CrmMutationReply> Map(CommandResult result, Guid customerId) => result.Status switch
    {
        CommandStatus.Success => new(CrmStatus.Success, new(result.Outcome!.ResourceId, customerId, result.Outcome.ResourceVersion, result.IsReplay)),
        CommandStatus.Forbidden => new(CrmStatus.Forbidden), CommandStatus.NotFound => new(CrmStatus.NotFound),
        CommandStatus.Conflict => new(CrmStatus.Conflict), _ => new(CrmStatus.Invalid)
    };
}
