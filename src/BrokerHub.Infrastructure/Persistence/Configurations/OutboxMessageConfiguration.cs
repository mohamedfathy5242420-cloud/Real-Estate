using BrokerHub.Domain.Entities;
using BrokerHub.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BrokerHub.Infrastructure.Persistence.Configurations;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> b)
    {
        b.ToTable("OutboxMessages", "Reliability", t =>
        {
            t.HasCheckConstraint("CK_OutboxMessages_Metadata", "LEN([Name])>0 AND [SchemaVersion]>0 AND [RecordedAtUtc]>=[OccurredAtUtc]");
            t.HasCheckConstraint("CK_OutboxMessages_Payload", "ISJSON([PayloadJson])=1 AND LEFT(LTRIM([PayloadJson]),1)=N'{' AND DATALENGTH([PayloadJson])<=32768");
        });
        b.HasKey(e => e.Id); b.Property(e => e.Id).ValueGeneratedNever();
        b.Property(e => e.Name).HasMaxLength(128).IsUnicode(false).IsRequired();
        b.Property(e => e.PayloadJson).HasMaxLength(16384).IsRequired();
        b.Property(e => e.OccurredAtUtc).HasPrecision(7); b.Property(e => e.RecordedAtUtc).HasPrecision(7);
        b.HasOne<CommandReceipt>().WithMany().HasForeignKey(e => e.CommandReceiptId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(e => e.ActorAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(e => new { e.RecordedAtUtc, e.Id });
    }
}
