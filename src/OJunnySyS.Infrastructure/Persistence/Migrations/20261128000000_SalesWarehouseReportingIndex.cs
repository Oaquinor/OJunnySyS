using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OJunnySyS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SalesWarehouseReportingIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Sales_TenantId_WarehouseId_CreatedAtUtc",
                table: "Sales",
                columns: new[] { "TenantId", "WarehouseId", "CreatedAtUtc" });

            migrationBuilder.DropIndex(
                name: "IX_Sales_TenantId_WarehouseId",
                table: "Sales");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Sales_TenantId_WarehouseId",
                table: "Sales",
                columns: new[] { "TenantId", "WarehouseId" });

            migrationBuilder.DropIndex(
                name: "IX_Sales_TenantId_WarehouseId_CreatedAtUtc",
                table: "Sales");
        }
    }
}
