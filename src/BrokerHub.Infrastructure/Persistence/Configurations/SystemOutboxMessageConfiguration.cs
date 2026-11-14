using BrokerHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BrokerHub.Infrastructure.Persistence.Configurations;
internal sealed class SystemOutboxMessageConfiguration : IEntityTypeConfiguration<SystemOutboxMessage>
{
    public void Configure(EntityTypeBuilder<SystemOutboxMessage> b)
    {
        b.ToTable("SystemOutboxMessages", "Reliability", t =>
        {
            t.HasCheckConstraint("CK_SystemOutboxMessages_Metadata", "LEN([Source])>0 AND LEN([Name])>0 AND [SchemaVersion]>0 AND [ResourceVersion]>0 AND [OccurredAtUtc]>'00010101' AND [RecordedAtUtc]>=[OccurredAtUtc]");
            t.HasCheckConstraint("CK_SystemOutboxMessages_Payload", "ISJSON([PayloadJson])=1 AND LEFT(LTRIM([PayloadJson]),1)=N'{' AND DATALENGTH([PayloadJson])<=32768");
        });
        b.HasKey(e => e.Id); b.Property(e => e.Id).ValueGeneratedNever();
        b.Property(e => e.Source).HasMaxLength(64).IsUnicode(false).IsRequired(); b.Property(e => e.Name).HasMaxLength(128).IsUnicode(false).IsRequired();
        b.Property(e => e.PayloadJson).HasMaxLength(16384).IsRequired(); b.Property(e => e.OccurredAtUtc).HasPrecision(7); b.Property(e => e.RecordedAtUtc).HasPrecision(7);
        b.HasIndex(e => new { e.ResourceId, e.ResourceVersion, e.Name }).IsUnique(); b.HasIndex(e => new { e.RecordedAtUtc, e.Id });
    }
}
