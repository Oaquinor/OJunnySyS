using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OJunnySyS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSaleReturns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_Products_TenantId_Id",
                table: "Products",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateTable(
                name: "SaleReturns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    SaleId = table.Column<Guid>(type: "char(36)", nullable: false),
                    Number = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    IdempotencyKey = table.Column<Guid>(type: "char(36)", nullable: false),
                    Subtotal = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    ReceivableCredit = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    RefundAmount = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    PaymentMethodId = table.Column<Guid>(type: "char(36)", nullable: true),
                    PaymentMethodName = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: true),
                    LedgerAccountId = table.Column<Guid>(type: "char(36)", nullable: true),
                    RefundMethodType = table.Column<int>(type: "int", nullable: true),
                    CashSessionId = table.Column<Guid>(type: "char(36)", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetime", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    TenantId = table.Column<Guid>(type: "char(36)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaleReturns", x => x.Id);
                    table.UniqueConstraint("AK_SaleReturns_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_SaleReturns_CashSessions_CashSessionId",
                        column: x => x.CashSessionId,
                        principalTable: "CashSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SaleReturns_ChartAccounts_TenantId_LedgerAccountId",
                        columns: x => new { x.TenantId, x.LedgerAccountId },
                        principalTable: "ChartAccounts",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SaleReturns_Sales_TenantId_SaleId",
                        columns: x => new { x.TenantId, x.SaleId },
                        principalTable: "Sales",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SaleReturns_TenantPaymentMethods_TenantId_PaymentMethodId",
                        columns: x => new { x.TenantId, x.PaymentMethodId },
                        principalTable: "TenantPaymentMethods",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SaleReturns_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SaleReturnLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    SaleReturnId = table.Column<Guid>(type: "char(36)", nullable: false),
                    SaleLineId = table.Column<Guid>(type: "char(36)", nullable: false),
                    ProductId = table.Column<Guid>(type: "char(36)", nullable: false),
                    Sku = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    ProductName = table.Column<string>(type: "varchar(160)", maxLength: 160, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    TaxRate = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(14,4)", precision: 14, scale: 4, nullable: false),
                    CostedQuantity = table.Column<int>(type: "int", nullable: false),
                    LineTotal = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    CostTotal = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetime", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    TenantId = table.Column<Guid>(type: "char(36)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaleReturnLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SaleReturnLines_Products_TenantId_ProductId",
                        columns: x => new { x.TenantId, x.ProductId },
                        principalTable: "Products",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SaleReturnLines_SaleLine_SaleLineId",
                        column: x => x.SaleLineId,
                        principalTable: "SaleLine",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SaleReturnLines_SaleReturns_TenantId_SaleReturnId",
                        columns: x => new { x.TenantId, x.SaleReturnId },
                        principalTable: "SaleReturns",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_SaleReturnLines_SaleLineId",
                table: "SaleReturnLines",
                column: "SaleLineId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleReturnLines_TenantId_ProductId",
                table: "SaleReturnLines",
                columns: new[] { "TenantId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_SaleReturnLines_TenantId_SaleLineId",
                table: "SaleReturnLines",
                columns: new[] { "TenantId", "SaleLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_SaleReturnLines_TenantId_SaleReturnId",
                table: "SaleReturnLines",
                columns: new[] { "TenantId", "SaleReturnId" });

            migrationBuilder.CreateIndex(
                name: "IX_SaleReturns_CashSessionId",
                table: "SaleReturns",
                column: "CashSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleReturns_TenantId_IdempotencyKey",
                table: "SaleReturns",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SaleReturns_TenantId_LedgerAccountId",
                table: "SaleReturns",
                columns: new[] { "TenantId", "LedgerAccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_SaleReturns_TenantId_Number",
                table: "SaleReturns",
                columns: new[] { "TenantId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SaleReturns_TenantId_PaymentMethodId",
                table: "SaleReturns",
                columns: new[] { "TenantId", "PaymentMethodId" });

            migrationBuilder.CreateIndex(
                name: "IX_SaleReturns_TenantId_SaleId_CreatedAtUtc",
                table: "SaleReturns",
                columns: new[] { "TenantId", "SaleId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SaleReturnLines");

            migrationBuilder.DropTable(
                name: "SaleReturns");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Products_TenantId_Id",
                table: "Products");
        }
    }
}
