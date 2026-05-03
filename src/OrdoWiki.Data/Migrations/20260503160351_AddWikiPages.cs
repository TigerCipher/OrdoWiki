using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrdoWiki.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWikiPages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "page_revisions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    markdown_body = table.Column<string>(type: "text", nullable: false),
                    edit_summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    edited_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    edited_by_id = table.Column<string>(type: "text", nullable: false),
                    page_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_page_revisions", x => x.id);
                    table.ForeignKey(
                        name: "fk_page_revisions_users_edited_by_id",
                        column: x => x.edited_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "wiki_pages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    current_revision_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_wiki_pages", x => x.id);
                    table.ForeignKey(
                        name: "fk_wiki_pages_page_revisions_current_revision_id",
                        column: x => x.current_revision_id,
                        principalTable: "page_revisions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_wiki_pages_users_created_by_id",
                        column: x => x.created_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_page_revisions_edited_by_id",
                table: "page_revisions",
                column: "edited_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_page_revisions_page_id",
                table: "page_revisions",
                column: "page_id");

            migrationBuilder.CreateIndex(
                name: "ix_wiki_pages_created_by_id",
                table: "wiki_pages",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_wiki_pages_current_revision_id",
                table: "wiki_pages",
                column: "current_revision_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_wiki_pages_slug",
                table: "wiki_pages",
                column: "slug",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_page_revisions_wiki_pages_page_id",
                table: "page_revisions",
                column: "page_id",
                principalTable: "wiki_pages",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_page_revisions_wiki_pages_page_id",
                table: "page_revisions");

            migrationBuilder.DropTable(
                name: "wiki_pages");

            migrationBuilder.DropTable(
                name: "page_revisions");
        }
    }
}
