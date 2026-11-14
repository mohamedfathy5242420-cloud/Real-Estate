using BrokerHub.Application.Abstractions.Identity;
using BrokerHub.Application.Authorization;
using BrokerHub.Application.DTOs.Projects;
using BrokerHub.Application.Exceptions;
using BrokerHub.Application.Interfaces.Persistence;
using BrokerHub.Application.Interfaces.Services;
using BrokerHub.Application.Mappings;
using BrokerHub.Domain.Entities;

namespace BrokerHub.Application.Services;

public sealed class ProjectService(
    IProjectRepository repository,
    IProjectQueryService queryService,
    IDeveloperRepository developerRepository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IProjectService
{
    public async Task<ProjectServiceResult<ProjectDto>> CreateAsync(
        CreateProjectDto request,
        CancellationToken cancellationToken)
    {
        if (!IsAuthorized())
        {
            return ProjectServiceResult<ProjectDto>.Forbidden();
        }

        var errors = Validate(
            request.DeveloperId,
            request.Code,
            request.Name,
            request.Area,
            request.Address,
            request.TimeZoneId,
            version: null);
        if (errors.Count > 0)
        {
            return ProjectServiceResult<ProjectDto>.Validation(errors);
        }

        var developer = await developerRepository.GetTrackedByIdAsync(
            request.DeveloperId,
            cancellationToken);
        if (developer is null)
        {
            return ProjectServiceResult<ProjectDto>.NotFound(
                nameof(request.DeveloperId),
                "Developer was not found.");
        }

        if (!developer.IsActive)
        {
            return ProjectServiceResult<ProjectDto>.Conflict(
                nameof(request.DeveloperId),
                "Developer must be active before a Project can be created.");
        }

        var code = request.Code.Trim();
        if (await repository.CodeExistsAsync(request.DeveloperId, code, cancellationToken))
        {
            return ProjectServiceResult<ProjectDto>.Conflict(
                nameof(request.Code),
                "Project code already exists for this Developer.");
        }

        var project = Project.Create(
            Guid.NewGuid(),
            request.DeveloperId,
            code,
            request.Name,
            request.Area,
            request.Address,
            request.TimeZoneId,
            timeProvider.GetUtcNow().UtcDateTime,
            currentUser.AccountId!.Value);

        await repository.AddAsync(project, cancellationToken);
        var persistenceFailure = await SaveAsync<ProjectDto>(cancellationToken, request.DeveloperId);
        return persistenceFailure ?? ProjectServiceResult<ProjectDto>.Success(project.ToDto());
    }

    public async Task<ProjectServiceResult<ProjectDto>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!IsAuthorized())
        {
            return ProjectServiceResult<ProjectDto>.Forbidden();
        }

        var project = await queryService.GetByIdAsync(id, cancellationToken);
        return project is null
            ? ProjectServiceResult<ProjectDto>.NotFound("Project", "Project was not found.")
            : ProjectServiceResult<ProjectDto>.Success(project);
    }

    public async Task<ProjectServiceResult<IReadOnlyList<ProjectDto>>> ListAsync(
        Guid? developerId,
        bool includeInactive,
        CancellationToken cancellationToken)
    {
        if (!IsAuthorized())
        {
            return ProjectServiceResult<IReadOnlyList<ProjectDto>>.Forbidden();
        }

        if (developerId == Guid.Empty)
        {
            return ProjectServiceResult<IReadOnlyList<ProjectDto>>.Validation(
                new Dictionary<string, string[]>(StringComparer.Ordinal)
                {
                    [nameof(developerId)] = ["Developer ID must not be empty."]
                });
        }

        var projects = await queryService.ListAsync(developerId, includeInactive, cancellationToken);
        return ProjectServiceResult<IReadOnlyList<ProjectDto>>.Success(projects);
    }

    public async Task<ProjectServiceResult<ProjectDto>> UpdateAsync(
        Guid id,
        UpdateProjectDto request,
        CancellationToken cancellationToken)
    {
        if (!IsAuthorized())
        {
            return ProjectServiceResult<ProjectDto>.Forbidden();
        }

        var errors = Validate(
            developerId: null,
            code: null,
            request.Name,
            request.Area,
            request.Address,
            request.TimeZoneId,
            request.Version);
        if (errors.Count > 0)
        {
            return ProjectServiceResult<ProjectDto>.Validation(errors);
        }

        var project = await repository.GetTrackedByIdAsync(id, cancellationToken);
        if (project is null)
        {
            return ProjectServiceResult<ProjectDto>.NotFound("Project", "Project was not found.");
        }

        if (project.Version != request.Version)
        {
            return ProjectServiceResult<ProjectDto>.Conflict(
                nameof(request.Version),
                "Project version is stale. Reload and retry.");
        }

        project.UpdateDetails(
            request.Name,
            request.Area,
            request.Address,
            request.TimeZoneId,
            request.Version,
            timeProvider.GetUtcNow().UtcDateTime,
            currentUser.AccountId!.Value);

        var persistenceFailure = await SaveAsync<ProjectDto>(cancellationToken);
        return persistenceFailure ?? ProjectServiceResult<ProjectDto>.Success(project.ToDto());
    }

    public Task<ProjectServiceResult<ProjectDto>> DeactivateAsync(
        Guid id,
        long version,
        CancellationToken cancellationToken) =>
        ChangeStatusAsync(id, version, activate: false, cancellationToken);

    public Task<ProjectServiceResult<ProjectDto>> ReactivateAsync(
        Guid id,
        long version,
        CancellationToken cancellationToken) =>
        ChangeStatusAsync(id, version, activate: true, cancellationToken);

    private async Task<ProjectServiceResult<ProjectDto>> ChangeStatusAsync(
        Guid id,
        long version,
        bool activate,
        CancellationToken cancellationToken)
    {
        if (!IsAuthorized())
        {
            return ProjectServiceResult<ProjectDto>.Forbidden();
        }

        if (version <= 0)
        {
            return ProjectServiceResult<ProjectDto>.Validation(
                new Dictionary<string, string[]>(StringComparer.Ordinal)
                {
                    [nameof(version)] = ["Version must be greater than zero."]
                });
        }

        var project = await repository.GetTrackedByIdAsync(id, cancellationToken);
        if (project is null)
        {
            return ProjectServiceResult<ProjectDto>.NotFound("Project", "Project was not found.");
        }

        if (project.Version != version)
        {
            return ProjectServiceResult<ProjectDto>.Conflict(
                nameof(version),
                "Project version is stale. Reload and retry.");
        }

        if (activate && !project.IsActive)
        {
            var developer = await developerRepository.GetTrackedByIdAsync(
                project.DeveloperId,
                cancellationToken);
            if (developer is null)
            {
                return ProjectServiceResult<ProjectDto>.NotFound(
                    "DeveloperId",
                    "Developer was not found.");
            }

            if (!developer.IsActive)
            {
                return ProjectServiceResult<ProjectDto>.Conflict(
                    "DeveloperId",
                    "Developer must be active before a Project can be reactivated.");
            }
        }

        var changed = activate
            ? project.Reactivate(version, timeProvider.GetUtcNow().UtcDateTime, currentUser.AccountId!.Value)
            : project.Deactivate(version, timeProvider.GetUtcNow().UtcDateTime, currentUser.AccountId!.Value);

        if (changed)
        {
            var persistenceFailure = await SaveAsync<ProjectDto>(
                cancellationToken,
                activate ? project.DeveloperId : null);
            if (persistenceFailure is not null)
            {
                return persistenceFailure;
            }
        }

        return ProjectServiceResult<ProjectDto>.Success(project.ToDto());
    }

    private bool IsAuthorized() =>
        currentUser.IsAuthenticated &&
        currentUser.AccountId.HasValue &&
        currentUser.IsInRole(AppRoles.SystemAdministrator);

    private async Task<ProjectServiceResult<T>?> SaveAsync<T>(
        CancellationToken cancellationToken,
        Guid? activeDeveloperId = null)
    {
        try
        {
            if (activeDeveloperId.HasValue)
            {
                await unitOfWork.SaveChangesWithActiveDeveloperAsync(activeDeveloperId.Value, cancellationToken);
            }
            else
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            return null;
        }
        catch (DeveloperNotActiveException)
        {
            return ProjectServiceResult<T>.Conflict(
                "DeveloperId",
                "Developer is no longer active. Reload and retry.");
        }
        catch (UniqueConstraintViolationException)
        {
            return ProjectServiceResult<T>.Conflict(
                "Code",
                "Project code already exists for this Developer.");
        }
        catch (OptimisticConcurrencyException)
        {
            return ProjectServiceResult<T>.Conflict(
                "Version",
                "Project changed concurrently. Reload and retry.");
        }
    }

    private static Dictionary<string, string[]> Validate(
        Guid? developerId,
        string? code,
        string name,
        string area,
        string? address,
        string timeZoneId,
        long? version)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);

        if (developerId == Guid.Empty)
        {
            errors[nameof(developerId)] = ["Developer ID is required."];
        }

        if (code is not null)
        {
            ValidateRequired(errors, nameof(code), code, Project.CodeMaxLength);
        }

        ValidateRequired(errors, nameof(name), name, Project.NameMaxLength);
        ValidateRequired(errors, nameof(area), area, Project.AreaMaxLength);
        ValidateOptional(errors, nameof(address), address, Project.AddressMaxLength);
        ValidateRequired(errors, nameof(timeZoneId), timeZoneId, Project.TimeZoneIdMaxLength);

        if (version.HasValue && version.Value <= 0)
        {
            errors[nameof(version)] = ["Version must be greater than zero."];
        }

        return errors;
    }

    private static void ValidateRequired(
        IDictionary<string, string[]> errors,
        string field,
        string value,
        int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors[field] = ["Value is required."];
        }
        else if (value.Trim().Length > maxLength)
        {
            errors[field] = [$"Value cannot exceed {maxLength} characters."];
        }
    }

    private static void ValidateOptional(
        IDictionary<string, string[]> errors,
        string field,
        string? value,
        int maxLength)
    {
        if (!string.IsNullOrWhiteSpace(value) && value.Trim().Length > maxLength)
        {
            errors[field] = [$"Value cannot exceed {maxLength} characters."];
        }
    }
}
