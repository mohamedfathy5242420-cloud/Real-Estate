using BrokerHub.Domain.Entities;
using BrokerHub.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace BrokerHub.Infrastructure.Persistence.Configurations;
internal sealed class BookingExtensionConfiguration : IEntityTypeConfiguration<BookingExtension>
{
    public void Configure(EntityTypeBuilder<BookingExtension> b)
    {
        b.ToTable("BookingExtensions","Crm",t => {
            t.HasCheckConstraint("CK_BookingExtensions_ClockVersion", "[Version]=1 AND [RequestedVersion]>=2 AND [BookingVersionAfter]>[RequestedVersion] AND [BookingVersionAfter]-[RequestedVersion]=1 AND [DeveloperExtendedAtUtc]>'00010101' AND [RecordedAtUtc]>=[DeveloperExtendedAtUtc] AND [PreviousReservationExpiresAtUtc]>[RecordedAtUtc] AND [NewReservationExpiresAtUtc]>[PreviousReservationExpiresAtUtc]");
            t.HasCheckConstraint("CK_BookingExtensions_Evidence", "LEN(LTRIM(RTRIM([Source])))>0 AND LEN(LTRIM(RTRIM([Reference])))>0 AND ([Reason] IS NULL OR LEN(LTRIM(RTRIM([Reason])))>0)");
        });
        b.HasKey(x => x.Id); b.Property(x => x.Id).ValueGeneratedNever(); b.HasAlternateKey(x => new { x.Id,x.BookingId,x.CustomerId });
        b.HasOne<Booking>().WithMany().HasForeignKey(x => new { x.BookingId,x.CustomerId,x.UnitId })
            .HasPrincipalKey(x => new { x.Id,x.CustomerId,x.UnitId }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.RecorderAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Employee>().WithMany().HasForeignKey(x => x.RecorderEmployeeId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.BookingId,x.RecordedAtUtc,x.Id });
        b.Property(x => x.Source).HasMaxLength(BookingExtension.SourceMaxLength).IsRequired(); b.Property(x => x.Reference).HasMaxLength(BookingExtension.ReferenceMaxLength).IsRequired();
        b.Property(x => x.Reason).HasMaxLength(BookingExtension.ReasonMaxLength);
        b.Property(x => x.PreviousReservationExpiresAtUtc).HasPrecision(7); b.Property(x => x.NewReservationExpiresAtUtc).HasPrecision(7);
        b.Property(x => x.DeveloperExtendedAtUtc).HasPrecision(7); b.Property(x => x.RecordedAtUtc).HasPrecision(7);
    }
}
