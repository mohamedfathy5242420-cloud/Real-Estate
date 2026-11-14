using BrokerHub.Domain.Entities;
using BrokerHub.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BrokerHub.Infrastructure.Persistence.Configurations;
internal sealed class CustomerNeedsChangeConfiguration : IEntityTypeConfiguration<CustomerNeedsChange>
{
    public void Configure(EntityTypeBuilder<CustomerNeedsChange> b)
    {
        b.ToTable("CustomerNeedsChanges", "Crm", t =>
        {
            t.HasCheckConstraint("CK_CustomerNeedsChanges_Version", "[CustomerVersion]>1");
            t.HasCheckConstraint("CK_CustomerNeedsChanges_Text", "([PreviousRequirements] IS NULL OR LEN(LTRIM(RTRIM([PreviousRequirements])))>0) AND ([Requirements] IS NULL OR LEN(LTRIM(RTRIM([Requirements])))>0)");
        });
        b.HasKey(h => h.Id); b.HasIndex(h => new { h.CustomerId, h.CustomerVersion }).IsUnique();
        b.HasOne<Customer>().WithMany().HasForeignKey(h => h.CustomerId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(h => h.ActorAccountId).OnDelete(DeleteBehavior.Restrict);
        b.Property(h => h.PreviousRequirements).HasMaxLength(Customer.RequirementsMaxLength); b.Property(h => h.Requirements).HasMaxLength(Customer.RequirementsMaxLength); b.Property(h => h.AtUtc).HasPrecision(7);
    }
}
