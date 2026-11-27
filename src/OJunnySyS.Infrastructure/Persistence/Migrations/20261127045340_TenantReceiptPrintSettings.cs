using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OJunnySyS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TenantReceiptPrintSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BusinessAddress",
                table: "Tenants",
                type: "varchar(240)",
                maxLength: 240,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BusinessPhone",
                table: "Tenants",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LegalName",
                table: "Tenants",
                type: "varchar(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ReceiptFooter",
                table: "Tenants",
                type: "varchar(240)",
                maxLength: 240,
                nullable: false,
                defaultValue: "Gracias por su compra.");

            migrationBuilder.AddColumn<string>(
                name: "ReceiptHeader",
                table: "Tenants",
                type: "varchar(240)",
                maxLength: 240,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ReceiptPaperWidthMm",
                table: "Tenants",
                type: "int",
                nullable: false,
                defaultValue: 80);

            migrationBuilder.AddColumn<bool>(
                name: "ReceiptShowCustomer",
                table: "Tenants",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "ReceiptShowSku",
                table: "Tenants",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "ReceiptShowTax",
                table: "Tenants",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "TaxIdentifier",
                table: "Tenants",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BusinessAddress",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "BusinessPhone",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "LegalName",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "ReceiptFooter",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "ReceiptHeader",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "ReceiptPaperWidthMm",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "ReceiptShowCustomer",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "ReceiptShowSku",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "ReceiptShowTax",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "TaxIdentifier",
                table: "Tenants");
        }
    }
}
