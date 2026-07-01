using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VenuePass.Modules.Ticketing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketInventoryForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "inventory_id",
                schema: "ticketing",
                table: "tickets",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_tickets_inventory_id",
                schema: "ticketing",
                table: "tickets",
                column: "inventory_id");

            migrationBuilder.AddForeignKey(
                name: "FK_tickets_inventories_inventory_id",
                schema: "ticketing",
                table: "tickets",
                column: "inventory_id",
                principalSchema: "ticketing",
                principalTable: "inventories",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tickets_inventories_inventory_id",
                schema: "ticketing",
                table: "tickets");

            migrationBuilder.DropIndex(
                name: "IX_tickets_inventory_id",
                schema: "ticketing",
                table: "tickets");

            migrationBuilder.DropColumn(
                name: "inventory_id",
                schema: "ticketing",
                table: "tickets");
        }
    }
}
