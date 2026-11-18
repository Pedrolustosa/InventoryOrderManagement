using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryOrderManagement.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Data-only migration: Orders.Ship and Orders.Deliver no longer exist (orders ship and deliver through their
    /// shipment), so their claims are removed from every stored role. Other permission claims are not touched.
    /// </summary>
    public partial class RemoveLegacyOrderShippingPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM "AspNetRoleClaims"
                WHERE "ClaimType" = 'permission'
                  AND "ClaimValue" IN ('Orders.Ship', 'Orders.Deliver');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty: which roles held the removed permissions is not known any more, and
            // granting them back blindly would widen access. The endpoints behind them no longer exist.
        }
    }
}
