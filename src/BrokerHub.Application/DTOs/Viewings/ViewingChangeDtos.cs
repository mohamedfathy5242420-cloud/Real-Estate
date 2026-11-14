namespace BrokerHub.Application.DTOs.Viewings;

public sealed record CancelViewingDto(long Version, string Reason);
public sealed record ChangeViewingProposalDto(long Version, DateTimeOffset ProposedStartUtc, string Reason);
public sealed record CancelStaffViewingDto(long Version, string CancellationKind, string Reason);
