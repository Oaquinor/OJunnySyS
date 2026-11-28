using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OJunnySyS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ProductCategoryHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProductCategoryId",
                table: "Products",
                type: "char(36)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProductCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    Name = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    NormalizedName = table.Column<string>(type: "varchar(160)", maxLength: 160, nullable: false),
                    HierarchyKey = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    ParentCategoryId = table.Column<Guid>(type: "char(36)", nullable: true),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Revision = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetime", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    TenantId = table.Column<Guid>(type: "char(36)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductCategories", x => x.Id);
                    table.UniqueConstraint("AK_ProductCategories_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_ProductCategories_ProductCategories_TenantId_ParentCategoryId",
                        columns: x => new { x.TenantId, x.ParentCategoryId },
                        principalTable: "ProductCategories",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductCategories_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Products_TenantId_ProductCategoryId",
                table: "Products",
                columns: new[] { "TenantId", "ProductCategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductCategories_TenantId_HierarchyKey",
                table: "ProductCategories",
                columns: new[] { "TenantId", "HierarchyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductCategories_TenantId_ParentCategoryId",
                table: "ProductCategories",
                columns: new[] { "TenantId", "ParentCategoryId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Products_ProductCategories_TenantId_ProductCategoryId",
                table: "Products",
                columns: new[] { "TenantId", "ProductCategoryId" },
                principalTable: "ProductCategories",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_ProductCategories_TenantId_ProductCategoryId",
                table: "Products");

            migrationBuilder.DropTable(
                name: "ProductCategories");

            migrationBuilder.DropIndex(
                name: "IX_Products_TenantId_ProductCategoryId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ProductCategoryId",
                table: "Products");
        }
    }
}
