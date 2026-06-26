using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddModifierCategoryAndHostControl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "modifier_definitions",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "round");

            migrationBuilder.AddColumn<bool>(
                name: "RequiresHostControl",
                table: "modifier_definitions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "Category",
                value: "round");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                columns: new[] { "Category", "RequiresHostControl" },
                values: new object[] { "result", true });

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "Category",
                value: "preparation");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                columns: new[] { "Category", "RequiresHostControl" },
                values: new object[] { "round", true });

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "Category",
                value: "preparation");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                columns: new[] { "Category", "RequiresHostControl" },
                values: new object[] { "result", true });

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000007"),
                columns: new[] { "Category", "RequiresHostControl" },
                values: new object[] { "round", true });

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000008"),
                columns: new[] { "Category", "RequiresHostControl" },
                values: new object[] { "round", true });

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                columns: new[] { "Category", "RequiresHostControl" },
                values: new object[] { "round", true });

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-00000000000a"),
                columns: new[] { "Category", "RequiresHostControl" },
                values: new object[] { "round", true });

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-00000000000b"),
                columns: new[] { "Category", "RequiresHostControl" },
                values: new object[] { "round", true });

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-00000000000c"),
                columns: new[] { "Category", "RequiresHostControl" },
                values: new object[] { "result", true });

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-00000000000d"),
                columns: new[] { "Category", "RequiresHostControl" },
                values: new object[] { "result", true });

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-00000000000e"),
                columns: new[] { "Category", "RequiresHostControl" },
                values: new object[] { "round", true });

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-00000000000f"),
                columns: new[] { "Category", "RequiresHostControl" },
                values: new object[] { "result", true });

            migrationBuilder.AddCheckConstraint(
                name: "CK_modifier_definitions_category_allowed",
                table: "modifier_definitions",
                sql: "\"Category\" IN ('preparation','round','result')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_modifier_definitions_category_allowed",
                table: "modifier_definitions");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "modifier_definitions");

            migrationBuilder.DropColumn(
                name: "RequiresHostControl",
                table: "modifier_definitions");
        }
    }
}
