using BrokerHub.Domain.Entities;
using BrokerHub.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BrokerHub.Infrastructure.Persistence.Configurations;

internal sealed class NotificationProcessingAuditConfiguration:IEntityTypeConfiguration<NotificationProcessingAudit>
{
    public void Configure(EntityTypeBuilder<NotificationProcessingAudit> b)
    {
        b.ToTable("NotificationProcessingAudits","Messaging",t=>
        {
            t.HasCheckConstraint("CK_NotificationProcessingAudits_Provenance","([SourceStore]='OutboxMessages' AND [SourceActorAccountId] IS NOT NULL AND [SystemSource] IS NULL) OR ([SourceStore]='SystemOutboxMessages' AND [SourceActorAccountId] IS NULL AND [SystemSource] IS NOT NULL AND LEN([SystemSource])>0)");
            t.HasCheckConstraint("CK_NotificationProcessingAudits_Outcome","[NotificationCount]>=0 AND [EventSchemaVersion]=1 AND [ConsumerName]='BrokerHub.Notifications.v1' AND LEN([BodyHash])=64 AND [BodyHash] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9A-F]%' AND LEN([RecipientDigest])=64 AND [RecipientDigest] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9A-F]%'");
            t.HasCheckConstraint("CK_NotificationProcessingAudits_Clocks","[OccurredAtUtc]>'00010101' AND [ProcessedAtUtc]>=[OccurredAtUtc]");
        });
        b.HasKey(x=>x.InboxMessageId);b.Property(x=>x.InboxMessageId).ValueGeneratedNever();
        b.Property(x=>x.ConsumerName).HasMaxLength(64).IsUnicode(false).IsRequired();
        b.Property(x=>x.SourceStore).HasMaxLength(32).IsUnicode(false).IsRequired();
        b.Property(x=>x.SystemSource).HasMaxLength(64).IsUnicode(false);
        b.Property(x=>x.EventName).HasMaxLength(128).IsUnicode(false).IsRequired();
        b.Property(x=>x.BodyHash).HasMaxLength(64).IsFixedLength().IsUnicode(false).IsRequired();
        b.Property(x=>x.RecipientDigest).HasMaxLength(64).IsFixedLength().IsUnicode(false).IsRequired();
        b.Property(x=>x.OccurredAtUtc).HasPrecision(7);b.Property(x=>x.ProcessedAtUtc).HasPrecision(7);
        b.HasOne<InboxMessage>().WithOne().HasForeignKey<NotificationProcessingAudit>(x=>x.InboxMessageId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x=>x.SourceActorAccountId).OnDelete(DeleteBehavior.Restrict);
    }
}
