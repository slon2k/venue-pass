using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VenuePass.Modules.Ticketing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixPriceZoneSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The domain model was refactored so that price lives on the price zone (offer_price_zones)
            // rather than on each individual seat/pool item. The EF model snapshot already reflects
            // this, but the prior migrations never applied the corresponding DDL changes.

            migrationBuilder.AddColumn<decimal>(
                name: "price",
                schema: "ticketing",
                table: "offer_price_zones",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.DropColumn(
                name: "price",
                schema: "ticketing",
                table: "offer_price_zone_inventory_seat_items");

            migrationBuilder.DropColumn(
                name: "price",
                schema: "ticketing",
                table: "offer_price_zone_general_admission_pool_items");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "price",
                schema: "ticketing",
                table: "offer_price_zones");

            migrationBuilder.AddColumn<decimal>(
                name: "price",
                schema: "ticketing",
                table: "offer_price_zone_inventory_seat_items",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "price",
                schema: "ticketing",
                table: "offer_price_zone_general_admission_pool_items",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }
    }
}
