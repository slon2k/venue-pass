using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VenuePass.Modules.Events.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CreateManifestTemplateConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "manifest_templates",
                schema: "events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    venue_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_manifest_templates", x => x.id);
                    table.ForeignKey(
                        name: "FK_manifest_templates_venues_venue_id",
                        column: x => x.venue_id,
                        principalSchema: "events",
                        principalTable: "venues",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "manifest_template_general_admission_areas",
                schema: "events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    capacity = table.Column<int>(type: "int", nullable: false),
                    manifest_template_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_manifest_template_general_admission_areas", x => x.id);
                    table.ForeignKey(
                        name: "FK_manifest_template_general_admission_areas_manifest_templates_manifest_template_id",
                        column: x => x.manifest_template_id,
                        principalSchema: "events",
                        principalTable: "manifest_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "manifest_template_sections",
                schema: "events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    manifest_template_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_manifest_template_sections", x => x.id);
                    table.ForeignKey(
                        name: "FK_manifest_template_sections_manifest_templates_manifest_template_id",
                        column: x => x.manifest_template_id,
                        principalSchema: "events",
                        principalTable: "manifest_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "manifest_template_section_rows",
                schema: "events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    label = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    section_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_manifest_template_section_rows", x => x.id);
                    table.ForeignKey(
                        name: "FK_manifest_template_section_rows_manifest_template_sections_section_id",
                        column: x => x.section_id,
                        principalSchema: "events",
                        principalTable: "manifest_template_sections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "manifest_template_section_row_seats",
                schema: "events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    label = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    row_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_manifest_template_section_row_seats", x => x.id);
                    table.ForeignKey(
                        name: "FK_manifest_template_section_row_seats_manifest_template_section_rows_row_id",
                        column: x => x.row_id,
                        principalSchema: "events",
                        principalTable: "manifest_template_section_rows",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_manifest_template_general_admission_areas_manifest_template_id",
                schema: "events",
                table: "manifest_template_general_admission_areas",
                column: "manifest_template_id");

            migrationBuilder.CreateIndex(
                name: "IX_manifest_template_section_row_seats_row_id",
                schema: "events",
                table: "manifest_template_section_row_seats",
                column: "row_id");

            migrationBuilder.CreateIndex(
                name: "IX_manifest_template_section_rows_section_id",
                schema: "events",
                table: "manifest_template_section_rows",
                column: "section_id");

            migrationBuilder.CreateIndex(
                name: "IX_manifest_template_sections_manifest_template_id",
                schema: "events",
                table: "manifest_template_sections",
                column: "manifest_template_id");

            migrationBuilder.CreateIndex(
                name: "IX_manifest_templates_venue_id",
                schema: "events",
                table: "manifest_templates",
                column: "venue_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "manifest_template_general_admission_areas",
                schema: "events");

            migrationBuilder.DropTable(
                name: "manifest_template_section_row_seats",
                schema: "events");

            migrationBuilder.DropTable(
                name: "manifest_template_section_rows",
                schema: "events");

            migrationBuilder.DropTable(
                name: "manifest_template_sections",
                schema: "events");

            migrationBuilder.DropTable(
                name: "manifest_templates",
                schema: "events");
        }
    }
}
