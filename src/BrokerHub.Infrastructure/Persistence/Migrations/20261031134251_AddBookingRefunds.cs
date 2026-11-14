using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrokerHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingRefunds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_BookingDeposits_Id_BookingId_CustomerId_UnitId_CurrencyCode_PaidAtUtc",
                schema: "Crm",
                table: "BookingDeposits",
                columns: new[] { "Id", "BookingId", "CustomerId", "UnitId", "CurrencyCode", "PaidAtUtc" });

            migrationBuilder.CreateTable(
                name: "BookingRefunds",
                schema: "Crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepositId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedVersion = table.Column<long>(type: "bigint", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    DepositPaidAtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    ReturnedAtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
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
                    table.PrimaryKey("PK_BookingRefunds", x => x.Id);
                    table.CheckConstraint("CK_BookingRefunds_ClockVersion", "[Version]=1 AND [RequestedVersion]>0 AND [DepositPaidAtUtc]>'00010101' AND [ReturnedAtUtc]>=[DepositPaidAtUtc] AND [RecordedAtUtc]>=[ReturnedAtUtc]");
                    table.CheckConstraint("CK_BookingRefunds_Evidence", "LEN(LTRIM(RTRIM([Source])))>0 AND LEN(LTRIM(RTRIM([Reference])))>0 AND ([Notes] IS NULL OR LEN(LTRIM(RTRIM([Notes])))>0)");
                    table.CheckConstraint("CK_BookingRefunds_Money", "[Amount]>0 AND LEN([CurrencyCode])=3 AND [CurrencyCode] COLLATE Latin1_General_100_BIN2 NOT LIKE N'%[^A-Z]%'");
                    table.ForeignKey(
                        name: "FK_BookingRefunds_Accounts_RecorderAccountId",
                        column: x => x.RecorderAccountId,
                        principalSchema: "Identity",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BookingRefunds_BookingDeposits_DepositId_BookingId_CustomerId_UnitId_CurrencyCode_DepositPaidAtUtc",
                        columns: x => new { x.DepositId, x.BookingId, x.CustomerId, x.UnitId, x.CurrencyCode, x.DepositPaidAtUtc },
                        principalSchema: "Crm",
                        principalTable: "BookingDeposits",
                        principalColumns: new[] { "Id", "BookingId", "CustomerId", "UnitId", "CurrencyCode", "PaidAtUtc" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BookingRefunds_Employees_RecorderEmployeeId",
                        column: x => x.RecorderEmployeeId,
                        principalSchema: "Crm",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BookingRefunds_DepositId_BookingId_CustomerId_UnitId_CurrencyCode_DepositPaidAtUtc",
                schema: "Crm",
                table: "BookingRefunds",
                columns: new[] { "DepositId", "BookingId", "CustomerId", "UnitId", "CurrencyCode", "DepositPaidAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_BookingRefunds_DepositId_RecordedAtUtc_Id",
                schema: "Crm",
                table: "BookingRefunds",
                columns: new[] { "DepositId", "RecordedAtUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_BookingRefunds_RecorderAccountId",
                schema: "Crm",
                table: "BookingRefunds",
                column: "RecorderAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingRefunds_RecorderEmployeeId",
                schema: "Crm",
                table: "BookingRefunds",
                column: "RecorderEmployeeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookingRefunds",
                schema: "Crm");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_BookingDeposits_Id_BookingId_CustomerId_UnitId_CurrencyCode_PaidAtUtc",
                schema: "Crm",
                table: "BookingDeposits");
        }
    }
}
