using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrdoWiki.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCharacters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "characters",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    markdown_body = table.Column<string>(type: "text", nullable: false),
                    owner_id = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_characters", x => x.id);
                    table.ForeignKey(
                        name: "fk_characters_users_owner_id",
                        column: x => x.owner_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "character_images",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    character_id = table.Column<Guid>(type: "uuid", nullable: false),
                    media_asset_id = table.Column<Guid>(type: "uuid", nullable: false),
                    caption = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    order_index = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_character_images", x => x.id);
                    table.ForeignKey(
                        name: "fk_character_images_characters_character_id",
                        column: x => x.character_id,
                        principalTable: "characters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_character_images_media_assets_media_asset_id",
                        column: x => x.media_asset_id,
                        principalTable: "media_assets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_character_images_character_id_order_index",
                table: "character_images",
                columns: new[] { "character_id", "order_index" });

            migrationBuilder.CreateIndex(
                name: "ix_character_images_media_asset_id",
                table: "character_images",
                column: "media_asset_id");

            migrationBuilder.CreateIndex(
                name: "ix_characters_owner_id",
                table: "characters",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "ix_characters_slug",
                table: "characters",
                column: "slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "character_images");

            migrationBuilder.DropTable(
                name: "characters");
        }
    }
}
