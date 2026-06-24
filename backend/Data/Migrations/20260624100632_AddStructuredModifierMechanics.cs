using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStructuredModifierMechanics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "MetadataJson",
                value: "{\"effect\":{\"mechanicType\":\"rule_only\",\"traits\":[],\"durationSeconds\":60,\"ruleText\":null,\"scoreImpact\":null,\"conditions\":[],\"resolutionInputs\":[],\"killEffect\":null,\"multiplierEffect\":null,\"mentorEffect\":null}}");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "MetadataJson",
                value: "{\"effect\":{\"mechanicType\":\"restriction_with_reward\",\"traits\":[\"requires_manual_resolution\"],\"durationSeconds\":null,\"ruleText\":null,\"scoreImpact\":{\"pointsDelta\":null,\"perKillBonus\":5,\"failurePenaltyPoints\":25,\"multiplierDelta\":null,\"killDelta\":null},\"conditions\":[{\"type\":\"at_least_one_kill\",\"source\":\"manual_input\"}],\"resolutionInputs\":[\"kills\"],\"killEffect\":null,\"multiplierEffect\":null,\"mentorEffect\":null}}");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "MetadataJson",
                value: "{\"effect\":{\"mechanicType\":\"rule_only\",\"traits\":[],\"durationSeconds\":null,\"ruleText\":null,\"scoreImpact\":null,\"conditions\":[],\"resolutionInputs\":[],\"killEffect\":null,\"multiplierEffect\":null,\"mentorEffect\":null}}");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                column: "MetadataJson",
                value: "{\"effect\":{\"mechanicType\":\"rule_only\",\"traits\":[],\"durationSeconds\":null,\"ruleText\":null,\"scoreImpact\":null,\"conditions\":[],\"resolutionInputs\":[],\"killEffect\":null,\"multiplierEffect\":null,\"mentorEffect\":null}}");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "MetadataJson",
                value: "{\"effect\":{\"mechanicType\":\"rule_only\",\"traits\":[],\"durationSeconds\":null,\"ruleText\":null,\"scoreImpact\":null,\"conditions\":[],\"resolutionInputs\":[],\"killEffect\":null,\"multiplierEffect\":null,\"mentorEffect\":null}}");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                column: "MetadataJson",
                value: "{\"effect\":{\"mechanicType\":\"kill_counter\",\"traits\":[\"requires_manual_resolution\"],\"durationSeconds\":null,\"ruleText\":null,\"scoreImpact\":{\"pointsDelta\":null,\"perKillBonus\":null,\"failurePenaltyPoints\":null,\"multiplierDelta\":null,\"killDelta\":1},\"conditions\":[{\"type\":\"first_kill_first_bullet\",\"source\":\"manual_input\"}],\"resolutionInputs\":[\"kills\"],\"killEffect\":{\"killDeltaMode\":\"conditional_bonus_kill\",\"killDeltaValue\":1,\"condition\":\"first_kill_first_bullet\",\"excludedWeapons\":[\"лук\",\"арбалет\",\"дробовик\"]},\"multiplierEffect\":null,\"mentorEffect\":null}}");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000007"),
                column: "MetadataJson",
                value: "{\"effect\":{\"mechanicType\":\"mentor\",\"traits\":[\"requires_manual_resolution\"],\"durationSeconds\":300,\"ruleText\":null,\"scoreImpact\":null,\"conditions\":[],\"resolutionInputs\":[\"mentorStatus\"],\"killEffect\":null,\"multiplierEffect\":null,\"mentorEffect\":{\"loadoutText\":\"Обманки и полтергейст\",\"durationSeconds\":300,\"canBeRevived\":false,\"canBeKilled\":false,\"killsCreditToTeam\":false}}}");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000008"),
                column: "MetadataJson",
                value: "{\"effect\":{\"mechanicType\":\"rule_only\",\"traits\":[],\"durationSeconds\":null,\"ruleText\":null,\"scoreImpact\":null,\"conditions\":[],\"resolutionInputs\":[],\"killEffect\":null,\"multiplierEffect\":null,\"mentorEffect\":null}}");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                column: "MetadataJson",
                value: "{\"effect\":{\"mechanicType\":\"mentor\",\"traits\":[\"requires_manual_resolution\"],\"durationSeconds\":300,\"ruleText\":null,\"scoreImpact\":null,\"conditions\":[],\"resolutionInputs\":[\"mentorStatus\"],\"killEffect\":null,\"multiplierEffect\":null,\"mentorEffect\":{\"loadoutText\":\"Набор шумелок\",\"durationSeconds\":300,\"canBeRevived\":false,\"canBeKilled\":true,\"killsCreditToTeam\":false}}}");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-00000000000a"),
                column: "MetadataJson",
                value: "{\"effect\":{\"mechanicType\":\"rule_only\",\"traits\":[],\"durationSeconds\":null,\"ruleText\":null,\"scoreImpact\":null,\"conditions\":[],\"resolutionInputs\":[],\"killEffect\":null,\"multiplierEffect\":null,\"mentorEffect\":null}}");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-00000000000b"),
                column: "MetadataJson",
                value: "{\"effect\":{\"mechanicType\":\"mentor\",\"traits\":[\"requires_manual_resolution\"],\"durationSeconds\":300,\"ruleText\":null,\"scoreImpact\":null,\"conditions\":[],\"resolutionInputs\":[\"mentorStatus\"],\"killEffect\":null,\"multiplierEffect\":null,\"mentorEffect\":{\"loadoutText\":\"Оружие с осветительными снарядами\",\"durationSeconds\":300,\"canBeRevived\":false,\"canBeKilled\":false,\"killsCreditToTeam\":false}}}");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-00000000000c"),
                column: "MetadataJson",
                value: "{\"effect\":{\"mechanicType\":\"mentor\",\"traits\":[\"requires_manual_resolution\",\"kill_counter\"],\"durationSeconds\":null,\"ruleText\":null,\"scoreImpact\":{\"pointsDelta\":null,\"perKillBonus\":null,\"failurePenaltyPoints\":null,\"multiplierDelta\":null,\"killDelta\":null},\"conditions\":[],\"resolutionInputs\":[\"mentorKills\"],\"killEffect\":{\"killDeltaMode\":\"mentor_kills_as_team_kills\",\"killDeltaValue\":1,\"condition\":null,\"excludedWeapons\":[]},\"multiplierEffect\":null,\"mentorEffect\":{\"loadoutText\":\"Менторское снаряжение\",\"durationSeconds\":null,\"canBeRevived\":false,\"canBeKilled\":true,\"killsCreditToTeam\":true}}}");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-00000000000d"),
                column: "MetadataJson",
                value: "{\"effect\":{\"mechanicType\":\"mentor\",\"traits\":[\"requires_manual_resolution\",\"kill_counter\"],\"durationSeconds\":null,\"ruleText\":null,\"scoreImpact\":{\"pointsDelta\":null,\"perKillBonus\":null,\"failurePenaltyPoints\":null,\"multiplierDelta\":null,\"killDelta\":null},\"conditions\":[],\"resolutionInputs\":[\"mentorKills\"],\"killEffect\":{\"killDeltaMode\":\"mentor_kills_as_team_kills\",\"killDeltaValue\":1,\"condition\":null,\"excludedWeapons\":[]},\"multiplierEffect\":null,\"mentorEffect\":{\"loadoutText\":\"Менторское снаряжение\",\"durationSeconds\":null,\"canBeRevived\":false,\"canBeKilled\":true,\"killsCreditToTeam\":true}}}");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-00000000000e"),
                column: "MetadataJson",
                value: "{\"effect\":{\"mechanicType\":\"rule_only\",\"traits\":[],\"durationSeconds\":null,\"ruleText\":null,\"scoreImpact\":null,\"conditions\":[],\"resolutionInputs\":[],\"killEffect\":null,\"multiplierEffect\":null,\"mentorEffect\":null}}");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-00000000000f"),
                column: "MetadataJson",
                value: "{\"effect\":{\"mechanicType\":\"multiplier\",\"traits\":[\"requires_manual_resolution\"],\"durationSeconds\":null,\"ruleText\":null,\"scoreImpact\":{\"pointsDelta\":null,\"perKillBonus\":null,\"failurePenaltyPoints\":null,\"multiplierDelta\":0.75,\"killDelta\":null},\"conditions\":[{\"type\":\"until_health_restored\",\"source\":\"manual_input\"}],\"resolutionInputs\":[\"killsDuringWindow\"],\"killEffect\":null,\"multiplierEffect\":{\"target\":\"kills\",\"delta\":0.75,\"activeWindow\":\"until_condition\",\"stopCondition\":\"health_restored\"},\"mentorEffect\":null}}");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "MetadataJson",
                value: null);

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "MetadataJson",
                value: "{\"bonusPerKill\":5,\"missionFailurePenalty\":25}");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "MetadataJson",
                value: null);

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                column: "MetadataJson",
                value: null);

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "MetadataJson",
                value: null);

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                column: "MetadataJson",
                value: "{\"bonusKills\":1}");

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000007"),
                column: "MetadataJson",
                value: null);

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000008"),
                column: "MetadataJson",
                value: null);

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"),
                column: "MetadataJson",
                value: null);

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-00000000000a"),
                column: "MetadataJson",
                value: null);

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-00000000000b"),
                column: "MetadataJson",
                value: null);

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-00000000000c"),
                column: "MetadataJson",
                value: null);

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-00000000000d"),
                column: "MetadataJson",
                value: null);

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-00000000000e"),
                column: "MetadataJson",
                value: null);

            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-00000000000f"),
                column: "MetadataJson",
                value: "{\"killMultiplierDelta\":0.75}");
        }
    }
}
