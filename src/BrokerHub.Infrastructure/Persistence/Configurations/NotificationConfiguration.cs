using BrokerHub.Domain.Entities;
using BrokerHub.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BrokerHub.Infrastructure.Persistence.Configurations;

internal sealed class NotificationConfiguration:IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> b)
    {
        b.ToTable("Notifications","Messaging",t=>
        {
            t.HasCheckConstraint("CK_Notifications_Scope","([ResourceKind]='Unit' AND [CustomerId] IS NULL) OR ([ResourceKind] IN ('Customer','Viewing','Booking','BookingDeposit','BookingRefund','Deal','CommissionCollection') AND [CustomerId] IS NOT NULL)");
            t.HasCheckConstraint("CK_Notifications_Identities","[Id]<>'00000000-0000-0000-0000-000000000000' AND [RecipientAccountId]<>'00000000-0000-0000-0000-000000000000' AND [ResourceId]<>'00000000-0000-0000-0000-000000000000' AND ([ParentResourceId] IS NULL OR [ParentResourceId]<>'00000000-0000-0000-0000-000000000000') AND ([RelatedResourceId] IS NULL OR [RelatedResourceId]<>'00000000-0000-0000-0000-000000000000')");
            t.HasCheckConstraint("CK_Notifications_Clocks","[OccurredAtUtc]>'00010101' AND [CreatedAtUtc]>=[OccurredAtUtc]");
        });
        b.HasKey(x=>x.Id);b.Property(x=>x.Id).ValueGeneratedNever();
        b.HasAlternateKey(x=>new{x.Id,x.CreatedAtUtc});
        b.Property(x=>x.EventName).HasMaxLength(128).IsUnicode(false).IsRequired();
        b.Property(x=>x.ResourceKind).HasMaxLength(32).IsUnicode(false).IsRequired();
        b.Property(x=>x.OccurredAtUtc).HasPrecision(7);b.Property(x=>x.CreatedAtUtc).HasPrecision(7);
        b.HasOne<InboxMessage>().WithMany().HasForeignKey(x=>x.InboxMessageId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x=>x.RecipientAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Customer>().WithMany().HasForeignKey(x=>x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x=>new{x.InboxMessageId,x.RecipientAccountId}).IsUnique();
        b.HasIndex(x=>new{x.RecipientAccountId,x.CreatedAtUtc,x.Id});
    }
}
