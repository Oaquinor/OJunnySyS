using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OJunnySyS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SalesQuotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SalesQuoteId",
                table: "Sales",
                type: "char(36)",
                nullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Customers_TenantId_Id",
                table: "Customers",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateTable(
                name: "SalesQuotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    Number = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    CustomerId = table.Column<Guid>(type: "char(36)", nullable: false),
                    CustomerName = table.Column<string>(type: "varchar(220)", maxLength: 220, nullable: false),
                    ValidUntil = table.Column<DateTime>(type: "date", nullable: false),
                    Notes = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Subtotal = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    ConvertedSaleId = table.Column<Guid>(type: "char(36)", nullable: true),
                    Revision = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetime", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    TenantId = table.Column<Guid>(type: "char(36)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesQuotes", x => x.Id);
                    table.UniqueConstraint("AK_SalesQuotes_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_SalesQuotes_Customers_TenantId_CustomerId",
                        columns: x => new { x.TenantId, x.CustomerId },
                        principalTable: "Customers",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesQuotes_Sales_TenantId_ConvertedSaleId",
                        columns: x => new { x.TenantId, x.ConvertedSaleId },
                        principalTable: "Sales",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesQuotes_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SalesQuoteLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    SalesQuoteId = table.Column<Guid>(type: "char(36)", nullable: false),
                    ProductId = table.Column<Guid>(type: "char(36)", nullable: false),
                    Sku = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    ProductName = table.Column<string>(type: "varchar(160)", maxLength: 160, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    LineTotal = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    TaxRate = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetime", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    TenantId = table.Column<Guid>(type: "char(36)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesQuoteLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesQuoteLines_SalesQuotes_TenantId_SalesQuoteId",
                        columns: x => new { x.TenantId, x.SalesQuoteId },
                        principalTable: "SalesQuotes",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SalesQuoteLines_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_TenantId_SalesQuoteId",
                table: "Sales",
                columns: new[] { "TenantId", "SalesQuoteId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuoteLines_TenantId_SalesQuoteId",
                table: "SalesQuoteLines",
                columns: new[] { "TenantId", "SalesQuoteId" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotes_TenantId_ConvertedSaleId",
                table: "SalesQuotes",
                columns: new[] { "TenantId", "ConvertedSaleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotes_TenantId_CustomerId_CreatedAtUtc",
                table: "SalesQuotes",
                columns: new[] { "TenantId", "CustomerId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotes_TenantId_Number",
                table: "SalesQuotes",
                columns: new[] { "TenantId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesQuotes_TenantId_Status_ValidUntil",
                table: "SalesQuotes",
                columns: new[] { "TenantId", "Status", "ValidUntil" });

            migrationBuilder.AddForeignKey(
                name: "FK_Sales_SalesQuotes_TenantId_SalesQuoteId",
                table: "Sales",
                columns: new[] { "TenantId", "SalesQuoteId" },
                principalTable: "SalesQuotes",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sales_SalesQuotes_TenantId_SalesQuoteId",
                table: "Sales");

            migrationBuilder.DropTable(
                name: "SalesQuoteLines");

            migrationBuilder.DropTable(
                name: "SalesQuotes");

            migrationBuilder.DropIndex(
                name: "IX_Sales_TenantId_SalesQuoteId",
                table: "Sales");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Customers_TenantId_Id",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "SalesQuoteId",
                table: "Sales");
        }
    }
}
