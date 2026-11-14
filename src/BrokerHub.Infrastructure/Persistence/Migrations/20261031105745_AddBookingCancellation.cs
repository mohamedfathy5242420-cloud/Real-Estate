using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrokerHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingCancellation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Bookings_LastExtension",
                schema: "Crm",
                table: "Bookings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Bookings_State",
                schema: "Crm",
                table: "Bookings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_BookingEvents_Shape",
                schema: "Crm",
                table: "BookingEvents");

            migrationBuilder.DropCheckConstraint(
                name: "CK_BookingResponses_Decision",
                schema: "Crm",
                table: "BookingDeveloperResponses");

            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                schema: "Crm",
                table: "Bookings",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAtUtc",
                schema: "Crm",
                table: "Bookings",
                type: "datetime2(7)",
                precision: 7,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CancelledByAccountId",
                schema: "Crm",
                table: "Bookings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancelledFromStatus",
                schema: "Crm",
                table: "Bookings",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                schema: "Crm",
                table: "BookingEvents",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancelledFromStatus",
                schema: "Crm",
                table: "BookingEvents",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_CancelledByAccountId",
                schema: "Crm",
                table: "Bookings",
                column: "CancelledByAccountId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Bookings_LastExtension",
                schema: "Crm",
                table: "Bookings",
                sql: "([LastExtendedAtUtc] IS NULL AND [LastExtensionId] IS NULL) OR ([LastExtendedAtUtc] IS NOT NULL AND [LastExtensionId] IS NOT NULL AND ([Status] IN (N'Confirmed',N'Expired') OR ([Status]=N'Cancelled' AND [CancelledFromStatus]=N'Confirmed')) AND [Version]>2 AND [ConfirmedAtUtc] IS NOT NULL AND [ReservationExpiresAtUtc] IS NOT NULL AND [LastExtendedAtUtc]>=[ConfirmedAtUtc] AND [LastExtendedAtUtc]<[ReservationExpiresAtUtc])");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Bookings_State",
                schema: "Crm",
                table: "Bookings",
                sql: "((([Status]=N'PendingReview' AND [Version]=1 AND [IsActiveRequest]=1 AND [RejectedAtUtc] IS NULL AND [RejectionReason] IS NULL AND [RejectedByAccountId] IS NULL AND [RejectionActorKind] IS NULL AND [ConfirmedAtUtc] IS NULL AND [ConfirmedByAccountId] IS NULL AND [ConfirmedPrice] IS NULL AND [ConfirmedCurrencyCode] IS NULL AND [ReservationExpiresAtUtc] IS NULL AND [ConfirmedResponseId] IS NULL AND [ExpiredAtUtc] IS NULL)\nOR ([Status]=N'Rejected' AND [Version]>1 AND [IsActiveRequest]=0 AND [RejectedAtUtc] IS NOT NULL AND [RejectionReason] IS NOT NULL AND [RejectionActorKind] IS NOT NULL AND [ConfirmedAtUtc] IS NULL AND [ConfirmedByAccountId] IS NULL AND [ConfirmedPrice] IS NULL AND [ConfirmedCurrencyCode] IS NULL AND [ReservationExpiresAtUtc] IS NULL AND [ConfirmedResponseId] IS NULL AND [ExpiredAtUtc] IS NULL AND\n    (([RejectedAtUtc]>=[ResponseDueAtUtc] AND [RejectionReason]=N'response-timeout' AND [RejectedByAccountId] IS NULL AND [RejectionActorKind]=N'System') OR\n    ([RejectedAtUtc]>=[RequestedAtUtc] AND [RejectedAtUtc]<[ResponseDueAtUtc] AND [RejectionReason] IN (N'developer-rejected',N'invalid-reservation-deadline',N'local-conflict') AND [RejectedByAccountId] IS NOT NULL AND [RejectionActorKind]=N'User')))\nOR ([Status] IN (N'Confirmed',N'Expired') AND [Version]>1 AND [RejectedAtUtc] IS NULL AND [RejectionReason] IS NULL AND [RejectedByAccountId] IS NULL AND [RejectionActorKind] IS NULL AND [ConfirmedAtUtc] IS NOT NULL AND [ConfirmedByAccountId] IS NOT NULL AND [ConfirmedPrice] IS NOT NULL AND [ConfirmedCurrencyCode] IS NOT NULL AND [ReservationExpiresAtUtc] IS NOT NULL AND [ConfirmedResponseId] IS NOT NULL AND\n    [ConfirmedAtUtc]>=[RequestedAtUtc] AND [ConfirmedAtUtc]<[ResponseDueAtUtc] AND [ConfirmedPrice]>0 AND LEN([ConfirmedCurrencyCode])=3 AND [ConfirmedCurrencyCode] COLLATE Latin1_General_100_BIN2 NOT LIKE N'%[^A-Z]%' AND [ReservationExpiresAtUtc]>[ConfirmedAtUtc] AND\n    (([Status]=N'Confirmed' AND [IsActiveRequest]=1 AND [ExpiredAtUtc] IS NULL) OR ([Status]=N'Expired' AND [Version]>2 AND [IsActiveRequest]=0 AND [ExpiredAtUtc] IS NOT NULL AND [ExpiredAtUtc]>=[ReservationExpiresAtUtc])))) AND [CancelledAtUtc] IS NULL AND [CancelledByAccountId] IS NULL AND [CancelledFromStatus] IS NULL AND [CancellationReason] IS NULL) OR ([Status]=N'Cancelled' AND [Version]>1 AND [IsActiveRequest]=0 AND [CancelledAtUtc] IS NOT NULL AND [CancelledByAccountId] IS NOT NULL AND [CancelledFromStatus] IS NOT NULL AND [CancelledAtUtc]>=[RequestedAtUtc] AND ([CancellationReason] IS NULL OR LEN(LTRIM(RTRIM([CancellationReason])))>0) AND [RejectedAtUtc] IS NULL AND [RejectionReason] IS NULL AND [RejectedByAccountId] IS NULL AND [RejectionActorKind] IS NULL AND [ExpiredAtUtc] IS NULL AND\n(([CancelledFromStatus]=N'PendingReview' AND [CancelledAtUtc]<[ResponseDueAtUtc] AND [ConfirmedAtUtc] IS NULL AND [ConfirmedByAccountId] IS NULL AND [ConfirmedPrice] IS NULL AND [ConfirmedCurrencyCode] IS NULL AND [ReservationExpiresAtUtc] IS NULL AND [ConfirmedResponseId] IS NULL AND [LastExtendedAtUtc] IS NULL AND [LastExtensionId] IS NULL) OR\n ([CancelledFromStatus]=N'Confirmed' AND [Version]>2 AND [ConfirmedAtUtc] IS NOT NULL AND [ConfirmedByAccountId] IS NOT NULL AND [ConfirmedPrice] IS NOT NULL AND [ConfirmedPrice]>0 AND [ConfirmedCurrencyCode] IS NOT NULL AND LEN([ConfirmedCurrencyCode])=3 AND [ConfirmedCurrencyCode] COLLATE Latin1_General_100_BIN2 NOT LIKE N'%[^A-Z]%' AND [ReservationExpiresAtUtc] IS NOT NULL AND [ConfirmedResponseId] IS NOT NULL AND [ConfirmedAtUtc]>=[RequestedAtUtc] AND [ConfirmedAtUtc]<[ResponseDueAtUtc] AND [ReservationExpiresAtUtc]>[ConfirmedAtUtc] AND [CancelledAtUtc]>=[ConfirmedAtUtc] AND [CancelledAtUtc]<[ReservationExpiresAtUtc] AND ([LastExtendedAtUtc] IS NULL OR [CancelledAtUtc]>=[LastExtendedAtUtc]))))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_BookingEvents_Shape",
                schema: "Crm",
                table: "BookingEvents",
                sql: "((([AtUtc]>'00010101' AND (\n  ([Kind]=N'Requested' AND [BookingVersion]=1 AND [Status]=N'PendingReview' AND [ActorKind]=N'User' AND [ActorAccountId] IS NOT NULL AND [ReasonCode] IS NULL AND [EffectiveCutoffUtc] IS NULL AND [DeveloperResponseId] IS NULL AND [ConfirmedPrice] IS NULL AND [ConfirmedCurrencyCode] IS NULL AND [ReservationExpiresAtUtc] IS NULL)\n  OR ([Kind]=N'ResponseTimedOut' AND [BookingVersion]>1 AND [Status]=N'Rejected' AND [ActorKind]=N'System' AND [ActorAccountId] IS NULL AND [ReasonCode] IS NOT NULL AND [ReasonCode]=N'response-timeout' AND [EffectiveCutoffUtc] IS NOT NULL AND [EffectiveCutoffUtc]>'00010101' AND [EffectiveCutoffUtc]<=[AtUtc] AND [DeveloperResponseId] IS NULL AND [ConfirmedPrice] IS NULL AND [ConfirmedCurrencyCode] IS NULL AND [ReservationExpiresAtUtc] IS NULL)\n  OR ([Kind]=N'Rejected' AND [BookingVersion]>1 AND [Status]=N'Rejected' AND [ActorKind]=N'User' AND [ActorAccountId] IS NOT NULL AND [ReasonCode] IS NOT NULL AND [ReasonCode] IN (N'developer-rejected',N'invalid-reservation-deadline',N'local-conflict') AND [EffectiveCutoffUtc] IS NULL AND [DeveloperResponseId] IS NOT NULL AND [ConfirmedPrice] IS NULL AND [ConfirmedCurrencyCode] IS NULL AND [ReservationExpiresAtUtc] IS NULL)\n  OR ([Kind] IN (N'Confirmed',N'Expired') AND [BookingVersion]>1 AND [ConfirmedPrice] IS NOT NULL AND [ConfirmedPrice]>0 AND [ConfirmedCurrencyCode] IS NOT NULL AND LEN([ConfirmedCurrencyCode])=3 AND [ConfirmedCurrencyCode] COLLATE Latin1_General_100_BIN2 NOT LIKE N'%[^A-Z]%' AND [ReservationExpiresAtUtc] IS NOT NULL AND\n    (([Kind]=N'Confirmed' AND [Status]=N'Confirmed' AND [ActorKind]=N'User' AND [ActorAccountId] IS NOT NULL AND [ReasonCode] IS NULL AND [EffectiveCutoffUtc] IS NULL AND [DeveloperResponseId] IS NOT NULL AND [ReservationExpiresAtUtc]>[AtUtc]) OR\n     ([Kind]=N'Expired' AND [BookingVersion]>2 AND [Status]=N'Expired' AND [ActorKind]=N'System' AND [ActorAccountId] IS NULL AND [ReasonCode] IS NOT NULL AND [ReasonCode]=N'reservation-expired' AND [EffectiveCutoffUtc] IS NOT NULL AND [EffectiveCutoffUtc]=[ReservationExpiresAtUtc] AND [EffectiveCutoffUtc]<=[AtUtc] AND [DeveloperResponseId] IS NULL)))) AND [PreviousReservationExpiresAtUtc] IS NULL AND [ExtensionId] IS NULL) OR ([AtUtc]>'00010101' AND [Kind]=N'Extended' AND [BookingVersion]>2 AND [Status]=N'Confirmed' AND [ActorKind]=N'User' AND [ActorAccountId] IS NOT NULL AND [ReasonCode] IS NULL AND [EffectiveCutoffUtc] IS NULL AND [DeveloperResponseId] IS NULL AND [ExtensionId] IS NOT NULL AND [PreviousReservationExpiresAtUtc] IS NOT NULL AND [PreviousReservationExpiresAtUtc]>[AtUtc] AND [ReservationExpiresAtUtc] IS NOT NULL AND [ReservationExpiresAtUtc]>[PreviousReservationExpiresAtUtc] AND [ConfirmedPrice] IS NOT NULL AND [ConfirmedPrice]>0 AND [ConfirmedCurrencyCode] IS NOT NULL AND LEN([ConfirmedCurrencyCode])=3 AND [ConfirmedCurrencyCode] COLLATE Latin1_General_100_BIN2 NOT LIKE N'%[^A-Z]%')) AND [CancelledFromStatus] IS NULL AND [CancellationReason] IS NULL) OR ([AtUtc]>'00010101' AND [Kind]=N'Cancelled' AND [Status]=N'Cancelled' AND [BookingVersion]>1 AND [ActorKind]=N'User' AND [ActorAccountId] IS NOT NULL AND [CancelledFromStatus] IS NOT NULL AND ([CancellationReason] IS NULL OR LEN(LTRIM(RTRIM([CancellationReason])))>0) AND [ReasonCode] IS NULL AND [EffectiveCutoffUtc] IS NULL AND [DeveloperResponseId] IS NULL AND [PreviousReservationExpiresAtUtc] IS NULL AND [ExtensionId] IS NULL AND\n(([CancelledFromStatus]=N'PendingReview' AND [ConfirmedPrice] IS NULL AND [ConfirmedCurrencyCode] IS NULL AND [ReservationExpiresAtUtc] IS NULL) OR\n ([CancelledFromStatus]=N'Confirmed' AND [BookingVersion]>2 AND [ConfirmedPrice] IS NOT NULL AND [ConfirmedPrice]>0 AND [ConfirmedCurrencyCode] IS NOT NULL AND LEN([ConfirmedCurrencyCode])=3 AND [ConfirmedCurrencyCode] COLLATE Latin1_General_100_BIN2 NOT LIKE N'%[^A-Z]%' AND [ReservationExpiresAtUtc] IS NOT NULL AND [AtUtc]<[ReservationExpiresAtUtc])))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_BookingResponses_Decision",
                schema: "Crm",
                table: "BookingDeveloperResponses",
                sql: "([Decision]=N'Confirmed' AND [ResponseKind]=N'Confirmed' AND [StatusAfter]=N'Confirmed' AND [BookingVersionAfter]-[RequestedVersion]=1 AND [OfferedDeadlineUtc]>[RecordedAtUtc]) OR\n([Decision]=N'DeveloperRejected' AND [ResponseKind]=N'Rejected' AND [StatusAfter]=N'Rejected' AND [BookingVersionAfter]-[RequestedVersion]=1) OR\n([Decision]=N'InvalidDeadline' AND [ResponseKind]=N'Confirmed' AND [StatusAfter]=N'Rejected' AND [BookingVersionAfter]-[RequestedVersion]=1 AND [OfferedDeadlineUtc]<=[RecordedAtUtc]) OR\n([Decision]=N'LocalConflict' AND [ResponseKind]=N'Confirmed' AND [StatusAfter]=N'Rejected' AND [BookingVersionAfter]-[RequestedVersion]=1 AND [OfferedDeadlineUtc]>[RecordedAtUtc]) OR\n([Decision]=N'ResponseTimeout' AND [StatusAfter]=N'Rejected' AND [BookingVersionAfter]-[RequestedVersion]=1) OR\n([Decision]=N'ReservationExpired' AND [StatusAfter]=N'Expired' AND [BookingVersionAfter]-[RequestedVersion]=1) OR\n([Decision]=N'StateUnchanged' AND [StatusAfter] IN (N'Confirmed',N'Rejected',N'Expired',N'Cancelled') AND [BookingVersionAfter]=[RequestedVersion])");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Accounts_CancelledByAccountId",
                schema: "Crm",
                table: "Bookings",
                column: "CancelledByAccountId",
                principalSchema: "Identity",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Accounts_CancelledByAccountId",
                schema: "Crm",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_CancelledByAccountId",
                schema: "Crm",
                table: "Bookings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Bookings_LastExtension",
                schema: "Crm",
                table: "Bookings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Bookings_State",
                schema: "Crm",
                table: "Bookings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_BookingEvents_Shape",
                schema: "Crm",
                table: "BookingEvents");

            migrationBuilder.DropCheckConstraint(
                name: "CK_BookingResponses_Decision",
                schema: "Crm",
                table: "BookingDeveloperResponses");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                schema: "Crm",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "CancelledAtUtc",
                schema: "Crm",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "CancelledByAccountId",
                schema: "Crm",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "CancelledFromStatus",
                schema: "Crm",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                schema: "Crm",
                table: "BookingEvents");

            migrationBuilder.DropColumn(
                name: "CancelledFromStatus",
                schema: "Crm",
                table: "BookingEvents");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Bookings_LastExtension",
                schema: "Crm",
                table: "Bookings",
                sql: "([LastExtendedAtUtc] IS NULL AND [LastExtensionId] IS NULL) OR ([LastExtendedAtUtc] IS NOT NULL AND [LastExtensionId] IS NOT NULL AND [Status] IN (N'Confirmed',N'Expired') AND [Version]>2 AND [ConfirmedAtUtc] IS NOT NULL AND [ReservationExpiresAtUtc] IS NOT NULL AND [LastExtendedAtUtc]>=[ConfirmedAtUtc] AND [LastExtendedAtUtc]<[ReservationExpiresAtUtc])");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Bookings_State",
                schema: "Crm",
                table: "Bookings",
                sql: "([Status]=N'PendingReview' AND [Version]=1 AND [IsActiveRequest]=1 AND [RejectedAtUtc] IS NULL AND [RejectionReason] IS NULL AND [RejectedByAccountId] IS NULL AND [RejectionActorKind] IS NULL AND [ConfirmedAtUtc] IS NULL AND [ConfirmedByAccountId] IS NULL AND [ConfirmedPrice] IS NULL AND [ConfirmedCurrencyCode] IS NULL AND [ReservationExpiresAtUtc] IS NULL AND [ConfirmedResponseId] IS NULL AND [ExpiredAtUtc] IS NULL)\nOR ([Status]=N'Rejected' AND [Version]>1 AND [IsActiveRequest]=0 AND [RejectedAtUtc] IS NOT NULL AND [RejectionReason] IS NOT NULL AND [RejectionActorKind] IS NOT NULL AND [ConfirmedAtUtc] IS NULL AND [ConfirmedByAccountId] IS NULL AND [ConfirmedPrice] IS NULL AND [ConfirmedCurrencyCode] IS NULL AND [ReservationExpiresAtUtc] IS NULL AND [ConfirmedResponseId] IS NULL AND [ExpiredAtUtc] IS NULL AND\n    (([RejectedAtUtc]>=[ResponseDueAtUtc] AND [RejectionReason]=N'response-timeout' AND [RejectedByAccountId] IS NULL AND [RejectionActorKind]=N'System') OR\n    ([RejectedAtUtc]>=[RequestedAtUtc] AND [RejectedAtUtc]<[ResponseDueAtUtc] AND [RejectionReason] IN (N'developer-rejected',N'invalid-reservation-deadline',N'local-conflict') AND [RejectedByAccountId] IS NOT NULL AND [RejectionActorKind]=N'User')))\nOR ([Status] IN (N'Confirmed',N'Expired') AND [Version]>1 AND [RejectedAtUtc] IS NULL AND [RejectionReason] IS NULL AND [RejectedByAccountId] IS NULL AND [RejectionActorKind] IS NULL AND [ConfirmedAtUtc] IS NOT NULL AND [ConfirmedByAccountId] IS NOT NULL AND [ConfirmedPrice] IS NOT NULL AND [ConfirmedCurrencyCode] IS NOT NULL AND [ReservationExpiresAtUtc] IS NOT NULL AND [ConfirmedResponseId] IS NOT NULL AND\n    [ConfirmedAtUtc]>=[RequestedAtUtc] AND [ConfirmedAtUtc]<[ResponseDueAtUtc] AND [ConfirmedPrice]>0 AND LEN([ConfirmedCurrencyCode])=3 AND [ConfirmedCurrencyCode] COLLATE Latin1_General_100_BIN2 NOT LIKE N'%[^A-Z]%' AND [ReservationExpiresAtUtc]>[ConfirmedAtUtc] AND\n    (([Status]=N'Confirmed' AND [IsActiveRequest]=1 AND [ExpiredAtUtc] IS NULL) OR ([Status]=N'Expired' AND [Version]>2 AND [IsActiveRequest]=0 AND [ExpiredAtUtc] IS NOT NULL AND [ExpiredAtUtc]>=[ReservationExpiresAtUtc])))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_BookingEvents_Shape",
                schema: "Crm",
                table: "BookingEvents",
                sql: "([AtUtc]>'00010101' AND (\n  ([Kind]=N'Requested' AND [BookingVersion]=1 AND [Status]=N'PendingReview' AND [ActorKind]=N'User' AND [ActorAccountId] IS NOT NULL AND [ReasonCode] IS NULL AND [EffectiveCutoffUtc] IS NULL AND [DeveloperResponseId] IS NULL AND [ConfirmedPrice] IS NULL AND [ConfirmedCurrencyCode] IS NULL AND [ReservationExpiresAtUtc] IS NULL)\n  OR ([Kind]=N'ResponseTimedOut' AND [BookingVersion]>1 AND [Status]=N'Rejected' AND [ActorKind]=N'System' AND [ActorAccountId] IS NULL AND [ReasonCode] IS NOT NULL AND [ReasonCode]=N'response-timeout' AND [EffectiveCutoffUtc] IS NOT NULL AND [EffectiveCutoffUtc]>'00010101' AND [EffectiveCutoffUtc]<=[AtUtc] AND [DeveloperResponseId] IS NULL AND [ConfirmedPrice] IS NULL AND [ConfirmedCurrencyCode] IS NULL AND [ReservationExpiresAtUtc] IS NULL)\n  OR ([Kind]=N'Rejected' AND [BookingVersion]>1 AND [Status]=N'Rejected' AND [ActorKind]=N'User' AND [ActorAccountId] IS NOT NULL AND [ReasonCode] IS NOT NULL AND [ReasonCode] IN (N'developer-rejected',N'invalid-reservation-deadline',N'local-conflict') AND [EffectiveCutoffUtc] IS NULL AND [DeveloperResponseId] IS NOT NULL AND [ConfirmedPrice] IS NULL AND [ConfirmedCurrencyCode] IS NULL AND [ReservationExpiresAtUtc] IS NULL)\n  OR ([Kind] IN (N'Confirmed',N'Expired') AND [BookingVersion]>1 AND [ConfirmedPrice] IS NOT NULL AND [ConfirmedPrice]>0 AND [ConfirmedCurrencyCode] IS NOT NULL AND LEN([ConfirmedCurrencyCode])=3 AND [ConfirmedCurrencyCode] COLLATE Latin1_General_100_BIN2 NOT LIKE N'%[^A-Z]%' AND [ReservationExpiresAtUtc] IS NOT NULL AND\n    (([Kind]=N'Confirmed' AND [Status]=N'Confirmed' AND [ActorKind]=N'User' AND [ActorAccountId] IS NOT NULL AND [ReasonCode] IS NULL AND [EffectiveCutoffUtc] IS NULL AND [DeveloperResponseId] IS NOT NULL AND [ReservationExpiresAtUtc]>[AtUtc]) OR\n     ([Kind]=N'Expired' AND [BookingVersion]>2 AND [Status]=N'Expired' AND [ActorKind]=N'System' AND [ActorAccountId] IS NULL AND [ReasonCode] IS NOT NULL AND [ReasonCode]=N'reservation-expired' AND [EffectiveCutoffUtc] IS NOT NULL AND [EffectiveCutoffUtc]=[ReservationExpiresAtUtc] AND [EffectiveCutoffUtc]<=[AtUtc] AND [DeveloperResponseId] IS NULL)))) AND [PreviousReservationExpiresAtUtc] IS NULL AND [ExtensionId] IS NULL) OR ([AtUtc]>'00010101' AND [Kind]=N'Extended' AND [BookingVersion]>2 AND [Status]=N'Confirmed' AND [ActorKind]=N'User' AND [ActorAccountId] IS NOT NULL AND [ReasonCode] IS NULL AND [EffectiveCutoffUtc] IS NULL AND [DeveloperResponseId] IS NULL AND [ExtensionId] IS NOT NULL AND [PreviousReservationExpiresAtUtc] IS NOT NULL AND [PreviousReservationExpiresAtUtc]>[AtUtc] AND [ReservationExpiresAtUtc] IS NOT NULL AND [ReservationExpiresAtUtc]>[PreviousReservationExpiresAtUtc] AND [ConfirmedPrice] IS NOT NULL AND [ConfirmedPrice]>0 AND [ConfirmedCurrencyCode] IS NOT NULL AND LEN([ConfirmedCurrencyCode])=3 AND [ConfirmedCurrencyCode] COLLATE Latin1_General_100_BIN2 NOT LIKE N'%[^A-Z]%')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_BookingResponses_Decision",
                schema: "Crm",
                table: "BookingDeveloperResponses",
                sql: "([Decision]=N'Confirmed' AND [ResponseKind]=N'Confirmed' AND [StatusAfter]=N'Confirmed' AND [BookingVersionAfter]-[RequestedVersion]=1 AND [OfferedDeadlineUtc]>[RecordedAtUtc]) OR\n([Decision]=N'DeveloperRejected' AND [ResponseKind]=N'Rejected' AND [StatusAfter]=N'Rejected' AND [BookingVersionAfter]-[RequestedVersion]=1) OR\n([Decision]=N'InvalidDeadline' AND [ResponseKind]=N'Confirmed' AND [StatusAfter]=N'Rejected' AND [BookingVersionAfter]-[RequestedVersion]=1 AND [OfferedDeadlineUtc]<=[RecordedAtUtc]) OR\n([Decision]=N'LocalConflict' AND [ResponseKind]=N'Confirmed' AND [StatusAfter]=N'Rejected' AND [BookingVersionAfter]-[RequestedVersion]=1 AND [OfferedDeadlineUtc]>[RecordedAtUtc]) OR\n([Decision]=N'ResponseTimeout' AND [StatusAfter]=N'Rejected' AND [BookingVersionAfter]-[RequestedVersion]=1) OR\n([Decision]=N'ReservationExpired' AND [StatusAfter]=N'Expired' AND [BookingVersionAfter]-[RequestedVersion]=1) OR\n([Decision]=N'StateUnchanged' AND [StatusAfter] IN (N'Confirmed',N'Rejected',N'Expired') AND [BookingVersionAfter]=[RequestedVersion])");
        }
    }
}
