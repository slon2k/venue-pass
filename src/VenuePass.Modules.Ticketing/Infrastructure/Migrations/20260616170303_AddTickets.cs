using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VenuePass.Modules.Ticketing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTickets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "created_at",
                schema: "ticketing",
                table: "reservations",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.CreateTable(
                name: "tickets",
                schema: "ticketing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    order_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    order_item_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ticket_code = table.Column<string>(type: "nchar(16)", fixedLength: true, maxLength: 16, nullable: false),
                    inventory_seat_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    general_admission_pool_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tickets", x => x.id);
                    table.CheckConstraint("CK_tickets_exactly_one_target", "([inventory_seat_id] IS NOT NULL AND [general_admission_pool_id] IS NULL) OR ([inventory_seat_id] IS NULL AND [general_admission_pool_id] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_tickets_orders_order_id",
                        column: x => x.order_id,
                        principalSchema: "ticketing",
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tickets_order_id",
                schema: "ticketing",
                table: "tickets",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_tickets_ticket_code",
                schema: "ticketing",
                table: "tickets",
                column: "ticket_code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tickets",
                schema: "ticketing");

            migrationBuilder.DropColumn(
                name: "created_at",
                schema: "ticketing",
                table: "reservations");
        }
    }
}
