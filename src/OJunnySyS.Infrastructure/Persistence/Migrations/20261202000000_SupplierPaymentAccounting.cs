using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OJunnySyS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SupplierPaymentAccounting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "LedgerAccountId",
                table: "SupplierPayments",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.Sql("INSERT INTO `ChartAccounts` (`Id`,`Code`,`Name`,`Type`,`IsSystem`,`IsActive`,`Revision`,`CreatedAtUtc`,`TenantId`) SELECT UUID(), '1300', 'Impuestos acreditables', 0, 1, 1, 0, UTC_TIMESTAMP(), t.`Id` FROM `Tenants` t WHERE NOT EXISTS (SELECT 1 FROM `ChartAccounts` a WHERE a.`TenantId` = t.`Id` AND a.`Code` = '1300');");
            migrationBuilder.Sql("UPDATE `SupplierPayments` p SET p.`LedgerAccountId` = (SELECT a.`Id` FROM `ChartAccounts` a WHERE a.`TenantId` = p.`TenantId` AND a.`Code` = CASE p.`Method` WHEN 0 THEN '1000' WHEN 1 THEN '1010' WHEN 2 THEN '1020' WHEN 3 THEN '1010' ELSE '1030' END LIMIT 1);");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPayments_TenantId_LedgerAccountId",
                table: "SupplierPayments",
                columns: new[] { "TenantId", "LedgerAccountId" });

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierPayments_ChartAccounts_TenantId_LedgerAccountId",
                table: "SupplierPayments",
                columns: new[] { "TenantId", "LedgerAccountId" },
                principalTable: "ChartAccounts",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SupplierPayments_ChartAccounts_TenantId_LedgerAccountId",
                table: "SupplierPayments");

            migrationBuilder.DropIndex(
                name: "IX_SupplierPayments_TenantId_LedgerAccountId",
                table: "SupplierPayments");

            migrationBuilder.DropColumn(
                name: "LedgerAccountId",
                table: "SupplierPayments");

        }
    }
}
