using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryOrderManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFinanceManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CashAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    AccountType = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    OpeningBalance = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    OpeningBalanceDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinancialTitles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TitleNumber = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CounterpartyType = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    CounterpartyId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CounterpartyName = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    SourceType = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    SourceId = table.Column<Guid>(type: "TEXT", nullable: true),
                    SourceReference = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    DocumentNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    IssueDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CancelledAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CancellationReason = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialTitles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinancialInstallments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FinancialTitleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    InstallmentNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    SettledPrincipalAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    OutstandingBalance = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialInstallments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinancialInstallments_FinancialTitles_FinancialTitleId",
                        column: x => x.FinancialTitleId,
                        principalTable: "FinancialTitles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FinancialSettlements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FinancialTitleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FinancialInstallmentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CashAccountId = table.Column<Guid>(type: "TEXT", nullable: true),
                    PaymentMethod = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    SettledPrincipalAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    InterestAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    FineAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    CashAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Reference = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IdempotencyKey = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialSettlements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinancialSettlements_CashAccounts_CashAccountId",
                        column: x => x.CashAccountId,
                        principalTable: "CashAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinancialSettlements_FinancialInstallments_FinancialInstallmentId",
                        column: x => x.FinancialInstallmentId,
                        principalTable: "FinancialInstallments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinancialSettlements_FinancialTitles_FinancialTitleId",
                        column: x => x.FinancialTitleId,
                        principalTable: "FinancialTitles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CashMovements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CashAccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FinancialSettlementId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Direction = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashMovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CashMovements_CashAccounts_CashAccountId",
                        column: x => x.CashAccountId,
                        principalTable: "CashAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CashMovements_FinancialSettlements_FinancialSettlementId",
                        column: x => x.FinancialSettlementId,
                        principalTable: "FinancialSettlements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CashAccounts_Name",
                table: "CashAccounts",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_CashMovements_CashAccountId",
                table: "CashMovements",
                column: "CashAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_CashMovements_FinancialSettlementId",
                table: "CashMovements",
                column: "FinancialSettlementId",
                unique: true,
                filter: "\"FinancialSettlementId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CashMovements_OccurredAtUtc",
                table: "CashMovements",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialInstallments_DueDate",
                table: "FinancialInstallments",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialInstallments_FinancialTitleId_InstallmentNumber",
                table: "FinancialInstallments",
                columns: new[] { "FinancialTitleId", "InstallmentNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancialSettlements_CashAccountId",
                table: "FinancialSettlements",
                column: "CashAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialSettlements_FinancialInstallmentId",
                table: "FinancialSettlements",
                column: "FinancialInstallmentId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialSettlements_FinancialTitleId",
                table: "FinancialSettlements",
                column: "FinancialTitleId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialSettlements_IdempotencyKey",
                table: "FinancialSettlements",
                column: "IdempotencyKey",
                unique: true,
                filter: "\"IdempotencyKey\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialSettlements_OccurredAtUtc",
                table: "FinancialSettlements",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialTitles_CounterpartyId",
                table: "FinancialTitles",
                column: "CounterpartyId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialTitles_CreatedAtUtc",
                table: "FinancialTitles",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialTitles_IssueDate",
                table: "FinancialTitles",
                column: "IssueDate");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialTitles_SourceId",
                table: "FinancialTitles",
                column: "SourceId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialTitles_TitleNumber",
                table: "FinancialTitles",
                column: "TitleNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancialTitles_Type",
                table: "FinancialTitles",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialTitles_Type_CounterpartyId_DocumentNumber",
                table: "FinancialTitles",
                columns: new[] { "Type", "CounterpartyId", "DocumentNumber" },
                unique: true,
                filter: "\"DocumentNumber\" IS NOT NULL AND \"CounterpartyId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialTitles_Type_CounterpartyName_DocumentNumber",
                table: "FinancialTitles",
                columns: new[] { "Type", "CounterpartyName", "DocumentNumber" },
                unique: true,
                filter: "\"DocumentNumber\" IS NOT NULL AND \"CounterpartyId\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CashMovements");

            migrationBuilder.DropTable(
                name: "FinancialSettlements");

            migrationBuilder.DropTable(
                name: "CashAccounts");

            migrationBuilder.DropTable(
                name: "FinancialInstallments");

            migrationBuilder.DropTable(
                name: "FinancialTitles");
        }
    }
}
