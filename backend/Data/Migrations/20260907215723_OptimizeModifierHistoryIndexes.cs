using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Data.Data.Migrations
{
    /// <inheritdoc />
    public partial class OptimizeModifierHistoryIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_modifier_versions_name_trgm;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_modifier_versions_category_trgm;");
            migrationBuilder.Sql(
                "CREATE INDEX ix_modifier_versions_name_trgm ON modifier_definition_versions USING gin (name gin_trgm_ops);");
            migrationBuilder.Sql(
                "CREATE INDEX ix_modifier_versions_category_trgm ON modifier_definition_versions USING gin (category gin_trgm_ops);");

            migrationBuilder.CreateIndex(
                name: "ix_modifier_definitions_created_at_utc_id",
                table: "modifier_definitions",
                columns: new[] { "created_at_utc", "id" },
                descending: new bool[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_modifier_definitions_created_at_utc_id",
                table: "modifier_definitions");

            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_modifier_versions_name_trgm;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_modifier_versions_category_trgm;");
            migrationBuilder.Sql(
                "CREATE INDEX ix_modifier_versions_name_trgm ON modifier_definition_versions USING gin (lower(name) gin_trgm_ops);");
            migrationBuilder.Sql(
                "CREATE INDEX ix_modifier_versions_category_trgm ON modifier_definition_versions USING gin (lower(category) gin_trgm_ops);");
        }
    }
}
