using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrokerHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AdoptExternalSales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Bookings_LastExtension",
                schema: "Crm",
                table: "Bookings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Bookings_State",
                schema: "Crm",
                table: "Bookings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_BookingEvents_Shape",
                schema: "Crm",
                table: "BookingEvents");

            migrationBuilder.EnsureSchema(
                name: "Sales");

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAtUtc",
                schema: "Crm",
                table: "Bookings",
                type: "datetime2(7)",
                precision: 7,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompletedByAccountId",
                schema: "Crm",
                table: "Bookings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompletedDealId",
                schema: "Crm",
                table: "Bookings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DealId",
                schema: "Crm",
                table: "BookingEvents",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Leads_Id_CustomerId_ProjectId_MarketingAgreementId",
                schema: "Crm",
                table: "Leads",
                columns: new[] { "Id", "CustomerId", "ProjectId", "MarketingAgreementId" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_CommissionRules_Id_MarketingAgreementId_DeveloperId",
                schema: "Catalog",
                table: "CommissionRules",
                columns: new[] { "Id", "MarketingAgreementId", "DeveloperId" });

            migrationBuilder.CreateTable(
                name: "Deals",
                schema: "Sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LeadId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LeadVersion = table.Column<long>(type: "bigint", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeveloperId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AgreementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AgreementVersion = table.Column<long>(type: "bigint", nullable: false),
                    AgreementNumber = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    AgreementScope = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    AgreementStartsOn = table.Column<DateOnly>(type: "date", nullable: false),
                    AgreementEndsOn = table.Column<DateOnly>(type: "date", nullable: false),
                    ProtectionPeriodDays = table.Column<int>(type: "int", nullable: false),
                    ProtectionEndsOn = table.Column<DateOnly>(type: "date", nullable: false),
                    LeadOpenedOnLocalDate = table.Column<DateOnly>(type: "date", nullable: false),
                    OpeningTimeZoneId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RuleVersion = table.Column<long>(type: "bigint", nullable: false),
                    RuleProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RuleType = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Percentage = table.Column<decimal>(type: "decimal(7,4)", precision: 7, scale: 4, nullable: true),
                    FixedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    SaleLocalDate = table.Column<DateOnly>(type: "date", nullable: false),
                    FinalUnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SaleOccurredAtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SelectionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AdoptedAtUtc = table.Column<DateTime>(type: "datetime2(7)", precision: 7, nullable: false),
                    AdoptedByAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Deals", x => x.Id);
                    table.UniqueConstraint("AK_Deals_Id_CustomerId_UnitId", x => new { x.Id, x.CustomerId, x.UnitId });
                    table.CheckConstraint("CK_Deals_Clock", "[SaleOccurredAtUtc]>'00010101' AND [AdoptedAtUtc]>=[SaleOccurredAtUtc]");
                    table.CheckConstraint("CK_Deals_Evidence", "LEN(LTRIM(RTRIM([Source])))>0 AND LEN(LTRIM(RTRIM([Reference])))>0 AND ([SelectionReason] IS NULL OR LEN(LTRIM(RTRIM([SelectionReason])))>0)");
                    table.CheckConstraint("CK_Deals_Money", "[FinalUnitPrice]>0 AND [Amount]>=0 AND LEN([CurrencyCode])=3 AND [CurrencyCode] COLLATE Latin1_General_100_BIN2 NOT LIKE N'%[^A-Z]%' AND (([RuleType]=N'Percentage' AND [Percentage] IS NOT NULL AND [Percentage]>0 AND [Percentage]<=100 AND [FixedAmount] IS NULL AND [Amount]=ROUND([FinalUnitPrice]*[Percentage]/CONVERT(decimal(3,0),100),2)) OR ([RuleType]=N'FixedAmount' AND [FixedAmount] IS NOT NULL AND [FixedAmount]>=0 AND [Percentage] IS NULL AND [Amount]=[FixedAmount]))");
                    table.CheckConstraint("CK_Deals_OriginDates", "[AgreementStartsOn]>'00010101' AND [AgreementEndsOn]>=[AgreementStartsOn] AND [LeadOpenedOnLocalDate]>=[AgreementStartsOn] AND [LeadOpenedOnLocalDate]<=[AgreementEndsOn] AND [ProtectionPeriodDays]>=0 AND [ProtectionEndsOn]>=[AgreementEndsOn] AND DATEDIFF(day,[AgreementEndsOn],[ProtectionEndsOn])=[ProtectionPeriodDays] AND [SaleLocalDate]>=[LeadOpenedOnLocalDate] AND [SaleLocalDate]<=[ProtectionEndsOn]");
                    table.CheckConstraint("CK_Deals_SnapshotScope", "[AgreementScope] IN (N'AllDeveloperProjects',N'SelectedProjects') AND ([RuleProjectId] IS NULL OR [RuleProjectId]=[ProjectId]) AND LEN(LTRIM(RTRIM([AgreementNumber])))>0 AND LEN(LTRIM(RTRIM([OpeningTimeZoneId])))>0");
                    table.CheckConstraint("CK_Deals_Versions", "[Version]=1 AND [LeadVersion]>0 AND [AgreementVersion]>0 AND [RuleVersion]>0");
                    table.ForeignKey(
                        name: "FK_Deals_Accounts_AdoptedByAccountId",
                        column: x => x.AdoptedByAccountId,
                        principalSchema: "Identity",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Deals_CommissionRules_RuleId_AgreementId_DeveloperId",
                        columns: x => new { x.RuleId, x.AgreementId, x.DeveloperId },
                        principalSchema: "Catalog",
                        principalTable: "CommissionRules",
                        principalColumns: new[] { "Id", "MarketingAgreementId", "DeveloperId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Deals_Leads_LeadId_CustomerId_ProjectId_AgreementId",
                        columns: x => new { x.LeadId, x.CustomerId, x.ProjectId, x.AgreementId },
                        principalSchema: "Crm",
                        principalTable: "Leads",
                        principalColumns: new[] { "Id", "CustomerId", "ProjectId", "MarketingAgreementId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Deals_MarketingAgreements_AgreementId_DeveloperId",
                        columns: x => new { x.AgreementId, x.DeveloperId },
                        principalSchema: "Catalog",
                        principalTable: "MarketingAgreements",
                        principalColumns: new[] { "Id", "DeveloperId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Deals_Units_UnitId_ProjectId",
                        columns: x => new { x.UnitId, x.ProjectId },
                        principalSchema: "Catalog",
                        principalTable: "Units",
                        principalColumns: new[] { "Id", "ProjectId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DealBookingLinks",
                schema: "Sales",
                columns: table => new
                {
                    DealId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookingVersionBefore = table.Column<long>(type: "bigint", nullable: false),
                    BookingVersionAfter = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DealBookingLinks", x => x.DealId);
                    table.UniqueConstraint("AK_DealBookingLinks_DealId_BookingId_CustomerId", x => new { x.DealId, x.BookingId, x.CustomerId });
                    table.UniqueConstraint("AK_DealBookingLinks_DealId_BookingId_CustomerId_UnitId", x => new { x.DealId, x.BookingId, x.CustomerId, x.UnitId });
                    table.CheckConstraint("CK_DealBookingLinks_Versions", "[BookingVersionBefore]>1 AND [BookingVersionBefore]<9223372036854775807 AND [BookingVersionAfter]=[BookingVersionBefore]+1");
                    table.ForeignKey(
                        name: "FK_DealBookingLinks_Bookings_BookingId_CustomerId_UnitId",
                        columns: x => new { x.BookingId, x.CustomerId, x.UnitId },
                        principalSchema: "Crm",
                        principalTable: "Bookings",
                        principalColumns: new[] { "Id", "CustomerId", "UnitId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DealBookingLinks_Deals_DealId_CustomerId_UnitId",
                        columns: x => new { x.DealId, x.CustomerId, x.UnitId },
                        principalSchema: "Sales",
                        principalTable: "Deals",
                        principalColumns: new[] { "Id", "CustomerId", "UnitId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_CompletedByAccountId",
                schema: "Crm",
                table: "Bookings",
                column: "CompletedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_CompletedDealId_Id_CustomerId_UnitId",
                schema: "Crm",
                table: "Bookings",
                columns: new[] { "CompletedDealId", "Id", "CustomerId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_CurrentUnitReservation",
                schema: "Crm",
                table: "Bookings",
                columns: new[] { "UnitId", "ReservationExpiresAtUtc", "Id" },
                filter: "[Status] = N'Confirmed'")
                .Annotation("SqlServer:Include", new[] { "CustomerId", "Version" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Bookings_LastExtension",
                schema: "Crm",
                table: "Bookings",
                sql: "([LastExtendedAtUtc] IS NULL AND [LastExtensionId] IS NULL) OR ([LastExtendedAtUtc] IS NOT NULL AND [LastExtensionId] IS NOT NULL AND ([Status] IN (N'Confirmed',N'Expired',N'Completed') OR ([Status]=N'Cancelled' AND [CancelledFromStatus]=N'Confirmed')) AND [Version]>2 AND [ConfirmedAtUtc] IS NOT NULL AND [ReservationExpiresAtUtc] IS NOT NULL AND [LastExtendedAtUtc]>=[ConfirmedAtUtc] AND [LastExtendedAtUtc]<[ReservationExpiresAtUtc])");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Bookings_State",
                schema: "Crm",
                table: "Bookings",
                sql: "((((([Status]=N'PendingReview' AND [Version]=1 AND [IsActiveRequest]=1 AND [RejectedAtUtc] IS NULL AND [RejectionReason] IS NULL AND [RejectedByAccountId] IS NULL AND [RejectionActorKind] IS NULL AND [ConfirmedAtUtc] IS NULL AND [ConfirmedByAccountId] IS NULL AND [ConfirmedPrice] IS NULL AND [ConfirmedCurrencyCode] IS NULL AND [ReservationExpiresAtUtc] IS NULL AND [ConfirmedResponseId] IS NULL AND [ExpiredAtUtc] IS NULL)\nOR ([Status]=N'Rejected' AND [Version]>1 AND [IsActiveRequest]=0 AND [RejectedAtUtc] IS NOT NULL AND [RejectionReason] IS NOT NULL AND [RejectionActorKind] IS NOT NULL AND [ConfirmedAtUtc] IS NULL AND [ConfirmedByAccountId] IS NULL AND [ConfirmedPrice] IS NULL AND [ConfirmedCurrencyCode] IS NULL AND [ReservationExpiresAtUtc] IS NULL AND [ConfirmedResponseId] IS NULL AND [ExpiredAtUtc] IS NULL AND\n    (([RejectedAtUtc]>=[ResponseDueAtUtc] AND [RejectionReason]=N'response-timeout' AND [RejectedByAccountId] IS NULL AND [RejectionActorKind]=N'System') OR\n    ([RejectedAtUtc]>=[RequestedAtUtc] AND [RejectedAtUtc]<[ResponseDueAtUtc] AND [RejectionReason] IN (N'developer-rejected',N'invalid-reservation-deadline',N'local-conflict') AND [RejectedByAccountId] IS NOT NULL AND [RejectionActorKind]=N'User')))\nOR ([Status] IN (N'Confirmed',N'Expired') AND [Version]>1 AND [RejectedAtUtc] IS NULL AND [RejectionReason] IS NULL AND [RejectedByAccountId] IS NULL AND [RejectionActorKind] IS NULL AND [ConfirmedAtUtc] IS NOT NULL AND [ConfirmedByAccountId] IS NOT NULL AND [ConfirmedPrice] IS NOT NULL AND [ConfirmedCurrencyCode] IS NOT NULL AND [ReservationExpiresAtUtc] IS NOT NULL AND [ConfirmedResponseId] IS NOT NULL AND\n    [ConfirmedAtUtc]>=[RequestedAtUtc] AND [ConfirmedAtUtc]<[ResponseDueAtUtc] AND [ConfirmedPrice]>0 AND LEN([ConfirmedCurrencyCode])=3 AND [ConfirmedCurrencyCode] COLLATE Latin1_General_100_BIN2 NOT LIKE N'%[^A-Z]%' AND [ReservationExpiresAtUtc]>[ConfirmedAtUtc] AND\n    (([Status]=N'Confirmed' AND [IsActiveRequest]=1 AND [ExpiredAtUtc] IS NULL) OR ([Status]=N'Expired' AND [Version]>2 AND [IsActiveRequest]=0 AND [ExpiredAtUtc] IS NOT NULL AND [ExpiredAtUtc]>=[ReservationExpiresAtUtc])))) AND [CancelledAtUtc] IS NULL AND [CancelledByAccountId] IS NULL AND [CancelledFromStatus] IS NULL AND [CancellationReason] IS NULL) OR ([Status]=N'Cancelled' AND [Version]>1 AND [IsActiveRequest]=0 AND [CancelledAtUtc] IS NOT NULL AND [CancelledByAccountId] IS NOT NULL AND [CancelledFromStatus] IS NOT NULL AND [CancelledAtUtc]>=[RequestedAtUtc] AND ([CancellationReason] IS NULL OR LEN(LTRIM(RTRIM([CancellationReason])))>0) AND [RejectedAtUtc] IS NULL AND [RejectionReason] IS NULL AND [RejectedByAccountId] IS NULL AND [RejectionActorKind] IS NULL AND [ExpiredAtUtc] IS NULL AND\n(([CancelledFromStatus]=N'PendingReview' AND [CancelledAtUtc]<[ResponseDueAtUtc] AND [ConfirmedAtUtc] IS NULL AND [ConfirmedByAccountId] IS NULL AND [ConfirmedPrice] IS NULL AND [ConfirmedCurrencyCode] IS NULL AND [ReservationExpiresAtUtc] IS NULL AND [ConfirmedResponseId] IS NULL AND [LastExtendedAtUtc] IS NULL AND [LastExtensionId] IS NULL) OR\n ([CancelledFromStatus]=N'Confirmed' AND [Version]>2 AND [ConfirmedAtUtc] IS NOT NULL AND [ConfirmedByAccountId] IS NOT NULL AND [ConfirmedPrice] IS NOT NULL AND [ConfirmedPrice]>0 AND [ConfirmedCurrencyCode] IS NOT NULL AND LEN([ConfirmedCurrencyCode])=3 AND [ConfirmedCurrencyCode] COLLATE Latin1_General_100_BIN2 NOT LIKE N'%[^A-Z]%' AND [ReservationExpiresAtUtc] IS NOT NULL AND [ConfirmedResponseId] IS NOT NULL AND [ConfirmedAtUtc]>=[RequestedAtUtc] AND [ConfirmedAtUtc]<[ResponseDueAtUtc] AND [ReservationExpiresAtUtc]>[ConfirmedAtUtc] AND [CancelledAtUtc]>=[ConfirmedAtUtc] AND [CancelledAtUtc]<[ReservationExpiresAtUtc] AND ([LastExtendedAtUtc] IS NULL OR [CancelledAtUtc]>=[LastExtendedAtUtc]))))) AND [CompletedAtUtc] IS NULL AND [CompletedByAccountId] IS NULL AND [CompletedDealId] IS NULL) OR ([Status]=N'Completed' AND [Version]>2 AND [IsActiveRequest]=0 AND [CompletedAtUtc] IS NOT NULL AND [CompletedByAccountId] IS NOT NULL AND [CompletedDealId] IS NOT NULL AND [RejectedAtUtc] IS NULL AND [RejectionReason] IS NULL AND [RejectedByAccountId] IS NULL AND [RejectionActorKind] IS NULL AND [ExpiredAtUtc] IS NULL AND [CancelledAtUtc] IS NULL AND [CancelledByAccountId] IS NULL AND [CancelledFromStatus] IS NULL AND [CancellationReason] IS NULL AND [ConfirmedAtUtc] IS NOT NULL AND [ConfirmedByAccountId] IS NOT NULL AND [ConfirmedPrice] IS NOT NULL AND [ConfirmedPrice]>0 AND [ConfirmedCurrencyCode] IS NOT NULL AND LEN([ConfirmedCurrencyCode])=3 AND [ConfirmedCurrencyCode] COLLATE Latin1_General_100_BIN2 NOT LIKE N'%[^A-Z]%' AND [ReservationExpiresAtUtc] IS NOT NULL AND [ConfirmedResponseId] IS NOT NULL AND [ConfirmedAtUtc]>=[RequestedAtUtc] AND [ConfirmedAtUtc]<[ResponseDueAtUtc] AND [ReservationExpiresAtUtc]>[ConfirmedAtUtc] AND [CompletedAtUtc]>=[ConfirmedAtUtc] AND [CompletedAtUtc]<[ReservationExpiresAtUtc] AND ([LastExtendedAtUtc] IS NULL OR [CompletedAtUtc]>=[LastExtendedAtUtc]))");

            migrationBuilder.CreateIndex(
                name: "IX_BookingEvents_DealId_BookingId_CustomerId",
                schema: "Crm",
                table: "BookingEvents",
                columns: new[] { "DealId", "BookingId", "CustomerId" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_BookingEvents_Shape",
                schema: "Crm",
                table: "BookingEvents",
                sql: "((((([AtUtc]>'00010101' AND (\n  ([Kind]=N'Requested' AND [BookingVersion]=1 AND [Status]=N'PendingReview' AND [ActorKind]=N'User' AND [ActorAccountId] IS NOT NULL AND [ReasonCode] IS NULL AND [EffectiveCutoffUtc] IS NULL AND [DeveloperResponseId] IS NULL AND [ConfirmedPrice] IS NULL AND [ConfirmedCurrencyCode] IS NULL AND [ReservationExpiresAtUtc] IS NULL)\n  OR ([Kind]=N'ResponseTimedOut' AND [BookingVersion]>1 AND [Status]=N'Rejected' AND [ActorKind]=N'System' AND [ActorAccountId] IS NULL AND [ReasonCode] IS NOT NULL AND [ReasonCode]=N'response-timeout' AND [EffectiveCutoffUtc] IS NOT NULL AND [EffectiveCutoffUtc]>'00010101' AND [EffectiveCutoffUtc]<=[AtUtc] AND [DeveloperResponseId] IS NULL AND [ConfirmedPrice] IS NULL AND [ConfirmedCurrencyCode] IS NULL AND [ReservationExpiresAtUtc] IS NULL)\n  OR ([Kind]=N'Rejected' AND [BookingVersion]>1 AND [Status]=N'Rejected' AND [ActorKind]=N'User' AND [ActorAccountId] IS NOT NULL AND [ReasonCode] IS NOT NULL AND [ReasonCode] IN (N'developer-rejected',N'invalid-reservation-deadline',N'local-conflict') AND [EffectiveCutoffUtc] IS NULL AND [DeveloperResponseId] IS NOT NULL AND [ConfirmedPrice] IS NULL AND [ConfirmedCurrencyCode] IS NULL AND [ReservationExpiresAtUtc] IS NULL)\n  OR ([Kind] IN (N'Confirmed',N'Expired') AND [BookingVersion]>1 AND [ConfirmedPrice] IS NOT NULL AND [ConfirmedPrice]>0 AND [ConfirmedCurrencyCode] IS NOT NULL AND LEN([ConfirmedCurrencyCode])=3 AND [ConfirmedCurrencyCode] COLLATE Latin1_General_100_BIN2 NOT LIKE N'%[^A-Z]%' AND [ReservationExpiresAtUtc] IS NOT NULL AND\n    (([Kind]=N'Confirmed' AND [Status]=N'Confirmed' AND [ActorKind]=N'User' AND [ActorAccountId] IS NOT NULL AND [ReasonCode] IS NULL AND [EffectiveCutoffUtc] IS NULL AND [DeveloperResponseId] IS NOT NULL AND [ReservationExpiresAtUtc]>[AtUtc]) OR\n     ([Kind]=N'Expired' AND [BookingVersion]>2 AND [Status]=N'Expired' AND [ActorKind]=N'System' AND [ActorAccountId] IS NULL AND [ReasonCode] IS NOT NULL AND [ReasonCode]=N'reservation-expired' AND [EffectiveCutoffUtc] IS NOT NULL AND [EffectiveCutoffUtc]=[ReservationExpiresAtUtc] AND [EffectiveCutoffUtc]<=[AtUtc] AND [DeveloperResponseId] IS NULL)))) AND [PreviousReservationExpiresAtUtc] IS NULL AND [ExtensionId] IS NULL) OR ([AtUtc]>'00010101' AND [Kind]=N'Extended' AND [BookingVersion]>2 AND [Status]=N'Confirmed' AND [ActorKind]=N'User' AND [ActorAccountId] IS NOT NULL AND [ReasonCode] IS NULL AND [EffectiveCutoffUtc] IS NULL AND [DeveloperResponseId] IS NULL AND [ExtensionId] IS NOT NULL AND [PreviousReservationExpiresAtUtc] IS NOT NULL AND [PreviousReservationExpiresAtUtc]>[AtUtc] AND [ReservationExpiresAtUtc] IS NOT NULL AND [ReservationExpiresAtUtc]>[PreviousReservationExpiresAtUtc] AND [ConfirmedPrice] IS NOT NULL AND [ConfirmedPrice]>0 AND [ConfirmedCurrencyCode] IS NOT NULL AND LEN([ConfirmedCurrencyCode])=3 AND [ConfirmedCurrencyCode] COLLATE Latin1_General_100_BIN2 NOT LIKE N'%[^A-Z]%')) AND [CancelledFromStatus] IS NULL AND [CancellationReason] IS NULL) OR ([AtUtc]>'00010101' AND [Kind]=N'Cancelled' AND [Status]=N'Cancelled' AND [BookingVersion]>1 AND [ActorKind]=N'User' AND [ActorAccountId] IS NOT NULL AND [CancelledFromStatus] IS NOT NULL AND ([CancellationReason] IS NULL OR LEN(LTRIM(RTRIM([CancellationReason])))>0) AND [ReasonCode] IS NULL AND [EffectiveCutoffUtc] IS NULL AND [DeveloperResponseId] IS NULL AND [PreviousReservationExpiresAtUtc] IS NULL AND [ExtensionId] IS NULL AND\n(([CancelledFromStatus]=N'PendingReview' AND [ConfirmedPrice] IS NULL AND [ConfirmedCurrencyCode] IS NULL AND [ReservationExpiresAtUtc] IS NULL) OR\n ([CancelledFromStatus]=N'Confirmed' AND [BookingVersion]>2 AND [ConfirmedPrice] IS NOT NULL AND [ConfirmedPrice]>0 AND [ConfirmedCurrencyCode] IS NOT NULL AND LEN([ConfirmedCurrencyCode])=3 AND [ConfirmedCurrencyCode] COLLATE Latin1_General_100_BIN2 NOT LIKE N'%[^A-Z]%' AND [ReservationExpiresAtUtc] IS NOT NULL AND [AtUtc]<[ReservationExpiresAtUtc])))) AND [DealId] IS NULL) OR ([AtUtc]>'00010101' AND [Kind]=N'Completed' AND [Status]=N'Completed' AND [BookingVersion]>2 AND [ActorKind]=N'User' AND [ActorAccountId] IS NOT NULL AND [DealId] IS NOT NULL AND [ReasonCode] IS NULL AND [EffectiveCutoffUtc] IS NULL AND [DeveloperResponseId] IS NULL AND [PreviousReservationExpiresAtUtc] IS NULL AND [ExtensionId] IS NULL AND [CancelledFromStatus] IS NULL AND [CancellationReason] IS NULL AND [ConfirmedPrice] IS NOT NULL AND [ConfirmedPrice]>0 AND [ConfirmedCurrencyCode] IS NOT NULL AND LEN([ConfirmedCurrencyCode])=3 AND [ConfirmedCurrencyCode] COLLATE Latin1_General_100_BIN2 NOT LIKE N'%[^A-Z]%' AND [ReservationExpiresAtUtc] IS NOT NULL AND [AtUtc]<[ReservationExpiresAtUtc])");

            migrationBuilder.CreateIndex(
                name: "IX_DealBookingLinks_BookingId_CustomerId_UnitId",
                schema: "Sales",
                table: "DealBookingLinks",
                columns: new[] { "BookingId", "CustomerId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_DealBookingLinks_DealId_CustomerId_UnitId",
                schema: "Sales",
                table: "DealBookingLinks",
                columns: new[] { "DealId", "CustomerId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "UX_DealBookingLinks_Booking",
                schema: "Sales",
                table: "DealBookingLinks",
                column: "BookingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Deals_AdoptedByAccountId",
                schema: "Sales",
                table: "Deals",
                column: "AdoptedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Deals_AgreementId_DeveloperId",
                schema: "Sales",
                table: "Deals",
                columns: new[] { "AgreementId", "DeveloperId" });

            migrationBuilder.CreateIndex(
                name: "IX_Deals_CustomerId_AdoptedAtUtc_Id",
                schema: "Sales",
                table: "Deals",
                columns: new[] { "CustomerId", "AdoptedAtUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Deals_LeadId_CustomerId_ProjectId_AgreementId",
                schema: "Sales",
                table: "Deals",
                columns: new[] { "LeadId", "CustomerId", "ProjectId", "AgreementId" });

            migrationBuilder.CreateIndex(
                name: "IX_Deals_RuleId_AgreementId_DeveloperId",
                schema: "Sales",
                table: "Deals",
                columns: new[] { "RuleId", "AgreementId", "DeveloperId" });

            migrationBuilder.CreateIndex(
                name: "IX_Deals_UnitId_ProjectId",
                schema: "Sales",
                table: "Deals",
                columns: new[] { "UnitId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "UX_Deals_Unit",
                schema: "Sales",
                table: "Deals",
                column: "UnitId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_BookingEvents_DealBookingLinks_DealId_BookingId_CustomerId",
                schema: "Crm",
                table: "BookingEvents",
                columns: new[] { "DealId", "BookingId", "CustomerId" },
                principalSchema: "Sales",
                principalTable: "DealBookingLinks",
                principalColumns: new[] { "DealId", "BookingId", "CustomerId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Accounts_CompletedByAccountId",
                schema: "Crm",
                table: "Bookings",
                column: "CompletedByAccountId",
                principalSchema: "Identity",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_DealBookingLinks_CompletedDealId_Id_CustomerId_UnitId",
                schema: "Crm",
                table: "Bookings",
                columns: new[] { "CompletedDealId", "Id", "CustomerId", "UnitId" },
                principalSchema: "Sales",
                principalTable: "DealBookingLinks",
                principalColumns: new[] { "DealId", "BookingId", "CustomerId", "UnitId" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BookingEvents_DealBookingLinks_DealId_BookingId_CustomerId",
                schema: "Crm",
                table: "BookingEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Accounts_CompletedByAccountId",
                schema: "Crm",
                table: "Bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_DealBookingLinks_CompletedDealId_Id_CustomerId_UnitId",
                schema: "Crm",
                table: "Bookings");

            migrationBuilder.DropTable(
                name: "DealBookingLinks",
                schema: "Sales");

            migrationBuilder.DropTable(
                name: "Deals",
                schema: "Sales");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Leads_Id_CustomerId_ProjectId_MarketingAgreementId",
                schema: "Crm",
                table: "Leads");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_CommissionRules_Id_MarketingAgreementId_DeveloperId",
                schema: "Catalog",
                table: "CommissionRules");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_CompletedByAccountId",
                schema: "Crm",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_CompletedDealId_Id_CustomerId_UnitId",
                schema: "Crm",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_CurrentUnitReservation",
                schema: "Crm",
                table: "Bookings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Bookings_LastExtension",
                schema: "Crm",
                table: "Bookings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Bookings_State",
                schema: "Crm",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_BookingEvents_DealId_BookingId_CustomerId",
                schema: "Crm",
                table: "BookingEvents");

            migrationBuilder.DropCheckConstraint(
                name: "CK_BookingEvents_Shape",
                schema: "Crm",
                table: "BookingEvents");

            migrationBuilder.DropColumn(
                name: "CompletedAtUtc",
                schema: "Crm",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "CompletedByAccountId",
                schema: "Crm",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "CompletedDealId",
                schema: "Crm",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "DealId",
                schema: "Crm",
                table: "BookingEvents");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Bookings_LastExtension",
                schema: "Crm",
                table: "Bookings",
                sql: "([LastExtendedAtUtc] IS NULL AND [LastExtensionId] IS NULL) OR ([LastExtendedAtUtc] IS NOT NULL AND [LastExtensionId] IS NOT NULL AND ([Status] IN (N'Confirmed',N'Expired') OR ([Status]=N'Cancelled' AND [CancelledFromStatus]=N'Confirmed')) AND [Version]>2 AND [ConfirmedAtUtc] IS NOT NULL AND [ReservationExpiresAtUtc] IS NOT NULL AND [LastExtendedAtUtc]>=[ConfirmedAtUtc] AND [LastExtendedAtUtc]<[ReservationExpiresAtUtc])");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Bookings_State",
                schema: "Crm",
                table: "Bookings",
                sql: "((([Status]=N'PendingReview' AND [Version]=1 AND [IsActiveRequest]=1 AND [RejectedAtUtc] IS NULL AND [RejectionReason] IS NULL AND [RejectedByAccountId] IS NULL AND [RejectionActorKind] IS NULL AND [ConfirmedAtUtc] IS NULL AND [ConfirmedByAccountId] IS NULL AND [ConfirmedPrice] IS NULL AND [ConfirmedCurrencyCode] IS NULL AND [ReservationExpiresAtUtc] IS NULL AND [ConfirmedResponseId] IS NULL AND [ExpiredAtUtc] IS NULL)\nOR ([Status]=N'Rejected' AND [Version]>1 AND [IsActiveRequest]=0 AND [RejectedAtUtc] IS NOT NULL AND [RejectionReason] IS NOT NULL AND [RejectionActorKind] IS NOT NULL AND [ConfirmedAtUtc] IS NULL AND [ConfirmedByAccountId] IS NULL AND [ConfirmedPrice] IS NULL AND [ConfirmedCurrencyCode] IS NULL AND [ReservationExpiresAtUtc] IS NULL AND [ConfirmedResponseId] IS NULL AND [ExpiredAtUtc] IS NULL AND\n    (([RejectedAtUtc]>=[ResponseDueAtUtc] AND [RejectionReason]=N'response-timeout' AND [RejectedByAccountId] IS NULL AND [RejectionActorKind]=N'System') OR\n    ([RejectedAtUtc]>=[RequestedAtUtc] AND [RejectedAtUtc]<[ResponseDueAtUtc] AND [RejectionReason] IN (N'developer-rejected',N'invalid-reservation-deadline',N'local-conflict') AND [RejectedByAccountId] IS NOT NULL AND [RejectionActorKind]=N'User')))\nOR ([Status] IN (N'Confirmed',N'Expired') AND [Version]>1 AND [RejectedAtUtc] IS NULL AND [RejectionReason] IS NULL AND [RejectedByAccountId] IS NULL AND [RejectionActorKind] IS NULL AND [ConfirmedAtUtc] IS NOT NULL AND [ConfirmedByAccountId] IS NOT NULL AND [ConfirmedPrice] IS NOT NULL AND [ConfirmedCurrencyCode] IS NOT NULL AND [ReservationExpiresAtUtc] IS NOT NULL AND [ConfirmedResponseId] IS NOT NULL AND\n    [ConfirmedAtUtc]>=[RequestedAtUtc] AND [ConfirmedAtUtc]<[ResponseDueAtUtc] AND [ConfirmedPrice]>0 AND LEN([ConfirmedCurrencyCode])=3 AND [ConfirmedCurrencyCode] COLLATE Latin1_General_100_BIN2 NOT LIKE N'%[^A-Z]%' AND [ReservationExpiresAtUtc]>[ConfirmedAtUtc] AND\n    (([Status]=N'Confirmed' AND [IsActiveRequest]=1 AND [ExpiredAtUtc] IS NULL) OR ([Status]=N'Expired' AND [Version]>2 AND [IsActiveRequest]=0 AND [ExpiredAtUtc] IS NOT NULL AND [ExpiredAtUtc]>=[ReservationExpiresAtUtc])))) AND [CancelledAtUtc] IS NULL AND [CancelledByAccountId] IS NULL AND [CancelledFromStatus] IS NULL AND [CancellationReason] IS NULL) OR ([Status]=N'Cancelled' AND [Version]>1 AND [IsActiveRequest]=0 AND [CancelledAtUtc] IS NOT NULL AND [CancelledByAccountId] IS NOT NULL AND [CancelledFromStatus] IS NOT NULL AND [CancelledAtUtc]>=[RequestedAtUtc] AND ([CancellationReason] IS NULL OR LEN(LTRIM(RTRIM([CancellationReason])))>0) AND [RejectedAtUtc] IS NULL AND [RejectionReason] IS NULL AND [RejectedByAccountId] IS NULL AND [RejectionActorKind] IS NULL AND [ExpiredAtUtc] IS NULL AND\n(([CancelledFromStatus]=N'PendingReview' AND [CancelledAtUtc]<[ResponseDueAtUtc] AND [ConfirmedAtUtc] IS NULL AND [ConfirmedByAccountId] IS NULL AND [ConfirmedPrice] IS NULL AND [ConfirmedCurrencyCode] IS NULL AND [ReservationExpiresAtUtc] IS NULL AND [ConfirmedResponseId] IS NULL AND [LastExtendedAtUtc] IS NULL AND [LastExtensionId] IS NULL) OR\n ([CancelledFromStatus]=N'Confirmed' AND [Version]>2 AND [ConfirmedAtUtc] IS NOT NULL AND [ConfirmedByAccountId] IS NOT NULL AND [ConfirmedPrice] IS NOT NULL AND [ConfirmedPrice]>0 AND [ConfirmedCurrencyCode] IS NOT NULL AND LEN([ConfirmedCurrencyCode])=3 AND [ConfirmedCurrencyCode] COLLATE Latin1_General_100_BIN2 NOT LIKE N'%[^A-Z]%' AND [ReservationExpiresAtUtc] IS NOT NULL AND [ConfirmedResponseId] IS NOT NULL AND [ConfirmedAtUtc]>=[RequestedAtUtc] AND [ConfirmedAtUtc]<[ResponseDueAtUtc] AND [ReservationExpiresAtUtc]>[ConfirmedAtUtc] AND [CancelledAtUtc]>=[ConfirmedAtUtc] AND [CancelledAtUtc]<[ReservationExpiresAtUtc] AND ([LastExtendedAtUtc] IS NULL OR [CancelledAtUtc]>=[LastExtendedAtUtc]))))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_BookingEvents_Shape",
                schema: "Crm",
                table: "BookingEvents",
                sql: "((([AtUtc]>'00010101' AND (\n  ([Kind]=N'Requested' AND [BookingVersion]=1 AND [Status]=N'PendingReview' AND [ActorKind]=N'User' AND [ActorAccountId] IS NOT NULL AND [ReasonCode] IS NULL AND [EffectiveCutoffUtc] IS NULL AND [DeveloperResponseId] IS NULL AND [ConfirmedPrice] IS NULL AND [ConfirmedCurrencyCode] IS NULL AND [ReservationExpiresAtUtc] IS NULL)\n  OR ([Kind]=N'ResponseTimedOut' AND [BookingVersion]>1 AND [Status]=N'Rejected' AND [ActorKind]=N'System' AND [ActorAccountId] IS NULL AND [ReasonCode] IS NOT NULL AND [ReasonCode]=N'response-timeout' AND [EffectiveCutoffUtc] IS NOT NULL AND [EffectiveCutoffUtc]>'00010101' AND [EffectiveCutoffUtc]<=[AtUtc] AND [DeveloperResponseId] IS NULL AND [ConfirmedPrice] IS NULL AND [ConfirmedCurrencyCode] IS NULL AND [ReservationExpiresAtUtc] IS NULL)\n  OR ([Kind]=N'Rejected' AND [BookingVersion]>1 AND [Status]=N'Rejected' AND [ActorKind]=N'User' AND [ActorAccountId] IS NOT NULL AND [ReasonCode] IS NOT NULL AND [ReasonCode] IN (N'developer-rejected',N'invalid-reservation-deadline',N'local-conflict') AND [EffectiveCutoffUtc] IS NULL AND [DeveloperResponseId] IS NOT NULL AND [ConfirmedPrice] IS NULL AND [ConfirmedCurrencyCode] IS NULL AND [ReservationExpiresAtUtc] IS NULL)\n  OR ([Kind] IN (N'Confirmed',N'Expired') AND [BookingVersion]>1 AND [ConfirmedPrice] IS NOT NULL AND [ConfirmedPrice]>0 AND [ConfirmedCurrencyCode] IS NOT NULL AND LEN([ConfirmedCurrencyCode])=3 AND [ConfirmedCurrencyCode] COLLATE Latin1_General_100_BIN2 NOT LIKE N'%[^A-Z]%' AND [ReservationExpiresAtUtc] IS NOT NULL AND\n    (([Kind]=N'Confirmed' AND [Status]=N'Confirmed' AND [ActorKind]=N'User' AND [ActorAccountId] IS NOT NULL AND [ReasonCode] IS NULL AND [EffectiveCutoffUtc] IS NULL AND [DeveloperResponseId] IS NOT NULL AND [ReservationExpiresAtUtc]>[AtUtc]) OR\n     ([Kind]=N'Expired' AND [BookingVersion]>2 AND [Status]=N'Expired' AND [ActorKind]=N'System' AND [ActorAccountId] IS NULL AND [ReasonCode] IS NOT NULL AND [ReasonCode]=N'reservation-expired' AND [EffectiveCutoffUtc] IS NOT NULL AND [EffectiveCutoffUtc]=[ReservationExpiresAtUtc] AND [EffectiveCutoffUtc]<=[AtUtc] AND [DeveloperResponseId] IS NULL)))) AND [PreviousReservationExpiresAtUtc] IS NULL AND [ExtensionId] IS NULL) OR ([AtUtc]>'00010101' AND [Kind]=N'Extended' AND [BookingVersion]>2 AND [Status]=N'Confirmed' AND [ActorKind]=N'User' AND [ActorAccountId] IS NOT NULL AND [ReasonCode] IS NULL AND [EffectiveCutoffUtc] IS NULL AND [DeveloperResponseId] IS NULL AND [ExtensionId] IS NOT NULL AND [PreviousReservationExpiresAtUtc] IS NOT NULL AND [PreviousReservationExpiresAtUtc]>[AtUtc] AND [ReservationExpiresAtUtc] IS NOT NULL AND [ReservationExpiresAtUtc]>[PreviousReservationExpiresAtUtc] AND [ConfirmedPrice] IS NOT NULL AND [ConfirmedPrice]>0 AND [ConfirmedCurrencyCode] IS NOT NULL AND LEN([ConfirmedCurrencyCode])=3 AND [ConfirmedCurrencyCode] COLLATE Latin1_General_100_BIN2 NOT LIKE N'%[^A-Z]%')) AND [CancelledFromStatus] IS NULL AND [CancellationReason] IS NULL) OR ([AtUtc]>'00010101' AND [Kind]=N'Cancelled' AND [Status]=N'Cancelled' AND [BookingVersion]>1 AND [ActorKind]=N'User' AND [ActorAccountId] IS NOT NULL AND [CancelledFromStatus] IS NOT NULL AND ([CancellationReason] IS NULL OR LEN(LTRIM(RTRIM([CancellationReason])))>0) AND [ReasonCode] IS NULL AND [EffectiveCutoffUtc] IS NULL AND [DeveloperResponseId] IS NULL AND [PreviousReservationExpiresAtUtc] IS NULL AND [ExtensionId] IS NULL AND\n(([CancelledFromStatus]=N'PendingReview' AND [ConfirmedPrice] IS NULL AND [ConfirmedCurrencyCode] IS NULL AND [ReservationExpiresAtUtc] IS NULL) OR\n ([CancelledFromStatus]=N'Confirmed' AND [BookingVersion]>2 AND [ConfirmedPrice] IS NOT NULL AND [ConfirmedPrice]>0 AND [ConfirmedCurrencyCode] IS NOT NULL AND LEN([ConfirmedCurrencyCode])=3 AND [ConfirmedCurrencyCode] COLLATE Latin1_General_100_BIN2 NOT LIKE N'%[^A-Z]%' AND [ReservationExpiresAtUtc] IS NOT NULL AND [AtUtc]<[ReservationExpiresAtUtc])))");
        }
    }
}
