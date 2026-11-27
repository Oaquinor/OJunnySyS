using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OJunnySyS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PurchaseReceiptIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_PurchaseOrders_TenantId_Id",
                table: "PurchaseOrders",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateTable(
                name: "PurchaseReceiptBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "char(36)", nullable: false),
                    IdempotencyKey = table.Column<Guid>(type: "char(36)", nullable: false),
                    RequestFingerprint = table.Column<string>(type: "varchar(12000)", maxLength: 12000, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetime", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    TenantId = table.Column<Guid>(type: "char(36)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseReceiptBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseReceiptBatches_PurchaseOrders_TenantId_PurchaseOrder~",
                        columns: x => new { x.TenantId, x.PurchaseOrderId },
                        principalTable: "PurchaseOrders",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseReceiptBatches_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReceiptBatches_TenantId_IdempotencyKey",
                table: "PurchaseReceiptBatches",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReceiptBatches_TenantId_PurchaseOrderId_CreatedAtUtc",
                table: "PurchaseReceiptBatches",
                columns: new[] { "TenantId", "PurchaseOrderId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PurchaseReceiptBatches");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_PurchaseOrders_TenantId_Id",
                table: "PurchaseOrders");
        }
    }
}
