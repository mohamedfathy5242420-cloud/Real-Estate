namespace BrokerHub.Domain.Entities;

public sealed class UnitPublicationTransition
{
    public const int ReasonMaxLength = 500;
    private UnitPublicationTransition() { }
    public Guid Id { get; private set; }
    public Guid UnitId { get; private set; }
    public string FromStatus { get; private set; } = string.Empty;
    public string ToStatus { get; private set; } = string.Empty;
    public long UnitVersion { get; private set; }
    public DateTime RecordedAtUtc { get; private set; }
    public Guid RecordedByAccountId { get; private set; }
    public string? Reason { get; private set; }
    public Guid? MarketingAgreementId { get; private set; }
    public DateOnly? EligibilityDate { get; private set; }
    public string? EligibilityTimeZoneId { get; private set; }

    internal static UnitPublicationTransition Create(Unit unit, bool publish, long version,
        DateTime at, Guid actor, string? reason, Guid? agreementId, DateOnly? date, string? zone)
    {
        if (at.Kind != DateTimeKind.Utc || actor == Guid.Empty)
            throw new ArgumentException("UTC time and current account actor required.");
        if (reason?.Trim().Length > ReasonMaxLength) throw new ArgumentException("Reason exceeds 500 characters.", nameof(reason));
        if (publish && (!agreementId.HasValue || date is null || string.IsNullOrWhiteSpace(zone)))
            throw new ArgumentException("Publication eligibility evidence required.");
        return new UnitPublicationTransition { Id = Guid.NewGuid(), UnitId = unit.Id,
            FromStatus = unit.PublicationStatus, ToStatus = publish ? Unit.Published : Unit.Unpublished,
            UnitVersion = version, RecordedAtUtc = at, RecordedByAccountId = actor,
            Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
            MarketingAgreementId = agreementId, EligibilityDate = date, EligibilityTimeZoneId = zone };
    }
}
