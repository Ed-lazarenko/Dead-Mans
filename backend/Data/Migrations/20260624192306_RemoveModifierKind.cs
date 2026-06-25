using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveModifierKind : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_modifier_definitions_kind_allowed",
                table: "modifier_definitions");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "modifier_definitions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Kind",
                table: "modifier_definitions",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "Kind",
                value: "active");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "Kind",
                value: "active");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "Kind",
                value: "active");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                column: "Kind",
                value: "active");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "Kind",
                value: "active");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                column: "Kind",
                value: "active");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000007"),
                column: "Kind",
                value: "active");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000008"),
                column: "Kind",
                value: "active");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                column: "Kind",
                value: "active");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-00000000000a"),
                column: "Kind",
                value: "active");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-00000000000b"),
                column: "Kind",
                value: "active");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-00000000000c"),
                column: "Kind",
                value: "active");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-00000000000d"),
                column: "Kind",
                value: "active");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-00000000000e"),
                column: "Kind",
                value: "active");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-00000000000f"),
                column: "Kind",
                value: "active");

            migrationBuilder.AddCheckConstraint(
                name: "CK_modifier_definitions_kind_allowed",
                table: "modifier_definitions",
                sql: "\"Kind\" IN ('active','passive')");
        }
    }
}
