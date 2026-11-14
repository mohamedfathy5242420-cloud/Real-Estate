using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrokerHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDeveloperCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Catalog");

            migrationBuilder.CreateTable(
                name: "Developers",
                schema: "Catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LegalName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ExternalContactName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ExternalContactEmail = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: true),
                    ExternalContactPhone = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    CreatedByAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Developers", x => x.Id);
                    table.CheckConstraint("CK_Developers_Code_NotBlank", "LEN(LTRIM(RTRIM([Code]))) > 0");
                    table.CheckConstraint("CK_Developers_Name_NotBlank", "LEN(LTRIM(RTRIM([Name]))) > 0");
                    table.CheckConstraint("CK_Developers_Version_Positive", "[Version] > 0");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Developers_IsActive",
                schema: "Catalog",
                table: "Developers",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "UX_Developers_Code",
                schema: "Catalog",
                table: "Developers",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Developers",
                schema: "Catalog");
        }
    }
}
