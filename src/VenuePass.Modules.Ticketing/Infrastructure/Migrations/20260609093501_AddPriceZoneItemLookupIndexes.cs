using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VenuePass.Modules.Ticketing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPriceZoneItemLookupIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_offer_price_zone_inventory_seat_items_inventory_seat_id",
                schema: "ticketing",
                table: "offer_price_zone_inventory_seat_items",
                column: "inventory_seat_id");

            migrationBuilder.CreateIndex(
                name: "IX_offer_price_zone_general_admission_pool_items_general_admission_pool_id",
                schema: "ticketing",
                table: "offer_price_zone_general_admission_pool_items",
                column: "general_admission_pool_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_offer_price_zone_inventory_seat_items_inventory_seat_id",
                schema: "ticketing",
                table: "offer_price_zone_inventory_seat_items");

            migrationBuilder.DropIndex(
                name: "IX_offer_price_zone_general_admission_pool_items_general_admission_pool_id",
                schema: "ticketing",
                table: "offer_price_zone_general_admission_pool_items");
        }
    }
}
