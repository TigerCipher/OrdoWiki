using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrdoWiki.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTimelineEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "timeline_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    markdown_body = table.Column<string>(type: "text", nullable: true),
                    epoch_day_number = table.Column<long>(type: "bigint", nullable: false),
                    mando_year = table.Column<int>(type: "integer", nullable: false),
                    mando_month = table.Column<int>(type: "integer", nullable: true),
                    mando_day = table.Column<int>(type: "integer", nullable: true),
                    display_override = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    created_by_id = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_timeline_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_timeline_events_users_created_by_id",
                        column: x => x.created_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_timeline_events_created_by_id",
                table: "timeline_events",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_timeline_events_epoch_day_number",
                table: "timeline_events",
                column: "epoch_day_number");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "timeline_events");
        }
    }
}
