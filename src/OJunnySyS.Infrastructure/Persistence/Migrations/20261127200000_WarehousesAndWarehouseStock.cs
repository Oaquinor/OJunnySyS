using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OJunnySyS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class WarehousesAndWarehouseStock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "WarehouseId",
                table: "Sales",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "WarehouseId",
                table: "PurchaseOrders",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "WarehouseId",
                table: "PosRegisters",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "WarehouseId",
                table: "InventoryMovements",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "Warehouses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    Code = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Revision = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetime", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    TenantId = table.Column<Guid>(type: "char(36)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Warehouses", x => x.Id);
                    table.UniqueConstraint("AK_Warehouses_TenantId_Code", x => new { x.TenantId, x.Code });
                    table.UniqueConstraint("AK_Warehouses_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_Warehouses_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "WarehouseStocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "char(36)", nullable: false),
                    ProductId = table.Column<Guid>(type: "char(36)", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Revision = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetime", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    TenantId = table.Column<Guid>(type: "char(36)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseStocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarehouseStocks_Products_TenantId_ProductId",
                        columns: x => new { x.TenantId, x.ProductId },
                        principalTable: "Products",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarehouseStocks_Warehouses_TenantId_WarehouseId",
                        columns: x => new { x.TenantId, x.WarehouseId },
                        principalTable: "Warehouses",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.Sql("INSERT INTO `Warehouses` (`Id`, `Code`, `Name`, `IsActive`, `Revision`, `CreatedAtUtc`, `UpdatedAtUtc`, `TenantId`) SELECT UUID(), 'MAIN', 'Almacén principal', 1, 0, UTC_TIMESTAMP(), NULL, `Id` FROM `Tenants`;");
            migrationBuilder.Sql("UPDATE `PosRegisters` r INNER JOIN `Warehouses` w ON w.`TenantId` = r.`TenantId` AND w.`Code` = 'MAIN' SET r.`WarehouseId` = w.`Id`;");
            migrationBuilder.Sql("UPDATE `Sales` s INNER JOIN `Warehouses` w ON w.`TenantId` = s.`TenantId` AND w.`Code` = 'MAIN' SET s.`WarehouseId` = w.`Id`;");
            migrationBuilder.Sql("UPDATE `PurchaseOrders` p INNER JOIN `Warehouses` w ON w.`TenantId` = p.`TenantId` AND w.`Code` = 'MAIN' SET p.`WarehouseId` = w.`Id`;");
            migrationBuilder.Sql("UPDATE `InventoryMovements` m INNER JOIN `Warehouses` w ON w.`TenantId` = m.`TenantId` AND w.`Code` = 'MAIN' SET m.`WarehouseId` = w.`Id`;");
            migrationBuilder.Sql("INSERT INTO `WarehouseStocks` (`Id`, `WarehouseId`, `ProductId`, `Quantity`, `Revision`, `CreatedAtUtc`, `UpdatedAtUtc`, `TenantId`) SELECT UUID(), w.`Id`, p.`Id`, p.`StockOnHand`, 0, UTC_TIMESTAMP(), NULL, p.`TenantId` FROM `Products` p INNER JOIN `Warehouses` w ON w.`TenantId` = p.`TenantId` AND w.`Code` = 'MAIN';");
            migrationBuilder.Sql("ALTER TABLE `Sales` ALTER COLUMN `WarehouseId` DROP DEFAULT;");
            migrationBuilder.Sql("ALTER TABLE `PurchaseOrders` ALTER COLUMN `WarehouseId` DROP DEFAULT;");
            migrationBuilder.Sql("ALTER TABLE `PosRegisters` ALTER COLUMN `WarehouseId` DROP DEFAULT;");
            migrationBuilder.Sql("ALTER TABLE `InventoryMovements` ALTER COLUMN `WarehouseId` DROP DEFAULT;");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_TenantId_WarehouseId",
                table: "Sales",
                columns: new[] { "TenantId", "WarehouseId" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_TenantId_WarehouseId",
                table: "PurchaseOrders",
                columns: new[] { "TenantId", "WarehouseId" });

            migrationBuilder.CreateIndex(
                name: "IX_PosRegisters_TenantId_WarehouseId",
                table: "PosRegisters",
                columns: new[] { "TenantId", "WarehouseId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_TenantId_WarehouseId_ProductId_CreatedAtU~",
                table: "InventoryMovements",
                columns: new[] { "TenantId", "WarehouseId", "ProductId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_TenantId_Name",
                table: "Warehouses",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseStocks_TenantId_ProductId",
                table: "WarehouseStocks",
                columns: new[] { "TenantId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseStocks_TenantId_WarehouseId_ProductId",
                table: "WarehouseStocks",
                columns: new[] { "TenantId", "WarehouseId", "ProductId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryMovements_Warehouses_TenantId_WarehouseId",
                table: "InventoryMovements",
                columns: new[] { "TenantId", "WarehouseId" },
                principalTable: "Warehouses",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PosRegisters_Warehouses_TenantId_WarehouseId",
                table: "PosRegisters",
                columns: new[] { "TenantId", "WarehouseId" },
                principalTable: "Warehouses",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_Warehouses_TenantId_WarehouseId",
                table: "PurchaseOrders",
                columns: new[] { "TenantId", "WarehouseId" },
                principalTable: "Warehouses",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Sales_Warehouses_TenantId_WarehouseId",
                table: "Sales",
                columns: new[] { "TenantId", "WarehouseId" },
                principalTable: "Warehouses",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryMovements_Warehouses_TenantId_WarehouseId",
                table: "InventoryMovements");

            migrationBuilder.DropForeignKey(
                name: "FK_PosRegisters_Warehouses_TenantId_WarehouseId",
                table: "PosRegisters");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_Warehouses_TenantId_WarehouseId",
                table: "PurchaseOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_Sales_Warehouses_TenantId_WarehouseId",
                table: "Sales");

            migrationBuilder.DropTable(
                name: "WarehouseStocks");

            migrationBuilder.DropTable(
                name: "Warehouses");

            migrationBuilder.DropIndex(
                name: "IX_Sales_TenantId_WarehouseId",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_TenantId_WarehouseId",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_PosRegisters_TenantId_WarehouseId",
                table: "PosRegisters");

            migrationBuilder.DropIndex(
                name: "IX_InventoryMovements_TenantId_WarehouseId_ProductId_CreatedAtU~",
                table: "InventoryMovements");

            migrationBuilder.DropColumn(
                name: "WarehouseId",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "WarehouseId",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "WarehouseId",
                table: "PosRegisters");

            migrationBuilder.DropColumn(
                name: "WarehouseId",
                table: "InventoryMovements");

        }
    }
}
