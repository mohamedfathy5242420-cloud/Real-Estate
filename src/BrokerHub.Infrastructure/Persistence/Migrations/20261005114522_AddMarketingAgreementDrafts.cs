using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrokerHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMarketingAgreementDrafts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_Projects_Id_DeveloperId",
                schema: "Catalog",
                table: "Projects",
                columns: new[] { "Id", "DeveloperId" });

            migrationBuilder.CreateTable(
                name: "MarketingAgreements",
                schema: "Catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeveloperId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AgreementNumber = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ScopeType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    StartsOn = table.Column<DateOnly>(type: "date", nullable: false),
                    EndsOn = table.Column<DateOnly>(type: "date", nullable: false),
                    ProtectionPeriodDays = table.Column<int>(type: "int", nullable: false, defaultValue: 90),
                    Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    CreatedByAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketingAgreements", x => x.Id);
                    table.UniqueConstraint("AK_MarketingAgreements_Id_DeveloperId", x => new { x.Id, x.DeveloperId });
                    table.CheckConstraint("CK_MarketingAgreements_Dates", "[StartsOn] > '00010101' AND [EndsOn] >= [StartsOn]");
                    table.CheckConstraint("CK_MarketingAgreements_Draft", "[Status] = 'Draft'");
                    table.CheckConstraint("CK_MarketingAgreements_Number", "LEN(LTRIM(RTRIM([AgreementNumber]))) > 0");
                    table.CheckConstraint("CK_MarketingAgreements_Protection", "[ProtectionPeriodDays] >= 0");
                    table.CheckConstraint("CK_MarketingAgreements_Scope", "[ScopeType] IN ('AllDeveloperProjects', 'SelectedProjects')");
                    table.CheckConstraint("CK_MarketingAgreements_UpdateAudit", "([UpdatedAtUtc] IS NULL AND [UpdatedByAccountId] IS NULL) OR ([UpdatedAtUtc] IS NOT NULL AND [UpdatedByAccountId] IS NOT NULL)");
                    table.CheckConstraint("CK_MarketingAgreements_Version", "[Version] > 0");
                    table.ForeignKey(
                        name: "FK_MarketingAgreements_Developers_DeveloperId",
                        column: x => x.DeveloperId,
                        principalSchema: "Catalog",
                        principalTable: "Developers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AgreementProjects",
                schema: "Catalog",
                columns: table => new
                {
                    MarketingAgreementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeveloperId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AddedAtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    AddedByAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsIncluded = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgreementProjects", x => new { x.MarketingAgreementId, x.ProjectId });
                    table.ForeignKey(
                        name: "FK_AgreementProjects_MarketingAgreements_MarketingAgreementId_DeveloperId",
                        columns: x => new { x.MarketingAgreementId, x.DeveloperId },
                        principalSchema: "Catalog",
                        principalTable: "MarketingAgreements",
                        principalColumns: new[] { "Id", "DeveloperId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AgreementProjects_Projects_ProjectId_DeveloperId",
                        columns: x => new { x.ProjectId, x.DeveloperId },
                        principalSchema: "Catalog",
                        principalTable: "Projects",
                        principalColumns: new[] { "Id", "DeveloperId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CommissionRules",
                schema: "Catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MarketingAgreementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeveloperId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RuleType = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Percentage = table.Column<decimal>(type: "decimal(7,4)", precision: 7, scale: 4, nullable: true),
                    FixedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    CreatedByAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommissionRules", x => x.Id);
                    table.CheckConstraint("CK_CommissionRules_Currency", "LEN([Currency]) = 3 AND [Currency] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^A-Z]%'");
                    table.CheckConstraint("CK_CommissionRules_DefaultNotArchived", "[ProjectId] IS NOT NULL OR [IsArchived] = 0");
                    table.CheckConstraint("CK_CommissionRules_Shape", "([RuleType] = 'Percentage' AND [Percentage] IS NOT NULL AND [Percentage] > 0 AND [Percentage] <= 100 AND [FixedAmount] IS NULL) OR ([RuleType] = 'FixedAmount' AND [FixedAmount] IS NOT NULL AND [FixedAmount] >= 0 AND [Percentage] IS NULL)");
                    table.CheckConstraint("CK_CommissionRules_Version", "[Version] > 0");
                    table.ForeignKey(
                        name: "FK_CommissionRules_MarketingAgreements_MarketingAgreementId_DeveloperId",
                        columns: x => new { x.MarketingAgreementId, x.DeveloperId },
                        principalSchema: "Catalog",
                        principalTable: "MarketingAgreements",
                        principalColumns: new[] { "Id", "DeveloperId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CommissionRules_Projects_ProjectId_DeveloperId",
                        columns: x => new { x.ProjectId, x.DeveloperId },
                        principalSchema: "Catalog",
                        principalTable: "Projects",
                        principalColumns: new[] { "Id", "DeveloperId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MarketingAgreementRevisions",
                schema: "Catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MarketingAgreementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AgreementVersion = table.Column<long>(type: "bigint", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    SnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    ActorAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketingAgreementRevisions", x => x.Id);
                    table.CheckConstraint("CK_MarketingAgreementRevisions_Action", "[Action] IN ('Created', 'DraftUpdated')");
                    table.CheckConstraint("CK_MarketingAgreementRevisions_Json", "ISJSON([SnapshotJson]) = 1");
                    table.CheckConstraint("CK_MarketingAgreementRevisions_Version", "[AgreementVersion] > 0");
                    table.ForeignKey(
                        name: "FK_MarketingAgreementRevisions_MarketingAgreements_MarketingAgreementId",
                        column: x => x.MarketingAgreementId,
                        principalSchema: "Catalog",
                        principalTable: "MarketingAgreements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgreementProjects_MarketingAgreementId_DeveloperId",
                schema: "Catalog",
                table: "AgreementProjects",
                columns: new[] { "MarketingAgreementId", "DeveloperId" });

            migrationBuilder.CreateIndex(
                name: "IX_AgreementProjects_ProjectId_DeveloperId",
                schema: "Catalog",
                table: "AgreementProjects",
                columns: new[] { "ProjectId", "DeveloperId" });

            migrationBuilder.CreateIndex(
                name: "IX_CommissionRules_MarketingAgreementId_DeveloperId",
                schema: "Catalog",
                table: "CommissionRules",
                columns: new[] { "MarketingAgreementId", "DeveloperId" });

            migrationBuilder.CreateIndex(
                name: "IX_CommissionRules_ProjectId_DeveloperId",
                schema: "Catalog",
                table: "CommissionRules",
                columns: new[] { "ProjectId", "DeveloperId" });

            migrationBuilder.CreateIndex(
                name: "UX_CommissionRules_CurrentDefault",
                schema: "Catalog",
                table: "CommissionRules",
                column: "MarketingAgreementId",
                unique: true,
                filter: "[ProjectId] IS NULL AND [IsArchived] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_CommissionRules_CurrentOverride",
                schema: "Catalog",
                table: "CommissionRules",
                columns: new[] { "MarketingAgreementId", "ProjectId" },
                unique: true,
                filter: "[ProjectId] IS NOT NULL AND [IsArchived] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_MarketingAgreementRevisions_MarketingAgreementId_AgreementVersion",
                schema: "Catalog",
                table: "MarketingAgreementRevisions",
                columns: new[] { "MarketingAgreementId", "AgreementVersion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MarketingAgreements_DeveloperId_AgreementNumber",
                schema: "Catalog",
                table: "MarketingAgreements",
                columns: new[] { "DeveloperId", "AgreementNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgreementProjects",
                schema: "Catalog");

            migrationBuilder.DropTable(
                name: "CommissionRules",
                schema: "Catalog");

            migrationBuilder.DropTable(
                name: "MarketingAgreementRevisions",
                schema: "Catalog");

            migrationBuilder.DropTable(
                name: "MarketingAgreements",
                schema: "Catalog");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Projects_Id_DeveloperId",
                schema: "Catalog",
                table: "Projects");
        }
    }
}
