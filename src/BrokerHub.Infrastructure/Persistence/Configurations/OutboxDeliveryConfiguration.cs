using BrokerHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BrokerHub.Infrastructure.Persistence.Configurations;

internal sealed class OutboxDeliveryConfiguration:IEntityTypeConfiguration<OutboxDelivery>
{
    public void Configure(EntityTypeBuilder<OutboxDelivery> b)
    {
        b.ToTable("OutboxDeliveries","Reliability",t=>
        {
            t.HasCheckConstraint("CK_OutboxDeliveries_Source","([HumanOutboxId] IS NOT NULL AND [SystemOutboxId] IS NULL) OR ([HumanOutboxId] IS NULL AND [SystemOutboxId] IS NOT NULL)");
            t.HasCheckConstraint("CK_OutboxDeliveries_Clocks","[EventRecordedAtUtc]>'00010101' AND [CreatedAtUtc]>=[EventRecordedAtUtc] AND [AvailableAfterUtc]>=[CreatedAtUtc] AND ([LastAttemptAtUtc] IS NULL OR [LastAttemptAtUtc]>=[CreatedAtUtc]) AND ([LastFailureAtUtc] IS NULL OR [LastFailureAtUtc]>=[CreatedAtUtc]) AND [Version]>0 AND [AttemptCount]>=0");
            t.HasCheckConstraint("CK_OutboxDeliveries_State","([State]='Pending' AND [LeaseToken] IS NULL AND [LeaseExpiresAtUtc] IS NULL AND [PublishedAtUtc] IS NULL) OR ([State]='Leased' AND [LeaseToken] IS NOT NULL AND [LeaseExpiresAtUtc]>[LastAttemptAtUtc] AND [LastAttemptAtUtc] IS NOT NULL AND [AttemptCount]>0 AND [PublishedAtUtc] IS NULL) OR ([State]='Published' AND [LeaseToken] IS NULL AND [LeaseExpiresAtUtc] IS NULL AND [PublishedAtUtc]>=[LastAttemptAtUtc] AND [LastAttemptAtUtc] IS NOT NULL AND [AttemptCount]>0) OR ([State]='DeadLetter' AND [LeaseToken] IS NULL AND [LeaseExpiresAtUtc] IS NULL AND [PublishedAtUtc] IS NULL AND [AttemptCount]>0)");
            t.HasCheckConstraint("CK_OutboxDeliveries_Failure","([LastFailureKind]='None' AND [LastFailureAtUtc] IS NULL) OR ([LastFailureKind] IN ('TransportUnavailable','NotConfirmed','ConfirmUncertain','InvalidEnvelope','Rejected','LeaseExpired') AND [LastFailureAtUtc] IS NOT NULL)");
        });
        b.HasKey(x=>x.Id);b.Property(x=>x.Id).ValueGeneratedNever();
        b.Property(x=>x.State).HasMaxLength(16).IsUnicode(false).IsRequired();
        b.Property(x=>x.LastFailureKind).HasConversion<string>().HasMaxLength(32).IsUnicode(false).IsRequired();
        b.Property(x=>x.Version).IsConcurrencyToken();
        b.Property(x=>x.EventRecordedAtUtc).HasPrecision(7);b.Property(x=>x.CreatedAtUtc).HasPrecision(7);
        b.Property(x=>x.AvailableAfterUtc).HasPrecision(7);b.Property(x=>x.LeaseExpiresAtUtc).HasPrecision(7);
        b.Property(x=>x.LastAttemptAtUtc).HasPrecision(7);b.Property(x=>x.PublishedAtUtc).HasPrecision(7);b.Property(x=>x.LastFailureAtUtc).HasPrecision(7);
        b.HasOne<OutboxMessage>().WithMany().HasForeignKey(x=>x.HumanOutboxId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<SystemOutboxMessage>().WithMany().HasForeignKey(x=>x.SystemOutboxId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x=>x.HumanOutboxId).IsUnique().HasFilter("[HumanOutboxId] IS NOT NULL");
        b.HasIndex(x=>x.SystemOutboxId).IsUnique().HasFilter("[SystemOutboxId] IS NOT NULL");
        b.HasIndex(x=>new{x.State,x.AvailableAfterUtc,x.EventRecordedAtUtc,x.Id});
        b.HasIndex(x=>new{x.State,x.LeaseExpiresAtUtc,x.EventRecordedAtUtc,x.Id});
    }
}
