using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrdoWiki.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSiteTheme : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "custom_theme_variables",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_custom_theme_variables", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "site_themes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    light_palette_json = table.Column<string>(type: "jsonb", nullable: false),
                    dark_palette_json = table.Column<string>(type: "jsonb", nullable: false),
                    custom_values_json = table.Column<string>(type: "jsonb", nullable: false),
                    light_background_asset_id = table.Column<Guid>(type: "uuid", nullable: true),
                    dark_background_asset_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_by_id = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_site_themes", x => x.id);
                    table.ForeignKey(
                        name: "fk_site_themes_media_assets_dark_background_asset_id",
                        column: x => x.dark_background_asset_id,
                        principalTable: "media_assets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_site_themes_media_assets_light_background_asset_id",
                        column: x => x.light_background_asset_id,
                        principalTable: "media_assets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_site_themes_users_updated_by_id",
                        column: x => x.updated_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.InsertData(
                table: "site_themes",
                columns: new[] { "id", "custom_values_json", "dark_background_asset_id", "dark_palette_json", "light_background_asset_id", "light_palette_json", "updated_at", "updated_by_id" },
                values: new object[] { new Guid("51000000-0000-0000-0000-000000000001"), "{}", null, "{}", null, "{}", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null });

            migrationBuilder.CreateIndex(
                name: "ix_custom_theme_variables_name",
                table: "custom_theme_variables",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_site_themes_dark_background_asset_id",
                table: "site_themes",
                column: "dark_background_asset_id");

            migrationBuilder.CreateIndex(
                name: "ix_site_themes_light_background_asset_id",
                table: "site_themes",
                column: "light_background_asset_id");

            migrationBuilder.CreateIndex(
                name: "ix_site_themes_updated_by_id",
                table: "site_themes",
                column: "updated_by_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "custom_theme_variables");

            migrationBuilder.DropTable(
                name: "site_themes");
        }
    }
}
