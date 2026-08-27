using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Data.Migrations;

public partial class GeneralizeModifierScoringModel : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE modifier_definitions
            SET behavior_v2_json = jsonb_set(
                jsonb_set(behavior_v2_json, '{resolution}', '{"type":"automaticRoundMetric","metric":"killsCount"}'::jsonb),
                '{formulaReference}', '{"code":"kill_value_increase_per_unit","version":1,"parameters":{"type":"killValueIncreasePerUnit","incrementPointsPerUnit":5,"zeroCountPenaltyPoints":25}}'::jsonb)
            WHERE id = '10000000-0000-0000-0000-000000000002'
              AND behavior_v2_json #>> '{formulaReference,code}' = 'growing_kill_value'
              AND behavior_v2_json #>> '{formulaReference,parameters,incrementPointsPerKill}' = '5'
              AND behavior_v2_json #>> '{formulaReference,parameters,zeroKillPenaltyPoints}' = '25';

            UPDATE modifier_definitions
            SET behavior_v2_json = jsonb_set(
                jsonb_set(behavior_v2_json, '{resolution}', '{"type":"boolean","inputLabel":"Условие выполнено"}'::jsonb),
                '{formulaReference}', '{"code":"bonus_kills_per_unit","version":1,"parameters":{"type":"bonusKillsPerUnit","bonusKillsPerUnit":1}}'::jsonb)
            WHERE id = '10000000-0000-0000-0000-000000000006'
              AND behavior_v2_json #>> '{formulaReference,code}' = 'bonus_kill_on_condition'
              AND behavior_v2_json #>> '{formulaReference,parameters,successBonusKills}' = '1';

            UPDATE modifier_definitions
            SET behavior_v2_json = jsonb_set(
                jsonb_set(behavior_v2_json, '{resolution}', '{"type":"nonNegativeCount","inputLabel":"Успешные убийства ведущего","maximumKind":"none","maximumPerActivation":null}'::jsonb),
                '{formulaReference}', '{"code":"bonus_kills_per_unit","version":1,"parameters":{"type":"bonusKillsPerUnit","bonusKillsPerUnit":1}}'::jsonb)
            WHERE id = '10000000-0000-0000-0000-00000000000c'
              AND behavior_v2_json #>> '{formulaReference,code}' = 'bonus_kills_by_count'
              AND behavior_v2_json #>> '{formulaReference,parameters,bonusKillsPerUnit}' = '1';

            UPDATE modifier_definitions
            SET behavior_v2_json = jsonb_set(
                jsonb_set(behavior_v2_json, '{resolution}', '{"type":"nonNegativeCount","inputLabel":"Успешные убийства ведущего","maximumKind":"activations","maximumPerActivation":1}'::jsonb),
                '{formulaReference}', '{"code":"bonus_kills_per_unit","version":1,"parameters":{"type":"bonusKillsPerUnit","bonusKillsPerUnit":1}}'::jsonb)
            WHERE id = '10000000-0000-0000-0000-00000000000d'
              AND behavior_v2_json #>> '{formulaReference,code}' = 'bonus_kill_on_condition'
              AND behavior_v2_json #>> '{formulaReference,parameters,successBonusKills}' = '1';

            UPDATE modifier_definitions
            SET behavior_v2_json = jsonb_set(
                jsonb_set(behavior_v2_json, '{resolution}', '{"type":"nonNegativeCount","inputLabel":"Подходящие убийства до восстановления здоровья","maximumKind":"resolvedKills","maximumPerActivation":null}'::jsonb),
                '{formulaReference}', '{"code":"card_percent_per_unit","version":1,"parameters":{"type":"cardPercentPerUnit","rate":0.75}}'::jsonb)
            WHERE id = '10000000-0000-0000-0000-00000000000f'
              AND behavior_v2_json #>> '{formulaReference,code}' = 'window_kill_bonus_points'
              AND behavior_v2_json #>> '{formulaReference,parameters,bonusRate}' = '0.75';
            """
        );
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE modifier_definitions
            SET behavior_v2_json = jsonb_set(
                jsonb_set(behavior_v2_json, '{resolution}', '{"type":"automaticRoundMetric","metric":"killsCount"}'::jsonb),
                '{formulaReference}', '{"code":"growing_kill_value","version":1,"parameters":{"type":"growingKillValue","incrementPointsPerKill":5,"zeroKillPenaltyPoints":25}}'::jsonb)
            WHERE id = '10000000-0000-0000-0000-000000000002'
              AND behavior_v2_json #>> '{formulaReference,code}' = 'kill_value_increase_per_unit'
              AND behavior_v2_json #>> '{formulaReference,parameters,incrementPointsPerUnit}' = '5'
              AND behavior_v2_json #>> '{formulaReference,parameters,zeroCountPenaltyPoints}' = '25';

            UPDATE modifier_definitions
            SET behavior_v2_json = jsonb_set(
                jsonb_set(behavior_v2_json, '{resolution}', '{"type":"boolean"}'::jsonb),
                '{formulaReference}', '{"code":"bonus_kill_on_condition","version":1,"parameters":{"type":"bonusKillOnCondition","successBonusKills":1}}'::jsonb)
            WHERE id IN ('10000000-0000-0000-0000-000000000006', '10000000-0000-0000-0000-00000000000d')
              AND behavior_v2_json #>> '{formulaReference,code}' = 'bonus_kills_per_unit'
              AND behavior_v2_json #>> '{formulaReference,parameters,bonusKillsPerUnit}' = '1';

            UPDATE modifier_definitions
            SET behavior_v2_json = jsonb_set(
                jsonb_set(behavior_v2_json, '{resolution}', '{"type":"nonNegativeCount"}'::jsonb),
                '{formulaReference}', '{"code":"bonus_kills_by_count","version":1,"parameters":{"type":"bonusKillsByCount","bonusKillsPerUnit":1}}'::jsonb)
            WHERE id = '10000000-0000-0000-0000-00000000000c'
              AND behavior_v2_json #>> '{formulaReference,code}' = 'bonus_kills_per_unit'
              AND behavior_v2_json #>> '{formulaReference,parameters,bonusKillsPerUnit}' = '1';

            UPDATE modifier_definitions
            SET behavior_v2_json = jsonb_set(
                jsonb_set(behavior_v2_json, '{resolution}', '{"type":"nonNegativeCount"}'::jsonb),
                '{formulaReference}', '{"code":"window_kill_bonus_points","version":1,"parameters":{"type":"windowKillBonusPoints","bonusRate":0.75}}'::jsonb)
            WHERE id = '10000000-0000-0000-0000-00000000000f'
              AND behavior_v2_json #>> '{formulaReference,code}' = 'card_percent_per_unit'
              AND behavior_v2_json #>> '{formulaReference,parameters,rate}' = '0.75';
            """
        );
    }
}
