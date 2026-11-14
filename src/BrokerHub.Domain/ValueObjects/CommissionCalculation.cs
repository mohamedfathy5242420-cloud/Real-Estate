using BrokerHub.Domain.Entities;
using BrokerHub.Domain.Enums;

namespace BrokerHub.Domain.ValueObjects;

// A calculated value only. Adoption/authority/UTC evidence/SQL concurrency belong to the caller.
public sealed class CommissionCalculation
{
    public Guid LeadId { get; }
    public long LeadVersion { get; }
    public Guid CustomerId { get; }
    public Guid ProjectId { get; }
    public Guid DeveloperId { get; }
    public Guid AgreementId { get; }
    public long AgreementVersion { get; }
    public string AgreementNumber { get; }
    public AgreementScopeType AgreementScope { get; }
    public DateOnly AgreementStartsOn { get; }
    public DateOnly AgreementEndsOn { get; }
    public int ProtectionPeriodDays { get; }
    public DateOnly ProtectionEndsOn { get; }
    public DateOnly LeadOpenedOnLocalDate { get; }
    public string OpeningTimeZoneId { get; }
    public Guid RuleId { get; }
    public long RuleVersion { get; }
    public Guid? RuleProjectId { get; }
    public CommissionRuleType RuleType { get; }
    public decimal? Percentage { get; }
    public decimal? FixedAmount { get; }
    public string CurrencyCode { get; }
    public DateOnly SaleLocalDate { get; }
    public decimal FinalUnitPrice { get; }
    public decimal Amount { get; }

    private CommissionCalculation(Lead lead,MarketingAgreement agreement,CommissionRule rule,
        DateOnly saleLocalDate,decimal finalUnitPrice,decimal amount)
    {
        LeadId=lead.Id;LeadVersion=lead.Version;CustomerId=lead.CustomerId;ProjectId=lead.ProjectId;
        DeveloperId=agreement.DeveloperId;AgreementId=agreement.Id;AgreementVersion=agreement.Version;
        AgreementNumber=agreement.AgreementNumber;AgreementScope=agreement.ScopeType;
        AgreementStartsOn=lead.AgreementStartsOn;AgreementEndsOn=lead.AgreementEndsOn;
        ProtectionPeriodDays=lead.ProtectionPeriodDays;ProtectionEndsOn=lead.ProtectionEndsOn;
        LeadOpenedOnLocalDate=lead.OpenedOnLocalDate;OpeningTimeZoneId=lead.OpeningTimeZoneId;
        RuleId=rule.Id;RuleVersion=rule.Version;RuleProjectId=rule.ProjectId;RuleType=rule.RuleType;
        Percentage=rule.Percentage;FixedAmount=rule.FixedAmount;CurrencyCode=rule.Currency;
        SaleLocalDate=saleLocalDate;FinalUnitPrice=finalUnitPrice;Amount=amount;
    }

    public static CommissionCalculation Calculate(Lead lead,MarketingAgreement agreement,
        Guid projectId,Guid developerId,DateOnly saleLocalDate,decimal finalUnitPrice,string currencyCode)
    {
        ArgumentNullException.ThrowIfNull(lead);ArgumentNullException.ThrowIfNull(agreement);
        if(projectId==Guid.Empty||developerId==Guid.Empty||saleLocalDate==default||
            finalUnitPrice<=0m||finalUnitPrice>9999999999999999.99m||decimal.Round(finalUnitPrice,2)!=finalUnitPrice||
            currencyCode is null||currencyCode.Length!=3||currencyCode.Any(c=>c is < 'A' or > 'Z'))
            throw new ArgumentException("Exact positive final Unit price, canonical currency and local sale date required.");
        if(lead.Status is not (Lead.Open or Lead.Closed)||lead.ProjectId!=projectId||agreement.DeveloperId!=developerId||
            agreement.Id!=lead.MarketingAgreementId||agreement.Status!="Active"||
            agreement.Version!=lead.AgreementVersionAtOpening||agreement.StartsOn!=lead.AgreementStartsOn||
            agreement.EndsOn!=lead.AgreementEndsOn||agreement.ProtectionPeriodDays!=lead.ProtectionPeriodDays||
            agreement.EndsOn.AddDays(agreement.ProtectionPeriodDays)!=lead.ProtectionEndsOn||
            !lead.CoversSaleDate(saleLocalDate))
            throw new InvalidOperationException("The original Lead and original agreement snapshot do not qualify for this sale date.");
        var covered=agreement.ScopeType==AgreementScopeType.AllDeveloperProjects||
            agreement.ScopeType==AgreementScopeType.SelectedProjects&&agreement.Projects.Any(p=>p.IsIncluded&&
                p.ProjectId==projectId&&p.MarketingAgreementId==agreement.Id&&p.DeveloperId==developerId);
        if(!covered)throw new InvalidOperationException("The original agreement does not cover this project.");
        var active=agreement.Rules.Where(r=>!r.IsArchived).ToArray();
        var defaults=active.Where(r=>r.ProjectId is null).ToArray();
        var overrides=active.Where(r=>r.ProjectId==projectId).ToArray();
        if(defaults.Length!=1||overrides.Length>1)
            throw new InvalidOperationException("Exactly one Default and at most one project override required.");
        var rule=overrides.SingleOrDefault()??defaults[0];
        if(rule.Id==Guid.Empty||rule.Version<=0||rule.MarketingAgreementId!=agreement.Id||rule.DeveloperId!=developerId)
            throw new InvalidOperationException("Selected rule identity or parent is invalid.");
        new CommissionRuleDefinition(rule.ProjectId,rule.RuleType,rule.Percentage,rule.FixedAmount,rule.Currency).Validate();
        if(rule.Currency!=currencyCode)throw new InvalidOperationException("Sale and selected commission rule currencies must match; no FX.");
        var raw=rule.RuleType==CommissionRuleType.Percentage?finalUnitPrice*rule.Percentage!.Value/100m:rule.FixedAmount!.Value;
        var amount=decimal.Round(raw,2,MidpointRounding.AwayFromZero);
        if(amount<0m||amount>9999999999999999.99m)throw new InvalidOperationException("Calculated commission exceeds decimal18,2.");
        return new(lead,agreement,rule,saleLocalDate,finalUnitPrice,amount);
    }
}
