using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryOrderManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CustomerType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    TaxIdentifier = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    NormalizedTaxIdentifier = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 254, nullable: true),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    AddressPostalCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    AddressStreet = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    AddressNumber = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    AddressComplement = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    AddressNeighborhood = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    AddressCity = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    AddressState = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    AddressCountry = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Customers_NormalizedTaxIdentifier",
                table: "Customers",
                column: "NormalizedTaxIdentifier",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Customers");
        }
    }
}
