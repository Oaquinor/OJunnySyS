using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OJunnySyS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CrmAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AssignedToUserId",
                table: "Opportunities",
                type: "char(36)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AssignedToUserId",
                table: "CrmActivities",
                type: "char(36)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Opportunities_AssignedToUserId",
                table: "Opportunities",
                column: "AssignedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Opportunities_TenantId_AssignedToUserId",
                table: "Opportunities",
                columns: new[] { "TenantId", "AssignedToUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_CrmActivities_AssignedToUserId",
                table: "CrmActivities",
                column: "AssignedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CrmActivities_TenantId_AssignedToUserId_Kind_DueAtUtc_Comple~",
                table: "CrmActivities",
                columns: new[] { "TenantId", "AssignedToUserId", "Kind", "DueAtUtc", "CompletedAtUtc" });

            migrationBuilder.AddForeignKey(
                name: "FK_CrmActivities_AspNetUsers_AssignedToUserId",
                table: "CrmActivities",
                column: "AssignedToUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Opportunities_AspNetUsers_AssignedToUserId",
                table: "Opportunities",
                column: "AssignedToUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CrmActivities_AspNetUsers_AssignedToUserId",
                table: "CrmActivities");

            migrationBuilder.DropForeignKey(
                name: "FK_Opportunities_AspNetUsers_AssignedToUserId",
                table: "Opportunities");

            migrationBuilder.DropIndex(
                name: "IX_Opportunities_AssignedToUserId",
                table: "Opportunities");

            migrationBuilder.DropIndex(
                name: "IX_Opportunities_TenantId_AssignedToUserId",
                table: "Opportunities");

            migrationBuilder.DropIndex(
                name: "IX_CrmActivities_AssignedToUserId",
                table: "CrmActivities");

            migrationBuilder.DropIndex(
                name: "IX_CrmActivities_TenantId_AssignedToUserId_Kind_DueAtUtc_Comple~",
                table: "CrmActivities");

            migrationBuilder.DropColumn(
                name: "AssignedToUserId",
                table: "Opportunities");

            migrationBuilder.DropColumn(
                name: "AssignedToUserId",
                table: "CrmActivities");
        }
    }
}
