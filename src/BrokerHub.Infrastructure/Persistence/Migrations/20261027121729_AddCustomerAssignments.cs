using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrokerHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AssignedEmployeeId",
                schema: "Crm",
                table: "Customers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CustomerAssignments",
                schema: "Crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreviousEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NewEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangedAtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CustomerVersion = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerAssignments", x => x.Id);
                    table.CheckConstraint("CK_CustomerAssignments_Reason", "([Reason] IS NULL OR LEN(LTRIM(RTRIM([Reason])))>0) AND ([PreviousEmployeeId] IS NULL OR [Reason] IS NOT NULL)");
                    table.CheckConstraint("CK_CustomerAssignments_Transition", "[PreviousEmployeeId] IS NULL OR [PreviousEmployeeId]<>[NewEmployeeId]");
                    table.CheckConstraint("CK_CustomerAssignments_Version", "[CustomerVersion]>1");
                    table.ForeignKey(
                        name: "FK_CustomerAssignments_Accounts_ActorAccountId",
                        column: x => x.ActorAccountId,
                        principalSchema: "Identity",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerAssignments_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "Crm",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerAssignments_Employees_NewEmployeeId",
                        column: x => x.NewEmployeeId,
                        principalSchema: "Crm",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerAssignments_Employees_PreviousEmployeeId",
                        column: x => x.PreviousEmployeeId,
                        principalSchema: "Crm",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Customers_AssignedEmployeeId",
                schema: "Crm",
                table: "Customers",
                column: "AssignedEmployeeId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Customers_Assignment_Audit",
                schema: "Crm",
                table: "Customers",
                sql: "[AssignedEmployeeId] IS NULL OR [Version]>1");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAssignments_ActorAccountId",
                schema: "Crm",
                table: "CustomerAssignments",
                column: "ActorAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAssignments_CustomerId_CustomerVersion",
                schema: "Crm",
                table: "CustomerAssignments",
                columns: new[] { "CustomerId", "CustomerVersion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAssignments_NewEmployeeId",
                schema: "Crm",
                table: "CustomerAssignments",
                column: "NewEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAssignments_PreviousEmployeeId",
                schema: "Crm",
                table: "CustomerAssignments",
                column: "PreviousEmployeeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Customers_Employees_AssignedEmployeeId",
                schema: "Crm",
                table: "Customers",
                column: "AssignedEmployeeId",
                principalSchema: "Crm",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Customers_Employees_AssignedEmployeeId",
                schema: "Crm",
                table: "Customers");

            migrationBuilder.DropTable(
                name: "CustomerAssignments",
                schema: "Crm");

            migrationBuilder.DropIndex(
                name: "IX_Customers_AssignedEmployeeId",
                schema: "Crm",
                table: "Customers");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Customers_Assignment_Audit",
                schema: "Crm",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "AssignedEmployeeId",
                schema: "Crm",
                table: "Customers");
        }
    }
}
