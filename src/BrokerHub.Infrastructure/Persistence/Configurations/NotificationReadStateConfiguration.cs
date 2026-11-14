using BrokerHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BrokerHub.Infrastructure.Persistence.Configurations;

internal sealed class NotificationReadStateConfiguration:IEntityTypeConfiguration<NotificationReadState>
{
    public void Configure(EntityTypeBuilder<NotificationReadState> b)
    {
        b.ToTable("NotificationReadStates","Messaging",t=>t.HasCheckConstraint("CK_NotificationReadStates_Clock","[NotificationCreatedAtUtc]>'00010101' AND [ReadAtUtc]>=[NotificationCreatedAtUtc]"));
        b.HasKey(x=>x.NotificationId);b.Property(x=>x.NotificationId).ValueGeneratedNever();
        b.Property(x=>x.NotificationCreatedAtUtc).HasPrecision(7);b.Property(x=>x.ReadAtUtc).HasPrecision(7);
        b.HasOne<Notification>().WithMany().HasForeignKey(x=>new{x.NotificationId,x.NotificationCreatedAtUtc})
            .HasPrincipalKey(x=>new{x.Id,x.CreatedAtUtc}).OnDelete(DeleteBehavior.Restrict);
    }
}
