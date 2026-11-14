namespace BrokerHub.Application.DTOs.Viewings;
public sealed record RecordViewingOutcomeDto(long Version, string Outcome, DateTimeOffset OccurredAtUtc, string Notes);
