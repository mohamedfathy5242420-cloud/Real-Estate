using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrokerHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ActivateMarketingAgreements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_MarketingAgreements_Draft",
                schema: "Catalog",
                table: "MarketingAgreements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_MarketingAgreementRevisions_Action",
                schema: "Catalog",
                table: "MarketingAgreementRevisions");

            migrationBuilder.AddCheckConstraint(
                name: "CK_MarketingAgreements_Status",
                schema: "Catalog",
                table: "MarketingAgreements",
                sql: "[Status] IN ('Draft', 'Active')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_MarketingAgreementRevisions_Action",
                schema: "Catalog",
                table: "MarketingAgreementRevisions",
                sql: "[Action] IN ('Created', 'DraftUpdated', 'Activated')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_MarketingAgreements_Status",
                schema: "Catalog",
                table: "MarketingAgreements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_MarketingAgreementRevisions_Action",
                schema: "Catalog",
                table: "MarketingAgreementRevisions");

            migrationBuilder.AddCheckConstraint(
                name: "CK_MarketingAgreements_Draft",
                schema: "Catalog",
                table: "MarketingAgreements",
                sql: "[Status] = 'Draft'");

            migrationBuilder.AddCheckConstraint(
                name: "CK_MarketingAgreementRevisions_Action",
                schema: "Catalog",
                table: "MarketingAgreementRevisions",
                sql: "[Action] IN ('Created', 'DraftUpdated')");
        }
    }
}
