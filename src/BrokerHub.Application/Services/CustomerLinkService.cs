using BrokerHub.Application.Abstractions.Identity;
using BrokerHub.Application.Authorization;
using BrokerHub.Application.DTOs.Customers;
using BrokerHub.Application.Exceptions;
using BrokerHub.Application.Interfaces.Persistence;
using BrokerHub.Application.Interfaces.Services;
using BrokerHub.Application.Mappings;
using BrokerHub.Domain.Entities;
using BrokerHub.Domain.Enums;

namespace BrokerHub.Application.Services;

public sealed class CustomerLinkService(ICustomerLinkRepository repository, ICustomerLinkQueries queries,
    ICurrentUser currentUser, TimeProvider clock) : ICustomerLinkService
{
    private Guid? Actor(string role) => currentUser.IsAuthenticated && currentUser.IsInRole(role) &&
        currentUser.AccountId is { } id && id != Guid.Empty ? id : null;
    private static bool PageValid(int page, int size) => page is > 0 and <= 1000000 && size is > 0 and <= 100;
    public async Task<CustomerLinkResult<OwnCustomerLinkDto>> SubmitAsync(string claim, CancellationToken ct)
    {
        if (Actor(AppRoles.Customer) is not { } actor) return new(CustomerLinkStatus.Forbidden);
        CustomerLinkRequest request;
        try { request = CustomerLinkRequest.Submit(Guid.NewGuid(), actor, claim, clock.GetUtcNow().UtcDateTime); }
        catch (ArgumentException) { return new(CustomerLinkStatus.Invalid); }
        if (await repository.HasProfileAsync(actor, ct)) return new(CustomerLinkStatus.Conflict);
        repository.Add(request);
        var error = await Commit(request, null, ct);
        return new(error ?? CustomerLinkStatus.Success, error is null ? request.ToOwnDto() : null);
    }
    public async Task<CustomerLinkResult<OwnCustomerLinkDto>> GetOwnAsync(Guid id, CancellationToken ct)
    {
        if (Actor(AppRoles.Customer) is not { } actor) return new(CustomerLinkStatus.Forbidden);
        var value = await queries.GetOwnAsync(id, actor, ct);
        return new(value is null ? CustomerLinkStatus.NotFound : CustomerLinkStatus.Success, value);
    }
    public async Task<CustomerLinkResult<CustomerLinkPage<OwnCustomerLinkDto>>> GetOwnPageAsync(int page, int size, CancellationToken ct)
    {
        if (Actor(AppRoles.Customer) is not { } actor) return new(CustomerLinkStatus.Forbidden);
        if (!PageValid(page, size)) return new(CustomerLinkStatus.Invalid);
        return new(CustomerLinkStatus.Success, await queries.GetOwnPageAsync(actor, page, size, ct));
    }
    public async Task<CustomerLinkResult<CustomerLinkReviewDto>> GetReviewAsync(Guid id, CancellationToken ct)
    {
        if (Actor(AppRoles.SalesManager) is null) return new(CustomerLinkStatus.Forbidden);
        var value = await queries.GetReviewAsync(id, ct);
        return new(value is null ? CustomerLinkStatus.NotFound : CustomerLinkStatus.Success, value);
    }
    public async Task<CustomerLinkResult<CustomerLinkPage<CustomerLinkReviewDto>>> GetPendingPageAsync(int page, int size, CancellationToken ct)
    {
        if (Actor(AppRoles.SalesManager) is null) return new(CustomerLinkStatus.Forbidden);
        if (!PageValid(page, size)) return new(CustomerLinkStatus.Invalid);
        return new(CustomerLinkStatus.Success, await queries.GetPendingPageAsync(page, size, ct));
    }
    public async Task<CustomerLinkResult<CustomerLinkPage<CustomerLinkCandidateDto>>> GetCandidatesAsync(int page, int size, CancellationToken ct)
    {
        if (Actor(AppRoles.SalesManager) is null) return new(CustomerLinkStatus.Forbidden);
        if (!PageValid(page, size)) return new(CustomerLinkStatus.Invalid);
        return new(CustomerLinkStatus.Success, await queries.GetCandidatesAsync(page, size, ct));
    }
    public async Task<CustomerLinkResult<CustomerLinkReviewDto>> DecideAsync(Guid id, bool approve, ReviewCustomerLinkDto command, CancellationToken ct)
    {
        if (Actor(AppRoles.SalesManager) is not { } actor) return new(CustomerLinkStatus.Forbidden);
        if (command.Version <= 0 || string.IsNullOrWhiteSpace(command.Reason) || command.Reason.Trim().Length > CustomerLinkRequest.TextMaxLength ||
            (approve ? command.CustomerId is null || command.CustomerId == Guid.Empty || command.CustomerVersion is null or <= 0 : command.CustomerId is not null || command.CustomerVersion is not null))
            return new(CustomerLinkStatus.Invalid);
        var request = await repository.GetTrackedAsync(id, ct);
        if (request is null) return new(CustomerLinkStatus.NotFound);
        if (request.Status != CustomerLinkRequestStatus.Pending || request.Version != command.Version) return new(CustomerLinkStatus.Conflict);
        Customer? target = null;
        if (approve)
        {
            target = await repository.GetCustomerTrackedAsync(command.CustomerId!.Value, ct);
            if (target is null || target.AccountId is not null || target.Version != command.CustomerVersion || target.Version == long.MaxValue ||
                await repository.HasProfileAsync(request.AccountId, ct)) return new(CustomerLinkStatus.Conflict);
        }
        try
        {
            var now = clock.GetUtcNow().UtcDateTime;
            target?.LinkReviewedAccount(request.AccountId, command.CustomerVersion!.Value, now, actor);
            request.Decide(approve, target?.Id, command.Version, command.Reason, now, actor);
        }
        catch (ArgumentException) { return new(CustomerLinkStatus.Invalid); }
        var error = await Commit(request, target, ct);
        return new(error ?? CustomerLinkStatus.Success, error is null ? request.ToReviewDto() : null);
    }
    private async Task<CustomerLinkStatus?> Commit(CustomerLinkRequest request, Customer? target, CancellationToken ct)
    {
        try { await repository.CommitAsync(request, target, ct); return null; }
        catch (OptimisticConcurrencyException) { return CustomerLinkStatus.Conflict; }
        catch (UniqueConstraintViolationException) { return CustomerLinkStatus.Conflict; }
        catch (CustomerLinkEligibilityException ex) { return ex.ActorDenied ? CustomerLinkStatus.Forbidden : CustomerLinkStatus.Conflict; }
    }
}
