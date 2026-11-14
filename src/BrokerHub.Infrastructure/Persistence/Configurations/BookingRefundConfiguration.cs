using BrokerHub.Domain.Entities;
using BrokerHub.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace BrokerHub.Infrastructure.Persistence.Configurations;
internal sealed class BookingRefundConfiguration : IEntityTypeConfiguration<BookingRefund>
{
    public void Configure(EntityTypeBuilder<BookingRefund> b)
    {
        b.ToTable("BookingRefunds","Crm",t => {
            t.HasCheckConstraint("CK_BookingRefunds_ClockVersion","[Version]=1 AND [RequestedVersion]>0 AND [DepositPaidAtUtc]>'00010101' AND [ReturnedAtUtc]>=[DepositPaidAtUtc] AND [RecordedAtUtc]>=[ReturnedAtUtc]");
            t.HasCheckConstraint("CK_BookingRefunds_Money","[Amount]>0 AND LEN([CurrencyCode])=3 AND [CurrencyCode] COLLATE Latin1_General_100_BIN2 NOT LIKE N'%[^A-Z]%'");
            t.HasCheckConstraint("CK_BookingRefunds_Evidence","LEN(LTRIM(RTRIM([Source])))>0 AND LEN(LTRIM(RTRIM([Reference])))>0 AND ([Notes] IS NULL OR LEN(LTRIM(RTRIM([Notes])))>0)");
        });
        b.HasKey(x => x.Id);b.Property(x => x.Id).ValueGeneratedNever();
        b.HasOne<BookingDeposit>().WithMany().HasForeignKey(x => new { x.DepositId,x.BookingId,x.CustomerId,x.UnitId,x.CurrencyCode,x.DepositPaidAtUtc })
            .HasPrincipalKey(x => new { x.Id,x.BookingId,x.CustomerId,x.UnitId,x.CurrencyCode,x.PaidAtUtc }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.RecorderAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Employee>().WithMany().HasForeignKey(x => x.RecorderEmployeeId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.DepositId,x.RecordedAtUtc,x.Id });
        b.Property(x => x.Amount).HasPrecision(18,2);b.Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();
        b.Property(x => x.Source).HasMaxLength(BookingRefund.SourceMaxLength).IsRequired();
        b.Property(x => x.Reference).HasMaxLength(BookingRefund.ReferenceMaxLength).IsRequired();b.Property(x => x.Notes).HasMaxLength(BookingRefund.NotesMaxLength);
        b.Property(x => x.DepositPaidAtUtc).HasPrecision(7);b.Property(x => x.ReturnedAtUtc).HasPrecision(7);b.Property(x => x.RecordedAtUtc).HasPrecision(7);
    }
}
