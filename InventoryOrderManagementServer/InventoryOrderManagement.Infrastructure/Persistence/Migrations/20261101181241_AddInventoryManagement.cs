using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryOrderManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InventoryBalances",
                columns: table => new
                {
                    ProductId = table.Column<Guid>(type: "TEXT", nullable: false),
                    QuantityOnHand = table.Column<int>(type: "INTEGER", nullable: false),
                    MinimumStockLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryBalances", x => x.ProductId);
                    table.CheckConstraint("CK_InventoryBalances_MinimumStockLevel", "\"MinimumStockLevel\" >= 0");
                    table.CheckConstraint("CK_InventoryBalances_QuantityOnHand", "\"QuantityOnHand\" >= 0");
                    table.ForeignKey(
                        name: "FK_InventoryBalances_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockMovements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SupplierId = table.Column<Guid>(type: "TEXT", nullable: true),
                    MovementType = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    PreviousQuantity = table.Column<int>(type: "INTEGER", nullable: false),
                    NewQuantity = table.Column<int>(type: "INTEGER", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockMovements", x => x.Id);
                    table.CheckConstraint("CK_StockMovements_NewQuantity", "\"NewQuantity\" >= 0");
                    table.CheckConstraint("CK_StockMovements_PreviousQuantity", "\"PreviousQuantity\" >= 0");
                    table.CheckConstraint("CK_StockMovements_Quantity", "\"Quantity\" > 0");
                    table.ForeignKey(
                        name: "FK_StockMovements_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockMovements_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_CreatedAtUtc",
                table: "StockMovements",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_ProductId_CreatedAtUtc",
                table: "StockMovements",
                columns: new[] { "ProductId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_SupplierId",
                table: "StockMovements",
                column: "SupplierId");

            migrationBuilder.Sql(
                """
                INSERT INTO "InventoryBalances" ("ProductId", "QuantityOnHand", "MinimumStockLevel", "Version")
                SELECT "Id", 0, 0, 0
                FROM "Products" AS p
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM "InventoryBalances" AS b
                    WHERE b."ProductId" = p."Id"
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryBalances");

            migrationBuilder.DropTable(
                name: "StockMovements");
        }
    }
}
