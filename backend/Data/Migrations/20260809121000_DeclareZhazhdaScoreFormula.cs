using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class DeclareZhazhdaScoreFormula : Migration
    {
        private const string LegacyMetadata =
            "{\"effect\":{\"mechanicType\":\"restriction_with_reward\",\"traits\":[\"requires_manual_resolution\"],\"durationSeconds\":null,\"ruleText\":null,\"scoreImpact\":{\"pointsDelta\":null,\"perKillBonus\":5,\"failurePenaltyPoints\":25,\"multiplierDelta\":null,\"killDelta\":null},\"conditions\":[{\"type\":\"at_least_one_kill\",\"source\":\"manual_input\"}],\"resolutionInputs\":[\"kills\"],\"killEffect\":null,\"multiplierEffect\":null,\"mentorEffect\":null}}";

        private const string FormulaMetadata =
            "{\"effect\":{\"mechanicType\":\"restriction_with_reward\",\"traits\":[\"requires_manual_resolution\",\"stacking_per_kill_bonus\"],\"durationSeconds\":null,\"ruleText\":null,\"scoreImpact\":{\"pointsDelta\":null,\"perKillBonus\":5,\"failurePenaltyPoints\":25,\"multiplierDelta\":null,\"killDelta\":null,\"scoreFormula\":{\"mode\":\"stacking_per_kill_bonus\",\"successExpression\":null,\"failureExpression\":null}},\"conditions\":[{\"type\":\"at_least_one_kill\",\"source\":\"manual_input\"}],\"resolutionInputs\":[\"kills\"],\"killEffect\":null,\"multiplierEffect\":null,\"mentorEffect\":null}}";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "metadata_json",
                value: FormulaMetadata);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "modifier_definitions",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "metadata_json",
                value: LegacyMetadata);
        }
    }
}
