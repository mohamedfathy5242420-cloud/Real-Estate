using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrokerHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingExtensions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_BookingEvents_Shape",
                schema: "Crm",
                table: "BookingEvents");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastExtendedAtUtc",
                schema: "Crm",
                table: "Bookings",
                type: "datetime2(7)",
                precision: 7,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LastExtensionId",
                schema: "Crm",
                table: "Bookings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ExtensionId",
                schema: "Crm",
                table: "BookingEvents",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PreviousReservationExpiresAtUtc",
                schema: "Crm",
                table: "BookingEvents",
                type: "datetime2(7)",
                precision: 7,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BookingExtensions",
                schema: "Crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedVersion = table.Column<long>(type: "bigint", nullable: false),
                    BookingVersionAfter = table.Column<long>(type: "bigint", nullable: false),
                    PreviousReservationExpiresAtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    NewReservationExpiresAtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    DeveloperExtendedAtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    RecordedAtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    RecorderAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecorderEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Source = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingExtensions", x => x.Id);
                    table.UniqueConstraint("AK_BookingExtensions_Id_BookingId_CustomerId", x => new { x.Id, x.BookingId, x.CustomerId });
                    table.CheckConstraint("CK_BookingExtensions_ClockVersion", "[Version]=1 AND [RequestedVersion]>=2 AND [BookingVersionAfter]>[RequestedVersion] AND [BookingVersionAfter]-[RequestedVersion]=1 AND [DeveloperExtendedAtUtc]>'00010101' AND [RecordedAtUtc]>=[DeveloperExtendedAtUtc] AND [PreviousReservationExpiresAtUtc]>[RecordedAtUtc] AND [NewReservationExpiresAtUtc]>[PreviousReservationExpiresAtUtc]");
                    table.CheckConstraint("CK_BookingExtensions_Evidence", "LEN(LTRIM(RTRIM([Source])))>0 AND LEN(LTRIM(RTRIM([Reference])))>0 AND ([Reason] IS NULL OR LEN(LTRIM(RTRIM([Reason])))>0)");
                    table.ForeignKey(
                        name: "FK_BookingExtensions_Accounts_RecorderAccountId",
                        column: x => x.RecorderAccountId,
                        principalSchema: "Identity",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BookingExtensions_Bookings_BookingId_CustomerId_UnitId",
                        columns: x => new { x.BookingId, x.CustomerId, x.UnitId },
                        principalSchema: "Crm",
                        principalTable: "Bookings",
                        principalColumns: new[] { "Id", "CustomerId", "UnitId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BookingExtensions_Employees_RecorderEmployeeId",
                        column: x => x.RecorderEmployeeId,
                        principalSchema: "Crm",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_LastExtensionId_Id_CustomerId",
                schema: "Crm",
                table: "Bookings",
                columns: new[] { "LastExtensionId", "Id", "CustomerId" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Bookings_LastExtension",
                schema: "Crm",
                table: "Bookings",
                sql: "([LastExtendedAtUtc] IS NULL AND [LastExtensionId] IS NULL) OR ([LastExtendedAtUtc] IS NOT NULL AND [LastExtensionId] IS NOT NULL AND [Status] IN (N'Confirmed',N'Expired') AND [Version]>2 AND [ConfirmedAtUtc] IS NOT NULL AND [ReservationExpiresAtUtc] IS NOT NULL AND [LastExtendedAtUtc]>=[ConfirmedAtUtc] AND [LastExtendedAtUtc]<[ReservationExpiresAtUtc])");

            migrationBuilder.CreateIndex(
                name: "IX_BookingEvents_ExtensionId_BookingId_CustomerId",
                schema: "Crm",
                table: "BookingEvents",
                columns: new[] { "ExtensionId", "BookingId", "CustomerId" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_BookingEvents_Shape",
                schema: "Crm",
                table: "BookingEvents",
                sql: "([AtUtc]>'00010101' AND (\n  ([Kind]=N'Requested' AND [BookingVersion]=1 AND [Status]=N'PendingReview' AND [ActorKind]=N'User' AND [ActorAccountId] IS NOT NULL AND [ReasonCode] IS NULL AND [EffectiveCutoffUtc] IS NULL AND [DeveloperResponseId] IS NULL AND [ConfirmedPrice] IS NULL AND [ConfirmedCurrencyCode] IS NULL AND [ReservationExpiresAtUtc] IS NULL)\n  OR ([Kind]=N'ResponseTimedOut' AND [BookingVersion]>1 AND [Status]=N'Rejected' AND [ActorKind]=N'System' AND [ActorAccountId] IS NULL AND [ReasonCode] IS NOT NULL AND [ReasonCode]=N'response-timeout' AND [EffectiveCutoffUtc] IS NOT NULL AND [EffectiveCutoffUtc]>'00010101' AND [EffectiveCutoffUtc]<=[AtUtc] AND [DeveloperResponseId] IS NULL AND [ConfirmedPrice] IS NULL AND [ConfirmedCurrencyCode] IS NULL AND [ReservationExpiresAtUtc] IS NULL)\n  OR ([Kind]=N'Rejected' AND [BookingVersion]>1 AND [Status]=N'Rejected' AND [ActorKind]=N'User' AND [ActorAccountId] IS NOT NULL AND [ReasonCode] IS NOT NULL AND [ReasonCode] IN (N'developer-rejected',N'invalid-reservation-deadline',N'local-conflict') AND [EffectiveCutoffUtc] IS NULL AND [DeveloperResponseId] IS NOT NULL AND [ConfirmedPrice] IS NULL AND [ConfirmedCurrencyCode] IS NULL AND [ReservationExpiresAtUtc] IS NULL)\n  OR ([Kind] IN (N'Confirmed',N'Expired') AND [BookingVersion]>1 AND [ConfirmedPrice] IS NOT NULL AND [ConfirmedPrice]>0 AND [ConfirmedCurrencyCode] IS NOT NULL AND LEN([ConfirmedCurrencyCode])=3 AND [ConfirmedCurrencyCode] COLLATE Latin1_General_100_BIN2 NOT LIKE N'%[^A-Z]%' AND [ReservationExpiresAtUtc] IS NOT NULL AND\n    (([Kind]=N'Confirmed' AND [Status]=N'Confirmed' AND [ActorKind]=N'User' AND [ActorAccountId] IS NOT NULL AND [ReasonCode] IS NULL AND [EffectiveCutoffUtc] IS NULL AND [DeveloperResponseId] IS NOT NULL AND [ReservationExpiresAtUtc]>[AtUtc]) OR\n     ([Kind]=N'Expired' AND [BookingVersion]>2 AND [Status]=N'Expired' AND [ActorKind]=N'System' AND [ActorAccountId] IS NULL AND [ReasonCode] IS NOT NULL AND [ReasonCode]=N'reservation-expired' AND [EffectiveCutoffUtc] IS NOT NULL AND [EffectiveCutoffUtc]=[ReservationExpiresAtUtc] AND [EffectiveCutoffUtc]<=[AtUtc] AND [DeveloperResponseId] IS NULL)))) AND [PreviousReservationExpiresAtUtc] IS NULL AND [ExtensionId] IS NULL) OR ([AtUtc]>'00010101' AND [Kind]=N'Extended' AND [BookingVersion]>2 AND [Status]=N'Confirmed' AND [ActorKind]=N'User' AND [ActorAccountId] IS NOT NULL AND [ReasonCode] IS NULL AND [EffectiveCutoffUtc] IS NULL AND [DeveloperResponseId] IS NULL AND [ExtensionId] IS NOT NULL AND [PreviousReservationExpiresAtUtc] IS NOT NULL AND [PreviousReservationExpiresAtUtc]>[AtUtc] AND [ReservationExpiresAtUtc] IS NOT NULL AND [ReservationExpiresAtUtc]>[PreviousReservationExpiresAtUtc] AND [ConfirmedPrice] IS NOT NULL AND [ConfirmedPrice]>0 AND [ConfirmedCurrencyCode] IS NOT NULL AND LEN([ConfirmedCurrencyCode])=3 AND [ConfirmedCurrencyCode] COLLATE Latin1_General_100_BIN2 NOT LIKE N'%[^A-Z]%')");

            migrationBuilder.CreateIndex(
                name: "IX_BookingExtensions_BookingId_CustomerId_UnitId",
                schema: "Crm",
                table: "BookingExtensions",
                columns: new[] { "BookingId", "CustomerId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_BookingExtensions_BookingId_RecordedAtUtc_Id",
                schema: "Crm",
                table: "BookingExtensions",
                columns: new[] { "BookingId", "RecordedAtUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_BookingExtensions_RecorderAccountId",
                schema: "Crm",
                table: "BookingExtensions",
                column: "RecorderAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingExtensions_RecorderEmployeeId",
                schema: "Crm",
                table: "BookingExtensions",
                column: "RecorderEmployeeId");

            migrationBuilder.AddForeignKey(
                name: "FK_BookingEvents_BookingExtensions_ExtensionId_BookingId_CustomerId",
                schema: "Crm",
                table: "BookingEvents",
                columns: new[] { "ExtensionId", "BookingId", "CustomerId" },
                principalSchema: "Crm",
                principalTable: "BookingExtensions",
                principalColumns: new[] { "Id", "BookingId", "CustomerId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_BookingExtensions_LastExtensionId_Id_CustomerId",
                schema: "Crm",
                table: "Bookings",
                columns: new[] { "LastExtensionId", "Id", "CustomerId" },
                principalSchema: "Crm",
                principalTable: "BookingExtensions",
                principalColumns: new[] { "Id", "BookingId", "CustomerId" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BookingEvents_BookingExtensions_ExtensionId_BookingId_CustomerId",
                schema: "Crm",
                table: "BookingEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_BookingExtensions_LastExtensionId_Id_CustomerId",
                schema: "Crm",
                table: "Bookings");

            migrationBuilder.DropTable(
                name: "BookingExtensions",
                schema: "Crm");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_LastExtensionId_Id_CustomerId",
                schema: "Crm",
                table: "Bookings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Bookings_LastExtension",
                schema: "Crm",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_BookingEvents_ExtensionId_BookingId_CustomerId",
                schema: "Crm",
                table: "BookingEvents");

            migrationBuilder.DropCheckConstraint(
                name: "CK_BookingEvents_Shape",
                schema: "Crm",
                table: "BookingEvents");

            migrationBuilder.DropColumn(
                name: "LastExtendedAtUtc",
                schema: "Crm",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "LastExtensionId",
                schema: "Crm",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ExtensionId",
                schema: "Crm",
                table: "BookingEvents");

            migrationBuilder.DropColumn(
                name: "PreviousReservationExpiresAtUtc",
                schema: "Crm",
                table: "BookingEvents");

            migrationBuilder.AddCheckConstraint(
                name: "CK_BookingEvents_Shape",
                schema: "Crm",
                table: "BookingEvents",
                sql: "[AtUtc]>'00010101' AND (\n  ([Kind]=N'Requested' AND [BookingVersion]=1 AND [Status]=N'PendingReview' AND [ActorKind]=N'User' AND [ActorAccountId] IS NOT NULL AND [ReasonCode] IS NULL AND [EffectiveCutoffUtc] IS NULL AND [DeveloperResponseId] IS NULL AND [ConfirmedPrice] IS NULL AND [ConfirmedCurrencyCode] IS NULL AND [ReservationExpiresAtUtc] IS NULL)\n  OR ([Kind]=N'ResponseTimedOut' AND [BookingVersion]>1 AND [Status]=N'Rejected' AND [ActorKind]=N'System' AND [ActorAccountId] IS NULL AND [ReasonCode] IS NOT NULL AND [ReasonCode]=N'response-timeout' AND [EffectiveCutoffUtc] IS NOT NULL AND [EffectiveCutoffUtc]>'00010101' AND [EffectiveCutoffUtc]<=[AtUtc] AND [DeveloperResponseId] IS NULL AND [ConfirmedPrice] IS NULL AND [ConfirmedCurrencyCode] IS NULL AND [ReservationExpiresAtUtc] IS NULL)\n  OR ([Kind]=N'Rejected' AND [BookingVersion]>1 AND [Status]=N'Rejected' AND [ActorKind]=N'User' AND [ActorAccountId] IS NOT NULL AND [ReasonCode] IS NOT NULL AND [ReasonCode] IN (N'developer-rejected',N'invalid-reservation-deadline',N'local-conflict') AND [EffectiveCutoffUtc] IS NULL AND [DeveloperResponseId] IS NOT NULL AND [ConfirmedPrice] IS NULL AND [ConfirmedCurrencyCode] IS NULL AND [ReservationExpiresAtUtc] IS NULL)\n  OR ([Kind] IN (N'Confirmed',N'Expired') AND [BookingVersion]>1 AND [ConfirmedPrice] IS NOT NULL AND [ConfirmedPrice]>0 AND [ConfirmedCurrencyCode] IS NOT NULL AND LEN([ConfirmedCurrencyCode])=3 AND [ConfirmedCurrencyCode] COLLATE Latin1_General_100_BIN2 NOT LIKE N'%[^A-Z]%' AND [ReservationExpiresAtUtc] IS NOT NULL AND\n    (([Kind]=N'Confirmed' AND [Status]=N'Confirmed' AND [ActorKind]=N'User' AND [ActorAccountId] IS NOT NULL AND [ReasonCode] IS NULL AND [EffectiveCutoffUtc] IS NULL AND [DeveloperResponseId] IS NOT NULL AND [ReservationExpiresAtUtc]>[AtUtc]) OR\n     ([Kind]=N'Expired' AND [BookingVersion]>2 AND [Status]=N'Expired' AND [ActorKind]=N'System' AND [ActorAccountId] IS NULL AND [ReasonCode] IS NOT NULL AND [ReasonCode]=N'reservation-expired' AND [EffectiveCutoffUtc] IS NOT NULL AND [EffectiveCutoffUtc]=[ReservationExpiresAtUtc] AND [EffectiveCutoffUtc]<=[AtUtc] AND [DeveloperResponseId] IS NULL))))");
        }
    }
}
