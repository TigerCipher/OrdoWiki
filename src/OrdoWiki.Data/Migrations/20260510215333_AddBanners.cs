using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace OrdoWiki.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBanners : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "banners",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slot_index = table.Column<int>(type: "integer", nullable: false),
                    media_asset_id = table.Column<Guid>(type: "uuid", nullable: true),
                    alt = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    link_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_by_id = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_banners", x => x.id);
                    table.ForeignKey(
                        name: "fk_banners_media_assets_media_asset_id",
                        column: x => x.media_asset_id,
                        principalTable: "media_assets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_banners_users_updated_by_id",
                        column: x => x.updated_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.InsertData(
                table: "banners",
                columns: new[] { "id", "alt", "link_url", "media_asset_id", "slot_index", "updated_at", "updated_by_id" },
                values: new object[,]
                {
                    { new Guid("b1000000-0000-0000-0000-000000000001"), null, null, null, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { new Guid("b1000000-0000-0000-0000-000000000002"), null, null, null, 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { new Guid("b1000000-0000-0000-0000-000000000003"), null, null, null, 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { new Guid("b1000000-0000-0000-0000-000000000004"), null, null, null, 4, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null }
                });

            migrationBuilder.CreateIndex(
                name: "ix_banners_media_asset_id",
                table: "banners",
                column: "media_asset_id");

            migrationBuilder.CreateIndex(
                name: "ix_banners_slot_index",
                table: "banners",
                column: "slot_index",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_banners_updated_by_id",
                table: "banners",
                column: "updated_by_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "banners");
        }
    }
}
