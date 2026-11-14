using BrokerHub.Domain.Entities;
using BrokerHub.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BrokerHub.Infrastructure.Persistence.Configurations;
internal sealed class LeadConfiguration : IEntityTypeConfiguration<Lead>
{
    public void Configure(EntityTypeBuilder<Lead> b)
    {
        b.ToTable("Leads", "Crm", t =>
        {
            t.HasCheckConstraint("CK_Leads_Origin", "[AgreementVersionAtOpening]>0 AND [AgreementStartsOn]>'00010101' AND [AgreementEndsOn]>=[AgreementStartsOn] AND [OpenedOnLocalDate]>=[AgreementStartsOn] AND [OpenedOnLocalDate]<=[AgreementEndsOn]");
            t.HasCheckConstraint("CK_Leads_Protection", "[ProtectionPeriodDays]>=0 AND [ProtectionEndsOn]>=[AgreementEndsOn] AND DATEDIFF(day,[AgreementEndsOn],[ProtectionEndsOn])=[ProtectionPeriodDays]");
            t.HasCheckConstraint("CK_Leads_Text", "LEN(LTRIM(RTRIM([Source])))>0 AND LEN(LTRIM(RTRIM([OpeningTimeZoneId])))>0");
            t.HasCheckConstraint("CK_Leads_StateAudit", "([Status]=N'Open' AND [Version]=1 AND [ClosedAtUtc] IS NULL AND [ClosedByAccountId] IS NULL AND [CloseReason] IS NULL) OR ([Status]=N'Closed' AND [Version]>1 AND [ClosedAtUtc]>=[OpenedAtUtc] AND [ClosedAtUtc] IS NOT NULL AND [ClosedByAccountId] IS NOT NULL AND [CloseReason] IS NOT NULL AND LEN(LTRIM(RTRIM([CloseReason])))>0)");
        });
        b.HasKey(l => l.Id); b.HasAlternateKey(l => new { l.Id, l.CustomerId });
        b.HasAlternateKey(l => new { l.Id,l.CustomerId,l.ProjectId,l.MarketingAgreementId });
        b.HasIndex(l => new { l.CustomerId, l.ProjectId }).IsUnique().HasFilter("[Status] = N'Open'").HasDatabaseName("UX_Leads_OpenCustomerProject");
        b.HasOne<Customer>().WithMany().HasForeignKey(l => l.CustomerId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Project>().WithMany().HasForeignKey(l => l.ProjectId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<MarketingAgreement>().WithMany().HasForeignKey(l => l.MarketingAgreementId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(l => l.OpenedByAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(l => l.ClosedByAccountId).OnDelete(DeleteBehavior.Restrict);
        b.Property(l => l.Source).HasMaxLength(Lead.SourceMaxLength).IsRequired();
        b.Property(l => l.OpeningTimeZoneId).HasMaxLength(100).IsRequired();
        b.Property(l => l.Status).HasMaxLength(16).IsRequired(); b.Property(l => l.CloseReason).HasMaxLength(Lead.ReasonMaxLength);
        b.Property(l => l.Version).IsConcurrencyToken(); b.Property(l => l.OpenedAtUtc).HasPrecision(7); b.Property(l => l.ClosedAtUtc).HasPrecision(7);
    }
}
