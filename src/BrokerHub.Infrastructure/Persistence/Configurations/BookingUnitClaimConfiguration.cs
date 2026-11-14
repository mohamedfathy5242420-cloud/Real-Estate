using BrokerHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace BrokerHub.Infrastructure.Persistence.Configurations;
internal sealed class BookingUnitClaimConfiguration : IEntityTypeConfiguration<BookingUnitClaim>
{
    public void Configure(EntityTypeBuilder<BookingUnitClaim> b)
    {
        b.ToTable("BookingUnitClaims","Crm",t => t.HasCheckConstraint("CK_BookingUnitClaims_Shape",
            "[Version]>0 AND [UpdatedAtUtc]>'00010101' AND (([BookingId] IS NULL AND [ExpiresAtUtc] IS NULL) OR ([BookingId] IS NOT NULL AND [ExpiresAtUtc] IS NOT NULL AND [ExpiresAtUtc]>[UpdatedAtUtc]))"));
        b.HasKey(x => x.UnitId); b.Property(x => x.UnitId).ValueGeneratedNever();
        b.HasOne<Unit>().WithMany().HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Booking>().WithMany().HasForeignKey(x => new { x.BookingId,x.UnitId }).HasPrincipalKey(x => new { x.Id,x.UnitId }).OnDelete(DeleteBehavior.Restrict);
        b.Property(x => x.Version).IsConcurrencyToken(); b.Property(x => x.ExpiresAtUtc).HasPrecision(7); b.Property(x => x.UpdatedAtUtc).HasPrecision(7);
    }
}
