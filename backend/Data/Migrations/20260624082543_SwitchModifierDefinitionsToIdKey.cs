using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class SwitchModifierDefinitionsToIdKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""CREATE EXTENSION IF NOT EXISTS "pgcrypto";""");

            migrationBuilder.DropForeignKey(
                name: "FK_game_active_modifiers_modifier_definitions_ModifierCode",
                table: "game_active_modifiers");

            migrationBuilder.DropForeignKey(
                name: "FK_game_modifier_selections_modifier_definitions_ModifierCode",
                table: "game_modifier_selections");

            migrationBuilder.DropForeignKey(
                name: "FK_modifier_conflicts_modifier_definitions_ConflictsWithModifi~",
                table: "modifier_conflicts");

            migrationBuilder.DropForeignKey(
                name: "FK_modifier_conflicts_modifier_definitions_ModifierCode",
                table: "modifier_conflicts");

            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "modifier_definitions",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE modifier_definitions
                SET "Id" = CASE "Code"
                    WHEN 'chirik' THEN '10000000-0000-0000-0000-000000000001'::uuid
                    WHEN 'zhazhda' THEN '10000000-0000-0000-0000-000000000002'::uuid
                    WHEN 'rashodnik' THEN '10000000-0000-0000-0000-000000000003'::uuid
                    WHEN 'trupy' THEN '10000000-0000-0000-0000-000000000004'::uuid
                    WHEN 'navyki' THEN '10000000-0000-0000-0000-000000000005'::uuid
                    WHEN 'patron' THEN '10000000-0000-0000-0000-000000000006'::uuid
                    WHEN 'prokaznik' THEN '10000000-0000-0000-0000-000000000007'::uuid
                    WHEN 'diareya' THEN '10000000-0000-0000-0000-000000000008'::uuid
                    WHEN 'mentorbait' THEN '10000000-0000-0000-0000-000000000009'::uuid
                    WHEN 'kep' THEN '10000000-0000-0000-0000-00000000000a'::uuid
                    WHEN 'feyerverk' THEN '10000000-0000-0000-0000-00000000000b'::uuid
                    WHEN 'krysa' THEN '10000000-0000-0000-0000-00000000000c'::uuid
                    WHEN 'shot' THEN '10000000-0000-0000-0000-00000000000d'::uuid
                    WHEN 'podem' THEN '10000000-0000-0000-0000-00000000000e'::uuid
                    WHEN 'hard75' THEN '10000000-0000-0000-0000-00000000000f'::uuid
                    ELSE gen_random_uuid()
                END
                WHERE "Id" IS NULL;
                """
            );

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "modifier_definitions",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModifierId",
                table: "modifier_conflicts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ConflictsWithModifierId",
                table: "modifier_conflicts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModifierId",
                table: "game_modifier_selections",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModifierId",
                table: "game_active_modifiers",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE modifier_conflicts AS mc
                SET
                    "ModifierId" = left_def."Id",
                    "ConflictsWithModifierId" = right_def."Id"
                FROM modifier_definitions AS left_def, modifier_definitions AS right_def
                WHERE mc."ModifierCode" = left_def."Code"
                  AND mc."ConflictsWithModifierCode" = right_def."Code";
                """
            );

            migrationBuilder.Sql(
                """
                UPDATE game_modifier_selections AS gms
                SET "ModifierId" = md."Id"
                FROM modifier_definitions AS md
                WHERE gms."ModifierCode" = md."Code";
                """
            );

            migrationBuilder.Sql(
                """
                UPDATE game_active_modifiers AS gam
                SET "ModifierId" = md."Id"
                FROM modifier_definitions AS md
                WHERE gam."ModifierCode" = md."Code";
                """
            );

            migrationBuilder.AlterColumn<Guid>(
                name: "ModifierId",
                table: "modifier_conflicts",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ConflictsWithModifierId",
                table: "modifier_conflicts",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ModifierId",
                table: "game_modifier_selections",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ModifierId",
                table: "game_active_modifiers",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.DropPrimaryKey(
                name: "PK_modifier_definitions",
                table: "modifier_definitions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_modifier_conflicts",
                table: "modifier_conflicts");

            migrationBuilder.DropIndex(
                name: "IX_modifier_conflicts_ConflictsWithModifierCode",
                table: "modifier_conflicts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_modifier_conflicts_distinct_codes",
                table: "modifier_conflicts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_game_modifier_selections",
                table: "game_modifier_selections");

            migrationBuilder.DropIndex(
                name: "IX_game_modifier_selections_ModifierCode",
                table: "game_modifier_selections");

            migrationBuilder.DropCheckConstraint(
                name: "CK_game_modifier_selections_code_not_blank",
                table: "game_modifier_selections");

            migrationBuilder.DropIndex(
                name: "IX_game_active_modifiers_GameId_ModifierCode",
                table: "game_active_modifiers");

            migrationBuilder.DropIndex(
                name: "IX_game_active_modifiers_ModifierCode",
                table: "game_active_modifiers");

            migrationBuilder.DropCheckConstraint(
                name: "CK_game_active_modifiers_code_not_blank",
                table: "game_active_modifiers");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "modifier_definitions");

            migrationBuilder.DropColumn(
                name: "ModifierCode",
                table: "modifier_conflicts");

            migrationBuilder.DropColumn(
                name: "ConflictsWithModifierCode",
                table: "modifier_conflicts");

            migrationBuilder.DropColumn(
                name: "ModifierCode",
                table: "game_modifier_selections");

            migrationBuilder.DropColumn(
                name: "ModifierCode",
                table: "game_active_modifiers");

            migrationBuilder.AddPrimaryKey(
                name: "PK_modifier_definitions",
                table: "modifier_definitions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_modifier_conflicts",
                table: "modifier_conflicts",
                columns: new[] { "ModifierId", "ConflictsWithModifierId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_game_modifier_selections",
                table: "game_modifier_selections",
                columns: new[] { "GameId", "ModifierId" });

            migrationBuilder.CreateIndex(
                name: "IX_game_active_modifiers_ModifierId",
                table: "game_active_modifiers",
                column: "ModifierId");

            migrationBuilder.CreateIndex(
                name: "IX_game_modifier_selections_ModifierId",
                table: "game_modifier_selections",
                column: "ModifierId");

            migrationBuilder.CreateIndex(
                name: "IX_modifier_conflicts_ConflictsWithModifierId",
                table: "modifier_conflicts",
                column: "ConflictsWithModifierId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_modifier_conflicts_distinct_ids",
                table: "modifier_conflicts",
                sql: "\"ModifierId\" <> \"ConflictsWithModifierId\"");

            migrationBuilder.AddForeignKey(
                name: "FK_game_active_modifiers_modifier_definitions_ModifierId",
                table: "game_active_modifiers",
                column: "ModifierId",
                principalTable: "modifier_definitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_game_modifier_selections_modifier_definitions_ModifierId",
                table: "game_modifier_selections",
                column: "ModifierId",
                principalTable: "modifier_definitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_modifier_conflicts_modifier_definitions_ConflictsWithModif~",
                table: "modifier_conflicts",
                column: "ConflictsWithModifierId",
                principalTable: "modifier_definitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_modifier_conflicts_modifier_definitions_ModifierId",
                table: "modifier_conflicts",
                column: "ModifierId",
                principalTable: "modifier_definitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException(
                "Down migration is not supported for SwitchModifierDefinitionsToIdKey."
            );
        }
    }
}
