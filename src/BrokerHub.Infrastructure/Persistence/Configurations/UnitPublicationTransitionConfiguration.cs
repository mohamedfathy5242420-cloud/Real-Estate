using BrokerHub.Domain.Entities;
using BrokerHub.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace BrokerHub.Infrastructure.Persistence.Configurations;
internal sealed class UnitPublicationTransitionConfiguration : IEntityTypeConfiguration<UnitPublicationTransition>
{
    public void Configure(EntityTypeBuilder<UnitPublicationTransition> b)
    {
        b.ToTable("UnitPublicationTransitions", "Catalog", t =>
        {
            t.HasCheckConstraint("CK_UnitPublicationTransitions_States", "([FromStatus]='Unpublished' AND [ToStatus]='Published') OR ([FromStatus]='Published' AND [ToStatus]='Unpublished')");
            t.HasCheckConstraint("CK_UnitPublicationTransitions_Version", "[UnitVersion]>1");
            t.HasCheckConstraint("CK_UnitPublicationTransitions_Time", "[RecordedAtUtc]>'0001-01-01'");
            t.HasCheckConstraint("CK_UnitPublicationTransitions_Eligibility", "([ToStatus]='Published' AND [MarketingAgreementId] IS NOT NULL AND [EligibilityDate] IS NOT NULL AND [EligibilityTimeZoneId] IS NOT NULL) OR ([ToStatus]='Unpublished' AND [MarketingAgreementId] IS NULL AND [EligibilityDate] IS NULL AND [EligibilityTimeZoneId] IS NULL)");
        });
        b.HasKey(t => t.Id);
        b.HasOne<Unit>().WithMany().HasForeignKey(t => t.UnitId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(t => t.RecordedByAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<MarketingAgreement>().WithMany().HasForeignKey(t => t.MarketingAgreementId).OnDelete(DeleteBehavior.Restrict);
        b.Property(t => t.FromStatus).HasMaxLength(32).IsRequired();
        b.Property(t => t.ToStatus).HasMaxLength(32).IsRequired();
        b.Property(t => t.Reason).HasMaxLength(UnitPublicationTransition.ReasonMaxLength);
        b.Property(t => t.EligibilityTimeZoneId).HasMaxLength(Project.TimeZoneIdMaxLength);
        b.Property(t => t.RecordedAtUtc).HasPrecision(7);
        b.HasIndex(t => new { t.UnitId, t.UnitVersion }).IsUnique();
    }
}
