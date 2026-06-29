using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VenuePass.Modules.Attendance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketProjectionReadModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ticket_projections",
                schema: "attendance",
                columns: table => new
                {
                    ticket_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ticket_code = table.Column<string>(type: "nchar(16)", fixedLength: true, maxLength: 16, nullable: false),
                    status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    published_event_reference_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    order_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    order_item_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    inventory_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    inventory_seat_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    general_admission_pool_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    last_updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ticket_projections", x => x.ticket_id);
                    table.CheckConstraint("CK_ticket_projections_seat_or_pool", "((inventory_seat_id IS NOT NULL AND general_admission_pool_id IS NULL) OR (inventory_seat_id IS NULL AND general_admission_pool_id IS NOT NULL))");
                    table.ForeignKey(
                        name: "FK_ticket_projections_published_event_references_published_event_reference_id",
                        column: x => x.published_event_reference_id,
                        principalSchema: "attendance",
                        principalTable: "published_event_references",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ticket_projections_published_event_reference_id_status",
                schema: "attendance",
                table: "ticket_projections",
                columns: ["published_event_reference_id", "status"]);

            migrationBuilder.CreateIndex(
                name: "IX_ticket_projections_ticket_code",
                schema: "attendance",
                table: "ticket_projections",
                column: "ticket_code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ticket_projections",
                schema: "attendance");
        }
    }
}
