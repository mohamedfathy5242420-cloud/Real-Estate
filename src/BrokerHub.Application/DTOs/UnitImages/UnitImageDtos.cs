namespace BrokerHub.Application.DTOs.UnitImages;

public sealed record UnitImageDto(Guid Id, Guid UnitId, string? AltText, int SortOrder, bool IsPrimary,
    DateTime CreatedAtUtc, Guid CreatedByAccountId, DateTime? UpdatedAtUtc, Guid? UpdatedByAccountId);
public sealed record UnitImageCollectionDto(Guid UnitId, long UnitVersion, IReadOnlyList<UnitImageDto> Images);
public sealed record UnitImageMutationDto(long UnitVersion, UnitImageDto Image);
public sealed record UploadUnitImageDto(Stream Content, string? AltText, int SortOrder, long Version);
public sealed record UpdateUnitImageDto(string? AltText, int SortOrder, long Version);
// Internal query contract: never serialize this storage key to an HTTP response.
public sealed record UnitImageFileReference(string StorageKey, UnitImageMutationDto Metadata);
public sealed record UnitImageContent(Stream Stream, string ContentType);
