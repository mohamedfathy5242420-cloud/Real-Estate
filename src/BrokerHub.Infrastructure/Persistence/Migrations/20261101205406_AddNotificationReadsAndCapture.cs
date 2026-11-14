using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrokerHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationReadsAndCapture : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_Notifications_Id_CreatedAtUtc",
                schema: "Messaging",
                table: "Notifications",
                columns: new[] { "Id", "CreatedAtUtc" });

            migrationBuilder.CreateTable(
                name: "NotificationEmailCaptures",
                schema: "Messaging",
                columns: table => new
                {
                    NotificationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NotificationCreatedAtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    ContentHash = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                    CapturedAtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationEmailCaptures", x => x.NotificationId);
                    table.CheckConstraint("CK_NotificationEmailCaptures_Clock", "[NotificationCreatedAtUtc]>'00010101' AND [CapturedAtUtc]>=[NotificationCreatedAtUtc]");
                    table.CheckConstraint("CK_NotificationEmailCaptures_Hash", "LEN([ContentHash])=64 AND [ContentHash] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9A-F]%'");
                    table.ForeignKey(
                        name: "FK_NotificationEmailCaptures_Notifications_NotificationId_NotificationCreatedAtUtc",
                        columns: x => new { x.NotificationId, x.NotificationCreatedAtUtc },
                        principalSchema: "Messaging",
                        principalTable: "Notifications",
                        principalColumns: new[] { "Id", "CreatedAtUtc" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NotificationReadStates",
                schema: "Messaging",
                columns: table => new
                {
                    NotificationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NotificationCreatedAtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    ReadAtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationReadStates", x => x.NotificationId);
                    table.CheckConstraint("CK_NotificationReadStates_Clock", "[NotificationCreatedAtUtc]>'00010101' AND [ReadAtUtc]>=[NotificationCreatedAtUtc]");
                    table.ForeignKey(
                        name: "FK_NotificationReadStates_Notifications_NotificationId_NotificationCreatedAtUtc",
                        columns: x => new { x.NotificationId, x.NotificationCreatedAtUtc },
                        principalSchema: "Messaging",
                        principalTable: "Notifications",
                        principalColumns: new[] { "Id", "CreatedAtUtc" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationEmailCaptures_NotificationId_NotificationCreatedAtUtc",
                schema: "Messaging",
                table: "NotificationEmailCaptures",
                columns: new[] { "NotificationId", "NotificationCreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationReadStates_NotificationId_NotificationCreatedAtUtc",
                schema: "Messaging",
                table: "NotificationReadStates",
                columns: new[] { "NotificationId", "NotificationCreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificationEmailCaptures",
                schema: "Messaging");

            migrationBuilder.DropTable(
                name: "NotificationReadStates",
                schema: "Messaging");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Notifications_Id_CreatedAtUtc",
                schema: "Messaging",
                table: "Notifications");
        }
    }
}
