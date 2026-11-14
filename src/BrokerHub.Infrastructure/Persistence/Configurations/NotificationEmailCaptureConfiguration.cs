using BrokerHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BrokerHub.Infrastructure.Persistence.Configurations;

internal sealed class NotificationEmailCaptureConfiguration:IEntityTypeConfiguration<NotificationEmailCapture>
{
    public void Configure(EntityTypeBuilder<NotificationEmailCapture> b)
    {
        b.ToTable("NotificationEmailCaptures","Messaging",t=>
        {
            t.HasCheckConstraint("CK_NotificationEmailCaptures_Clock","[NotificationCreatedAtUtc]>'00010101' AND [CapturedAtUtc]>=[NotificationCreatedAtUtc]");
            t.HasCheckConstraint("CK_NotificationEmailCaptures_Hash","LEN([ContentHash])=64 AND [ContentHash] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9A-F]%'");
        });
        b.HasKey(x=>x.NotificationId);b.Property(x=>x.NotificationId).ValueGeneratedNever();
        b.Property(x=>x.NotificationCreatedAtUtc).HasPrecision(7);b.Property(x=>x.CapturedAtUtc).HasPrecision(7);
        b.Property(x=>x.ContentHash).HasMaxLength(64).IsFixedLength().IsUnicode(false).IsRequired();
        b.HasOne<Notification>().WithMany().HasForeignKey(x=>new{x.NotificationId,x.NotificationCreatedAtUtc})
            .HasPrincipalKey(x=>new{x.Id,x.CreatedAtUtc}).OnDelete(DeleteBehavior.Restrict);
    }
}
