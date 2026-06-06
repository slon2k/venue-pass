using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VenuePass.Modules.Ticketing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPublishedEventUniqueEventIdIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_published_event_references_event_id",
                schema: "ticketing",
                table: "published_event_references",
                column: "event_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_published_event_references_event_id",
                schema: "ticketing",
                table: "published_event_references");
        }
    }
}
