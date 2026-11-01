using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryOrderManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryManagement : Migration
    {
        private static readonly Guid UncategorizedCategoryId =
            new("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    NormalizedName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "CreatedAtUtc", "Description", "IsActive", "Name", "NormalizedName", "UpdatedAtUtc" },
                values: new object[]
                {
                    UncategorizedCategoryId,
                    new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    null,
                    true,
                    "Uncategorized",
                    "UNCATEGORIZED",
                    null
                });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_NormalizedName",
                table: "Categories",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                table: "Products",
                type: "TEXT",
                nullable: true);

            migrationBuilder.Sql(
                $"UPDATE Products SET CategoryId = '{UncategorizedCategoryId}' WHERE CategoryId IS NULL;");

            migrationBuilder.AlterColumn<Guid>(
                name: "CategoryId",
                table: "Products",
                type: "TEXT",
                nullable: false,
                defaultValue: UncategorizedCategoryId,
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_CategoryId",
                table: "Products",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Categories_CategoryId",
                table: "Products",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_Categories_CategoryId",
                table: "Products");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Products_CategoryId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Products");
        }
    }
}
