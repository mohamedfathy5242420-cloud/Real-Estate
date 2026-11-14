using BrokerHub.Application.DTOs.Customers;

namespace BrokerHub.Application.Interfaces.Services;

public interface ICustomerProfileService
{
    Task<CustomerProfileResult<CustomerProfileDto>> CreateOwnAsync(CreateCustomerProfileDto request, CancellationToken ct);
    Task<CustomerProfileResult<CustomerProfileDto>> GetOwnAsync(CancellationToken ct);
    Task<CustomerProfileResult<CustomerProfileDto>> GetByIdAsync(Guid id, CancellationToken ct);
    Task<CustomerProfileResult<CustomerProfileDto>> UpdateAsync(Guid id, UpdateCustomerProfileDto request, CancellationToken ct);
}
