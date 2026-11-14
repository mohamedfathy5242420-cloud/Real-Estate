namespace BrokerHub.Domain.Entities;

public sealed class Lead
{
    public const string Open = "Open", Closed = "Closed";
    public const int SourceMaxLength = 100, ReasonMaxLength = 2000;
    private Lead() { }
    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid MarketingAgreementId { get; private set; }
    public long AgreementVersionAtOpening { get; private set; }
    public DateOnly AgreementStartsOn { get; private set; }
    public DateOnly AgreementEndsOn { get; private set; }
    public int ProtectionPeriodDays { get; private set; }
    public DateOnly ProtectionEndsOn { get; private set; }
    public DateOnly OpenedOnLocalDate { get; private set; }
    public string OpeningTimeZoneId { get; private set; } = "";
    public DateTime OpenedAtUtc { get; private set; }
    public Guid OpenedByAccountId { get; private set; }
    public string Source { get; private set; } = "";
    public string Status { get; private set; } = Open;
    public long Version { get; private set; }
    public DateTime? ClosedAtUtc { get; private set; }
    public Guid? ClosedByAccountId { get; private set; }
    public string? CloseReason { get; private set; }

    public static Lead Create(Guid id, Guid customer, Guid project, Guid agreement, long agreementVersion,
        DateOnly starts, DateOnly ends, int protection, DateOnly localDate, string zone, DateTime atUtc, Guid actor, string source)
    {
        if (id == Guid.Empty || customer == Guid.Empty || project == Guid.Empty || agreement == Guid.Empty || actor == Guid.Empty || agreementVersion <= 0)
            throw new ArgumentException("Lead origin identities/version required.");
        if (starts == default || ends < starts || localDate < starts || localDate > ends || protection < 0)
            throw new ArgumentException("Eligible original agreement period required.");
        var protectionEnd = ends.AddDays(protection); // explicit overflow failure, never truncate protection
        source = Text(source, SourceMaxLength); zone = Text(zone, 100); Utc(atUtc);
        return new() { Id = id, CustomerId = customer, ProjectId = project, MarketingAgreementId = agreement,
            AgreementVersionAtOpening = agreementVersion, AgreementStartsOn = starts, AgreementEndsOn = ends,
            ProtectionPeriodDays = protection, ProtectionEndsOn = protectionEnd, OpenedOnLocalDate = localDate,
            OpeningTimeZoneId = zone, OpenedAtUtc = atUtc, OpenedByAccountId = actor, Source = source, Version = 1 };
    }
    public void Close(long expectedVersion, string reason, DateTime atUtc, Guid actor)
    {
        if (Version != expectedVersion || Version == long.MaxValue || Status != Open)
            throw new InvalidOperationException("Lead state/version changed.");
        reason = Text(reason, ReasonMaxLength); Utc(atUtc);
        if (actor == Guid.Empty || atUtc < OpenedAtUtc) throw new ArgumentException("Valid closing audit required.");
        Status = Closed; Version++; ClosedAtUtc = atUtc; ClosedByAccountId = actor; CloseReason = reason;
    }
    // Date-only original-agreement eligibility component; future adoption must also guard actual
    // external sale evidence/current role/Unit/state/currency. This alone never adopts a commission.
    public bool CoversSaleDate(DateOnly saleDate) => OpenedOnLocalDate >= AgreementStartsOn &&
        OpenedOnLocalDate <= AgreementEndsOn && saleDate >= OpenedOnLocalDate && saleDate <= ProtectionEndsOn;
    private static string Text(string? value, int max) => !string.IsNullOrWhiteSpace(value) && value.Trim().Length <= max
        ? value.Trim() : throw new ArgumentException("Nonblank bounded Lead text required.");
    private static void Utc(DateTime value)
    { if (value == default || value.Kind != DateTimeKind.Utc) throw new ArgumentException("Explicit server UTC required."); }
}
