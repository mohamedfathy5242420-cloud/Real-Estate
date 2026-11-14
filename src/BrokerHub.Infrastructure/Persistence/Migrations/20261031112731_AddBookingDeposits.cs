using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrokerHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingDeposits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BookingDeposits",
                schema: "Crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedVersion = table.Column<long>(type: "bigint", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    PaidAtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    RecordedAtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    RecorderAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecorderEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Source = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingDeposits", x => x.Id);
                    table.CheckConstraint("CK_BookingDeposits_ClockVersion", "[Version]=1 AND [RequestedVersion]>0 AND [PaidAtUtc]>'00010101' AND [RecordedAtUtc]>=[PaidAtUtc]");
                    table.CheckConstraint("CK_BookingDeposits_Evidence", "LEN(LTRIM(RTRIM([Source])))>0 AND LEN(LTRIM(RTRIM([Reference])))>0 AND ([Notes] IS NULL OR LEN(LTRIM(RTRIM([Notes])))>0)");
                    table.CheckConstraint("CK_BookingDeposits_Money", "[Amount]>0 AND LEN([CurrencyCode])=3 AND [CurrencyCode] COLLATE Latin1_General_100_BIN2 NOT LIKE N'%[^A-Z]%'");
                    table.ForeignKey(
                        name: "FK_BookingDeposits_Accounts_RecorderAccountId",
                        column: x => x.RecorderAccountId,
                        principalSchema: "Identity",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BookingDeposits_Bookings_BookingId_CustomerId_UnitId",
                        columns: x => new { x.BookingId, x.CustomerId, x.UnitId },
                        principalSchema: "Crm",
                        principalTable: "Bookings",
                        principalColumns: new[] { "Id", "CustomerId", "UnitId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BookingDeposits_Employees_RecorderEmployeeId",
                        column: x => x.RecorderEmployeeId,
                        principalSchema: "Crm",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BookingDeposits_BookingId_CustomerId_UnitId",
                schema: "Crm",
                table: "BookingDeposits",
                columns: new[] { "BookingId", "CustomerId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_BookingDeposits_BookingId_RecordedAtUtc_Id",
                schema: "Crm",
                table: "BookingDeposits",
                columns: new[] { "BookingId", "RecordedAtUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_BookingDeposits_RecorderAccountId",
                schema: "Crm",
                table: "BookingDeposits",
                column: "RecorderAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingDeposits_RecorderEmployeeId",
                schema: "Crm",
                table: "BookingDeposits",
                column: "RecorderEmployeeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookingDeposits",
                schema: "Crm");
        }
    }
}
