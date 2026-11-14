using BrokerHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BrokerHub.Infrastructure.Persistence.Configurations;

internal sealed class CommissionRuleConfiguration : IEntityTypeConfiguration<CommissionRule>
{
    public void Configure(EntityTypeBuilder<CommissionRule> b)
    {
        b.ToTable("CommissionRules", "Catalog", t =>
        {
            t.HasCheckConstraint("CK_CommissionRules_Shape", "([RuleType] = 'Percentage' AND [Percentage] IS NOT NULL AND [Percentage] > 0 AND [Percentage] <= 100 AND [FixedAmount] IS NULL) OR ([RuleType] = 'FixedAmount' AND [FixedAmount] IS NOT NULL AND [FixedAmount] >= 0 AND [Percentage] IS NULL)");
            t.HasCheckConstraint("CK_CommissionRules_Currency", "LEN([Currency]) = 3 AND [Currency] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^A-Z]%'");
            t.HasCheckConstraint("CK_CommissionRules_Version", "[Version] > 0");
            t.HasCheckConstraint("CK_CommissionRules_DefaultNotArchived", "[ProjectId] IS NOT NULL OR [IsArchived] = 0");
        });
        b.HasKey(r => r.Id);
        b.HasAlternateKey(r => new { r.Id,r.MarketingAgreementId,r.DeveloperId });
        b.Property(r => r.RuleType).HasConversion<string>().HasMaxLength(16).IsRequired();
        b.Property(r => r.Percentage).HasPrecision(7,4);
        b.Property(r => r.FixedAmount).HasPrecision(18,2);
        b.Property(r => r.Currency).HasMaxLength(3).IsRequired();
        b.Property(r => r.Version).IsConcurrencyToken();
        b.Property(r => r.CreatedAtUtc).HasPrecision(7);
        b.Property(r => r.UpdatedAtUtc).HasPrecision(7);
        b.HasOne<Project>().WithMany().HasForeignKey(r => new { r.ProjectId, r.DeveloperId })
            .HasPrincipalKey(p => new { p.Id, p.DeveloperId }).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(r => r.MarketingAgreementId).IsUnique().HasFilter("[ProjectId] IS NULL AND [IsArchived] = 0")
            .HasDatabaseName("UX_CommissionRules_CurrentDefault");
        b.HasIndex(r => new { r.MarketingAgreementId, r.ProjectId }).IsUnique()
            .HasFilter("[ProjectId] IS NOT NULL AND [IsArchived] = 0").HasDatabaseName("UX_CommissionRules_CurrentOverride");
    }
}
