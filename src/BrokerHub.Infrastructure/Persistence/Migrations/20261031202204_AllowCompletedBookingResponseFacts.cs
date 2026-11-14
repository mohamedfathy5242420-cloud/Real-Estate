using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrokerHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AllowCompletedBookingResponseFacts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_BookingResponses_Decision",
                schema: "Crm",
                table: "BookingDeveloperResponses");

            migrationBuilder.AddCheckConstraint(
                name: "CK_BookingResponses_Decision",
                schema: "Crm",
                table: "BookingDeveloperResponses",
                sql: "([Decision]=N'Confirmed' AND [ResponseKind]=N'Confirmed' AND [StatusAfter]=N'Confirmed' AND [BookingVersionAfter]-[RequestedVersion]=1 AND [OfferedDeadlineUtc]>[RecordedAtUtc]) OR\n([Decision]=N'DeveloperRejected' AND [ResponseKind]=N'Rejected' AND [StatusAfter]=N'Rejected' AND [BookingVersionAfter]-[RequestedVersion]=1) OR\n([Decision]=N'InvalidDeadline' AND [ResponseKind]=N'Confirmed' AND [StatusAfter]=N'Rejected' AND [BookingVersionAfter]-[RequestedVersion]=1 AND [OfferedDeadlineUtc]<=[RecordedAtUtc]) OR\n([Decision]=N'LocalConflict' AND [ResponseKind]=N'Confirmed' AND [StatusAfter]=N'Rejected' AND [BookingVersionAfter]-[RequestedVersion]=1 AND [OfferedDeadlineUtc]>[RecordedAtUtc]) OR\n([Decision]=N'ResponseTimeout' AND [StatusAfter]=N'Rejected' AND [BookingVersionAfter]-[RequestedVersion]=1) OR\n([Decision]=N'ReservationExpired' AND [StatusAfter]=N'Expired' AND [BookingVersionAfter]-[RequestedVersion]=1) OR\n([Decision]=N'StateUnchanged' AND [StatusAfter] IN (N'Confirmed',N'Rejected',N'Expired',N'Cancelled',N'Completed') AND [BookingVersionAfter]=[RequestedVersion])");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_BookingResponses_Decision",
                schema: "Crm",
                table: "BookingDeveloperResponses");

            migrationBuilder.AddCheckConstraint(
                name: "CK_BookingResponses_Decision",
                schema: "Crm",
                table: "BookingDeveloperResponses",
                sql: "([Decision]=N'Confirmed' AND [ResponseKind]=N'Confirmed' AND [StatusAfter]=N'Confirmed' AND [BookingVersionAfter]-[RequestedVersion]=1 AND [OfferedDeadlineUtc]>[RecordedAtUtc]) OR\n([Decision]=N'DeveloperRejected' AND [ResponseKind]=N'Rejected' AND [StatusAfter]=N'Rejected' AND [BookingVersionAfter]-[RequestedVersion]=1) OR\n([Decision]=N'InvalidDeadline' AND [ResponseKind]=N'Confirmed' AND [StatusAfter]=N'Rejected' AND [BookingVersionAfter]-[RequestedVersion]=1 AND [OfferedDeadlineUtc]<=[RecordedAtUtc]) OR\n([Decision]=N'LocalConflict' AND [ResponseKind]=N'Confirmed' AND [StatusAfter]=N'Rejected' AND [BookingVersionAfter]-[RequestedVersion]=1 AND [OfferedDeadlineUtc]>[RecordedAtUtc]) OR\n([Decision]=N'ResponseTimeout' AND [StatusAfter]=N'Rejected' AND [BookingVersionAfter]-[RequestedVersion]=1) OR\n([Decision]=N'ReservationExpired' AND [StatusAfter]=N'Expired' AND [BookingVersionAfter]-[RequestedVersion]=1) OR\n([Decision]=N'StateUnchanged' AND [StatusAfter] IN (N'Confirmed',N'Rejected',N'Expired',N'Cancelled') AND [BookingVersionAfter]=[RequestedVersion])");
        }
    }
}
