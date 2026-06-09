using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VenuePass.Modules.Ticketing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenamePriceLevelToPriceZoneTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "offer_price_levels",
                schema: "ticketing",
                newName: "offer_price_zones",
                newSchema: "ticketing");

            migrationBuilder.RenameTable(
                name: "offer_price_level_inventory_seat_items",
                schema: "ticketing",
                newName: "offer_price_zone_inventory_seat_items",
                newSchema: "ticketing");

            migrationBuilder.RenameTable(
                name: "offer_price_level_general_admission_pool_items",
                schema: "ticketing",
                newName: "offer_price_zone_general_admission_pool_items",
                newSchema: "ticketing");

            migrationBuilder.RenameColumn(
                name: "price_level_id",
                schema: "ticketing",
                table: "offer_price_zone_inventory_seat_items",
                newName: "price_zone_id");

            migrationBuilder.RenameColumn(
                name: "price_level_id",
                schema: "ticketing",
                table: "offer_price_zone_general_admission_pool_items",
                newName: "price_zone_id");

            migrationBuilder.RenameIndex(
                name: "ux_offer_price_levels_offer_id_name",
                schema: "ticketing",
                table: "offer_price_zones",
                newName: "ux_offer_price_zones_offer_id_name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "ux_offer_price_zones_offer_id_name",
                schema: "ticketing",
                table: "offer_price_zones",
                newName: "ux_offer_price_levels_offer_id_name");

            migrationBuilder.RenameColumn(
                name: "price_zone_id",
                schema: "ticketing",
                table: "offer_price_zone_inventory_seat_items",
                newName: "price_level_id");

            migrationBuilder.RenameColumn(
                name: "price_zone_id",
                schema: "ticketing",
                table: "offer_price_zone_general_admission_pool_items",
                newName: "price_level_id");

            migrationBuilder.RenameTable(
                name: "offer_price_zone_general_admission_pool_items",
                schema: "ticketing",
                newName: "offer_price_level_general_admission_pool_items",
                newSchema: "ticketing");

            migrationBuilder.RenameTable(
                name: "offer_price_zone_inventory_seat_items",
                schema: "ticketing",
                newName: "offer_price_level_inventory_seat_items",
                newSchema: "ticketing");

            migrationBuilder.RenameTable(
                name: "offer_price_zones",
                schema: "ticketing",
                newName: "offer_price_levels",
                newSchema: "ticketing");
        }
    }
}
