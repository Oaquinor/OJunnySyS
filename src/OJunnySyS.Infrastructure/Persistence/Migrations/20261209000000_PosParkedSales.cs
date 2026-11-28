using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OJunnySyS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PosParkedSales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ParkedSaleId",
                table: "Sales",
                type: "char(36)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ParkedSales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    RegisterCode = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    CashierUserId = table.Column<Guid>(type: "char(36)", nullable: false),
                    CustomerId = table.Column<Guid>(type: "char(36)", nullable: true),
                    CustomerName = table.Column<string>(type: "varchar(220)", maxLength: 220, nullable: false),
                    SellerUserId = table.Column<Guid>(type: "char(36)", nullable: true),
                    SellerName = table.Column<string>(type: "varchar(160)", maxLength: 160, nullable: true),
                    IdempotencyKey = table.Column<Guid>(type: "char(36)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ConvertedSaleId = table.Column<Guid>(type: "char(36)", nullable: true),
                    CancelledByUserId = table.Column<Guid>(type: "char(36)", nullable: true),
                    CancelledAtUtc = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    Revision = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetime", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    TenantId = table.Column<Guid>(type: "char(36)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParkedSales", x => x.Id);
                    table.UniqueConstraint("AK_ParkedSales_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_ParkedSales_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ParkedSaleLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    ParkedSaleId = table.Column<Guid>(type: "char(36)", nullable: false),
                    ProductId = table.Column<Guid>(type: "char(36)", nullable: false),
                    Sku = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    ProductName = table.Column<string>(type: "varchar(160)", maxLength: 160, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    DiscountPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetime", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    TenantId = table.Column<Guid>(type: "char(36)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParkedSaleLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParkedSaleLines_ParkedSales_TenantId_ParkedSaleId",
                        columns: x => new { x.TenantId, x.ParkedSaleId },
                        principalTable: "ParkedSales",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ParkedSaleLines_Products_TenantId_ProductId",
                        columns: x => new { x.TenantId, x.ProductId },
                        principalTable: "Products",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_TenantId_ParkedSaleId",
                table: "Sales",
                columns: new[] { "TenantId", "ParkedSaleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ParkedSaleLines_TenantId_ParkedSaleId",
                table: "ParkedSaleLines",
                columns: new[] { "TenantId", "ParkedSaleId" });

            migrationBuilder.CreateIndex(
                name: "IX_ParkedSaleLines_TenantId_ProductId",
                table: "ParkedSaleLines",
                columns: new[] { "TenantId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_ParkedSales_TenantId_IdempotencyKey",
                table: "ParkedSales",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ParkedSales_TenantId_RegisterCode_Status_CreatedAtUtc",
                table: "ParkedSales",
                columns: new[] { "TenantId", "RegisterCode", "Status", "CreatedAtUtc" });

            migrationBuilder.AddForeignKey(
                name: "FK_Sales_ParkedSales_TenantId_ParkedSaleId",
                table: "Sales",
                columns: new[] { "TenantId", "ParkedSaleId" },
                principalTable: "ParkedSales",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sales_ParkedSales_TenantId_ParkedSaleId",
                table: "Sales");

            migrationBuilder.DropTable(
                name: "ParkedSaleLines");

            migrationBuilder.DropTable(
                name: "ParkedSales");

            migrationBuilder.DropIndex(
                name: "IX_Sales_TenantId_ParkedSaleId",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "ParkedSaleId",
                table: "Sales");
        }
    }
}
