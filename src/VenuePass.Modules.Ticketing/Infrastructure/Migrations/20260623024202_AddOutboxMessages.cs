using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VenuePass.Modules.Ticketing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "ticketing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    occurred_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    type = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    attempt_count = table.Column<int>(type: "int", nullable: false),
                    last_attempted_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    next_attempt_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    processed_on = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    error = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_dispatch",
                schema: "ticketing",
                table: "outbox_messages",
                columns: ["processed_on", "next_attempt_on"]);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_occurred_on",
                schema: "ticketing",
                table: "outbox_messages",
                column: "occurred_on");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "ticketing");
        }
    }
}
