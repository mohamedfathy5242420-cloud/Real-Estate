using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrokerHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddViewingOutcomes : Migration
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

            migrationBuilder.AddColumn<Guid>(
                name: "OutcomeNoteId",
                schema: "Crm",
                table: "Viewings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OutcomeOccurredAtUtc",
                schema: "Crm",
                table: "Viewings",
                type: "datetime2(7)",
                precision: 7,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OutcomeRecordedAtUtc",
                schema: "Crm",
                table: "Viewings",
                type: "datetime2(7)",
                precision: 7,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OutcomeRecordedByAccountId",
                schema: "Crm",
                table: "Viewings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OutcomeNoteId",
                schema: "Crm",
                table: "ViewingEvents",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OutcomeOccurredAtUtc",
                schema: "Crm",
                table: "ViewingEvents",
                type: "datetime2(7)",
                precision: 7,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Viewings_OutcomeNoteId_CustomerId",
                schema: "Crm",
                table: "Viewings",
                columns: new[] { "OutcomeNoteId", "CustomerId" });

            migrationBuilder.CreateIndex(
                name: "IX_Viewings_OutcomeRecordedByAccountId",
                schema: "Crm",
                table: "Viewings",
                column: "OutcomeRecordedByAccountId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Viewings_InitialState",
                schema: "Crm",
                table: "Viewings",
                sql: "([Status]=N'PendingConfirmation' AND [Version]>=1 AND [ConfirmedStartUtc] IS NULL AND [ConfirmedEndUtc] IS NULL AND [ConfirmedEmployeeId] IS NULL AND [ConfirmedAtUtc] IS NULL AND [ConfirmedByAccountId] IS NULL AND [CoordinatedAtUtc] IS NULL AND [CoordinationSource] IS NULL AND [CoordinationReference] IS NULL AND [OfferedAtUtc] IS NULL AND [OfferedByAccountId] IS NULL AND [OfferedEmployeeId] IS NULL AND [CancelledAtUtc] IS NULL AND [CancelledByAccountId] IS NULL AND [CancellationKind] IS NULL AND [CancellationReason] IS NULL AND [CancelledFromStatus] IS NULL AND [ExpiredAtUtc] IS NULL AND [ExpiredFromStatus] IS NULL AND [OutcomeOccurredAtUtc] IS NULL AND [OutcomeRecordedAtUtc] IS NULL AND [OutcomeRecordedByAccountId] IS NULL AND [OutcomeNoteId] IS NULL) OR ([Status]=N'AwaitingCustomerAcceptance' AND [Version]>1 AND [ConfirmedStartUtc] IS NULL AND [ConfirmedEndUtc] IS NULL AND [ConfirmedEmployeeId] IS NULL AND [ConfirmedAtUtc] IS NULL AND [ConfirmedByAccountId] IS NULL AND [CoordinatedAtUtc] IS NOT NULL AND [CoordinationSource] IS NOT NULL AND [OfferedAtUtc] IS NOT NULL AND [OfferedByAccountId] IS NOT NULL AND [OfferedEmployeeId] IS NOT NULL AND [CancelledAtUtc] IS NULL AND [CancelledByAccountId] IS NULL AND [CancellationKind] IS NULL AND [CancellationReason] IS NULL AND [CancelledFromStatus] IS NULL AND [ExpiredAtUtc] IS NULL AND [ExpiredFromStatus] IS NULL AND [OutcomeOccurredAtUtc] IS NULL AND [OutcomeRecordedAtUtc] IS NULL AND [OutcomeRecordedByAccountId] IS NULL AND [OutcomeNoteId] IS NULL) OR ([Status]=N'Confirmed' AND [Version]>1 AND [ConfirmedStartUtc] IS NOT NULL AND [ConfirmedEndUtc] IS NOT NULL AND [ConfirmedEmployeeId] IS NOT NULL AND [ConfirmedAtUtc] IS NOT NULL AND [ConfirmedByAccountId] IS NOT NULL AND [CoordinatedAtUtc] IS NOT NULL AND [CoordinationSource] IS NOT NULL AND (([OfferedAtUtc] IS NULL AND [OfferedByAccountId] IS NULL AND [OfferedEmployeeId] IS NULL) OR ([OfferedAtUtc] IS NOT NULL AND [OfferedByAccountId] IS NOT NULL AND [OfferedEmployeeId] IS NOT NULL AND [OfferedEmployeeId]=[ConfirmedEmployeeId])) AND [CancelledAtUtc] IS NULL AND [CancelledByAccountId] IS NULL AND [CancellationKind] IS NULL AND [CancellationReason] IS NULL AND [CancelledFromStatus] IS NULL AND [ExpiredAtUtc] IS NULL AND [ExpiredFromStatus] IS NULL AND [OutcomeOccurredAtUtc] IS NULL AND [OutcomeRecordedAtUtc] IS NULL AND [OutcomeRecordedByAccountId] IS NULL AND [OutcomeNoteId] IS NULL) OR ([Status]=N'Cancelled' AND [Version]>1 AND [CancelledAtUtc] IS NOT NULL AND [CancelledByAccountId] IS NOT NULL AND [CancellationKind] IS NOT NULL AND [CancellationReason] IS NOT NULL AND [CancelledFromStatus] IS NOT NULL AND [ExpiredAtUtc] IS NULL AND [ExpiredFromStatus] IS NULL AND [OutcomeOccurredAtUtc] IS NULL AND [OutcomeRecordedAtUtc] IS NULL AND [OutcomeRecordedByAccountId] IS NULL AND [OutcomeNoteId] IS NULL AND (([CancelledFromStatus]=N'PendingConfirmation' AND [Version]>=1 AND [ConfirmedStartUtc] IS NULL AND [ConfirmedEndUtc] IS NULL AND [ConfirmedEmployeeId] IS NULL AND [ConfirmedAtUtc] IS NULL AND [ConfirmedByAccountId] IS NULL AND [CoordinatedAtUtc] IS NULL AND [CoordinationSource] IS NULL AND [CoordinationReference] IS NULL AND [OfferedAtUtc] IS NULL AND [OfferedByAccountId] IS NULL AND [OfferedEmployeeId] IS NULL) OR ([CancelledFromStatus]=N'AwaitingCustomerAcceptance' AND [Version]>1 AND [ConfirmedStartUtc] IS NULL AND [ConfirmedEndUtc] IS NULL AND [ConfirmedEmployeeId] IS NULL AND [ConfirmedAtUtc] IS NULL AND [ConfirmedByAccountId] IS NULL AND [CoordinatedAtUtc] IS NOT NULL AND [CoordinationSource] IS NOT NULL AND [OfferedAtUtc] IS NOT NULL AND [OfferedByAccountId] IS NOT NULL AND [OfferedEmployeeId] IS NOT NULL) OR ([CancelledFromStatus]=N'Confirmed' AND [Version]>1 AND [ConfirmedStartUtc] IS NOT NULL AND [ConfirmedEndUtc] IS NOT NULL AND [ConfirmedEmployeeId] IS NOT NULL AND [ConfirmedAtUtc] IS NOT NULL AND [ConfirmedByAccountId] IS NOT NULL AND [CoordinatedAtUtc] IS NOT NULL AND [CoordinationSource] IS NOT NULL AND (([OfferedAtUtc] IS NULL AND [OfferedByAccountId] IS NULL AND [OfferedEmployeeId] IS NULL) OR ([OfferedAtUtc] IS NOT NULL AND [OfferedByAccountId] IS NOT NULL AND [OfferedEmployeeId] IS NOT NULL AND [OfferedEmployeeId]=[ConfirmedEmployeeId]))))) OR ([Status]=N'Expired' AND [Version]>1 AND [ExpiredAtUtc] IS NOT NULL AND [ExpiredFromStatus] IS NOT NULL AND [CancelledAtUtc] IS NULL AND [CancelledByAccountId] IS NULL AND [CancellationKind] IS NULL AND [CancellationReason] IS NULL AND [CancelledFromStatus] IS NULL AND [OutcomeOccurredAtUtc] IS NULL AND [OutcomeRecordedAtUtc] IS NULL AND [OutcomeRecordedByAccountId] IS NULL AND [OutcomeNoteId] IS NULL AND (([ExpiredFromStatus]=N'PendingConfirmation' AND [Version]>=1 AND [ConfirmedStartUtc] IS NULL AND [ConfirmedEndUtc] IS NULL AND [ConfirmedEmployeeId] IS NULL AND [ConfirmedAtUtc] IS NULL AND [ConfirmedByAccountId] IS NULL AND [CoordinatedAtUtc] IS NULL AND [CoordinationSource] IS NULL AND [CoordinationReference] IS NULL AND [OfferedAtUtc] IS NULL AND [OfferedByAccountId] IS NULL AND [OfferedEmployeeId] IS NULL) OR ([ExpiredFromStatus]=N'AwaitingCustomerAcceptance' AND [Version]>1 AND [ConfirmedStartUtc] IS NULL AND [ConfirmedEndUtc] IS NULL AND [ConfirmedEmployeeId] IS NULL AND [ConfirmedAtUtc] IS NULL AND [ConfirmedByAccountId] IS NULL AND [CoordinatedAtUtc] IS NOT NULL AND [CoordinationSource] IS NOT NULL AND [OfferedAtUtc] IS NOT NULL AND [OfferedByAccountId] IS NOT NULL AND [OfferedEmployeeId] IS NOT NULL))) OR ([Status] IN(N'Completed',N'CustomerNoShow') AND [Version]>2 AND [Version]>1 AND [ConfirmedStartUtc] IS NOT NULL AND [ConfirmedEndUtc] IS NOT NULL AND [ConfirmedEmployeeId] IS NOT NULL AND [ConfirmedAtUtc] IS NOT NULL AND [ConfirmedByAccountId] IS NOT NULL AND [CoordinatedAtUtc] IS NOT NULL AND [CoordinationSource] IS NOT NULL AND (([OfferedAtUtc] IS NULL AND [OfferedByAccountId] IS NULL AND [OfferedEmployeeId] IS NULL) OR ([OfferedAtUtc] IS NOT NULL AND [OfferedByAccountId] IS NOT NULL AND [OfferedEmployeeId] IS NOT NULL AND [OfferedEmployeeId]=[ConfirmedEmployeeId])) AND [CancelledAtUtc] IS NULL AND [CancelledByAccountId] IS NULL AND [CancellationKind] IS NULL AND [CancellationReason] IS NULL AND [CancelledFromStatus] IS NULL AND [ExpiredAtUtc] IS NULL AND [ExpiredFromStatus] IS NULL AND [OutcomeOccurredAtUtc] IS NOT NULL AND [OutcomeRecordedAtUtc] IS NOT NULL AND [OutcomeRecordedByAccountId] IS NOT NULL AND [OutcomeNoteId] IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Viewings_OutcomeAudit",
                schema: "Crm",
                table: "Viewings",
                sql: "[OutcomeOccurredAtUtc] IS NULL OR ([ConfirmedStartUtc] IS NOT NULL AND [OutcomeRecordedAtUtc] IS NOT NULL AND [OutcomeOccurredAtUtc]>=[ConfirmedStartUtc] AND [OutcomeOccurredAtUtc]<=[OutcomeRecordedAtUtc])");

            migrationBuilder.CreateIndex(
                name: "IX_ViewingEvents_OutcomeNoteId_CustomerId",
                schema: "Crm",
                table: "ViewingEvents",
                columns: new[] { "OutcomeNoteId", "CustomerId" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_ViewingEvents_Shape",
                schema: "Crm",
                table: "ViewingEvents",
                sql: "[AtUtc]>'00010101' AND [ProposedStartUtc]>'00010101' AND [ProposedEndUtc]>[ProposedStartUtc] AND ((([ActorKind]=N'User' AND [ActorAccountId] IS NOT NULL AND [EffectiveCutoffUtc] IS NULL) AND [OutcomeOccurredAtUtc] IS NULL AND [OutcomeNoteId] IS NULL AND [ProposedStartUtc]>[AtUtc] AND [ProposedStartUtc]<=DATEADD(day,30,[AtUtc]) AND (([Kind]=N'Requested' AND [ViewingVersion]=1 AND [ConfirmedStartUtc] IS NULL AND [ConfirmedEndUtc] IS NULL AND [ConfirmedEmployeeId] IS NULL AND [CoordinatedAtUtc] IS NULL AND [CoordinationSource] IS NULL AND [CoordinationReference] IS NULL AND [PreviousProposedStartUtc] IS NULL AND [PreviousProposedEndUtc] IS NULL AND [Reason] IS NULL AND [PreviousStatus] IS NULL AND [PreviousConfirmedStartUtc] IS NULL AND [PreviousConfirmedEndUtc] IS NULL AND [PreviousConfirmedEmployeeId] IS NULL AND [CancellationKind] IS NULL AND [OfferedEmployeeId] IS NULL AND [ProposedEndUtc]=DATEADD(minute,60,[ProposedStartUtc])) OR ([Kind]=N'Confirmed' AND [ViewingVersion]>1 AND [ConfirmedStartUtc] IS NOT NULL AND [ConfirmedEndUtc] IS NOT NULL AND [ConfirmedEmployeeId] IS NOT NULL AND [ConfirmedStartUtc]=[ProposedStartUtc] AND [ConfirmedEndUtc]>[ConfirmedStartUtc] AND [CoordinatedAtUtc] IS NOT NULL AND [CoordinationSource] IS NOT NULL AND [CoordinatedAtUtc]>'00010101' AND [CoordinatedAtUtc]<=[AtUtc] AND LEN(LTRIM(RTRIM([CoordinationSource])))>0 AND ([CoordinationReference] IS NULL OR LEN(LTRIM(RTRIM([CoordinationReference])))>0) AND [PreviousProposedStartUtc] IS NULL AND [PreviousProposedEndUtc] IS NULL AND [Reason] IS NULL AND [PreviousStatus] IS NULL AND [PreviousConfirmedStartUtc] IS NULL AND [PreviousConfirmedEndUtc] IS NULL AND [PreviousConfirmedEmployeeId] IS NULL AND [CancellationKind] IS NULL AND [OfferedEmployeeId] IS NULL AND [ProposedEndUtc]=DATEADD(minute,60,[ProposedStartUtc])) OR ([Kind]=N'AlternativeOffered' AND [ViewingVersion]>1 AND [ConfirmedStartUtc] IS NULL AND [ConfirmedEndUtc] IS NULL AND [ConfirmedEmployeeId] IS NULL AND [CoordinatedAtUtc] IS NOT NULL AND [CoordinationSource] IS NOT NULL AND [CoordinatedAtUtc]>'00010101' AND [CoordinatedAtUtc]<=[AtUtc] AND LEN(LTRIM(RTRIM([CoordinationSource])))>0 AND ([CoordinationReference] IS NULL OR LEN(LTRIM(RTRIM([CoordinationReference])))>0) AND [PreviousStatus] IS NULL AND [PreviousConfirmedStartUtc] IS NULL AND [PreviousConfirmedEndUtc] IS NULL AND [PreviousConfirmedEmployeeId] IS NULL AND [CancellationKind] IS NULL AND [OfferedEmployeeId] IS NOT NULL AND [PreviousProposedStartUtc] IS NOT NULL AND [PreviousProposedEndUtc] IS NOT NULL AND [PreviousProposedStartUtc]>'00010101' AND [PreviousProposedEndUtc]>[PreviousProposedStartUtc] AND [Reason] IS NOT NULL AND LEN(LTRIM(RTRIM([Reason])))>0) OR ([Kind]=N'AlternativeAccepted' AND [ViewingVersion]>=3 AND [ConfirmedStartUtc] IS NOT NULL AND [ConfirmedEndUtc] IS NOT NULL AND [ConfirmedEmployeeId] IS NOT NULL AND [ConfirmedStartUtc]=[ProposedStartUtc] AND [ConfirmedEndUtc]>[ConfirmedStartUtc] AND [CoordinatedAtUtc] IS NOT NULL AND [CoordinationSource] IS NOT NULL AND [CoordinatedAtUtc]>'00010101' AND [CoordinatedAtUtc]<=[AtUtc] AND LEN(LTRIM(RTRIM([CoordinationSource])))>0 AND ([CoordinationReference] IS NULL OR LEN(LTRIM(RTRIM([CoordinationReference])))>0) AND [PreviousProposedStartUtc] IS NULL AND [PreviousProposedEndUtc] IS NULL AND [Reason] IS NULL AND [PreviousStatus] IS NULL AND [PreviousConfirmedStartUtc] IS NULL AND [PreviousConfirmedEndUtc] IS NULL AND [PreviousConfirmedEmployeeId] IS NULL AND [CancellationKind] IS NULL AND [OfferedEmployeeId] IS NOT NULL AND [OfferedEmployeeId]=[ConfirmedEmployeeId] AND [ConfirmedEndUtc]=[ProposedEndUtc]) OR ([Kind]=N'Rescheduled' AND [ViewingVersion]>1 AND [PreviousStatus] IS NOT NULL AND [ConfirmedStartUtc] IS NULL AND [ConfirmedEndUtc] IS NULL AND [ConfirmedEmployeeId] IS NULL AND [CoordinatedAtUtc] IS NULL AND [CoordinationSource] IS NULL AND [CoordinationReference] IS NULL AND [Reason] IS NOT NULL AND LEN(LTRIM(RTRIM([Reason])))>0 AND [OfferedEmployeeId] IS NULL AND [CancellationKind] IS NULL AND [ProposedEndUtc]=DATEADD(minute,60,[ProposedStartUtc]) AND [PreviousProposedStartUtc] IS NOT NULL AND [PreviousProposedEndUtc] IS NOT NULL AND [PreviousProposedStartUtc]>[AtUtc] AND [PreviousProposedEndUtc]>[PreviousProposedStartUtc] AND (([PreviousStatus] IN(N'PendingConfirmation',N'AwaitingCustomerAcceptance') AND [PreviousConfirmedStartUtc] IS NULL AND [PreviousConfirmedEndUtc] IS NULL AND [PreviousConfirmedEmployeeId] IS NULL) OR ([PreviousStatus]=N'Confirmed' AND [PreviousConfirmedStartUtc] IS NOT NULL AND [PreviousConfirmedEndUtc] IS NOT NULL AND [PreviousConfirmedEmployeeId] IS NOT NULL AND [PreviousConfirmedStartUtc]>[AtUtc] AND [PreviousConfirmedEndUtc]>[PreviousConfirmedStartUtc]))) OR ([Kind]=N'Cancelled' AND [ViewingVersion]>1 AND [PreviousStatus] IS NOT NULL AND [Reason] IS NOT NULL AND LEN(LTRIM(RTRIM([Reason])))>0 AND [PreviousProposedStartUtc] IS NULL AND [PreviousProposedEndUtc] IS NULL AND [PreviousConfirmedStartUtc] IS NULL AND [PreviousConfirmedEndUtc] IS NULL AND [PreviousConfirmedEmployeeId] IS NULL AND [CancellationKind] IS NOT NULL AND [CancellationKind] IN(N'Customer',N'Broker',N'Developer') AND (([PreviousStatus]=N'PendingConfirmation' AND [ConfirmedStartUtc] IS NULL AND [ConfirmedEndUtc] IS NULL AND [ConfirmedEmployeeId] IS NULL AND [CoordinatedAtUtc] IS NULL AND [CoordinationSource] IS NULL AND [CoordinationReference] IS NULL AND [OfferedEmployeeId] IS NULL) OR ([PreviousStatus]=N'AwaitingCustomerAcceptance' AND [ConfirmedStartUtc] IS NULL AND [ConfirmedEndUtc] IS NULL AND [ConfirmedEmployeeId] IS NULL AND [CoordinatedAtUtc] IS NOT NULL AND [CoordinationSource] IS NOT NULL AND [CoordinatedAtUtc]>'00010101' AND [CoordinatedAtUtc]<=[AtUtc] AND LEN(LTRIM(RTRIM([CoordinationSource])))>0 AND ([CoordinationReference] IS NULL OR LEN(LTRIM(RTRIM([CoordinationReference])))>0) AND [OfferedEmployeeId] IS NOT NULL) OR ([PreviousStatus]=N'Confirmed' AND [ConfirmedStartUtc] IS NOT NULL AND [ConfirmedEndUtc] IS NOT NULL AND [ConfirmedEmployeeId] IS NOT NULL AND [ConfirmedStartUtc]=[ProposedStartUtc] AND [ConfirmedEndUtc]>[ConfirmedStartUtc] AND [CoordinatedAtUtc] IS NOT NULL AND [CoordinationSource] IS NOT NULL AND [CoordinatedAtUtc]>'00010101' AND [CoordinatedAtUtc]<=[AtUtc] AND LEN(LTRIM(RTRIM([CoordinationSource])))>0 AND ([CoordinationReference] IS NULL OR LEN(LTRIM(RTRIM([CoordinationReference])))>0) AND ([OfferedEmployeeId] IS NULL OR [OfferedEmployeeId]=[ConfirmedEmployeeId])))))) OR ([Kind]=N'Expired' AND [ViewingVersion]>1 AND [ActorKind]=N'System' AND [ActorAccountId] IS NULL AND [EffectiveCutoffUtc] IS NOT NULL AND [EffectiveCutoffUtc]>'00010101' AND [EffectiveCutoffUtc]<=[AtUtc] AND [EffectiveCutoffUtc]<=[ProposedStartUtc] AND [PreviousStatus] IS NOT NULL AND [Reason] IS NOT NULL AND [ConfirmedStartUtc] IS NULL AND [ConfirmedEndUtc] IS NULL AND [ConfirmedEmployeeId] IS NULL AND [PreviousConfirmedStartUtc] IS NULL AND [PreviousConfirmedEndUtc] IS NULL AND [PreviousConfirmedEmployeeId] IS NULL AND [PreviousProposedStartUtc] IS NULL AND [PreviousProposedEndUtc] IS NULL AND [CancellationKind] IS NULL AND (([PreviousStatus]=N'PendingConfirmation' AND [CoordinatedAtUtc] IS NULL AND [CoordinationSource] IS NULL AND [CoordinationReference] IS NULL AND [OfferedEmployeeId] IS NULL AND [Reason]=N'CoordinationDeadline') OR ([PreviousStatus]=N'AwaitingCustomerAcceptance' AND [CoordinatedAtUtc] IS NOT NULL AND [CoordinationSource] IS NOT NULL AND [CoordinatedAtUtc]>'00010101' AND [CoordinatedAtUtc]<=[AtUtc] AND LEN(LTRIM(RTRIM([CoordinationSource])))>0 AND ([CoordinationReference] IS NULL OR LEN(LTRIM(RTRIM([CoordinationReference])))>0) AND [OfferedEmployeeId] IS NOT NULL AND [EffectiveCutoffUtc]=[ProposedStartUtc] AND [Reason]=N'AlternativeStart')) AND [OutcomeOccurredAtUtc] IS NULL AND [OutcomeNoteId] IS NULL) OR ([ActorKind]=N'User' AND [ActorAccountId] IS NOT NULL AND [EffectiveCutoffUtc] IS NULL AND [Kind] IN(N'Completed',N'CustomerNoShow') AND [ViewingVersion]>2 AND [ConfirmedStartUtc] IS NOT NULL AND [ConfirmedEndUtc] IS NOT NULL AND [ConfirmedEmployeeId] IS NOT NULL AND [ConfirmedStartUtc]=[ProposedStartUtc] AND [ConfirmedEndUtc]>[ConfirmedStartUtc] AND [CoordinatedAtUtc] IS NOT NULL AND [CoordinationSource] IS NOT NULL AND [CoordinatedAtUtc]>'00010101' AND [CoordinatedAtUtc]<=[AtUtc] AND LEN(LTRIM(RTRIM([CoordinationSource])))>0 AND ([CoordinationReference] IS NULL OR LEN(LTRIM(RTRIM([CoordinationReference])))>0) AND [PreviousProposedStartUtc] IS NULL AND [PreviousProposedEndUtc] IS NULL AND [Reason] IS NULL AND [PreviousStatus] IS NULL AND [PreviousConfirmedStartUtc] IS NULL AND [PreviousConfirmedEndUtc] IS NULL AND [PreviousConfirmedEmployeeId] IS NULL AND [CancellationKind] IS NULL AND [OutcomeOccurredAtUtc] IS NOT NULL AND [OutcomeNoteId] IS NOT NULL AND [OutcomeOccurredAtUtc]>=[ConfirmedStartUtc] AND [OutcomeOccurredAtUtc]<=[AtUtc] AND ([OfferedEmployeeId] IS NULL OR [OfferedEmployeeId]=[ConfirmedEmployeeId])))");

            migrationBuilder.AddForeignKey(
                name: "FK_ViewingEvents_SalesNotes_OutcomeNoteId_CustomerId",
                schema: "Crm",
                table: "ViewingEvents",
                columns: new[] { "OutcomeNoteId", "CustomerId" },
                principalSchema: "Crm",
                principalTable: "SalesNotes",
                principalColumns: new[] { "Id", "CustomerId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Viewings_Accounts_OutcomeRecordedByAccountId",
                schema: "Crm",
                table: "Viewings",
                column: "OutcomeRecordedByAccountId",
                principalSchema: "Identity",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Viewings_SalesNotes_OutcomeNoteId_CustomerId",
                schema: "Crm",
                table: "Viewings",
                columns: new[] { "OutcomeNoteId", "CustomerId" },
                principalSchema: "Crm",
                principalTable: "SalesNotes",
                principalColumns: new[] { "Id", "CustomerId" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ViewingEvents_SalesNotes_OutcomeNoteId_CustomerId",
                schema: "Crm",
                table: "ViewingEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_Viewings_Accounts_OutcomeRecordedByAccountId",
                schema: "Crm",
                table: "Viewings");

            migrationBuilder.DropForeignKey(
                name: "FK_Viewings_SalesNotes_OutcomeNoteId_CustomerId",
                schema: "Crm",
                table: "Viewings");

            migrationBuilder.DropIndex(
                name: "IX_Viewings_OutcomeNoteId_CustomerId",
                schema: "Crm",
                table: "Viewings");

            migrationBuilder.DropIndex(
                name: "IX_Viewings_OutcomeRecordedByAccountId",
                schema: "Crm",
                table: "Viewings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Viewings_InitialState",
                schema: "Crm",
                table: "Viewings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Viewings_OutcomeAudit",
                schema: "Crm",
                table: "Viewings");

            migrationBuilder.DropIndex(
                name: "IX_ViewingEvents_OutcomeNoteId_CustomerId",
                schema: "Crm",
                table: "ViewingEvents");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ViewingEvents_Shape",
                schema: "Crm",
                table: "ViewingEvents");

            migrationBuilder.DropColumn(
                name: "OutcomeNoteId",
                schema: "Crm",
                table: "Viewings");

            migrationBuilder.DropColumn(
                name: "OutcomeOccurredAtUtc",
                schema: "Crm",
                table: "Viewings");

            migrationBuilder.DropColumn(
                name: "OutcomeRecordedAtUtc",
                schema: "Crm",
                table: "Viewings");

            migrationBuilder.DropColumn(
                name: "OutcomeRecordedByAccountId",
                schema: "Crm",
                table: "Viewings");

            migrationBuilder.DropColumn(
                name: "OutcomeNoteId",
                schema: "Crm",
                table: "ViewingEvents");

            migrationBuilder.DropColumn(
                name: "OutcomeOccurredAtUtc",
                schema: "Crm",
                table: "ViewingEvents");

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
        }
    }
}
