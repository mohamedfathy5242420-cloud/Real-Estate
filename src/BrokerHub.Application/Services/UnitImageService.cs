using BrokerHub.Application.Abstractions.Identity;
using BrokerHub.Application.Authorization;
using BrokerHub.Application.DTOs.UnitImages;
using BrokerHub.Application.Exceptions;
using BrokerHub.Application.Interfaces.Persistence;
using BrokerHub.Application.Interfaces.Services;
using BrokerHub.Application.Interfaces.Storage;
using BrokerHub.Application.Mappings;
using BrokerHub.Domain.Entities;

namespace BrokerHub.Application.Services;

public sealed class UnitImageService(IUnitRepository units, IUnitImageRepository images,
    IUnitImageQueryService queries, IUnitImageStorage storage, IUnitOfWork unitOfWork,
    ICurrentUser user, TimeProvider clock) : IUnitImageService
{
    private bool Authorized => user.IsAuthenticated && user.AccountId.HasValue && user.IsInRole(AppRoles.SystemAdministrator);
    private static UnitImageResult<T> Failure<T>(UnitImageStatus status, string field, string message) => UnitImageResult<T>.Failure(status, field, message);
    private static UnitImageResult<T> Denied<T>() => Failure<T>(UnitImageStatus.Forbidden, "Authorization", "SystemAdministrator permission is required.");
    private static UnitImageResult<T> Unavailable<T>() => Failure<T>(UnitImageStatus.Unavailable, "Image", "Image operation could not be completed. Reload before retrying.");
    private static UnitImageResult<T>? Validate<T>(string? altText, int sortOrder, long version)
    {
        if (version <= 0) return Failure<T>(UnitImageStatus.ValidationFailed, "Version", "Version must be positive.");
        try { UnitImage.ValidateMetadata(altText, sortOrder); return null; }
        catch (ArgumentException e) { return Failure<T>(UnitImageStatus.ValidationFailed, e.ParamName ?? "Metadata", e.Message); }
    }

    public async Task<UnitImageResult<UnitImageMutationDto>> UploadAsync(Guid unitId, UploadUnitImageDto request, CancellationToken cancellationToken)
    {
        if (!Authorized) return Denied<UnitImageMutationDto>();
        var invalid = Validate<UnitImageMutationDto>(request.AltText, request.SortOrder, request.Version);
        if (invalid is not null) return invalid;
        var unit = await units.GetTrackedByIdAsync(unitId, cancellationToken);
        if (unit is null) return Failure<UnitImageMutationDto>(UnitImageStatus.NotFound, "Unit", "Unit was not found.");
        if (unit.Version != request.Version || unit.Version == long.MaxValue)
            return Failure<UnitImageMutationDto>(UnitImageStatus.Conflict, "Version", "Unit collection version is stale or exhausted.");
        StoredUnitImage? stored = null;
        UnitImage? image = null;
        var committed = false;
        UnitImageResult<UnitImageMutationDto>? outcome = null;
        try
        {
            stored = await storage.StoreValidatedAsync(request.Content, cancellationToken);
            var now = clock.GetUtcNow().UtcDateTime;
            image = UnitImage.Create(Guid.NewGuid(), unitId, stored.Key, request.AltText, request.SortOrder, now, user.AccountId!.Value);
            unit.RecordImageChange(request.Version, now, user.AccountId.Value);
            await images.AddAsync(image, cancellationToken);
            // One relational save: the image and root collection Version/audit either both commit or neither does.
            await unitOfWork.SaveChangesAsync(cancellationToken);
            committed = true;
            outcome = UnitImageResult<UnitImageMutationDto>.Success(new(unit.Version, image.ToDto()));
        }
        catch (InvalidImageException e) { outcome = Failure<UnitImageMutationDto>(UnitImageStatus.ValidationFailed, "File", e.Message); }
        catch (Exception e) when (e is not OutOfMemoryException)
        {
            if (image is not null && stored is not null)
            {
                // Resolve a possible commit despite a lost acknowledgement, using an untracked persisted query.
                using var resolution = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                try
                {
                    var persisted = await queries.GetImageAsync(unitId, image.Id, resolution.Token);
                    if (persisted?.StorageKey == stored.Key)
                    {
                        committed = true;
                        outcome = UnitImageResult<UnitImageMutationDto>.Success(persisted.Metadata);
                    }
                }
                catch (Exception resolutionError) when (resolutionError is not OutOfMemoryException) { /* Retain on unknown outcome below. */ }
            }
            if (!committed)
            {
                if (e is OperationCanceledException) throw;
                outcome = e is OptimisticConcurrencyException
                    ? Failure<UnitImageMutationDto>(UnitImageStatus.Conflict, "Version", "Unit collection changed concurrently. Reload and retry.")
                    : Unavailable<UnitImageMutationDto>();
            }
        }
        finally
        {
            if (stored is not null && !committed)
            {
                using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                // Never delete on unknown SQL outcome; only a fresh confirmed absence authorizes compensation.
                var referenced = await images.IsStorageKeyReferencedAsync(stored.Key, cleanup.Token);
                if (referenced == false)
                {
                    if (!await storage.DiscardCreatedAsync(stored, cleanup.Token))
                        outcome = Unavailable<UnitImageMutationDto>();
                }
                else outcome = Unavailable<UnitImageMutationDto>();
            }
        }
        return outcome!;
    }

    public async Task<UnitImageResult<UnitImageCollectionDto>> ListAsync(Guid unitId, CancellationToken cancellationToken)
    {
        if (!Authorized) return Denied<UnitImageCollectionDto>();
        var collection = await queries.ListAsync(unitId, cancellationToken);
        return collection is null ? Failure<UnitImageCollectionDto>(UnitImageStatus.NotFound, "Unit", "Unit was not found.")
            : UnitImageResult<UnitImageCollectionDto>.Success(collection);
    }

    public async Task<UnitImageResult<UnitImageMutationDto>> UpdateAsync(Guid unitId, Guid imageId, UpdateUnitImageDto request, CancellationToken cancellationToken)
    {
        if (!Authorized) return Denied<UnitImageMutationDto>();
        var invalid = Validate<UnitImageMutationDto>(request.AltText, request.SortOrder, request.Version);
        if (invalid is not null) return invalid;
        var unit = await units.GetTrackedByIdAsync(unitId, cancellationToken);
        if (unit is null) return Failure<UnitImageMutationDto>(UnitImageStatus.NotFound, "Unit", "Unit was not found.");
        var image = await images.GetTrackedAsync(unitId, imageId, cancellationToken);
        if (image is null) return Failure<UnitImageMutationDto>(UnitImageStatus.NotFound, "Image", "Unit image was not found.");
        if (unit.Version != request.Version || unit.Version == long.MaxValue)
            return Failure<UnitImageMutationDto>(UnitImageStatus.Conflict, "Version", "Unit collection version is stale or exhausted.");
        var now = clock.GetUtcNow().UtcDateTime;
        image.UpdateMetadata(request.AltText, request.SortOrder, now, user.AccountId!.Value);
        unit.RecordImageChange(request.Version, now, user.AccountId.Value);
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return UnitImageResult<UnitImageMutationDto>.Success(new(unit.Version, image.ToDto()));
        }
        catch (OptimisticConcurrencyException) { return Failure<UnitImageMutationDto>(UnitImageStatus.Conflict, "Version", "Unit collection changed concurrently. Reload and retry."); }
        catch (Exception e) when (e is not (OperationCanceledException or OutOfMemoryException))
        { return Unavailable<UnitImageMutationDto>(); }
    }

    public async Task<UnitImageResult<UnitImageContent>> ReadAsync(Guid unitId, Guid imageId, CancellationToken cancellationToken)
    {
        if (!Authorized) return Denied<UnitImageContent>();
        var image = await queries.GetImageAsync(unitId, imageId, cancellationToken);
        if (image is null) return Failure<UnitImageContent>(UnitImageStatus.NotFound, "Image", "Unit image was not found.");
        try
        {
            var content = await storage.OpenReadAsync(image.StorageKey, cancellationToken);
            return content is null ? Failure<UnitImageContent>(UnitImageStatus.NotFound, "File", "Stored image was not found.")
                : UnitImageResult<UnitImageContent>.Success(content);
        }
        catch (IOException) { return Unavailable<UnitImageContent>(); }
        catch (UnauthorizedAccessException) { return Unavailable<UnitImageContent>(); }
    }
}
