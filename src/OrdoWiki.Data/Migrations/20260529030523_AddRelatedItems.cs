using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrdoWiki.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRelatedItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "related_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_kind = table.Column<int>(type: "integer", nullable: false),
                    source_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_kind = table.Column<int>(type: "integer", nullable: false),
                    target_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_related_items", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_related_items_source_kind_source_id_target_kind_target_id",
                table: "related_items",
                columns: new[] { "source_kind", "source_id", "target_kind", "target_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_related_items_target_kind_target_id",
                table: "related_items",
                columns: new[] { "target_kind", "target_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "related_items");
        }
    }
}
