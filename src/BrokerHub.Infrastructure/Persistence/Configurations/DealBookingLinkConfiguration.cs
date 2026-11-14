using BrokerHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BrokerHub.Infrastructure.Persistence.Configurations;

internal sealed class DealBookingLinkConfiguration:IEntityTypeConfiguration<DealBookingLink>
{
    public void Configure(EntityTypeBuilder<DealBookingLink> b)
    {
        b.ToTable("DealBookingLinks","Sales",t=>t.HasCheckConstraint("CK_DealBookingLinks_Versions",
            "[BookingVersionBefore]>1 AND [BookingVersionBefore]<9223372036854775807 AND [BookingVersionAfter]=[BookingVersionBefore]+1"));
        b.HasKey(x=>x.DealId);b.Property(x=>x.DealId).ValueGeneratedNever();
        b.HasIndex(x=>x.BookingId).IsUnique().HasDatabaseName("UX_DealBookingLinks_Booking");
        b.HasAlternateKey(x=>new{x.DealId,x.BookingId,x.CustomerId,x.UnitId});
        b.HasAlternateKey(x=>new{x.DealId,x.BookingId,x.CustomerId});
        b.HasOne<Deal>().WithMany().HasForeignKey(x=>new{x.DealId,x.CustomerId,x.UnitId})
            .HasPrincipalKey(x=>new{x.Id,x.CustomerId,x.UnitId}).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Booking>().WithMany().HasForeignKey(x=>new{x.BookingId,x.CustomerId,x.UnitId})
            .HasPrincipalKey(x=>new{x.Id,x.CustomerId,x.UnitId}).OnDelete(DeleteBehavior.Restrict);
    }
}
