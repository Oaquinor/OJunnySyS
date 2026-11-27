using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OJunnySyS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TenantTaxProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TaxProfileId",
                table: "Products",
                type: "char(36)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TaxProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    Code = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false),
                    Name = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Revision = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetime", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    TenantId = table.Column<Guid>(type: "char(36)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxProfiles", x => x.Id);
                    table.UniqueConstraint("AK_TaxProfiles_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_TaxProfiles_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Products_TaxProfileId",
                table: "Products",
                column: "TaxProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_TenantId_TaxProfileId",
                table: "Products",
                columns: new[] { "TenantId", "TaxProfileId" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxProfiles_TenantId_Code",
                table: "TaxProfiles",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaxProfiles_TenantId_IsActive_Name",
                table: "TaxProfiles",
                columns: new[] { "TenantId", "IsActive", "Name" });

            migrationBuilder.AddForeignKey(
                name: "FK_Products_TaxProfiles_TaxProfileId",
                table: "Products",
                column: "TaxProfileId",
                principalTable: "TaxProfiles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_TaxProfiles_TenantId_TaxProfileId",
                table: "Products",
                columns: new[] { "TenantId", "TaxProfileId" },
                principalTable: "TaxProfiles",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_TaxProfiles_TaxProfileId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_TaxProfiles_TenantId_TaxProfileId",
                table: "Products");

            migrationBuilder.DropTable(
                name: "TaxProfiles");

            migrationBuilder.DropIndex(
                name: "IX_Products_TaxProfileId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_TenantId_TaxProfileId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "TaxProfileId",
                table: "Products");
        }
    }
}
