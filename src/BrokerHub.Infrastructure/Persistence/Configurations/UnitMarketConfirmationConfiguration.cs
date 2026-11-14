using BrokerHub.Domain.Entities;
using BrokerHub.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace BrokerHub.Infrastructure.Persistence.Configurations;
internal sealed class UnitMarketConfirmationConfiguration : IEntityTypeConfiguration<UnitMarketConfirmation>
{
    public void Configure(EntityTypeBuilder<UnitMarketConfirmation> b)
    {
        b.ToTable("UnitMarketConfirmations", "Catalog", t =>
        {
            t.HasCheckConstraint("CK_UnitMarketConfirmations_Fact", "[ConfirmedPrice] IS NOT NULL OR [AvailabilityStatus] IS NOT NULL");
            t.HasCheckConstraint("CK_UnitMarketConfirmations_Money", "([ConfirmedPrice] IS NULL AND [Currency] IS NULL) OR ([ConfirmedPrice] IS NOT NULL AND [ConfirmedPrice] >= 0 AND [Currency] IS NOT NULL AND LEN([Currency]) = 3 AND [Currency] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^A-Z]%')");
            t.HasCheckConstraint("CK_UnitMarketConfirmations_Availability", "[AvailabilityStatus] IS NULL OR [AvailabilityStatus] IN ('Available','Unavailable')");
            t.HasCheckConstraint("CK_UnitMarketConfirmations_Source", "LEN(LTRIM(RTRIM([Source]))) > 0");
            t.HasCheckConstraint("CK_UnitMarketConfirmations_Time", "[ConfirmedAtUtc] > '0001-01-01' AND [ConfirmedAtUtc] <= [RecordedAtUtc]");
        });
        b.HasKey(c => c.Id);
        b.HasAlternateKey(c => new { c.UnitId, c.Id });
        b.HasOne<Unit>().WithMany().HasForeignKey(c => c.UnitId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(c => c.RecordedByAccountId).OnDelete(DeleteBehavior.Restrict);
        b.Property(c => c.Source).HasMaxLength(UnitMarketConfirmation.SourceMaxLength).IsRequired();
        b.Property(c => c.SourceReference).HasMaxLength(UnitMarketConfirmation.ReferenceMaxLength);
        b.Property(c => c.Notes).HasMaxLength(UnitMarketConfirmation.NotesMaxLength);
        b.Property(c => c.Currency).HasMaxLength(3);
        b.Property(c => c.ConfirmedPrice).HasPrecision(18, 2);
        b.Property(c => c.AvailabilityStatus).HasMaxLength(32);
        b.Property(c => c.ConfirmedAtUtc).HasPrecision(7);
        b.Property(c => c.RecordedAtUtc).HasPrecision(7);
        b.HasIndex(c => new { c.UnitId, c.ConfirmedAtUtc, c.RecordedAtUtc, c.Id })
            .IsDescending(false,true,true,true).HasDatabaseName("IX_UnitMarketConfirmations_History");
    }
}
