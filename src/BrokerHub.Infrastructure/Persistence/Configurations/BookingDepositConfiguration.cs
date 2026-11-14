using BrokerHub.Domain.Entities;
using BrokerHub.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace BrokerHub.Infrastructure.Persistence.Configurations;
internal sealed class BookingDepositConfiguration : IEntityTypeConfiguration<BookingDeposit>
{
    public void Configure(EntityTypeBuilder<BookingDeposit> b)
    {
        b.ToTable("BookingDeposits","Crm",t => {
            t.HasCheckConstraint("CK_BookingDeposits_ClockVersion","[Version]=1 AND [RequestedVersion]>0 AND [PaidAtUtc]>'00010101' AND [RecordedAtUtc]>=[PaidAtUtc]");
            t.HasCheckConstraint("CK_BookingDeposits_Money","[Amount]>0 AND LEN([CurrencyCode])=3 AND [CurrencyCode] COLLATE Latin1_General_100_BIN2 NOT LIKE N'%[^A-Z]%'");
            t.HasCheckConstraint("CK_BookingDeposits_Evidence","LEN(LTRIM(RTRIM([Source])))>0 AND LEN(LTRIM(RTRIM([Reference])))>0 AND ([Notes] IS NULL OR LEN(LTRIM(RTRIM([Notes])))>0)");
        });
        b.HasKey(x => x.Id); b.Property(x => x.Id).ValueGeneratedNever();
        b.HasAlternateKey(x => new { x.Id,x.BookingId,x.CustomerId,x.UnitId,x.CurrencyCode,x.PaidAtUtc });
        b.HasOne<Booking>().WithMany().HasForeignKey(x => new { x.BookingId,x.CustomerId,x.UnitId })
            .HasPrincipalKey(x => new { x.Id,x.CustomerId,x.UnitId }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.RecorderAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Employee>().WithMany().HasForeignKey(x => x.RecorderEmployeeId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.BookingId,x.RecordedAtUtc,x.Id });
        b.Property(x => x.Amount).HasPrecision(18,2); b.Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();
        b.Property(x => x.Source).HasMaxLength(BookingDeposit.SourceMaxLength).IsRequired();
        b.Property(x => x.Reference).HasMaxLength(BookingDeposit.ReferenceMaxLength).IsRequired();
        b.Property(x => x.Notes).HasMaxLength(BookingDeposit.NotesMaxLength);
        b.Property(x => x.PaidAtUtc).HasPrecision(7); b.Property(x => x.RecordedAtUtc).HasPrecision(7);
    }
}
