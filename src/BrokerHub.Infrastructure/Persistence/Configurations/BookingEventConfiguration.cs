using BrokerHub.Domain.Entities;
using BrokerHub.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace BrokerHub.Infrastructure.Persistence.Configurations;
internal sealed class BookingEventConfiguration : IEntityTypeConfiguration<BookingEvent>
{
    public void Configure(EntityTypeBuilder<BookingEvent> b)
    {
        var originalShape =
            """
            [AtUtc]>'00010101' AND (
              ([Kind]=N'Requested' AND [BookingVersion]=1 AND [Status]=N'PendingReview' AND [ActorKind]=N'User' AND [ActorAccountId] IS NOT NULL AND [ReasonCode] IS NULL AND [EffectiveCutoffUtc] IS NULL AND [DeveloperResponseId] IS NULL AND [ConfirmedPrice] IS NULL AND [ConfirmedCurrencyCode] IS NULL AND [ReservationExpiresAtUtc] IS NULL)
              OR ([Kind]=N'ResponseTimedOut' AND [BookingVersion]>1 AND [Status]=N'Rejected' AND [ActorKind]=N'System' AND [ActorAccountId] IS NULL AND [ReasonCode] IS NOT NULL AND [ReasonCode]=N'response-timeout' AND [EffectiveCutoffUtc] IS NOT NULL AND [EffectiveCutoffUtc]>'00010101' AND [EffectiveCutoffUtc]<=[AtUtc] AND [DeveloperResponseId] IS NULL AND [ConfirmedPrice] IS NULL AND [ConfirmedCurrencyCode] IS NULL AND [ReservationExpiresAtUtc] IS NULL)
              OR ([Kind]=N'Rejected' AND [BookingVersion]>1 AND [Status]=N'Rejected' AND [ActorKind]=N'User' AND [ActorAccountId] IS NOT NULL AND [ReasonCode] IS NOT NULL AND [ReasonCode] IN (N'developer-rejected',N'invalid-reservation-deadline',N'local-conflict') AND [EffectiveCutoffUtc] IS NULL AND [DeveloperResponseId] IS NOT NULL AND [ConfirmedPrice] IS NULL AND [ConfirmedCurrencyCode] IS NULL AND [ReservationExpiresAtUtc] IS NULL)
              OR ([Kind] IN (N'Confirmed',N'Expired') AND [BookingVersion]>1 AND [ConfirmedPrice] IS NOT NULL AND [ConfirmedPrice]>0 AND [ConfirmedCurrencyCode] IS NOT NULL AND LEN([ConfirmedCurrencyCode])=3 AND [ConfirmedCurrencyCode] COLLATE Latin1_General_100_BIN2 NOT LIKE N'%[^A-Z]%' AND [ReservationExpiresAtUtc] IS NOT NULL AND
                (([Kind]=N'Confirmed' AND [Status]=N'Confirmed' AND [ActorKind]=N'User' AND [ActorAccountId] IS NOT NULL AND [ReasonCode] IS NULL AND [EffectiveCutoffUtc] IS NULL AND [DeveloperResponseId] IS NOT NULL AND [ReservationExpiresAtUtc]>[AtUtc]) OR
                 ([Kind]=N'Expired' AND [BookingVersion]>2 AND [Status]=N'Expired' AND [ActorKind]=N'System' AND [ActorAccountId] IS NULL AND [ReasonCode] IS NOT NULL AND [ReasonCode]=N'reservation-expired' AND [EffectiveCutoffUtc] IS NOT NULL AND [EffectiveCutoffUtc]=[ReservationExpiresAtUtc] AND [EffectiveCutoffUtc]<=[AtUtc] AND [DeveloperResponseId] IS NULL))))
            """;
        var extensionShape = """
            [AtUtc]>'00010101' AND [Kind]=N'Extended' AND [BookingVersion]>2 AND [Status]=N'Confirmed' AND [ActorKind]=N'User' AND [ActorAccountId] IS NOT NULL AND [ReasonCode] IS NULL AND [EffectiveCutoffUtc] IS NULL AND [DeveloperResponseId] IS NULL AND [ExtensionId] IS NOT NULL AND [PreviousReservationExpiresAtUtc] IS NOT NULL AND [PreviousReservationExpiresAtUtc]>[AtUtc] AND [ReservationExpiresAtUtc] IS NOT NULL AND [ReservationExpiresAtUtc]>[PreviousReservationExpiresAtUtc] AND [ConfirmedPrice] IS NOT NULL AND [ConfirmedPrice]>0 AND [ConfirmedCurrencyCode] IS NOT NULL AND LEN([ConfirmedCurrencyCode])=3 AND [ConfirmedCurrencyCode] COLLATE Latin1_General_100_BIN2 NOT LIKE N'%[^A-Z]%'
            """;
        var cancellationShape = """
            [AtUtc]>'00010101' AND [Kind]=N'Cancelled' AND [Status]=N'Cancelled' AND [BookingVersion]>1 AND [ActorKind]=N'User' AND [ActorAccountId] IS NOT NULL AND [CancelledFromStatus] IS NOT NULL AND ([CancellationReason] IS NULL OR LEN(LTRIM(RTRIM([CancellationReason])))>0) AND [ReasonCode] IS NULL AND [EffectiveCutoffUtc] IS NULL AND [DeveloperResponseId] IS NULL AND [PreviousReservationExpiresAtUtc] IS NULL AND [ExtensionId] IS NULL AND
            (([CancelledFromStatus]=N'PendingReview' AND [ConfirmedPrice] IS NULL AND [ConfirmedCurrencyCode] IS NULL AND [ReservationExpiresAtUtc] IS NULL) OR
             ([CancelledFromStatus]=N'Confirmed' AND [BookingVersion]>2 AND [ConfirmedPrice] IS NOT NULL AND [ConfirmedPrice]>0 AND [ConfirmedCurrencyCode] IS NOT NULL AND LEN([ConfirmedCurrencyCode])=3 AND [ConfirmedCurrencyCode] COLLATE Latin1_General_100_BIN2 NOT LIKE N'%[^A-Z]%' AND [ReservationExpiresAtUtc] IS NOT NULL AND [AtUtc]<[ReservationExpiresAtUtc]))
            """;
        var completionShape = """
            [AtUtc]>'00010101' AND [Kind]=N'Completed' AND [Status]=N'Completed' AND [BookingVersion]>2 AND [ActorKind]=N'User' AND [ActorAccountId] IS NOT NULL AND [DealId] IS NOT NULL AND [ReasonCode] IS NULL AND [EffectiveCutoffUtc] IS NULL AND [DeveloperResponseId] IS NULL AND [PreviousReservationExpiresAtUtc] IS NULL AND [ExtensionId] IS NULL AND [CancelledFromStatus] IS NULL AND [CancellationReason] IS NULL AND [ConfirmedPrice] IS NOT NULL AND [ConfirmedPrice]>0 AND [ConfirmedCurrencyCode] IS NOT NULL AND LEN([ConfirmedCurrencyCode])=3 AND [ConfirmedCurrencyCode] COLLATE Latin1_General_100_BIN2 NOT LIKE N'%[^A-Z]%' AND [ReservationExpiresAtUtc] IS NOT NULL AND [AtUtc]<[ReservationExpiresAtUtc]
            """;
        b.ToTable("BookingEvents","Crm",t => t.HasCheckConstraint("CK_BookingEvents_Shape",
            "((((("+originalShape+" AND [PreviousReservationExpiresAtUtc] IS NULL AND [ExtensionId] IS NULL) OR ("+extensionShape+")) AND [CancelledFromStatus] IS NULL AND [CancellationReason] IS NULL) OR ("+cancellationShape+")) AND [DealId] IS NULL) OR ("+completionShape+")"));
        b.HasKey(x => x.Id); b.Property(x => x.Id).ValueGeneratedNever();
        b.HasIndex(x => new { x.BookingId, x.BookingVersion }).IsUnique();
        b.HasOne<Booking>().WithMany().HasForeignKey(x => new { x.BookingId, x.CustomerId })
            .HasPrincipalKey(x => new { x.Id, x.CustomerId }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ActorAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<DealBookingLink>().WithMany().HasForeignKey(x => new { x.DealId,x.BookingId,x.CustomerId })
            .HasPrincipalKey(x => new { x.DealId,x.BookingId,x.CustomerId }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<BookingDeveloperResponse>().WithMany().HasForeignKey(x => new { x.DeveloperResponseId,x.BookingId,x.CustomerId })
            .HasPrincipalKey(x => new { x.Id,x.BookingId,x.CustomerId }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<BookingExtension>().WithMany().HasForeignKey(x => new { x.ExtensionId,x.BookingId,x.CustomerId })
            .HasPrincipalKey(x => new { x.Id,x.BookingId,x.CustomerId }).OnDelete(DeleteBehavior.Restrict);
        b.Property(x => x.PreviousReservationExpiresAtUtc).HasPrecision(7);
        b.Property(x => x.CancelledFromStatus).HasMaxLength(32); b.Property(x => x.CancellationReason).HasMaxLength(Booking.CancellationReasonMaxLength);
        b.Property(x => x.Kind).HasMaxLength(32).IsRequired(); b.Property(x => x.ActorKind).HasMaxLength(16).IsRequired();
        b.Property(x => x.Status).HasMaxLength(32).IsRequired(); b.Property(x => x.ReasonCode).HasMaxLength(64);
        b.Property(x => x.AtUtc).HasPrecision(7); b.Property(x => x.EffectiveCutoffUtc).HasPrecision(7);
        b.Property(x => x.ConfirmedPrice).HasPrecision(18,2); b.Property(x => x.ConfirmedCurrencyCode).HasMaxLength(3);
        b.Property(x => x.ReservationExpiresAtUtc).HasPrecision(7);
    }
}
