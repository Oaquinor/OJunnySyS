using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace OJunnySyS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(StoreDbContext))]
[Migration("20261127183000_TenantReceiptDesigner")]
public partial class TenantReceiptDesigner : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "ReceiptDesign", table: "Tenants", type: "varchar(20)", maxLength: 20, nullable: false, defaultValue: "classic");
        migrationBuilder.AddColumn<bool>(name: "ReceiptCompactLineSpacing", table: "Tenants", type: "tinyint(1)", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<string>(name: "ReceiptFontFamily", table: "Tenants", type: "varchar(20)", maxLength: 20, nullable: false, defaultValue: "modern");
        migrationBuilder.AddColumn<int>(name: "ReceiptFontSize", table: "Tenants", type: "int", nullable: false, defaultValue: 12);
        migrationBuilder.AddColumn<int>(name: "ReceiptMarginLeftMm", table: "Tenants", type: "int", nullable: false, defaultValue: 0);
        migrationBuilder.AddColumn<int>(name: "ReceiptMarginRightMm", table: "Tenants", type: "int", nullable: false, defaultValue: 0);
        migrationBuilder.AddColumn<bool>(name: "ReceiptShowUnitPrice", table: "Tenants", type: "tinyint(1)", nullable: false, defaultValue: true);
        migrationBuilder.AddColumn<bool>(name: "ReceiptShowLineTotals", table: "Tenants", type: "tinyint(1)", nullable: false, defaultValue: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "ReceiptDesign", table: "Tenants");
        migrationBuilder.DropColumn(name: "ReceiptCompactLineSpacing", table: "Tenants");
        migrationBuilder.DropColumn(name: "ReceiptFontFamily", table: "Tenants");
        migrationBuilder.DropColumn(name: "ReceiptFontSize", table: "Tenants");
        migrationBuilder.DropColumn(name: "ReceiptMarginLeftMm", table: "Tenants");
        migrationBuilder.DropColumn(name: "ReceiptMarginRightMm", table: "Tenants");
        migrationBuilder.DropColumn(name: "ReceiptShowUnitPrice", table: "Tenants");
        migrationBuilder.DropColumn(name: "ReceiptShowLineTotals", table: "Tenants");
    }
}
