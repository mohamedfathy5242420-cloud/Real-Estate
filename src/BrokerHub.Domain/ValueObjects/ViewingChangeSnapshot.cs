namespace BrokerHub.Domain.ValueObjects;

public sealed record ViewingChangeSnapshot(string Status, DateTime ProposedStartUtc, DateTime ProposedEndUtc,
    DateTime? ConfirmedStartUtc, DateTime? ConfirmedEndUtc, Guid? ConfirmedEmployeeId);
