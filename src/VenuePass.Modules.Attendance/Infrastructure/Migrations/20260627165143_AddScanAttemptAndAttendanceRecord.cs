using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VenuePass.Modules.Attendance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddScanAttemptAndAttendanceRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "published_event_references",
                schema: "attendance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    event_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    manifest_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    synced_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_published_event_references", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "attendance_records",
                schema: "attendance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ticket_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ticket_code = table.Column<string>(type: "nchar(16)", fixedLength: true, maxLength: 16, nullable: false),
                    published_event_reference_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    checked_in_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    order_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    order_item_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    inventory_seat_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    general_admission_pool_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attendance_records", x => x.id);
                    table.CheckConstraint("CK_AttendanceRecords_SeatOrPool", "((inventory_seat_id IS NOT NULL AND general_admission_pool_id IS NULL) OR (inventory_seat_id IS NULL AND general_admission_pool_id IS NOT NULL))");
                    table.ForeignKey(
                        name: "FK_attendance_records_published_event_references_published_event_reference_id",
                        column: x => x.published_event_reference_id,
                        principalSchema: "attendance",
                        principalTable: "published_event_references",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "scan_attempts",
                schema: "attendance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    submitted_ticket_code = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    normalized_ticket_code = table.Column<string>(type: "nchar(16)", fixedLength: true, maxLength: 16, nullable: true),
                    outcome = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    rejection_category = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    scanned_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ticket_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    published_event_reference_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scan_attempts", x => x.id);
                    table.ForeignKey(
                        name: "FK_scan_attempts_published_event_references_published_event_reference_id",
                        column: x => x.published_event_reference_id,
                        principalSchema: "attendance",
                        principalTable: "published_event_references",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_attendance_records_event_checked_in_at",
                schema: "attendance",
                table: "attendance_records",
                columns: ["published_event_reference_id", "checked_in_at"]);

            migrationBuilder.CreateIndex(
                name: "IX_attendance_records_ticket_code",
                schema: "attendance",
                table: "attendance_records",
                column: "ticket_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_attendance_records_ticket_id",
                schema: "attendance",
                table: "attendance_records",
                column: "ticket_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_published_event_references_event_id_manifest_id",
                schema: "attendance",
                table: "published_event_references",
                columns: ["event_id", "manifest_id"],
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_scan_attempts_normalized_ticket_code",
                schema: "attendance",
                table: "scan_attempts",
                column: "normalized_ticket_code");

            migrationBuilder.CreateIndex(
                name: "IX_scan_attempts_published_event_reference_id_scanned_at",
                schema: "attendance",
                table: "scan_attempts",
                columns: ["published_event_reference_id", "scanned_at"]);

            migrationBuilder.CreateIndex(
                name: "IX_scan_attempts_ticket_id",
                schema: "attendance",
                table: "scan_attempts",
                column: "ticket_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "attendance_records",
                schema: "attendance");

            migrationBuilder.DropTable(
                name: "scan_attempts",
                schema: "attendance");

            migrationBuilder.DropTable(
                name: "published_event_references",
                schema: "attendance");
        }
    }
}
