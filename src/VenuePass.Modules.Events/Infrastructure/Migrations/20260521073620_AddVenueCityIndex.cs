using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VenuePass.Modules.Events.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVenueCityIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "address",
                schema: "events",
                table: "venues",
                newName: "street_address");

            migrationBuilder.CreateIndex(
                name: "UX_venues_name_city",
                schema: "events",
                table: "venues",
                columns: ["name", "city"],
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "street_address",
                schema: "events",
                table: "venues",
                newName: "address");

            migrationBuilder.DropIndex(
                name: "UX_venues_name_city",
                schema: "events",
                table: "venues");
        }
    }
}
