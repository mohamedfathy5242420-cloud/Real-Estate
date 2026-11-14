using BrokerHub.Application.Abstractions.Identity;
using BrokerHub.Application.Authorization;
using BrokerHub.Application.DTOs.Employees;
using BrokerHub.Application.Exceptions;
using BrokerHub.Application.Interfaces.Persistence;
using BrokerHub.Application.Interfaces.Services;
using BrokerHub.Application.Mappings;
using BrokerHub.Domain.Entities;

namespace BrokerHub.Application.Services;

public sealed class EmployeeProfileService(IEmployeeRepository repository, IEmployeeQueries queries,
    ICurrentUser currentUser, TimeProvider clock) : IEmployeeProfileService
{
    private Guid? Administrator => currentUser.IsAuthenticated && currentUser.IsInRole(AppRoles.SystemAdministrator) &&
        currentUser.AccountId is { } id && id != Guid.Empty ? id : null;
    private static bool PageValid(int page, int size) => page is > 0 and <= 1000000 && size is > 0 and <= 100;
    public async Task<EmployeeProfileResult<EmployeeProfileDto>> CreateAsync(CreateEmployeeDto command, CancellationToken ct)
    {
        if (Administrator is not { } actor) return new(EmployeeProfileStatus.Forbidden);
        Employee employee;
        try { employee = Employee.Create(Guid.NewGuid(), command.AccountId, command.EmployeeNumber, command.DisplayName, clock.GetUtcNow().UtcDateTime, actor); }
        catch (ArgumentException) { return new(EmployeeProfileStatus.Invalid); }
        repository.Add(employee); return await Commit(employee, actor, ct);
    }
    public async Task<EmployeeProfileResult<EmployeeProfileDto>> UpdateAsync(Guid id, UpdateEmployeeDto command, CancellationToken ct)
    {
        if (Administrator is not { } actor) return new(EmployeeProfileStatus.Forbidden);
        if (command.Version <= 0) return new(EmployeeProfileStatus.Invalid);
        var employee = await repository.GetTrackedAsync(id, ct);
        if (employee is null) return new(EmployeeProfileStatus.NotFound);
        if (employee.Version != command.Version || employee.Version == long.MaxValue) return new(EmployeeProfileStatus.Conflict);
        try { employee.Update(command.DisplayName, command.IsActive, command.Version, clock.GetUtcNow().UtcDateTime, actor); }
        catch (ArgumentException) { return new(EmployeeProfileStatus.Invalid); }
        return await Commit(employee, actor, ct);
    }
    public async Task<EmployeeProfileResult<EmployeeProfileDto>> GetAsync(Guid id, CancellationToken ct)
    {
        if (Administrator is null) return new(EmployeeProfileStatus.Forbidden);
        var value = await queries.GetAsync(id, ct); return new(value is null ? EmployeeProfileStatus.NotFound : EmployeeProfileStatus.Success, value);
    }
    public async Task<EmployeeProfileResult<EmployeePage<EmployeeProfileDto>>> GetPageAsync(int page, int size, bool? isActive, CancellationToken ct)
    {
        if (Administrator is null) return new(EmployeeProfileStatus.Forbidden);
        if (!PageValid(page, size)) return new(EmployeeProfileStatus.Invalid);
        return new(EmployeeProfileStatus.Success, await queries.GetPageAsync(page, size, isActive, ct));
    }
    public async Task<EmployeeProfileResult<EmployeePage<EmployeeRevisionDto>>> GetHistoryAsync(Guid id, int page, int size, CancellationToken ct)
    {
        if (Administrator is null) return new(EmployeeProfileStatus.Forbidden);
        if (!PageValid(page, size)) return new(EmployeeProfileStatus.Invalid);
        var value = await queries.GetHistoryAsync(id, page, size, ct); return new(value is null ? EmployeeProfileStatus.NotFound : EmployeeProfileStatus.Success, value);
    }
    private async Task<EmployeeProfileResult<EmployeeProfileDto>> Commit(Employee employee, Guid actor, CancellationToken ct)
    {
        try { await repository.CommitAsync(employee, actor, ct); }
        catch (OptimisticConcurrencyException) { return new(EmployeeProfileStatus.Conflict); }
        catch (UniqueConstraintViolationException) { return new(EmployeeProfileStatus.Conflict); }
        catch (EmployeeEligibilityException ex) { return new(ex.ActorDenied ? EmployeeProfileStatus.Forbidden : EmployeeProfileStatus.Conflict); }
        return new(EmployeeProfileStatus.Success, employee.ToProfileDto());
    }
}
