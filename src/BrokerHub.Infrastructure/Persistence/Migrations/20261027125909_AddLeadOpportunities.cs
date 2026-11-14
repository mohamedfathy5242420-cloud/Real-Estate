using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrokerHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLeadOpportunities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Leads",
                schema: "Crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MarketingAgreementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AgreementVersionAtOpening = table.Column<long>(type: "bigint", nullable: false),
                    AgreementStartsOn = table.Column<DateOnly>(type: "date", nullable: false),
                    AgreementEndsOn = table.Column<DateOnly>(type: "date", nullable: false),
                    ProtectionPeriodDays = table.Column<int>(type: "int", nullable: false),
                    ProtectionEndsOn = table.Column<DateOnly>(type: "date", nullable: false),
                    OpenedOnLocalDate = table.Column<DateOnly>(type: "date", nullable: false),
                    OpeningTimeZoneId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OpenedAtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    OpenedByAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    ClosedAtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    ClosedByAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CloseReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leads", x => x.Id);
                    table.UniqueConstraint("AK_Leads_Id_CustomerId", x => new { x.Id, x.CustomerId });
                    table.CheckConstraint("CK_Leads_Origin", "[AgreementVersionAtOpening]>0 AND [AgreementStartsOn]>'00010101' AND [AgreementEndsOn]>=[AgreementStartsOn] AND [OpenedOnLocalDate]>=[AgreementStartsOn] AND [OpenedOnLocalDate]<=[AgreementEndsOn]");
                    table.CheckConstraint("CK_Leads_Protection", "[ProtectionPeriodDays]>=0 AND [ProtectionEndsOn]>=[AgreementEndsOn] AND DATEDIFF(day,[AgreementEndsOn],[ProtectionEndsOn])=[ProtectionPeriodDays]");
                    table.CheckConstraint("CK_Leads_StateAudit", "([Status]=N'Open' AND [Version]=1 AND [ClosedAtUtc] IS NULL AND [ClosedByAccountId] IS NULL AND [CloseReason] IS NULL) OR ([Status]=N'Closed' AND [Version]>1 AND [ClosedAtUtc]>=[OpenedAtUtc] AND [ClosedAtUtc] IS NOT NULL AND [ClosedByAccountId] IS NOT NULL AND [CloseReason] IS NOT NULL AND LEN(LTRIM(RTRIM([CloseReason])))>0)");
                    table.CheckConstraint("CK_Leads_Text", "LEN(LTRIM(RTRIM([Source])))>0 AND LEN(LTRIM(RTRIM([OpeningTimeZoneId])))>0");
                    table.ForeignKey(
                        name: "FK_Leads_Accounts_ClosedByAccountId",
                        column: x => x.ClosedByAccountId,
                        principalSchema: "Identity",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Leads_Accounts_OpenedByAccountId",
                        column: x => x.OpenedByAccountId,
                        principalSchema: "Identity",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Leads_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "Crm",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Leads_MarketingAgreements_MarketingAgreementId",
                        column: x => x.MarketingAgreementId,
                        principalSchema: "Catalog",
                        principalTable: "MarketingAgreements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Leads_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "Catalog",
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LeadEvents",
                schema: "Crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LeadId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LeadVersion = table.Column<long>(type: "bigint", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    ActorAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeadEvents", x => x.Id);
                    table.CheckConstraint("CK_LeadEvents_Shape", "([EventType]=N'Opened' AND [LeadVersion]=1 AND [Reason] IS NULL) OR ([EventType]=N'Closed' AND [LeadVersion]>1 AND [Reason] IS NOT NULL AND LEN(LTRIM(RTRIM([Reason])))>0)");
                    table.ForeignKey(
                        name: "FK_LeadEvents_Accounts_ActorAccountId",
                        column: x => x.ActorAccountId,
                        principalSchema: "Identity",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LeadEvents_Leads_LeadId_CustomerId",
                        columns: x => new { x.LeadId, x.CustomerId },
                        principalSchema: "Crm",
                        principalTable: "Leads",
                        principalColumns: new[] { "Id", "CustomerId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LeadEvents_ActorAccountId",
                schema: "Crm",
                table: "LeadEvents",
                column: "ActorAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_LeadEvents_LeadId_CustomerId",
                schema: "Crm",
                table: "LeadEvents",
                columns: new[] { "LeadId", "CustomerId" });

            migrationBuilder.CreateIndex(
                name: "IX_LeadEvents_LeadId_LeadVersion",
                schema: "Crm",
                table: "LeadEvents",
                columns: new[] { "LeadId", "LeadVersion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Leads_ClosedByAccountId",
                schema: "Crm",
                table: "Leads",
                column: "ClosedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Leads_MarketingAgreementId",
                schema: "Crm",
                table: "Leads",
                column: "MarketingAgreementId");

            migrationBuilder.CreateIndex(
                name: "IX_Leads_OpenedByAccountId",
                schema: "Crm",
                table: "Leads",
                column: "OpenedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Leads_ProjectId",
                schema: "Crm",
                table: "Leads",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "UX_Leads_OpenCustomerProject",
                schema: "Crm",
                table: "Leads",
                columns: new[] { "CustomerId", "ProjectId" },
                unique: true,
                filter: "[Status] = N'Open'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LeadEvents",
                schema: "Crm");

            migrationBuilder.DropTable(
                name: "Leads",
                schema: "Crm");
        }
    }
}
