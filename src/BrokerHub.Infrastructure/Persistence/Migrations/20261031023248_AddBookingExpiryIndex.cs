using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrokerHub.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingExpiryIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Bookings_ConfirmedReservationDue",
                schema: "Crm",
                table: "Bookings",
                columns: new[] { "Status", "ReservationExpiresAtUtc", "Id" },
                filter: "[Status] = N'Confirmed'")
                .Annotation("SqlServer:Include", new[] { "CustomerId", "UnitId", "Version" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bookings_ConfirmedReservationDue",
                schema: "Crm",
                table: "Bookings");
        }
    }
}
