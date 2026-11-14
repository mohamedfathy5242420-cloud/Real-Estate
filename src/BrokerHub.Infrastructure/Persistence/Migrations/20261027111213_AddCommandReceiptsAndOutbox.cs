using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrokerHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCommandReceiptsAndOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Reliability");

            migrationBuilder.CreateTable(
                name: "CommandReceipts",
                schema: "Reliability",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Operation = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false, collation: "Latin1_General_100_BIN2"),
                    IdempotencyKey = table.Column<string>(type: "varchar(128)", unicode: false, maxLength: 128, nullable: false, collation: "Latin1_General_100_BIN2"),
                    RequestHash = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                    FingerprintVersion = table.Column<int>(type: "int", nullable: false),
                    ResourceType = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    ResourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResourceVersion = table.Column<long>(type: "bigint", nullable: false),
                    EventCount = table.Column<int>(type: "int", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommandReceipts", x => x.Id);
                    table.CheckConstraint("CK_CommandReceipts_Fingerprint", "[FingerprintVersion]=1 AND LEN([RequestHash])=64 AND [RequestHash] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9A-F]%'");
                    table.CheckConstraint("CK_CommandReceipts_Identity", "LEN([Operation])>0 AND LEN([IdempotencyKey])>0 AND [Operation] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^a-zA-Z0-9._:-]%' AND LEFT([Operation],1) COLLATE Latin1_General_100_BIN2 LIKE '[a-zA-Z]' AND [IdempotencyKey] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^a-zA-Z0-9._:-]%'");
                    table.CheckConstraint("CK_CommandReceipts_Outcome", "LEN([ResourceType])>0 AND [ResourceVersion]>0 AND [EventCount] BETWEEN 0 AND 100");
                    table.ForeignKey(
                        name: "FK_CommandReceipts_Accounts_ActorAccountId",
                        column: x => x.ActorAccountId,
                        principalSchema: "Identity",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                schema: "Reliability",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CommandReceiptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "varchar(128)", unicode: false, maxLength: 128, nullable: false),
                    SchemaVersion = table.Column<int>(type: "int", nullable: false),
                    AggregateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", maxLength: 16384, nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    RecordedAtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                    table.CheckConstraint("CK_OutboxMessages_Metadata", "LEN([Name])>0 AND [SchemaVersion]>0 AND [RecordedAtUtc]>=[OccurredAtUtc]");
                    table.CheckConstraint("CK_OutboxMessages_Payload", "ISJSON([PayloadJson])=1 AND LEFT(LTRIM([PayloadJson]),1)=N'{' AND DATALENGTH([PayloadJson])<=32768");
                    table.ForeignKey(
                        name: "FK_OutboxMessages_Accounts_ActorAccountId",
                        column: x => x.ActorAccountId,
                        principalSchema: "Identity",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OutboxMessages_CommandReceipts_CommandReceiptId",
                        column: x => x.CommandReceiptId,
                        principalSchema: "Reliability",
                        principalTable: "CommandReceipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "UX_CommandReceipts_ActorOperationKey",
                schema: "Reliability",
                table: "CommandReceipts",
                columns: new[] { "ActorAccountId", "Operation", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ActorAccountId",
                schema: "Reliability",
                table: "OutboxMessages",
                column: "ActorAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_CommandReceiptId",
                schema: "Reliability",
                table: "OutboxMessages",
                column: "CommandReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_RecordedAtUtc_Id",
                schema: "Reliability",
                table: "OutboxMessages",
                columns: new[] { "RecordedAtUtc", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OutboxMessages",
                schema: "Reliability");

            migrationBuilder.DropTable(
                name: "CommandReceipts",
                schema: "Reliability");
        }
    }
}
