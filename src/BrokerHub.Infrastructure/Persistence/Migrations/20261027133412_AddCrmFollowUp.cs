using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrokerHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCrmFollowUp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomerNeedsChanges",
                schema: "Crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreviousRequirements = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Requirements = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CustomerVersion = table.Column<long>(type: "bigint", nullable: false),
                    ActorAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerNeedsChanges", x => x.Id);
                    table.CheckConstraint("CK_CustomerNeedsChanges_Text", "([PreviousRequirements] IS NULL OR LEN(LTRIM(RTRIM([PreviousRequirements])))>0) AND ([Requirements] IS NULL OR LEN(LTRIM(RTRIM([Requirements])))>0)");
                    table.CheckConstraint("CK_CustomerNeedsChanges_Version", "[CustomerVersion]>1");
                    table.ForeignKey(
                        name: "FK_CustomerNeedsChanges_Accounts_ActorAccountId",
                        column: x => x.ActorAccountId,
                        principalSchema: "Identity",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerNeedsChanges_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "Crm",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalesNotes",
                schema: "Crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LeadId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AuthorAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuthorEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Body = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    CorrectsNoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesNotes", x => x.Id);
                    table.UniqueConstraint("AK_SalesNotes_Id_CustomerId", x => new { x.Id, x.CustomerId });
                    table.CheckConstraint("CK_SalesNotes_Body", "LEN(LTRIM(RTRIM([Body])))>0");
                    table.CheckConstraint("CK_SalesNotes_Correction", "[CorrectsNoteId] IS NULL OR [CorrectsNoteId]<>[Id]");
                    table.ForeignKey(
                        name: "FK_SalesNotes_Accounts_AuthorAccountId",
                        column: x => x.AuthorAccountId,
                        principalSchema: "Identity",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesNotes_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "Crm",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesNotes_Employees_AuthorEmployeeId",
                        column: x => x.AuthorEmployeeId,
                        principalSchema: "Crm",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesNotes_Leads_LeadId_CustomerId",
                        columns: x => new { x.LeadId, x.CustomerId },
                        principalSchema: "Crm",
                        principalTable: "Leads",
                        principalColumns: new[] { "Id", "CustomerId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesNotes_SalesNotes_CorrectsNoteId_CustomerId",
                        columns: x => new { x.CorrectsNoteId, x.CustomerId },
                        principalSchema: "Crm",
                        principalTable: "SalesNotes",
                        principalColumns: new[] { "Id", "CustomerId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerNeedsChanges_ActorAccountId",
                schema: "Crm",
                table: "CustomerNeedsChanges",
                column: "ActorAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerNeedsChanges_CustomerId_CustomerVersion",
                schema: "Crm",
                table: "CustomerNeedsChanges",
                columns: new[] { "CustomerId", "CustomerVersion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesNotes_AuthorAccountId",
                schema: "Crm",
                table: "SalesNotes",
                column: "AuthorAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesNotes_AuthorEmployeeId",
                schema: "Crm",
                table: "SalesNotes",
                column: "AuthorEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesNotes_CorrectsNoteId_CustomerId",
                schema: "Crm",
                table: "SalesNotes",
                columns: new[] { "CorrectsNoteId", "CustomerId" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesNotes_CustomerId_CreatedAtUtc",
                schema: "Crm",
                table: "SalesNotes",
                columns: new[] { "CustomerId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesNotes_LeadId_CustomerId",
                schema: "Crm",
                table: "SalesNotes",
                columns: new[] { "LeadId", "CustomerId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerNeedsChanges",
                schema: "Crm");

            migrationBuilder.DropTable(
                name: "SalesNotes",
                schema: "Crm");
        }
    }
}
