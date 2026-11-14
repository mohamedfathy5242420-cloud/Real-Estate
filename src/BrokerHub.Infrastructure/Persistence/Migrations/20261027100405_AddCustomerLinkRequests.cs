using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrokerHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerLinkRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomerLinkRequests",
                schema: "Crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Claim = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    DecidedAtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    DecidedByAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DecisionReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerLinkRequests", x => x.Id);
                    table.CheckConstraint("CK_CustomerLinkRequests_Claim", "LEN(LTRIM(RTRIM([Claim])))>0");
                    table.CheckConstraint("CK_CustomerLinkRequests_State", "([Status]='Pending' AND [Version]=1 AND [DecidedAtUtc] IS NULL AND [DecidedByAccountId] IS NULL AND [DecisionReason] IS NULL AND [CustomerId] IS NULL) OR ([Status] IN ('Approved','Rejected') AND [Version]=2 AND [DecidedAtUtc] IS NOT NULL AND [DecidedAtUtc]>=[CreatedAtUtc] AND [DecidedByAccountId] IS NOT NULL AND [DecisionReason] IS NOT NULL AND LEN(LTRIM(RTRIM([DecisionReason])))>0 AND (([Status]='Approved' AND [CustomerId] IS NOT NULL) OR ([Status]='Rejected' AND [CustomerId] IS NULL)))");
                    table.ForeignKey(
                        name: "FK_CustomerLinkRequests_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalSchema: "Identity",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerLinkRequests_Accounts_DecidedByAccountId",
                        column: x => x.DecidedByAccountId,
                        principalSchema: "Identity",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerLinkRequests_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "Crm",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CustomerLinkRequestEvents",
                schema: "Crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    ActorAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerLinkRequestEvents", x => x.Id);
                    table.CheckConstraint("CK_CustomerLinkRequestEvents_State", "([Sequence]=1 AND [Kind]='Submitted' AND [Reason] IS NULL AND [CustomerId] IS NULL) OR ([Sequence]=2 AND [Reason] IS NOT NULL AND LEN(LTRIM(RTRIM([Reason])))>0 AND (([Kind]='Approved' AND [CustomerId] IS NOT NULL) OR ([Kind]='Rejected' AND [CustomerId] IS NULL)))");
                    table.ForeignKey(
                        name: "FK_CustomerLinkRequestEvents_Accounts_ActorAccountId",
                        column: x => x.ActorAccountId,
                        principalSchema: "Identity",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerLinkRequestEvents_CustomerLinkRequests_RequestId",
                        column: x => x.RequestId,
                        principalSchema: "Crm",
                        principalTable: "CustomerLinkRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerLinkRequestEvents_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "Crm",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerLinkRequestEvents_ActorAccountId",
                schema: "Crm",
                table: "CustomerLinkRequestEvents",
                column: "ActorAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerLinkRequestEvents_CustomerId",
                schema: "Crm",
                table: "CustomerLinkRequestEvents",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerLinkRequestEvents_RequestId_Sequence",
                schema: "Crm",
                table: "CustomerLinkRequestEvents",
                columns: new[] { "RequestId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerLinkRequests_CustomerId",
                schema: "Crm",
                table: "CustomerLinkRequests",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerLinkRequests_DecidedByAccountId",
                schema: "Crm",
                table: "CustomerLinkRequests",
                column: "DecidedByAccountId");

            migrationBuilder.CreateIndex(
                name: "UX_CustomerLinkRequests_PendingAccount",
                schema: "Crm",
                table: "CustomerLinkRequests",
                column: "AccountId",
                unique: true,
                filter: "[Status]='Pending'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerLinkRequestEvents",
                schema: "Crm");

            migrationBuilder.DropTable(
                name: "CustomerLinkRequests",
                schema: "Crm");
        }
    }
}
