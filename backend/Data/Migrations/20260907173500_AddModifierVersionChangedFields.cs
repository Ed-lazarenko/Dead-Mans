using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Data.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260907173500_AddModifierVersionChangedFields")]
public sealed class AddModifierVersionChangedFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            "DROP TRIGGER IF EXISTS trg_modifier_definition_versions_immutable "
            + "ON modifier_definition_versions;");
        migrationBuilder.AddColumn<string[]>(
            name: "changed_fields",
            table: "modifier_definition_versions",
            type: "text[]",
            nullable: false,
            defaultValue: Array.Empty<string>());
        migrationBuilder.Sql(
            """
            UPDATE modifier_definition_versions
            SET changed_fields = CASE change_type
                WHEN 'compatibility_cascade' THEN ARRAY['compatibility']::text[]
                WHEN 'created' THEN ARRAY['created']::text[]
                WHEN 'migration_baseline' THEN ARRAY['created']::text[]
                ELSE ARRAY[]::text[]
            END;

            CREATE TRIGGER trg_modifier_definition_versions_immutable
            BEFORE UPDATE OR DELETE ON modifier_definition_versions
            FOR EACH ROW EXECUTE FUNCTION reject_modifier_revision_mutation();
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            "DROP TRIGGER IF EXISTS trg_modifier_definition_versions_immutable "
            + "ON modifier_definition_versions;");
        migrationBuilder.DropColumn(
            name: "changed_fields",
            table: "modifier_definition_versions");
        migrationBuilder.Sql(
            """
            CREATE TRIGGER trg_modifier_definition_versions_immutable
            BEFORE UPDATE OR DELETE ON modifier_definition_versions
            FOR EACH ROW EXECUTE FUNCTION reject_modifier_revision_mutation();
            """);
    }
}
