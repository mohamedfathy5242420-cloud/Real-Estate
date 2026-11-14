using BrokerHub.Domain.Entities;
using BrokerHub.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BrokerHub.Infrastructure.Persistence.Configurations;

internal sealed class CustomerLinkRequestConfiguration : IEntityTypeConfiguration<CustomerLinkRequest>
{
    public void Configure(EntityTypeBuilder<CustomerLinkRequest> b)
    {
        b.ToTable("CustomerLinkRequests", "Crm", t =>
        {
            t.HasCheckConstraint("CK_CustomerLinkRequests_Claim", "LEN(LTRIM(RTRIM([Claim])))>0");
            t.HasCheckConstraint("CK_CustomerLinkRequests_State", "([Status]='Pending' AND [Version]=1 AND [DecidedAtUtc] IS NULL AND [DecidedByAccountId] IS NULL AND [DecisionReason] IS NULL AND [CustomerId] IS NULL) OR ([Status] IN ('Approved','Rejected') AND [Version]=2 AND [DecidedAtUtc] IS NOT NULL AND [DecidedAtUtc]>=[CreatedAtUtc] AND [DecidedByAccountId] IS NOT NULL AND [DecisionReason] IS NOT NULL AND LEN(LTRIM(RTRIM([DecisionReason])))>0 AND (([Status]='Approved' AND [CustomerId] IS NOT NULL) OR ([Status]='Rejected' AND [CustomerId] IS NULL)))");
        });
        b.HasKey(r => r.Id); b.Property(r => r.Id).ValueGeneratedNever();
        b.Property(r => r.Claim).HasMaxLength(CustomerLinkRequest.TextMaxLength).IsRequired();
        b.Property(r => r.DecisionReason).HasMaxLength(CustomerLinkRequest.TextMaxLength);
        b.Property(r => r.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
        b.Property(r => r.Version).IsConcurrencyToken();
        b.Property(r => r.CreatedAtUtc).HasPrecision(7); b.Property(r => r.DecidedAtUtc).HasPrecision(7);
        b.HasIndex(r => r.AccountId).IsUnique().HasFilter("[Status]='Pending'").HasDatabaseName("UX_CustomerLinkRequests_PendingAccount");
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(r => r.AccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(r => r.DecidedByAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Customer>().WithMany().HasForeignKey(r => r.CustomerId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(r => r.History).WithOne().HasForeignKey(e => e.RequestId).OnDelete(DeleteBehavior.Restrict);
        b.Navigation(r => r.History).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
