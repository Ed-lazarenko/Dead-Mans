using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLegacyModifierCompatibility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_modifier_definitions_limit_positive_or_null",
                table: "modifier_definitions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_game_round_modifier_results_behavior_v2_schema",
                table: "game_round_modifier_results");

            migrationBuilder.DropCheckConstraint(
                name: "ck_game_round_modifier_results_definition_revision_positive",
                table: "game_round_modifier_results");

            // Pre-V2 result rows cannot be interpreted after the compatibility reader is removed.
            // The local-only cutover explicitly discards them instead of inventing score semantics.
            migrationBuilder.Sql(
                """
                DELETE FROM game_round_modifier_results
                WHERE modifier_behavior_v2_snapshot_json IS NULL
                   OR definition_revision_snapshot IS NULL;
                """
            );

            migrationBuilder.DropColumn(
                name: "metadata_json",
                table: "modifier_definitions");

            migrationBuilder.DropColumn(
                name: "requires_host_control",
                table: "modifier_definitions");

            migrationBuilder.DropColumn(
                name: "scoring_type",
                table: "modifier_definitions");

            migrationBuilder.DropColumn(
                name: "modifier_effect_snapshot_json",
                table: "game_round_modifier_results");

            migrationBuilder.DropColumn(
                name: "modifier_mechanic_type_snapshot",
                table: "game_round_modifier_results");

            migrationBuilder.DropColumn(
                name: "modifier_scoring_type_snapshot",
                table: "game_round_modifier_results");

            migrationBuilder.DropColumn(
                name: "legacy_effect_snapshot_json",
                table: "game_modifier_activations");

            migrationBuilder.DropColumn(
                name: "modifier_mechanic_type_snapshot",
                table: "game_modifier_activations");

            migrationBuilder.DropColumn(
                name: "modifier_scoring_type_snapshot",
                table: "game_modifier_activations");

            migrationBuilder.RenameColumn(
                name: "default_limit_per_game",
                table: "modifier_definitions",
                newName: "max_activations_per_round");

            migrationBuilder.AlterColumn<string>(
                name: "modifier_behavior_v2_snapshot_json",
                table: "game_round_modifier_results",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "definition_revision_snapshot",
                table: "game_round_modifier_results",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "behavior_v2_json",
                value: "{\"schemaVersion\":2,\"kind\":\"rule\",\"phase\":\"round\",\"performer\":\"activeTeam\",\"requiresHostMonitoring\":false,\"rule\":\"\\u041F\\u0435\\u0440\\u0432\\u044B\\u0435 60 \\u0441\\u0435\\u043A\\u0443\\u043D\\u0434 \\u0437\\u0430 \\u043A\\u0430\\u0436\\u0434\\u0443\\u044E \\u0430\\u043A\\u0442\\u0438\\u0432\\u0430\\u0446\\u0438\\u044E \\u0440\\u0430\\u0437\\u0440\\u0435\\u0448\\u0435\\u043D\\u043E \\u043F\\u0435\\u0440\\u0435\\u043C\\u0435\\u0449\\u0430\\u0442\\u044C\\u0441\\u044F \\u0442\\u043E\\u043B\\u044C\\u043A\\u043E \\u043D\\u0430 \\u043A\\u043E\\u0440\\u0442\\u043E\\u0447\\u043A\\u0430\\u0445.\",\"stackingPolicy\":\"aggregateParameters\",\"resolution\":{\"type\":\"ruleStatus\"},\"reward\":\"none\",\"formulaReference\":null,\"durationSecondsPerActivation\":60}");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "behavior_v2_json",
                value: "{\"schemaVersion\":2,\"kind\":\"scoring\",\"phase\":\"result\",\"performer\":\"activeTeam\",\"requiresHostMonitoring\":true,\"rule\":\"\\u041A\\u0430\\u0436\\u0434\\u0430\\u044F \\u0430\\u043A\\u0442\\u0438\\u0432\\u0430\\u0446\\u0438\\u044F \\u0434\\u0430\\u0451\\u0442 \\u043D\\u0430\\u0440\\u0430\\u0441\\u0442\\u0430\\u044E\\u0449\\u0438\\u0435 \\u043E\\u0447\\u043A\\u0438 \\u0437\\u0430 \\u0443\\u0431\\u0438\\u0439\\u0441\\u0442\\u0432\\u0430 \\u0438 \\u0448\\u0442\\u0440\\u0430\\u0444 \\u043F\\u0440\\u0438 \\u043D\\u0443\\u043B\\u0435 \\u0443\\u0431\\u0438\\u0439\\u0441\\u0442\\u0432.\",\"stackingPolicy\":\"independentInstances\",\"resolution\":{\"type\":\"automaticRoundMetric\",\"metric\":\"killsCount\"},\"reward\":\"points\",\"formulaReference\":{\"code\":\"growing_kill_value\",\"version\":1,\"parameters\":{\"type\":\"growingKillValue\",\"incrementPointsPerKill\":5,\"zeroKillPenaltyPoints\":25}},\"durationSecondsPerActivation\":null}");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "behavior_v2_json",
                value: "{\"schemaVersion\":2,\"kind\":\"rule\",\"phase\":\"preparation\",\"performer\":\"activeTeam\",\"requiresHostMonitoring\":false,\"rule\":\"\\u041A\\u043E\\u043C\\u0430\\u043D\\u0434\\u0430 \\u043C\\u043E\\u0436\\u0435\\u0442 \\u0437\\u0430\\u043C\\u0435\\u043D\\u0438\\u0442\\u044C \\u043E\\u0434\\u0438\\u043D \\u0440\\u0430\\u0441\\u0445\\u043E\\u0434\\u043D\\u0438\\u043A \\u043D\\u0430 \\u0441\\u0432\\u043E\\u0439 \\u0432\\u044B\\u0431\\u043E\\u0440 \\u0437\\u0430 \\u043A\\u0430\\u0436\\u0434\\u0443\\u044E \\u0430\\u043A\\u0442\\u0438\\u0432\\u0430\\u0446\\u0438\\u044E.\",\"stackingPolicy\":\"aggregateParameters\",\"resolution\":{\"type\":\"ruleStatus\"},\"reward\":\"none\",\"formulaReference\":null,\"durationSecondsPerActivation\":null}");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                column: "behavior_v2_json",
                value: "{\"schemaVersion\":2,\"kind\":\"rule\",\"phase\":\"round\",\"performer\":\"activeTeam\",\"requiresHostMonitoring\":true,\"rule\":\"\\u0417\\u0430\\u043F\\u0440\\u0435\\u0449\\u0435\\u043D\\u043E \\u0441\\u0436\\u0438\\u0433\\u0430\\u0442\\u044C \\u0442\\u0440\\u0443\\u043F\\u044B \\u0432\\u0435\\u0441\\u044C \\u0440\\u0430\\u0443\\u043D\\u0434.\",\"stackingPolicy\":\"aggregateParameters\",\"resolution\":{\"type\":\"ruleStatus\"},\"reward\":\"none\",\"formulaReference\":null,\"durationSecondsPerActivation\":null}");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "behavior_v2_json",
                value: "{\"schemaVersion\":2,\"kind\":\"rule\",\"phase\":\"preparation\",\"performer\":\"activeTeam\",\"requiresHostMonitoring\":true,\"rule\":\"\\u0412\\u043D\\u0435\\u0448\\u043D\\u0438\\u0439 \\u043B\\u0438\\u043C\\u0438\\u0442 \\u043D\\u0430\\u0432\\u044B\\u043A\\u043E\\u0432 \\u0443\\u043C\\u0435\\u043D\\u044C\\u0448\\u0430\\u0435\\u0442\\u0441\\u044F \\u043D\\u0430 20% \\u0437\\u0430 \\u0430\\u043A\\u0442\\u0438\\u0432\\u0430\\u0446\\u0438\\u044E, \\u043D\\u043E \\u043D\\u0435 \\u0431\\u043E\\u043B\\u0435\\u0435 \\u0447\\u0435\\u043C \\u043D\\u0430 100%.\",\"stackingPolicy\":\"aggregateParameters\",\"resolution\":{\"type\":\"ruleStatus\"},\"reward\":\"none\",\"formulaReference\":null,\"durationSecondsPerActivation\":null}");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                column: "behavior_v2_json",
                value: "{\"schemaVersion\":2,\"kind\":\"scoring\",\"phase\":\"result\",\"performer\":\"activeTeam\",\"requiresHostMonitoring\":true,\"rule\":\"\\u0415\\u0441\\u043B\\u0438 \\u0432\\u0440\\u0430\\u0433 \\u0443\\u0431\\u0438\\u0442 \\u043F\\u0435\\u0440\\u0432\\u043E\\u0439 \\u043F\\u0443\\u043B\\u0435\\u0439 \\u043D\\u0435 \\u0438\\u0437 \\u043B\\u0443\\u043A\\u0430, \\u0430\\u0440\\u0431\\u0430\\u043B\\u0435\\u0442\\u0430 \\u0438\\u043B\\u0438 \\u0434\\u0440\\u043E\\u0431\\u043E\\u0432\\u0438\\u043A\\u0430, \\u043A\\u043E\\u043C\\u0430\\u043D\\u0434\\u0430 \\u043F\\u043E\\u043B\\u0443\\u0447\\u0430\\u0435\\u0442 \\u0431\\u043E\\u043D\\u0443\\u0441\\u043D\\u043E\\u0435 \\u0443\\u0431\\u0438\\u0439\\u0441\\u0442\\u0432\\u043E.\",\"stackingPolicy\":\"independentInstances\",\"resolution\":{\"type\":\"boolean\"},\"reward\":\"bonusKills\",\"formulaReference\":{\"code\":\"bonus_kill_on_condition\",\"version\":1,\"parameters\":{\"type\":\"bonusKillOnCondition\",\"successBonusKills\":1}},\"durationSecondsPerActivation\":null}");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000007"),
                column: "behavior_v2_json",
                value: "{\"schemaVersion\":2,\"kind\":\"rule\",\"phase\":\"round\",\"performer\":\"mentor\",\"requiresHostMonitoring\":true,\"rule\":\"\\u041C\\u0435\\u043D\\u0442\\u043E\\u0440 \\u0441 \\u043E\\u0431\\u043C\\u0430\\u043D\\u043A\\u0430\\u043C\\u0438 \\u0438 \\u043F\\u043E\\u043B\\u0442\\u0435\\u0440\\u0433\\u0435\\u0439\\u0441\\u0442\\u043E\\u043C \\u043C\\u0435\\u0448\\u0430\\u0435\\u0442 \\u043A\\u043E\\u043C\\u0430\\u043D\\u0434\\u0435 300 \\u0441\\u0435\\u043A\\u0443\\u043D\\u0434 \\u0437\\u0430 \\u0430\\u043A\\u0442\\u0438\\u0432\\u0430\\u0446\\u0438\\u044E; \\u0435\\u0433\\u043E \\u043D\\u0435\\u043B\\u044C\\u0437\\u044F \\u0443\\u0431\\u0438\\u0442\\u044C \\u0438\\u043B\\u0438 \\u043F\\u043E\\u0434\\u043D\\u044F\\u0442\\u044C.\",\"stackingPolicy\":\"aggregateParameters\",\"resolution\":{\"type\":\"ruleStatus\"},\"reward\":\"none\",\"formulaReference\":null,\"durationSecondsPerActivation\":300}");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000008"),
                column: "behavior_v2_json",
                value: "{\"schemaVersion\":2,\"kind\":\"rule\",\"phase\":\"round\",\"performer\":\"activeTeam\",\"requiresHostMonitoring\":true,\"rule\":\"\\u041F\\u0440\\u0438 \\u0443\\u043F\\u043E\\u043C\\u0438\\u043D\\u0430\\u043D\\u0438\\u0438 \\u0438\\u043B\\u0438 \\u043E\\u0431\\u043D\\u0430\\u0440\\u0443\\u0436\\u0435\\u043D\\u0438\\u0438 \\u0442\\u0443\\u0430\\u043B\\u0435\\u0442\\u0430 \\u0438\\u0433\\u0440\\u043E\\u043A \\u043E\\u0431\\u044F\\u0437\\u0430\\u043D \\u0437\\u0430\\u0439\\u0442\\u0438 \\u0432 \\u043D\\u0435\\u0433\\u043E, \\u0435\\u0441\\u043B\\u0438 \\u0432\\u0440\\u0430\\u0433\\u0430 \\u043D\\u0435\\u0442 \\u0432 \\u043F\\u043E\\u043B\\u0435 \\u0437\\u0440\\u0435\\u043D\\u0438\\u044F.\",\"stackingPolicy\":\"aggregateParameters\",\"resolution\":{\"type\":\"ruleStatus\"},\"reward\":\"none\",\"formulaReference\":null,\"durationSecondsPerActivation\":null}");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                column: "behavior_v2_json",
                value: "{\"schemaVersion\":2,\"kind\":\"rule\",\"phase\":\"round\",\"performer\":\"mentor\",\"requiresHostMonitoring\":true,\"rule\":\"\\u041C\\u0435\\u043D\\u0442\\u043E\\u0440 \\u0441 \\u043D\\u0430\\u0431\\u043E\\u0440\\u043E\\u043C \\u0448\\u0443\\u043C\\u0435\\u043B\\u043E\\u043A \\u0434\\u0435\\u0439\\u0441\\u0442\\u0432\\u0443\\u0435\\u0442 300 \\u0441\\u0435\\u043A\\u0443\\u043D\\u0434; \\u0435\\u0433\\u043E \\u043C\\u043E\\u0436\\u043D\\u043E \\u0443\\u0431\\u0438\\u0442\\u044C, \\u043D\\u043E \\u043D\\u0435\\u043B\\u044C\\u0437\\u044F \\u043F\\u043E\\u0434\\u043D\\u044F\\u0442\\u044C.\",\"stackingPolicy\":\"aggregateParameters\",\"resolution\":{\"type\":\"ruleStatus\"},\"reward\":\"none\",\"formulaReference\":null,\"durationSecondsPerActivation\":300}");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-00000000000a"),
                column: "behavior_v2_json",
                value: "{\"schemaVersion\":2,\"kind\":\"rule\",\"phase\":\"round\",\"performer\":\"activeTeam\",\"requiresHostMonitoring\":true,\"rule\":\"\\u041F\\u043E\\u043B\\u044C\\u0437\\u043E\\u0432\\u0430\\u0442\\u044C\\u0441\\u044F \\u0433\\u043E\\u043B\\u043E\\u0441\\u043E\\u0432\\u044B\\u043C \\u0447\\u0430\\u0442\\u043E\\u043C \\u043C\\u043E\\u0436\\u0435\\u0442 \\u0442\\u043E\\u043B\\u044C\\u043A\\u043E \\u043A\\u0430\\u043F\\u0438\\u0442\\u0430\\u043D.\",\"stackingPolicy\":\"aggregateParameters\",\"resolution\":{\"type\":\"ruleStatus\"},\"reward\":\"none\",\"formulaReference\":null,\"durationSecondsPerActivation\":null}");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-00000000000b"),
                column: "behavior_v2_json",
                value: "{\"schemaVersion\":2,\"kind\":\"rule\",\"phase\":\"round\",\"performer\":\"mentor\",\"requiresHostMonitoring\":true,\"rule\":\"\\u041C\\u0435\\u043D\\u0442\\u043E\\u0440 \\u0441\\u0442\\u0440\\u0435\\u043B\\u044F\\u0435\\u0442 \\u043E\\u0441\\u0432\\u0435\\u0442\\u0438\\u0442\\u0435\\u043B\\u044C\\u043D\\u044B\\u043C\\u0438 \\u0441\\u043D\\u0430\\u0440\\u044F\\u0434\\u0430\\u043C\\u0438 \\u043F\\u0440\\u0438 \\u0441\\u0442\\u0430\\u0440\\u0442\\u0435 \\u0438 \\u0447\\u0435\\u0440\\u0435\\u0437 60, 120, 180 \\u0438 240 \\u0441\\u0435\\u043A\\u0443\\u043D\\u0434; \\u0435\\u0433\\u043E \\u043D\\u0435\\u043B\\u044C\\u0437\\u044F \\u0443\\u0431\\u0438\\u0442\\u044C \\u0438\\u043B\\u0438 \\u043F\\u043E\\u0434\\u043D\\u044F\\u0442\\u044C.\",\"stackingPolicy\":\"aggregateParameters\",\"resolution\":{\"type\":\"ruleStatus\"},\"reward\":\"none\",\"formulaReference\":null,\"durationSecondsPerActivation\":300}");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-00000000000c"),
                column: "behavior_v2_json",
                value: "{\"schemaVersion\":2,\"kind\":\"scoring\",\"phase\":\"result\",\"performer\":\"mentor\",\"requiresHostMonitoring\":true,\"rule\":\"\\u0423\\u0431\\u0438\\u0439\\u0441\\u0442\\u0432\\u0430 \\u043C\\u0435\\u043D\\u0442\\u043E\\u0440\\u0430 \\u0441 \\u043F\\u043E\\u043B\\u043D\\u044B\\u043C \\u043D\\u0430\\u0431\\u043E\\u0440\\u043E\\u043C \\u043B\\u043E\\u0432\\u0443\\u0448\\u0435\\u043A \\u0441\\u0447\\u0438\\u0442\\u0430\\u044E\\u0442\\u0441\\u044F \\u0431\\u043E\\u043D\\u0443\\u0441\\u043D\\u044B\\u043C\\u0438 \\u0443\\u0431\\u0438\\u0439\\u0441\\u0442\\u0432\\u0430\\u043C\\u0438 \\u043A\\u043E\\u043C\\u0430\\u043D\\u0434\\u044B.\",\"stackingPolicy\":\"independentInstances\",\"resolution\":{\"type\":\"nonNegativeCount\"},\"reward\":\"bonusKills\",\"formulaReference\":{\"code\":\"bonus_kills_by_count\",\"version\":1,\"parameters\":{\"type\":\"bonusKillsByCount\",\"bonusKillsPerUnit\":1}},\"durationSecondsPerActivation\":null}");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-00000000000d"),
                column: "behavior_v2_json",
                value: "{\"schemaVersion\":2,\"kind\":\"scoring\",\"phase\":\"result\",\"performer\":\"mentor\",\"requiresHostMonitoring\":true,\"rule\":\"\\u041A\\u0430\\u0436\\u0434\\u0430\\u044F \\u0430\\u043A\\u0442\\u0438\\u0432\\u0430\\u0446\\u0438\\u044F \\u0434\\u0430\\u0451\\u0442 \\u043C\\u0435\\u043D\\u0442\\u043E\\u0440\\u0443 \\u043E\\u0440\\u0443\\u0436\\u0438\\u0435 \\u0441 \\u043E\\u0434\\u043D\\u0438\\u043C \\u0432\\u044B\\u0441\\u0442\\u0440\\u0435\\u043B\\u043E\\u043C; \\u0443\\u0441\\u043F\\u0435\\u0448\\u043D\\u044B\\u0439 \\u0432\\u044B\\u0441\\u0442\\u0440\\u0435\\u043B \\u0441\\u0447\\u0438\\u0442\\u0430\\u0435\\u0442\\u0441\\u044F \\u0431\\u043E\\u043D\\u0443\\u0441\\u043D\\u044B\\u043C \\u0443\\u0431\\u0438\\u0439\\u0441\\u0442\\u0432\\u043E\\u043C \\u043A\\u043E\\u043C\\u0430\\u043D\\u0434\\u044B.\",\"stackingPolicy\":\"independentInstances\",\"resolution\":{\"type\":\"boolean\"},\"reward\":\"bonusKills\",\"formulaReference\":{\"code\":\"bonus_kill_on_condition\",\"version\":1,\"parameters\":{\"type\":\"bonusKillOnCondition\",\"successBonusKills\":1}},\"durationSecondsPerActivation\":null}");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-00000000000e"),
                column: "behavior_v2_json",
                value: "{\"schemaVersion\":2,\"kind\":\"rule\",\"phase\":\"round\",\"performer\":\"activeTeam\",\"requiresHostMonitoring\":true,\"rule\":\"\\u041D\\u0435\\u043B\\u044C\\u0437\\u044F \\u043F\\u043E\\u0434\\u043D\\u0438\\u043C\\u0430\\u0442\\u044C \\u0441\\u043E\\u044E\\u0437\\u043D\\u0438\\u043A\\u0430, \\u043F\\u043E\\u043A\\u0430 \\u043A\\u043E\\u043C\\u0430\\u043D\\u0434\\u0430 \\u043D\\u0435 \\u0443\\u0431\\u0438\\u043B\\u0430 \\u0432\\u0440\\u0430\\u0433\\u0430.\",\"stackingPolicy\":\"aggregateParameters\",\"resolution\":{\"type\":\"ruleStatus\"},\"reward\":\"none\",\"formulaReference\":null,\"durationSecondsPerActivation\":null}");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-00000000000f"),
                column: "behavior_v2_json",
                value: "{\"schemaVersion\":2,\"kind\":\"scoring\",\"phase\":\"result\",\"performer\":\"activeTeam\",\"requiresHostMonitoring\":true,\"rule\":\"\\u041F\\u043E\\u0434\\u0445\\u043E\\u0434\\u044F\\u0449\\u0438\\u0435 \\u0443\\u0431\\u0438\\u0439\\u0441\\u0442\\u0432\\u0430 \\u0434\\u043E \\u0432\\u043E\\u0441\\u0441\\u0442\\u0430\\u043D\\u043E\\u0432\\u043B\\u0435\\u043D\\u0438\\u044F \\u0437\\u0434\\u043E\\u0440\\u043E\\u0432\\u044C\\u044F \\u0434\\u0430\\u044E\\u0442 \\u0434\\u043E\\u043F\\u043E\\u043B\\u043D\\u0438\\u0442\\u0435\\u043B\\u044C\\u043D\\u044B\\u0435 75% \\u0441\\u0442\\u043E\\u0438\\u043C\\u043E\\u0441\\u0442\\u0438 \\u043A\\u0430\\u0440\\u0442\\u043E\\u0447\\u043A\\u0438.\",\"stackingPolicy\":\"independentInstances\",\"resolution\":{\"type\":\"nonNegativeCount\"},\"reward\":\"points\",\"formulaReference\":{\"code\":\"window_kill_bonus_points\",\"version\":1,\"parameters\":{\"type\":\"windowKillBonusPoints\",\"bonusRate\":0.75}},\"durationSecondsPerActivation\":null}");

            migrationBuilder.AddCheckConstraint(
                name: "ck_modifier_definitions_limit_positive_or_null",
                table: "modifier_definitions",
                sql: "max_activations_per_round IS NULL OR max_activations_per_round > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_game_round_modifier_results_behavior_v2_schema",
                table: "game_round_modifier_results",
                sql: "modifier_behavior_v2_snapshot_json ->> 'schemaVersion' = '2'");

            migrationBuilder.AddCheckConstraint(
                name: "ck_game_round_modifier_results_definition_revision_positive",
                table: "game_round_modifier_results",
                sql: "definition_revision_snapshot >= 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_modifier_definitions_limit_positive_or_null",
                table: "modifier_definitions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_game_round_modifier_results_behavior_v2_schema",
                table: "game_round_modifier_results");

            migrationBuilder.DropCheckConstraint(
                name: "ck_game_round_modifier_results_definition_revision_positive",
                table: "game_round_modifier_results");

            migrationBuilder.RenameColumn(
                name: "max_activations_per_round",
                table: "modifier_definitions",
                newName: "default_limit_per_game");

            migrationBuilder.AddColumn<string>(
                name: "metadata_json",
                table: "modifier_definitions",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "requires_host_control",
                table: "modifier_definitions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "scoring_type",
                table: "modifier_definitions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "modifier_behavior_v2_snapshot_json",
                table: "game_round_modifier_results",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<int>(
                name: "definition_revision_snapshot",
                table: "game_round_modifier_results",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "modifier_effect_snapshot_json",
                table: "game_round_modifier_results",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "modifier_mechanic_type_snapshot",
                table: "game_round_modifier_results",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "modifier_scoring_type_snapshot",
                table: "game_round_modifier_results",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "legacy_effect_snapshot_json",
                table: "game_modifier_activations",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "modifier_mechanic_type_snapshot",
                table: "game_modifier_activations",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "modifier_scoring_type_snapshot",
                table: "game_modifier_activations",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                columns: new[] { "behavior_v2_json", "metadata_json", "scoring_type" },
                values: new object[] { "{\"schemaVersion\":2,\"kind\":\"rule\",\"phase\":\"round\",\"performer\":\"activeTeam\",\"requiresHostMonitoring\":false,\"rule\":\"\\u041F\\u0435\\u0440\\u0432\\u044B\\u0435 60 \\u0441\\u0435\\u043A\\u0443\\u043D\\u0434 \\u0437\\u0430 \\u043A\\u0430\\u0436\\u0434\\u0443\\u044E \\u0430\\u043A\\u0442\\u0438\\u0432\\u0430\\u0446\\u0438\\u044E \\u0440\\u0430\\u0437\\u0440\\u0435\\u0448\\u0435\\u043D\\u043E \\u043F\\u0435\\u0440\\u0435\\u043C\\u0435\\u0449\\u0430\\u0442\\u044C\\u0441\\u044F \\u0442\\u043E\\u043B\\u044C\\u043A\\u043E \\u043D\\u0430 \\u043A\\u043E\\u0440\\u0442\\u043E\\u0447\\u043A\\u0430\\u0445.\",\"stackingPolicy\":\"aggregateParameters\",\"resolution\":{\"type\":\"ruleStatus\"},\"reward\":\"none\",\"formulaReference\":null}", "{\"effect\":{\"mechanicType\":\"rule_only\",\"traits\":[],\"durationSeconds\":60,\"ruleText\":null,\"scoreImpact\":null,\"conditions\":[],\"resolutionInputs\":[],\"killEffect\":null,\"multiplierEffect\":null,\"mentorEffect\":null}}", "non_scoring" });

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                columns: new[] { "behavior_v2_json", "metadata_json", "requires_host_control", "scoring_type" },
                values: new object[] { "{\"schemaVersion\":2,\"kind\":\"scoring\",\"phase\":\"result\",\"performer\":\"activeTeam\",\"requiresHostMonitoring\":true,\"rule\":\"\\u041A\\u0430\\u0436\\u0434\\u0430\\u044F \\u0430\\u043A\\u0442\\u0438\\u0432\\u0430\\u0446\\u0438\\u044F \\u0434\\u0430\\u0451\\u0442 \\u043D\\u0430\\u0440\\u0430\\u0441\\u0442\\u0430\\u044E\\u0449\\u0438\\u0435 \\u043E\\u0447\\u043A\\u0438 \\u0437\\u0430 \\u0443\\u0431\\u0438\\u0439\\u0441\\u0442\\u0432\\u0430 \\u0438 \\u0448\\u0442\\u0440\\u0430\\u0444 \\u043F\\u0440\\u0438 \\u043D\\u0443\\u043B\\u0435 \\u0443\\u0431\\u0438\\u0439\\u0441\\u0442\\u0432.\",\"stackingPolicy\":\"independentInstances\",\"resolution\":{\"type\":\"automaticRoundMetric\",\"metric\":\"killsCount\"},\"reward\":\"points\",\"formulaReference\":{\"code\":\"growing_kill_value\",\"version\":1,\"parameters\":{\"type\":\"growingKillValue\",\"incrementPointsPerKill\":5,\"zeroKillPenaltyPoints\":25}}}", "{\"effect\":{\"mechanicType\":\"restriction_with_reward\",\"traits\":[\"requires_manual_resolution\",\"stacking_per_kill_bonus\"],\"durationSeconds\":null,\"ruleText\":null,\"scoreImpact\":{\"pointsDelta\":null,\"perKillBonus\":5,\"failurePenaltyPoints\":25,\"multiplierDelta\":null,\"killDelta\":null,\"scoreFormula\":{\"mode\":\"stacking_per_kill_bonus\",\"successExpression\":null,\"failureExpression\":null}},\"conditions\":[{\"type\":\"at_least_one_kill\",\"source\":\"manual_input\"}],\"resolutionInputs\":[\"kills\"],\"killEffect\":null,\"multiplierEffect\":null,\"mentorEffect\":null}}", true, "conditional_bonus_penalty" });

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                columns: new[] { "behavior_v2_json", "metadata_json", "scoring_type" },
                values: new object[] { "{\"schemaVersion\":2,\"kind\":\"rule\",\"phase\":\"preparation\",\"performer\":\"activeTeam\",\"requiresHostMonitoring\":false,\"rule\":\"\\u041A\\u043E\\u043C\\u0430\\u043D\\u0434\\u0430 \\u043C\\u043E\\u0436\\u0435\\u0442 \\u0437\\u0430\\u043C\\u0435\\u043D\\u0438\\u0442\\u044C \\u043E\\u0434\\u0438\\u043D \\u0440\\u0430\\u0441\\u0445\\u043E\\u0434\\u043D\\u0438\\u043A \\u043D\\u0430 \\u0441\\u0432\\u043E\\u0439 \\u0432\\u044B\\u0431\\u043E\\u0440 \\u0437\\u0430 \\u043A\\u0430\\u0436\\u0434\\u0443\\u044E \\u0430\\u043A\\u0442\\u0438\\u0432\\u0430\\u0446\\u0438\\u044E.\",\"stackingPolicy\":\"aggregateParameters\",\"resolution\":{\"type\":\"ruleStatus\"},\"reward\":\"none\",\"formulaReference\":null}", "{\"effect\":{\"mechanicType\":\"rule_only\",\"traits\":[],\"durationSeconds\":null,\"ruleText\":null,\"scoreImpact\":null,\"conditions\":[],\"resolutionInputs\":[],\"killEffect\":null,\"multiplierEffect\":null,\"mentorEffect\":null}}", "non_scoring" });

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                columns: new[] { "behavior_v2_json", "metadata_json", "requires_host_control", "scoring_type" },
                values: new object[] { "{\"schemaVersion\":2,\"kind\":\"rule\",\"phase\":\"round\",\"performer\":\"activeTeam\",\"requiresHostMonitoring\":true,\"rule\":\"\\u0417\\u0430\\u043F\\u0440\\u0435\\u0449\\u0435\\u043D\\u043E \\u0441\\u0436\\u0438\\u0433\\u0430\\u0442\\u044C \\u0442\\u0440\\u0443\\u043F\\u044B \\u0432\\u0435\\u0441\\u044C \\u0440\\u0430\\u0443\\u043D\\u0434.\",\"stackingPolicy\":\"aggregateParameters\",\"resolution\":{\"type\":\"ruleStatus\"},\"reward\":\"none\",\"formulaReference\":null}", "{\"effect\":{\"mechanicType\":\"rule_only\",\"traits\":[],\"durationSeconds\":null,\"ruleText\":null,\"scoreImpact\":null,\"conditions\":[],\"resolutionInputs\":[],\"killEffect\":null,\"multiplierEffect\":null,\"mentorEffect\":null}}", true, "non_scoring" });

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                columns: new[] { "behavior_v2_json", "metadata_json", "scoring_type" },
                values: new object[] { "{\"schemaVersion\":2,\"kind\":\"rule\",\"phase\":\"preparation\",\"performer\":\"activeTeam\",\"requiresHostMonitoring\":true,\"rule\":\"\\u0412\\u043D\\u0435\\u0448\\u043D\\u0438\\u0439 \\u043B\\u0438\\u043C\\u0438\\u0442 \\u043D\\u0430\\u0432\\u044B\\u043A\\u043E\\u0432 \\u0443\\u043C\\u0435\\u043D\\u044C\\u0448\\u0430\\u0435\\u0442\\u0441\\u044F \\u043D\\u0430 20% \\u0437\\u0430 \\u0430\\u043A\\u0442\\u0438\\u0432\\u0430\\u0446\\u0438\\u044E, \\u043D\\u043E \\u043D\\u0435 \\u0431\\u043E\\u043B\\u0435\\u0435 \\u0447\\u0435\\u043C \\u043D\\u0430 100%.\",\"stackingPolicy\":\"aggregateParameters\",\"resolution\":{\"type\":\"ruleStatus\"},\"reward\":\"none\",\"formulaReference\":null}", "{\"effect\":{\"mechanicType\":\"rule_only\",\"traits\":[],\"durationSeconds\":null,\"ruleText\":null,\"scoreImpact\":null,\"conditions\":[],\"resolutionInputs\":[],\"killEffect\":null,\"multiplierEffect\":null,\"mentorEffect\":null}}", "non_scoring" });

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                columns: new[] { "behavior_v2_json", "metadata_json", "requires_host_control", "scoring_type" },
                values: new object[] { "{\"schemaVersion\":2,\"kind\":\"scoring\",\"phase\":\"result\",\"performer\":\"activeTeam\",\"requiresHostMonitoring\":true,\"rule\":\"\\u0415\\u0441\\u043B\\u0438 \\u0432\\u0440\\u0430\\u0433 \\u0443\\u0431\\u0438\\u0442 \\u043F\\u0435\\u0440\\u0432\\u043E\\u0439 \\u043F\\u0443\\u043B\\u0435\\u0439 \\u043D\\u0435 \\u0438\\u0437 \\u043B\\u0443\\u043A\\u0430, \\u0430\\u0440\\u0431\\u0430\\u043B\\u0435\\u0442\\u0430 \\u0438\\u043B\\u0438 \\u0434\\u0440\\u043E\\u0431\\u043E\\u0432\\u0438\\u043A\\u0430, \\u043A\\u043E\\u043C\\u0430\\u043D\\u0434\\u0430 \\u043F\\u043E\\u043B\\u0443\\u0447\\u0430\\u0435\\u0442 \\u0431\\u043E\\u043D\\u0443\\u0441\\u043D\\u043E\\u0435 \\u0443\\u0431\\u0438\\u0439\\u0441\\u0442\\u0432\\u043E.\",\"stackingPolicy\":\"independentInstances\",\"resolution\":{\"type\":\"boolean\"},\"reward\":\"bonusKills\",\"formulaReference\":{\"code\":\"bonus_kill_on_condition\",\"version\":1,\"parameters\":{\"type\":\"bonusKillOnCondition\",\"successBonusKills\":1}}}", "{\"effect\":{\"mechanicType\":\"kill_counter\",\"traits\":[\"requires_manual_resolution\"],\"durationSeconds\":null,\"ruleText\":null,\"scoreImpact\":{\"pointsDelta\":null,\"perKillBonus\":null,\"failurePenaltyPoints\":null,\"multiplierDelta\":null,\"killDelta\":1},\"conditions\":[{\"type\":\"first_kill_first_bullet\",\"source\":\"manual_input\"}],\"resolutionInputs\":[\"kills\"],\"killEffect\":{\"killDeltaMode\":\"conditional_bonus_kill\",\"killDeltaValue\":1,\"condition\":\"first_kill_first_bullet\",\"excludedWeapons\":[\"лук\",\"арбалет\",\"дробовик\"]},\"multiplierEffect\":null,\"mentorEffect\":null}}", true, "conditional_bonus" });

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000007"),
                columns: new[] { "behavior_v2_json", "metadata_json", "requires_host_control", "scoring_type" },
                values: new object[] { "{\"schemaVersion\":2,\"kind\":\"rule\",\"phase\":\"round\",\"performer\":\"mentor\",\"requiresHostMonitoring\":true,\"rule\":\"\\u041C\\u0435\\u043D\\u0442\\u043E\\u0440 \\u0441 \\u043E\\u0431\\u043C\\u0430\\u043D\\u043A\\u0430\\u043C\\u0438 \\u0438 \\u043F\\u043E\\u043B\\u0442\\u0435\\u0440\\u0433\\u0435\\u0439\\u0441\\u0442\\u043E\\u043C \\u043C\\u0435\\u0448\\u0430\\u0435\\u0442 \\u043A\\u043E\\u043C\\u0430\\u043D\\u0434\\u0435 300 \\u0441\\u0435\\u043A\\u0443\\u043D\\u0434 \\u0437\\u0430 \\u0430\\u043A\\u0442\\u0438\\u0432\\u0430\\u0446\\u0438\\u044E; \\u0435\\u0433\\u043E \\u043D\\u0435\\u043B\\u044C\\u0437\\u044F \\u0443\\u0431\\u0438\\u0442\\u044C \\u0438\\u043B\\u0438 \\u043F\\u043E\\u0434\\u043D\\u044F\\u0442\\u044C.\",\"stackingPolicy\":\"aggregateParameters\",\"resolution\":{\"type\":\"ruleStatus\"},\"reward\":\"none\",\"formulaReference\":null}", "{\"effect\":{\"mechanicType\":\"mentor\",\"traits\":[\"requires_manual_resolution\"],\"durationSeconds\":300,\"ruleText\":null,\"scoreImpact\":null,\"conditions\":[],\"resolutionInputs\":[\"mentorStatus\"],\"killEffect\":null,\"multiplierEffect\":null,\"mentorEffect\":{\"loadoutText\":\"Обманки и полтергейст\",\"durationSeconds\":300,\"canBeRevived\":false,\"canBeKilled\":false,\"killsCreditToTeam\":false}}}", true, "non_scoring" });

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000008"),
                columns: new[] { "behavior_v2_json", "metadata_json", "requires_host_control", "scoring_type" },
                values: new object[] { "{\"schemaVersion\":2,\"kind\":\"rule\",\"phase\":\"round\",\"performer\":\"activeTeam\",\"requiresHostMonitoring\":true,\"rule\":\"\\u041F\\u0440\\u0438 \\u0443\\u043F\\u043E\\u043C\\u0438\\u043D\\u0430\\u043D\\u0438\\u0438 \\u0438\\u043B\\u0438 \\u043E\\u0431\\u043D\\u0430\\u0440\\u0443\\u0436\\u0435\\u043D\\u0438\\u0438 \\u0442\\u0443\\u0430\\u043B\\u0435\\u0442\\u0430 \\u0438\\u0433\\u0440\\u043E\\u043A \\u043E\\u0431\\u044F\\u0437\\u0430\\u043D \\u0437\\u0430\\u0439\\u0442\\u0438 \\u0432 \\u043D\\u0435\\u0433\\u043E, \\u0435\\u0441\\u043B\\u0438 \\u0432\\u0440\\u0430\\u0433\\u0430 \\u043D\\u0435\\u0442 \\u0432 \\u043F\\u043E\\u043B\\u0435 \\u0437\\u0440\\u0435\\u043D\\u0438\\u044F.\",\"stackingPolicy\":\"aggregateParameters\",\"resolution\":{\"type\":\"ruleStatus\"},\"reward\":\"none\",\"formulaReference\":null}", "{\"effect\":{\"mechanicType\":\"rule_only\",\"traits\":[],\"durationSeconds\":null,\"ruleText\":null,\"scoreImpact\":null,\"conditions\":[],\"resolutionInputs\":[],\"killEffect\":null,\"multiplierEffect\":null,\"mentorEffect\":null}}", true, "non_scoring" });

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                columns: new[] { "behavior_v2_json", "metadata_json", "requires_host_control", "scoring_type" },
                values: new object[] { "{\"schemaVersion\":2,\"kind\":\"rule\",\"phase\":\"round\",\"performer\":\"mentor\",\"requiresHostMonitoring\":true,\"rule\":\"\\u041C\\u0435\\u043D\\u0442\\u043E\\u0440 \\u0441 \\u043D\\u0430\\u0431\\u043E\\u0440\\u043E\\u043C \\u0448\\u0443\\u043C\\u0435\\u043B\\u043E\\u043A \\u0434\\u0435\\u0439\\u0441\\u0442\\u0432\\u0443\\u0435\\u0442 300 \\u0441\\u0435\\u043A\\u0443\\u043D\\u0434; \\u0435\\u0433\\u043E \\u043C\\u043E\\u0436\\u043D\\u043E \\u0443\\u0431\\u0438\\u0442\\u044C, \\u043D\\u043E \\u043D\\u0435\\u043B\\u044C\\u0437\\u044F \\u043F\\u043E\\u0434\\u043D\\u044F\\u0442\\u044C.\",\"stackingPolicy\":\"aggregateParameters\",\"resolution\":{\"type\":\"ruleStatus\"},\"reward\":\"none\",\"formulaReference\":null}", "{\"effect\":{\"mechanicType\":\"mentor\",\"traits\":[\"requires_manual_resolution\"],\"durationSeconds\":300,\"ruleText\":null,\"scoreImpact\":null,\"conditions\":[],\"resolutionInputs\":[\"mentorStatus\"],\"killEffect\":null,\"multiplierEffect\":null,\"mentorEffect\":{\"loadoutText\":\"Набор шумелок\",\"durationSeconds\":300,\"canBeRevived\":false,\"canBeKilled\":true,\"killsCreditToTeam\":false}}}", true, "non_scoring" });

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-00000000000a"),
                columns: new[] { "behavior_v2_json", "metadata_json", "requires_host_control", "scoring_type" },
                values: new object[] { "{\"schemaVersion\":2,\"kind\":\"rule\",\"phase\":\"round\",\"performer\":\"activeTeam\",\"requiresHostMonitoring\":true,\"rule\":\"\\u041F\\u043E\\u043B\\u044C\\u0437\\u043E\\u0432\\u0430\\u0442\\u044C\\u0441\\u044F \\u0433\\u043E\\u043B\\u043E\\u0441\\u043E\\u0432\\u044B\\u043C \\u0447\\u0430\\u0442\\u043E\\u043C \\u043C\\u043E\\u0436\\u0435\\u0442 \\u0442\\u043E\\u043B\\u044C\\u043A\\u043E \\u043A\\u0430\\u043F\\u0438\\u0442\\u0430\\u043D.\",\"stackingPolicy\":\"aggregateParameters\",\"resolution\":{\"type\":\"ruleStatus\"},\"reward\":\"none\",\"formulaReference\":null}", "{\"effect\":{\"mechanicType\":\"rule_only\",\"traits\":[],\"durationSeconds\":null,\"ruleText\":null,\"scoreImpact\":null,\"conditions\":[],\"resolutionInputs\":[],\"killEffect\":null,\"multiplierEffect\":null,\"mentorEffect\":null}}", true, "non_scoring" });

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-00000000000b"),
                columns: new[] { "behavior_v2_json", "metadata_json", "requires_host_control", "scoring_type" },
                values: new object[] { "{\"schemaVersion\":2,\"kind\":\"rule\",\"phase\":\"round\",\"performer\":\"mentor\",\"requiresHostMonitoring\":true,\"rule\":\"\\u041C\\u0435\\u043D\\u0442\\u043E\\u0440 \\u0441\\u0442\\u0440\\u0435\\u043B\\u044F\\u0435\\u0442 \\u043E\\u0441\\u0432\\u0435\\u0442\\u0438\\u0442\\u0435\\u043B\\u044C\\u043D\\u044B\\u043C\\u0438 \\u0441\\u043D\\u0430\\u0440\\u044F\\u0434\\u0430\\u043C\\u0438 \\u043F\\u0440\\u0438 \\u0441\\u0442\\u0430\\u0440\\u0442\\u0435 \\u0438 \\u0447\\u0435\\u0440\\u0435\\u0437 60, 120, 180 \\u0438 240 \\u0441\\u0435\\u043A\\u0443\\u043D\\u0434; \\u0435\\u0433\\u043E \\u043D\\u0435\\u043B\\u044C\\u0437\\u044F \\u0443\\u0431\\u0438\\u0442\\u044C \\u0438\\u043B\\u0438 \\u043F\\u043E\\u0434\\u043D\\u044F\\u0442\\u044C.\",\"stackingPolicy\":\"aggregateParameters\",\"resolution\":{\"type\":\"ruleStatus\"},\"reward\":\"none\",\"formulaReference\":null}", "{\"effect\":{\"mechanicType\":\"mentor\",\"traits\":[\"requires_manual_resolution\"],\"durationSeconds\":300,\"ruleText\":null,\"scoreImpact\":null,\"conditions\":[],\"resolutionInputs\":[\"mentorStatus\"],\"killEffect\":null,\"multiplierEffect\":null,\"mentorEffect\":{\"loadoutText\":\"Оружие с осветительными снарядами\",\"durationSeconds\":300,\"canBeRevived\":false,\"canBeKilled\":false,\"killsCreditToTeam\":false}}}", true, "non_scoring" });

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-00000000000c"),
                columns: new[] { "behavior_v2_json", "metadata_json", "requires_host_control", "scoring_type" },
                values: new object[] { "{\"schemaVersion\":2,\"kind\":\"scoring\",\"phase\":\"result\",\"performer\":\"mentor\",\"requiresHostMonitoring\":true,\"rule\":\"\\u0423\\u0431\\u0438\\u0439\\u0441\\u0442\\u0432\\u0430 \\u043C\\u0435\\u043D\\u0442\\u043E\\u0440\\u0430 \\u0441 \\u043F\\u043E\\u043B\\u043D\\u044B\\u043C \\u043D\\u0430\\u0431\\u043E\\u0440\\u043E\\u043C \\u043B\\u043E\\u0432\\u0443\\u0448\\u0435\\u043A \\u0441\\u0447\\u0438\\u0442\\u0430\\u044E\\u0442\\u0441\\u044F \\u0431\\u043E\\u043D\\u0443\\u0441\\u043D\\u044B\\u043C\\u0438 \\u0443\\u0431\\u0438\\u0439\\u0441\\u0442\\u0432\\u0430\\u043C\\u0438 \\u043A\\u043E\\u043C\\u0430\\u043D\\u0434\\u044B.\",\"stackingPolicy\":\"independentInstances\",\"resolution\":{\"type\":\"nonNegativeCount\"},\"reward\":\"bonusKills\",\"formulaReference\":{\"code\":\"bonus_kills_by_count\",\"version\":1,\"parameters\":{\"type\":\"bonusKillsByCount\",\"bonusKillsPerUnit\":1}}}", "{\"effect\":{\"mechanicType\":\"mentor\",\"traits\":[\"requires_manual_resolution\",\"kill_counter\"],\"durationSeconds\":null,\"ruleText\":null,\"scoreImpact\":{\"pointsDelta\":null,\"perKillBonus\":null,\"failurePenaltyPoints\":null,\"multiplierDelta\":null,\"killDelta\":null},\"conditions\":[],\"resolutionInputs\":[\"mentorKills\"],\"killEffect\":{\"killDeltaMode\":\"mentor_kills_as_team_kills\",\"killDeltaValue\":1,\"condition\":null,\"excludedWeapons\":[]},\"multiplierEffect\":null,\"mentorEffect\":{\"loadoutText\":\"Менторское снаряжение\",\"durationSeconds\":null,\"canBeRevived\":false,\"canBeKilled\":true,\"killsCreditToTeam\":true}}}", true, "conditional_bonus" });

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-00000000000d"),
                columns: new[] { "behavior_v2_json", "metadata_json", "requires_host_control", "scoring_type" },
                values: new object[] { "{\"schemaVersion\":2,\"kind\":\"scoring\",\"phase\":\"result\",\"performer\":\"mentor\",\"requiresHostMonitoring\":true,\"rule\":\"\\u041A\\u0430\\u0436\\u0434\\u0430\\u044F \\u0430\\u043A\\u0442\\u0438\\u0432\\u0430\\u0446\\u0438\\u044F \\u0434\\u0430\\u0451\\u0442 \\u043C\\u0435\\u043D\\u0442\\u043E\\u0440\\u0443 \\u043E\\u0440\\u0443\\u0436\\u0438\\u0435 \\u0441 \\u043E\\u0434\\u043D\\u0438\\u043C \\u0432\\u044B\\u0441\\u0442\\u0440\\u0435\\u043B\\u043E\\u043C; \\u0443\\u0441\\u043F\\u0435\\u0448\\u043D\\u044B\\u0439 \\u0432\\u044B\\u0441\\u0442\\u0440\\u0435\\u043B \\u0441\\u0447\\u0438\\u0442\\u0430\\u0435\\u0442\\u0441\\u044F \\u0431\\u043E\\u043D\\u0443\\u0441\\u043D\\u044B\\u043C \\u0443\\u0431\\u0438\\u0439\\u0441\\u0442\\u0432\\u043E\\u043C \\u043A\\u043E\\u043C\\u0430\\u043D\\u0434\\u044B.\",\"stackingPolicy\":\"independentInstances\",\"resolution\":{\"type\":\"boolean\"},\"reward\":\"bonusKills\",\"formulaReference\":{\"code\":\"bonus_kill_on_condition\",\"version\":1,\"parameters\":{\"type\":\"bonusKillOnCondition\",\"successBonusKills\":1}}}", "{\"effect\":{\"mechanicType\":\"mentor\",\"traits\":[\"requires_manual_resolution\",\"kill_counter\"],\"durationSeconds\":null,\"ruleText\":null,\"scoreImpact\":{\"pointsDelta\":null,\"perKillBonus\":null,\"failurePenaltyPoints\":null,\"multiplierDelta\":null,\"killDelta\":null},\"conditions\":[],\"resolutionInputs\":[\"mentorKills\"],\"killEffect\":{\"killDeltaMode\":\"mentor_kills_as_team_kills\",\"killDeltaValue\":1,\"condition\":null,\"excludedWeapons\":[]},\"multiplierEffect\":null,\"mentorEffect\":{\"loadoutText\":\"Менторское снаряжение\",\"durationSeconds\":null,\"canBeRevived\":false,\"canBeKilled\":true,\"killsCreditToTeam\":true}}}", true, "conditional_bonus" });

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-00000000000e"),
                columns: new[] { "behavior_v2_json", "metadata_json", "requires_host_control", "scoring_type" },
                values: new object[] { "{\"schemaVersion\":2,\"kind\":\"rule\",\"phase\":\"round\",\"performer\":\"activeTeam\",\"requiresHostMonitoring\":true,\"rule\":\"\\u041D\\u0435\\u043B\\u044C\\u0437\\u044F \\u043F\\u043E\\u0434\\u043D\\u0438\\u043C\\u0430\\u0442\\u044C \\u0441\\u043E\\u044E\\u0437\\u043D\\u0438\\u043A\\u0430, \\u043F\\u043E\\u043A\\u0430 \\u043A\\u043E\\u043C\\u0430\\u043D\\u0434\\u0430 \\u043D\\u0435 \\u0443\\u0431\\u0438\\u043B\\u0430 \\u0432\\u0440\\u0430\\u0433\\u0430.\",\"stackingPolicy\":\"aggregateParameters\",\"resolution\":{\"type\":\"ruleStatus\"},\"reward\":\"none\",\"formulaReference\":null}", "{\"effect\":{\"mechanicType\":\"rule_only\",\"traits\":[],\"durationSeconds\":null,\"ruleText\":null,\"scoreImpact\":null,\"conditions\":[],\"resolutionInputs\":[],\"killEffect\":null,\"multiplierEffect\":null,\"mentorEffect\":null}}", true, "non_scoring" });

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-00000000000f"),
                columns: new[] { "behavior_v2_json", "metadata_json", "requires_host_control", "scoring_type" },
                values: new object[] { "{\"schemaVersion\":2,\"kind\":\"scoring\",\"phase\":\"result\",\"performer\":\"activeTeam\",\"requiresHostMonitoring\":true,\"rule\":\"\\u041F\\u043E\\u0434\\u0445\\u043E\\u0434\\u044F\\u0449\\u0438\\u0435 \\u0443\\u0431\\u0438\\u0439\\u0441\\u0442\\u0432\\u0430 \\u0434\\u043E \\u0432\\u043E\\u0441\\u0441\\u0442\\u0430\\u043D\\u043E\\u0432\\u043B\\u0435\\u043D\\u0438\\u044F \\u0437\\u0434\\u043E\\u0440\\u043E\\u0432\\u044C\\u044F \\u0434\\u0430\\u044E\\u0442 \\u0434\\u043E\\u043F\\u043E\\u043B\\u043D\\u0438\\u0442\\u0435\\u043B\\u044C\\u043D\\u044B\\u0435 75% \\u0441\\u0442\\u043E\\u0438\\u043C\\u043E\\u0441\\u0442\\u0438 \\u043A\\u0430\\u0440\\u0442\\u043E\\u0447\\u043A\\u0438.\",\"stackingPolicy\":\"independentInstances\",\"resolution\":{\"type\":\"nonNegativeCount\"},\"reward\":\"points\",\"formulaReference\":{\"code\":\"window_kill_bonus_points\",\"version\":1,\"parameters\":{\"type\":\"windowKillBonusPoints\",\"bonusRate\":0.75}}}", "{\"effect\":{\"mechanicType\":\"multiplier\",\"traits\":[\"requires_manual_resolution\"],\"durationSeconds\":null,\"ruleText\":null,\"scoreImpact\":{\"pointsDelta\":null,\"perKillBonus\":null,\"failurePenaltyPoints\":null,\"multiplierDelta\":0.75,\"killDelta\":null},\"conditions\":[{\"type\":\"until_health_restored\",\"source\":\"manual_input\"}],\"resolutionInputs\":[\"killsDuringWindow\"],\"killEffect\":null,\"multiplierEffect\":{\"target\":\"kills\",\"delta\":0.75,\"activeWindow\":\"until_condition\",\"stopCondition\":\"health_restored\"},\"mentorEffect\":null}}", true, "multiplier" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_modifier_definitions_limit_positive_or_null",
                table: "modifier_definitions",
                sql: "default_limit_per_game IS NULL OR default_limit_per_game > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_game_round_modifier_results_behavior_v2_schema",
                table: "game_round_modifier_results",
                sql: "modifier_behavior_v2_snapshot_json IS NULL OR modifier_behavior_v2_snapshot_json ->> 'schemaVersion' = '2'");

            migrationBuilder.AddCheckConstraint(
                name: "ck_game_round_modifier_results_definition_revision_positive",
                table: "game_round_modifier_results",
                sql: "definition_revision_snapshot IS NULL OR definition_revision_snapshot >= 1");
        }
    }
}
