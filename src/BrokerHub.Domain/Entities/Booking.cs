namespace BrokerHub.Domain.Entities;

public sealed class Booking
{
    public const string PendingReview = "PendingReview", Rejected = "Rejected", ResponseTimeout = "response-timeout";
    public const string Confirmed = "Confirmed", Expired = "Expired";
    public const string Cancelled = "Cancelled";
    public const string Completed = "Completed";
    public const int CancellationReasonMaxLength = 1000;
    public const int RequestReasonMaxLength = 1000, ResponseWaitHours = 72;
    private Booking() { }
    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public Guid LeadId { get; private set; }
    public Guid UnitId { get; private set; }
    public Guid ProjectId { get; private set; }
    public string Status { get; private set; } = PendingReview;
    public DateTime RequestedAtUtc { get; private set; }
    public Guid RequestedByAccountId { get; private set; }
    public DateTime ResponseDueAtUtc { get; private set; }
    public Guid SubmissionAgreementId { get; private set; }
    public DateOnly SubmissionLocalDate { get; private set; }
    public string TimeZoneId { get; private set; } = "";
    public string? RequestReason { get; private set; }
    public long Version { get; private set; }
    public DateTime? RejectedAtUtc { get; private set; }
    public string? RejectionReason { get; private set; }
    public Guid? RejectedByAccountId { get; private set; }
    public string? RejectionActorKind { get; private set; }
    public bool IsActiveRequest { get; private set; }
    public DateTime? ConfirmedAtUtc { get; private set; }
    public Guid? ConfirmedByAccountId { get; private set; }
    public decimal? ConfirmedPrice { get; private set; }
    public string? ConfirmedCurrencyCode { get; private set; }
    public DateTime? ReservationExpiresAtUtc { get; private set; }
    public Guid? ConfirmedResponseId { get; private set; }
    public DateTime? ExpiredAtUtc { get; private set; }
    public DateTime? LastExtendedAtUtc { get; private set; }
    public Guid? LastExtensionId { get; private set; }
    public DateTime? CancelledAtUtc { get; private set; }
    public Guid? CancelledByAccountId { get; private set; }
    public string? CancellationReason { get; private set; }
    public string? CancelledFromStatus { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public Guid? CompletedByAccountId { get; private set; }
    public Guid? CompletedDealId { get; private set; }
    public void Complete(long version,Guid dealId,DateTime now,Guid actor)
    {
        if(Status!=Confirmed||Version!=version||Version==long.MaxValue||ReservationExpiresAtUtc is not { } deadline||now>=deadline)
            throw new InvalidOperationException("Current unexpired Confirmed Booking/version required for adopted sale.");
        if(dealId==Guid.Empty||actor==Guid.Empty||now==default||now.Kind!=DateTimeKind.Utc||
            now<RequestedAtUtc||now<ConfirmedAtUtc||now<LastExtendedAtUtc)
            throw new ArgumentException("Actual Manager/Deal identity and monotonic completion UTC required.");
        Status=Completed;Version++;IsActiveRequest=false;CompletedDealId=dealId;CompletedAtUtc=now;CompletedByAccountId=actor;
    }
    public void Cancel(long version, DateTime now, Guid actor, string? reason)
    {
        if (Status is not (PendingReview or Confirmed) || Version != version || Version == long.MaxValue ||
            (Status == PendingReview ? now >= ResponseDueAtUtc : ReservationExpiresAtUtc is not { } deadline || now >= deadline))
            throw new InvalidOperationException("Current cancellable booking/version before its cutoff required.");
        if (now == default || now.Kind != DateTimeKind.Utc || now < RequestedAtUtc || now < ConfirmedAtUtc || now < LastExtendedAtUtc ||
            actor == Guid.Empty || reason?.Trim().Length > CancellationReasonMaxLength) throw new ArgumentException("Actual owner/UTC and bounded reason required.");
        CancelledFromStatus = Status; Status = Cancelled; IsActiveRequest = false; Version++;
        CancelledAtUtc = now; CancelledByAccountId = actor; CancellationReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
    }
    public void Extend(long version, Guid extensionId, DateTime newDeadline, DateTime now)
    {
        if (Status != Confirmed || Version != version || Version == long.MaxValue || ReservationExpiresAtUtc is not { } oldDeadline || now >= oldDeadline)
            throw new InvalidOperationException("Current unexpired Confirmed booking/version required.");
        if (extensionId == Guid.Empty || now == default || now.Kind != DateTimeKind.Utc || now < ConfirmedAtUtc || now < LastExtendedAtUtc ||
            newDeadline.Kind != DateTimeKind.Utc || newDeadline <= oldDeadline)
            throw new ArgumentException("Actual recording time and a later external deadline required.");
        ReservationExpiresAtUtc = newDeadline; LastExtendedAtUtc = now; LastExtensionId = extensionId; Version++;
    }
    public bool IsResponseDue(DateTime observedUtc) => Status == PendingReview && observedUtc >= ResponseDueAtUtc;
    public bool IsReservationDue(DateTime observedUtc) => Status == Confirmed && ReservationExpiresAtUtc is { } due && observedUtc >= due;
    public bool IsDue(DateTime observedUtc) => IsResponseDue(observedUtc) || IsReservationDue(observedUtc);
    public string EffectiveStatus(DateTime observedUtc) => IsResponseDue(observedUtc) ? Rejected : IsReservationDue(observedUtc) ? Expired : Status;
    public string? EffectiveRejectionReason(DateTime observedUtc) => IsResponseDue(observedUtc) ? ResponseTimeout : RejectionReason;
    public bool TryTimeout(long version, DateTime now)
    {
        if (now == default || now.Kind != DateTimeKind.Utc) throw new ArgumentException("Explicit timeout observation UTC required.");
        if (Version != version || Version == long.MaxValue || !IsActiveRequest || !IsResponseDue(now)) return false;
        Status = Rejected; IsActiveRequest = false; Version++; RejectedAtUtc = now;
        RejectionReason = ResponseTimeout; RejectionActorKind = "System"; RejectedByAccountId = null; return true;
    }
    public bool TryEndIfDue(long version, DateTime now)
    {
        if (TryTimeout(version,now)) return true;
        if (Version != version || Version == long.MaxValue || !IsActiveRequest || !IsReservationDue(now)) return false;
        Status = Expired; IsActiveRequest = false; Version++; ExpiredAtUtc = now; return true;
    }
    public void Confirm(long version, Guid responseId, decimal price, string currency, DateTime deadline, DateTime now, Guid actor)
    {
        RequirePending(version,now,actor);
        if (responseId == Guid.Empty || !BookingDeveloperResponse.ValidMoney(price,currency) || deadline.Kind != DateTimeKind.Utc || deadline <= now)
            throw new ArgumentException("Fresh external confirmation price/currency/deadline required.");
        Status = Confirmed; Version++; ConfirmedAtUtc = now; ConfirmedByAccountId = actor;
        ConfirmedPrice = price; ConfirmedCurrencyCode = currency; ReservationExpiresAtUtc = deadline; ConfirmedResponseId = responseId;
    }
    public void Reject(long version, string reasonCode, DateTime now, Guid actor)
    {
        RequirePending(version,now,actor);
        if (reasonCode is not ("developer-rejected" or "invalid-reservation-deadline" or "local-conflict")) throw new ArgumentException("Known recorded rejection code required.");
        Status = Rejected; IsActiveRequest = false; Version++; RejectedAtUtc = now;
        RejectionReason = reasonCode; RejectionActorKind = "User"; RejectedByAccountId = actor;
    }
    private void RequirePending(long version, DateTime now, Guid actor)
    {
        if (Status != PendingReview || Version != version || Version == long.MaxValue || now >= ResponseDueAtUtc)
            throw new InvalidOperationException("Current unexpired pending booking/version required.");
        if (now == default || now.Kind != DateTimeKind.Utc || now < RequestedAtUtc || actor == Guid.Empty)
            throw new ArgumentException("Current UTC and actual recorder required.");
    }
    public static Booking Request(Guid id, Guid customer, Guid lead, Guid unit, Guid project, Guid agreement,
        DateOnly localDate, string zone, string? reason, DateTime now, Guid actor)
    {
        if (new[] { id, customer, lead, unit, project, agreement, actor }.Any(x => x == Guid.Empty))
            throw new ArgumentException("Booking identities required.");
        if (now == default || now.Kind != DateTimeKind.Utc || now > DateTime.MaxValue.AddHours(-ResponseWaitHours) ||
            localDate == default || string.IsNullOrWhiteSpace(zone) || zone.Trim().Length > 100 || reason?.Trim().Length > RequestReasonMaxLength)
            throw new ArgumentException("Current UTC, project local context and bounded reason required.");
        return new() { Id = id, CustomerId = customer, LeadId = lead, UnitId = unit, ProjectId = project,
            SubmissionAgreementId = agreement, SubmissionLocalDate = localDate, TimeZoneId = zone.Trim(),
            RequestedAtUtc = now, RequestedByAccountId = actor, ResponseDueAtUtc = now.AddHours(ResponseWaitHours),
            RequestReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(), Version = 1, IsActiveRequest = true };
    }
}
