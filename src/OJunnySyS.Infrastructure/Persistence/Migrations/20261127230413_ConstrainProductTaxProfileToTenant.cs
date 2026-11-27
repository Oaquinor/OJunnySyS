using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OJunnySyS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ConstrainProductTaxProfileToTenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_TaxProfiles_TaxProfileId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_TaxProfileId",
                table: "Products");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Products_TaxProfileId",
                table: "Products",
                column: "TaxProfileId");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_TaxProfiles_TaxProfileId",
                table: "Products",
                column: "TaxProfileId",
                principalTable: "TaxProfiles",
                principalColumn: "Id");
        }
    }
}
