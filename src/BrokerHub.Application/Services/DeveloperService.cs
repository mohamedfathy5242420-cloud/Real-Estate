using BrokerHub.Application.Abstractions.Identity;
using BrokerHub.Application.Authorization;
using BrokerHub.Application.DTOs.Developers;
using BrokerHub.Application.Exceptions;
using BrokerHub.Application.Interfaces.Persistence;
using BrokerHub.Application.Interfaces.Services;
using BrokerHub.Application.Mappings;
using BrokerHub.Domain.Entities;

namespace BrokerHub.Application.Services;

public sealed class DeveloperService(
    IDeveloperRepository repository,
    IDeveloperQueryService queryService,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IDeveloperService
{
    public async Task<DeveloperServiceResult<DeveloperDto>> CreateAsync(
        CreateDeveloperDto request,
        CancellationToken cancellationToken)
    {
        if (!IsAuthorized())
        {
            return DeveloperServiceResult<DeveloperDto>.Forbidden();
        }

        var errors = Validate(request.Code, request.Name, request.LegalName, request.ExternalContactName,
            request.ExternalContactEmail, request.ExternalContactPhone, version: null);
        if (errors.Count > 0)
        {
            return DeveloperServiceResult<DeveloperDto>.Validation(errors);
        }

        var code = request.Code.Trim();
        if (await repository.CodeExistsAsync(code, cancellationToken))
        {
            return DeveloperServiceResult<DeveloperDto>.Conflict(
                nameof(request.Code),
                "Developer code already exists.");
        }

        var developer = Developer.Create(
            Guid.NewGuid(),
            code,
            request.Name,
            request.LegalName,
            request.ExternalContactName,
            request.ExternalContactEmail,
            request.ExternalContactPhone,
            timeProvider.GetUtcNow().UtcDateTime,
            currentUser.AccountId!.Value);

        await repository.AddAsync(developer, cancellationToken);
        var persistenceFailure = await SaveAsync<DeveloperDto>(cancellationToken);
        return persistenceFailure ?? DeveloperServiceResult<DeveloperDto>.Success(developer.ToDto());
    }

    public async Task<DeveloperServiceResult<DeveloperDto>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!IsAuthorized())
        {
            return DeveloperServiceResult<DeveloperDto>.Forbidden();
        }

        var developer = await queryService.GetByIdAsync(id, cancellationToken);
        return developer is null
            ? DeveloperServiceResult<DeveloperDto>.NotFound("Developer was not found.")
            : DeveloperServiceResult<DeveloperDto>.Success(developer);
    }

    public async Task<DeveloperServiceResult<IReadOnlyList<DeveloperDto>>> ListAsync(
        bool includeInactive,
        CancellationToken cancellationToken)
    {
        if (!IsAuthorized())
        {
            return DeveloperServiceResult<IReadOnlyList<DeveloperDto>>.Forbidden();
        }

        var developers = await queryService.ListAsync(includeInactive, cancellationToken);
        return DeveloperServiceResult<IReadOnlyList<DeveloperDto>>.Success(developers);
    }

    public async Task<DeveloperServiceResult<DeveloperDto>> UpdateAsync(
        Guid id,
        UpdateDeveloperDto request,
        CancellationToken cancellationToken)
    {
        if (!IsAuthorized())
        {
            return DeveloperServiceResult<DeveloperDto>.Forbidden();
        }

        var errors = Validate(code: null, request.Name, request.LegalName, request.ExternalContactName,
            request.ExternalContactEmail, request.ExternalContactPhone, request.Version);
        if (errors.Count > 0)
        {
            return DeveloperServiceResult<DeveloperDto>.Validation(errors);
        }

        var developer = await repository.GetTrackedByIdAsync(id, cancellationToken);
        if (developer is null)
        {
            return DeveloperServiceResult<DeveloperDto>.NotFound("Developer was not found.");
        }

        if (developer.Version != request.Version)
        {
            return DeveloperServiceResult<DeveloperDto>.Conflict(
                nameof(request.Version),
                "Developer version is stale. Reload and retry.");
        }

        developer.UpdateDetails(
            request.Name,
            request.LegalName,
            request.ExternalContactName,
            request.ExternalContactEmail,
            request.ExternalContactPhone,
            request.Version,
            timeProvider.GetUtcNow().UtcDateTime,
            currentUser.AccountId!.Value);

        var persistenceFailure = await SaveAsync<DeveloperDto>(cancellationToken);
        return persistenceFailure ?? DeveloperServiceResult<DeveloperDto>.Success(developer.ToDto());
    }

    public Task<DeveloperServiceResult<DeveloperDto>> DeactivateAsync(
        Guid id,
        long version,
        CancellationToken cancellationToken) =>
        ChangeStatusAsync(id, version, activate: false, cancellationToken);

    public Task<DeveloperServiceResult<DeveloperDto>> ReactivateAsync(
        Guid id,
        long version,
        CancellationToken cancellationToken) =>
        ChangeStatusAsync(id, version, activate: true, cancellationToken);

    private async Task<DeveloperServiceResult<DeveloperDto>> ChangeStatusAsync(
        Guid id,
        long version,
        bool activate,
        CancellationToken cancellationToken)
    {
        if (!IsAuthorized())
        {
            return DeveloperServiceResult<DeveloperDto>.Forbidden();
        }

        if (version <= 0)
        {
            return DeveloperServiceResult<DeveloperDto>.Validation(
                new Dictionary<string, string[]>(StringComparer.Ordinal)
                {
                    [nameof(version)] = ["Version must be greater than zero."]
                });
        }

        var developer = await repository.GetTrackedByIdAsync(id, cancellationToken);
        if (developer is null)
        {
            return DeveloperServiceResult<DeveloperDto>.NotFound("Developer was not found.");
        }

        if (developer.Version != version)
        {
            return DeveloperServiceResult<DeveloperDto>.Conflict(
                nameof(version),
                "Developer version is stale. Reload and retry.");
        }

        var changed = activate
            ? developer.Reactivate(version, timeProvider.GetUtcNow().UtcDateTime, currentUser.AccountId!.Value)
            : developer.Deactivate(version, timeProvider.GetUtcNow().UtcDateTime, currentUser.AccountId!.Value);

        if (changed)
        {
            var persistenceFailure = await SaveAsync<DeveloperDto>(cancellationToken);
            if (persistenceFailure is not null)
            {
                return persistenceFailure;
            }
        }

        return DeveloperServiceResult<DeveloperDto>.Success(developer.ToDto());
    }

    private bool IsAuthorized() =>
        currentUser.IsAuthenticated &&
        currentUser.AccountId.HasValue &&
        currentUser.IsInRole(AppRoles.SystemAdministrator);

    private async Task<DeveloperServiceResult<T>?> SaveAsync<T>(CancellationToken cancellationToken)
    {
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return null;
        }
        catch (UniqueConstraintViolationException)
        {
            return DeveloperServiceResult<T>.Conflict("Code", "Developer code already exists.");
        }
        catch (OptimisticConcurrencyException)
        {
            return DeveloperServiceResult<T>.Conflict(
                "Version",
                "Developer changed concurrently. Reload and retry.");
        }
    }

    private static Dictionary<string, string[]> Validate(
        string? code,
        string name,
        string? legalName,
        string? externalContactName,
        string? externalContactEmail,
        string? externalContactPhone,
        long? version)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);

        if (code is not null)
        {
            ValidateRequired(errors, nameof(code), code, Developer.CodeMaxLength);
        }

        ValidateRequired(errors, nameof(name), name, Developer.NameMaxLength);
        ValidateOptional(errors, nameof(legalName), legalName, Developer.LegalNameMaxLength);
        ValidateOptional(
            errors,
            nameof(externalContactName),
            externalContactName,
            Developer.ContactNameMaxLength);
        ValidateOptional(
            errors,
            nameof(externalContactEmail),
            externalContactEmail,
            Developer.ContactEmailMaxLength);
        ValidateOptional(
            errors,
            nameof(externalContactPhone),
            externalContactPhone,
            Developer.ContactPhoneMaxLength);

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
