using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrokerHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUnitMarketConfirmations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ConfirmedCurrency",
                schema: "Catalog",
                table: "Units",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ConfirmedPrice",
                schema: "Catalog",
                table: "Units",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LatestAvailabilityConfirmationId",
                schema: "Catalog",
                table: "Units",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LatestPriceConfirmationId",
                schema: "Catalog",
                table: "Units",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UnitMarketConfirmations",
                schema: "Catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConfirmedAtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    RecordedAtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    RecordedByAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SourceReference = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ConfirmedPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    AvailabilityStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitMarketConfirmations", x => x.Id);
                    table.UniqueConstraint("AK_UnitMarketConfirmations_UnitId_Id", x => new { x.UnitId, x.Id });
                    table.CheckConstraint("CK_UnitMarketConfirmations_Availability", "[AvailabilityStatus] IS NULL OR [AvailabilityStatus] IN ('Available','Unavailable')");
                    table.CheckConstraint("CK_UnitMarketConfirmations_Fact", "[ConfirmedPrice] IS NOT NULL OR [AvailabilityStatus] IS NOT NULL");
                    table.CheckConstraint("CK_UnitMarketConfirmations_Money", "([ConfirmedPrice] IS NULL AND [Currency] IS NULL) OR ([ConfirmedPrice] IS NOT NULL AND [ConfirmedPrice] >= 0 AND [Currency] IS NOT NULL AND LEN([Currency]) = 3 AND [Currency] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^A-Z]%')");
                    table.CheckConstraint("CK_UnitMarketConfirmations_Source", "LEN(LTRIM(RTRIM([Source]))) > 0");
                    table.CheckConstraint("CK_UnitMarketConfirmations_Time", "[ConfirmedAtUtc] > '0001-01-01' AND [ConfirmedAtUtc] <= [RecordedAtUtc]");
                    table.ForeignKey(
                        name: "FK_UnitMarketConfirmations_Accounts_RecordedByAccountId",
                        column: x => x.RecordedByAccountId,
                        principalSchema: "Identity",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UnitMarketConfirmations_Units_UnitId",
                        column: x => x.UnitId,
                        principalSchema: "Catalog",
                        principalTable: "Units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Units_Id_LatestAvailabilityConfirmationId",
                schema: "Catalog",
                table: "Units",
                columns: new[] { "Id", "LatestAvailabilityConfirmationId" });

            migrationBuilder.CreateIndex(
                name: "IX_Units_Id_LatestPriceConfirmationId",
                schema: "Catalog",
                table: "Units",
                columns: new[] { "Id", "LatestPriceConfirmationId" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Units_Availability_Observation",
                schema: "Catalog",
                table: "Units",
                sql: "[AvailabilityStatus] IN ('Unconfirmed','Available','Unavailable')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Units_Availability_Projection",
                schema: "Catalog",
                table: "Units",
                sql: "([LatestAvailabilityConfirmationId] IS NULL AND [AvailabilityStatus] = 'Unconfirmed' AND [LastAvailabilityConfirmedAtUtc] IS NULL AND [LastAvailabilitySource] IS NULL) OR ([LatestAvailabilityConfirmationId] IS NOT NULL AND [AvailabilityStatus] IN ('Available','Unavailable') AND [LastAvailabilityConfirmedAtUtc] IS NOT NULL AND [LastAvailabilitySource] IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Units_ConfirmedPrice_Projection",
                schema: "Catalog",
                table: "Units",
                sql: "([LatestPriceConfirmationId] IS NULL AND [ConfirmedPrice] IS NULL AND [ConfirmedCurrency] IS NULL AND [LastPriceConfirmedAtUtc] IS NULL AND [LastPriceSource] IS NULL) OR ([LatestPriceConfirmationId] IS NOT NULL AND [ConfirmedPrice] IS NOT NULL AND [ConfirmedPrice] >= 0 AND [ConfirmedCurrency] IS NOT NULL AND LEN([ConfirmedCurrency]) = 3 AND [ConfirmedCurrency] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^A-Z]%' AND [LastPriceConfirmedAtUtc] IS NOT NULL AND [LastPriceSource] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_UnitMarketConfirmations_History",
                schema: "Catalog",
                table: "UnitMarketConfirmations",
                columns: new[] { "UnitId", "ConfirmedAtUtc", "RecordedAtUtc", "Id" },
                descending: new[] { false, true, true, true });

            migrationBuilder.CreateIndex(
                name: "IX_UnitMarketConfirmations_RecordedByAccountId",
                schema: "Catalog",
                table: "UnitMarketConfirmations",
                column: "RecordedByAccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_Units_UnitMarketConfirmations_Id_LatestAvailabilityConfirmationId",
                schema: "Catalog",
                table: "Units",
                columns: new[] { "Id", "LatestAvailabilityConfirmationId" },
                principalSchema: "Catalog",
                principalTable: "UnitMarketConfirmations",
                principalColumns: new[] { "UnitId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Units_UnitMarketConfirmations_Id_LatestPriceConfirmationId",
                schema: "Catalog",
                table: "Units",
                columns: new[] { "Id", "LatestPriceConfirmationId" },
                principalSchema: "Catalog",
                principalTable: "UnitMarketConfirmations",
                principalColumns: new[] { "UnitId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Units_UnitMarketConfirmations_Id_LatestAvailabilityConfirmationId",
                schema: "Catalog",
                table: "Units");

            migrationBuilder.DropForeignKey(
                name: "FK_Units_UnitMarketConfirmations_Id_LatestPriceConfirmationId",
                schema: "Catalog",
                table: "Units");

            migrationBuilder.DropTable(
                name: "UnitMarketConfirmations",
                schema: "Catalog");

            migrationBuilder.DropIndex(
                name: "IX_Units_Id_LatestAvailabilityConfirmationId",
                schema: "Catalog",
                table: "Units");

            migrationBuilder.DropIndex(
                name: "IX_Units_Id_LatestPriceConfirmationId",
                schema: "Catalog",
                table: "Units");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Units_Availability_Observation",
                schema: "Catalog",
                table: "Units");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Units_Availability_Projection",
                schema: "Catalog",
                table: "Units");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Units_ConfirmedPrice_Projection",
                schema: "Catalog",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "ConfirmedCurrency",
                schema: "Catalog",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "ConfirmedPrice",
                schema: "Catalog",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "LatestAvailabilityConfirmationId",
                schema: "Catalog",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "LatestPriceConfirmationId",
                schema: "Catalog",
                table: "Units");
        }
    }
}
