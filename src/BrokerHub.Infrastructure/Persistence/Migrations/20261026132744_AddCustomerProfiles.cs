using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrokerHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Crm");

            migrationBuilder.CreateTable(
                name: "Customers",
                schema: "Crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: true),
                    PreferredContactMethod = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Requirements = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    CreatedByAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                    table.CheckConstraint("CK_Customers_Audit_Pair", "([Version]=1 AND [UpdatedAtUtc] IS NULL AND [UpdatedByAccountId] IS NULL) OR ([Version]>1 AND [UpdatedAtUtc] IS NOT NULL AND [UpdatedByAccountId] IS NOT NULL AND [UpdatedAtUtc]>=[CreatedAtUtc])");
                    table.CheckConstraint("CK_Customers_FullName_NotBlank", "LEN(LTRIM(RTRIM([FullName])))>0");
                    table.CheckConstraint("CK_Customers_Phone_NotBlank", "LEN(LTRIM(RTRIM([Phone])))>0");
                    table.CheckConstraint("CK_Customers_Version_Positive", "[Version]>0");
                    table.ForeignKey(
                        name: "FK_Customers_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalSchema: "Identity",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Customers_Accounts_CreatedByAccountId",
                        column: x => x.CreatedByAccountId,
                        principalSchema: "Identity",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Customers_Accounts_UpdatedByAccountId",
                        column: x => x.UpdatedByAccountId,
                        principalSchema: "Identity",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Customers_CreatedByAccountId",
                schema: "Crm",
                table: "Customers",
                column: "CreatedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_UpdatedByAccountId",
                schema: "Crm",
                table: "Customers",
                column: "UpdatedByAccountId");

            migrationBuilder.CreateIndex(
                name: "UX_Customers_AccountId",
                schema: "Crm",
                table: "Customers",
                column: "AccountId",
                unique: true,
                filter: "[AccountId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Customers",
                schema: "Crm");
        }
    }
}
