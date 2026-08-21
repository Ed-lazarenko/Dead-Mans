using backend.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260809121000_DeclareZhazhdaScoreFormula")]
    public partial class DeclareZhazhdaScoreFormula : Migration
    {
        private const string LegacyMetadata =
            "{\"effect\":{\"mechanicType\":\"restriction_with_reward\",\"traits\":[\"requires_manual_resolution\"],\"durationSeconds\":null,\"ruleText\":null,\"scoreImpact\":{\"pointsDelta\":null,\"perKillBonus\":5,\"failurePenaltyPoints\":25,\"multiplierDelta\":null,\"killDelta\":null},\"conditions\":[{\"type\":\"at_least_one_kill\",\"source\":\"manual_input\"}],\"resolutionInputs\":[\"kills\"],\"killEffect\":null,\"multiplierEffect\":null,\"mentorEffect\":null}}";

        private const string FormulaMetadata =
            "{\"effect\":{\"mechanicType\":\"restriction_with_reward\",\"traits\":[\"requires_manual_resolution\",\"stacking_per_kill_bonus\"],\"durationSeconds\":null,\"ruleText\":null,\"scoreImpact\":{\"pointsDelta\":null,\"perKillBonus\":5,\"failurePenaltyPoints\":25,\"multiplierDelta\":null,\"killDelta\":null,\"scoreFormula\":{\"mode\":\"stacking_per_kill_bonus\",\"successExpression\":null,\"failureExpression\":null}},\"conditions\":[{\"type\":\"at_least_one_kill\",\"source\":\"manual_input\"}],\"resolutionInputs\":[\"kills\"],\"killEffect\":null,\"multiplierEffect\":null,\"mentorEffect\":null}}";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE modifier_definitions "
                + "SET metadata_json = $modifier_metadata$"
                + FormulaMetadata
                + "$modifier_metadata$::jsonb "
                + "WHERE id = '10000000-0000-0000-0000-000000000002'::uuid;"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE modifier_definitions "
                + "SET metadata_json = $modifier_metadata$"
                + LegacyMetadata
                + "$modifier_metadata$::jsonb "
                + "WHERE id = '10000000-0000-0000-0000-000000000002'::uuid;"
            );
        }
    }
}
