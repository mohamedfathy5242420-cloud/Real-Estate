using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrokerHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUnitPublicationTransitions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UnitPublicationTransitions",
                schema: "Catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ToStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    UnitVersion = table.Column<long>(type: "bigint", nullable: false),
                    RecordedAtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    RecordedByAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MarketingAgreementId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EligibilityDate = table.Column<DateOnly>(type: "date", nullable: true),
                    EligibilityTimeZoneId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitPublicationTransitions", x => x.Id);
                    table.CheckConstraint("CK_UnitPublicationTransitions_Eligibility", "([ToStatus]='Published' AND [MarketingAgreementId] IS NOT NULL AND [EligibilityDate] IS NOT NULL AND [EligibilityTimeZoneId] IS NOT NULL) OR ([ToStatus]='Unpublished' AND [MarketingAgreementId] IS NULL AND [EligibilityDate] IS NULL AND [EligibilityTimeZoneId] IS NULL)");
                    table.CheckConstraint("CK_UnitPublicationTransitions_States", "([FromStatus]='Unpublished' AND [ToStatus]='Published') OR ([FromStatus]='Published' AND [ToStatus]='Unpublished')");
                    table.CheckConstraint("CK_UnitPublicationTransitions_Time", "[RecordedAtUtc]>'0001-01-01'");
                    table.CheckConstraint("CK_UnitPublicationTransitions_Version", "[UnitVersion]>1");
                    table.ForeignKey(
                        name: "FK_UnitPublicationTransitions_Accounts_RecordedByAccountId",
                        column: x => x.RecordedByAccountId,
                        principalSchema: "Identity",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UnitPublicationTransitions_MarketingAgreements_MarketingAgreementId",
                        column: x => x.MarketingAgreementId,
                        principalSchema: "Catalog",
                        principalTable: "MarketingAgreements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UnitPublicationTransitions_Units_UnitId",
                        column: x => x.UnitId,
                        principalSchema: "Catalog",
                        principalTable: "Units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Units_PublicationStatus",
                schema: "Catalog",
                table: "Units",
                sql: "[PublicationStatus] IN ('Unpublished','Published')");

            migrationBuilder.CreateIndex(
                name: "IX_UnitPublicationTransitions_MarketingAgreementId",
                schema: "Catalog",
                table: "UnitPublicationTransitions",
                column: "MarketingAgreementId");

            migrationBuilder.CreateIndex(
                name: "IX_UnitPublicationTransitions_RecordedByAccountId",
                schema: "Catalog",
                table: "UnitPublicationTransitions",
                column: "RecordedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_UnitPublicationTransitions_UnitId_UnitVersion",
                schema: "Catalog",
                table: "UnitPublicationTransitions",
                columns: new[] { "UnitId", "UnitVersion" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UnitPublicationTransitions",
                schema: "Catalog");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Units_PublicationStatus",
                schema: "Catalog",
                table: "Units");
        }
    }
}
