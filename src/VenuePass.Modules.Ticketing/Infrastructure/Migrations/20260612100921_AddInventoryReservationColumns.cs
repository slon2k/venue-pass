using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VenuePass.Modules.Ticketing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryReservationColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // available_count was a derived value (capacity - reserved - sold) stored redundantly.
            // Replace it with explicit reserved_count and sold_count columns, both starting at 0.
            migrationBuilder.DropColumn(
                name: "available_count",
                schema: "ticketing",
                table: "inventory_pools");

            migrationBuilder.AddColumn<int>(
                name: "reserved_count",
                schema: "ticketing",
                table: "inventory_pools",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "sold_count",
                schema: "ticketing",
                table: "inventory_pools",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "row_version",
                schema: "ticketing",
                table: "inventory_pools",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "row_version",
                schema: "ticketing",
                table: "inventory_seats",
                type: "rowversion",
                rowVersion: true,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "row_version",
                schema: "ticketing",
                table: "inventory_seats");

            migrationBuilder.DropColumn(
                name: "row_version",
                schema: "ticketing",
                table: "inventory_pools");

            migrationBuilder.DropColumn(
                name: "sold_count",
                schema: "ticketing",
                table: "inventory_pools");

            migrationBuilder.DropColumn(
                name: "reserved_count",
                schema: "ticketing",
                table: "inventory_pools");

            migrationBuilder.AddColumn<int>(
                name: "available_count",
                schema: "ticketing",
                table: "inventory_pools",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
