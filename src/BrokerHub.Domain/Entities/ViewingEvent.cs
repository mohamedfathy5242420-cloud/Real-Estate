using BrokerHub.Domain.ValueObjects;

namespace BrokerHub.Domain.Entities;

public sealed class ViewingEvent
{
    private ViewingEvent() { }
    public Guid Id { get; private set; }
    public Guid ViewingId { get; private set; }
    public Guid CustomerId { get; private set; }
    public long ViewingVersion { get; private set; }
    public string Kind { get; private set; } = "";
    public Guid? ActorAccountId { get; private set; }
    public string ActorKind { get; private set; } = "User";
    public DateTime? EffectiveCutoffUtc { get; private set; }
    public DateTime AtUtc { get; private set; }
    public DateTime ProposedStartUtc { get; private set; }
    public DateTime ProposedEndUtc { get; private set; }
    public DateTime? ConfirmedStartUtc { get; private set; }
    public DateTime? ConfirmedEndUtc { get; private set; }
    public Guid? ConfirmedEmployeeId { get; private set; }
    public DateTime? CoordinatedAtUtc { get; private set; }
    public string? CoordinationSource { get; private set; }
    public string? CoordinationReference { get; private set; }
    public Guid? OfferedEmployeeId { get; private set; }
    public DateTime? PreviousProposedStartUtc { get; private set; }
    public DateTime? PreviousProposedEndUtc { get; private set; }
    public string? Reason { get; private set; }
    public string? PreviousStatus { get; private set; }
    public DateTime? PreviousConfirmedStartUtc { get; private set; }
    public DateTime? PreviousConfirmedEndUtc { get; private set; }
    public Guid? PreviousConfirmedEmployeeId { get; private set; }
    public string? CancellationKind { get; private set; }
    public DateTime? OutcomeOccurredAtUtc { get; private set; }
    public Guid? OutcomeNoteId { get; private set; }
    public static ViewingEvent Outcome(Guid id, Viewing v)
    {
        if (id == Guid.Empty || v.Status is not (Viewing.Completed or Viewing.CustomerNoShow) || v.Version <= 2 ||
            v.OutcomeOccurredAtUtc is null || v.OutcomeRecordedAtUtc is null || v.OutcomeRecordedByAccountId is null || v.OutcomeNoteId is null)
            throw new ArgumentException("Recorded viewing outcome audit required.");
        return new() { Id = id, ViewingId = v.Id, CustomerId = v.CustomerId, ViewingVersion = v.Version,
            Kind = v.Status, ActorAccountId = v.OutcomeRecordedByAccountId, AtUtc = v.OutcomeRecordedAtUtc.Value,
            ProposedStartUtc = v.CurrentProposedStartUtc, ProposedEndUtc = v.CurrentProposedEndUtc,
            ConfirmedStartUtc = v.ConfirmedStartUtc, ConfirmedEndUtc = v.ConfirmedEndUtc, ConfirmedEmployeeId = v.ConfirmedEmployeeId,
            CoordinatedAtUtc = v.CoordinatedAtUtc, CoordinationSource = v.CoordinationSource, CoordinationReference = v.CoordinationReference,
            OfferedEmployeeId = v.OfferedEmployeeId, OutcomeOccurredAtUtc = v.OutcomeOccurredAtUtc, OutcomeNoteId = v.OutcomeNoteId };
    }
    public static ViewingEvent Expiry(Guid id, Viewing v)
    {
        if (id == Guid.Empty || v.Status != Viewing.Expired || v.Version <= 1 || v.ExpiredAtUtc is null ||
            v.ExpiredFromStatus is not (Viewing.PendingConfirmation or Viewing.AwaitingCustomerAcceptance))
            throw new ArgumentException("Persistable system expiry audit required.");
        return new() { Id = id, ViewingId = v.Id, CustomerId = v.CustomerId, ViewingVersion = v.Version,
            Kind = "Expired", ActorKind = "System", ActorAccountId = null, AtUtc = v.ExpiredAtUtc.Value,
            EffectiveCutoffUtc = DateTime.SpecifyKind(v.PendingExpiresAtUtc, DateTimeKind.Utc),
            PreviousStatus = v.ExpiredFromStatus, ProposedStartUtc = v.CurrentProposedStartUtc,
            ProposedEndUtc = v.CurrentProposedEndUtc, OfferedEmployeeId = v.OfferedEmployeeId,
            CoordinatedAtUtc = v.CoordinatedAtUtc, CoordinationSource = v.CoordinationSource,
            CoordinationReference = v.CoordinationReference,
            Reason = v.ExpiredFromStatus == Viewing.AwaitingCustomerAcceptance ? "AlternativeStart" : "CoordinationDeadline" };
    }
    public static ViewingEvent Rescheduled(Guid id, Viewing v, ViewingChangeSnapshot old, Guid actor, string reason)
    {
        if (id == Guid.Empty || actor == Guid.Empty || v.Status != Viewing.PendingConfirmation || v.Version <= 1 ||
            old.Status is not (Viewing.PendingConfirmation or Viewing.AwaitingCustomerAcceptance or Viewing.Confirmed) ||
            string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > Viewing.ChangeReasonMaxLength)
            throw new ArgumentException("Changed proposal and previous appointment audit required.");
        return new() { Id = id, ViewingId = v.Id, CustomerId = v.CustomerId, ViewingVersion = v.Version,
            Kind = "Rescheduled", ActorAccountId = actor, AtUtc = v.PendingCycleStartedAtUtc,
            ProposedStartUtc = v.CurrentProposedStartUtc, ProposedEndUtc = v.CurrentProposedEndUtc,
            PreviousStatus = old.Status, PreviousProposedStartUtc = old.ProposedStartUtc,
            PreviousProposedEndUtc = old.ProposedEndUtc, PreviousConfirmedStartUtc = old.ConfirmedStartUtc,
            PreviousConfirmedEndUtc = old.ConfirmedEndUtc, PreviousConfirmedEmployeeId = old.ConfirmedEmployeeId, Reason = reason.Trim() };
    }
    public static ViewingEvent Cancellation(Guid id, Viewing v)
    {
        if (id == Guid.Empty || v.Status != Viewing.Cancelled || v.Version <= 1 || v.CancelledAtUtc is null ||
            v.CancelledByAccountId is null || v.CancelledFromStatus is null || v.CancellationReason is null || v.CancellationKind is null)
            throw new ArgumentException("Cancelled viewing audit required.");
        return new() { Id = id, ViewingId = v.Id, CustomerId = v.CustomerId, ViewingVersion = v.Version,
            Kind = "Cancelled", ActorAccountId = v.CancelledByAccountId.Value, AtUtc = v.CancelledAtUtc.Value,
            ProposedStartUtc = v.CurrentProposedStartUtc, ProposedEndUtc = v.CurrentProposedEndUtc,
            ConfirmedStartUtc = v.ConfirmedStartUtc, ConfirmedEndUtc = v.ConfirmedEndUtc, ConfirmedEmployeeId = v.ConfirmedEmployeeId,
            CoordinatedAtUtc = v.CoordinatedAtUtc, CoordinationSource = v.CoordinationSource, CoordinationReference = v.CoordinationReference,
            OfferedEmployeeId = v.OfferedEmployeeId, PreviousStatus = v.CancelledFromStatus,
            Reason = v.CancellationReason, CancellationKind = v.CancellationKind };
    }
    public static ViewingEvent AlternativeOffered(Guid id, Viewing viewing, DateTime previousStart, DateTime previousEnd, string reason)
    {
        if (id == Guid.Empty || viewing.Status != Viewing.AwaitingCustomerAcceptance || viewing.Version <= 1 ||
            viewing.OfferedAtUtc is null || viewing.OfferedByAccountId is null || viewing.OfferedEmployeeId is null ||
            previousStart == default || previousEnd <= previousStart || string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > Viewing.AlternativeReasonMaxLength)
            throw new ArgumentException("Coordinated offer and old/new interval/reason audit required.");
        return new() { Id = id, ViewingId = viewing.Id, CustomerId = viewing.CustomerId, ViewingVersion = viewing.Version,
            Kind = "AlternativeOffered", ActorAccountId = viewing.OfferedByAccountId.Value, AtUtc = viewing.OfferedAtUtc.Value,
            ProposedStartUtc = viewing.CurrentProposedStartUtc, ProposedEndUtc = viewing.CurrentProposedEndUtc,
            OfferedEmployeeId = viewing.OfferedEmployeeId, PreviousProposedStartUtc = previousStart,
            PreviousProposedEndUtc = previousEnd, Reason = reason.Trim(), CoordinatedAtUtc = viewing.CoordinatedAtUtc,
            CoordinationSource = viewing.CoordinationSource, CoordinationReference = viewing.CoordinationReference };
    }
    public static ViewingEvent AlternativeAccepted(Guid id, Viewing viewing)
    {
        if (viewing.OfferedEmployeeId is null || viewing.OfferedEmployeeId != viewing.ConfirmedEmployeeId || viewing.Version < 3)
            throw new ArgumentException("Accepted coordinated alternative required.");
        var result = Confirmation(id, viewing); result.Kind = "AlternativeAccepted"; result.OfferedEmployeeId = viewing.OfferedEmployeeId; return result;
    }
    public static ViewingEvent Confirmation(Guid id, Viewing viewing)
    {
        if (id == Guid.Empty || viewing.Status != Viewing.Confirmed || viewing.Version <= 1 || viewing.ConfirmedAtUtc is null || viewing.ConfirmedByAccountId is null)
            throw new ArgumentException("Confirmed viewing audit required.");
        return new() { Id = id, ViewingId = viewing.Id, CustomerId = viewing.CustomerId, ViewingVersion = viewing.Version,
            Kind = "Confirmed", ActorAccountId = viewing.ConfirmedByAccountId.Value, AtUtc = viewing.ConfirmedAtUtc.Value,
            ProposedStartUtc = viewing.CurrentProposedStartUtc, ProposedEndUtc = viewing.CurrentProposedEndUtc,
            ConfirmedStartUtc = viewing.ConfirmedStartUtc, ConfirmedEndUtc = viewing.ConfirmedEndUtc,
            ConfirmedEmployeeId = viewing.ConfirmedEmployeeId, CoordinatedAtUtc = viewing.CoordinatedAtUtc,
            CoordinationSource = viewing.CoordinationSource, CoordinationReference = viewing.CoordinationReference };
    }
    public static ViewingEvent Requested(Guid id, Viewing viewing)
    {
        if (id == Guid.Empty || viewing.Version != 1 || viewing.Status != Viewing.PendingConfirmation)
            throw new ArgumentException("Initial viewing event required.");
        return new() { Id = id, ViewingId = viewing.Id, CustomerId = viewing.CustomerId, ViewingVersion = 1,
            Kind = "Requested", ActorAccountId = viewing.RequestedByAccountId, AtUtc = viewing.RequestedAtUtc,
            ProposedStartUtc = viewing.CurrentProposedStartUtc, ProposedEndUtc = viewing.CurrentProposedEndUtc };
    }
}
