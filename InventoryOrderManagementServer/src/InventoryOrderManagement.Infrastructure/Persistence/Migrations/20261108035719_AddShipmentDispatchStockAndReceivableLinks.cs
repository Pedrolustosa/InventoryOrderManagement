using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryOrderManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddShipmentDispatchStockAndReceivableLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ShipmentId",
                table: "StockMovements",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_ShipmentId_ProductId",
                table: "StockMovements",
                columns: new[] { "ShipmentId", "ProductId" },
                unique: true,
                filter: "\"ShipmentId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialTitles_Type_SourceType_SourceId",
                table: "FinancialTitles",
                columns: new[] { "Type", "SourceType", "SourceId" },
                unique: true,
                filter: "\"Type\" = 'Receivable' AND \"SourceType\" = 'SalesOrder' AND \"SourceId\" IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_Shipments_ShipmentId",
                table: "StockMovements",
                column: "ShipmentId",
                principalTable: "Shipments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_Shipments_ShipmentId",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_ShipmentId_ProductId",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_FinancialTitles_Type_SourceType_SourceId",
                table: "FinancialTitles");

            migrationBuilder.DropColumn(
                name: "ShipmentId",
                table: "StockMovements");
        }
    }
}
