using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinemaTicketingAssignment.Migrations;

public partial class AddSeatReservationEngine : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "PaymentReference", table: "Bookings", type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>(name: "PaymentStatus", table: "Bookings", type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "Paid");

        migrationBuilder.CreateTable(
            name: "BookingSeats",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                BookingId = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                ShowtimeId = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                SeatCode = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_BookingSeats", x => x.Id);
                table.ForeignKey(name: "FK_BookingSeats_Bookings_BookingId", column: x => x.BookingId, principalTable: "Bookings", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "SeatHolds",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                ShowtimeId = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                SeatCode = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                UserId = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_SeatHolds", x => x.Id));

        migrationBuilder.CreateIndex(name: "IX_BookingSeats_BookingId", table: "BookingSeats", column: "BookingId");
        migrationBuilder.CreateIndex(name: "IX_BookingSeats_ShowtimeId_SeatCode", table: "BookingSeats", columns: new[] { "ShowtimeId", "SeatCode" }, unique: true);
        migrationBuilder.CreateIndex(name: "IX_SeatHolds_ShowtimeId_SeatCode", table: "SeatHolds", columns: new[] { "ShowtimeId", "SeatCode" }, unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "BookingSeats");
        migrationBuilder.DropTable(name: "SeatHolds");
        migrationBuilder.DropColumn(name: "PaymentReference", table: "Bookings");
        migrationBuilder.DropColumn(name: "PaymentStatus", table: "Bookings");
    }
}
