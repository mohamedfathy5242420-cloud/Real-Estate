using BrokerHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BrokerHub.Infrastructure.Persistence.Configurations;

internal sealed class AgreementProjectConfiguration : IEntityTypeConfiguration<AgreementProject>
{
    public void Configure(EntityTypeBuilder<AgreementProject> b)
    {
        b.ToTable("AgreementProjects", "Catalog");
        b.HasKey(p => new { p.MarketingAgreementId, p.ProjectId });
        b.HasOne<Project>().WithMany().HasForeignKey(p => new { p.ProjectId, p.DeveloperId })
            .HasPrincipalKey(p => new { p.Id, p.DeveloperId }).OnDelete(DeleteBehavior.Restrict);
        b.Property(p => p.AddedAtUtc).HasPrecision(7);
        b.Property(p => p.UpdatedAtUtc).HasPrecision(7);
    }
}
