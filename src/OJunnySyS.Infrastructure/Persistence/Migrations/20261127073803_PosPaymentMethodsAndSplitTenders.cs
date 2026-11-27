using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OJunnySyS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PosPaymentMethodsAndSplitTenders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_Sales_TenantId_Id",
                table: "Sales",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateTable(
                name: "TenantPaymentMethods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    Code = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Instructions = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true),
                    AccountLabel = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true),
                    AccountReference = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    QrPayload = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Revision = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetime", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    TenantId = table.Column<Guid>(type: "char(36)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantPaymentMethods", x => x.Id);
                    table.UniqueConstraint("AK_TenantPaymentMethods_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_TenantPaymentMethods_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SalePayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    SaleId = table.Column<Guid>(type: "char(36)", nullable: false),
                    PaymentMethodId = table.Column<Guid>(type: "char(36)", nullable: false),
                    MethodName = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    Reference = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetime", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    TenantId = table.Column<Guid>(type: "char(36)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalePayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalePayments_Sales_TenantId_SaleId",
                        columns: x => new { x.TenantId, x.SaleId },
                        principalTable: "Sales",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalePayments_TenantPaymentMethods_TenantId_PaymentMethodId",
                        columns: x => new { x.TenantId, x.PaymentMethodId },
                        principalTable: "TenantPaymentMethods",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalePayments_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.Sql("""
                INSERT INTO TenantPaymentMethods (Id, Code, Name, Type, Instructions, AccountLabel, AccountReference, QrPayload, IsActive, Revision, CreatedAtUtc, UpdatedAtUtc, TenantId)
                SELECT UUID(), 'cash', 'Efectivo', 0, NULL, NULL, NULL, NULL, 1, 0, UTC_TIMESTAMP(6), NULL, Id FROM Tenants;
                INSERT INTO TenantPaymentMethods (Id, Code, Name, Type, Instructions, AccountLabel, AccountReference, QrPayload, IsActive, Revision, CreatedAtUtc, UpdatedAtUtc, TenantId)
                SELECT UUID(), 'card', 'Tarjeta', 1, NULL, NULL, NULL, NULL, 1, 0, UTC_TIMESTAMP(6), NULL, Id FROM Tenants;
                INSERT INTO TenantPaymentMethods (Id, Code, Name, Type, Instructions, AccountLabel, AccountReference, QrPayload, IsActive, Revision, CreatedAtUtc, UpdatedAtUtc, TenantId)
                SELECT UUID(), 'transfer', 'Transferencia', 2, NULL, NULL, NULL, NULL, 1, 0, UTC_TIMESTAMP(6), NULL, Id FROM Tenants;
                INSERT INTO TenantPaymentMethods (Id, Code, Name, Type, Instructions, AccountLabel, AccountReference, QrPayload, IsActive, Revision, CreatedAtUtc, UpdatedAtUtc, TenantId)
                SELECT UUID(), 'other', 'Otro', 3, NULL, NULL, NULL, NULL, 1, 0, UTC_TIMESTAMP(6), NULL, Id FROM Tenants;
                INSERT INTO SalePayments (Id, SaleId, PaymentMethodId, MethodName, Type, Amount, Reference, CreatedAtUtc, UpdatedAtUtc, TenantId)
                SELECT UUID(), s.Id, pm.Id, pm.Name,
                    CASE s.PaymentMethod WHEN 0 THEN 0 WHEN 1 THEN 1 WHEN 2 THEN 2 ELSE 3 END,
                    s.Total, NULL, s.CreatedAtUtc, NULL, s.TenantId
                FROM Sales s
                INNER JOIN TenantPaymentMethods pm ON pm.TenantId = s.TenantId AND pm.Code =
                    CASE s.PaymentMethod WHEN 0 THEN 'cash' WHEN 1 THEN 'card' WHEN 2 THEN 'transfer' ELSE 'other' END;
                """);
            migrationBuilder.CreateIndex(
                name: "IX_SalePayments_TenantId_PaymentMethodId_Type",
                table: "SalePayments",
                columns: new[] { "TenantId", "PaymentMethodId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_SalePayments_TenantId_SaleId",
                table: "SalePayments",
                columns: new[] { "TenantId", "SaleId" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantPaymentMethods_TenantId_Code",
                table: "TenantPaymentMethods",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantPaymentMethods_TenantId_Name",
                table: "TenantPaymentMethods",
                columns: new[] { "TenantId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SalePayments");

            migrationBuilder.DropTable(
                name: "TenantPaymentMethods");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Sales_TenantId_Id",
                table: "Sales");
        }
    }
}
