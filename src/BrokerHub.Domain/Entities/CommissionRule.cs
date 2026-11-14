using BrokerHub.Domain.Enums;
using BrokerHub.Domain.ValueObjects;

namespace BrokerHub.Domain.Entities;

public sealed class CommissionRule
{
    private CommissionRule() { }
    internal CommissionRule(Guid agreementId, Guid developerId, CommissionRuleDefinition definition, DateTime at, Guid actor)
    {
        Id = Guid.NewGuid(); MarketingAgreementId = agreementId; DeveloperId = developerId;
        ProjectId = definition.ProjectId; CreatedAtUtc = at; CreatedByAccountId = actor; Version = 1;
        Apply(definition);
    }
    public Guid Id { get; private set; }
    public Guid MarketingAgreementId { get; private set; }
    public Guid DeveloperId { get; private set; }
    public Guid? ProjectId { get; private set; }
    public CommissionRuleType RuleType { get; private set; }
    public decimal? Percentage { get; private set; }
    public decimal? FixedAmount { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public bool IsArchived { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid CreatedByAccountId { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public Guid? UpdatedByAccountId { get; private set; }
    public long Version { get; private set; }
    internal void Update(CommissionRuleDefinition definition, DateTime at, Guid actor)
    {
        if (ProjectId != definition.ProjectId) throw new ArgumentException("Rule project is immutable.");
        Apply(definition); IsArchived = false; UpdatedAtUtc = at; UpdatedByAccountId = actor; Version++;
    }
    internal void Archive(DateTime at, Guid actor)
    {
        if (IsArchived) return;
        if (!ProjectId.HasValue) throw new InvalidOperationException("Default rule cannot be archived.");
        IsArchived = true; UpdatedAtUtc = at; UpdatedByAccountId = actor; Version++;
    }
    private void Apply(CommissionRuleDefinition definition)
    {
        definition.Validate(); RuleType = definition.RuleType; Percentage = definition.Percentage;
        FixedAmount = definition.FixedAmount; Currency = definition.Currency;
    }
}
