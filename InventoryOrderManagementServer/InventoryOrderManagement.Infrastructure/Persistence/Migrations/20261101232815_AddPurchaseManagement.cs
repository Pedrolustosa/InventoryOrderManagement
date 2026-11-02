using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryOrderManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PurchaseOrderId",
                table: "StockMovements",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PurchaseReceiptId",
                table: "StockMovements",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PurchaseOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PurchaseOrderNumber = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    SupplierId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SupplierName = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Subtotal = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SentAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ReceivedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CancelledAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CancellationReason = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrderItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductSku = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ProductName = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    OrderedQuantity = table.Column<int>(type: "INTEGER", nullable: false),
                    ReceivedQuantity = table.Column<int>(type: "INTEGER", nullable: false),
                    UnitCost = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    LineTotal = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrderItems", x => x.Id);
                    table.CheckConstraint("CK_PurchaseOrderItems_LineTotal", "\"LineTotal\" >= 0");
                    table.CheckConstraint("CK_PurchaseOrderItems_OrderedQuantity", "\"OrderedQuantity\" > 0");
                    table.CheckConstraint("CK_PurchaseOrderItems_ReceivedQuantity", "\"ReceivedQuantity\" >= 0");
                    table.CheckConstraint("CK_PurchaseOrderItems_UnitCost", "\"UnitCost\" >= 0");
                    table.ForeignKey(
                        name: "FK_PurchaseOrderItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderItems_PurchaseOrders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseReceipts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ReceiptNumber = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ReceivedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseReceipts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseReceipts_PurchaseOrders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseReceiptItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PurchaseReceiptId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PurchaseOrderItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductSku = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ProductName = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    QuantityReceived = table.Column<int>(type: "INTEGER", nullable: false),
                    UnitCostSnapshot = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseReceiptItems", x => x.Id);
                    table.CheckConstraint("CK_PurchaseReceiptItems_QuantityReceived", "\"QuantityReceived\" > 0");
                    table.CheckConstraint("CK_PurchaseReceiptItems_UnitCostSnapshot", "\"UnitCostSnapshot\" >= 0");
                    table.ForeignKey(
                        name: "FK_PurchaseReceiptItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseReceiptItems_PurchaseOrderItems_PurchaseOrderItemId",
                        column: x => x.PurchaseOrderItemId,
                        principalTable: "PurchaseOrderItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseReceiptItems_PurchaseReceipts_PurchaseReceiptId",
                        column: x => x.PurchaseReceiptId,
                        principalTable: "PurchaseReceipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_PurchaseOrderId",
                table: "StockMovements",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_PurchaseReceiptId",
                table: "StockMovements",
                column: "PurchaseReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderItems_ProductId",
                table: "PurchaseOrderItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderItems_PurchaseOrderId_ProductId",
                table: "PurchaseOrderItems",
                columns: new[] { "PurchaseOrderId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_CreatedAtUtc",
                table: "PurchaseOrders",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_PurchaseOrderNumber",
                table: "PurchaseOrders",
                column: "PurchaseOrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_Status",
                table: "PurchaseOrders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_SupplierId",
                table: "PurchaseOrders",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReceiptItems_ProductId",
                table: "PurchaseReceiptItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReceiptItems_PurchaseOrderItemId",
                table: "PurchaseReceiptItems",
                column: "PurchaseOrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReceiptItems_PurchaseReceiptId_PurchaseOrderItemId",
                table: "PurchaseReceiptItems",
                columns: new[] { "PurchaseReceiptId", "PurchaseOrderItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReceipts_PurchaseOrderId",
                table: "PurchaseReceipts",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReceipts_ReceiptNumber",
                table: "PurchaseReceipts",
                column: "ReceiptNumber",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_PurchaseOrders_PurchaseOrderId",
                table: "StockMovements",
                column: "PurchaseOrderId",
                principalTable: "PurchaseOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_PurchaseReceipts_PurchaseReceiptId",
                table: "StockMovements",
                column: "PurchaseReceiptId",
                principalTable: "PurchaseReceipts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_PurchaseOrders_PurchaseOrderId",
                table: "StockMovements");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_PurchaseReceipts_PurchaseReceiptId",
                table: "StockMovements");

            migrationBuilder.DropTable(
                name: "PurchaseReceiptItems");

            migrationBuilder.DropTable(
                name: "PurchaseOrderItems");

            migrationBuilder.DropTable(
                name: "PurchaseReceipts");

            migrationBuilder.DropTable(
                name: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_PurchaseOrderId",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_PurchaseReceiptId",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "PurchaseOrderId",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "PurchaseReceiptId",
                table: "StockMovements");
        }
    }
}
