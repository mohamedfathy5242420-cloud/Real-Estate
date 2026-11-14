using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrokerHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingResponses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Bookings_State",
                schema: "Crm",
                table: "Bookings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_BookingEvents_Shape",
                schema: "Crm",
                table: "BookingEvents");

            migrationBuilder.AddColumn<DateTime>(
                name: "ConfirmedAtUtc",
                schema: "Crm",
                table: "Bookings",
                type: "datetime2(7)",
                precision: 7,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ConfirmedByAccountId",
                schema: "Crm",
                table: "Bookings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConfirmedCurrencyCode",
                schema: "Crm",
                table: "Bookings",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ConfirmedPrice",
                schema: "Crm",
                table: "Bookings",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ConfirmedResponseId",
                schema: "Crm",
                table: "Bookings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiredAtUtc",
                schema: "Crm",
                table: "Bookings",
                type: "datetime2(7)",
                precision: 7,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReservationExpiresAtUtc",
                schema: "Crm",
                table: "Bookings",
                type: "datetime2(7)",
                precision: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConfirmedCurrencyCode",
                schema: "Crm",
                table: "BookingEvents",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ConfirmedPrice",
                schema: "Crm",
                table: "BookingEvents",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeveloperResponseId",
                schema: "Crm",
                table: "BookingEvents",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReservationExpiresAtUtc",
                schema: "Crm",
                table: "BookingEvents",
                type: "datetime2(7)",
                precision: 7,
                nullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Bookings_Id_CustomerId_UnitId",
                schema: "Crm",
                table: "Bookings",
                columns: new[] { "Id", "CustomerId", "UnitId" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Bookings_Id_UnitId",
                schema: "Crm",
                table: "Bookings",
                columns: new[] { "Id", "UnitId" });

            migrationBuilder.CreateTable(
                name: "BookingDeveloperResponses",
                schema: "Crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedVersion = table.Column<long>(type: "bigint", nullable: false),
                    BookingVersionAfter = table.Column<long>(type: "bigint", nullable: false),
                    ResponseKind = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Decision = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    StatusAfter = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    DeveloperRespondedAtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    RecordedAtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    RecorderAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecorderEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Source = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    OfferedPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    OfferedDeadlineUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingDeveloperResponses", x => x.Id);
                    table.UniqueConstraint("AK_BookingDeveloperResponses_Id_BookingId_CustomerId", x => new { x.Id, x.BookingId, x.CustomerId });
                    table.CheckConstraint("CK_BookingResponses_Decision", "([Decision]=N'Confirmed' AND [ResponseKind]=N'Confirmed' AND [StatusAfter]=N'Confirmed' AND [BookingVersionAfter]-[RequestedVersion]=1 AND [OfferedDeadlineUtc]>[RecordedAtUtc]) OR\n([Decision]=N'DeveloperRejected' AND [ResponseKind]=N'Rejected' AND [StatusAfter]=N'Rejected' AND [BookingVersionAfter]-[RequestedVersion]=1) OR\n([Decision]=N'InvalidDeadline' AND [ResponseKind]=N'Confirmed' AND [StatusAfter]=N'Rejected' AND [BookingVersionAfter]-[RequestedVersion]=1 AND [OfferedDeadlineUtc]<=[RecordedAtUtc]) OR\n([Decision]=N'LocalConflict' AND [ResponseKind]=N'Confirmed' AND [StatusAfter]=N'Rejected' AND [BookingVersionAfter]-[RequestedVersion]=1 AND [OfferedDeadlineUtc]>[RecordedAtUtc]) OR\n([Decision]=N'ResponseTimeout' AND [StatusAfter]=N'Rejected' AND [BookingVersionAfter]-[RequestedVersion]=1) OR\n([Decision]=N'ReservationExpired' AND [StatusAfter]=N'Expired' AND [BookingVersionAfter]-[RequestedVersion]=1) OR\n([Decision]=N'StateUnchanged' AND [StatusAfter] IN (N'Confirmed',N'Rejected',N'Expired') AND [BookingVersionAfter]=[RequestedVersion])");
                    table.CheckConstraint("CK_BookingResponses_Facts", "[Version]=1 AND [RequestedVersion]>0 AND [BookingVersionAfter]>=[RequestedVersion] AND [BookingVersionAfter]-[RequestedVersion]<=1 AND\n[DeveloperRespondedAtUtc]>'00010101' AND [RecordedAtUtc]>=[DeveloperRespondedAtUtc] AND LEN(LTRIM(RTRIM([Source])))>0 AND LEN(LTRIM(RTRIM([Reference])))>0 AND\n(([ResponseKind]=N'Confirmed' AND [OfferedPrice] IS NOT NULL AND [OfferedPrice]>0 AND [CurrencyCode] IS NOT NULL AND LEN([CurrencyCode])=3 AND [CurrencyCode] COLLATE Latin1_General_100_BIN2 NOT LIKE N'%[^A-Z]%' AND [OfferedDeadlineUtc] IS NOT NULL AND [OfferedDeadlineUtc]>'00010101' AND [Reason] IS NULL)\n OR ([ResponseKind]=N'Rejected' AND [OfferedPrice] IS NULL AND [CurrencyCode] IS NULL AND [OfferedDeadlineUtc] IS NULL AND [Reason] IS NOT NULL AND LEN(LTRIM(RTRIM([Reason])))>0))");
                    table.ForeignKey(
                        name: "FK_BookingDeveloperResponses_Accounts_RecorderAccountId",
                        column: x => x.RecorderAccountId,
                        principalSchema: "Identity",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BookingDeveloperResponses_Bookings_BookingId_CustomerId_UnitId",
                        columns: x => new { x.BookingId, x.CustomerId, x.UnitId },
                        principalSchema: "Crm",
                        principalTable: "Bookings",
                        principalColumns: new[] { "Id", "CustomerId", "UnitId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BookingDeveloperResponses_Employees_RecorderEmployeeId",
                        column: x => x.RecorderEmployeeId,
                        principalSchema: "Crm",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BookingUnitClaims",
                schema: "Crm",
                columns: table => new
                {
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingUnitClaims", x => x.UnitId);
                    table.CheckConstraint("CK_BookingUnitClaims_Shape", "[Version]>0 AND [UpdatedAtUtc]>'00010101' AND (([BookingId] IS NULL AND [ExpiresAtUtc] IS NULL) OR ([BookingId] IS NOT NULL AND [ExpiresAtUtc] IS NOT NULL AND [ExpiresAtUtc]>[UpdatedAtUtc]))");
                    table.ForeignKey(
                        name: "FK_BookingUnitClaims_Bookings_BookingId_UnitId",
                        columns: x => new { x.BookingId, x.UnitId },
                        principalSchema: "Crm",
                        principalTable: "Bookings",
                        principalColumns: new[] { "Id", "UnitId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BookingUnitClaims_Units_UnitId",
                        column: x => x.UnitId,
                        principalSchema: "Catalog",
                        principalTable: "Units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_ConfirmedByAccountId",
                schema: "Crm",
                table: "Bookings",
                column: "ConfirmedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_ConfirmedResponseId_Id_CustomerId",
                schema: "Crm",
                table: "Bookings",
                columns: new[] { "ConfirmedResponseId", "Id", "CustomerId" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Bookings_State",
                schema: "Crm",
                table: "Bookings",
                sql: "([Status]=N'PendingReview' AND [Version]=1 AND [IsActiveRequest]=1 AND [RejectedAtUtc] IS NULL AND [RejectionReason] IS NULL AND [RejectedByAccountId] IS NULL AND [RejectionActorKind] IS NULL AND [ConfirmedAtUtc] IS NULL AND [ConfirmedByAccountId] IS NULL AND [ConfirmedPrice] IS NULL AND [ConfirmedCurrencyCode] IS NULL AND [ReservationExpiresAtUtc] IS NULL AND [ConfirmedResponseId] IS NULL AND [ExpiredAtUtc] IS NULL)\nOR ([Status]=N'Rejected' AND [Version]>1 AND [IsActiveRequest]=0 AND [RejectedAtUtc] IS NOT NULL AND [RejectionReason] IS NOT NULL AND [RejectionActorKind] IS NOT NULL AND [ConfirmedAtUtc] IS NULL AND [ConfirmedByAccountId] IS NULL AND [ConfirmedPrice] IS NULL AND [ConfirmedCurrencyCode] IS NULL AND [ReservationExpiresAtUtc] IS NULL AND [ConfirmedResponseId] IS NULL AND [ExpiredAtUtc] IS NULL AND\n    (([RejectedAtUtc]>=[ResponseDueAtUtc] AND [RejectionReason]=N'response-timeout' AND [RejectedByAccountId] IS NULL AND [RejectionActorKind]=N'System') OR\n    ([RejectedAtUtc]>=[RequestedAtUtc] AND [RejectedAtUtc]<[ResponseDueAtUtc] AND [RejectionReason] IN (N'developer-rejected',N'invalid-reservation-deadline',N'local-conflict') AND [RejectedByAccountId] IS NOT NULL AND [RejectionActorKind]=N'User')))\nOR ([Status] IN (N'Confirmed',N'Expired') AND [Version]>1 AND [RejectedAtUtc] IS NULL AND [RejectionReason] IS NULL AND [RejectedByAccountId] IS NULL AND [RejectionActorKind] IS NULL AND [ConfirmedAtUtc] IS NOT NULL AND [ConfirmedByAccountId] IS NOT NULL AND [ConfirmedPrice] IS NOT NULL AND [ConfirmedCurrencyCode] IS NOT NULL AND [ReservationExpiresAtUtc] IS NOT NULL AND [ConfirmedResponseId] IS NOT NULL AND\n    [ConfirmedAtUtc]>=[RequestedAtUtc] AND [ConfirmedAtUtc]<[ResponseDueAtUtc] AND [ConfirmedPrice]>0 AND LEN([ConfirmedCurrencyCode])=3 AND [ConfirmedCurrencyCode] COLLATE Latin1_General_100_BIN2 NOT LIKE N'%[^A-Z]%' AND [ReservationExpiresAtUtc]>[ConfirmedAtUtc] AND\n    (([Status]=N'Confirmed' AND [IsActiveRequest]=1 AND [ExpiredAtUtc] IS NULL) OR ([Status]=N'Expired' AND [Version]>2 AND [IsActiveRequest]=0 AND [ExpiredAtUtc] IS NOT NULL AND [ExpiredAtUtc]>=[ReservationExpiresAtUtc])))");

            migrationBuilder.CreateIndex(
                name: "IX_BookingEvents_DeveloperResponseId_BookingId_CustomerId",
                schema: "Crm",
                table: "BookingEvents",
                columns: new[] { "DeveloperResponseId", "BookingId", "CustomerId" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_BookingEvents_Shape",
                schema: "Crm",
                table: "BookingEvents",
                sql: "[AtUtc]>'00010101' AND (\n  ([Kind]=N'Requested' AND [BookingVersion]=1 AND [Status]=N'PendingReview' AND [ActorKind]=N'User' AND [ActorAccountId] IS NOT NULL AND [ReasonCode] IS NULL AND [EffectiveCutoffUtc] IS NULL AND [DeveloperResponseId] IS NULL AND [ConfirmedPrice] IS NULL AND [ConfirmedCurrencyCode] IS NULL AND [ReservationExpiresAtUtc] IS NULL)\n  OR ([Kind]=N'ResponseTimedOut' AND [BookingVersion]>1 AND [Status]=N'Rejected' AND [ActorKind]=N'System' AND [ActorAccountId] IS NULL AND [ReasonCode] IS NOT NULL AND [ReasonCode]=N'response-timeout' AND [EffectiveCutoffUtc] IS NOT NULL AND [EffectiveCutoffUtc]>'00010101' AND [EffectiveCutoffUtc]<=[AtUtc] AND [DeveloperResponseId] IS NULL AND [ConfirmedPrice] IS NULL AND [ConfirmedCurrencyCode] IS NULL AND [ReservationExpiresAtUtc] IS NULL)\n  OR ([Kind]=N'Rejected' AND [BookingVersion]>1 AND [Status]=N'Rejected' AND [ActorKind]=N'User' AND [ActorAccountId] IS NOT NULL AND [ReasonCode] IS NOT NULL AND [ReasonCode] IN (N'developer-rejected',N'invalid-reservation-deadline',N'local-conflict') AND [EffectiveCutoffUtc] IS NULL AND [DeveloperResponseId] IS NOT NULL AND [ConfirmedPrice] IS NULL AND [ConfirmedCurrencyCode] IS NULL AND [ReservationExpiresAtUtc] IS NULL)\n  OR ([Kind] IN (N'Confirmed',N'Expired') AND [BookingVersion]>1 AND [ConfirmedPrice] IS NOT NULL AND [ConfirmedPrice]>0 AND [ConfirmedCurrencyCode] IS NOT NULL AND LEN([ConfirmedCurrencyCode])=3 AND [ConfirmedCurrencyCode] COLLATE Latin1_General_100_BIN2 NOT LIKE N'%[^A-Z]%' AND [ReservationExpiresAtUtc] IS NOT NULL AND\n    (([Kind]=N'Confirmed' AND [Status]=N'Confirmed' AND [ActorKind]=N'User' AND [ActorAccountId] IS NOT NULL AND [ReasonCode] IS NULL AND [EffectiveCutoffUtc] IS NULL AND [DeveloperResponseId] IS NOT NULL AND [ReservationExpiresAtUtc]>[AtUtc]) OR\n     ([Kind]=N'Expired' AND [BookingVersion]>2 AND [Status]=N'Expired' AND [ActorKind]=N'System' AND [ActorAccountId] IS NULL AND [ReasonCode] IS NOT NULL AND [ReasonCode]=N'reservation-expired' AND [EffectiveCutoffUtc] IS NOT NULL AND [EffectiveCutoffUtc]=[ReservationExpiresAtUtc] AND [EffectiveCutoffUtc]<=[AtUtc] AND [DeveloperResponseId] IS NULL))))");

            migrationBuilder.CreateIndex(
                name: "IX_BookingDeveloperResponses_BookingId_CustomerId_UnitId",
                schema: "Crm",
                table: "BookingDeveloperResponses",
                columns: new[] { "BookingId", "CustomerId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_BookingDeveloperResponses_BookingId_RecordedAtUtc_Id",
                schema: "Crm",
                table: "BookingDeveloperResponses",
                columns: new[] { "BookingId", "RecordedAtUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_BookingDeveloperResponses_RecorderAccountId",
                schema: "Crm",
                table: "BookingDeveloperResponses",
                column: "RecorderAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingDeveloperResponses_RecorderEmployeeId",
                schema: "Crm",
                table: "BookingDeveloperResponses",
                column: "RecorderEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingUnitClaims_BookingId_UnitId",
                schema: "Crm",
                table: "BookingUnitClaims",
                columns: new[] { "BookingId", "UnitId" });

            migrationBuilder.AddForeignKey(
                name: "FK_BookingEvents_BookingDeveloperResponses_DeveloperResponseId_BookingId_CustomerId",
                schema: "Crm",
                table: "BookingEvents",
                columns: new[] { "DeveloperResponseId", "BookingId", "CustomerId" },
                principalSchema: "Crm",
                principalTable: "BookingDeveloperResponses",
                principalColumns: new[] { "Id", "BookingId", "CustomerId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Accounts_ConfirmedByAccountId",
                schema: "Crm",
                table: "Bookings",
                column: "ConfirmedByAccountId",
                principalSchema: "Identity",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_BookingDeveloperResponses_ConfirmedResponseId_Id_CustomerId",
                schema: "Crm",
                table: "Bookings",
                columns: new[] { "ConfirmedResponseId", "Id", "CustomerId" },
                principalSchema: "Crm",
                principalTable: "BookingDeveloperResponses",
                principalColumns: new[] { "Id", "BookingId", "CustomerId" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BookingEvents_BookingDeveloperResponses_DeveloperResponseId_BookingId_CustomerId",
                schema: "Crm",
                table: "BookingEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Accounts_ConfirmedByAccountId",
                schema: "Crm",
                table: "Bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_BookingDeveloperResponses_ConfirmedResponseId_Id_CustomerId",
                schema: "Crm",
                table: "Bookings");

            migrationBuilder.DropTable(
                name: "BookingDeveloperResponses",
                schema: "Crm");

            migrationBuilder.DropTable(
                name: "BookingUnitClaims",
                schema: "Crm");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Bookings_Id_CustomerId_UnitId",
                schema: "Crm",
                table: "Bookings");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Bookings_Id_UnitId",
                schema: "Crm",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_ConfirmedByAccountId",
                schema: "Crm",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_ConfirmedResponseId_Id_CustomerId",
                schema: "Crm",
                table: "Bookings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Bookings_State",
                schema: "Crm",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_BookingEvents_DeveloperResponseId_BookingId_CustomerId",
                schema: "Crm",
                table: "BookingEvents");

            migrationBuilder.DropCheckConstraint(
                name: "CK_BookingEvents_Shape",
                schema: "Crm",
                table: "BookingEvents");

            migrationBuilder.DropColumn(
                name: "ConfirmedAtUtc",
                schema: "Crm",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ConfirmedByAccountId",
                schema: "Crm",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ConfirmedCurrencyCode",
                schema: "Crm",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ConfirmedPrice",
                schema: "Crm",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ConfirmedResponseId",
                schema: "Crm",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ExpiredAtUtc",
                schema: "Crm",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ReservationExpiresAtUtc",
                schema: "Crm",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ConfirmedCurrencyCode",
                schema: "Crm",
                table: "BookingEvents");

            migrationBuilder.DropColumn(
                name: "ConfirmedPrice",
                schema: "Crm",
                table: "BookingEvents");

            migrationBuilder.DropColumn(
                name: "DeveloperResponseId",
                schema: "Crm",
                table: "BookingEvents");

            migrationBuilder.DropColumn(
                name: "ReservationExpiresAtUtc",
                schema: "Crm",
                table: "BookingEvents");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Bookings_State",
                schema: "Crm",
                table: "Bookings",
                sql: "([Status]=N'PendingReview' AND [Version]=1 AND [IsActiveRequest]=1 AND [RejectedAtUtc] IS NULL AND [RejectionReason] IS NULL AND [RejectedByAccountId] IS NULL AND [RejectionActorKind] IS NULL) OR ([Status]=N'Rejected' AND [Version]>1 AND [IsActiveRequest]=0 AND [RejectedAtUtc] IS NOT NULL AND [RejectedAtUtc]>=[ResponseDueAtUtc] AND [RejectionReason] IS NOT NULL AND [RejectionReason]=N'response-timeout' AND [RejectedByAccountId] IS NULL AND [RejectionActorKind] IS NOT NULL AND [RejectionActorKind]=N'System')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_BookingEvents_Shape",
                schema: "Crm",
                table: "BookingEvents",
                sql: "[AtUtc]>'00010101' AND (([Kind]=N'Requested' AND [BookingVersion]=1 AND [Status]=N'PendingReview' AND [ActorKind]=N'User' AND [ActorAccountId] IS NOT NULL AND [ReasonCode] IS NULL AND [EffectiveCutoffUtc] IS NULL) OR ([Kind]=N'ResponseTimedOut' AND [BookingVersion]>1 AND [Status]=N'Rejected' AND [ActorKind]=N'System' AND [ActorAccountId] IS NULL AND [ReasonCode] IS NOT NULL AND [ReasonCode]=N'response-timeout' AND [EffectiveCutoffUtc] IS NOT NULL AND [EffectiveCutoffUtc]>'00010101' AND [EffectiveCutoffUtc]<=[AtUtc]))");
        }
    }
}
