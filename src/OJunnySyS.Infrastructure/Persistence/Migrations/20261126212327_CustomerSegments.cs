using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OJunnySyS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CustomerSegments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomerSegments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    NormalizedName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    RequiredTagId = table.Column<Guid>(type: "char(36)", nullable: true),
                    MinimumLifetimeValue = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: true),
                    MaximumLifetimeValue = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: true),
                    MinimumSalesCount = table.Column<int>(type: "int", nullable: true),
                    PurchasedWithinDays = table.Column<int>(type: "int", nullable: true),
                    ActiveCustomersOnly = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Revision = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetime", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    TenantId = table.Column<Guid>(type: "char(36)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerSegments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerSegments_CustomerTags_RequiredTagId",
                        column: x => x.RequiredTagId,
                        principalTable: "CustomerTags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CustomerSegments_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerSegments_RequiredTagId",
                table: "CustomerSegments",
                column: "RequiredTagId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerSegments_TenantId_NormalizedName",
                table: "CustomerSegments",
                columns: new[] { "TenantId", "NormalizedName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerSegments_TenantId_RequiredTagId",
                table: "CustomerSegments",
                columns: new[] { "TenantId", "RequiredTagId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerSegments");
        }
    }
}
