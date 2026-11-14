namespace BrokerHub.Domain.Entities;

public sealed class AgreementProject
{
    private AgreementProject() { }
    internal AgreementProject(Guid agreementId, Guid developerId, Guid projectId, DateTime at, Guid actor)
    {
        MarketingAgreementId = agreementId; DeveloperId = developerId; ProjectId = projectId;
        AddedAtUtc = at; AddedByAccountId = actor; IsIncluded = true;
    }
    public Guid MarketingAgreementId { get; private set; }
    public Guid DeveloperId { get; private set; }
    public Guid ProjectId { get; private set; }
    public DateTime AddedAtUtc { get; private set; }
    public Guid AddedByAccountId { get; private set; }
    public bool IsIncluded { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public Guid? UpdatedByAccountId { get; private set; }
    internal void SetIncluded(bool included, DateTime at, Guid actor)
    {
        if (included == IsIncluded) return;
        IsIncluded = included; UpdatedAtUtc = at; UpdatedByAccountId = actor;
    }
}
