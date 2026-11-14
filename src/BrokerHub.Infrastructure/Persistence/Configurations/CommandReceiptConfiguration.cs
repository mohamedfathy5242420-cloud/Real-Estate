using BrokerHub.Domain.Entities;
using BrokerHub.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BrokerHub.Infrastructure.Persistence.Configurations;

internal sealed class CommandReceiptConfiguration : IEntityTypeConfiguration<CommandReceipt>
{
    public void Configure(EntityTypeBuilder<CommandReceipt> b)
    {
        b.ToTable("CommandReceipts", "Reliability", t =>
        {
            t.HasCheckConstraint("CK_CommandReceipts_Outcome", "LEN([ResourceType])>0 AND [ResourceVersion]>0 AND [EventCount] BETWEEN 0 AND 100");
            t.HasCheckConstraint("CK_CommandReceipts_Identity", "LEN([Operation])>0 AND LEN([IdempotencyKey])>0 AND [Operation] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^a-zA-Z0-9._:-]%' AND LEFT([Operation],1) COLLATE Latin1_General_100_BIN2 LIKE '[a-zA-Z]' AND [IdempotencyKey] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^a-zA-Z0-9._:-]%'");
            t.HasCheckConstraint("CK_CommandReceipts_Fingerprint", "[FingerprintVersion]=1 AND LEN([RequestHash])=64 AND [RequestHash] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9A-F]%'");
        });
        b.HasKey(r => r.Id); b.Property(r => r.Id).ValueGeneratedNever();
        b.Property(r => r.Operation).HasMaxLength(64).IsUnicode(false).UseCollation("Latin1_General_100_BIN2").IsRequired();
        b.Property(r => r.IdempotencyKey).HasMaxLength(128).IsUnicode(false).UseCollation("Latin1_General_100_BIN2").IsRequired();
        b.Property(r => r.RequestHash).HasMaxLength(64).IsUnicode(false).IsFixedLength().IsRequired();
        b.Property(r => r.ResourceType).HasMaxLength(64).IsUnicode(false).IsRequired();
        b.Property(r => r.CompletedAtUtc).HasPrecision(7);
        b.HasIndex(r => new { r.ActorAccountId, r.Operation, r.IdempotencyKey }).IsUnique().HasDatabaseName("UX_CommandReceipts_ActorOperationKey");
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(r => r.ActorAccountId).OnDelete(DeleteBehavior.Restrict);
    }
}
