using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OJunnySyS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CustomerReceiptPaymentMethodTenantLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_CustomerReceipts_TenantId_PaymentMethodId",
                table: "CustomerReceipts",
                columns: new[] { "TenantId", "PaymentMethodId" });

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerReceipts_TenantPaymentMethods_TenantId_PaymentMethod~",
                table: "CustomerReceipts",
                columns: new[] { "TenantId", "PaymentMethodId" },
                principalTable: "TenantPaymentMethods",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomerReceipts_TenantPaymentMethods_TenantId_PaymentMethod~",
                table: "CustomerReceipts");

            migrationBuilder.DropIndex(
                name: "IX_CustomerReceipts_TenantId_PaymentMethodId",
                table: "CustomerReceipts");
        }
    }
}
