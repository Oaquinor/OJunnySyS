using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OJunnySyS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PosRegistersAndMultiTerminalCash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RegisterCode",
                table: "SupplierPayments",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "MAIN");

            migrationBuilder.AddColumn<string>(
                name: "RegisterCode",
                table: "Sales",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "MAIN");

            migrationBuilder.CreateTable(
                name: "PosRegisters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    Code = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Revision = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetime", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    TenantId = table.Column<Guid>(type: "char(36)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PosRegisters", x => x.Id);
                    table.UniqueConstraint("AK_PosRegisters_TenantId_Code", x => new { x.TenantId, x.Code });
                    table.ForeignKey(
                        name: "FK_PosRegisters_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_PosRegisters_TenantId_Name",
                table: "PosRegisters",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.Sql("INSERT INTO `PosRegisters` (`Id`, `Code`, `Name`, `IsActive`, `Revision`, `CreatedAtUtc`, `UpdatedAtUtc`, `TenantId`) SELECT UUID(), 'MAIN', 'Caja principal', 1, 0, UTC_TIMESTAMP(), NULL, `Id` FROM `Tenants`;");

            migrationBuilder.AddForeignKey(
                name: "FK_CashSessions_PosRegisters_TenantId_RegisterCode",
                table: "CashSessions",
                columns: new[] { "TenantId", "RegisterCode" },
                principalTable: "PosRegisters",
                principalColumns: new[] { "TenantId", "Code" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CashSessions_PosRegisters_TenantId_RegisterCode",
                table: "CashSessions");

            migrationBuilder.DropTable(
                name: "PosRegisters");

            migrationBuilder.DropColumn(
                name: "RegisterCode",
                table: "SupplierPayments");

            migrationBuilder.DropColumn(
                name: "RegisterCode",
                table: "Sales");
        }
    }
}
