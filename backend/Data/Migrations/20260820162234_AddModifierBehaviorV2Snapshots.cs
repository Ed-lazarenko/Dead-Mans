using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddModifierBehaviorV2Snapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "behavior_v2_json",
                table: "modifier_definitions",
                type: "jsonb",
                nullable: false,
                defaultValue: "{\"schemaVersion\":2,\"kind\":\"rule\",\"phase\":\"round\",\"performer\":\"activeTeam\",\"requiresHostMonitoring\":false,\"rule\":\"Archived legacy modifier\",\"stackingPolicy\":\"aggregateParameters\",\"resolution\":{\"type\":\"ruleStatus\"},\"reward\":\"none\",\"formulaReference\":null}");

            migrationBuilder.AddColumn<string[]>(
                name: "normalized_tags",
                table: "modifier_definitions",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);

            migrationBuilder.AddColumn<int>(
                name: "revision",
                table: "modifier_definitions",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "calculation_breakdown_json",
                table: "game_round_modifier_results",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "definition_revision_snapshot",
                table: "game_round_modifier_results",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "modifier_activation_command_snapshot",
                table: "game_round_modifier_results",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "modifier_behavior_v2_snapshot_json",
                table: "game_round_modifier_results",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string[]>(
                name: "modifier_normalized_tags_snapshot",
                table: "game_round_modifier_results",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);

            migrationBuilder.AddColumn<Guid>(
                name: "resolution_group_id",
                table: "game_round_modifier_results",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "resolution_kind",
                table: "game_round_modifier_results",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "violation_comment",
                table: "game_round_modifier_results",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "activation_command_snapshot",
                table: "game_modifier_activations",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "behavior_v2_snapshot_json",
                table: "game_modifier_activations",
                type: "jsonb",
                nullable: false,
                defaultValue: "{\"schemaVersion\":2,\"kind\":\"rule\",\"phase\":\"round\",\"performer\":\"activeTeam\",\"requiresHostMonitoring\":false,\"rule\":\"Archived legacy modifier\",\"stackingPolicy\":\"aggregateParameters\",\"resolution\":{\"type\":\"ruleStatus\"},\"reward\":\"none\",\"formulaReference\":null}");

            migrationBuilder.AddColumn<int>(
                name: "definition_revision_snapshot",
                table: "game_modifier_activations",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "legacy_effect_snapshot_json",
                table: "game_modifier_activations",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "modifier_category_snapshot",
                table: "game_modifier_activations",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "modifier_description_snapshot",
                table: "game_modifier_activations",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "modifier_icon_emoji_snapshot",
                table: "game_modifier_activations",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "modifier_mechanic_type_snapshot",
                table: "game_modifier_activations",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "modifier_name_snapshot",
                table: "game_modifier_activations",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "modifier_scoring_type_snapshot",
                table: "game_modifier_activations",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string[]>(
                name: "normalized_tags_snapshot",
                table: "game_modifier_activations",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);

            migrationBuilder.Sql(
                """
                DO $migration$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM modifier_definitions
                        WHERE NOT is_archived
                          AND id NOT IN (
                            '10000000-0000-0000-0000-000000000001'::uuid,
                            '10000000-0000-0000-0000-000000000002'::uuid,
                            '10000000-0000-0000-0000-000000000003'::uuid,
                            '10000000-0000-0000-0000-000000000004'::uuid,
                            '10000000-0000-0000-0000-000000000005'::uuid,
                            '10000000-0000-0000-0000-000000000006'::uuid,
                            '10000000-0000-0000-0000-000000000007'::uuid,
                            '10000000-0000-0000-0000-000000000008'::uuid,
                            '10000000-0000-0000-0000-000000000009'::uuid,
                            '10000000-0000-0000-0000-00000000000a'::uuid,
                            '10000000-0000-0000-0000-00000000000b'::uuid,
                            '10000000-0000-0000-0000-00000000000c'::uuid,
                            '10000000-0000-0000-0000-00000000000d'::uuid,
                            '10000000-0000-0000-0000-00000000000e'::uuid,
                            '10000000-0000-0000-0000-00000000000f'::uuid
                          )
                    ) THEN
                        RAISE EXCEPTION USING
                            ERRCODE = 'check_violation',
                            MESSAGE = 'BehaviorV2 rollout blocked: active custom modifier definitions require explicit mapping or archive.';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM modifier_definitions
                        WHERE metadata_json #> '{activationLimit,count}' IS NOT NULL
                          AND (metadata_json #>> '{activationLimit,count}')::integer
                              IS DISTINCT FROM default_limit_per_game
                    ) THEN
                        RAISE EXCEPTION USING
                            ERRCODE = 'check_violation',
                            MESSAGE = 'BehaviorV2 rollout blocked: legacy activation limit differs from default_limit_per_game.';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM modifier_definitions
                        WHERE metadata_json #>> '{effect,scoreImpact,scoreFormula,mode}' = 'custom_expression'
                          AND NOT is_archived
                    ) THEN
                        RAISE EXCEPTION USING
                            ERRCODE = 'check_violation',
                            MESSAGE = 'BehaviorV2 rollout blocked: custom expression requires explicit formula mapping or archive.';
                    END IF;
                END $migration$;
                """
            );

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                columns: new[] { "behavior_v2_json", "normalized_tags", "revision" },
                values: new object[] { "{\"schemaVersion\":2,\"kind\":\"rule\",\"phase\":\"round\",\"performer\":\"activeTeam\",\"requiresHostMonitoring\":false,\"rule\":\"\\u041F\\u0435\\u0440\\u0432\\u044B\\u0435 60 \\u0441\\u0435\\u043A\\u0443\\u043D\\u0434 \\u0437\\u0430 \\u043A\\u0430\\u0436\\u0434\\u0443\\u044E \\u0430\\u043A\\u0442\\u0438\\u0432\\u0430\\u0446\\u0438\\u044E \\u0440\\u0430\\u0437\\u0440\\u0435\\u0448\\u0435\\u043D\\u043E \\u043F\\u0435\\u0440\\u0435\\u043C\\u0435\\u0449\\u0430\\u0442\\u044C\\u0441\\u044F \\u0442\\u043E\\u043B\\u044C\\u043A\\u043E \\u043D\\u0430 \\u043A\\u043E\\u0440\\u0442\\u043E\\u0447\\u043A\\u0430\\u0445.\",\"stackingPolicy\":\"aggregateParameters\",\"resolution\":{\"type\":\"ruleStatus\"},\"reward\":\"none\",\"formulaReference\":null}", new[] { "движение", "приседание", "таймер" }, 1 });

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                columns: new[] { "behavior_v2_json", "normalized_tags", "revision" },
                values: new object[] { "{\"schemaVersion\":2,\"kind\":\"scoring\",\"phase\":\"result\",\"performer\":\"activeTeam\",\"requiresHostMonitoring\":true,\"rule\":\"\\u041A\\u0430\\u0436\\u0434\\u0430\\u044F \\u0430\\u043A\\u0442\\u0438\\u0432\\u0430\\u0446\\u0438\\u044F \\u0434\\u0430\\u0451\\u0442 \\u043D\\u0430\\u0440\\u0430\\u0441\\u0442\\u0430\\u044E\\u0449\\u0438\\u0435 \\u043E\\u0447\\u043A\\u0438 \\u0437\\u0430 \\u0443\\u0431\\u0438\\u0439\\u0441\\u0442\\u0432\\u0430 \\u0438 \\u0448\\u0442\\u0440\\u0430\\u0444 \\u043F\\u0440\\u0438 \\u043D\\u0443\\u043B\\u0435 \\u0443\\u0431\\u0438\\u0439\\u0441\\u0442\\u0432.\",\"stackingPolicy\":\"independentInstances\",\"resolution\":{\"type\":\"automaticRoundMetric\",\"metric\":\"killsCount\"},\"reward\":\"points\",\"formulaReference\":{\"code\":\"growing_kill_value\",\"version\":1,\"parameters\":{\"type\":\"growingKillValue\",\"incrementPointsPerKill\":5,\"zeroKillPenaltyPoints\":25}}}", new[] { "убийства", "очки", "бонус", "штраф", "риск" }, 1 });

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                columns: new[] { "behavior_v2_json", "normalized_tags", "revision" },
                values: new object[] { "{\"schemaVersion\":2,\"kind\":\"rule\",\"phase\":\"preparation\",\"performer\":\"activeTeam\",\"requiresHostMonitoring\":false,\"rule\":\"\\u041A\\u043E\\u043C\\u0430\\u043D\\u0434\\u0430 \\u043C\\u043E\\u0436\\u0435\\u0442 \\u0437\\u0430\\u043C\\u0435\\u043D\\u0438\\u0442\\u044C \\u043E\\u0434\\u0438\\u043D \\u0440\\u0430\\u0441\\u0445\\u043E\\u0434\\u043D\\u0438\\u043A \\u043D\\u0430 \\u0441\\u0432\\u043E\\u0439 \\u0432\\u044B\\u0431\\u043E\\u0440 \\u0437\\u0430 \\u043A\\u0430\\u0436\\u0434\\u0443\\u044E \\u0430\\u043A\\u0442\\u0438\\u0432\\u0430\\u0446\\u0438\\u044E.\",\"stackingPolicy\":\"aggregateParameters\",\"resolution\":{\"type\":\"ruleStatus\"},\"reward\":\"none\",\"formulaReference\":null}", new[] { "снаряжение", "расходники", "замена" }, 1 });

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                columns: new[] { "behavior_v2_json", "normalized_tags", "revision" },
                values: new object[] { "{\"schemaVersion\":2,\"kind\":\"rule\",\"phase\":\"round\",\"performer\":\"activeTeam\",\"requiresHostMonitoring\":true,\"rule\":\"\\u0417\\u0430\\u043F\\u0440\\u0435\\u0449\\u0435\\u043D\\u043E \\u0441\\u0436\\u0438\\u0433\\u0430\\u0442\\u044C \\u0442\\u0440\\u0443\\u043F\\u044B \\u0432\\u0435\\u0441\\u044C \\u0440\\u0430\\u0443\\u043D\\u0434.\",\"stackingPolicy\":\"aggregateParameters\",\"resolution\":{\"type\":\"ruleStatus\"},\"reward\":\"none\",\"formulaReference\":null}", new[] { "трупы", "огонь", "запрет" }, 1 });

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                columns: new[] { "behavior_v2_json", "normalized_tags", "revision" },
                values: new object[] { "{\"schemaVersion\":2,\"kind\":\"rule\",\"phase\":\"preparation\",\"performer\":\"activeTeam\",\"requiresHostMonitoring\":true,\"rule\":\"\\u0412\\u043D\\u0435\\u0448\\u043D\\u0438\\u0439 \\u043B\\u0438\\u043C\\u0438\\u0442 \\u043D\\u0430\\u0432\\u044B\\u043A\\u043E\\u0432 \\u0443\\u043C\\u0435\\u043D\\u044C\\u0448\\u0430\\u0435\\u0442\\u0441\\u044F \\u043D\\u0430 20% \\u0437\\u0430 \\u0430\\u043A\\u0442\\u0438\\u0432\\u0430\\u0446\\u0438\\u044E, \\u043D\\u043E \\u043D\\u0435 \\u0431\\u043E\\u043B\\u0435\\u0435 \\u0447\\u0435\\u043C \\u043D\\u0430 100%.\",\"stackingPolicy\":\"aggregateParameters\",\"resolution\":{\"type\":\"ruleStatus\"},\"reward\":\"none\",\"formulaReference\":null}", new[] { "навыки", "подготовка", "ограничение" }, 1 });

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                columns: new[] { "behavior_v2_json", "normalized_tags", "revision" },
                values: new object[] { "{\"schemaVersion\":2,\"kind\":\"scoring\",\"phase\":\"result\",\"performer\":\"activeTeam\",\"requiresHostMonitoring\":true,\"rule\":\"\\u0415\\u0441\\u043B\\u0438 \\u0432\\u0440\\u0430\\u0433 \\u0443\\u0431\\u0438\\u0442 \\u043F\\u0435\\u0440\\u0432\\u043E\\u0439 \\u043F\\u0443\\u043B\\u0435\\u0439 \\u043D\\u0435 \\u0438\\u0437 \\u043B\\u0443\\u043A\\u0430, \\u0430\\u0440\\u0431\\u0430\\u043B\\u0435\\u0442\\u0430 \\u0438\\u043B\\u0438 \\u0434\\u0440\\u043E\\u0431\\u043E\\u0432\\u0438\\u043A\\u0430, \\u043A\\u043E\\u043C\\u0430\\u043D\\u0434\\u0430 \\u043F\\u043E\\u043B\\u0443\\u0447\\u0430\\u0435\\u0442 \\u0431\\u043E\\u043D\\u0443\\u0441\\u043D\\u043E\\u0435 \\u0443\\u0431\\u0438\\u0439\\u0441\\u0442\\u0432\\u043E.\",\"stackingPolicy\":\"independentInstances\",\"resolution\":{\"type\":\"boolean\"},\"reward\":\"bonusKills\",\"formulaReference\":{\"code\":\"bonus_kill_on_condition\",\"version\":1,\"parameters\":{\"type\":\"bonusKillOnCondition\",\"successBonusKills\":1}}}", new[] { "оружие", "точность", "первая пуля", "исключения" }, 1 });

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000007"),
                columns: new[] { "behavior_v2_json", "normalized_tags", "revision" },
                values: new object[] { "{\"schemaVersion\":2,\"kind\":\"rule\",\"phase\":\"round\",\"performer\":\"mentor\",\"requiresHostMonitoring\":true,\"rule\":\"\\u041C\\u0435\\u043D\\u0442\\u043E\\u0440 \\u0441 \\u043E\\u0431\\u043C\\u0430\\u043D\\u043A\\u0430\\u043C\\u0438 \\u0438 \\u043F\\u043E\\u043B\\u0442\\u0435\\u0440\\u0433\\u0435\\u0439\\u0441\\u0442\\u043E\\u043C \\u043C\\u0435\\u0448\\u0430\\u0435\\u0442 \\u043A\\u043E\\u043C\\u0430\\u043D\\u0434\\u0435 300 \\u0441\\u0435\\u043A\\u0443\\u043D\\u0434 \\u0437\\u0430 \\u0430\\u043A\\u0442\\u0438\\u0432\\u0430\\u0446\\u0438\\u044E; \\u0435\\u0433\\u043E \\u043D\\u0435\\u043B\\u044C\\u0437\\u044F \\u0443\\u0431\\u0438\\u0442\\u044C \\u0438\\u043B\\u0438 \\u043F\\u043E\\u0434\\u043D\\u044F\\u0442\\u044C.\",\"stackingPolicy\":\"aggregateParameters\",\"resolution\":{\"type\":\"ruleStatus\"},\"reward\":\"none\",\"formulaReference\":null}", new[] { "ментор", "помеха", "обманки", "полтергейст", "таймер" }, 1 });

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000008"),
                columns: new[] { "behavior_v2_json", "normalized_tags", "revision" },
                values: new object[] { "{\"schemaVersion\":2,\"kind\":\"rule\",\"phase\":\"round\",\"performer\":\"activeTeam\",\"requiresHostMonitoring\":true,\"rule\":\"\\u041F\\u0440\\u0438 \\u0443\\u043F\\u043E\\u043C\\u0438\\u043D\\u0430\\u043D\\u0438\\u0438 \\u0438\\u043B\\u0438 \\u043E\\u0431\\u043D\\u0430\\u0440\\u0443\\u0436\\u0435\\u043D\\u0438\\u0438 \\u0442\\u0443\\u0430\\u043B\\u0435\\u0442\\u0430 \\u0438\\u0433\\u0440\\u043E\\u043A \\u043E\\u0431\\u044F\\u0437\\u0430\\u043D \\u0437\\u0430\\u0439\\u0442\\u0438 \\u0432 \\u043D\\u0435\\u0433\\u043E, \\u0435\\u0441\\u043B\\u0438 \\u0432\\u0440\\u0430\\u0433\\u0430 \\u043D\\u0435\\u0442 \\u0432 \\u043F\\u043E\\u043B\\u0435 \\u0437\\u0440\\u0435\\u043D\\u0438\\u044F.\",\"stackingPolicy\":\"aggregateParameters\",\"resolution\":{\"type\":\"ruleStatus\"},\"reward\":\"none\",\"formulaReference\":null}", new[] { "окружение", "туалет", "триггер" }, 1 });

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                columns: new[] { "behavior_v2_json", "normalized_tags", "revision" },
                values: new object[] { "{\"schemaVersion\":2,\"kind\":\"rule\",\"phase\":\"round\",\"performer\":\"mentor\",\"requiresHostMonitoring\":true,\"rule\":\"\\u041C\\u0435\\u043D\\u0442\\u043E\\u0440 \\u0441 \\u043D\\u0430\\u0431\\u043E\\u0440\\u043E\\u043C \\u0448\\u0443\\u043C\\u0435\\u043B\\u043E\\u043A \\u0434\\u0435\\u0439\\u0441\\u0442\\u0432\\u0443\\u0435\\u0442 300 \\u0441\\u0435\\u043A\\u0443\\u043D\\u0434; \\u0435\\u0433\\u043E \\u043C\\u043E\\u0436\\u043D\\u043E \\u0443\\u0431\\u0438\\u0442\\u044C, \\u043D\\u043E \\u043D\\u0435\\u043B\\u044C\\u0437\\u044F \\u043F\\u043E\\u0434\\u043D\\u044F\\u0442\\u044C.\",\"stackingPolicy\":\"aggregateParameters\",\"resolution\":{\"type\":\"ruleStatus\"},\"reward\":\"none\",\"formulaReference\":null}", new[] { "ментор", "шум", "приманка", "таймер" }, 1 });

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-00000000000a"),
                columns: new[] { "behavior_v2_json", "normalized_tags", "revision" },
                values: new object[] { "{\"schemaVersion\":2,\"kind\":\"rule\",\"phase\":\"round\",\"performer\":\"activeTeam\",\"requiresHostMonitoring\":true,\"rule\":\"\\u041F\\u043E\\u043B\\u044C\\u0437\\u043E\\u0432\\u0430\\u0442\\u044C\\u0441\\u044F \\u0433\\u043E\\u043B\\u043E\\u0441\\u043E\\u0432\\u044B\\u043C \\u0447\\u0430\\u0442\\u043E\\u043C \\u043C\\u043E\\u0436\\u0435\\u0442 \\u0442\\u043E\\u043B\\u044C\\u043A\\u043E \\u043A\\u0430\\u043F\\u0438\\u0442\\u0430\\u043D.\",\"stackingPolicy\":\"aggregateParameters\",\"resolution\":{\"type\":\"ruleStatus\"},\"reward\":\"none\",\"formulaReference\":null}", new[] { "коммуникация", "капитан", "голос" }, 1 });

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-00000000000b"),
                columns: new[] { "behavior_v2_json", "normalized_tags", "revision" },
                values: new object[] { "{\"schemaVersion\":2,\"kind\":\"rule\",\"phase\":\"round\",\"performer\":\"mentor\",\"requiresHostMonitoring\":true,\"rule\":\"\\u041C\\u0435\\u043D\\u0442\\u043E\\u0440 \\u0441\\u0442\\u0440\\u0435\\u043B\\u044F\\u0435\\u0442 \\u043E\\u0441\\u0432\\u0435\\u0442\\u0438\\u0442\\u0435\\u043B\\u044C\\u043D\\u044B\\u043C\\u0438 \\u0441\\u043D\\u0430\\u0440\\u044F\\u0434\\u0430\\u043C\\u0438 \\u043F\\u0440\\u0438 \\u0441\\u0442\\u0430\\u0440\\u0442\\u0435 \\u0438 \\u0447\\u0435\\u0440\\u0435\\u0437 60, 120, 180 \\u0438 240 \\u0441\\u0435\\u043A\\u0443\\u043D\\u0434; \\u0435\\u0433\\u043E \\u043D\\u0435\\u043B\\u044C\\u0437\\u044F \\u0443\\u0431\\u0438\\u0442\\u044C \\u0438\\u043B\\u0438 \\u043F\\u043E\\u0434\\u043D\\u044F\\u0442\\u044C.\",\"stackingPolicy\":\"aggregateParameters\",\"resolution\":{\"type\":\"ruleStatus\"},\"reward\":\"none\",\"formulaReference\":null}", new[] { "ментор", "сигналы", "осветительные снаряды", "таймер" }, 1 });

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-00000000000c"),
                columns: new[] { "behavior_v2_json", "normalized_tags", "revision" },
                values: new object[] { "{\"schemaVersion\":2,\"kind\":\"scoring\",\"phase\":\"result\",\"performer\":\"mentor\",\"requiresHostMonitoring\":true,\"rule\":\"\\u0423\\u0431\\u0438\\u0439\\u0441\\u0442\\u0432\\u0430 \\u043C\\u0435\\u043D\\u0442\\u043E\\u0440\\u0430 \\u0441 \\u043F\\u043E\\u043B\\u043D\\u044B\\u043C \\u043D\\u0430\\u0431\\u043E\\u0440\\u043E\\u043C \\u043B\\u043E\\u0432\\u0443\\u0448\\u0435\\u043A \\u0441\\u0447\\u0438\\u0442\\u0430\\u044E\\u0442\\u0441\\u044F \\u0431\\u043E\\u043D\\u0443\\u0441\\u043D\\u044B\\u043C\\u0438 \\u0443\\u0431\\u0438\\u0439\\u0441\\u0442\\u0432\\u0430\\u043C\\u0438 \\u043A\\u043E\\u043C\\u0430\\u043D\\u0434\\u044B.\",\"stackingPolicy\":\"independentInstances\",\"resolution\":{\"type\":\"nonNegativeCount\"},\"reward\":\"bonusKills\",\"formulaReference\":{\"code\":\"bonus_kills_by_count\",\"version\":1,\"parameters\":{\"type\":\"bonusKillsByCount\",\"bonusKillsPerUnit\":1}}}", new[] { "ментор", "ловушки", "убийства" }, 1 });

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-00000000000d"),
                columns: new[] { "behavior_v2_json", "normalized_tags", "revision" },
                values: new object[] { "{\"schemaVersion\":2,\"kind\":\"scoring\",\"phase\":\"result\",\"performer\":\"mentor\",\"requiresHostMonitoring\":true,\"rule\":\"\\u041A\\u0430\\u0436\\u0434\\u0430\\u044F \\u0430\\u043A\\u0442\\u0438\\u0432\\u0430\\u0446\\u0438\\u044F \\u0434\\u0430\\u0451\\u0442 \\u043C\\u0435\\u043D\\u0442\\u043E\\u0440\\u0443 \\u043E\\u0440\\u0443\\u0436\\u0438\\u0435 \\u0441 \\u043E\\u0434\\u043D\\u0438\\u043C \\u0432\\u044B\\u0441\\u0442\\u0440\\u0435\\u043B\\u043E\\u043C; \\u0443\\u0441\\u043F\\u0435\\u0448\\u043D\\u044B\\u0439 \\u0432\\u044B\\u0441\\u0442\\u0440\\u0435\\u043B \\u0441\\u0447\\u0438\\u0442\\u0430\\u0435\\u0442\\u0441\\u044F \\u0431\\u043E\\u043D\\u0443\\u0441\\u043D\\u044B\\u043C \\u0443\\u0431\\u0438\\u0439\\u0441\\u0442\\u0432\\u043E\\u043C \\u043A\\u043E\\u043C\\u0430\\u043D\\u0434\\u044B.\",\"stackingPolicy\":\"independentInstances\",\"resolution\":{\"type\":\"boolean\"},\"reward\":\"bonusKills\",\"formulaReference\":{\"code\":\"bonus_kill_on_condition\",\"version\":1,\"parameters\":{\"type\":\"bonusKillOnCondition\",\"successBonusKills\":1}}}", new[] { "ментор", "оружие", "один выстрел", "убийства" }, 1 });

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-00000000000e"),
                columns: new[] { "behavior_v2_json", "normalized_tags", "revision" },
                values: new object[] { "{\"schemaVersion\":2,\"kind\":\"rule\",\"phase\":\"round\",\"performer\":\"activeTeam\",\"requiresHostMonitoring\":true,\"rule\":\"\\u041D\\u0435\\u043B\\u044C\\u0437\\u044F \\u043F\\u043E\\u0434\\u043D\\u0438\\u043C\\u0430\\u0442\\u044C \\u0441\\u043E\\u044E\\u0437\\u043D\\u0438\\u043A\\u0430, \\u043F\\u043E\\u043A\\u0430 \\u043A\\u043E\\u043C\\u0430\\u043D\\u0434\\u0430 \\u043D\\u0435 \\u0443\\u0431\\u0438\\u043B\\u0430 \\u0432\\u0440\\u0430\\u0433\\u0430.\",\"stackingPolicy\":\"aggregateParameters\",\"resolution\":{\"type\":\"ruleStatus\"},\"reward\":\"none\",\"formulaReference\":null}", new[] { "оживление", "союзник", "условие" }, 1 });

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-00000000000f"),
                columns: new[] { "behavior_v2_json", "normalized_tags", "revision" },
                values: new object[] { "{\"schemaVersion\":2,\"kind\":\"scoring\",\"phase\":\"result\",\"performer\":\"activeTeam\",\"requiresHostMonitoring\":true,\"rule\":\"\\u041F\\u043E\\u0434\\u0445\\u043E\\u0434\\u044F\\u0449\\u0438\\u0435 \\u0443\\u0431\\u0438\\u0439\\u0441\\u0442\\u0432\\u0430 \\u0434\\u043E \\u0432\\u043E\\u0441\\u0441\\u0442\\u0430\\u043D\\u043E\\u0432\\u043B\\u0435\\u043D\\u0438\\u044F \\u0437\\u0434\\u043E\\u0440\\u043E\\u0432\\u044C\\u044F \\u0434\\u0430\\u044E\\u0442 \\u0434\\u043E\\u043F\\u043E\\u043B\\u043D\\u0438\\u0442\\u0435\\u043B\\u044C\\u043D\\u044B\\u0435 75% \\u0441\\u0442\\u043E\\u0438\\u043C\\u043E\\u0441\\u0442\\u0438 \\u043A\\u0430\\u0440\\u0442\\u043E\\u0447\\u043A\\u0438.\",\"stackingPolicy\":\"independentInstances\",\"resolution\":{\"type\":\"nonNegativeCount\"},\"reward\":\"points\",\"formulaReference\":{\"code\":\"window_kill_bonus_points\",\"version\":1,\"parameters\":{\"type\":\"windowKillBonusPoints\",\"bonusRate\":0.75}}}", new[] { "здоровье", "убийства", "окно действия", "бонус" }, 1 });

            migrationBuilder.Sql(
                """
                UPDATE modifier_definitions
                SET behavior_v2_json = jsonb_set(
                    jsonb_set(
                        behavior_v2_json,
                        '{formulaReference,parameters,incrementPointsPerKill}',
                        to_jsonb(COALESCE((metadata_json #>> '{effect,scoreImpact,perKillBonus}')::integer, 5))
                    ),
                    '{formulaReference,parameters,zeroKillPenaltyPoints}',
                    to_jsonb(COALESCE((metadata_json #>> '{effect,scoreImpact,failurePenaltyPoints}')::integer, 25))
                )
                WHERE id = '10000000-0000-0000-0000-000000000002'::uuid;

                UPDATE modifier_definitions
                SET behavior_v2_json = jsonb_set(
                    behavior_v2_json,
                    '{formulaReference,parameters,successBonusKills}',
                    to_jsonb(COALESCE((metadata_json #>> '{effect,killEffect,killDeltaValue}')::integer, 1))
                )
                WHERE id = '10000000-0000-0000-0000-000000000006'::uuid;

                UPDATE modifier_definitions
                SET behavior_v2_json = jsonb_set(
                    behavior_v2_json,
                    '{formulaReference,parameters,bonusRate}',
                    to_jsonb(COALESCE((metadata_json #>> '{effect,multiplierEffect,delta}')::numeric, 0.75))
                )
                WHERE id = '10000000-0000-0000-0000-00000000000f'::uuid;

                UPDATE game_modifier_activations AS activation
                SET definition_revision_snapshot = definition.revision,
                    modifier_name_snapshot = definition.name,
                    modifier_description_snapshot = definition.description,
                    modifier_category_snapshot = definition.category,
                    modifier_scoring_type_snapshot = definition.scoring_type,
                    modifier_mechanic_type_snapshot = COALESCE(
                        definition.metadata_json #>> '{effect,mechanicType}',
                        'rule_only'
                    ),
                    modifier_icon_emoji_snapshot = definition.icon_emoji,
                    legacy_effect_snapshot_json = definition.metadata_json -> 'effect',
                    activation_command_snapshot = definition.activation_command,
                    normalized_tags_snapshot = definition.normalized_tags,
                    behavior_v2_snapshot_json = definition.behavior_v2_json
                FROM modifier_definitions AS definition
                WHERE definition.id = activation.modifier_id;
                """
            );

            migrationBuilder.AddCheckConstraint(
                name: "ck_modifier_definitions_behavior_v2_schema",
                table: "modifier_definitions",
                sql: "behavior_v2_json ->> 'schemaVersion' = '2'");

            migrationBuilder.AddCheckConstraint(
                name: "ck_modifier_definitions_revision_positive",
                table: "modifier_definitions",
                sql: "revision >= 1");

            migrationBuilder.AddCheckConstraint(
                name: "ck_game_round_modifier_results_behavior_v2_schema",
                table: "game_round_modifier_results",
                sql: "modifier_behavior_v2_snapshot_json IS NULL OR modifier_behavior_v2_snapshot_json ->> 'schemaVersion' = '2'");

            migrationBuilder.AddCheckConstraint(
                name: "ck_game_round_modifier_results_definition_revision_positive",
                table: "game_round_modifier_results",
                sql: "definition_revision_snapshot IS NULL OR definition_revision_snapshot >= 1");

            migrationBuilder.AddCheckConstraint(
                name: "ck_game_modifier_activations_behavior_v2_schema",
                table: "game_modifier_activations",
                sql: "behavior_v2_snapshot_json ->> 'schemaVersion' = '2'");

            migrationBuilder.AddCheckConstraint(
                name: "ck_game_modifier_activations_definition_revision_positive",
                table: "game_modifier_activations",
                sql: "definition_revision_snapshot >= 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_modifier_definitions_behavior_v2_schema",
                table: "modifier_definitions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_modifier_definitions_revision_positive",
                table: "modifier_definitions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_game_round_modifier_results_behavior_v2_schema",
                table: "game_round_modifier_results");

            migrationBuilder.DropCheckConstraint(
                name: "ck_game_round_modifier_results_definition_revision_positive",
                table: "game_round_modifier_results");

            migrationBuilder.DropCheckConstraint(
                name: "ck_game_modifier_activations_behavior_v2_schema",
                table: "game_modifier_activations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_game_modifier_activations_definition_revision_positive",
                table: "game_modifier_activations");

            migrationBuilder.DropColumn(
                name: "behavior_v2_json",
                table: "modifier_definitions");

            migrationBuilder.DropColumn(
                name: "normalized_tags",
                table: "modifier_definitions");

            migrationBuilder.DropColumn(
                name: "revision",
                table: "modifier_definitions");

            migrationBuilder.DropColumn(
                name: "calculation_breakdown_json",
                table: "game_round_modifier_results");

            migrationBuilder.DropColumn(
                name: "definition_revision_snapshot",
                table: "game_round_modifier_results");

            migrationBuilder.DropColumn(
                name: "modifier_activation_command_snapshot",
                table: "game_round_modifier_results");

            migrationBuilder.DropColumn(
                name: "modifier_behavior_v2_snapshot_json",
                table: "game_round_modifier_results");

            migrationBuilder.DropColumn(
                name: "modifier_normalized_tags_snapshot",
                table: "game_round_modifier_results");

            migrationBuilder.DropColumn(
                name: "resolution_group_id",
                table: "game_round_modifier_results");

            migrationBuilder.DropColumn(
                name: "resolution_kind",
                table: "game_round_modifier_results");

            migrationBuilder.DropColumn(
                name: "violation_comment",
                table: "game_round_modifier_results");

            migrationBuilder.DropColumn(
                name: "activation_command_snapshot",
                table: "game_modifier_activations");

            migrationBuilder.DropColumn(
                name: "behavior_v2_snapshot_json",
                table: "game_modifier_activations");

            migrationBuilder.DropColumn(
                name: "definition_revision_snapshot",
                table: "game_modifier_activations");

            migrationBuilder.DropColumn(
                name: "legacy_effect_snapshot_json",
                table: "game_modifier_activations");

            migrationBuilder.DropColumn(
                name: "modifier_category_snapshot",
                table: "game_modifier_activations");

            migrationBuilder.DropColumn(
                name: "modifier_description_snapshot",
                table: "game_modifier_activations");

            migrationBuilder.DropColumn(
                name: "modifier_icon_emoji_snapshot",
                table: "game_modifier_activations");

            migrationBuilder.DropColumn(
                name: "modifier_mechanic_type_snapshot",
                table: "game_modifier_activations");

            migrationBuilder.DropColumn(
                name: "modifier_name_snapshot",
                table: "game_modifier_activations");

            migrationBuilder.DropColumn(
                name: "modifier_scoring_type_snapshot",
                table: "game_modifier_activations");

            migrationBuilder.DropColumn(
                name: "normalized_tags_snapshot",
                table: "game_modifier_activations");
        }
    }
}
