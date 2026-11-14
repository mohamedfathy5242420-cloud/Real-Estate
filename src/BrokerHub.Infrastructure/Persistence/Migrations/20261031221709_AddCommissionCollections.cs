using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrokerHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCommissionCollections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_Deals_Id_CustomerId_UnitId_Version_CurrencyCode_SaleOccurredAtUtc_Amount",
                schema: "Sales",
                table: "Deals",
                columns: new[] { "Id", "CustomerId", "UnitId", "Version", "CurrencyCode", "SaleOccurredAtUtc", "Amount" });

            migrationBuilder.CreateTable(
                name: "CommissionCollections",
                schema: "Sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DealId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedVersion = table.Column<long>(type: "bigint", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    EntitledAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SaleOccurredAtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    CollectedAtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    RecordedAtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    RecorderAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommissionCollections", x => x.Id);
                    table.CheckConstraint("CK_CommissionCollections_ClockVersion", "[Version]=1 AND [RequestedVersion]=1 AND [SaleOccurredAtUtc]>'00010101' AND [CollectedAtUtc]>=[SaleOccurredAtUtc] AND [RecordedAtUtc]>=[CollectedAtUtc]");
                    table.CheckConstraint("CK_CommissionCollections_Evidence", "LEN(LTRIM(RTRIM([Source])))>0 AND LEN(LTRIM(RTRIM([Reference])))>0 AND ([Notes] IS NULL OR LEN(LTRIM(RTRIM([Notes])))>0)");
                    table.CheckConstraint("CK_CommissionCollections_Money", "[Amount]>0 AND [EntitledAmount]>=0 AND [Amount]<=[EntitledAmount] AND LEN([CurrencyCode])=3 AND [CurrencyCode] COLLATE Latin1_General_100_BIN2 NOT LIKE N'%[^A-Z]%'");
                    table.ForeignKey(
                        name: "FK_CommissionCollections_Accounts_RecorderAccountId",
                        column: x => x.RecorderAccountId,
                        principalSchema: "Identity",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CommissionCollections_Deals_DealId_CustomerId_UnitId_RequestedVersion_CurrencyCode_SaleOccurredAtUtc_EntitledAmount",
                        columns: x => new { x.DealId, x.CustomerId, x.UnitId, x.RequestedVersion, x.CurrencyCode, x.SaleOccurredAtUtc, x.EntitledAmount },
                        principalSchema: "Sales",
                        principalTable: "Deals",
                        principalColumns: new[] { "Id", "CustomerId", "UnitId", "Version", "CurrencyCode", "SaleOccurredAtUtc", "Amount" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommissionCollections_DealId_CustomerId_UnitId_RequestedVersion_CurrencyCode_SaleOccurredAtUtc_EntitledAmount",
                schema: "Sales",
                table: "CommissionCollections",
                columns: new[] { "DealId", "CustomerId", "UnitId", "RequestedVersion", "CurrencyCode", "SaleOccurredAtUtc", "EntitledAmount" });

            migrationBuilder.CreateIndex(
                name: "IX_CommissionCollections_DealId_RecordedAtUtc_Id",
                schema: "Sales",
                table: "CommissionCollections",
                columns: new[] { "DealId", "RecordedAtUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_CommissionCollections_RecorderAccountId",
                schema: "Sales",
                table: "CommissionCollections",
                column: "RecorderAccountId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommissionCollections",
                schema: "Sales");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Deals_Id_CustomerId_UnitId_Version_CurrencyCode_SaleOccurredAtUtc_Amount",
                schema: "Sales",
                table: "Deals");
        }
    }
}
