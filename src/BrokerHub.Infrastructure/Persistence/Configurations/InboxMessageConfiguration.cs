using BrokerHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BrokerHub.Infrastructure.Persistence.Configurations;

internal sealed class InboxMessageConfiguration:IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> b)
    {
        b.ToTable("InboxMessages","Reliability",t=>
        {
            t.HasCheckConstraint("CK_InboxMessages_Source","[ConsumerName]='BrokerHub.Notifications.v1' AND [SourceStore] IN ('OutboxMessages','SystemOutboxMessages') AND [EventSchemaVersion]=1 AND [SourceEventId]<>'00000000-0000-0000-0000-000000000000'");
            t.HasCheckConstraint("CK_InboxMessages_Outcome","[NotificationCount]>=0 AND LEN([BodyHash])=64 AND [BodyHash] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9A-F]%' AND LEN([RecipientDigest])=64 AND [RecipientDigest] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9A-F]%'");
            t.HasCheckConstraint("CK_InboxMessages_Clocks","[OccurredAtUtc]>'00010101' AND [SourceRecordedAtUtc]>=[OccurredAtUtc] AND [ProcessedAtUtc]>=[SourceRecordedAtUtc]");
        });
        b.HasKey(x=>x.Id);b.Property(x=>x.Id).ValueGeneratedNever();
        b.Property(x=>x.ConsumerName).HasMaxLength(64).IsUnicode(false).IsRequired();
        b.Property(x=>x.SourceStore).HasMaxLength(32).IsUnicode(false).IsRequired();
        b.Property(x=>x.EventName).HasMaxLength(128).IsUnicode(false).IsRequired();
        b.Property(x=>x.BodyHash).HasMaxLength(64).IsFixedLength().IsUnicode(false).IsRequired();
        b.Property(x=>x.RecipientDigest).HasMaxLength(64).IsFixedLength().IsUnicode(false).IsRequired();
        b.Property(x=>x.OccurredAtUtc).HasPrecision(7);b.Property(x=>x.SourceRecordedAtUtc).HasPrecision(7);b.Property(x=>x.ProcessedAtUtc).HasPrecision(7);
        b.HasIndex(x=>new{x.ConsumerName,x.MessageId}).IsUnique().HasDatabaseName("UX_InboxMessages_ConsumerMessage");
        b.HasOne<OutboxDelivery>().WithMany().HasForeignKey(x=>x.MessageId).OnDelete(DeleteBehavior.Restrict);
    }
}
