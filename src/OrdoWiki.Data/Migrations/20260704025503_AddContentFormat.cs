using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrdoWiki.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddContentFormat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "content_format",
                table: "timeline_events",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "content_format",
                table: "page_revisions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "content_format",
                table: "characters",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "content_format",
                table: "timeline_events");

            migrationBuilder.DropColumn(
                name: "content_format",
                table: "page_revisions");

            migrationBuilder.DropColumn(
                name: "content_format",
                table: "characters");
        }
    }
}
