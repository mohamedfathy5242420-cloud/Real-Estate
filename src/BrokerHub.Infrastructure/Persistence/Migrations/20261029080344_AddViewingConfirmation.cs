using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrokerHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddViewingConfirmation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Viewings_InitialState",
                schema: "Crm",
                table: "Viewings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ViewingEvents_InitialShape",
                schema: "Crm",
                table: "ViewingEvents");

            migrationBuilder.AddColumn<DateTime>(
                name: "ConfirmedAtUtc",
                schema: "Crm",
                table: "Viewings",
                type: "datetime2(7)",
                precision: 7,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ConfirmedByAccountId",
                schema: "Crm",
                table: "Viewings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ConfirmedEmployeeId",
                schema: "Crm",
                table: "Viewings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ConfirmedEndUtc",
                schema: "Crm",
                table: "Viewings",
                type: "datetime2(7)",
                precision: 7,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ConfirmedStartUtc",
                schema: "Crm",
                table: "Viewings",
                type: "datetime2(7)",
                precision: 7,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CoordinatedAtUtc",
                schema: "Crm",
                table: "Viewings",
                type: "datetime2(7)",
                precision: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CoordinationReference",
                schema: "Crm",
                table: "Viewings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CoordinationSource",
                schema: "Crm",
                table: "Viewings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ConfirmedEmployeeId",
                schema: "Crm",
                table: "ViewingEvents",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ConfirmedEndUtc",
                schema: "Crm",
                table: "ViewingEvents",
                type: "datetime2(7)",
                precision: 7,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ConfirmedStartUtc",
                schema: "Crm",
                table: "ViewingEvents",
                type: "datetime2(7)",
                precision: 7,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CoordinatedAtUtc",
                schema: "Crm",
                table: "ViewingEvents",
                type: "datetime2(7)",
                precision: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CoordinationReference",
                schema: "Crm",
                table: "ViewingEvents",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CoordinationSource",
                schema: "Crm",
                table: "ViewingEvents",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Viewings_ConfirmedByAccountId",
                schema: "Crm",
                table: "Viewings",
                column: "ConfirmedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Viewings_ConfirmedEmployeeInterval",
                schema: "Crm",
                table: "Viewings",
                columns: new[] { "ConfirmedEmployeeId", "ConfirmedStartUtc", "ConfirmedEndUtc" },
                filter: "[Status] = N'Confirmed'");

            migrationBuilder.CreateIndex(
                name: "IX_Viewings_ConfirmedUnitInterval",
                schema: "Crm",
                table: "Viewings",
                columns: new[] { "UnitId", "ConfirmedStartUtc", "ConfirmedEndUtc" },
                filter: "[Status] = N'Confirmed'");

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

            migrationBuilder.CreateIndex(
                name: "IX_ViewingEvents_ConfirmedEmployeeId",
                schema: "Crm",
                table: "ViewingEvents",
                column: "ConfirmedEmployeeId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ViewingEvents_Shape",
                schema: "Crm",
                table: "ViewingEvents",
                sql: "[AtUtc]>'00010101' AND [ProposedStartUtc]>[AtUtc] AND [ProposedStartUtc]<=DATEADD(day,30,[AtUtc]) AND [ProposedEndUtc]=DATEADD(minute,60,[ProposedStartUtc]) AND (([Kind]=N'Requested' AND [ViewingVersion]=1 AND [ConfirmedStartUtc] IS NULL AND [ConfirmedEndUtc] IS NULL AND [ConfirmedEmployeeId] IS NULL AND [CoordinatedAtUtc] IS NULL AND [CoordinationSource] IS NULL AND [CoordinationReference] IS NULL) OR ([Kind]=N'Confirmed' AND [ViewingVersion]>1 AND [ConfirmedStartUtc] IS NOT NULL AND [ConfirmedEndUtc] IS NOT NULL AND [ConfirmedEmployeeId] IS NOT NULL AND [CoordinatedAtUtc] IS NOT NULL AND [CoordinationSource] IS NOT NULL AND [ConfirmedStartUtc]=[ProposedStartUtc] AND [ConfirmedEndUtc]>[ConfirmedStartUtc] AND [CoordinatedAtUtc]>'00010101' AND [CoordinatedAtUtc]<=[AtUtc] AND LEN(LTRIM(RTRIM([CoordinationSource])))>0 AND ([CoordinationReference] IS NULL OR LEN(LTRIM(RTRIM([CoordinationReference])))>0)))");

            migrationBuilder.AddForeignKey(
                name: "FK_ViewingEvents_Employees_ConfirmedEmployeeId",
                schema: "Crm",
                table: "ViewingEvents",
                column: "ConfirmedEmployeeId",
                principalSchema: "Crm",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Viewings_Accounts_ConfirmedByAccountId",
                schema: "Crm",
                table: "Viewings",
                column: "ConfirmedByAccountId",
                principalSchema: "Identity",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Viewings_Employees_ConfirmedEmployeeId",
                schema: "Crm",
                table: "Viewings",
                column: "ConfirmedEmployeeId",
                principalSchema: "Crm",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ViewingEvents_Employees_ConfirmedEmployeeId",
                schema: "Crm",
                table: "ViewingEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_Viewings_Accounts_ConfirmedByAccountId",
                schema: "Crm",
                table: "Viewings");

            migrationBuilder.DropForeignKey(
                name: "FK_Viewings_Employees_ConfirmedEmployeeId",
                schema: "Crm",
                table: "Viewings");

            migrationBuilder.DropIndex(
                name: "IX_Viewings_ConfirmedByAccountId",
                schema: "Crm",
                table: "Viewings");

            migrationBuilder.DropIndex(
                name: "IX_Viewings_ConfirmedEmployeeInterval",
                schema: "Crm",
                table: "Viewings");

            migrationBuilder.DropIndex(
                name: "IX_Viewings_ConfirmedUnitInterval",
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

            migrationBuilder.DropIndex(
                name: "IX_ViewingEvents_ConfirmedEmployeeId",
                schema: "Crm",
                table: "ViewingEvents");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ViewingEvents_Shape",
                schema: "Crm",
                table: "ViewingEvents");

            migrationBuilder.DropColumn(
                name: "ConfirmedAtUtc",
                schema: "Crm",
                table: "Viewings");

            migrationBuilder.DropColumn(
                name: "ConfirmedByAccountId",
                schema: "Crm",
                table: "Viewings");

            migrationBuilder.DropColumn(
                name: "ConfirmedEmployeeId",
                schema: "Crm",
                table: "Viewings");

            migrationBuilder.DropColumn(
                name: "ConfirmedEndUtc",
                schema: "Crm",
                table: "Viewings");

            migrationBuilder.DropColumn(
                name: "ConfirmedStartUtc",
                schema: "Crm",
                table: "Viewings");

            migrationBuilder.DropColumn(
                name: "CoordinatedAtUtc",
                schema: "Crm",
                table: "Viewings");

            migrationBuilder.DropColumn(
                name: "CoordinationReference",
                schema: "Crm",
                table: "Viewings");

            migrationBuilder.DropColumn(
                name: "CoordinationSource",
                schema: "Crm",
                table: "Viewings");

            migrationBuilder.DropColumn(
                name: "ConfirmedEmployeeId",
                schema: "Crm",
                table: "ViewingEvents");

            migrationBuilder.DropColumn(
                name: "ConfirmedEndUtc",
                schema: "Crm",
                table: "ViewingEvents");

            migrationBuilder.DropColumn(
                name: "ConfirmedStartUtc",
                schema: "Crm",
                table: "ViewingEvents");

            migrationBuilder.DropColumn(
                name: "CoordinatedAtUtc",
                schema: "Crm",
                table: "ViewingEvents");

            migrationBuilder.DropColumn(
                name: "CoordinationReference",
                schema: "Crm",
                table: "ViewingEvents");

            migrationBuilder.DropColumn(
                name: "CoordinationSource",
                schema: "Crm",
                table: "ViewingEvents");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Viewings_InitialState",
                schema: "Crm",
                table: "Viewings",
                sql: "[Status]=N'PendingConfirmation' AND [Version]=1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ViewingEvents_InitialShape",
                schema: "Crm",
                table: "ViewingEvents",
                sql: "[Kind]=N'Requested' AND [ViewingVersion]=1 AND [AtUtc]>'00010101' AND [ProposedStartUtc]>[AtUtc] AND [ProposedStartUtc]<=DATEADD(day,30,[AtUtc]) AND [ProposedEndUtc]=DATEADD(minute,60,[ProposedStartUtc])");
        }
    }
}
