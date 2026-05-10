using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace OrdoWiki.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCalendarConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "mando_eras",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    short_code = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    anchor_year = table.Column<int>(type: "integer", nullable: false),
                    direction = table.Column<int>(type: "integer", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mando_eras", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "mando_months",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    month_index = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mando_months", x => x.id);
                });

            migrationBuilder.InsertData(
                table: "mando_eras",
                columns: new[] { "id", "anchor_year", "direction", "name", "short_code", "sort_order" },
                values: new object[,]
                {
                    { new Guid("e1a0e1a0-0000-0000-0000-000000000001"), 0, 1, "Before Civil War", "BCW", 0 },
                    { new Guid("e1a0e1a0-0000-0000-0000-000000000002"), 0, 0, "After Civil War", "ACW", 1 }
                });

            migrationBuilder.InsertData(
                table: "mando_months",
                columns: new[] { "id", "month_index", "name" },
                values: new object[,]
                {
                    { new Guid("a1c0c0a0-0001-0000-0000-000000000001"), 1, "Vhett'yc" },
                    { new Guid("a1c0c0a0-0002-0000-0000-000000000002"), 2, "Beskar'yc" },
                    { new Guid("a1c0c0a0-0003-0000-0000-000000000003"), 3, "Verd'yc" },
                    { new Guid("a1c0c0a0-0004-0000-0000-000000000004"), 4, "Kote'yc" },
                    { new Guid("a1c0c0a0-0005-0000-0000-000000000005"), 5, "Manda'yc" },
                    { new Guid("a1c0c0a0-0006-0000-0000-000000000006"), 6, "Aliit'yc" },
                    { new Guid("a1c0c0a0-0007-0000-0000-000000000007"), 7, "Ka'ra'yc" },
                    { new Guid("a1c0c0a0-0008-0000-0000-000000000008"), 8, "Aay'han'yc" },
                    { new Guid("a1c0c0a0-0009-0000-0000-000000000009"), 9, "Akaan'yc" },
                    { new Guid("a1c0c0a0-0010-0000-0000-000000000010"), 10, "Mando'yc" },
                    { new Guid("a1c0c0a0-0011-0000-0000-000000000011"), 11, "Resol'yc" },
                    { new Guid("a1c0c0a0-0012-0000-0000-000000000012"), 12, "Buir'yc" }
                });

            migrationBuilder.CreateIndex(
                name: "ix_mando_eras_anchor_year",
                table: "mando_eras",
                column: "anchor_year");

            migrationBuilder.CreateIndex(
                name: "ix_mando_eras_short_code",
                table: "mando_eras",
                column: "short_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_mando_months_month_index",
                table: "mando_months",
                column: "month_index",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "mando_eras");

            migrationBuilder.DropTable(
                name: "mando_months");
        }
    }
}
