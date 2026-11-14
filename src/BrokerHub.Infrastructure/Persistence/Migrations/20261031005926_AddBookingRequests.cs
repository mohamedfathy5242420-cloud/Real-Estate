using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrokerHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Bookings",
                schema: "Crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LeadId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    RequestedAtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    RequestedByAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResponseDueAtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    SubmissionAgreementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmissionLocalDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TimeZoneId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RequestReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    RejectedAtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    RejectedByAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RejectionActorKind = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    IsActiveRequest = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bookings", x => x.Id);
                    table.UniqueConstraint("AK_Bookings_Id_CustomerId", x => new { x.Id, x.CustomerId });
                    table.CheckConstraint("CK_Bookings_LocalContext", "[SubmissionLocalDate]>'00010101' AND LEN(LTRIM(RTRIM([TimeZoneId])))>0");
                    table.CheckConstraint("CK_Bookings_RequestReason", "[RequestReason] IS NULL OR LEN(LTRIM(RTRIM([RequestReason])))>0");
                    table.CheckConstraint("CK_Bookings_ResponseClock", "[RequestedAtUtc]>'00010101' AND [ResponseDueAtUtc]=DATEADD(hour,72,[RequestedAtUtc])");
                    table.CheckConstraint("CK_Bookings_State", "([Status]=N'PendingReview' AND [Version]=1 AND [IsActiveRequest]=1 AND [RejectedAtUtc] IS NULL AND [RejectionReason] IS NULL AND [RejectedByAccountId] IS NULL AND [RejectionActorKind] IS NULL) OR ([Status]=N'Rejected' AND [Version]>1 AND [IsActiveRequest]=0 AND [RejectedAtUtc] IS NOT NULL AND [RejectedAtUtc]>=[ResponseDueAtUtc] AND [RejectionReason] IS NOT NULL AND [RejectionReason]=N'response-timeout' AND [RejectedByAccountId] IS NULL AND [RejectionActorKind] IS NOT NULL AND [RejectionActorKind]=N'System')");
                    table.ForeignKey(
                        name: "FK_Bookings_Accounts_RejectedByAccountId",
                        column: x => x.RejectedByAccountId,
                        principalSchema: "Identity",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Bookings_Accounts_RequestedByAccountId",
                        column: x => x.RequestedByAccountId,
                        principalSchema: "Identity",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Bookings_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "Crm",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Bookings_Leads_LeadId_CustomerId_ProjectId",
                        columns: x => new { x.LeadId, x.CustomerId, x.ProjectId },
                        principalSchema: "Crm",
                        principalTable: "Leads",
                        principalColumns: new[] { "Id", "CustomerId", "ProjectId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Bookings_MarketingAgreements_SubmissionAgreementId",
                        column: x => x.SubmissionAgreementId,
                        principalSchema: "Catalog",
                        principalTable: "MarketingAgreements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Bookings_Units_UnitId_ProjectId",
                        columns: x => new { x.UnitId, x.ProjectId },
                        principalSchema: "Catalog",
                        principalTable: "Units",
                        principalColumns: new[] { "Id", "ProjectId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BookingEvents",
                schema: "Crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookingVersion = table.Column<long>(type: "bigint", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ActorKind = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    ActorAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ReasonCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    EffectiveCutoffUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingEvents", x => x.Id);
                    table.CheckConstraint("CK_BookingEvents_Shape", "[AtUtc]>'00010101' AND (([Kind]=N'Requested' AND [BookingVersion]=1 AND [Status]=N'PendingReview' AND [ActorKind]=N'User' AND [ActorAccountId] IS NOT NULL AND [ReasonCode] IS NULL AND [EffectiveCutoffUtc] IS NULL) OR ([Kind]=N'ResponseTimedOut' AND [BookingVersion]>1 AND [Status]=N'Rejected' AND [ActorKind]=N'System' AND [ActorAccountId] IS NULL AND [ReasonCode] IS NOT NULL AND [ReasonCode]=N'response-timeout' AND [EffectiveCutoffUtc] IS NOT NULL AND [EffectiveCutoffUtc]>'00010101' AND [EffectiveCutoffUtc]<=[AtUtc]))");
                    table.ForeignKey(
                        name: "FK_BookingEvents_Accounts_ActorAccountId",
                        column: x => x.ActorAccountId,
                        principalSchema: "Identity",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BookingEvents_Bookings_BookingId_CustomerId",
                        columns: x => new { x.BookingId, x.CustomerId },
                        principalSchema: "Crm",
                        principalTable: "Bookings",
                        principalColumns: new[] { "Id", "CustomerId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BookingEvents_ActorAccountId",
                schema: "Crm",
                table: "BookingEvents",
                column: "ActorAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingEvents_BookingId_BookingVersion",
                schema: "Crm",
                table: "BookingEvents",
                columns: new[] { "BookingId", "BookingVersion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BookingEvents_BookingId_CustomerId",
                schema: "Crm",
                table: "BookingEvents",
                columns: new[] { "BookingId", "CustomerId" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_CustomerId_RequestedAtUtc_Id",
                schema: "Crm",
                table: "Bookings",
                columns: new[] { "CustomerId", "RequestedAtUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_LeadId_CustomerId_ProjectId",
                schema: "Crm",
                table: "Bookings",
                columns: new[] { "LeadId", "CustomerId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_PendingResponseDue",
                schema: "Crm",
                table: "Bookings",
                columns: new[] { "Status", "ResponseDueAtUtc", "Id" },
                filter: "[Status] = N'PendingReview'")
                .Annotation("SqlServer:Include", new[] { "CustomerId", "Version" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_RejectedByAccountId",
                schema: "Crm",
                table: "Bookings",
                column: "RejectedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_RequestedByAccountId",
                schema: "Crm",
                table: "Bookings",
                column: "RequestedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_SubmissionAgreementId",
                schema: "Crm",
                table: "Bookings",
                column: "SubmissionAgreementId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_UnitId_ProjectId",
                schema: "Crm",
                table: "Bookings",
                columns: new[] { "UnitId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "UX_Bookings_ActiveCustomerUnit",
                schema: "Crm",
                table: "Bookings",
                columns: new[] { "CustomerId", "UnitId", "IsActiveRequest" },
                unique: true,
                filter: "[IsActiveRequest] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookingEvents",
                schema: "Crm");

            migrationBuilder.DropTable(
                name: "Bookings",
                schema: "Crm");
        }
    }
}
