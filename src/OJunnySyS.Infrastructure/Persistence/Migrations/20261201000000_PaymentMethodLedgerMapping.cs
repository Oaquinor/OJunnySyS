using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OJunnySyS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PaymentMethodLedgerMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "LedgerAccountId",
                table: "TenantPaymentMethods",
                type: "char(36)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LedgerAccountId",
                table: "SalePayments",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.Sql("INSERT INTO `ChartAccounts` (`Id`,`Code`,`Name`,`Type`,`IsSystem`,`IsActive`,`Revision`,`CreatedAtUtc`,`TenantId`) SELECT UUID(), '1010', 'Bancos', 0, 1, 1, 0, UTC_TIMESTAMP(), t.`Id` FROM `Tenants` t WHERE NOT EXISTS (SELECT 1 FROM `ChartAccounts` a WHERE a.`TenantId` = t.`Id` AND a.`Code` = '1010');");
            migrationBuilder.Sql("INSERT INTO `ChartAccounts` (`Id`,`Code`,`Name`,`Type`,`IsSystem`,`IsActive`,`Revision`,`CreatedAtUtc`,`TenantId`) SELECT UUID(), '1020', 'Tarjetas por liquidar', 0, 1, 1, 0, UTC_TIMESTAMP(), t.`Id` FROM `Tenants` t WHERE NOT EXISTS (SELECT 1 FROM `ChartAccounts` a WHERE a.`TenantId` = t.`Id` AND a.`Code` = '1020');");
            migrationBuilder.Sql("INSERT INTO `ChartAccounts` (`Id`,`Code`,`Name`,`Type`,`IsSystem`,`IsActive`,`Revision`,`CreatedAtUtc`,`TenantId`) SELECT UUID(), '1030', 'Otros medios por liquidar', 0, 1, 1, 0, UTC_TIMESTAMP(), t.`Id` FROM `Tenants` t WHERE NOT EXISTS (SELECT 1 FROM `ChartAccounts` a WHERE a.`TenantId` = t.`Id` AND a.`Code` = '1030');");
            migrationBuilder.Sql("UPDATE `TenantPaymentMethods` m JOIN `ChartAccounts` a ON a.`TenantId` = m.`TenantId` AND a.`Code` = CASE m.`Type` WHEN 0 THEN '1000' WHEN 1 THEN '1020' WHEN 2 THEN '1010' ELSE '1030' END SET m.`LedgerAccountId` = a.`Id` WHERE m.`LedgerAccountId` IS NULL;");
            migrationBuilder.Sql("UPDATE `SalePayments` p JOIN `TenantPaymentMethods` m ON m.`TenantId` = p.`TenantId` AND m.`Id` = p.`PaymentMethodId` SET p.`LedgerAccountId` = m.`LedgerAccountId`;");

            migrationBuilder.CreateIndex(
                name: "IX_TenantPaymentMethods_TenantId_LedgerAccountId",
                table: "TenantPaymentMethods",
                columns: new[] { "TenantId", "LedgerAccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_SalePayments_TenantId_LedgerAccountId",
                table: "SalePayments",
                columns: new[] { "TenantId", "LedgerAccountId" });

            migrationBuilder.AddForeignKey(
                name: "FK_SalePayments_ChartAccounts_TenantId_LedgerAccountId",
                table: "SalePayments",
                columns: new[] { "TenantId", "LedgerAccountId" },
                principalTable: "ChartAccounts",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TenantPaymentMethods_ChartAccounts_TenantId_LedgerAccountId",
                table: "TenantPaymentMethods",
                columns: new[] { "TenantId", "LedgerAccountId" },
                principalTable: "ChartAccounts",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalePayments_ChartAccounts_TenantId_LedgerAccountId",
                table: "SalePayments");

            migrationBuilder.DropForeignKey(
                name: "FK_TenantPaymentMethods_ChartAccounts_TenantId_LedgerAccountId",
                table: "TenantPaymentMethods");

            migrationBuilder.DropIndex(
                name: "IX_TenantPaymentMethods_TenantId_LedgerAccountId",
                table: "TenantPaymentMethods");

            migrationBuilder.DropIndex(
                name: "IX_SalePayments_TenantId_LedgerAccountId",
                table: "SalePayments");

            migrationBuilder.DropColumn(
                name: "LedgerAccountId",
                table: "TenantPaymentMethods");

            migrationBuilder.DropColumn(
                name: "LedgerAccountId",
                table: "SalePayments");
        }
    }
}
