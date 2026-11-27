using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OJunnySyS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BackfillOpeningInventoryMovements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO `InventoryMovements`
                    (`Id`, `ProductId`, `Type`, `QuantityDelta`, `BalanceAfter`, `Reference`, `CreatedAtUtc`, `UpdatedAtUtc`, `TenantId`)
                SELECT UUID(), `Id`, 3, `StockOnHand`, `StockOnHand`, 'Initial stock when enabling ledger', UTC_TIMESTAMP(), NULL, `TenantId`
                FROM `Products`
                WHERE `StockOnHand` > 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM `InventoryMovements` WHERE `Type` = 3 AND `Reference` = 'Initial stock when enabling ledger';");
        }
    }
}
