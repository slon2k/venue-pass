using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VenuePass.Modules.Ticketing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOffer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "offers",
                schema: "ticketing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    inventory_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    currency = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: false),
                    status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    sales_end = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    sales_start = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_offers", x => x.id);
                    table.ForeignKey(
                        name: "FK_offers_inventories_inventory_id",
                        column: x => x.inventory_id,
                        principalSchema: "ticketing",
                        principalTable: "inventories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "offer_price_levels",
                schema: "ticketing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    offer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_offer_price_levels", x => x.id);
                    table.ForeignKey(
                        name: "FK_offer_price_levels_offers_offer_id",
                        column: x => x.offer_id,
                        principalSchema: "ticketing",
                        principalTable: "offers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "offer_price_level_general_admission_pool_items",
                schema: "ticketing",
                columns: table => new
                {
                    general_admission_pool_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    price_level_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_offer_price_level_general_admission_pool_items", x => new { x.price_level_id, x.general_admission_pool_id });
                    table.ForeignKey(
                        name: "FK_offer_price_level_general_admission_pool_items_offer_price_levels_price_level_id",
                        column: x => x.price_level_id,
                        principalSchema: "ticketing",
                        principalTable: "offer_price_levels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "offer_price_level_inventory_seat_items",
                schema: "ticketing",
                columns: table => new
                {
                    inventory_seat_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    price_level_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_offer_price_level_inventory_seat_items", x => new { x.price_level_id, x.inventory_seat_id });
                    table.ForeignKey(
                        name: "FK_offer_price_level_inventory_seat_items_offer_price_levels_price_level_id",
                        column: x => x.price_level_id,
                        principalSchema: "ticketing",
                        principalTable: "offer_price_levels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ux_offer_price_levels_offer_id_name",
                schema: "ticketing",
                table: "offer_price_levels",
                columns: ["offer_id", "name"],
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_offers_inventory_id",
                schema: "ticketing",
                table: "offers",
                column: "inventory_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "offer_price_level_general_admission_pool_items",
                schema: "ticketing");

            migrationBuilder.DropTable(
                name: "offer_price_level_inventory_seat_items",
                schema: "ticketing");

            migrationBuilder.DropTable(
                name: "offer_price_levels",
                schema: "ticketing");

            migrationBuilder.DropTable(
                name: "offers",
                schema: "ticketing");
        }
    }
}
