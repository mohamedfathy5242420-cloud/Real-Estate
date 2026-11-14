using BrokerHub.Application.DTOs.Employees;

namespace BrokerHub.Application.Interfaces.Persistence;

public interface IEmployeeQueries
{
    Task<EmployeeProfileDto?> GetAsync(Guid id, CancellationToken ct);
    Task<EmployeePage<EmployeeProfileDto>> GetPageAsync(int page, int size, bool? isActive, CancellationToken ct);
    Task<EmployeePage<EmployeeRevisionDto>?> GetHistoryAsync(Guid id, int page, int size, CancellationToken ct);
}
