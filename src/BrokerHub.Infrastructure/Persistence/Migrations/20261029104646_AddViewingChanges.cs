using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrokerHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddViewingChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Viewings_ConfirmationInterval",
                schema: "Crm",
                table: "Viewings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Viewings_Coordination",
                schema: "Crm",
                table: "Viewings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Viewings_Cycle",
                schema: "Crm",
                table: "Viewings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Viewings_InitialState",
                schema: "Crm",
                table: "Viewings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Viewings_Interval",
                schema: "Crm",
                table: "Viewings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ViewingEvents_Shape",
                schema: "Crm",
                table: "ViewingEvents");

            migrationBuilder.AddColumn<string>(
                name: "CancellationKind",
                schema: "Crm",
                table: "Viewings",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                schema: "Crm",
                table: "Viewings",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAtUtc",
                schema: "Crm",
                table: "Viewings",
                type: "datetime2(7)",
                precision: 7,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CancelledByAccountId",
                schema: "Crm",
                table: "Viewings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancelledFromStatus",
                schema: "Crm",
                table: "Viewings",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancellationKind",
                schema: "Crm",
                table: "ViewingEvents",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PreviousConfirmedEmployeeId",
                schema: "Crm",
                table: "ViewingEvents",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PreviousConfirmedEndUtc",
                schema: "Crm",
                table: "ViewingEvents",
                type: "datetime2(7)",
                precision: 7,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PreviousConfirmedStartUtc",
                schema: "Crm",
                table: "ViewingEvents",
                type: "datetime2(7)",
                precision: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreviousStatus",
                schema: "Crm",
                table: "ViewingEvents",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Viewings_CancelledByAccountId",
                schema: "Crm",
                table: "Viewings",
                column: "CancelledByAccountId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Viewings_CancellationAudit",
                schema: "Crm",
                table: "Viewings",
                sql: "[CancelledAtUtc] IS NULL OR ([CancelledAtUtc]>=[PendingCycleStartedAtUtc] AND [CancelledAtUtc]<[CurrentProposedStartUtc] AND LEN(LTRIM(RTRIM([CancellationReason])))>0 AND [CancellationKind] IN(N'Customer',N'Broker',N'Developer') AND ([CancelledFromStatus]<>N'PendingConfirmation' OR [CancelledAtUtc]<DATEADD(day,7,[PendingCycleStartedAtUtc])))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Viewings_ConfirmationInterval",
                schema: "Crm",
                table: "Viewings",
                sql: "[ConfirmedAtUtc] IS NULL OR ([ConfirmedStartUtc]=[CurrentProposedStartUtc] AND [ConfirmedEndUtc]>[ConfirmedStartUtc] AND [ConfirmedAtUtc]>=[PendingCycleStartedAtUtc] AND [ConfirmedAtUtc]<[ConfirmedStartUtc] AND (([OfferedAtUtc] IS NULL AND [ConfirmedAtUtc]<DATEADD(day,7,[PendingCycleStartedAtUtc])) OR ([OfferedAtUtc] IS NOT NULL AND [ConfirmedAtUtc]>=[OfferedAtUtc] AND [ConfirmedEndUtc]=[CurrentProposedEndUtc])))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Viewings_Coordination",
                schema: "Crm",
                table: "Viewings",
                sql: "[CoordinatedAtUtc] IS NULL OR ([CoordinatedAtUtc]>=[PendingCycleStartedAtUtc] AND [CoordinatedAtUtc]<=COALESCE([ConfirmedAtUtc],[OfferedAtUtc]) AND LEN(LTRIM(RTRIM([CoordinationSource])))>0 AND ([CoordinationReference] IS NULL OR LEN(LTRIM(RTRIM([CoordinationReference])))>0))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Viewings_Cycle",
                schema: "Crm",
                table: "Viewings",
                sql: "[PendingCycleStartedAtUtc]>=[RequestedAtUtc]");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Viewings_InitialState",
                schema: "Crm",
                table: "Viewings",
                sql: "([Status]=N'PendingConfirmation' AND [Version]>=1 AND [ConfirmedStartUtc] IS NULL AND [ConfirmedEndUtc] IS NULL AND [ConfirmedEmployeeId] IS NULL AND [ConfirmedAtUtc] IS NULL AND [ConfirmedByAccountId] IS NULL AND [CoordinatedAtUtc] IS NULL AND [CoordinationSource] IS NULL AND [CoordinationReference] IS NULL AND [OfferedAtUtc] IS NULL AND [OfferedByAccountId] IS NULL AND [OfferedEmployeeId] IS NULL AND [CancelledAtUtc] IS NULL AND [CancelledByAccountId] IS NULL AND [CancellationKind] IS NULL AND [CancellationReason] IS NULL AND [CancelledFromStatus] IS NULL) OR ([Status]=N'AwaitingCustomerAcceptance' AND [Version]>1 AND [ConfirmedStartUtc] IS NULL AND [ConfirmedEndUtc] IS NULL AND [ConfirmedEmployeeId] IS NULL AND [ConfirmedAtUtc] IS NULL AND [ConfirmedByAccountId] IS NULL AND [CoordinatedAtUtc] IS NOT NULL AND [CoordinationSource] IS NOT NULL AND [OfferedAtUtc] IS NOT NULL AND [OfferedByAccountId] IS NOT NULL AND [OfferedEmployeeId] IS NOT NULL AND [CancelledAtUtc] IS NULL AND [CancelledByAccountId] IS NULL AND [CancellationKind] IS NULL AND [CancellationReason] IS NULL AND [CancelledFromStatus] IS NULL) OR ([Status]=N'Confirmed' AND [Version]>1 AND [ConfirmedStartUtc] IS NOT NULL AND [ConfirmedEndUtc] IS NOT NULL AND [ConfirmedEmployeeId] IS NOT NULL AND [ConfirmedAtUtc] IS NOT NULL AND [ConfirmedByAccountId] IS NOT NULL AND [CoordinatedAtUtc] IS NOT NULL AND [CoordinationSource] IS NOT NULL AND (([OfferedAtUtc] IS NULL AND [OfferedByAccountId] IS NULL AND [OfferedEmployeeId] IS NULL) OR ([OfferedAtUtc] IS NOT NULL AND [OfferedByAccountId] IS NOT NULL AND [OfferedEmployeeId] IS NOT NULL AND [OfferedEmployeeId]=[ConfirmedEmployeeId])) AND [CancelledAtUtc] IS NULL AND [CancelledByAccountId] IS NULL AND [CancellationKind] IS NULL AND [CancellationReason] IS NULL AND [CancelledFromStatus] IS NULL) OR ([Status]=N'Cancelled' AND [Version]>1 AND [CancelledAtUtc] IS NOT NULL AND [CancelledByAccountId] IS NOT NULL AND [CancellationKind] IS NOT NULL AND [CancellationReason] IS NOT NULL AND [CancelledFromStatus] IS NOT NULL AND (([CancelledFromStatus]=N'PendingConfirmation' AND [Version]>=1 AND [ConfirmedStartUtc] IS NULL AND [ConfirmedEndUtc] IS NULL AND [ConfirmedEmployeeId] IS NULL AND [ConfirmedAtUtc] IS NULL AND [ConfirmedByAccountId] IS NULL AND [CoordinatedAtUtc] IS NULL AND [CoordinationSource] IS NULL AND [CoordinationReference] IS NULL AND [OfferedAtUtc] IS NULL AND [OfferedByAccountId] IS NULL AND [OfferedEmployeeId] IS NULL) OR ([CancelledFromStatus]=N'AwaitingCustomerAcceptance' AND [Version]>1 AND [ConfirmedStartUtc] IS NULL AND [ConfirmedEndUtc] IS NULL AND [ConfirmedEmployeeId] IS NULL AND [ConfirmedAtUtc] IS NULL AND [ConfirmedByAccountId] IS NULL AND [CoordinatedAtUtc] IS NOT NULL AND [CoordinationSource] IS NOT NULL AND [OfferedAtUtc] IS NOT NULL AND [OfferedByAccountId] IS NOT NULL AND [OfferedEmployeeId] IS NOT NULL) OR ([CancelledFromStatus]=N'Confirmed' AND [Version]>1 AND [ConfirmedStartUtc] IS NOT NULL AND [ConfirmedEndUtc] IS NOT NULL AND [ConfirmedEmployeeId] IS NOT NULL AND [ConfirmedAtUtc] IS NOT NULL AND [ConfirmedByAccountId] IS NOT NULL AND [CoordinatedAtUtc] IS NOT NULL AND [CoordinationSource] IS NOT NULL AND (([OfferedAtUtc] IS NULL AND [OfferedByAccountId] IS NULL AND [OfferedEmployeeId] IS NULL) OR ([OfferedAtUtc] IS NOT NULL AND [OfferedByAccountId] IS NOT NULL AND [OfferedEmployeeId] IS NOT NULL AND [OfferedEmployeeId]=[ConfirmedEmployeeId])))))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Viewings_Interval",
                schema: "Crm",
                table: "Viewings",
                sql: "[RequestedAtUtc]>'00010101' AND [CurrentProposedStartUtc]>[PendingCycleStartedAtUtc] AND [CurrentProposedEndUtc]>[CurrentProposedStartUtc] AND (([OfferedAtUtc] IS NULL AND [CurrentProposedStartUtc]<=DATEADD(day,30,[PendingCycleStartedAtUtc]) AND [CurrentProposedEndUtc]=DATEADD(minute,60,[CurrentProposedStartUtc])) OR ([OfferedAtUtc] IS NOT NULL AND [CurrentProposedStartUtc]<=DATEADD(day,30,[OfferedAtUtc])))");

            migrationBuilder.CreateIndex(
                name: "IX_ViewingEvents_PreviousConfirmedEmployeeId",
                schema: "Crm",
                table: "ViewingEvents",
                column: "PreviousConfirmedEmployeeId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ViewingEvents_Shape",
                schema: "Crm",
                table: "ViewingEvents",
                sql: "[AtUtc]>'00010101' AND [ProposedStartUtc]>[AtUtc] AND [ProposedStartUtc]<=DATEADD(day,30,[AtUtc]) AND [ProposedEndUtc]>[ProposedStartUtc] AND (([Kind]=N'Requested' AND [ViewingVersion]=1 AND [ConfirmedStartUtc] IS NULL AND [ConfirmedEndUtc] IS NULL AND [ConfirmedEmployeeId] IS NULL AND [CoordinatedAtUtc] IS NULL AND [CoordinationSource] IS NULL AND [CoordinationReference] IS NULL AND [PreviousProposedStartUtc] IS NULL AND [PreviousProposedEndUtc] IS NULL AND [Reason] IS NULL AND [PreviousStatus] IS NULL AND [PreviousConfirmedStartUtc] IS NULL AND [PreviousConfirmedEndUtc] IS NULL AND [PreviousConfirmedEmployeeId] IS NULL AND [CancellationKind] IS NULL AND [OfferedEmployeeId] IS NULL AND [ProposedEndUtc]=DATEADD(minute,60,[ProposedStartUtc])) OR ([Kind]=N'Confirmed' AND [ViewingVersion]>1 AND [ConfirmedStartUtc] IS NOT NULL AND [ConfirmedEndUtc] IS NOT NULL AND [ConfirmedEmployeeId] IS NOT NULL AND [ConfirmedStartUtc]=[ProposedStartUtc] AND [ConfirmedEndUtc]>[ConfirmedStartUtc] AND [CoordinatedAtUtc] IS NOT NULL AND [CoordinationSource] IS NOT NULL AND [CoordinatedAtUtc]>'00010101' AND [CoordinatedAtUtc]<=[AtUtc] AND LEN(LTRIM(RTRIM([CoordinationSource])))>0 AND ([CoordinationReference] IS NULL OR LEN(LTRIM(RTRIM([CoordinationReference])))>0) AND [PreviousProposedStartUtc] IS NULL AND [PreviousProposedEndUtc] IS NULL AND [Reason] IS NULL AND [PreviousStatus] IS NULL AND [PreviousConfirmedStartUtc] IS NULL AND [PreviousConfirmedEndUtc] IS NULL AND [PreviousConfirmedEmployeeId] IS NULL AND [CancellationKind] IS NULL AND [OfferedEmployeeId] IS NULL AND [ProposedEndUtc]=DATEADD(minute,60,[ProposedStartUtc])) OR ([Kind]=N'AlternativeOffered' AND [ViewingVersion]>1 AND [ConfirmedStartUtc] IS NULL AND [ConfirmedEndUtc] IS NULL AND [ConfirmedEmployeeId] IS NULL AND [CoordinatedAtUtc] IS NOT NULL AND [CoordinationSource] IS NOT NULL AND [CoordinatedAtUtc]>'00010101' AND [CoordinatedAtUtc]<=[AtUtc] AND LEN(LTRIM(RTRIM([CoordinationSource])))>0 AND ([CoordinationReference] IS NULL OR LEN(LTRIM(RTRIM([CoordinationReference])))>0) AND [PreviousStatus] IS NULL AND [PreviousConfirmedStartUtc] IS NULL AND [PreviousConfirmedEndUtc] IS NULL AND [PreviousConfirmedEmployeeId] IS NULL AND [CancellationKind] IS NULL AND [OfferedEmployeeId] IS NOT NULL AND [PreviousProposedStartUtc] IS NOT NULL AND [PreviousProposedEndUtc] IS NOT NULL AND [PreviousProposedStartUtc]>'00010101' AND [PreviousProposedEndUtc]>[PreviousProposedStartUtc] AND [Reason] IS NOT NULL AND LEN(LTRIM(RTRIM([Reason])))>0) OR ([Kind]=N'AlternativeAccepted' AND [ViewingVersion]>=3 AND [ConfirmedStartUtc] IS NOT NULL AND [ConfirmedEndUtc] IS NOT NULL AND [ConfirmedEmployeeId] IS NOT NULL AND [ConfirmedStartUtc]=[ProposedStartUtc] AND [ConfirmedEndUtc]>[ConfirmedStartUtc] AND [CoordinatedAtUtc] IS NOT NULL AND [CoordinationSource] IS NOT NULL AND [CoordinatedAtUtc]>'00010101' AND [CoordinatedAtUtc]<=[AtUtc] AND LEN(LTRIM(RTRIM([CoordinationSource])))>0 AND ([CoordinationReference] IS NULL OR LEN(LTRIM(RTRIM([CoordinationReference])))>0) AND [PreviousProposedStartUtc] IS NULL AND [PreviousProposedEndUtc] IS NULL AND [Reason] IS NULL AND [PreviousStatus] IS NULL AND [PreviousConfirmedStartUtc] IS NULL AND [PreviousConfirmedEndUtc] IS NULL AND [PreviousConfirmedEmployeeId] IS NULL AND [CancellationKind] IS NULL AND [OfferedEmployeeId] IS NOT NULL AND [OfferedEmployeeId]=[ConfirmedEmployeeId] AND [ConfirmedEndUtc]=[ProposedEndUtc]) OR ([Kind]=N'Rescheduled' AND [ViewingVersion]>1 AND [PreviousStatus] IS NOT NULL AND [ConfirmedStartUtc] IS NULL AND [ConfirmedEndUtc] IS NULL AND [ConfirmedEmployeeId] IS NULL AND [CoordinatedAtUtc] IS NULL AND [CoordinationSource] IS NULL AND [CoordinationReference] IS NULL AND [Reason] IS NOT NULL AND LEN(LTRIM(RTRIM([Reason])))>0 AND [OfferedEmployeeId] IS NULL AND [CancellationKind] IS NULL AND [ProposedEndUtc]=DATEADD(minute,60,[ProposedStartUtc]) AND [PreviousProposedStartUtc] IS NOT NULL AND [PreviousProposedEndUtc] IS NOT NULL AND [PreviousProposedStartUtc]>[AtUtc] AND [PreviousProposedEndUtc]>[PreviousProposedStartUtc] AND (([PreviousStatus] IN(N'PendingConfirmation',N'AwaitingCustomerAcceptance') AND [PreviousConfirmedStartUtc] IS NULL AND [PreviousConfirmedEndUtc] IS NULL AND [PreviousConfirmedEmployeeId] IS NULL) OR ([PreviousStatus]=N'Confirmed' AND [PreviousConfirmedStartUtc] IS NOT NULL AND [PreviousConfirmedEndUtc] IS NOT NULL AND [PreviousConfirmedEmployeeId] IS NOT NULL AND [PreviousConfirmedStartUtc]>[AtUtc] AND [PreviousConfirmedEndUtc]>[PreviousConfirmedStartUtc]))) OR ([Kind]=N'Cancelled' AND [ViewingVersion]>1 AND [PreviousStatus] IS NOT NULL AND [Reason] IS NOT NULL AND LEN(LTRIM(RTRIM([Reason])))>0 AND [PreviousProposedStartUtc] IS NULL AND [PreviousProposedEndUtc] IS NULL AND [PreviousConfirmedStartUtc] IS NULL AND [PreviousConfirmedEndUtc] IS NULL AND [PreviousConfirmedEmployeeId] IS NULL AND [CancellationKind] IS NOT NULL AND [CancellationKind] IN(N'Customer',N'Broker',N'Developer') AND (([PreviousStatus]=N'PendingConfirmation' AND [ConfirmedStartUtc] IS NULL AND [ConfirmedEndUtc] IS NULL AND [ConfirmedEmployeeId] IS NULL AND [CoordinatedAtUtc] IS NULL AND [CoordinationSource] IS NULL AND [CoordinationReference] IS NULL AND [OfferedEmployeeId] IS NULL) OR ([PreviousStatus]=N'AwaitingCustomerAcceptance' AND [ConfirmedStartUtc] IS NULL AND [ConfirmedEndUtc] IS NULL AND [ConfirmedEmployeeId] IS NULL AND [CoordinatedAtUtc] IS NOT NULL AND [CoordinationSource] IS NOT NULL AND [CoordinatedAtUtc]>'00010101' AND [CoordinatedAtUtc]<=[AtUtc] AND LEN(LTRIM(RTRIM([CoordinationSource])))>0 AND ([CoordinationReference] IS NULL OR LEN(LTRIM(RTRIM([CoordinationReference])))>0) AND [OfferedEmployeeId] IS NOT NULL) OR ([PreviousStatus]=N'Confirmed' AND [ConfirmedStartUtc] IS NOT NULL AND [ConfirmedEndUtc] IS NOT NULL AND [ConfirmedEmployeeId] IS NOT NULL AND [ConfirmedStartUtc]=[ProposedStartUtc] AND [ConfirmedEndUtc]>[ConfirmedStartUtc] AND [CoordinatedAtUtc] IS NOT NULL AND [CoordinationSource] IS NOT NULL AND [CoordinatedAtUtc]>'00010101' AND [CoordinatedAtUtc]<=[AtUtc] AND LEN(LTRIM(RTRIM([CoordinationSource])))>0 AND ([CoordinationReference] IS NULL OR LEN(LTRIM(RTRIM([CoordinationReference])))>0) AND ([OfferedEmployeeId] IS NULL OR [OfferedEmployeeId]=[ConfirmedEmployeeId])))))");

            migrationBuilder.AddForeignKey(
                name: "FK_ViewingEvents_Employees_PreviousConfirmedEmployeeId",
                schema: "Crm",
                table: "ViewingEvents",
                column: "PreviousConfirmedEmployeeId",
                principalSchema: "Crm",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Viewings_Accounts_CancelledByAccountId",
                schema: "Crm",
                table: "Viewings",
                column: "CancelledByAccountId",
                principalSchema: "Identity",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ViewingEvents_Employees_PreviousConfirmedEmployeeId",
                schema: "Crm",
                table: "ViewingEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_Viewings_Accounts_CancelledByAccountId",
                schema: "Crm",
                table: "Viewings");

            migrationBuilder.DropIndex(
                name: "IX_Viewings_CancelledByAccountId",
                schema: "Crm",
                table: "Viewings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Viewings_CancellationAudit",
                schema: "Crm",
                table: "Viewings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Viewings_ConfirmationInterval",
                schema: "Crm",
                table: "Viewings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Viewings_Coordination",
                schema: "Crm",
                table: "Viewings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Viewings_Cycle",
                schema: "Crm",
                table: "Viewings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Viewings_InitialState",
                schema: "Crm",
                table: "Viewings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Viewings_Interval",
                schema: "Crm",
                table: "Viewings");

            migrationBuilder.DropIndex(
                name: "IX_ViewingEvents_PreviousConfirmedEmployeeId",
                schema: "Crm",
                table: "ViewingEvents");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ViewingEvents_Shape",
                schema: "Crm",
                table: "ViewingEvents");

            migrationBuilder.DropColumn(
                name: "CancellationKind",
                schema: "Crm",
                table: "Viewings");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                schema: "Crm",
                table: "Viewings");

            migrationBuilder.DropColumn(
                name: "CancelledAtUtc",
                schema: "Crm",
                table: "Viewings");

            migrationBuilder.DropColumn(
                name: "CancelledByAccountId",
                schema: "Crm",
                table: "Viewings");

            migrationBuilder.DropColumn(
                name: "CancelledFromStatus",
                schema: "Crm",
                table: "Viewings");

            migrationBuilder.DropColumn(
                name: "CancellationKind",
                schema: "Crm",
                table: "ViewingEvents");

            migrationBuilder.DropColumn(
                name: "PreviousConfirmedEmployeeId",
                schema: "Crm",
                table: "ViewingEvents");

            migrationBuilder.DropColumn(
                name: "PreviousConfirmedEndUtc",
                schema: "Crm",
                table: "ViewingEvents");

            migrationBuilder.DropColumn(
                name: "PreviousConfirmedStartUtc",
                schema: "Crm",
                table: "ViewingEvents");

            migrationBuilder.DropColumn(
                name: "PreviousStatus",
                schema: "Crm",
                table: "ViewingEvents");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Viewings_ConfirmationInterval",
                schema: "Crm",
                table: "Viewings",
                sql: "[Status]<>N'Confirmed' OR ([ConfirmedStartUtc]=[CurrentProposedStartUtc] AND [ConfirmedEndUtc]>[ConfirmedStartUtc] AND [ConfirmedAtUtc]>=[RequestedAtUtc] AND [ConfirmedAtUtc]<[ConfirmedStartUtc] AND (([OfferedAtUtc] IS NULL AND [ConfirmedAtUtc]<DATEADD(day,7,[PendingCycleStartedAtUtc])) OR ([OfferedAtUtc] IS NOT NULL AND [ConfirmedAtUtc]>=[OfferedAtUtc] AND [ConfirmedEndUtc]=[CurrentProposedEndUtc])))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Viewings_Coordination",
                schema: "Crm",
                table: "Viewings",
                sql: "[Status]=N'PendingConfirmation' OR ([CoordinatedAtUtc]>=[PendingCycleStartedAtUtc] AND [CoordinatedAtUtc]<=CASE WHEN [Status]=N'Confirmed' THEN [ConfirmedAtUtc] ELSE [OfferedAtUtc] END AND LEN(LTRIM(RTRIM([CoordinationSource])))>0 AND ([CoordinationReference] IS NULL OR LEN(LTRIM(RTRIM([CoordinationReference])))>0))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Viewings_Cycle",
                schema: "Crm",
                table: "Viewings",
                sql: "[PendingCycleStartedAtUtc]=[RequestedAtUtc]");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Viewings_InitialState",
                schema: "Crm",
                table: "Viewings",
                sql: "([Status]=N'PendingConfirmation' AND [Version]=1 AND [ConfirmedStartUtc] IS NULL AND [ConfirmedEndUtc] IS NULL AND [ConfirmedEmployeeId] IS NULL AND [ConfirmedAtUtc] IS NULL AND [ConfirmedByAccountId] IS NULL AND [CoordinatedAtUtc] IS NULL AND [CoordinationSource] IS NULL AND [CoordinationReference] IS NULL AND [OfferedAtUtc] IS NULL AND [OfferedByAccountId] IS NULL AND [OfferedEmployeeId] IS NULL) OR ([Status]=N'AwaitingCustomerAcceptance' AND [Version]>1 AND [ConfirmedStartUtc] IS NULL AND [ConfirmedEndUtc] IS NULL AND [ConfirmedEmployeeId] IS NULL AND [ConfirmedAtUtc] IS NULL AND [ConfirmedByAccountId] IS NULL AND [CoordinatedAtUtc] IS NOT NULL AND [CoordinationSource] IS NOT NULL AND [OfferedAtUtc] IS NOT NULL AND [OfferedByAccountId] IS NOT NULL AND [OfferedEmployeeId] IS NOT NULL) OR ([Status]=N'Confirmed' AND [Version]>1 AND [ConfirmedStartUtc] IS NOT NULL AND [ConfirmedEndUtc] IS NOT NULL AND [ConfirmedEmployeeId] IS NOT NULL AND [ConfirmedAtUtc] IS NOT NULL AND [ConfirmedByAccountId] IS NOT NULL AND [CoordinatedAtUtc] IS NOT NULL AND [CoordinationSource] IS NOT NULL AND (([OfferedAtUtc] IS NULL AND [OfferedByAccountId] IS NULL AND [OfferedEmployeeId] IS NULL) OR ([OfferedAtUtc] IS NOT NULL AND [OfferedByAccountId] IS NOT NULL AND [OfferedEmployeeId] IS NOT NULL AND [OfferedEmployeeId]=[ConfirmedEmployeeId])))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Viewings_Interval",
                schema: "Crm",
                table: "Viewings",
                sql: "[RequestedAtUtc]>'00010101' AND [CurrentProposedStartUtc]>[RequestedAtUtc] AND [CurrentProposedEndUtc]>[CurrentProposedStartUtc] AND (([OfferedAtUtc] IS NULL AND [CurrentProposedStartUtc]<=DATEADD(day,30,[RequestedAtUtc]) AND [CurrentProposedEndUtc]=DATEADD(minute,60,[CurrentProposedStartUtc])) OR ([OfferedAtUtc] IS NOT NULL AND [CurrentProposedStartUtc]<=DATEADD(day,30,[OfferedAtUtc])))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ViewingEvents_Shape",
                schema: "Crm",
                table: "ViewingEvents",
                sql: "[AtUtc]>'00010101' AND [ProposedStartUtc]>[AtUtc] AND [ProposedStartUtc]<=DATEADD(day,30,[AtUtc]) AND [ProposedEndUtc]>[ProposedStartUtc] AND (([Kind]=N'Requested' AND [ViewingVersion]=1 AND [ConfirmedStartUtc] IS NULL AND [ConfirmedEndUtc] IS NULL AND [ConfirmedEmployeeId] IS NULL AND [CoordinatedAtUtc] IS NULL AND [CoordinationSource] IS NULL AND [CoordinationReference] IS NULL AND [PreviousProposedStartUtc] IS NULL AND [PreviousProposedEndUtc] IS NULL AND [Reason] IS NULL AND [OfferedEmployeeId] IS NULL AND [ProposedEndUtc]=DATEADD(minute,60,[ProposedStartUtc])) OR ([Kind]=N'Confirmed' AND [ViewingVersion]>1 AND [ConfirmedStartUtc] IS NOT NULL AND [ConfirmedEndUtc] IS NOT NULL AND [ConfirmedEmployeeId] IS NOT NULL AND [ConfirmedStartUtc]=[ProposedStartUtc] AND [ConfirmedEndUtc]>[ConfirmedStartUtc] AND [CoordinatedAtUtc] IS NOT NULL AND [CoordinationSource] IS NOT NULL AND [CoordinatedAtUtc]>'00010101' AND [CoordinatedAtUtc]<=[AtUtc] AND LEN(LTRIM(RTRIM([CoordinationSource])))>0 AND ([CoordinationReference] IS NULL OR LEN(LTRIM(RTRIM([CoordinationReference])))>0) AND [PreviousProposedStartUtc] IS NULL AND [PreviousProposedEndUtc] IS NULL AND [Reason] IS NULL AND [OfferedEmployeeId] IS NULL AND [ProposedEndUtc]=DATEADD(minute,60,[ProposedStartUtc])) OR ([Kind]=N'AlternativeOffered' AND [ViewingVersion]>1 AND [ConfirmedStartUtc] IS NULL AND [ConfirmedEndUtc] IS NULL AND [ConfirmedEmployeeId] IS NULL AND [CoordinatedAtUtc] IS NOT NULL AND [CoordinationSource] IS NOT NULL AND [CoordinatedAtUtc]>'00010101' AND [CoordinatedAtUtc]<=[AtUtc] AND LEN(LTRIM(RTRIM([CoordinationSource])))>0 AND ([CoordinationReference] IS NULL OR LEN(LTRIM(RTRIM([CoordinationReference])))>0) AND [OfferedEmployeeId] IS NOT NULL AND [PreviousProposedStartUtc] IS NOT NULL AND [PreviousProposedEndUtc] IS NOT NULL AND [PreviousProposedStartUtc]>'00010101' AND [PreviousProposedEndUtc]>[PreviousProposedStartUtc] AND [Reason] IS NOT NULL AND LEN(LTRIM(RTRIM([Reason])))>0) OR ([Kind]=N'AlternativeAccepted' AND [ViewingVersion]>=3 AND [ConfirmedStartUtc] IS NOT NULL AND [ConfirmedEndUtc] IS NOT NULL AND [ConfirmedEmployeeId] IS NOT NULL AND [ConfirmedStartUtc]=[ProposedStartUtc] AND [ConfirmedEndUtc]>[ConfirmedStartUtc] AND [CoordinatedAtUtc] IS NOT NULL AND [CoordinationSource] IS NOT NULL AND [CoordinatedAtUtc]>'00010101' AND [CoordinatedAtUtc]<=[AtUtc] AND LEN(LTRIM(RTRIM([CoordinationSource])))>0 AND ([CoordinationReference] IS NULL OR LEN(LTRIM(RTRIM([CoordinationReference])))>0) AND [PreviousProposedStartUtc] IS NULL AND [PreviousProposedEndUtc] IS NULL AND [Reason] IS NULL AND [OfferedEmployeeId] IS NOT NULL AND [OfferedEmployeeId]=[ConfirmedEmployeeId] AND [ConfirmedEndUtc]=[ProposedEndUtc]))");
        }
    }
}
