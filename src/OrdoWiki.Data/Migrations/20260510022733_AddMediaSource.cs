using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrdoWiki.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMediaSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "source_id",
                table: "media_assets",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "source_type",
                table: "media_assets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "ix_media_assets_source_type_uploaded_at",
                table: "media_assets",
                columns: new[] { "source_type", "uploaded_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_media_assets_source_type_uploaded_at",
                table: "media_assets");

            migrationBuilder.DropColumn(
                name: "source_id",
                table: "media_assets");

            migrationBuilder.DropColumn(
                name: "source_type",
                table: "media_assets");
        }
    }
}
