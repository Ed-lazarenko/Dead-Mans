using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveModifierTier : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Tier",
                table: "modifier_definitions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Tier",
                table: "modifier_definitions",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "Tier",
                value: "low");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "Tier",
                value: "low");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "Tier",
                value: "low");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                column: "Tier",
                value: "low");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "Tier",
                value: "low");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                column: "Tier",
                value: "low");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000007"),
                column: "Tier",
                value: "mid");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000008"),
                column: "Tier",
                value: "mid");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                column: "Tier",
                value: "mid");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-00000000000a"),
                column: "Tier",
                value: "high");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-00000000000b"),
                column: "Tier",
                value: "high");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-00000000000c"),
                column: "Tier",
                value: "high");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-00000000000d"),
                column: "Tier",
                value: "high");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-00000000000e"),
                column: "Tier",
                value: "high");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-00000000000f"),
                column: "Tier",
                value: "high");
        }
    }
}
