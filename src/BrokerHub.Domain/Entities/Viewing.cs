using BrokerHub.Domain.ValueObjects;

namespace BrokerHub.Domain.Entities;

public sealed class Viewing
{
    public const string PendingConfirmation = "PendingConfirmation", Confirmed = "Confirmed", AwaitingCustomerAcceptance = "AwaitingCustomerAcceptance";
    public const string Cancelled = "Cancelled";
    public const string Expired = "Expired";
    public const string Completed = "Completed", CustomerNoShow = "CustomerNoShow";
    public const int ChangeReasonMaxLength = 1000;
    public const int AlternativeReasonMaxLength = 1000;
    public const int CoordinationSourceMaxLength = 200, CoordinationReferenceMaxLength = 500;
    public const int DefaultDurationMinutes = 60, ProposalWindowDays = 30;
    private Viewing() { }
    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public Guid LeadId { get; private set; }
    public Guid UnitId { get; private set; }
    public Guid ProjectId { get; private set; }
    public string Status { get; private set; } = PendingConfirmation;
    public DateTime CurrentProposedStartUtc { get; private set; }
    public DateTime CurrentProposedEndUtc { get; private set; }
    public string TimeZoneId { get; private set; } = "";
    public DateTime RequestedAtUtc { get; private set; }
    public DateTime PendingCycleStartedAtUtc { get; private set; }
    public Guid RequestedByAccountId { get; private set; }
    public Guid SubmissionAgreementId { get; private set; }
    public DateOnly SubmissionLocalDate { get; private set; }
    public long Version { get; private set; }
    public DateTime? ConfirmedStartUtc { get; private set; }
    public DateTime? ConfirmedEndUtc { get; private set; }
    public Guid? ConfirmedEmployeeId { get; private set; }
    public DateTime? ConfirmedAtUtc { get; private set; }
    public Guid? ConfirmedByAccountId { get; private set; }
    public DateTime? CoordinatedAtUtc { get; private set; }
    public string? CoordinationSource { get; private set; }
    public string? CoordinationReference { get; private set; }
    public DateTime? OfferedAtUtc { get; private set; }
    public Guid? OfferedByAccountId { get; private set; }
    public Guid? OfferedEmployeeId { get; private set; }
    public DateTime? CancelledAtUtc { get; private set; }
    public Guid? CancelledByAccountId { get; private set; }
    public string? CancellationKind { get; private set; }
    public string? CancellationReason { get; private set; }
    public string? CancelledFromStatus { get; private set; }
    public DateTime? ExpiredAtUtc { get; private set; }
    public string? ExpiredFromStatus { get; private set; }
    public DateTime? OutcomeOccurredAtUtc { get; private set; }
    public DateTime? OutcomeRecordedAtUtc { get; private set; }
    public Guid? OutcomeRecordedByAccountId { get; private set; }
    public Guid? OutcomeNoteId { get; private set; }
    public void RecordOutcome(long version, string outcome, DateTime occurredAt, DateTime now, Guid actor, Guid noteId)
    {
        if (Status != Confirmed || Version != version || Version == long.MaxValue || ConfirmedStartUtc is null ||
            occurredAt < ConfirmedStartUtc.Value || occurredAt > now)
            throw new InvalidOperationException("Current confirmed viewing/version and actual occurrence between start and final now required.");
        if (outcome is not (Completed or CustomerNoShow) || actor == Guid.Empty || noteId == Guid.Empty ||
            occurredAt == default || occurredAt.Kind != DateTimeKind.Utc || now == default || now.Kind != DateTimeKind.Utc)
            throw new ArgumentException("Explicit factual outcome UTC and recorder/note required.");
        Status = outcome; Version++; OutcomeOccurredAtUtc = occurredAt; OutcomeRecordedAtUtc = now;
        OutcomeRecordedByAccountId = actor; OutcomeNoteId = noteId;
    }
    public DateTime PendingExpiresAtUtc => OfferedAtUtc.HasValue ? CurrentProposedStartUtc :
        CurrentProposedStartUtc < PendingCycleStartedAtUtc.AddDays(7) ? CurrentProposedStartUtc : PendingCycleStartedAtUtc.AddDays(7);
    public string EffectiveStatus(DateTime observedUtc) => (Status is PendingConfirmation or AwaitingCustomerAcceptance) &&
        observedUtc >= PendingExpiresAtUtc ? Expired : Status;
    public bool TryExpire(long version, DateTime now)
    {
        if (now == default || now.Kind != DateTimeKind.Utc) throw new ArgumentException("Explicit expiry observation UTC required.");
        if (Version != version || Version == long.MaxValue || Status is not (PendingConfirmation or AwaitingCustomerAcceptance) || now < PendingExpiresAtUtc) return false;
        ExpiredFromStatus = Status; ExpiredAtUtc = now; Status = Expired; Version++; return true;
    }

