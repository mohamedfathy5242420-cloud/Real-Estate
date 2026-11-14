using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrokerHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddViewingAlternatives : Migration
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

            migrationBuilder.AddColumn<DateTime>(
                name: "OfferedAtUtc",
                schema: "Crm",
                table: "Viewings",
                type: "datetime2(7)",
                precision: 7,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OfferedByAccountId",
                schema: "Crm",
                table: "Viewings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OfferedEmployeeId",
                schema: "Crm",
                table: "Viewings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OfferedEmployeeId",
                schema: "Crm",
                table: "ViewingEvents",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PreviousProposedEndUtc",
                schema: "Crm",
                table: "ViewingEvents",
                type: "datetime2(7)",
                precision: 7,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PreviousProposedStartUtc",
                schema: "Crm",
                table: "ViewingEvents",
                type: "datetime2(7)",
                precision: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Reason",
                schema: "Crm",
                table: "ViewingEvents",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Viewings_OfferedByAccountId",
                schema: "Crm",
                table: "Viewings",
                column: "OfferedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Viewings_OfferedEmployeeId",
                schema: "Crm",
                table: "Viewings",
                column: "OfferedEmployeeId");

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
                name: "CK_Viewings_OfferAudit",
                schema: "Crm",
                table: "Viewings",
                sql: "[OfferedAtUtc] IS NULL OR ([OfferedAtUtc]>=[RequestedAtUtc] AND [OfferedAtUtc]<[CurrentProposedStartUtc] AND [CoordinatedAtUtc]<=[OfferedAtUtc])");

            migrationBuilder.CreateIndex(
                name: "IX_ViewingEvents_OfferedEmployeeId",
                schema: "Crm",
                table: "ViewingEvents",
                column: "OfferedEmployeeId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ViewingEvents_Shape",
                schema: "Crm",
                table: "ViewingEvents",
                sql: "[AtUtc]>'00010101' AND [ProposedStartUtc]>[AtUtc] AND [ProposedStartUtc]<=DATEADD(day,30,[AtUtc]) AND [ProposedEndUtc]>[ProposedStartUtc] AND (([Kind]=N'Requested' AND [ViewingVersion]=1 AND [ConfirmedStartUtc] IS NULL AND [ConfirmedEndUtc] IS NULL AND [ConfirmedEmployeeId] IS NULL AND [CoordinatedAtUtc] IS NULL AND [CoordinationSource] IS NULL AND [CoordinationReference] IS NULL AND [PreviousProposedStartUtc] IS NULL AND [PreviousProposedEndUtc] IS NULL AND [Reason] IS NULL AND [OfferedEmployeeId] IS NULL AND [ProposedEndUtc]=DATEADD(minute,60,[ProposedStartUtc])) OR ([Kind]=N'Confirmed' AND [ViewingVersion]>1 AND [ConfirmedStartUtc] IS NOT NULL AND [ConfirmedEndUtc] IS NOT NULL AND [ConfirmedEmployeeId] IS NOT NULL AND [ConfirmedStartUtc]=[ProposedStartUtc] AND [ConfirmedEndUtc]>[ConfirmedStartUtc] AND [CoordinatedAtUtc] IS NOT NULL AND [CoordinationSource] IS NOT NULL AND [CoordinatedAtUtc]>'00010101' AND [CoordinatedAtUtc]<=[AtUtc] AND LEN(LTRIM(RTRIM([CoordinationSource])))>0 AND ([CoordinationReference] IS NULL OR LEN(LTRIM(RTRIM([CoordinationReference])))>0) AND [PreviousProposedStartUtc] IS NULL AND [PreviousProposedEndUtc] IS NULL AND [Reason] IS NULL AND [OfferedEmployeeId] IS NULL AND [ProposedEndUtc]=DATEADD(minute,60,[ProposedStartUtc])) OR ([Kind]=N'AlternativeOffered' AND [ViewingVersion]>1 AND [ConfirmedStartUtc] IS NULL AND [ConfirmedEndUtc] IS NULL AND [ConfirmedEmployeeId] IS NULL AND [CoordinatedAtUtc] IS NOT NULL AND [CoordinationSource] IS NOT NULL AND [CoordinatedAtUtc]>'00010101' AND [CoordinatedAtUtc]<=[AtUtc] AND LEN(LTRIM(RTRIM([CoordinationSource])))>0 AND ([CoordinationReference] IS NULL OR LEN(LTRIM(RTRIM([CoordinationReference])))>0) AND [OfferedEmployeeId] IS NOT NULL AND [PreviousProposedStartUtc] IS NOT NULL AND [PreviousProposedEndUtc] IS NOT NULL AND [PreviousProposedStartUtc]>'00010101' AND [PreviousProposedEndUtc]>[PreviousProposedStartUtc] AND [Reason] IS NOT NULL AND LEN(LTRIM(RTRIM([Reason])))>0) OR ([Kind]=N'AlternativeAccepted' AND [ViewingVersion]>=3 AND [ConfirmedStartUtc] IS NOT NULL AND [ConfirmedEndUtc] IS NOT NULL AND [ConfirmedEmployeeId] IS NOT NULL AND [ConfirmedStartUtc]=[ProposedStartUtc] AND [ConfirmedEndUtc]>[ConfirmedStartUtc] AND [CoordinatedAtUtc] IS NOT NULL AND [CoordinationSource] IS NOT NULL AND [CoordinatedAtUtc]>'00010101' AND [CoordinatedAtUtc]<=[AtUtc] AND LEN(LTRIM(RTRIM([CoordinationSource])))>0 AND ([CoordinationReference] IS NULL OR LEN(LTRIM(RTRIM([CoordinationReference])))>0) AND [PreviousProposedStartUtc] IS NULL AND [PreviousProposedEndUtc] IS NULL AND [Reason] IS NULL AND [OfferedEmployeeId] IS NOT NULL AND [OfferedEmployeeId]=[ConfirmedEmployeeId] AND [ConfirmedEndUtc]=[ProposedEndUtc]))");

            migrationBuilder.AddForeignKey(
                name: "FK_ViewingEvents_Employees_OfferedEmployeeId",
                schema: "Crm",
                table: "ViewingEvents",
                column: "OfferedEmployeeId",
                principalSchema: "Crm",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Viewings_Accounts_OfferedByAccountId",
                schema: "Crm",
                table: "Viewings",
                column: "OfferedByAccountId",
                principalSchema: "Identity",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Viewings_Employees_OfferedEmployeeId",
                schema: "Crm",
                table: "Viewings",
                column: "OfferedEmployeeId",
                principalSchema: "Crm",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ViewingEvents_Employees_OfferedEmployeeId",
                schema: "Crm",
                table: "ViewingEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_Viewings_Accounts_OfferedByAccountId",
                schema: "Crm",
                table: "Viewings");

            migrationBuilder.DropForeignKey(
                name: "FK_Viewings_Employees_OfferedEmployeeId",
                schema: "Crm",
                table: "Viewings");

            migrationBuilder.DropIndex(
                name: "IX_Viewings_OfferedByAccountId",
                schema: "Crm",
                table: "Viewings");

            migrationBuilder.DropIndex(
                name: "IX_Viewings_OfferedEmployeeId",
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
                name: "CK_Viewings_InitialState",
                schema: "Crm",
                table: "Viewings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Viewings_Interval",
                schema: "Crm",
                table: "Viewings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Viewings_OfferAudit",
                schema: "Crm",
                table: "Viewings");

            migrationBuilder.DropIndex(
                name: "IX_ViewingEvents_OfferedEmployeeId",
                schema: "Crm",
                table: "ViewingEvents");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ViewingEvents_Shape",
                schema: "Crm",
                table: "ViewingEvents");

            migrationBuilder.DropColumn(
                name: "OfferedAtUtc",
                schema: "Crm",
                table: "Viewings");

            migrationBuilder.DropColumn(
                name: "OfferedByAccountId",
                schema: "Crm",
                table: "Viewings");

            migrationBuilder.DropColumn(
                name: "OfferedEmployeeId",
                schema: "Crm",
                table: "Viewings");

            migrationBuilder.DropColumn(
                name: "OfferedEmployeeId",
                schema: "Crm",
                table: "ViewingEvents");

            migrationBuilder.DropColumn(
                name: "PreviousProposedEndUtc",
                schema: "Crm",
                table: "ViewingEvents");

            migrationBuilder.DropColumn(
                name: "PreviousProposedStartUtc",
                schema: "Crm",
                table: "ViewingEvents");

            migrationBuilder.DropColumn(
                name: "Reason",
                schema: "Crm",
                table: "ViewingEvents");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Viewings_ConfirmationInterval",
                schema: "Crm",
                table: "Viewings",
                sql: "[Status]<>N'Confirmed' OR ([ConfirmedStartUtc]=[CurrentProposedStartUtc] AND [ConfirmedEndUtc]>[ConfirmedStartUtc] AND [ConfirmedAtUtc]<[ConfirmedStartUtc] AND [ConfirmedAtUtc]<DATEADD(day,7,[PendingCycleStartedAtUtc]))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Viewings_Coordination",
                schema: "Crm",
                table: "Viewings",
                sql: "[Status]<>N'Confirmed' OR ([CoordinatedAtUtc]>=[PendingCycleStartedAtUtc] AND [CoordinatedAtUtc]<=[ConfirmedAtUtc] AND LEN(LTRIM(RTRIM([CoordinationSource])))>0 AND ([CoordinationReference] IS NULL OR LEN(LTRIM(RTRIM([CoordinationReference])))>0))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Viewings_InitialState",
                schema: "Crm",
                table: "Viewings",
                sql: "([Status]=N'PendingConfirmation' AND [Version]=1 AND [ConfirmedStartUtc] IS NULL AND [ConfirmedEndUtc] IS NULL AND [ConfirmedEmployeeId] IS NULL AND [ConfirmedAtUtc] IS NULL AND [ConfirmedByAccountId] IS NULL AND [CoordinatedAtUtc] IS NULL AND [CoordinationSource] IS NULL AND [CoordinationReference] IS NULL) OR ([Status]=N'Confirmed' AND [Version]>1 AND [ConfirmedStartUtc] IS NOT NULL AND [ConfirmedEndUtc] IS NOT NULL AND [ConfirmedEmployeeId] IS NOT NULL AND [ConfirmedAtUtc] IS NOT NULL AND [ConfirmedByAccountId] IS NOT NULL AND [CoordinatedAtUtc] IS NOT NULL AND [CoordinationSource] IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Viewings_Interval",
                schema: "Crm",
                table: "Viewings",
                sql: "[RequestedAtUtc]>'00010101' AND [CurrentProposedStartUtc]>[RequestedAtUtc] AND [CurrentProposedStartUtc]<=DATEADD(day,30,[RequestedAtUtc]) AND [CurrentProposedEndUtc]=DATEADD(minute,60,[CurrentProposedStartUtc])");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ViewingEvents_Shape",
                schema: "Crm",
                table: "ViewingEvents",
                sql: "[AtUtc]>'00010101' AND [ProposedStartUtc]>[AtUtc] AND [ProposedStartUtc]<=DATEADD(day,30,[AtUtc]) AND [ProposedEndUtc]=DATEADD(minute,60,[ProposedStartUtc]) AND (([Kind]=N'Requested' AND [ViewingVersion]=1 AND [ConfirmedStartUtc] IS NULL AND [ConfirmedEndUtc] IS NULL AND [ConfirmedEmployeeId] IS NULL AND [CoordinatedAtUtc] IS NULL AND [CoordinationSource] IS NULL AND [CoordinationReference] IS NULL) OR ([Kind]=N'Confirmed' AND [ViewingVersion]>1 AND [ConfirmedStartUtc] IS NOT NULL AND [ConfirmedEndUtc] IS NOT NULL AND [ConfirmedEmployeeId] IS NOT NULL AND [CoordinatedAtUtc] IS NOT NULL AND [CoordinationSource] IS NOT NULL AND [ConfirmedStartUtc]=[ProposedStartUtc] AND [ConfirmedEndUtc]>[ConfirmedStartUtc] AND [CoordinatedAtUtc]>'00010101' AND [CoordinatedAtUtc]<=[AtUtc] AND LEN(LTRIM(RTRIM([CoordinationSource])))>0 AND ([CoordinationReference] IS NULL OR LEN(LTRIM(RTRIM([CoordinationReference])))>0)))");
        }
    }
}
