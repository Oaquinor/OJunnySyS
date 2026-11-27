using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OJunnySyS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SellerAttribution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SellerName",
                table: "Sales",
                type: "varchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SellerUserId",
                table: "Sales",
                type: "char(36)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sales_TenantId_SellerUserId_CreatedAtUtc",
                table: "Sales",
                columns: new[] { "TenantId", "SellerUserId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sales_TenantId_SellerUserId_CreatedAtUtc",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "SellerName",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "SellerUserId",
                table: "Sales");
        }
    }
}
