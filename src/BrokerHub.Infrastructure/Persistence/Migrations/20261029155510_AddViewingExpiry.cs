using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrokerHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddViewingExpiry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Viewings_InitialState",
                schema: "Crm",
                table: "Viewings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ViewingEvents_Shape",
                schema: "Crm",
                table: "ViewingEvents");

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiredAtUtc",
                schema: "Crm",
                table: "Viewings",
                type: "datetime2(7)",
                precision: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExpiredFromStatus",
                schema: "Crm",
                table: "Viewings",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ActorAccountId",
                schema: "Crm",
                table: "ViewingEvents",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<string>(
                name: "ActorKind",
                schema: "Crm",
                table: "ViewingEvents",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "User");

            migrationBuilder.AddColumn<DateTime>(
                name: "EffectiveCutoffUtc",
                schema: "Crm",
                table: "ViewingEvents",
                type: "datetime2(7)",
                precision: 7,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SystemOutboxMessages",
                schema: "Reliability",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Source = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "varchar(128)", unicode: false, maxLength: 128, nullable: false),
                    SchemaVersion = table.Column<int>(type: "int", nullable: false),
                    AggregateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResourceVersion = table.Column<long>(type: "bigint", nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", maxLength: 16384, nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    RecordedAtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemOutboxMessages", x => x.Id);
                    table.CheckConstraint("CK_SystemOutboxMessages_Metadata", "LEN([Source])>0 AND LEN([Name])>0 AND [SchemaVersion]>0 AND [ResourceVersion]>0 AND [OccurredAtUtc]>'00010101' AND [RecordedAtUtc]>=[OccurredAtUtc]");
                    table.CheckConstraint("CK_SystemOutboxMessages_Payload", "ISJSON([PayloadJson])=1 AND LEFT(LTRIM([PayloadJson]),1)=N'{' AND DATALENGTH([PayloadJson])<=32768");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Viewings_PendingExpiry",
                schema: "Crm",
                table: "Viewings",
                columns: new[] { "Status", "CurrentProposedStartUtc", "PendingCycleStartedAtUtc", "Id" },
                filter: "[Status] IN (N'PendingConfirmation',N'AwaitingCustomerAcceptance')")
                .Annotation("SqlServer:Include", new[] { "CustomerId", "Version" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Viewings_ExpiryAudit",
                schema: "Crm",
                table: "Viewings",
                sql: "[ExpiredAtUtc] IS NULL OR ([ExpiredAtUtc]>=[PendingCycleStartedAtUtc] AND [ExpiredAtUtc]>=CASE WHEN [ExpiredFromStatus]=N'AwaitingCustomerAcceptance' OR [CurrentProposedStartUtc]<DATEADD(day,7,[PendingCycleStartedAtUtc]) THEN [CurrentProposedStartUtc] ELSE DATEADD(day,7,[PendingCycleStartedAtUtc]) END)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Viewings_InitialState",
                schema: "Crm",
                table: "Viewings",
                sql: "([Status]=N'PendingConfirmation' AND [Version]>=1 AND [ConfirmedStartUtc] IS NULL AND [ConfirmedEndUtc] IS NULL AND [ConfirmedEmployeeId] IS NULL AND [ConfirmedAtUtc] IS NULL AND [ConfirmedByAccountId] IS NULL AND [CoordinatedAtUtc] IS NULL AND [CoordinationSource] IS NULL AND [CoordinationReference] IS NULL AND [OfferedAtUtc] IS NULL AND [OfferedByAccountId] IS NULL AND [OfferedEmployeeId] IS NULL AND [CancelledAtUtc] IS NULL AND [CancelledByAccountId] IS NULL AND [CancellationKind] IS NULL AND [CancellationReason] IS NULL AND [CancelledFromStatus] IS NULL AND [ExpiredAtUtc] IS NULL AND [ExpiredFromStatus] IS NULL) OR ([Status]=N'AwaitingCustomerAcceptance' AND [Version]>1 AND [ConfirmedStartUtc] IS NULL AND [ConfirmedEndUtc] IS NULL AND [ConfirmedEmployeeId] IS NULL AND [ConfirmedAtUtc] IS NULL AND [ConfirmedByAccountId] IS NULL AND [CoordinatedAtUtc] IS NOT NULL AND [CoordinationSource] IS NOT NULL AND [OfferedAtUtc] IS NOT NULL AND [OfferedByAccountId] IS NOT NULL AND [OfferedEmployeeId] IS NOT NULL AND [CancelledAtUtc] IS NULL AND [CancelledByAccountId] IS NULL AND [CancellationKind] IS NULL AND [CancellationReason] IS NULL AND [CancelledFromStatus] IS NULL AND [ExpiredAtUtc] IS NULL AND [ExpiredFromStatus] IS NULL) OR ([Status]=N'Confirmed' AND [Version]>1 AND [ConfirmedStartUtc] IS NOT NULL AND [ConfirmedEndUtc] IS NOT NULL AND [ConfirmedEmployeeId] IS NOT NULL AND [ConfirmedAtUtc] IS NOT NULL AND [ConfirmedByAccountId] IS NOT NULL AND [CoordinatedAtUtc] IS NOT NULL AND [CoordinationSource] IS NOT NULL AND (([OfferedAtUtc] IS NULL AND [OfferedByAccountId] IS NULL AND [OfferedEmployeeId] IS NULL) OR ([OfferedAtUtc] IS NOT NULL AND [OfferedByAccountId] IS NOT NULL AND [OfferedEmployeeId] IS NOT NULL AND [OfferedEmployeeId]=[ConfirmedEmployeeId])) AND [CancelledAtUtc] IS NULL AND [CancelledByAccountId] IS NULL AND [CancellationKind] IS NULL AND [CancellationReason] IS NULL AND [CancelledFromStatus] IS NULL AND [ExpiredAtUtc] IS NULL AND [ExpiredFromStatus] IS NULL) OR ([Status]=N'Cancelled' AND [Version]>1 AND [CancelledAtUtc] IS NOT NULL AND [CancelledByAccountId] IS NOT NULL AND [CancellationKind] IS NOT NULL AND [CancellationReason] IS NOT NULL AND [CancelledFromStatus] IS NOT NULL AND [ExpiredAtUtc] IS NULL AND [ExpiredFromStatus] IS NULL AND (([CancelledFromStatus]=N'PendingConfirmation' AND [Version]>=1 AND [ConfirmedStartUtc] IS NULL AND [ConfirmedEndUtc] IS NULL AND [ConfirmedEmployeeId] IS NULL AND [ConfirmedAtUtc] IS NULL AND [ConfirmedByAccountId] IS NULL AND [CoordinatedAtUtc] IS NULL AND [CoordinationSource] IS NULL AND [CoordinationReference] IS NULL AND [OfferedAtUtc] IS NULL AND [OfferedByAccountId] IS NULL AND [OfferedEmployeeId] IS NULL) OR ([CancelledFromStatus]=N'AwaitingCustomerAcceptance' AND [Version]>1 AND [ConfirmedStartUtc] IS NULL AND [ConfirmedEndUtc] IS NULL AND [ConfirmedEmployeeId] IS NULL AND [ConfirmedAtUtc] IS NULL AND [ConfirmedByAccountId] IS NULL AND [CoordinatedAtUtc] IS NOT NULL AND [CoordinationSource] IS NOT NULL AND [OfferedAtUtc] IS NOT NULL AND [OfferedByAccountId] IS NOT NULL AND [OfferedEmployeeId] IS NOT NULL) OR ([CancelledFromStatus]=N'Confirmed' AND [Version]>1 AND [ConfirmedStartUtc] IS NOT NULL AND [ConfirmedEndUtc] IS NOT NULL AND [ConfirmedEmployeeId] IS NOT NULL AND [ConfirmedAtUtc] IS NOT NULL AND [ConfirmedByAccountId] IS NOT NULL AND [CoordinatedAtUtc] IS NOT NULL AND [CoordinationSource] IS NOT NULL AND (([OfferedAtUtc] IS NULL AND [OfferedByAccountId] IS NULL AND [OfferedEmployeeId] IS NULL) OR ([OfferedAtUtc] IS NOT NULL AND [OfferedByAccountId] IS NOT NULL AND [OfferedEmployeeId] IS NOT NULL AND [OfferedEmployeeId]=[ConfirmedEmployeeId]))))) OR ([Status]=N'Expired' AND [Version]>1 AND [ExpiredAtUtc] IS NOT NULL AND [ExpiredFromStatus] IS NOT NULL AND [CancelledAtUtc] IS NULL AND [CancelledByAccountId] IS NULL AND [CancellationKind] IS NULL AND [CancellationReason] IS NULL AND [CancelledFromStatus] IS NULL AND (([ExpiredFromStatus]=N'PendingConfirmation' AND [Version]>=1 AND [ConfirmedStartUtc] IS NULL AND [ConfirmedEndUtc] IS NULL AND [ConfirmedEmployeeId] IS NULL AND [ConfirmedAtUtc] IS NULL AND [ConfirmedByAccountId] IS NULL AND [CoordinatedAtUtc] IS NULL AND [CoordinationSource] IS NULL AND [CoordinationReference] IS NULL AND [OfferedAtUtc] IS NULL AND [OfferedByAccountId] IS NULL AND [OfferedEmployeeId] IS NULL) OR ([ExpiredFromStatus]=N'AwaitingCustomerAcceptance' AND [Version]>1 AND [ConfirmedStartUtc] IS NULL AND [ConfirmedEndUtc] IS NULL AND [ConfirmedEmployeeId] IS NULL AND [ConfirmedAtUtc] IS NULL AND [ConfirmedByAccountId] IS NULL AND [CoordinatedAtUtc] IS NOT NULL AND [CoordinationSource] IS NOT NULL AND [OfferedAtUtc] IS NOT NULL AND [OfferedByAccountId] IS NOT NULL AND [OfferedEmployeeId] IS NOT NULL)))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ViewingEvents_Shape",
                schema: "Crm",
                table: "ViewingEvents",
                sql: "[AtUtc]>'00010101' AND [ProposedStartUtc]>'00010101' AND [ProposedEndUtc]>[ProposedStartUtc] AND ((([ActorKind]=N'User' AND [ActorAccountId] IS NOT NULL AND [EffectiveCutoffUtc] IS NULL) AND [ProposedStartUtc]>[AtUtc] AND [ProposedStartUtc]<=DATEADD(day,30,[AtUtc]) AND (([Kind]=N'Requested' AND [ViewingVersion]=1 AND [ConfirmedStartUtc] IS NULL AND [ConfirmedEndUtc] IS NULL AND [ConfirmedEmployeeId] IS NULL AND [CoordinatedAtUtc] IS NULL AND [CoordinationSource] IS NULL AND [CoordinationReference] IS NULL AND [PreviousProposedStartUtc] IS NULL AND [PreviousProposedEndUtc] IS NULL AND [Reason] IS NULL AND [PreviousStatus] IS NULL AND [PreviousConfirmedStartUtc] IS NULL AND [PreviousConfirmedEndUtc] IS NULL AND [PreviousConfirmedEmployeeId] IS NULL AND [CancellationKind] IS NULL AND [OfferedEmployeeId] IS NULL AND [ProposedEndUtc]=DATEADD(minute,60,[ProposedStartUtc])) OR ([Kind]=N'Confirmed' AND [ViewingVersion]>1 AND [ConfirmedStartUtc] IS NOT NULL AND [ConfirmedEndUtc] IS NOT NULL AND [ConfirmedEmployeeId] IS NOT NULL AND [ConfirmedStartUtc]=[ProposedStartUtc] AND [ConfirmedEndUtc]>[ConfirmedStartUtc] AND [CoordinatedAtUtc] IS NOT NULL AND [CoordinationSource] IS NOT NULL AND [CoordinatedAtUtc]>'00010101' AND [CoordinatedAtUtc]<=[AtUtc] AND LEN(LTRIM(RTRIM([CoordinationSource])))>0 AND ([CoordinationReference] IS NULL OR LEN(LTRIM(RTRIM([CoordinationReference])))>0) AND [PreviousProposedStartUtc] IS NULL AND [PreviousProposedEndUtc] IS NULL AND [Reason] IS NULL AND [PreviousStatus] IS NULL AND [PreviousConfirmedStartUtc] IS NULL AND [PreviousConfirmedEndUtc] IS NULL AND [PreviousConfirmedEmployeeId] IS NULL AND [CancellationKind] IS NULL AND [OfferedEmployeeId] IS NULL AND [ProposedEndUtc]=DATEADD(minute,60,[ProposedStartUtc])) OR ([Kind]=N'AlternativeOffered' AND [ViewingVersion]>1 AND [ConfirmedStartUtc] IS NULL AND [ConfirmedEndUtc] IS NULL AND [ConfirmedEmployeeId] IS NULL AND [CoordinatedAtUtc] IS NOT NULL AND [CoordinationSource] IS NOT NULL AND [CoordinatedAtUtc]>'00010101' AND [CoordinatedAtUtc]<=[AtUtc] AND LEN(LTRIM(RTRIM([CoordinationSource])))>0 AND ([CoordinationReference] IS NULL OR LEN(LTRIM(RTRIM([CoordinationReference])))>0) AND [PreviousStatus] IS NULL AND [PreviousConfirmedStartUtc] IS NULL AND [PreviousConfirmedEndUtc] IS NULL AND [PreviousConfirmedEmployeeId] IS NULL AND [CancellationKind] IS NULL AND [OfferedEmployeeId] IS NOT NULL AND [PreviousProposedStartUtc] IS NOT NULL AND [PreviousProposedEndUtc] IS NOT NULL AND [PreviousProposedStartUtc]>'00010101' AND [PreviousProposedEndUtc]>[PreviousProposedStartUtc] AND [Reason] IS NOT NULL AND LEN(LTRIM(RTRIM([Reason])))>0) OR ([Kind]=N'AlternativeAccepted' AND [ViewingVersion]>=3 AND [ConfirmedStartUtc] IS NOT NULL AND [ConfirmedEndUtc] IS NOT NULL AND [ConfirmedEmployeeId] IS NOT NULL AND [ConfirmedStartUtc]=[ProposedStartUtc] AND [ConfirmedEndUtc]>[ConfirmedStartUtc] AND [CoordinatedAtUtc] IS NOT NULL AND [CoordinationSource] IS NOT NULL AND [CoordinatedAtUtc]>'00010101' AND [CoordinatedAtUtc]<=[AtUtc] AND LEN(LTRIM(RTRIM([CoordinationSource])))>0 AND ([CoordinationReference] IS NULL OR LEN(LTRIM(RTRIM([CoordinationReference])))>0) AND [PreviousProposedStartUtc] IS NULL AND [PreviousProposedEndUtc] IS NULL AND [Reason] IS NULL AND [PreviousStatus] IS NULL AND [PreviousConfirmedStartUtc] IS NULL AND [PreviousConfirmedEndUtc] IS NULL AND [PreviousConfirmedEmployeeId] IS NULL AND [CancellationKind] IS NULL AND [OfferedEmployeeId] IS NOT NULL AND [OfferedEmployeeId]=[ConfirmedEmployeeId] AND [ConfirmedEndUtc]=[ProposedEndUtc]) OR ([Kind]=N'Rescheduled' AND [ViewingVersion]>1 AND [PreviousStatus] IS NOT NULL AND [ConfirmedStartUtc] IS NULL AND [ConfirmedEndUtc] IS NULL AND [ConfirmedEmployeeId] IS NULL AND [CoordinatedAtUtc] IS NULL AND [CoordinationSource] IS NULL AND [CoordinationReference] IS NULL AND [Reason] IS NOT NULL AND LEN(LTRIM(RTRIM([Reason])))>0 AND [OfferedEmployeeId] IS NULL AND [CancellationKind] IS NULL AND [ProposedEndUtc]=DATEADD(minute,60,[ProposedStartUtc]) AND [PreviousProposedStartUtc] IS NOT NULL AND [PreviousProposedEndUtc] IS NOT NULL AND [PreviousProposedStartUtc]>[AtUtc] AND [PreviousProposedEndUtc]>[PreviousProposedStartUtc] AND (([PreviousStatus] IN(N'PendingConfirmation',N'AwaitingCustomerAcceptance') AND [PreviousConfirmedStartUtc] IS NULL AND [PreviousConfirmedEndUtc] IS NULL AND [PreviousConfirmedEmployeeId] IS NULL) OR ([PreviousStatus]=N'Confirmed' AND [PreviousConfirmedStartUtc] IS NOT NULL AND [PreviousConfirmedEndUtc] IS NOT NULL AND [PreviousConfirmedEmployeeId] IS NOT NULL AND [PreviousConfirmedStartUtc]>[AtUtc] AND [PreviousConfirmedEndUtc]>[PreviousConfirmedStartUtc]))) OR ([Kind]=N'Cancelled' AND [ViewingVersion]>1 AND [PreviousStatus] IS NOT NULL AND [Reason] IS NOT NULL AND LEN(LTRIM(RTRIM([Reason])))>0 AND [PreviousProposedStartUtc] IS NULL AND [PreviousProposedEndUtc] IS NULL AND [PreviousConfirmedStartUtc] IS NULL AND [PreviousConfirmedEndUtc] IS NULL AND [PreviousConfirmedEmployeeId] IS NULL AND [CancellationKind] IS NOT NULL AND [CancellationKind] IN(N'Customer',N'Broker',N'Developer') AND (([PreviousStatus]=N'PendingConfirmation' AND [ConfirmedStartUtc] IS NULL AND [ConfirmedEndUtc] IS NULL AND [ConfirmedEmployeeId] IS NULL AND [CoordinatedAtUtc] IS NULL AND [CoordinationSource] IS NULL AND [CoordinationReference] IS NULL AND [OfferedEmployeeId] IS NULL) OR ([PreviousStatus]=N'AwaitingCustomerAcceptance' AND [ConfirmedStartUtc] IS NULL AND [ConfirmedEndUtc] IS NULL AND [ConfirmedEmployeeId] IS NULL AND [CoordinatedAtUtc] IS NOT NULL AND [CoordinationSource] IS NOT NULL AND [CoordinatedAtUtc]>'00010101' AND [CoordinatedAtUtc]<=[AtUtc] AND LEN(LTRIM(RTRIM([CoordinationSource])))>0 AND ([CoordinationReference] IS NULL OR LEN(LTRIM(RTRIM([CoordinationReference])))>0) AND [OfferedEmployeeId] IS NOT NULL) OR ([PreviousStatus]=N'Confirmed' AND [ConfirmedStartUtc] IS NOT NULL AND [ConfirmedEndUtc] IS NOT NULL AND [ConfirmedEmployeeId] IS NOT NULL AND [ConfirmedStartUtc]=[ProposedStartUtc] AND [ConfirmedEndUtc]>[ConfirmedStartUtc] AND [CoordinatedAtUtc] IS NOT NULL AND [CoordinationSource] IS NOT NULL AND [CoordinatedAtUtc]>'00010101' AND [CoordinatedAtUtc]<=[AtUtc] AND LEN(LTRIM(RTRIM([CoordinationSource])))>0 AND ([CoordinationReference] IS NULL OR LEN(LTRIM(RTRIM([CoordinationReference])))>0) AND ([OfferedEmployeeId] IS NULL OR [OfferedEmployeeId]=[ConfirmedEmployeeId])))))) OR ([Kind]=N'Expired' AND [ViewingVersion]>1 AND [ActorKind]=N'System' AND [ActorAccountId] IS NULL AND [EffectiveCutoffUtc] IS NOT NULL AND [EffectiveCutoffUtc]>'00010101' AND [EffectiveCutoffUtc]<=[AtUtc] AND [EffectiveCutoffUtc]<=[ProposedStartUtc] AND [PreviousStatus] IS NOT NULL AND [Reason] IS NOT NULL AND [ConfirmedStartUtc] IS NULL AND [ConfirmedEndUtc] IS NULL AND [ConfirmedEmployeeId] IS NULL AND [PreviousConfirmedStartUtc] IS NULL AND [PreviousConfirmedEndUtc] IS NULL AND [PreviousConfirmedEmployeeId] IS NULL AND [PreviousProposedStartUtc] IS NULL AND [PreviousProposedEndUtc] IS NULL AND [CancellationKind] IS NULL AND (([PreviousStatus]=N'PendingConfirmation' AND [CoordinatedAtUtc] IS NULL AND [CoordinationSource] IS NULL AND [CoordinationReference] IS NULL AND [OfferedEmployeeId] IS NULL AND [Reason]=N'CoordinationDeadline') OR ([PreviousStatus]=N'AwaitingCustomerAcceptance' AND [CoordinatedAtUtc] IS NOT NULL AND [CoordinationSource] IS NOT NULL AND [CoordinatedAtUtc]>'00010101' AND [CoordinatedAtUtc]<=[AtUtc] AND LEN(LTRIM(RTRIM([CoordinationSource])))>0 AND ([CoordinationReference] IS NULL OR LEN(LTRIM(RTRIM([CoordinationReference])))>0) AND [OfferedEmployeeId] IS NOT NULL AND [EffectiveCutoffUtc]=[ProposedStartUtc] AND [Reason]=N'AlternativeStart'))))");

            migrationBuilder.CreateIndex(
                name: "IX_SystemOutboxMessages_RecordedAtUtc_Id",
                schema: "Reliability",
                table: "SystemOutboxMessages",
                columns: new[] { "RecordedAtUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_SystemOutboxMessages_ResourceId_ResourceVersion_Name",
                schema: "Reliability",
                table: "SystemOutboxMessages",
                columns: new[] { "ResourceId", "ResourceVersion", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SystemOutboxMessages",
                schema: "Reliability");

            migrationBuilder.DropIndex(
                name: "IX_Viewings_PendingExpiry",
                schema: "Crm",
                table: "Viewings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Viewings_ExpiryAudit",
                schema: "Crm",
                table: "Viewings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Viewings_InitialState",
                schema: "Crm",
                table: "Viewings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ViewingEvents_Shape",
                schema: "Crm",
                table: "ViewingEvents");

            migrationBuilder.DropColumn(
                name: "ExpiredAtUtc",
                schema: "Crm",
                table: "Viewings");

            migrationBuilder.DropColumn(
                name: "ExpiredFromStatus",
                schema: "Crm",
                table: "Viewings");

            migrationBuilder.DropColumn(
                name: "ActorKind",
                schema: "Crm",
                table: "ViewingEvents");

            migrationBuilder.DropColumn(
                name: "EffectiveCutoffUtc",
                schema: "Crm",
                table: "ViewingEvents");

            migrationBuilder.AlterColumn<Guid>(
                name: "ActorAccountId",
                schema: "Crm",
                table: "ViewingEvents",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Viewings_InitialState",
                schema: "Crm",
                table: "Viewings",
                sql: "([Status]=N'PendingConfirmation' AND [Version]>=1 AND [ConfirmedStartUtc] IS NULL AND [ConfirmedEndUtc] IS NULL AND [ConfirmedEmployeeId] IS NULL AND [ConfirmedAtUtc] IS NULL AND [ConfirmedByAccountId] IS NULL AND [CoordinatedAtUtc] IS NULL AND [CoordinationSource] IS NULL AND [CoordinationReference] IS NULL AND [OfferedAtUtc] IS NULL AND [OfferedByAccountId] IS NULL AND [OfferedEmployeeId] IS NULL AND [CancelledAtUtc] IS NULL AND [CancelledByAccountId] IS NULL AND [CancellationKind] IS NULL AND [CancellationReason] IS NULL AND [CancelledFromStatus] IS NULL) OR ([Status]=N'AwaitingCustomerAcceptance' AND [Version]>1 AND [ConfirmedStartUtc] IS NULL AND [ConfirmedEndUtc] IS NULL AND [ConfirmedEmployeeId] IS NULL AND [ConfirmedAtUtc] IS NULL AND [ConfirmedByAccountId] IS NULL AND [CoordinatedAtUtc] IS NOT NULL AND [CoordinationSource] IS NOT NULL AND [OfferedAtUtc] IS NOT NULL AND [OfferedByAccountId] IS NOT NULL AND [OfferedEmployeeId] IS NOT NULL AND [CancelledAtUtc] IS NULL AND [CancelledByAccountId] IS NULL AND [CancellationKind] IS NULL AND [CancellationReason] IS NULL AND [CancelledFromStatus] IS NULL) OR ([Status]=N'Confirmed' AND [Version]>1 AND [ConfirmedStartUtc] IS NOT NULL AND [ConfirmedEndUtc] IS NOT NULL AND [ConfirmedEmployeeId] IS NOT NULL AND [ConfirmedAtUtc] IS NOT NULL AND [ConfirmedByAccountId] IS NOT NULL AND [CoordinatedAtUtc] IS NOT NULL AND [CoordinationSource] IS NOT NULL AND (([OfferedAtUtc] IS NULL AND [OfferedByAccountId] IS NULL AND [OfferedEmployeeId] IS NULL) OR ([OfferedAtUtc] IS NOT NULL AND [OfferedByAccountId] IS NOT NULL AND [OfferedEmployeeId] IS NOT NULL AND [OfferedEmployeeId]=[ConfirmedEmployeeId])) AND [CancelledAtUtc] IS NULL AND [CancelledByAccountId] IS NULL AND [CancellationKind] IS NULL AND [CancellationReason] IS NULL AND [CancelledFromStatus] IS NULL) OR ([Status]=N'Cancelled' AND [Version]>1 AND [CancelledAtUtc] IS NOT NULL AND [CancelledByAccountId] IS NOT NULL AND [CancellationKind] IS NOT NULL AND [CancellationReason] IS NOT NULL AND [CancelledFromStatus] IS NOT NULL AND (([CancelledFromStatus]=N'PendingConfirmation' AND [Version]>=1 AND [ConfirmedStartUtc] IS NULL AND [ConfirmedEndUtc] IS NULL AND [ConfirmedEmployeeId] IS NULL AND [ConfirmedAtUtc] IS NULL AND [ConfirmedByAccountId] IS NULL AND [CoordinatedAtUtc] IS NULL AND [CoordinationSource] IS NULL AND [CoordinationReference] IS NULL AND [OfferedAtUtc] IS NULL AND [OfferedByAccountId] IS NULL AND [OfferedEmployeeId] IS NULL) OR ([CancelledFromStatus]=N'AwaitingCustomerAcceptance' AND [Version]>1 AND [ConfirmedStartUtc] IS NULL AND [ConfirmedEndUtc] IS NULL AND [ConfirmedEmployeeId] IS NULL AND [ConfirmedAtUtc] IS NULL AND [ConfirmedByAccountId] IS NULL AND [CoordinatedAtUtc] IS NOT NULL AND [CoordinationSource] IS NOT NULL AND [OfferedAtUtc] IS NOT NULL AND [OfferedByAccountId] IS NOT NULL AND [OfferedEmployeeId] IS NOT NULL) OR ([CancelledFromStatus]=N'Confirmed' AND [Version]>1 AND [ConfirmedStartUtc] IS NOT NULL AND [ConfirmedEndUtc] IS NOT NULL AND [ConfirmedEmployeeId] IS NOT NULL AND [ConfirmedAtUtc] IS NOT NULL AND [ConfirmedByAccountId] IS NOT NULL AND [CoordinatedAtUtc] IS NOT NULL AND [CoordinationSource] IS NOT NULL AND (([OfferedAtUtc] IS NULL AND [OfferedByAccountId] IS NULL AND [OfferedEmployeeId] IS NULL) OR ([OfferedAtUtc] IS NOT NULL AND [OfferedByAccountId] IS NOT NULL AND [OfferedEmployeeId] IS NOT NULL AND [OfferedEmployeeId]=[ConfirmedEmployeeId])))))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ViewingEvents_Shape",
                schema: "Crm",
                table: "ViewingEvents",
                sql: "[AtUtc]>'00010101' AND [ProposedStartUtc]>[AtUtc] AND [ProposedStartUtc]<=DATEADD(day,30,[AtUtc]) AND [ProposedEndUtc]>[ProposedStartUtc] AND (([Kind]=N'Requested' AND [ViewingVersion]=1 AND [ConfirmedStartUtc] IS NULL AND [ConfirmedEndUtc] IS NULL AND [ConfirmedEmployeeId] IS NULL AND [CoordinatedAtUtc] IS NULL AND [CoordinationSource] IS NULL AND [CoordinationReference] IS NULL AND [PreviousProposedStartUtc] IS NULL AND [PreviousProposedEndUtc] IS NULL AND [Reason] IS NULL AND [PreviousStatus] IS NULL AND [PreviousConfirmedStartUtc] IS NULL AND [PreviousConfirmedEndUtc] IS NULL AND [PreviousConfirmedEmployeeId] IS NULL AND [CancellationKind] IS NULL AND [OfferedEmployeeId] IS NULL AND [ProposedEndUtc]=DATEADD(minute,60,[ProposedStartUtc])) OR ([Kind]=N'Confirmed' AND [ViewingVersion]>1 AND [ConfirmedStartUtc] IS NOT NULL AND [ConfirmedEndUtc] IS NOT NULL AND [ConfirmedEmployeeId] IS NOT NULL AND [ConfirmedStartUtc]=[ProposedStartUtc] AND [ConfirmedEndUtc]>[ConfirmedStartUtc] AND [CoordinatedAtUtc] IS NOT NULL AND [CoordinationSource] IS NOT NULL AND [CoordinatedAtUtc]>'00010101' AND [CoordinatedAtUtc]<=[AtUtc] AND LEN(LTRIM(RTRIM([CoordinationSource])))>0 AND ([CoordinationReference] IS NULL OR LEN(LTRIM(RTRIM([CoordinationReference])))>0) AND [PreviousProposedStartUtc] IS NULL AND [PreviousProposedEndUtc] IS NULL AND [Reason] IS NULL AND [PreviousStatus] IS NULL AND [PreviousConfirmedStartUtc] IS NULL AND [PreviousConfirmedEndUtc] IS NULL AND [PreviousConfirmedEmployeeId] IS NULL AND [CancellationKind] IS NULL AND [OfferedEmployeeId] IS NULL AND [ProposedEndUtc]=DATEADD(minute,60,[ProposedStartUtc])) OR ([Kind]=N'AlternativeOffered' AND [ViewingVersion]>1 AND [ConfirmedStartUtc] IS NULL AND [ConfirmedEndUtc] IS NULL AND [ConfirmedEmployeeId] IS NULL AND [CoordinatedAtUtc] IS NOT NULL AND [CoordinationSource] IS NOT NULL AND [CoordinatedAtUtc]>'00010101' AND [CoordinatedAtUtc]<=[AtUtc] AND LEN(LTRIM(RTRIM([CoordinationSource])))>0 AND ([CoordinationReference] IS NULL OR LEN(LTRIM(RTRIM([CoordinationReference])))>0) AND [PreviousStatus] IS NULL AND [PreviousConfirmedStartUtc] IS NULL AND [PreviousConfirmedEndUtc] IS NULL AND [PreviousConfirmedEmployeeId] IS NULL AND [CancellationKind] IS NULL AND [OfferedEmployeeId] IS NOT NULL AND [PreviousProposedStartUtc] IS NOT NULL AND [PreviousProposedEndUtc] IS NOT NULL AND [PreviousProposedStartUtc]>'00010101' AND [PreviousProposedEndUtc]>[PreviousProposedStartUtc] AND [Reason] IS NOT NULL AND LEN(LTRIM(RTRIM([Reason])))>0) OR ([Kind]=N'AlternativeAccepted' AND [ViewingVersion]>=3 AND [ConfirmedStartUtc] IS NOT NULL AND [ConfirmedEndUtc] IS NOT NULL AND [ConfirmedEmployeeId] IS NOT NULL AND [ConfirmedStartUtc]=[ProposedStartUtc] AND [ConfirmedEndUtc]>[ConfirmedStartUtc] AND [CoordinatedAtUtc] IS NOT NULL AND [CoordinationSource] IS NOT NULL AND [CoordinatedAtUtc]>'00010101' AND [CoordinatedAtUtc]<=[AtUtc] AND LEN(LTRIM(RTRIM([CoordinationSource])))>0 AND ([CoordinationReference] IS NULL OR LEN(LTRIM(RTRIM([CoordinationReference])))>0) AND [PreviousProposedStartUtc] IS NULL AND [PreviousProposedEndUtc] IS NULL AND [Reason] IS NULL AND [PreviousStatus] IS NULL AND [PreviousConfirmedStartUtc] IS NULL AND [PreviousConfirmedEndUtc] IS NULL AND [PreviousConfirmedEmployeeId] IS NULL AND [CancellationKind] IS NULL AND [OfferedEmployeeId] IS NOT NULL AND [OfferedEmployeeId]=[ConfirmedEmployeeId] AND [ConfirmedEndUtc]=[ProposedEndUtc]) OR ([Kind]=N'Rescheduled' AND [ViewingVersion]>1 AND [PreviousStatus] IS NOT NULL AND [ConfirmedStartUtc] IS NULL AND [ConfirmedEndUtc] IS NULL AND [ConfirmedEmployeeId] IS NULL AND [CoordinatedAtUtc] IS NULL AND [CoordinationSource] IS NULL AND [CoordinationReference] IS NULL AND [Reason] IS NOT NULL AND LEN(LTRIM(RTRIM([Reason])))>0 AND [OfferedEmployeeId] IS NULL AND [CancellationKind] IS NULL AND [ProposedEndUtc]=DATEADD(minute,60,[ProposedStartUtc]) AND [PreviousProposedStartUtc] IS NOT NULL AND [PreviousProposedEndUtc] IS NOT NULL AND [PreviousProposedStartUtc]>[AtUtc] AND [PreviousProposedEndUtc]>[PreviousProposedStartUtc] AND (([PreviousStatus] IN(N'PendingConfirmation',N'AwaitingCustomerAcceptance') AND [PreviousConfirmedStartUtc] IS NULL AND [PreviousConfirmedEndUtc] IS NULL AND [PreviousConfirmedEmployeeId] IS NULL) OR ([PreviousStatus]=N'Confirmed' AND [PreviousConfirmedStartUtc] IS NOT NULL AND [PreviousConfirmedEndUtc] IS NOT NULL AND [PreviousConfirmedEmployeeId] IS NOT NULL AND [PreviousConfirmedStartUtc]>[AtUtc] AND [PreviousConfirmedEndUtc]>[PreviousConfirmedStartUtc]))) OR ([Kind]=N'Cancelled' AND [ViewingVersion]>1 AND [PreviousStatus] IS NOT NULL AND [Reason] IS NOT NULL AND LEN(LTRIM(RTRIM([Reason])))>0 AND [PreviousProposedStartUtc] IS NULL AND [PreviousProposedEndUtc] IS NULL AND [PreviousConfirmedStartUtc] IS NULL AND [PreviousConfirmedEndUtc] IS NULL AND [PreviousConfirmedEmployeeId] IS NULL AND [CancellationKind] IS NOT NULL AND [CancellationKind] IN(N'Customer',N'Broker',N'Developer') AND (([PreviousStatus]=N'PendingConfirmation' AND [ConfirmedStartUtc] IS NULL AND [ConfirmedEndUtc] IS NULL AND [ConfirmedEmployeeId] IS NULL AND [CoordinatedAtUtc] IS NULL AND [CoordinationSource] IS NULL AND [CoordinationReference] IS NULL AND [OfferedEmployeeId] IS NULL) OR ([PreviousStatus]=N'AwaitingCustomerAcceptance' AND [ConfirmedStartUtc] IS NULL AND [ConfirmedEndUtc] IS NULL AND [ConfirmedEmployeeId] IS NULL AND [CoordinatedAtUtc] IS NOT NULL AND [CoordinationSource] IS NOT NULL AND [CoordinatedAtUtc]>'00010101' AND [CoordinatedAtUtc]<=[AtUtc] AND LEN(LTRIM(RTRIM([CoordinationSource])))>0 AND ([CoordinationReference] IS NULL OR LEN(LTRIM(RTRIM([CoordinationReference])))>0) AND [OfferedEmployeeId] IS NOT NULL) OR ([PreviousStatus]=N'Confirmed' AND [ConfirmedStartUtc] IS NOT NULL AND [ConfirmedEndUtc] IS NOT NULL AND [ConfirmedEmployeeId] IS NOT NULL AND [ConfirmedStartUtc]=[ProposedStartUtc] AND [ConfirmedEndUtc]>[ConfirmedStartUtc] AND [CoordinatedAtUtc] IS NOT NULL AND [CoordinationSource] IS NOT NULL AND [CoordinatedAtUtc]>'00010101' AND [CoordinatedAtUtc]<=[AtUtc] AND LEN(LTRIM(RTRIM([CoordinationSource])))>0 AND ([CoordinationReference] IS NULL OR LEN(LTRIM(RTRIM([CoordinationReference])))>0) AND ([OfferedEmployeeId] IS NULL OR [OfferedEmployeeId]=[ConfirmedEmployeeId])))))");
        }
    }
}
