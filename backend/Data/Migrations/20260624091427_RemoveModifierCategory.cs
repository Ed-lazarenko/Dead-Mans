using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveModifierCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "modifier_definitions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "modifier_definitions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "Category",
                value: "movement_restriction");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "Category",
                value: "score");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "Category",
                value: "loadout");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                column: "Category",
                value: "combat_rule");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "Category",
                value: "loadout");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                column: "Category",
                value: "score");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000007"),
                column: "Category",
                value: "mentor_intervention");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000008"),
                column: "Category",
                value: "behavior_rule");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                column: "Category",
                value: "mentor_intervention");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-00000000000a"),
                column: "Category",
                value: "communication_rule");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-00000000000b"),
                column: "Category",
                value: "mentor_intervention");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-00000000000c"),
                column: "Category",
                value: "mentor_intervention");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-00000000000d"),
                column: "Category",
                value: "mentor_intervention");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-00000000000e"),
                column: "Category",
                value: "combat_rule");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-00000000000f"),
                column: "Category",
                value: "score");
        }
    }
}
