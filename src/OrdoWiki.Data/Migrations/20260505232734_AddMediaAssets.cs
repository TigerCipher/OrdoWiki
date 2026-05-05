using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrdoWiki.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMediaAssets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "media_assets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    storage_path = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    original_name = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    width = table.Column<int>(type: "integer", nullable: true),
                    height = table.Column<int>(type: "integer", nullable: true),
                    uploaded_by_id = table.Column<string>(type: "text", nullable: false),
                    uploaded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_media_assets", x => x.id);
                    table.ForeignKey(
                        name: "fk_media_assets_users_uploaded_by_id",
                        column: x => x.uploaded_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_media_assets_uploaded_at",
                table: "media_assets",
                column: "uploaded_at");

            migrationBuilder.CreateIndex(
                name: "ix_media_assets_uploaded_by_id",
                table: "media_assets",
                column: "uploaded_by_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "media_assets");
        }
    }
}
