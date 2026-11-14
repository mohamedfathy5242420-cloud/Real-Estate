using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrokerHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddViewingRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_Units_Id_ProjectId",
                schema: "Catalog",
                table: "Units",
                columns: new[] { "Id", "ProjectId" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Leads_Id_CustomerId_ProjectId",
                schema: "Crm",
                table: "Leads",
                columns: new[] { "Id", "CustomerId", "ProjectId" });

            migrationBuilder.CreateTable(
                name: "Viewings",
                schema: "Crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LeadId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CurrentProposedStartUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    CurrentProposedEndUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    TimeZoneId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RequestedAtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    PendingCycleStartedAtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    RequestedByAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmissionAgreementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmissionLocalDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Viewings", x => x.Id);
                    table.UniqueConstraint("AK_Viewings_Id_CustomerId", x => new { x.Id, x.CustomerId });
                    table.CheckConstraint("CK_Viewings_Cycle", "[PendingCycleStartedAtUtc]=[RequestedAtUtc]");
                    table.CheckConstraint("CK_Viewings_InitialState", "[Status]=N'PendingConfirmation' AND [Version]=1");
                    table.CheckConstraint("CK_Viewings_Interval", "[RequestedAtUtc]>'00010101' AND [CurrentProposedStartUtc]>[RequestedAtUtc] AND [CurrentProposedStartUtc]<=DATEADD(day,30,[RequestedAtUtc]) AND [CurrentProposedEndUtc]=DATEADD(minute,60,[CurrentProposedStartUtc])");
                    table.CheckConstraint("CK_Viewings_LocalContext", "[SubmissionLocalDate]>'00010101' AND LEN(LTRIM(RTRIM([TimeZoneId])))>0");
                    table.ForeignKey(
                        name: "FK_Viewings_Accounts_RequestedByAccountId",
                        column: x => x.RequestedByAccountId,
                        principalSchema: "Identity",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Viewings_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "Crm",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Viewings_Leads_LeadId_CustomerId_ProjectId",
                        columns: x => new { x.LeadId, x.CustomerId, x.ProjectId },
                        principalSchema: "Crm",
                        principalTable: "Leads",
                        principalColumns: new[] { "Id", "CustomerId", "ProjectId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Viewings_MarketingAgreements_SubmissionAgreementId",
                        column: x => x.SubmissionAgreementId,
                        principalSchema: "Catalog",
                        principalTable: "MarketingAgreements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Viewings_Units_UnitId_ProjectId",
                        columns: x => new { x.UnitId, x.ProjectId },
                        principalSchema: "Catalog",
                        principalTable: "Units",
                        principalColumns: new[] { "Id", "ProjectId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ViewingEvents",
                schema: "Crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ViewingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ViewingVersion = table.Column<long>(type: "bigint", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ActorAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    ProposedStartUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    ProposedEndUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ViewingEvents", x => x.Id);
                    table.CheckConstraint("CK_ViewingEvents_InitialShape", "[Kind]=N'Requested' AND [ViewingVersion]=1 AND [AtUtc]>'00010101' AND [ProposedStartUtc]>[AtUtc] AND [ProposedStartUtc]<=DATEADD(day,30,[AtUtc]) AND [ProposedEndUtc]=DATEADD(minute,60,[ProposedStartUtc])");
                    table.ForeignKey(
                        name: "FK_ViewingEvents_Accounts_ActorAccountId",
                        column: x => x.ActorAccountId,
                        principalSchema: "Identity",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ViewingEvents_Viewings_ViewingId_CustomerId",
                        columns: x => new { x.ViewingId, x.CustomerId },
                        principalSchema: "Crm",
                        principalTable: "Viewings",
                        principalColumns: new[] { "Id", "CustomerId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ViewingEvents_ActorAccountId",
                schema: "Crm",
                table: "ViewingEvents",
                column: "ActorAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ViewingEvents_ViewingId_CustomerId",
                schema: "Crm",
                table: "ViewingEvents",
                columns: new[] { "ViewingId", "CustomerId" });

            migrationBuilder.CreateIndex(
                name: "IX_ViewingEvents_ViewingId_ViewingVersion",
                schema: "Crm",
                table: "ViewingEvents",
                columns: new[] { "ViewingId", "ViewingVersion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Viewings_CustomerId_RequestedAtUtc_Id",
                schema: "Crm",
                table: "Viewings",
                columns: new[] { "CustomerId", "RequestedAtUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Viewings_LeadId_CustomerId_ProjectId",
                schema: "Crm",
                table: "Viewings",
                columns: new[] { "LeadId", "CustomerId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_Viewings_RequestedByAccountId",
                schema: "Crm",
                table: "Viewings",
                column: "RequestedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Viewings_SubmissionAgreementId",
                schema: "Crm",
                table: "Viewings",
                column: "SubmissionAgreementId");

            migrationBuilder.CreateIndex(
                name: "IX_Viewings_UnitId_ProjectId",
                schema: "Crm",
                table: "Viewings",
                columns: new[] { "UnitId", "ProjectId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ViewingEvents",
                schema: "Crm");

            migrationBuilder.DropTable(
                name: "Viewings",
                schema: "Crm");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Units_Id_ProjectId",
                schema: "Catalog",
                table: "Units");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Leads_Id_CustomerId_ProjectId",
                schema: "Crm",
                table: "Leads");
        }
    }
}
