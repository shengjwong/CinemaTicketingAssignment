using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinemaTicketingAssignment.Migrations;

public partial class AddFoodOrderPaymentsAndOrderManagement : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "FoodOrders",
            columns: table => new
            {
                Id = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                OrderDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                PaymentStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                PaymentReference = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                UserId = table.Column<string>(type: "nvarchar(10)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_FoodOrders", x => x.Id);
                table.ForeignKey(
                    name: "FK_FoodOrders_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "FoodOrderItems",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                FoodOrderId = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                ConcessionId = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                ItemName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                Quantity = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_FoodOrderItems", x => x.Id);
                table.ForeignKey(
                    name: "FK_FoodOrderItems_FoodOrders_FoodOrderId",
                    column: x => x.FoodOrderId,
                    principalTable: "FoodOrders",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(name: "IX_FoodOrders_UserId", table: "FoodOrders", column: "UserId");
        migrationBuilder.CreateIndex(name: "IX_FoodOrderItems_FoodOrderId", table: "FoodOrderItems", column: "FoodOrderId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "FoodOrderItems");
        migrationBuilder.DropTable(name: "FoodOrders");
    }
}
