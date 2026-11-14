using BrokerHub.Domain.Entities;
using BrokerHub.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace BrokerHub.Infrastructure.Persistence.Configurations;
internal sealed class BookingDeveloperResponseConfiguration : IEntityTypeConfiguration<BookingDeveloperResponse>
{
    public void Configure(EntityTypeBuilder<BookingDeveloperResponse> b)
    {
        b.ToTable("BookingDeveloperResponses","Crm",t => {
            t.HasCheckConstraint("CK_BookingResponses_Facts","""
                [Version]=1 AND [RequestedVersion]>0 AND [BookingVersionAfter]>=[RequestedVersion] AND [BookingVersionAfter]-[RequestedVersion]<=1 AND
                [DeveloperRespondedAtUtc]>'00010101' AND [RecordedAtUtc]>=[DeveloperRespondedAtUtc] AND LEN(LTRIM(RTRIM([Source])))>0 AND LEN(LTRIM(RTRIM([Reference])))>0 AND
                (([ResponseKind]=N'Confirmed' AND [OfferedPrice] IS NOT NULL AND [OfferedPrice]>0 AND [CurrencyCode] IS NOT NULL AND LEN([CurrencyCode])=3 AND [CurrencyCode] COLLATE Latin1_General_100_BIN2 NOT LIKE N'%[^A-Z]%' AND [OfferedDeadlineUtc] IS NOT NULL AND [OfferedDeadlineUtc]>'00010101' AND [Reason] IS NULL)
                 OR ([ResponseKind]=N'Rejected' AND [OfferedPrice] IS NULL AND [CurrencyCode] IS NULL AND [OfferedDeadlineUtc] IS NULL AND [Reason] IS NOT NULL AND LEN(LTRIM(RTRIM([Reason])))>0))
                """);
            t.HasCheckConstraint("CK_BookingResponses_Decision","""
                ([Decision]=N'Confirmed' AND [ResponseKind]=N'Confirmed' AND [StatusAfter]=N'Confirmed' AND [BookingVersionAfter]-[RequestedVersion]=1 AND [OfferedDeadlineUtc]>[RecordedAtUtc]) OR
                ([Decision]=N'DeveloperRejected' AND [ResponseKind]=N'Rejected' AND [StatusAfter]=N'Rejected' AND [BookingVersionAfter]-[RequestedVersion]=1) OR
                ([Decision]=N'InvalidDeadline' AND [ResponseKind]=N'Confirmed' AND [StatusAfter]=N'Rejected' AND [BookingVersionAfter]-[RequestedVersion]=1 AND [OfferedDeadlineUtc]<=[RecordedAtUtc]) OR
                ([Decision]=N'LocalConflict' AND [ResponseKind]=N'Confirmed' AND [StatusAfter]=N'Rejected' AND [BookingVersionAfter]-[RequestedVersion]=1 AND [OfferedDeadlineUtc]>[RecordedAtUtc]) OR
                ([Decision]=N'ResponseTimeout' AND [StatusAfter]=N'Rejected' AND [BookingVersionAfter]-[RequestedVersion]=1) OR
                ([Decision]=N'ReservationExpired' AND [StatusAfter]=N'Expired' AND [BookingVersionAfter]-[RequestedVersion]=1) OR
                ([Decision]=N'StateUnchanged' AND [StatusAfter] IN (N'Confirmed',N'Rejected',N'Expired',N'Cancelled',N'Completed') AND [BookingVersionAfter]=[RequestedVersion])
                """);
        });
        b.HasKey(x => x.Id); b.Property(x => x.Id).ValueGeneratedNever();
        b.HasAlternateKey(x => new { x.Id,x.BookingId,x.CustomerId });
        b.HasOne<Booking>().WithMany().HasForeignKey(x => new { x.BookingId,x.CustomerId,x.UnitId })
            .HasPrincipalKey(x => new { x.Id,x.CustomerId,x.UnitId }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.RecorderAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Employee>().WithMany().HasForeignKey(x => x.RecorderEmployeeId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.BookingId,x.RecordedAtUtc,x.Id });
        b.Property(x => x.ResponseKind).HasMaxLength(32).IsRequired(); b.Property(x => x.Decision).HasMaxLength(32).IsRequired(); b.Property(x => x.StatusAfter).HasMaxLength(32).IsRequired();
        b.Property(x => x.Source).HasMaxLength(BookingDeveloperResponse.SourceMaxLength).IsRequired(); b.Property(x => x.Reference).HasMaxLength(BookingDeveloperResponse.ReferenceMaxLength).IsRequired();
        b.Property(x => x.Reason).HasMaxLength(BookingDeveloperResponse.ReasonMaxLength); b.Property(x => x.OfferedPrice).HasPrecision(18,2); b.Property(x => x.CurrencyCode).HasMaxLength(3);
        b.Property(x => x.DeveloperRespondedAtUtc).HasPrecision(7); b.Property(x => x.RecordedAtUtc).HasPrecision(7); b.Property(x => x.OfferedDeadlineUtc).HasPrecision(7);
    }
}
