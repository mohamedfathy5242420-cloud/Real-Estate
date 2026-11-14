using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrokerHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationInbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Messaging");

            migrationBuilder.CreateTable(
                name: "InboxMessages",
                schema: "Reliability",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConsumerName = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    MessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BodyHash = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                    SourceStore = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    SourceEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventName = table.Column<string>(type: "varchar(128)", unicode: false, maxLength: 128, nullable: false),
                    EventSchemaVersion = table.Column<int>(type: "int", nullable: false),
                    NotificationCount = table.Column<int>(type: "int", nullable: false),
                    RecipientDigest = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    SourceRecordedAtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    ProcessedAtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InboxMessages", x => x.Id);
                    table.CheckConstraint("CK_InboxMessages_Clocks", "[OccurredAtUtc]>'00010101' AND [SourceRecordedAtUtc]>=[OccurredAtUtc] AND [ProcessedAtUtc]>=[SourceRecordedAtUtc]");
                    table.CheckConstraint("CK_InboxMessages_Outcome", "[NotificationCount]>=0 AND LEN([BodyHash])=64 AND [BodyHash] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9A-F]%' AND LEN([RecipientDigest])=64 AND [RecipientDigest] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9A-F]%'");
                    table.CheckConstraint("CK_InboxMessages_Source", "[ConsumerName]='BrokerHub.Notifications.v1' AND [SourceStore] IN ('OutboxMessages','SystemOutboxMessages') AND [EventSchemaVersion]=1 AND [SourceEventId]<>'00000000-0000-0000-0000-000000000000'");
                    table.ForeignKey(
                        name: "FK_InboxMessages_OutboxDeliveries_MessageId",
                        column: x => x.MessageId,
                        principalSchema: "Reliability",
                        principalTable: "OutboxDeliveries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NotificationProcessingAudits",
                schema: "Messaging",
                columns: table => new
                {
                    InboxMessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConsumerName = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    SourceStore = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    SourceEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceActorAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SystemSource = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: true),
                    EventName = table.Column<string>(type: "varchar(128)", unicode: false, maxLength: 128, nullable: false),
                    EventSchemaVersion = table.Column<int>(type: "int", nullable: false),
                    BodyHash = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                    NotificationCount = table.Column<int>(type: "int", nullable: false),
                    RecipientDigest = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    ProcessedAtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationProcessingAudits", x => x.InboxMessageId);
                    table.CheckConstraint("CK_NotificationProcessingAudits_Clocks", "[OccurredAtUtc]>'00010101' AND [ProcessedAtUtc]>=[OccurredAtUtc]");
                    table.CheckConstraint("CK_NotificationProcessingAudits_Outcome", "[NotificationCount]>=0 AND [EventSchemaVersion]=1 AND [ConsumerName]='BrokerHub.Notifications.v1' AND LEN([BodyHash])=64 AND [BodyHash] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9A-F]%' AND LEN([RecipientDigest])=64 AND [RecipientDigest] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9A-F]%'");
                    table.CheckConstraint("CK_NotificationProcessingAudits_Provenance", "([SourceStore]='OutboxMessages' AND [SourceActorAccountId] IS NOT NULL AND [SystemSource] IS NULL) OR ([SourceStore]='SystemOutboxMessages' AND [SourceActorAccountId] IS NULL AND [SystemSource] IS NOT NULL AND LEN([SystemSource])>0)");
                    table.ForeignKey(
                        name: "FK_NotificationProcessingAudits_Accounts_SourceActorAccountId",
                        column: x => x.SourceActorAccountId,
                        principalSchema: "Identity",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NotificationProcessingAudits_InboxMessages_InboxMessageId",
                        column: x => x.InboxMessageId,
                        principalSchema: "Reliability",
                        principalTable: "InboxMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                schema: "Messaging",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InboxMessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecipientAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EventName = table.Column<string>(type: "varchar(128)", unicode: false, maxLength: 128, nullable: false),
                    ResourceKind = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    ResourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentResourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RelatedResourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.CheckConstraint("CK_Notifications_Clocks", "[OccurredAtUtc]>'00010101' AND [CreatedAtUtc]>=[OccurredAtUtc]");
                    table.CheckConstraint("CK_Notifications_Identities", "[Id]<>'00000000-0000-0000-0000-000000000000' AND [RecipientAccountId]<>'00000000-0000-0000-0000-000000000000' AND [ResourceId]<>'00000000-0000-0000-0000-000000000000' AND ([ParentResourceId] IS NULL OR [ParentResourceId]<>'00000000-0000-0000-0000-000000000000') AND ([RelatedResourceId] IS NULL OR [RelatedResourceId]<>'00000000-0000-0000-0000-000000000000')");
                    table.CheckConstraint("CK_Notifications_Scope", "([ResourceKind]='Unit' AND [CustomerId] IS NULL) OR ([ResourceKind] IN ('Customer','Viewing','Booking','BookingDeposit','BookingRefund','Deal','CommissionCollection') AND [CustomerId] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_Notifications_Accounts_RecipientAccountId",
                        column: x => x.RecipientAccountId,
                        principalSchema: "Identity",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Notifications_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "Crm",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Notifications_InboxMessages_InboxMessageId",
                        column: x => x.InboxMessageId,
                        principalSchema: "Reliability",
                        principalTable: "InboxMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InboxMessages_MessageId",
                schema: "Reliability",
                table: "InboxMessages",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "UX_InboxMessages_ConsumerMessage",
                schema: "Reliability",
                table: "InboxMessages",
                columns: new[] { "ConsumerName", "MessageId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationProcessingAudits_SourceActorAccountId",
                schema: "Messaging",
                table: "NotificationProcessingAudits",
                column: "SourceActorAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_CustomerId",
                schema: "Messaging",
                table: "Notifications",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_InboxMessageId_RecipientAccountId",
                schema: "Messaging",
                table: "Notifications",
                columns: new[] { "InboxMessageId", "RecipientAccountId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_RecipientAccountId_CreatedAtUtc_Id",
                schema: "Messaging",
                table: "Notifications",
                columns: new[] { "RecipientAccountId", "CreatedAtUtc", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificationProcessingAudits",
                schema: "Messaging");

            migrationBuilder.DropTable(
                name: "Notifications",
                schema: "Messaging");

            migrationBuilder.DropTable(
                name: "InboxMessages",
                schema: "Reliability");
        }
    }
}
