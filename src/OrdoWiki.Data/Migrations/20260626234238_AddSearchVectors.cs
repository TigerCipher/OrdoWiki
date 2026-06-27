using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace OrdoWiki.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSearchVectors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "search_vector",
                table: "wiki_pages",
                type: "tsvector",
                nullable: false,
                computedColumnSql: "setweight(to_tsvector('english', title), 'A') || setweight(to_tsvector('english', coalesce(summary, '')), 'B')",
                stored: true);

            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "search_vector",
                table: "timeline_events",
                type: "tsvector",
                nullable: false,
                computedColumnSql: "setweight(to_tsvector('english', title), 'A') || setweight(to_tsvector('english', coalesce(summary, '')), 'B') || setweight(to_tsvector('english', coalesce(markdown_body, '')), 'C')",
                stored: true);

            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "search_vector",
                table: "page_revisions",
                type: "tsvector",
                nullable: false,
                computedColumnSql: "to_tsvector('english', markdown_body)",
                stored: true);

            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "search_vector",
                table: "characters",
                type: "tsvector",
                nullable: false,
                computedColumnSql: "setweight(to_tsvector('english', name), 'A') || setweight(to_tsvector('english', coalesce(summary, '')), 'B') || setweight(to_tsvector('english', coalesce(markdown_body, '')), 'C')",
                stored: true);

            migrationBuilder.CreateIndex(
                name: "ix_wiki_pages_search_vector",
                table: "wiki_pages",
                column: "search_vector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "ix_timeline_events_search_vector",
                table: "timeline_events",
                column: "search_vector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "ix_page_revisions_search_vector",
                table: "page_revisions",
                column: "search_vector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "ix_characters_search_vector",
                table: "characters",
                column: "search_vector")
                .Annotation("Npgsql:IndexMethod", "GIN");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_wiki_pages_search_vector",
                table: "wiki_pages");

            migrationBuilder.DropIndex(
                name: "ix_timeline_events_search_vector",
                table: "timeline_events");

            migrationBuilder.DropIndex(
                name: "ix_page_revisions_search_vector",
                table: "page_revisions");

            migrationBuilder.DropIndex(
                name: "ix_characters_search_vector",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "search_vector",
                table: "wiki_pages");

            migrationBuilder.DropColumn(
                name: "search_vector",
                table: "timeline_events");

            migrationBuilder.DropColumn(
                name: "search_vector",
                table: "page_revisions");

            migrationBuilder.DropColumn(
                name: "search_vector",
                table: "characters");
        }
    }
}
