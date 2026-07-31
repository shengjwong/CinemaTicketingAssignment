using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CinemaTicketingAssignment.Migrations
{
    /// <inheritdoc />
    public partial class SeedCatalogData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HallName",
                table: "Showtimes");

            migrationBuilder.AddColumn<string>(
                name: "CinemaHallId",
                table: "Showtimes",
                type: "nvarchar(10)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDateTime",
                table: "Showtimes",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<decimal>(
                name: "TicketPrice",
                table: "Showtimes",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "AgeClassification",
                table: "Movies",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Language",
                table: "Movies",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PosterImagePath",
                table: "Movies",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReleaseDate",
                table: "Movies",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Movies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Synopsis",
                table: "Movies",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "CinemaHalls",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    HallName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    HallType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SeatCapacity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CinemaHalls", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "CinemaHalls",
                columns: new[] { "Id", "HallName", "HallType", "SeatCapacity" },
                values: new object[,]
                {
                    { "HALL001", "Deluxe", "Standard Deluxe", 120 },
                    { "HALL002", "4DX", "Motion & Effects", 80 },
                    { "HALL003", "Dolby D-BOX", "Dolby Atmos + D-BOX Seats", 100 },
                    { "HALL004", "Samsung Onyx", "LED Cinema Screen", 150 },
                    { "HALL005", "70MM IMAX", "IMAX", 200 }
                });

            migrationBuilder.InsertData(
                table: "Movies",
                columns: new[] { "Id", "AgeClassification", "Genre", "Language", "PosterImagePath", "ReleaseDate", "Status", "Synopsis", "Title" },
                values: new object[,]
                {
                    { "MOV001", "18", "Action", "English", null, new DateTime(2008, 7, 18, 0, 0, 0, 0, DateTimeKind.Unspecified), 0, "Batman faces the Joker in a battle for Gotham's soul.", "The Dark Knight" },
                    { "MOV002", "PG13", "Sci-Fi", "English", null, new DateTime(2011, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), 0, "A thief who steals secrets through dream-sharing takes on one last job.", "Inception" },
                    { "MOV003", "PG13", "Action", "English", null, new DateTime(2026, 7, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, "Spiderman returns for a brand new adventure in New York City.", "Spiderman Brand New Day" },
                    { "MOV004", "PG13", "Adventure", "English", null, new DateTime(2026, 7, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, "An epic retelling of Homer's legendary voyage.", "The Odyssey" },
                    { "MOV005", "PG13", "Sci-Fi", "English", null, new DateTime(2014, 11, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), 0, "A team of explorers travel through a wormhole in space to save humanity.", "Interstellar" }
                });

            migrationBuilder.InsertData(
                table: "Showtimes",
                columns: new[] { "Id", "CinemaHallId", "EndDateTime", "MovieId", "StartDateTime", "TicketPrice" },
                values: new object[,]
                {
                    { "SHOW001", "HALL001", new DateTime(2026, 8, 1, 13, 17, 0, 0, DateTimeKind.Unspecified), "MOV001", new DateTime(2026, 8, 1, 10, 45, 0, 0, DateTimeKind.Unspecified), 25.00m },
                    { "SHOW002", "HALL002", new DateTime(2026, 8, 1, 16, 42, 0, 0, DateTimeKind.Unspecified), "MOV001", new DateTime(2026, 8, 1, 14, 10, 0, 0, DateTimeKind.Unspecified), 35.00m },
                    { "SHOW003", "HALL003", new DateTime(2026, 8, 1, 13, 13, 0, 0, DateTimeKind.Unspecified), "MOV002", new DateTime(2026, 8, 1, 10, 45, 0, 0, DateTimeKind.Unspecified), 30.00m },
                    { "SHOW004", "HALL004", new DateTime(2026, 8, 1, 16, 38, 0, 0, DateTimeKind.Unspecified), "MOV002", new DateTime(2026, 8, 1, 14, 10, 0, 0, DateTimeKind.Unspecified), 40.00m },
                    { "SHOW005", "HALL005", new DateTime(2026, 8, 1, 12, 55, 0, 0, DateTimeKind.Unspecified), "MOV003", new DateTime(2026, 8, 1, 10, 45, 0, 0, DateTimeKind.Unspecified), 45.00m },
                    { "SHOW006", "HALL001", new DateTime(2026, 8, 1, 16, 20, 0, 0, DateTimeKind.Unspecified), "MOV003", new DateTime(2026, 8, 1, 14, 10, 0, 0, DateTimeKind.Unspecified), 25.00m },
                    { "SHOW007", "HALL002", new DateTime(2026, 8, 1, 13, 5, 0, 0, DateTimeKind.Unspecified), "MOV004", new DateTime(2026, 8, 1, 10, 45, 0, 0, DateTimeKind.Unspecified), 35.00m },
                    { "SHOW008", "HALL003", new DateTime(2026, 8, 1, 16, 30, 0, 0, DateTimeKind.Unspecified), "MOV004", new DateTime(2026, 8, 1, 14, 10, 0, 0, DateTimeKind.Unspecified), 30.00m },
                    { "SHOW009", "HALL004", new DateTime(2026, 8, 1, 13, 34, 0, 0, DateTimeKind.Unspecified), "MOV005", new DateTime(2026, 8, 1, 10, 45, 0, 0, DateTimeKind.Unspecified), 40.00m },
                    { "SHOW010", "HALL005", new DateTime(2026, 8, 1, 16, 59, 0, 0, DateTimeKind.Unspecified), "MOV005", new DateTime(2026, 8, 1, 14, 10, 0, 0, DateTimeKind.Unspecified), 45.00m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Showtimes_CinemaHallId",
                table: "Showtimes",
                column: "CinemaHallId");

            migrationBuilder.AddForeignKey(
                name: "FK_Showtimes_CinemaHalls_CinemaHallId",
                table: "Showtimes",
                column: "CinemaHallId",
                principalTable: "CinemaHalls",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Showtimes_CinemaHalls_CinemaHallId",
                table: "Showtimes");

            migrationBuilder.DropTable(
                name: "CinemaHalls");

            migrationBuilder.DropIndex(
                name: "IX_Showtimes_CinemaHallId",
                table: "Showtimes");

            migrationBuilder.DeleteData(
                table: "Showtimes",
                keyColumn: "Id",
                keyValue: "SHOW001");

            migrationBuilder.DeleteData(
                table: "Showtimes",
                keyColumn: "Id",
                keyValue: "SHOW002");

            migrationBuilder.DeleteData(
                table: "Showtimes",
                keyColumn: "Id",
                keyValue: "SHOW003");

            migrationBuilder.DeleteData(
                table: "Showtimes",
                keyColumn: "Id",
                keyValue: "SHOW004");

            migrationBuilder.DeleteData(
                table: "Showtimes",
                keyColumn: "Id",
                keyValue: "SHOW005");

            migrationBuilder.DeleteData(
                table: "Showtimes",
                keyColumn: "Id",
                keyValue: "SHOW006");

            migrationBuilder.DeleteData(
                table: "Showtimes",
                keyColumn: "Id",
                keyValue: "SHOW007");

            migrationBuilder.DeleteData(
                table: "Showtimes",
                keyColumn: "Id",
                keyValue: "SHOW008");

            migrationBuilder.DeleteData(
                table: "Showtimes",
                keyColumn: "Id",
                keyValue: "SHOW009");

            migrationBuilder.DeleteData(
                table: "Showtimes",
                keyColumn: "Id",
                keyValue: "SHOW010");

            migrationBuilder.DeleteData(
                table: "Movies",
                keyColumn: "Id",
                keyValue: "MOV001");

            migrationBuilder.DeleteData(
                table: "Movies",
                keyColumn: "Id",
                keyValue: "MOV002");

            migrationBuilder.DeleteData(
                table: "Movies",
                keyColumn: "Id",
                keyValue: "MOV003");

            migrationBuilder.DeleteData(
                table: "Movies",
                keyColumn: "Id",
                keyValue: "MOV004");

            migrationBuilder.DeleteData(
                table: "Movies",
                keyColumn: "Id",
                keyValue: "MOV005");

            migrationBuilder.DropColumn(
                name: "CinemaHallId",
                table: "Showtimes");

            migrationBuilder.DropColumn(
                name: "EndDateTime",
                table: "Showtimes");

            migrationBuilder.DropColumn(
                name: "TicketPrice",
                table: "Showtimes");

            migrationBuilder.DropColumn(
                name: "AgeClassification",
                table: "Movies");

            migrationBuilder.DropColumn(
                name: "Language",
                table: "Movies");

            migrationBuilder.DropColumn(
                name: "PosterImagePath",
                table: "Movies");

            migrationBuilder.DropColumn(
                name: "ReleaseDate",
                table: "Movies");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Movies");

            migrationBuilder.DropColumn(
                name: "Synopsis",
                table: "Movies");

            migrationBuilder.AddColumn<string>(
                name: "HallName",
                table: "Showtimes",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }
    }
}
