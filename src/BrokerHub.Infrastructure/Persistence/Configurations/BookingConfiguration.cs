using BrokerHub.Domain.Entities;
using BrokerHub.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace BrokerHub.Infrastructure.Persistence.Configurations;
internal sealed class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> b)
    {
        var originalState = """
                ([Status]=N'PendingReview' AND [Version]=1 AND [IsActiveRequest]=1 AND [RejectedAtUtc] IS NULL AND [RejectionReason] IS NULL AND [RejectedByAccountId] IS NULL AND [RejectionActorKind] IS NULL AND [ConfirmedAtUtc] IS NULL AND [ConfirmedByAccountId] IS NULL AND [ConfirmedPrice] IS NULL AND [ConfirmedCurrencyCode] IS NULL AND [ReservationExpiresAtUtc] IS NULL AND [ConfirmedResponseId] IS NULL AND [ExpiredAtUtc] IS NULL)
                OR ([Status]=N'Rejected' AND [Version]>1 AND [IsActiveRequest]=0 AND [RejectedAtUtc] IS NOT NULL AND [RejectionReason] IS NOT NULL AND [RejectionActorKind] IS NOT NULL AND [ConfirmedAtUtc] IS NULL AND [ConfirmedByAccountId] IS NULL AND [ConfirmedPrice] IS NULL AND [ConfirmedCurrencyCode] IS NULL AND [ReservationExpiresAtUtc] IS NULL AND [ConfirmedResponseId] IS NULL AND [ExpiredAtUtc] IS NULL AND
                    (([RejectedAtUtc]>=[ResponseDueAtUtc] AND [RejectionReason]=N'response-timeout' AND [RejectedByAccountId] IS NULL AND [RejectionActorKind]=N'System') OR
                    ([RejectedAtUtc]>=[RequestedAtUtc] AND [RejectedAtUtc]<[ResponseDueAtUtc] AND [RejectionReason] IN (N'developer-rejected',N'invalid-reservation-deadline',N'local-conflict') AND [RejectedByAccountId] IS NOT NULL AND [RejectionActorKind]=N'User')))
                OR ([Status] IN (N'Confirmed',N'Expired') AND [Version]>1 AND [RejectedAtUtc] IS NULL AND [RejectionReason] IS NULL AND [RejectedByAccountId] IS NULL AND [RejectionActorKind] IS NULL AND [ConfirmedAtUtc] IS NOT NULL AND [ConfirmedByAccountId] IS NOT NULL AND [ConfirmedPrice] IS NOT NULL AND [ConfirmedCurrencyCode] IS NOT NULL AND [ReservationExpiresAtUtc] IS NOT NULL AND [ConfirmedResponseId] IS NOT NULL AND
                    [ConfirmedAtUtc]>=[RequestedAtUtc] AND [ConfirmedAtUtc]<[ResponseDueAtUtc] AND [ConfirmedPrice]>0 AND LEN([ConfirmedCurrencyCode])=3 AND [ConfirmedCurrencyCode] COLLATE Latin1_General_100_BIN2 NOT LIKE N'%[^A-Z]%' AND [ReservationExpiresAtUtc]>[ConfirmedAtUtc] AND
                    (([Status]=N'Confirmed' AND [IsActiveRequest]=1 AND [ExpiredAtUtc] IS NULL) OR ([Status]=N'Expired' AND [Version]>2 AND [IsActiveRequest]=0 AND [ExpiredAtUtc] IS NOT NULL AND [ExpiredAtUtc]>=[ReservationExpiresAtUtc])))
                """;
        var cancelledState = """
            [Status]=N'Cancelled' AND [Version]>1 AND [IsActiveRequest]=0 AND [CancelledAtUtc] IS NOT NULL AND [CancelledByAccountId] IS NOT NULL AND [CancelledFromStatus] IS NOT NULL AND [CancelledAtUtc]>=[RequestedAtUtc] AND ([CancellationReason] IS NULL OR LEN(LTRIM(RTRIM([CancellationReason])))>0) AND [RejectedAtUtc] IS NULL AND [RejectionReason] IS NULL AND [RejectedByAccountId] IS NULL AND [RejectionActorKind] IS NULL AND [ExpiredAtUtc] IS NULL AND
            (([CancelledFromStatus]=N'PendingReview' AND [CancelledAtUtc]<[ResponseDueAtUtc] AND [ConfirmedAtUtc] IS NULL AND [ConfirmedByAccountId] IS NULL AND [ConfirmedPrice] IS NULL AND [ConfirmedCurrencyCode] IS NULL AND [ReservationExpiresAtUtc] IS NULL AND [ConfirmedResponseId] IS NULL AND [LastExtendedAtUtc] IS NULL AND [LastExtensionId] IS NULL) OR
             ([CancelledFromStatus]=N'Confirmed' AND [Version]>2 AND [ConfirmedAtUtc] IS NOT NULL AND [ConfirmedByAccountId] IS NOT NULL AND [ConfirmedPrice] IS NOT NULL AND [ConfirmedPrice]>0 AND [ConfirmedCurrencyCode] IS NOT NULL AND LEN([ConfirmedCurrencyCode])=3 AND [ConfirmedCurrencyCode] COLLATE Latin1_General_100_BIN2 NOT LIKE N'%[^A-Z]%' AND [ReservationExpiresAtUtc] IS NOT NULL AND [ConfirmedResponseId] IS NOT NULL AND [ConfirmedAtUtc]>=[RequestedAtUtc] AND [ConfirmedAtUtc]<[ResponseDueAtUtc] AND [ReservationExpiresAtUtc]>[ConfirmedAtUtc] AND [CancelledAtUtc]>=[ConfirmedAtUtc] AND [CancelledAtUtc]<[ReservationExpiresAtUtc] AND ([LastExtendedAtUtc] IS NULL OR [CancelledAtUtc]>=[LastExtendedAtUtc])))
            """;
        var completedState = """
            [Status]=N'Completed' AND [Version]>2 AND [IsActiveRequest]=0 AND [CompletedAtUtc] IS NOT NULL AND [CompletedByAccountId] IS NOT NULL AND [CompletedDealId] IS NOT NULL AND [RejectedAtUtc] IS NULL AND [RejectionReason] IS NULL AND [RejectedByAccountId] IS NULL AND [RejectionActorKind] IS NULL AND [ExpiredAtUtc] IS NULL AND [CancelledAtUtc] IS NULL AND [CancelledByAccountId] IS NULL AND [CancelledFromStatus] IS NULL AND [CancellationReason] IS NULL AND [ConfirmedAtUtc] IS NOT NULL AND [ConfirmedByAccountId] IS NOT NULL AND [ConfirmedPrice] IS NOT NULL AND [ConfirmedPrice]>0 AND [ConfirmedCurrencyCode] IS NOT NULL AND LEN([ConfirmedCurrencyCode])=3 AND [ConfirmedCurrencyCode] COLLATE Latin1_General_100_BIN2 NOT LIKE N'%[^A-Z]%' AND [ReservationExpiresAtUtc] IS NOT NULL AND [ConfirmedResponseId] IS NOT NULL AND [ConfirmedAtUtc]>=[RequestedAtUtc] AND [ConfirmedAtUtc]<[ResponseDueAtUtc] AND [ReservationExpiresAtUtc]>[ConfirmedAtUtc] AND [CompletedAtUtc]>=[ConfirmedAtUtc] AND [CompletedAtUtc]<[ReservationExpiresAtUtc] AND ([LastExtendedAtUtc] IS NULL OR [CompletedAtUtc]>=[LastExtendedAtUtc])
            """;
        b.ToTable("Bookings","Crm",t => {
            t.HasCheckConstraint("CK_Bookings_State", "(((("+originalState+") AND [CancelledAtUtc] IS NULL AND [CancelledByAccountId] IS NULL AND [CancelledFromStatus] IS NULL AND [CancellationReason] IS NULL) OR ("+cancelledState+")) AND [CompletedAtUtc] IS NULL AND [CompletedByAccountId] IS NULL AND [CompletedDealId] IS NULL) OR ("+completedState+")");
            t.HasCheckConstraint("CK_Bookings_ResponseClock", "[RequestedAtUtc]>'00010101' AND [ResponseDueAtUtc]=DATEADD(hour,72,[RequestedAtUtc])");
            t.HasCheckConstraint("CK_Bookings_RequestReason", "[RequestReason] IS NULL OR LEN(LTRIM(RTRIM([RequestReason])))>0");
            t.HasCheckConstraint("CK_Bookings_LocalContext", "[SubmissionLocalDate]>'00010101' AND LEN(LTRIM(RTRIM([TimeZoneId])))>0");
            t.HasCheckConstraint("CK_Bookings_LastExtension", "([LastExtendedAtUtc] IS NULL AND [LastExtensionId] IS NULL) OR ([LastExtendedAtUtc] IS NOT NULL AND [LastExtensionId] IS NOT NULL AND ([Status] IN (N'Confirmed',N'Expired',N'Completed') OR ([Status]=N'Cancelled' AND [CancelledFromStatus]=N'Confirmed')) AND [Version]>2 AND [ConfirmedAtUtc] IS NOT NULL AND [ReservationExpiresAtUtc] IS NOT NULL AND [LastExtendedAtUtc]>=[ConfirmedAtUtc] AND [LastExtendedAtUtc]<[ReservationExpiresAtUtc])");
        });
        b.HasKey(x => x.Id); b.Property(x => x.Id).ValueGeneratedNever(); b.HasAlternateKey(x => new { x.Id, x.CustomerId });
        b.HasAlternateKey(x => new { x.Id,x.UnitId });
        b.HasAlternateKey(x => new { x.Id,x.CustomerId,x.UnitId });
        b.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Lead>().WithMany().HasForeignKey(x => new { x.LeadId, x.CustomerId, x.ProjectId })
            .HasPrincipalKey(l => new { l.Id, l.CustomerId, l.ProjectId }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Unit>().WithMany().HasForeignKey(x => new { x.UnitId, x.ProjectId })
            .HasPrincipalKey(u => new { u.Id, u.ProjectId }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<MarketingAgreement>().WithMany().HasForeignKey(x => x.SubmissionAgreementId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.RequestedByAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.RejectedByAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ConfirmedByAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<BookingDeveloperResponse>().WithMany().HasForeignKey(x => new { x.ConfirmedResponseId,x.Id,x.CustomerId })
            .HasPrincipalKey(x => new { x.Id,x.BookingId,x.CustomerId }).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<BookingExtension>().WithMany().HasForeignKey(x => new { x.LastExtensionId,x.Id,x.CustomerId })
            .HasPrincipalKey(x => new { x.Id,x.BookingId,x.CustomerId }).OnDelete(DeleteBehavior.Restrict);
        // The discriminator is part of the key so EF orders release before a new active insertion.
        b.HasIndex(x => new { x.CustomerId, x.UnitId, x.IsActiveRequest }).IsUnique().HasFilter("[IsActiveRequest] = 1")
            .HasDatabaseName("UX_Bookings_ActiveCustomerUnit");
        b.HasIndex(x => new { x.CustomerId, x.RequestedAtUtc, x.Id });
        b.HasIndex(x => new { x.Status, x.ResponseDueAtUtc, x.Id }).HasFilter("[Status] = N'PendingReview'")
            .IncludeProperties(x => new { x.CustomerId, x.Version }).HasDatabaseName("IX_Bookings_PendingResponseDue");
        b.HasIndex(x => new { x.Status,x.ReservationExpiresAtUtc,x.Id }).HasFilter("[Status] = N'Confirmed'")
            .IncludeProperties(x => new { x.CustomerId,x.UnitId,x.Version }).HasDatabaseName("IX_Bookings_ConfirmedReservationDue");
        b.HasIndex(x => new { x.UnitId,x.ReservationExpiresAtUtc,x.Id }).HasFilter("[Status] = N'Confirmed'")
            .IncludeProperties(x => new { x.CustomerId,x.Version }).HasDatabaseName("IX_Bookings_CurrentUnitReservation");
        b.Property(x => x.Status).HasMaxLength(32).IsRequired(); b.Property(x => x.TimeZoneId).HasMaxLength(100).IsRequired();
        b.Property(x => x.RequestReason).HasMaxLength(Booking.RequestReasonMaxLength);
        b.Property(x => x.RejectionReason).HasMaxLength(64); b.Property(x => x.RejectionActorKind).HasMaxLength(16);
        b.Property(x => x.RequestedAtUtc).HasPrecision(7); b.Property(x => x.ResponseDueAtUtc).HasPrecision(7);
        b.Property(x => x.RejectedAtUtc).HasPrecision(7); b.Property(x => x.Version).IsConcurrencyToken();
        b.Property(x => x.ConfirmedAtUtc).HasPrecision(7); b.Property(x => x.ReservationExpiresAtUtc).HasPrecision(7); b.Property(x => x.ExpiredAtUtc).HasPrecision(7);
        b.Property(x => x.ConfirmedPrice).HasPrecision(18,2); b.Property(x => x.ConfirmedCurrencyCode).HasMaxLength(3);
        b.Property(x => x.LastExtendedAtUtc).HasPrecision(7);
        b.Property(x => x.CancelledAtUtc).HasPrecision(7); b.Property(x => x.CancellationReason).HasMaxLength(Booking.CancellationReasonMaxLength);
        b.Property(x => x.CancelledFromStatus).HasMaxLength(32);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.CancelledByAccountId).OnDelete(DeleteBehavior.Restrict);
        b.Property(x => x.CompletedAtUtc).HasPrecision(7);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.CompletedByAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<DealBookingLink>().WithMany().HasForeignKey(x => new { x.CompletedDealId,x.Id,x.CustomerId,x.UnitId })
            .HasPrincipalKey(x => new { x.DealId,x.BookingId,x.CustomerId,x.UnitId }).OnDelete(DeleteBehavior.Restrict);
    }
}
