using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrdoWiki.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHomePage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "home_pages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    bio_markdown = table.Column<string>(type: "text", nullable: false),
                    featured_log_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_by_id = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_home_pages", x => x.id);
                    table.ForeignKey(
                        name: "fk_home_pages_users_updated_by_id",
                        column: x => x.updated_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_home_pages_wiki_pages_featured_log_id",
                        column: x => x.featured_log_id,
                        principalTable: "wiki_pages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.InsertData(
                table: "home_pages",
                columns: new[] { "id", "bio_markdown", "featured_log_id", "updated_at", "updated_by_id" },
                values: new object[] { new Guid("a1000000-0000-0000-0000-000000000001"), "", null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null });

            migrationBuilder.CreateIndex(
                name: "ix_home_pages_featured_log_id",
                table: "home_pages",
                column: "featured_log_id");

            migrationBuilder.CreateIndex(
                name: "ix_home_pages_updated_by_id",
                table: "home_pages",
                column: "updated_by_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "home_pages");
        }
    }
}
