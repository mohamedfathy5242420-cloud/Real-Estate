using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrokerHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUnitCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Units",
                schema: "Catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    UnitType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AreaSqm = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Bedrooms = table.Column<int>(type: "int", nullable: true),
                    Bathrooms = table.Column<int>(type: "int", nullable: true),
                    ListPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    PublicationStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    AvailabilityStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    LastPriceConfirmedAtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    LastPriceSource = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LastAvailabilityConfirmedAtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    LastAvailabilitySource = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    CreatedByAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Units", x => x.Id);
                    table.CheckConstraint("CK_Units_AreaSqm_Positive", "[AreaSqm] > 0");
                    table.CheckConstraint("CK_Units_AvailabilityStatus_NotBlank", "LEN(LTRIM(RTRIM([AvailabilityStatus]))) > 0");
                    table.CheckConstraint("CK_Units_Bathrooms_Nonnegative", "[Bathrooms] IS NULL OR [Bathrooms] >= 0");
                    table.CheckConstraint("CK_Units_Bedrooms_Nonnegative", "[Bedrooms] IS NULL OR [Bedrooms] >= 0");
                    table.CheckConstraint("CK_Units_Code_NotBlank", "LEN(LTRIM(RTRIM([Code]))) > 0");
                    table.CheckConstraint("CK_Units_Currency_Alphabetic", "LEN([Currency]) = 3 AND [Currency] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^A-Z]%'");
                    table.CheckConstraint("CK_Units_ListPrice_Nonnegative", "[ListPrice] >= 0");
                    table.CheckConstraint("CK_Units_PublicationStatus_NotBlank", "LEN(LTRIM(RTRIM([PublicationStatus]))) > 0");
                    table.CheckConstraint("CK_Units_Title_NotBlank", "LEN(LTRIM(RTRIM([Title]))) > 0");
                    table.CheckConstraint("CK_Units_UnitType_NotBlank", "LEN(LTRIM(RTRIM([UnitType]))) > 0");
                    table.CheckConstraint("CK_Units_Version_Positive", "[Version] > 0");
                    table.ForeignKey(
                        name: "FK_Units_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "Catalog",
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "UX_Units_ProjectId_Code",
                schema: "Catalog",
                table: "Units",
                columns: new[] { "ProjectId", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Units",
                schema: "Catalog");
        }
    }
}
