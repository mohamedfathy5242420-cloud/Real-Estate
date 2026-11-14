namespace BrokerHub.Application.DTOs.Deals;

// Current read input for the existing Manager sale command, not catalog administration.
public sealed record DealPreparationUnitDto(Guid Id, Guid ProjectId, string Code, string Title,
    string PublicationStatus, string AvailabilityStatus, bool HasAdoptedSale, long Version);
