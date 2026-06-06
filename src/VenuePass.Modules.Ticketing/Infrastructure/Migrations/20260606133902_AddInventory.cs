using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VenuePass.Modules.Ticketing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "inventories",
                schema: "ticketing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    event_reference_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventories", x => x.id);
                    table.ForeignKey(
                        name: "FK_inventories_published_event_references_event_reference_id",
                        column: x => x.event_reference_id,
                        principalSchema: "ticketing",
                        principalTable: "published_event_references",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "inventory_pools",
                schema: "ticketing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    source_area_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    capacity = table.Column<int>(type: "int", nullable: false),
                    available_count = table.Column<int>(type: "int", nullable: false),
                    inventory_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_pools", x => x.id);
                    table.ForeignKey(
                        name: "FK_inventory_pools_inventories_inventory_id",
                        column: x => x.inventory_id,
                        principalSchema: "ticketing",
                        principalTable: "inventories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "inventory_seats",
                schema: "ticketing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    source_seat_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    section = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    row = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    seat = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    availability = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    inventory_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_seats", x => x.id);
                    table.ForeignKey(
                        name: "FK_inventory_seats_inventories_inventory_id",
                        column: x => x.inventory_id,
                        principalSchema: "ticketing",
                        principalTable: "inventories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_inventories_event_reference_id",
                schema: "ticketing",
                table: "inventories",
                column: "event_reference_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_pools_inventory_id",
                schema: "ticketing",
                table: "inventory_pools",
                column: "inventory_id");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_seats_inventory_id",
                schema: "ticketing",
                table: "inventory_seats",
                column: "inventory_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inventory_pools",
                schema: "ticketing");

            migrationBuilder.DropTable(
                name: "inventory_seats",
                schema: "ticketing");

            migrationBuilder.DropTable(
                name: "inventories",
                schema: "ticketing");
        }
    }
}
