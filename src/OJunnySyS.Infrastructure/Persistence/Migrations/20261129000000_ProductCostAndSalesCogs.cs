using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OJunnySyS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ProductCostAndSalesCogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "UnitCost",
                table: "SaleLine",
                type: "decimal(14,4)",
                precision: 14,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitCost",
                table: "Products",
                type: "decimal(14,4)",
                precision: 14,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UnitCost",
                table: "SaleLine");

            migrationBuilder.DropColumn(
                name: "UnitCost",
                table: "Products");

        }
    }
}
