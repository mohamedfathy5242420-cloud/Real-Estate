using BrokerHub.Application.Abstractions.Identity;
using BrokerHub.Application.Authorization;
using BrokerHub.Application.DTOs.Customers;
using BrokerHub.Application.Exceptions;
using BrokerHub.Application.Interfaces.Persistence;
using BrokerHub.Application.Interfaces.Services;
using BrokerHub.Application.Mappings;
using BrokerHub.Domain.Entities;

namespace BrokerHub.Application.Services;

public sealed class CustomerProfileService(ICustomerRepository repository, ICustomerQueryService queries,
    IUnitOfWork unitOfWork, ICurrentUser currentUser, TimeProvider clock) : ICustomerProfileService
{
    private Guid? Owner => currentUser.IsAuthenticated && currentUser.IsInRole(AppRoles.Customer) &&
        currentUser.AccountId is { } id && id != Guid.Empty ? id : null;

    public async Task<CustomerProfileResult<CustomerProfileDto>> CreateOwnAsync(CreateCustomerProfileDto request, CancellationToken ct)
    {
        if (Owner is not { } owner) return new(CustomerProfileStatus.Forbidden);
        Customer customer;
        try
        {
            customer = Customer.Create(Guid.NewGuid(), owner, request.FullName, request.Phone, request.Email,
                request.PreferredContactMethod, request.Requirements, clock.GetUtcNow().UtcDateTime, owner);
        }
        catch (ArgumentException ex) { return Invalid(ex); }
        if (await queries.GetOwnedAsync(owner, null, ct) is not null) return new(CustomerProfileStatus.Conflict);
        await repository.AddAsync(customer, ct);
        return await SaveAsync(customer, ct);
    }

    public Task<CustomerProfileResult<CustomerProfileDto>> GetOwnAsync(CancellationToken ct) => ReadAsync(null, ct);
    public Task<CustomerProfileResult<CustomerProfileDto>> GetByIdAsync(Guid id, CancellationToken ct) => ReadAsync(id, ct);

    private async Task<CustomerProfileResult<CustomerProfileDto>> ReadAsync(Guid? id, CancellationToken ct)
    {
        if (Owner is not { } owner) return new(CustomerProfileStatus.Forbidden);
        var profile = await queries.GetOwnedAsync(owner, id, ct);
        return new(profile is null ? CustomerProfileStatus.NotFound : CustomerProfileStatus.Success, profile);
    }

    public async Task<CustomerProfileResult<CustomerProfileDto>> UpdateAsync(Guid id, UpdateCustomerProfileDto request, CancellationToken ct)
    {
        if (Owner is not { } owner) return new(CustomerProfileStatus.Forbidden);
        if (request.Version <= 0) return new(CustomerProfileStatus.Invalid, Errors: new Dictionary<string, string[]>
            { ["version"] = ["A positive current version is required."] });
        var customer = await repository.GetOwnedTrackedAsync(id, owner, ct);
        if (customer is null) return new(CustomerProfileStatus.NotFound);
        if (customer.Version != request.Version || customer.Version == long.MaxValue) return new(CustomerProfileStatus.Conflict);
        try
        {
            customer.UpdateProfile(request.FullName, request.Phone, request.Email, request.PreferredContactMethod,
                request.Requirements, request.Version, clock.GetUtcNow().UtcDateTime, owner);
        }
        catch (ArgumentException ex) { return Invalid(ex); }
        return await SaveAsync(customer, ct);
    }

    private async Task<CustomerProfileResult<CustomerProfileDto>> SaveAsync(Customer customer, CancellationToken ct)
    {
        try { await unitOfWork.SaveChangesAsync(ct); }
        catch (OptimisticConcurrencyException) { return new(CustomerProfileStatus.Conflict); }
        catch (UniqueConstraintViolationException) { return new(CustomerProfileStatus.Conflict); }
        return new(CustomerProfileStatus.Success, customer.ToProfileDto());
    }

    private static CustomerProfileResult<CustomerProfileDto> Invalid(ArgumentException ex) =>
        new(CustomerProfileStatus.Invalid, Errors: new Dictionary<string, string[]>
            { [ex.ParamName ?? "profile"] = [ex.Message] });
}
