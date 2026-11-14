using BrokerHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BrokerHub.Infrastructure.Persistence.Configurations;

internal sealed class MarketingAgreementConfiguration : IEntityTypeConfiguration<MarketingAgreement>
{
    public void Configure(EntityTypeBuilder<MarketingAgreement> b)
    {
        b.ToTable("MarketingAgreements", "Catalog", t =>
        {
            t.HasCheckConstraint("CK_MarketingAgreements_Number", "LEN(LTRIM(RTRIM([AgreementNumber]))) > 0");
            t.HasCheckConstraint("CK_MarketingAgreements_Dates", "[StartsOn] > '00010101' AND [EndsOn] >= [StartsOn]");
            t.HasCheckConstraint("CK_MarketingAgreements_Protection", "[ProtectionPeriodDays] >= 0");
            t.HasCheckConstraint("CK_MarketingAgreements_Scope", "[ScopeType] IN ('AllDeveloperProjects', 'SelectedProjects')");
            t.HasCheckConstraint("CK_MarketingAgreements_Status", "[Status] IN ('Draft', 'Active')");
            t.HasCheckConstraint("CK_MarketingAgreements_Version", "[Version] > 0");
            t.HasCheckConstraint("CK_MarketingAgreements_UpdateAudit", "([UpdatedAtUtc] IS NULL AND [UpdatedByAccountId] IS NULL) OR ([UpdatedAtUtc] IS NOT NULL AND [UpdatedByAccountId] IS NOT NULL)");
        });
        b.HasKey(a => a.Id);
        b.HasAlternateKey(a => new { a.Id, a.DeveloperId });
        b.HasOne<Developer>().WithMany().HasForeignKey(a => a.DeveloperId).OnDelete(DeleteBehavior.Restrict);
        b.Property(a => a.AgreementNumber).HasMaxLength(MarketingAgreement.NumberMaxLength).IsRequired();
        b.HasIndex(a => new { a.DeveloperId, a.AgreementNumber }).IsUnique();
        b.Property(a => a.ScopeType).HasConversion<string>().HasMaxLength(32).IsRequired();
        b.Property(a => a.Status).HasMaxLength(16).IsRequired();
        // Zero is a supplied valid value; never let EF treat it as an omitted database default.
        b.Property(a => a.ProtectionPeriodDays).HasDefaultValue(90).HasSentinel(-1);
        b.Property(a => a.Version).IsConcurrencyToken();
        b.Property(a => a.CreatedAtUtc).HasPrecision(7);
        b.Property(a => a.UpdatedAtUtc).HasPrecision(7);
        b.HasMany(a => a.Projects).WithOne().HasForeignKey(p => new { p.MarketingAgreementId, p.DeveloperId })
            .HasPrincipalKey(a => new { a.Id, a.DeveloperId }).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(a => a.Rules).WithOne().HasForeignKey(r => new { r.MarketingAgreementId, r.DeveloperId })
            .HasPrincipalKey(a => new { a.Id, a.DeveloperId }).OnDelete(DeleteBehavior.Restrict);
        b.Navigation(a => a.Projects).UsePropertyAccessMode(PropertyAccessMode.Field);
        b.Navigation(a => a.Rules).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
