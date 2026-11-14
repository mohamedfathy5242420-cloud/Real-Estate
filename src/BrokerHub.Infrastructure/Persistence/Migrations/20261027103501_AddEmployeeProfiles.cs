using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrokerHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Employees",
                schema: "Crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeNumber = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    CreatedByAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    UpdatedByAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.Id);
                    table.CheckConstraint("CK_Employees_Audit", "([Version]=1 AND [UpdatedAtUtc] IS NULL AND [UpdatedByAccountId] IS NULL) OR ([Version]>1 AND [UpdatedAtUtc] IS NOT NULL AND [UpdatedByAccountId] IS NOT NULL AND [UpdatedAtUtc]>=[CreatedAtUtc])");
                    table.CheckConstraint("CK_Employees_Name", "LEN(LTRIM(RTRIM([DisplayName])))>0");
                    table.CheckConstraint("CK_Employees_Number", "LEN(LTRIM(RTRIM([EmployeeNumber])))>0");
                    table.CheckConstraint("CK_Employees_Version", "[Version]>0");
                    table.ForeignKey(
                        name: "FK_Employees_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalSchema: "Identity",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Employees_Accounts_CreatedByAccountId",
                        column: x => x.CreatedByAccountId,
                        principalSchema: "Identity",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Employees_Accounts_UpdatedByAccountId",
                        column: x => x.UpdatedByAccountId,
                        principalSchema: "Identity",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeRevisions",
                schema: "Crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    AtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    ActorAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeRevisions", x => x.Id);
                    table.CheckConstraint("CK_EmployeeRevisions_Name", "LEN(LTRIM(RTRIM([DisplayName])))>0");
                    table.CheckConstraint("CK_EmployeeRevisions_Version", "[Version]>0");
                    table.ForeignKey(
                        name: "FK_EmployeeRevisions_Accounts_ActorAccountId",
                        column: x => x.ActorAccountId,
                        principalSchema: "Identity",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeRevisions_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "Crm",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeRevisions_ActorAccountId",
                schema: "Crm",
                table: "EmployeeRevisions",
                column: "ActorAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeRevisions_EmployeeId_Version",
                schema: "Crm",
                table: "EmployeeRevisions",
                columns: new[] { "EmployeeId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_AccountId",
                schema: "Crm",
                table: "Employees",
                column: "AccountId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_CreatedByAccountId",
                schema: "Crm",
                table: "Employees",
                column: "CreatedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_EmployeeNumber",
                schema: "Crm",
                table: "Employees",
                column: "EmployeeNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_UpdatedByAccountId",
                schema: "Crm",
                table: "Employees",
                column: "UpdatedByAccountId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmployeeRevisions",
                schema: "Crm");

            migrationBuilder.DropTable(
                name: "Employees",
                schema: "Crm");
        }
    }
}
