using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VenuePass.Modules.Events.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEventAndManifest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "events",
                schema: "events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    manifest_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    venue_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    event_date = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    state = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_events_venues_venue_id",
                        column: x => x.venue_id,
                        principalSchema: "events",
                        principalTable: "venues",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "manifests",
                schema: "events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    venue_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    event_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_frozen = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_manifests", x => x.id);
                    table.ForeignKey(
                        name: "FK_manifests_events_event_id",
                        column: x => x.event_id,
                        principalSchema: "events",
                        principalTable: "events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_manifests_venues_venue_id",
                        column: x => x.venue_id,
                        principalSchema: "events",
                        principalTable: "venues",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "manifest_general_admission_areas",
                schema: "events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    capacity = table.Column<int>(type: "int", nullable: false),
                    manifest_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_manifest_general_admission_areas", x => x.id);
                    table.ForeignKey(
                        name: "FK_manifest_general_admission_areas_manifests_manifest_id",
                        column: x => x.manifest_id,
                        principalSchema: "events",
                        principalTable: "manifests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "manifest_sections",
                schema: "events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    manifest_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_manifest_sections", x => x.id);
                    table.ForeignKey(
                        name: "FK_manifest_sections_manifests_manifest_id",
                        column: x => x.manifest_id,
                        principalSchema: "events",
                        principalTable: "manifests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "manifest_section_rows",
                schema: "events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    label = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    section_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_manifest_section_rows", x => x.id);
                    table.ForeignKey(
                        name: "FK_manifest_section_rows_manifest_sections_section_id",
                        column: x => x.section_id,
                        principalSchema: "events",
                        principalTable: "manifest_sections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "manifest_section_row_seats",
                schema: "events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    label = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    row_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_manifest_section_row_seats", x => x.id);
                    table.ForeignKey(
                        name: "FK_manifest_section_row_seats_manifest_section_rows_row_id",
                        column: x => x.row_id,
                        principalSchema: "events",
                        principalTable: "manifest_section_rows",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_events_venue_id_event_date",
                schema: "events",
                table: "events",
                columns: new[] { "venue_id", "event_date" });

            migrationBuilder.CreateIndex(
                name: "UX_events_manifest_id",
                schema: "events",
                table: "events",
                column: "manifest_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_manifest_general_admission_areas_manifest_id",
                schema: "events",
                table: "manifest_general_admission_areas",
                column: "manifest_id");

            migrationBuilder.CreateIndex(
                name: "IX_manifest_section_row_seats_row_id",
                schema: "events",
                table: "manifest_section_row_seats",
                column: "row_id");

            migrationBuilder.CreateIndex(
                name: "IX_manifest_section_rows_section_id",
                schema: "events",
                table: "manifest_section_rows",
                column: "section_id");

            migrationBuilder.CreateIndex(
                name: "IX_manifest_sections_manifest_id",
                schema: "events",
                table: "manifest_sections",
                column: "manifest_id");

            migrationBuilder.CreateIndex(
                name: "IX_manifests_venue_id",
                schema: "events",
                table: "manifests",
                column: "venue_id");

            migrationBuilder.CreateIndex(
                name: "UX_manifests_event_id",
                schema: "events",
                table: "manifests",
                column: "event_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "manifest_general_admission_areas",
                schema: "events");

            migrationBuilder.DropTable(
                name: "manifest_section_row_seats",
                schema: "events");

            migrationBuilder.DropTable(
                name: "manifest_section_rows",
                schema: "events");

            migrationBuilder.DropTable(
                name: "manifest_sections",
                schema: "events");

            migrationBuilder.DropTable(
                name: "manifests",
                schema: "events");

            migrationBuilder.DropTable(
                name: "events",
                schema: "events");
        }
    }
}
