using BrokerHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BrokerHub.Infrastructure.Persistence.Configurations;

internal sealed class MarketingAgreementRevisionConfiguration : IEntityTypeConfiguration<MarketingAgreementRevision>
{
    public void Configure(EntityTypeBuilder<MarketingAgreementRevision> b)
    {
        b.ToTable("MarketingAgreementRevisions", "Catalog", t =>
        {
            t.HasCheckConstraint("CK_MarketingAgreementRevisions_Version", "[AgreementVersion] > 0");
            t.HasCheckConstraint("CK_MarketingAgreementRevisions_Action", "[Action] IN ('Created', 'DraftUpdated', 'Activated')");
            t.HasCheckConstraint("CK_MarketingAgreementRevisions_Json", "ISJSON([SnapshotJson]) = 1");
        });
        b.HasKey(r => r.Id);
        b.HasOne<MarketingAgreement>().WithMany().HasForeignKey(r => r.MarketingAgreementId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(r => new { r.MarketingAgreementId, r.AgreementVersion }).IsUnique();
        b.Property(r => r.Action).HasMaxLength(32).IsRequired();
        b.Property(r => r.SnapshotJson).IsRequired();
        b.Property(r => r.OccurredAtUtc).HasPrecision(7);
    }
}
