using BrokerHub.Domain.Enums;
using BrokerHub.Domain.ValueObjects;

namespace BrokerHub.Domain.Entities;

// Immutable adopted fact. These copied financial scalars are the historical snapshot.
public sealed class Deal
{
    public const int SourceMaxLength=200,ReferenceMaxLength=500,SelectionReasonMaxLength=1000;
    private Deal() { }
    public Guid Id { get; private set; }
    public Guid UnitId { get; private set; }
    public Guid LeadId { get; private set; }
    public long LeadVersion { get; private set; }
    public Guid CustomerId { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid DeveloperId { get; private set; }
    public Guid AgreementId { get; private set; }
    public long AgreementVersion { get; private set; }
    public string AgreementNumber { get; private set; }="";
    public AgreementScopeType AgreementScope { get; private set; }
    public DateOnly AgreementStartsOn { get; private set; }
    public DateOnly AgreementEndsOn { get; private set; }
    public int ProtectionPeriodDays { get; private set; }
    public DateOnly ProtectionEndsOn { get; private set; }
    public DateOnly LeadOpenedOnLocalDate { get; private set; }
    public string OpeningTimeZoneId { get; private set; }="";
    public Guid RuleId { get; private set; }
    public long RuleVersion { get; private set; }
    public Guid? RuleProjectId { get; private set; }
    public CommissionRuleType RuleType { get; private set; }
    public decimal? Percentage { get; private set; }
    public decimal? FixedAmount { get; private set; }
    public string CurrencyCode { get; private set; }="";
    public DateOnly SaleLocalDate { get; private set; }
    public decimal FinalUnitPrice { get; private set; }
    public decimal Amount { get; private set; }
    public DateTime SaleOccurredAtUtc { get; private set; }
    public string Source { get; private set; }="";
    public string Reference { get; private set; }="";
    public string? SelectionReason { get; private set; }
    public DateTime AdoptedAtUtc { get; private set; }
    public Guid AdoptedByAccountId { get; private set; }
    public long Version { get; private set; }=1;

    public static Deal Adopt(Guid id,Unit unit,Lead lead,CommissionCalculation calculation,
        DateTime occurred,DateTime recorded,Guid actor,string source,string reference,string? selectionReason)
    {
        ArgumentNullException.ThrowIfNull(unit);ArgumentNullException.ThrowIfNull(lead);ArgumentNullException.ThrowIfNull(calculation);
        if(id==Guid.Empty||actor==Guid.Empty||unit.Id==Guid.Empty||unit.ProjectId!=calculation.ProjectId||
            lead.Id!=calculation.LeadId||lead.Version!=calculation.LeadVersion||lead.Status is not (Lead.Open or Lead.Closed)||
            lead.CustomerId!=calculation.CustomerId||lead.ProjectId!=calculation.ProjectId)
            throw new InvalidOperationException("Current actual Unit and original Lead must match the calculated origin.");
        if(occurred==default||recorded==default||occurred.Kind!=DateTimeKind.Utc||recorded.Kind!=DateTimeKind.Utc||
            occurred<lead.OpenedAtUtc||occurred>recorded||recorded<lead.ClosedAtUtc||recorded<unit.CreatedAtUtc||recorded<unit.UpdatedAtUtc)
            throw new ArgumentException("Actual sale UTC and monotonic recording audit required.");
        source=Text(source,SourceMaxLength);reference=Text(reference,ReferenceMaxLength);
        if(selectionReason?.Length>SelectionReasonMaxLength)throw new ArgumentException("Bounded selection reason required.");
        return new()
        {
            Id=id,UnitId=unit.Id,LeadId=calculation.LeadId,LeadVersion=calculation.LeadVersion,
            CustomerId=calculation.CustomerId,ProjectId=calculation.ProjectId,DeveloperId=calculation.DeveloperId,
            AgreementId=calculation.AgreementId,AgreementVersion=calculation.AgreementVersion,
            AgreementNumber=calculation.AgreementNumber,AgreementScope=calculation.AgreementScope,
            AgreementStartsOn=calculation.AgreementStartsOn,AgreementEndsOn=calculation.AgreementEndsOn,
            ProtectionPeriodDays=calculation.ProtectionPeriodDays,ProtectionEndsOn=calculation.ProtectionEndsOn,
            LeadOpenedOnLocalDate=calculation.LeadOpenedOnLocalDate,OpeningTimeZoneId=calculation.OpeningTimeZoneId,
            RuleId=calculation.RuleId,RuleVersion=calculation.RuleVersion,RuleProjectId=calculation.RuleProjectId,
            RuleType=calculation.RuleType,Percentage=calculation.Percentage,FixedAmount=calculation.FixedAmount,
            CurrencyCode=calculation.CurrencyCode,SaleLocalDate=calculation.SaleLocalDate,
            FinalUnitPrice=calculation.FinalUnitPrice,Amount=calculation.Amount,SaleOccurredAtUtc=occurred,
            Source=source,Reference=reference,SelectionReason=string.IsNullOrWhiteSpace(selectionReason)?null:selectionReason.Trim(),
            AdoptedAtUtc=recorded,AdoptedByAccountId=actor
        };
    }
    private static string Text(string? value,int max)=>!string.IsNullOrWhiteSpace(value)&&value.Length<=max
        ?value.Trim():throw new ArgumentException("Nonblank bounded sale evidence required.");
}