    public bool CanChange(long version, DateTime now) => Version == version && Version != long.MaxValue &&
        (Status is PendingConfirmation or AwaitingCustomerAcceptance ? now < PendingExpiresAtUtc :
            Status == Confirmed && ConfirmedStartUtc.HasValue && now < ConfirmedStartUtc.Value);
    public void Cancel(long version, string kind, string reason, DateTime now, Guid actor)
    {
        ValidateChange(version, reason, now, actor);
        if (kind is not ("Customer" or "Broker" or "Developer")) throw new ArgumentException("Cancellation kind required.");
        CancelledFromStatus = Status; Status = Cancelled; Version++; CancelledAtUtc = now;
        CancelledByAccountId = actor; CancellationKind = kind; CancellationReason = reason.Trim();
    }
    public ViewingChangeSnapshot ChangeProposal(long version, DateTime start, string reason, DateTime now, Guid actor)
    {
        ValidateChange(version, reason, now, actor);
        if (start.Kind != DateTimeKind.Utc || start <= now || start > now.AddDays(ProposalWindowDays))
            throw new ArgumentException("Future UTC proposal within thirty days required.");
        var previous = new ViewingChangeSnapshot(Status, CurrentProposedStartUtc, CurrentProposedEndUtc,
            ConfirmedStartUtc, ConfirmedEndUtc, ConfirmedEmployeeId);
        Status = PendingConfirmation; Version++; PendingCycleStartedAtUtc = now;
        CurrentProposedStartUtc = start; CurrentProposedEndUtc = start.AddMinutes(DefaultDurationMinutes);
        ConfirmedStartUtc = null; ConfirmedEndUtc = null; ConfirmedEmployeeId = null;
        ConfirmedAtUtc = null; ConfirmedByAccountId = null;
        CoordinatedAtUtc = null; CoordinationSource = null; CoordinationReference = null;
        OfferedAtUtc = null; OfferedByAccountId = null; OfferedEmployeeId = null;
        return previous;
    }
    private void ValidateChange(long version, string reason, DateTime now, Guid actor)
    {
        if (!CanChange(version, now)) throw new InvalidOperationException("Viewing state, version or current cutoff changed.");
        if (actor == Guid.Empty || now == default || now.Kind != DateTimeKind.Utc || now < PendingCycleStartedAtUtc ||
            string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > ChangeReasonMaxLength)
            throw new ArgumentException("Bounded change reason and current UTC actor audit required.");
    }

