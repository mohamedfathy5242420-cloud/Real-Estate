using BrokerHub.Domain.Enums;

namespace BrokerHub.Domain.ValueObjects;

public sealed record CommissionRuleDefinition(Guid? ProjectId, CommissionRuleType RuleType,
    decimal? Percentage, decimal? FixedAmount, string Currency)
{
    public void Validate()
    {
        if (ProjectId == Guid.Empty) throw new ArgumentException("Override ProjectId cannot be empty.");
        if (Currency is null || Currency.Length != 3 || Currency.Any(c => c is < 'A' or > 'Z'))
            throw new ArgumentException("Currency must be three uppercase ASCII letters.");
        if (RuleType == CommissionRuleType.Percentage)
        {
            if (FixedAmount.HasValue || !Percentage.HasValue || Percentage <= 0 || Percentage > 100 ||
                decimal.Round(Percentage.Value, 4) != Percentage.Value)
                throw new ArgumentException("Percentage requires >0 and <=100, at most four decimals, and no FixedAmount.");
        }
        else if (RuleType == CommissionRuleType.FixedAmount)
        {
            if (Percentage.HasValue || !FixedAmount.HasValue || FixedAmount < 0 ||
                FixedAmount > 9999999999999999.99m || decimal.Round(FixedAmount.Value, 2) != FixedAmount.Value)
                throw new ArgumentException("FixedAmount requires a nonnegative decimal(18,2) and no Percentage.");
        }
        else throw new ArgumentException("Unknown commission rule type.");
    }
}
