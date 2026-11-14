using BrokerHub.Domain.Entities;
using BrokerHub.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BrokerHub.Infrastructure.Persistence.Configurations;

internal sealed class CommissionCollectionConfiguration:IEntityTypeConfiguration<CommissionCollection>
{
    public void Configure(EntityTypeBuilder<CommissionCollection> b)
    {
        b.ToTable("CommissionCollections","Sales",t=>
        {
            t.HasCheckConstraint("CK_CommissionCollections_ClockVersion","[Version]=1 AND [RequestedVersion]=1 AND [SaleOccurredAtUtc]>'00010101' AND [CollectedAtUtc]>=[SaleOccurredAtUtc] AND [RecordedAtUtc]>=[CollectedAtUtc]");
            t.HasCheckConstraint("CK_CommissionCollections_Money","[Amount]>0 AND [EntitledAmount]>=0 AND [Amount]<=[EntitledAmount] AND LEN([CurrencyCode])=3 AND [CurrencyCode] COLLATE Latin1_General_100_BIN2 NOT LIKE N'%[^A-Z]%'");
            t.HasCheckConstraint("CK_CommissionCollections_Evidence","LEN(LTRIM(RTRIM([Source])))>0 AND LEN(LTRIM(RTRIM([Reference])))>0 AND ([Notes] IS NULL OR LEN(LTRIM(RTRIM([Notes])))>0)");
        });
        b.HasKey(x=>x.Id);b.Property(x=>x.Id).ValueGeneratedNever();
        b.HasOne<Deal>().WithMany().HasForeignKey(x=>new{x.DealId,x.CustomerId,x.UnitId,x.RequestedVersion,x.CurrencyCode,x.SaleOccurredAtUtc,x.EntitledAmount})
            .HasPrincipalKey(x=>new{x.Id,x.CustomerId,x.UnitId,x.Version,x.CurrencyCode,x.SaleOccurredAtUtc,x.Amount}).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x=>x.RecorderAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x=>new{x.DealId,x.RecordedAtUtc,x.Id});
        b.Property(x=>x.Amount).HasPrecision(18,2);b.Property(x=>x.EntitledAmount).HasPrecision(18,2);
        b.Property(x=>x.CurrencyCode).HasMaxLength(3).IsRequired();
        b.Property(x=>x.Source).HasMaxLength(CommissionCollection.SourceMaxLength).IsRequired();
        b.Property(x=>x.Reference).HasMaxLength(CommissionCollection.ReferenceMaxLength).IsRequired();
        b.Property(x=>x.Notes).HasMaxLength(CommissionCollection.NotesMaxLength);
        b.Property(x=>x.SaleOccurredAtUtc).HasPrecision(7);b.Property(x=>x.CollectedAtUtc).HasPrecision(7);b.Property(x=>x.RecordedAtUtc).HasPrecision(7);
    }
}
