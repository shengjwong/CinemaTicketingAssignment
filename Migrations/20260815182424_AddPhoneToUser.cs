using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinemaTicketingAssignment.Migrations
{
    /// <inheritdoc />
    public partial class AddPhoneToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "Users",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Discriminator", "Email", "FailedLoginAttempts", "Hash", "LockoutEnd", "Name", "Phone", "ResetToken", "ResetTokenExpiry" },
                values: new object[] { "ADM001", "Admin", "admin@cinema.com", 0, "Admin@123", null, "System Admin", null, null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: "ADM001");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "Users");
        }
    }
}
