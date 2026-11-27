using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OJunnySyS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CashMovementsAccounting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CashPaymentMethodId",
                table: "CashMovements",
                type: "char(36)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CounterAccountId",
                table: "CashMovements",
                type: "char(36)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "IdempotencyKey",
                table: "CashMovements",
                type: "char(36)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CashMovements_TenantId_CashPaymentMethodId",
                table: "CashMovements",
                columns: new[] { "TenantId", "CashPaymentMethodId" });

            migrationBuilder.CreateIndex(
                name: "IX_CashMovements_TenantId_CounterAccountId",
                table: "CashMovements",
                columns: new[] { "TenantId", "CounterAccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_CashMovements_TenantId_IdempotencyKey",
                table: "CashMovements",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CashMovements_ChartAccounts_TenantId_CounterAccountId",
                table: "CashMovements",
                columns: new[] { "TenantId", "CounterAccountId" },
                principalTable: "ChartAccounts",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CashMovements_TenantPaymentMethods_TenantId_CashPaymentMetho~",
                table: "CashMovements",
                columns: new[] { "TenantId", "CashPaymentMethodId" },
                principalTable: "TenantPaymentMethods",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CashMovements_ChartAccounts_TenantId_CounterAccountId",
                table: "CashMovements");

            migrationBuilder.DropForeignKey(
                name: "FK_CashMovements_TenantPaymentMethods_TenantId_CashPaymentMetho~",
                table: "CashMovements");

            migrationBuilder.DropIndex(
                name: "IX_CashMovements_TenantId_CashPaymentMethodId",
                table: "CashMovements");

            migrationBuilder.DropIndex(
                name: "IX_CashMovements_TenantId_CounterAccountId",
                table: "CashMovements");

            migrationBuilder.DropIndex(
                name: "IX_CashMovements_TenantId_IdempotencyKey",
                table: "CashMovements");

            migrationBuilder.DropColumn(
                name: "CashPaymentMethodId",
                table: "CashMovements");

            migrationBuilder.DropColumn(
                name: "CounterAccountId",
                table: "CashMovements");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "CashMovements");
        }
    }
}
