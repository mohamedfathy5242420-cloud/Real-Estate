namespace BrokerHub.Domain.Entities;

public sealed class MarketingAgreementRevision
{
    private MarketingAgreementRevision() { }
    public MarketingAgreementRevision(Guid agreementId, long version, string action, string snapshotJson, DateTime at, Guid actor)
    {
        if (agreementId == Guid.Empty || actor == Guid.Empty || version <= 0 || at.Kind != DateTimeKind.Utc ||
            string.IsNullOrWhiteSpace(snapshotJson) || action is not ("Created" or "DraftUpdated" or "Activated"))
            throw new ArgumentException("Invalid agreement revision.");
        Id = Guid.NewGuid(); MarketingAgreementId = agreementId; AgreementVersion = version;
        Action = action; SnapshotJson = snapshotJson; OccurredAtUtc = at; ActorAccountId = actor;
    }
    public Guid Id { get; private set; }
    public Guid MarketingAgreementId { get; private set; }
    public long AgreementVersion { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string SnapshotJson { get; private set; } = string.Empty;
    public DateTime OccurredAtUtc { get; private set; }
    public Guid ActorAccountId { get; private set; }
}