    public void Confirm(long version, Guid employee, DateTime start, DateTime end, DateTime coordinatedAt,
        string source, string? reference, DateTime now, Guid actor)
    {
        if (Status != PendingConfirmation || Version != version || Version == long.MaxValue || now >= PendingExpiresAtUtc)
            throw new InvalidOperationException("Viewing state, version or pending cutoff changed.");
        ApplyConfirmation(employee, start, end, coordinatedAt, source, reference, now, actor);
    }
    public (DateTime StartUtc, DateTime EndUtc) OfferAlternative(long version, Guid employee, DateTime start,
        DateTime end, DateTime coordinatedAt, string source, string? reference, DateTime now, Guid actor)
    {
        if (Status is not (PendingConfirmation or AwaitingCustomerAcceptance) || Version != version || Version == long.MaxValue || now >= PendingExpiresAtUtc)
            throw new InvalidOperationException("Current pending viewing and version required.");
        if (employee == Guid.Empty || actor == Guid.Empty || now == default || now.Kind != DateTimeKind.Utc ||
            start.Kind != DateTimeKind.Utc || end.Kind != DateTimeKind.Utc || start <= now || start > now.AddDays(ProposalWindowDays) || end <= start)
            throw new ArgumentException("Future coordinated UTC alternative within thirty days required.");
        ValidateCoordination(coordinatedAt, source, reference, now);
        var previous = (CurrentProposedStartUtc, CurrentProposedEndUtc);
        Status = AwaitingCustomerAcceptance; Version++; CurrentProposedStartUtc = start; CurrentProposedEndUtc = end;
        OfferedAtUtc = now; OfferedByAccountId = actor; OfferedEmployeeId = employee;
        CoordinatedAtUtc = coordinatedAt; CoordinationSource = source.Trim();
        CoordinationReference = string.IsNullOrWhiteSpace(reference) ? null : reference.Trim();
        return previous;
    }
    public void AcceptAlternative(long version, Guid employee, DateTime now, Guid actor)
    {
        if (Status != AwaitingCustomerAcceptance || Version != version || Version == long.MaxValue ||
            now >= PendingExpiresAtUtc || OfferedEmployeeId != employee || OfferedAtUtc is null ||
            CoordinatedAtUtc is null || CoordinationSource is null)
            throw new InvalidOperationException("Current alternative, responsible Employee and version required.");
        // Stored feature timestamps are UTC instants; restore their explicit Kind for the command.
        ApplyConfirmation(employee, DateTime.SpecifyKind(CurrentProposedStartUtc, DateTimeKind.Utc),
            DateTime.SpecifyKind(CurrentProposedEndUtc, DateTimeKind.Utc), DateTime.SpecifyKind(CoordinatedAtUtc.Value, DateTimeKind.Utc),
            CoordinationSource, CoordinationReference, now, actor);
    }
    private void ApplyConfirmation(Guid employee, DateTime start, DateTime end, DateTime coordinatedAt,
        string source, string? reference, DateTime now, Guid actor)
    {
        if (employee == Guid.Empty || actor == Guid.Empty || now == default || now.Kind != DateTimeKind.Utc ||
            start.Kind != DateTimeKind.Utc || end.Kind != DateTimeKind.Utc || coordinatedAt.Kind != DateTimeKind.Utc ||
            start != CurrentProposedStartUtc || start <= now || end <= start)
            throw new ArgumentException("Current coordinated UTC interval and audit required.");
        ValidateCoordination(coordinatedAt, source, reference, now);
        Status = Confirmed; Version++; ConfirmedStartUtc = start; ConfirmedEndUtc = end; ConfirmedEmployeeId = employee;
        ConfirmedAtUtc = now; ConfirmedByAccountId = actor; CoordinatedAtUtc = coordinatedAt;
        CoordinationSource = source.Trim(); CoordinationReference = string.IsNullOrWhiteSpace(reference) ? null : reference.Trim();
    }
    private void ValidateCoordination(DateTime coordinatedAt, string source, string? reference, DateTime now)
    {
        if (coordinatedAt == default || coordinatedAt.Kind != DateTimeKind.Utc || coordinatedAt < PendingCycleStartedAtUtc || coordinatedAt > now)
            throw new ArgumentException("External coordination must belong to this cycle and not be in the future.");
        if (string.IsNullOrWhiteSpace(source) || source.Trim().Length > CoordinationSourceMaxLength || reference?.Trim().Length > CoordinationReferenceMaxLength)
            throw new ArgumentException("Bounded external coordination source/reference required.");
    }

    public static Viewing Request(Guid id, Guid customer, Guid lead, Guid unit, Guid project, Guid agreement,
        DateOnly localDate, string zone, DateTime startUtc, DateTime atUtc, Guid actor)
    {
        if (new[] { id, customer, lead, unit, project, agreement, actor }.Any(x => x == Guid.Empty))
            throw new ArgumentException("Viewing identities required.");
        if (atUtc == default || atUtc.Kind != DateTimeKind.Utc || startUtc.Kind != DateTimeKind.Utc ||
            startUtc <= atUtc || startUtc > atUtc.AddDays(ProposalWindowDays))
            throw new ArgumentException("Future UTC proposal within thirty days required.");
        if (localDate == default || string.IsNullOrWhiteSpace(zone) || zone.Trim().Length > 100)
            throw new ArgumentException("Project local date and time zone required.");
        return new() { Id = id, CustomerId = customer, LeadId = lead, UnitId = unit, ProjectId = project,
            SubmissionAgreementId = agreement, SubmissionLocalDate = localDate, TimeZoneId = zone.Trim(),
            CurrentProposedStartUtc = startUtc, CurrentProposedEndUtc = startUtc.AddMinutes(DefaultDurationMinutes),
            RequestedAtUtc = atUtc, PendingCycleStartedAtUtc = atUtc, RequestedByAccountId = actor, Version = 1 };
    }
}
