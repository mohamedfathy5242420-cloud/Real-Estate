namespace BrokerHub.Application.DTOs.Viewings;
public sealed record OfferViewingAlternativeDto(long Version, DateTimeOffset ProposedStartUtc, DateTimeOffset ProposedEndUtc,
    DateTimeOffset CoordinatedAtUtc, string CoordinationSource, string? CoordinationReference, string Reason);
public sealed record AcceptViewingAlternativeDto(long Version);
