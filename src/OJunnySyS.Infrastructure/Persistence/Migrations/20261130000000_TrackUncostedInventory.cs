using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OJunnySyS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TrackUncostedInventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UncostedQuantity",
                table: "SaleLine",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UncostedQuantity",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("UPDATE `Products` SET `UncostedQuantity` = `StockOnHand`");
            migrationBuilder.Sql("UPDATE `SaleLine` SET `UncostedQuantity` = `Quantity`");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UncostedQuantity",
                table: "SaleLine");

            migrationBuilder.DropColumn(
                name: "UncostedQuantity",
                table: "Products");
        }
    }
}
