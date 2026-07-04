using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace OrdoWiki.Data.Migrations
{
    /// <inheritdoc />
    public partial class StripHtmlFromSearchVectors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<NpgsqlTsVector>(
                name: "search_vector",
                table: "timeline_events",
                type: "tsvector",
                nullable: false,
                computedColumnSql: "setweight(to_tsvector('english', title), 'A') || setweight(to_tsvector('english', coalesce(summary, '')), 'B') || setweight(to_tsvector('english', regexp_replace(coalesce(markdown_body, ''), '<[^>]+>', ' ', 'g')), 'C')",
                stored: true,
                oldClrType: typeof(NpgsqlTsVector),
                oldType: "tsvector",
                oldComputedColumnSql: "setweight(to_tsvector('english', title), 'A') || setweight(to_tsvector('english', coalesce(summary, '')), 'B') || setweight(to_tsvector('english', coalesce(markdown_body, '')), 'C')",
                oldStored: true);

            migrationBuilder.AlterColumn<NpgsqlTsVector>(
                name: "search_vector",
                table: "page_revisions",
                type: "tsvector",
                nullable: false,
                computedColumnSql: "to_tsvector('english', regexp_replace(markdown_body, '<[^>]+>', ' ', 'g'))",
                stored: true,
                oldClrType: typeof(NpgsqlTsVector),
                oldType: "tsvector",
                oldComputedColumnSql: "to_tsvector('english', markdown_body)",
                oldStored: true);

            migrationBuilder.AlterColumn<NpgsqlTsVector>(
                name: "search_vector",
                table: "characters",
                type: "tsvector",
                nullable: false,
                computedColumnSql: "setweight(to_tsvector('english', name), 'A') || setweight(to_tsvector('english', coalesce(summary, '')), 'B') || setweight(to_tsvector('english', regexp_replace(coalesce(markdown_body, ''), '<[^>]+>', ' ', 'g')), 'C')",
                stored: true,
                oldClrType: typeof(NpgsqlTsVector),
                oldType: "tsvector",
                oldComputedColumnSql: "setweight(to_tsvector('english', name), 'A') || setweight(to_tsvector('english', coalesce(summary, '')), 'B') || setweight(to_tsvector('english', coalesce(markdown_body, '')), 'C')",
                oldStored: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<NpgsqlTsVector>(
                name: "search_vector",
                table: "timeline_events",
                type: "tsvector",
                nullable: false,
                computedColumnSql: "setweight(to_tsvector('english', title), 'A') || setweight(to_tsvector('english', coalesce(summary, '')), 'B') || setweight(to_tsvector('english', coalesce(markdown_body, '')), 'C')",
                stored: true,
                oldClrType: typeof(NpgsqlTsVector),
                oldType: "tsvector",
                oldComputedColumnSql: "setweight(to_tsvector('english', title), 'A') || setweight(to_tsvector('english', coalesce(summary, '')), 'B') || setweight(to_tsvector('english', regexp_replace(coalesce(markdown_body, ''), '<[^>]+>', ' ', 'g')), 'C')",
                oldStored: true);

            migrationBuilder.AlterColumn<NpgsqlTsVector>(
                name: "search_vector",
                table: "page_revisions",
                type: "tsvector",
                nullable: false,
                computedColumnSql: "to_tsvector('english', markdown_body)",
                stored: true,
                oldClrType: typeof(NpgsqlTsVector),
                oldType: "tsvector",
                oldComputedColumnSql: "to_tsvector('english', regexp_replace(markdown_body, '<[^>]+>', ' ', 'g'))",
                oldStored: true);

            migrationBuilder.AlterColumn<NpgsqlTsVector>(
                name: "search_vector",
                table: "characters",
                type: "tsvector",
                nullable: false,
                computedColumnSql: "setweight(to_tsvector('english', name), 'A') || setweight(to_tsvector('english', coalesce(summary, '')), 'B') || setweight(to_tsvector('english', coalesce(markdown_body, '')), 'C')",
                stored: true,
                oldClrType: typeof(NpgsqlTsVector),
                oldType: "tsvector",
                oldComputedColumnSql: "setweight(to_tsvector('english', name), 'A') || setweight(to_tsvector('english', coalesce(summary, '')), 'B') || setweight(to_tsvector('english', regexp_replace(coalesce(markdown_body, ''), '<[^>]+>', ' ', 'g')), 'C')",
                oldStored: true);
        }
    }
}
