using BrokerHub.Application.Abstractions.Identity;
using BrokerHub.Application.Authorization;
using BrokerHub.Application.DTOs.Units;
using BrokerHub.Application.Exceptions;
using BrokerHub.Application.Interfaces.Persistence;
using BrokerHub.Application.Interfaces.Services;
using BrokerHub.Application.Mappings;
using BrokerHub.Domain.Entities;

namespace BrokerHub.Application.Services;

public sealed class UnitService(
    IUnitRepository repository,
    IUnitQueryService queryService,
    IProjectRepository projectRepository,
    IDeveloperRepository developerRepository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IUnitService
{
    public async Task<UnitServiceResult<UnitDto>> CreateAsync(CreateUnitDto request, CancellationToken cancellationToken)
    {
        if (!IsAuthorized()) return UnitServiceResult<UnitDto>.Forbidden();
        string code;
        // Validate before any database access; no failed mutation of tracked objects.
        try
        {
            if (request.ProjectId == Guid.Empty)
                throw new ArgumentException("Project ID is required.", nameof(request.ProjectId));
            code = Unit.NormalizeCode(request.Code);
            Unit.ValidateDetails(request.Title, request.Description, request.UnitType, request.AreaSqm,
                request.Bedrooms, request.Bathrooms, request.ListPrice, request.Currency);
        }
        catch (ArgumentException exception)
        {
            return Validation(exception);
        }

        var project = await projectRepository.GetTrackedByIdAsync(request.ProjectId, cancellationToken);
        if (project is null) return UnitServiceResult<UnitDto>.NotFound("ProjectId", "Project was not found.");
        if (!project.IsActive) return UnitServiceResult<UnitDto>.Conflict("ProjectId", "Project must be active.");
        var developer = await developerRepository.GetTrackedByIdAsync(project.DeveloperId, cancellationToken);
        if (developer is null) return UnitServiceResult<UnitDto>.NotFound("DeveloperId", "Developer was not found.");
        if (!developer.IsActive) return UnitServiceResult<UnitDto>.Conflict("DeveloperId", "Developer must be active.");
        if (await repository.CodeExistsAsync(request.ProjectId, code, cancellationToken))
            return UnitServiceResult<UnitDto>.Conflict("Code", "Unit code already exists for this Project.");

        // The early checks are not the final guard; persistence protects both parents until commit.
        var unit = Unit.Create(Guid.NewGuid(), request.ProjectId, code, request.Title, request.Description,
            request.UnitType, request.AreaSqm, request.Bedrooms, request.Bathrooms, request.ListPrice,
            request.Currency, timeProvider.GetUtcNow().UtcDateTime, currentUser.AccountId!.Value);
        await repository.AddAsync(unit, cancellationToken);
        var failure = await SaveAsync(cancellationToken, project);
        return failure ?? UnitServiceResult<UnitDto>.Success(unit.ToDto());
    }

    public async Task<UnitServiceResult<UnitDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!IsAuthorized()) return UnitServiceResult<UnitDto>.Forbidden();
        var unit = await queryService.GetByIdAsync(id, cancellationToken);
        return unit is null ? UnitServiceResult<UnitDto>.NotFound("Unit", "Unit was not found.")
            : UnitServiceResult<UnitDto>.Success(unit);
    }

    public async Task<UnitServiceResult<IReadOnlyList<UnitDto>>> ListAsync(Guid? projectId, CancellationToken cancellationToken)
    {
        if (!IsAuthorized()) return UnitServiceResult<IReadOnlyList<UnitDto>>.Forbidden();
        if (projectId == Guid.Empty)
            return UnitServiceResult<IReadOnlyList<UnitDto>>.Validation(new Dictionary<string, string[]>
            {
                ["ProjectId"] = ["Project ID must not be empty."]
            });
        return UnitServiceResult<IReadOnlyList<UnitDto>>.Success(await queryService.ListAsync(projectId, cancellationToken));
    }

    public async Task<UnitServiceResult<UnitDto>> UpdateAsync(Guid id, UpdateUnitDto request, CancellationToken cancellationToken)
    {
        if (!IsAuthorized()) return UnitServiceResult<UnitDto>.Forbidden();
        if (request.Version <= 0)
            return UnitServiceResult<UnitDto>.Validation(new Dictionary<string, string[]> { ["Version"] = ["Version must be positive."] });
        try
        {
            Unit.ValidateDetails(request.Title, request.Description, request.UnitType, request.AreaSqm,
                request.Bedrooms, request.Bathrooms, request.ListPrice, request.Currency);
        }
        catch (ArgumentException exception) { return Validation(exception); }
        var unit = await repository.GetTrackedByIdAsync(id, cancellationToken);
        if (unit is null) return UnitServiceResult<UnitDto>.NotFound("Unit", "Unit was not found.");
        if (unit.Version != request.Version || unit.Version == long.MaxValue)
            return UnitServiceResult<UnitDto>.Conflict("Version", "Unit version is stale or exhausted. Reload and retry.");
        unit.UpdateDetails(request.Title, request.Description, request.UnitType, request.AreaSqm,
            request.Bedrooms, request.Bathrooms, request.ListPrice, request.Currency, request.Version,
            timeProvider.GetUtcNow().UtcDateTime, currentUser.AccountId!.Value);
        var failure = await SaveAsync(cancellationToken);
        return failure ?? UnitServiceResult<UnitDto>.Success(unit.ToDto());
    }

    private bool IsAuthorized() => currentUser.IsAuthenticated && currentUser.AccountId.HasValue &&
        currentUser.IsInRole(AppRoles.SystemAdministrator);

    private static UnitServiceResult<UnitDto> Validation(ArgumentException exception) =>
        UnitServiceResult<UnitDto>.Validation(new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            [exception.ParamName ?? "Unit"] = [exception.Message]
        });

    private async Task<UnitServiceResult<UnitDto>?> SaveAsync(CancellationToken cancellationToken, Project? parent = null)
    {
        try
        {
            if (parent is null) await unitOfWork.SaveChangesAsync(cancellationToken);
            else await unitOfWork.SaveChangesWithActiveCatalogParentsAsync(parent.Id, parent.DeveloperId, cancellationToken);
            return null;
        }
        catch (CatalogParentNotActiveException exception)
        {
            return UnitServiceResult<UnitDto>.Conflict(exception.Field, "Catalog parent is no longer active. Reload and retry.");
        }
        catch (UniqueConstraintViolationException)
        {
            return UnitServiceResult<UnitDto>.Conflict("Code", "Unit code already exists for this Project.");
        }
        catch (OptimisticConcurrencyException)
        {
            return UnitServiceResult<UnitDto>.Conflict("Version", "Unit changed concurrently. Reload and retry.");
        }
    }
}
