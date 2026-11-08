using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryOrderManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReturnStockFinanceReversal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AdjustedFinancialTitleId",
                table: "ReturnRequests",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FinancialAdjustedAmount",
                table: "ReturnRequests",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "ReturnReplacementItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ReturnRequestId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductSku = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ProductName = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    UnitPriceSnapshot = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    LineAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReturnReplacementItems", x => x.Id);
                    table.CheckConstraint("CK_ReturnReplacementItems_LineAmount", "\"LineAmount\" >= 0");
                    table.CheckConstraint("CK_ReturnReplacementItems_Quantity", "\"Quantity\" > 0");
                    table.CheckConstraint("CK_ReturnReplacementItems_UnitPriceSnapshot", "\"UnitPriceSnapshot\" >= 0");
                    table.ForeignKey(
                        name: "FK_ReturnReplacementItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReturnReplacementItems_ReturnRequests_ReturnRequestId",
                        column: x => x.ReturnRequestId,
                        principalTable: "ReturnRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_ReturnRequestId_ProductId_MovementType",
                table: "StockMovements",
                columns: new[] { "ReturnRequestId", "ProductId", "MovementType" },
                unique: true,
                filter: "\"ReturnRequestId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnRequests_AdjustedFinancialTitleId",
                table: "ReturnRequests",
                column: "AdjustedFinancialTitleId");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnRequests_FinancialTitleId",
                table: "ReturnRequests",
                column: "FinancialTitleId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialTitles_SourceType_SourceId",
                table: "FinancialTitles",
                columns: new[] { "SourceType", "SourceId" },
                unique: true,
                filter: "\"SourceType\" = 'Return' AND \"SourceId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnReplacementItems_ProductId",
                table: "ReturnReplacementItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnReplacementItems_ReturnRequestId_ProductId",
                table: "ReturnReplacementItems",
                columns: new[] { "ReturnRequestId", "ProductId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ReturnRequests_FinancialTitles_AdjustedFinancialTitleId",
                table: "ReturnRequests",
                column: "AdjustedFinancialTitleId",
                principalTable: "FinancialTitles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ReturnRequests_FinancialTitles_FinancialTitleId",
                table: "ReturnRequests",
                column: "FinancialTitleId",
                principalTable: "FinancialTitles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReturnRequests_FinancialTitles_AdjustedFinancialTitleId",
                table: "ReturnRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_ReturnRequests_FinancialTitles_FinancialTitleId",
                table: "ReturnRequests");

            migrationBuilder.DropTable(
                name: "ReturnReplacementItems");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_ReturnRequestId_ProductId_MovementType",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_ReturnRequests_AdjustedFinancialTitleId",
                table: "ReturnRequests");

            migrationBuilder.DropIndex(
                name: "IX_ReturnRequests_FinancialTitleId",
                table: "ReturnRequests");

            migrationBuilder.DropIndex(
                name: "IX_FinancialTitles_SourceType_SourceId",
                table: "FinancialTitles");

            migrationBuilder.DropColumn(
                name: "AdjustedFinancialTitleId",
                table: "ReturnRequests");

            migrationBuilder.DropColumn(
                name: "FinancialAdjustedAmount",
                table: "ReturnRequests");
        }
    }
}
