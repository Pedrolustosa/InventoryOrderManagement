using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryOrderManagement.Infrastructure.Persistence.Migrations
{
        public partial class AddShippingManagement : Migration
    {
                protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Shipments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OrderId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OrderNumber = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    CustomerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CustomerName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    CarrierName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    TrackingCode = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    FailureReason = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ShippedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeliveredAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shipments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Shipments_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Shipments_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ShipmentStatusHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ShipmentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PreviousStatus = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    NewStatus = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ChangedByUserId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShipmentStatusHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShipmentStatusHistories_Shipments_ShipmentId",
                        column: x => x.ShipmentId,
                        principalTable: "Shipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_CreatedAtUtc",
                table: "Shipments",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_CustomerId",
                table: "Shipments",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_OrderId",
                table: "Shipments",
                column: "OrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_Status",
                table: "Shipments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_TrackingCode",
                table: "Shipments",
                column: "TrackingCode");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentStatusHistories_OccurredAtUtc",
                table: "ShipmentStatusHistories",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentStatusHistories_ShipmentId",
                table: "ShipmentStatusHistories",
                column: "ShipmentId");
        }

                protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShipmentStatusHistories");

            migrationBuilder.DropTable(
                name: "Shipments");
        }
    }
}
