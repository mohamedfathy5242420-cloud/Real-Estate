using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrokerHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxDispatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OutboxDeliveries",
                schema: "Reliability",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HumanOutboxId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SystemOutboxId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EventRecordedAtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    State = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: false),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    AvailableAfterUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    LeaseToken = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LeaseExpiresAtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    LastAttemptAtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    PublishedAtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    LastFailureKind = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    LastFailureAtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxDeliveries", x => x.Id);
                    table.CheckConstraint("CK_OutboxDeliveries_Clocks", "[EventRecordedAtUtc]>'00010101' AND [CreatedAtUtc]>=[EventRecordedAtUtc] AND [AvailableAfterUtc]>=[CreatedAtUtc] AND ([LastAttemptAtUtc] IS NULL OR [LastAttemptAtUtc]>=[CreatedAtUtc]) AND ([LastFailureAtUtc] IS NULL OR [LastFailureAtUtc]>=[CreatedAtUtc]) AND [Version]>0 AND [AttemptCount]>=0");
                    table.CheckConstraint("CK_OutboxDeliveries_Failure", "([LastFailureKind]='None' AND [LastFailureAtUtc] IS NULL) OR ([LastFailureKind] IN ('TransportUnavailable','NotConfirmed','ConfirmUncertain','InvalidEnvelope','Rejected','LeaseExpired') AND [LastFailureAtUtc] IS NOT NULL)");
                    table.CheckConstraint("CK_OutboxDeliveries_Source", "([HumanOutboxId] IS NOT NULL AND [SystemOutboxId] IS NULL) OR ([HumanOutboxId] IS NULL AND [SystemOutboxId] IS NOT NULL)");
                    table.CheckConstraint("CK_OutboxDeliveries_State", "([State]='Pending' AND [LeaseToken] IS NULL AND [LeaseExpiresAtUtc] IS NULL AND [PublishedAtUtc] IS NULL) OR ([State]='Leased' AND [LeaseToken] IS NOT NULL AND [LeaseExpiresAtUtc]>[LastAttemptAtUtc] AND [LastAttemptAtUtc] IS NOT NULL AND [AttemptCount]>0 AND [PublishedAtUtc] IS NULL) OR ([State]='Published' AND [LeaseToken] IS NULL AND [LeaseExpiresAtUtc] IS NULL AND [PublishedAtUtc]>=[LastAttemptAtUtc] AND [LastAttemptAtUtc] IS NOT NULL AND [AttemptCount]>0) OR ([State]='DeadLetter' AND [LeaseToken] IS NULL AND [LeaseExpiresAtUtc] IS NULL AND [PublishedAtUtc] IS NULL AND [AttemptCount]>0)");
                    table.ForeignKey(
                        name: "FK_OutboxDeliveries_OutboxMessages_HumanOutboxId",
                        column: x => x.HumanOutboxId,
                        principalSchema: "Reliability",
                        principalTable: "OutboxMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OutboxDeliveries_SystemOutboxMessages_SystemOutboxId",
                        column: x => x.SystemOutboxId,
                        principalSchema: "Reliability",
                        principalTable: "SystemOutboxMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OutboxDispatchFences",
                schema: "Reliability",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxDispatchFences", x => x.Id);
                    table.CheckConstraint("CK_OutboxDispatchFences_Singleton", "[Id]=1");
                });

            migrationBuilder.InsertData(
                schema: "Reliability",
                table: "OutboxDispatchFences",
                column: "Id",
                value: 1);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxDeliveries_HumanOutboxId",
                schema: "Reliability",
                table: "OutboxDeliveries",
                column: "HumanOutboxId",
                unique: true,
                filter: "[HumanOutboxId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxDeliveries_State_AvailableAfterUtc_EventRecordedAtUtc_Id",
                schema: "Reliability",
                table: "OutboxDeliveries",
                columns: new[] { "State", "AvailableAfterUtc", "EventRecordedAtUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxDeliveries_State_LeaseExpiresAtUtc_EventRecordedAtUtc_Id",
                schema: "Reliability",
                table: "OutboxDeliveries",
                columns: new[] { "State", "LeaseExpiresAtUtc", "EventRecordedAtUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxDeliveries_SystemOutboxId",
                schema: "Reliability",
                table: "OutboxDeliveries",
                column: "SystemOutboxId",
                unique: true,
                filter: "[SystemOutboxId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OutboxDeliveries",
                schema: "Reliability");

            migrationBuilder.DropTable(
                name: "OutboxDispatchFences",
                schema: "Reliability");
        }
    }
}
