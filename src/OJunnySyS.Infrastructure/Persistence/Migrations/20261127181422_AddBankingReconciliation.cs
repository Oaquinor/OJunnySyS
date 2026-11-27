using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OJunnySyS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBankingReconciliation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_JournalEntryLines_TenantId_Id",
                table: "JournalEntryLines",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateTable(
                name: "BankAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    Name = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    Institution = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    AccountSuffix = table.Column<string>(type: "varchar(4)", maxLength: 4, nullable: true),
                    CurrencyCode = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false),
                    LedgerAccountId = table.Column<Guid>(type: "char(36)", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Revision = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetime", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    TenantId = table.Column<Guid>(type: "char(36)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankAccounts", x => x.Id);
                    table.UniqueConstraint("AK_BankAccounts_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_BankAccounts_ChartAccounts_TenantId_LedgerAccountId",
                        columns: x => new { x.TenantId, x.LedgerAccountId },
                        principalTable: "ChartAccounts",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BankAccounts_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BankReconciliations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    BankAccountId = table.Column<Guid>(type: "char(36)", nullable: false),
                    StatementNumber = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "date", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "date", nullable: false),
                    OpeningBalance = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    ClosingBalance = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReconciledAtUtc = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    Revision = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetime", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    TenantId = table.Column<Guid>(type: "char(36)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankReconciliations", x => x.Id);
                    table.UniqueConstraint("AK_BankReconciliations_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_BankReconciliations_BankAccounts_TenantId_BankAccountId",
                        columns: x => new { x.TenantId, x.BankAccountId },
                        principalTable: "BankAccounts",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BankReconciliations_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BankStatementLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    BankReconciliationId = table.Column<Guid>(type: "char(36)", nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "date", nullable: false),
                    Description = table.Column<string>(type: "varchar(180)", maxLength: 180, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    Reference = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true),
                    JournalEntryLineId = table.Column<Guid>(type: "char(36)", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetime", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    TenantId = table.Column<Guid>(type: "char(36)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankStatementLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BankStatementLines_BankReconciliations_TenantId_BankReconcil~",
                        columns: x => new { x.TenantId, x.BankReconciliationId },
                        principalTable: "BankReconciliations",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BankStatementLines_JournalEntryLines_TenantId_JournalEntryLi~",
                        columns: x => new { x.TenantId, x.JournalEntryLineId },
                        principalTable: "JournalEntryLines",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BankStatementLines_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_BankAccounts_TenantId_LedgerAccountId",
                table: "BankAccounts",
                columns: new[] { "TenantId", "LedgerAccountId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BankAccounts_TenantId_Name",
                table: "BankAccounts",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BankReconciliations_TenantId_BankAccountId_PeriodEnd",
                table: "BankReconciliations",
                columns: new[] { "TenantId", "BankAccountId", "PeriodEnd" });

            migrationBuilder.CreateIndex(
                name: "IX_BankReconciliations_TenantId_BankAccountId_StatementNumber",
                table: "BankReconciliations",
                columns: new[] { "TenantId", "BankAccountId", "StatementNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BankStatementLines_TenantId_BankReconciliationId_Transaction~",
                table: "BankStatementLines",
                columns: new[] { "TenantId", "BankReconciliationId", "TransactionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_BankStatementLines_TenantId_JournalEntryLineId",
                table: "BankStatementLines",
                columns: new[] { "TenantId", "JournalEntryLineId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BankStatementLines");

            migrationBuilder.DropTable(
                name: "BankReconciliations");

            migrationBuilder.DropTable(
                name: "BankAccounts");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_JournalEntryLines_TenantId_Id",
                table: "JournalEntryLines");
        }
    }
}
