using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CinemaTicketingAssignment.Migrations
{
    /// <inheritdoc />
    public partial class AddStudent4Features : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Reviews",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MovieId = table.Column<string>(type: "nvarchar(10)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(10)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reviews_Movies_MovieId",
                        column: x => x.MovieId,
                        principalTable: "Movies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Reviews_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Concessions",
                columns: new[] { "Id", "ItemName", "Price" },
                values: new object[,]
                {
                    { "CON001", "Caramel Popcorn Combo (Large Popcorn + Drink)", 18.50m },
                    { "CON002", "Royale Hotdog Combo", 16.00m },
                    { "CON003", "Cheesy Nachos Dip", 14.50m },
                    { "CON004", "Fountain Soft Drink (Regular)", 7.00m }
                });

            migrationBuilder.InsertData(
                table: "Reviews",
                columns: new[] { "Id", "Comment", "CreatedAt", "MovieId", "Rating", "UserId" },
                values: new object[,]
                {
                    { "REV001", "An absolute masterpiece of cinematic superhero drama!", new DateTime(2026, 8, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), "MOV001", 5, "ADM001" },
                    { "REV002", "Mind-bending visual effects and incredible score.", new DateTime(2026, 8, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), "MOV002", 5, "ADM001" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_MovieId",
                table: "Reviews",
                column: "MovieId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_UserId",
                table: "Reviews",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Reviews");

            migrationBuilder.DeleteData(
                table: "Concessions",
                keyColumn: "Id",
                keyValue: "CON001");

            migrationBuilder.DeleteData(
                table: "Concessions",
                keyColumn: "Id",
                keyValue: "CON002");

            migrationBuilder.DeleteData(
                table: "Concessions",
                keyColumn: "Id",
                keyValue: "CON003");

            migrationBuilder.DeleteData(
                table: "Concessions",
                keyColumn: "Id",
                keyValue: "CON004");
        }
    }
}
