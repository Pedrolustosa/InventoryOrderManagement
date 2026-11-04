using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryOrderManagement.Infrastructure.Persistence.Migrations
{
        public partial class AddReturnsManagement : Migration
    {
                protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ReturnItemId",
                table: "StockMovements",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReturnRequestId",
                table: "StockMovements",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SupplierReturnedQuantity",
                table: "PurchaseOrderItems",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ReturnedQuantity",
                table: "OrderItems",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ReturnRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ReturnNumber = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    CustomerId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CustomerName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    SupplierId = table.Column<Guid>(type: "TEXT", nullable: true),
                    SupplierName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    OrderId = table.Column<Guid>(type: "TEXT", nullable: true),
                    PurchaseOrderId = table.Column<Guid>(type: "TEXT", nullable: true),
                    SourceReference = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    ReasonDescription = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    EstimatedAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    FinancialTreatment = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    FinancialStatus = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    EstimatedFinancialAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    FinancialTitleId = table.Column<Guid>(type: "TEXT", nullable: true),
                    FinancialNotes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    AuthorizationReference = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    DispatchReference = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ReceivedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    InspectedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AuthorizedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DispatchedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CancelledAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CancellationReason = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReturnRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReturnRequests_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReturnRequests_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReturnRequests_PurchaseOrders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReturnRequests_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReturnItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ReturnRequestId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductSku = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ProductName = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    OrderItemId = table.Column<Guid>(type: "TEXT", nullable: true),
                    PurchaseOrderItemId = table.Column<Guid>(type: "TEXT", nullable: true),
                    SourceQuantity = table.Column<int>(type: "INTEGER", nullable: false),
                    PreviouslyReturnedQuantity = table.Column<int>(type: "INTEGER", nullable: false),
                    RequestedQuantity = table.Column<int>(type: "INTEGER", nullable: false),
                    ReceivedQuantity = table.Column<int>(type: "INTEGER", nullable: false),
                    DispatchedQuantity = table.Column<int>(type: "INTEGER", nullable: false),
                    RestockedQuantity = table.Column<int>(type: "INTEGER", nullable: false),
                    QuarantinedQuantity = table.Column<int>(type: "INTEGER", nullable: false),
                    DiscardedQuantity = table.Column<int>(type: "INTEGER", nullable: false),
                    RejectedQuantity = table.Column<int>(type: "INTEGER", nullable: false),
                    UnitValueSnapshot = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    LineEstimatedAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Condition = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    InspectionNotes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    InspectedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    InspectedByUserId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReturnItems", x => x.Id);
                    table.CheckConstraint("CK_ReturnItems_DiscardedQuantity", "\"DiscardedQuantity\" >= 0");
                    table.CheckConstraint("CK_ReturnItems_DispatchedQuantity", "\"DispatchedQuantity\" >= 0");
                    table.CheckConstraint("CK_ReturnItems_LineEstimatedAmount", "\"LineEstimatedAmount\" >= 0");
                    table.CheckConstraint("CK_ReturnItems_QuarantinedQuantity", "\"QuarantinedQuantity\" >= 0");
                    table.CheckConstraint("CK_ReturnItems_ReceivedQuantity", "\"ReceivedQuantity\" >= 0");
                    table.CheckConstraint("CK_ReturnItems_RejectedQuantity", "\"RejectedQuantity\" >= 0");
                    table.CheckConstraint("CK_ReturnItems_RequestedQuantity", "\"RequestedQuantity\" > 0");
                    table.CheckConstraint("CK_ReturnItems_RestockedQuantity", "\"RestockedQuantity\" >= 0");
                    table.CheckConstraint("CK_ReturnItems_UnitValueSnapshot", "\"UnitValueSnapshot\" >= 0");
                    table.ForeignKey(
                        name: "FK_ReturnItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReturnItems_ReturnRequests_ReturnRequestId",
                        column: x => x.ReturnRequestId,
                        principalTable: "ReturnRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_ReturnItemId",
                table: "StockMovements",
                column: "ReturnItemId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_ReturnRequestId",
                table: "StockMovements",
                column: "ReturnRequestId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PurchaseOrderItems_SupplierReturnedQuantity",
                table: "PurchaseOrderItems",
                sql: "\"SupplierReturnedQuantity\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OrderItems_ReturnedQuantity",
                table: "OrderItems",
                sql: "\"ReturnedQuantity\" >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnItems_OrderItemId",
                table: "ReturnItems",
                column: "OrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnItems_ProductId",
                table: "ReturnItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnItems_PurchaseOrderItemId",
                table: "ReturnItems",
                column: "PurchaseOrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnItems_ReturnRequestId_ProductId",
                table: "ReturnItems",
                columns: new[] { "ReturnRequestId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReturnRequests_CreatedAtUtc",
                table: "ReturnRequests",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnRequests_CustomerId",
                table: "ReturnRequests",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnRequests_OrderId",
                table: "ReturnRequests",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnRequests_PurchaseOrderId",
                table: "ReturnRequests",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnRequests_ReturnNumber",
                table: "ReturnRequests",
                column: "ReturnNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReturnRequests_Status",
                table: "ReturnRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnRequests_SupplierId",
                table: "ReturnRequests",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnRequests_Type",
                table: "ReturnRequests",
                column: "Type");

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_ReturnItems_ReturnItemId",
                table: "StockMovements",
                column: "ReturnItemId",
                principalTable: "ReturnItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_ReturnRequests_ReturnRequestId",
                table: "StockMovements",
                column: "ReturnRequestId",
                principalTable: "ReturnRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

                protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_ReturnItems_ReturnItemId",
                table: "StockMovements");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_ReturnRequests_ReturnRequestId",
                table: "StockMovements");

            migrationBuilder.DropTable(
                name: "ReturnItems");

            migrationBuilder.DropTable(
                name: "ReturnRequests");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_ReturnItemId",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_ReturnRequestId",
                table: "StockMovements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PurchaseOrderItems_SupplierReturnedQuantity",
                table: "PurchaseOrderItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OrderItems_ReturnedQuantity",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "ReturnItemId",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "ReturnRequestId",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "SupplierReturnedQuantity",
                table: "PurchaseOrderItems");

            migrationBuilder.DropColumn(
                name: "ReturnedQuantity",
                table: "OrderItems");
        }
    }
}
