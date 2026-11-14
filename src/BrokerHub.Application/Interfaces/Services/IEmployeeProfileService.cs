using BrokerHub.Application.DTOs.Employees;

namespace BrokerHub.Application.Interfaces.Services;

public interface IEmployeeProfileService
{
    Task<EmployeeProfileResult<EmployeeProfileDto>> CreateAsync(CreateEmployeeDto command, CancellationToken ct);
    Task<EmployeeProfileResult<EmployeeProfileDto>> UpdateAsync(Guid id, UpdateEmployeeDto command, CancellationToken ct);
    Task<EmployeeProfileResult<EmployeeProfileDto>> GetAsync(Guid id, CancellationToken ct);
    Task<EmployeeProfileResult<EmployeePage<EmployeeProfileDto>>> GetPageAsync(int page, int size, bool? isActive, CancellationToken ct);
    Task<EmployeeProfileResult<EmployeePage<EmployeeRevisionDto>>> GetHistoryAsync(Guid id, int page, int size, CancellationToken ct);
}
