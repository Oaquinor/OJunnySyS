using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OJunnySyS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InventoryAdjustments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InventoryAdjustments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    Number = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    IdempotencyKey = table.Column<Guid>(type: "char(36)", nullable: false),
                    RequestFingerprint = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    ProductId = table.Column<Guid>(type: "char(36)", nullable: false),
                    ProductSku = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    ProductName = table.Column<string>(type: "varchar(160)", maxLength: 160, nullable: false),
                    WarehouseId = table.Column<Guid>(type: "char(36)", nullable: false),
                    WarehouseName = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false),
                    ActorUserId = table.Column<Guid>(type: "char(36)", nullable: false),
                    ActorName = table.Column<string>(type: "varchar(160)", maxLength: 160, nullable: false),
                    QuantityDelta = table.Column<int>(type: "int", nullable: false),
                    BalanceBefore = table.Column<int>(type: "int", nullable: false),
                    BalanceAfter = table.Column<int>(type: "int", nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(14,4)", precision: 14, scale: 4, nullable: false),
                    ValuationAmount = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    Reason = table.Column<string>(type: "varchar(240)", maxLength: 240, nullable: false),
                    CounterAccountId = table.Column<Guid>(type: "char(36)", nullable: false),
                    CounterAccountCode = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    CounterAccountName = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false),
                    JournalEntryNumber = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetime", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    TenantId = table.Column<Guid>(type: "char(36)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryAdjustments", x => x.Id);
                    table.UniqueConstraint("AK_InventoryAdjustments_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_InventoryAdjustments_ChartAccounts_TenantId_CounterAccountId",
                        columns: x => new { x.TenantId, x.CounterAccountId },
                        principalTable: "ChartAccounts",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryAdjustments_Products_TenantId_ProductId",
                        columns: x => new { x.TenantId, x.ProductId },
                        principalTable: "Products",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryAdjustments_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryAdjustments_Warehouses_TenantId_WarehouseId",
                        columns: x => new { x.TenantId, x.WarehouseId },
                        principalTable: "Warehouses",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAdjustments_TenantId_CounterAccountId",
                table: "InventoryAdjustments",
                columns: new[] { "TenantId", "CounterAccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAdjustments_TenantId_IdempotencyKey",
                table: "InventoryAdjustments",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAdjustments_TenantId_Number",
                table: "InventoryAdjustments",
                columns: new[] { "TenantId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAdjustments_TenantId_ProductId",
                table: "InventoryAdjustments",
                columns: new[] { "TenantId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryAdjustments_TenantId_WarehouseId_CreatedAtUtc",
                table: "InventoryAdjustments",
                columns: new[] { "TenantId", "WarehouseId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryAdjustments");
        }
    }
}
