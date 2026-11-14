using BrokerHub.Domain.Entities;
using BrokerHub.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BrokerHub.Infrastructure.Persistence.Configurations;

internal sealed class CustomerLinkRequestEventConfiguration : IEntityTypeConfiguration<CustomerLinkRequestEvent>
{
    public void Configure(EntityTypeBuilder<CustomerLinkRequestEvent> b)
    {
        b.ToTable("CustomerLinkRequestEvents", "Crm", t => t.HasCheckConstraint("CK_CustomerLinkRequestEvents_State",
            "([Sequence]=1 AND [Kind]='Submitted' AND [Reason] IS NULL AND [CustomerId] IS NULL) OR ([Sequence]=2 AND [Reason] IS NOT NULL AND LEN(LTRIM(RTRIM([Reason])))>0 AND (([Kind]='Approved' AND [CustomerId] IS NOT NULL) OR ([Kind]='Rejected' AND [CustomerId] IS NULL)))"));
        b.HasKey(e => e.Id); b.Property(e => e.Id).ValueGeneratedNever();
        b.Property(e => e.Kind).HasMaxLength(16).IsRequired();
        b.Property(e => e.Reason).HasMaxLength(CustomerLinkRequest.TextMaxLength);
        b.Property(e => e.AtUtc).HasPrecision(7);
        b.HasIndex(e => new { e.RequestId, e.Sequence }).IsUnique();
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(e => e.ActorAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Customer>().WithMany().HasForeignKey(e => e.CustomerId).OnDelete(DeleteBehavior.Restrict);
    }
}
