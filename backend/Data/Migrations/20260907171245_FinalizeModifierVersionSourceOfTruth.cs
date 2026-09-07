using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Data.Data.Migrations;

public partial class FinalizeModifierVersionSourceOfTruth : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DO $$
            BEGIN
                IF EXISTS (
                    SELECT 1
                    FROM modifier_definitions d
                    LEFT JOIN modifier_definition_versions v
                      ON v.id = d.current_version_id AND v.modifier_id = d.id
                    WHERE v.id IS NULL
                ) THEN
                    RAISE EXCEPTION 'Cannot finalize modifier versioning: a current version binding is missing';
                END IF;
            END $$;
            """);

        migrationBuilder.DropTable(name: "modifier_conflicts");
        migrationBuilder.DropIndex(
            name: "ix_modifier_definitions_is_archived_id",
            table: "modifier_definitions");

        foreach (var constraint in new[]
        {
            "ck_modifier_definitions_behavior_v2_schema",
            "ck_modifier_definitions_category_allowed",
            "ck_modifier_definitions_cost_non_negative",
            "ck_modifier_definitions_limit_positive_or_null",
            "ck_modifier_definitions_revision_positive"
        })
        {
            migrationBuilder.DropCheckConstraint(name: constraint, table: "modifier_definitions");
        }

        foreach (var column in new[]
        {
            "activation_command", "activation_cost", "behavior_v2_json", "category",
            "description", "icon_emoji", "max_activations_per_round", "name",
            "normalized_tags", "revision", "updated_at_utc"
        })
        {
            migrationBuilder.DropColumn(name: column, table: "modifier_definitions");
        }

        migrationBuilder.CreateIndex(
            name: "ix_modifier_definitions_is_archived_created_at_utc_id",
            table: "modifier_definitions",
            columns: new[] { "is_archived", "created_at_utc", "id" },
            descending: new[] { false, true, true });
        migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");
        migrationBuilder.Sql(
            "CREATE INDEX ix_modifier_versions_name_trgm ON modifier_definition_versions USING gin (lower(name) gin_trgm_ops);");
        migrationBuilder.Sql(
            "CREATE INDEX ix_modifier_versions_category_trgm ON modifier_definition_versions USING gin (lower(category) gin_trgm_ops);");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP INDEX IF EXISTS ix_modifier_versions_category_trgm;");
        migrationBuilder.Sql("DROP INDEX IF EXISTS ix_modifier_versions_name_trgm;");
        migrationBuilder.DropIndex(
            name: "ix_modifier_definitions_is_archived_created_at_utc_id",
            table: "modifier_definitions");
        migrationBuilder.AddColumn<string>(
            name: "activation_command", table: "modifier_definitions",
            type: "character varying(128)", maxLength: 128, nullable: true);
        migrationBuilder.AddColumn<int>(
            name: "activation_cost", table: "modifier_definitions",
            type: "integer", nullable: false, defaultValue: 0);
        migrationBuilder.AddColumn<string>(
            name: "behavior_v2_json", table: "modifier_definitions",
            type: "jsonb", nullable: false, defaultValue: "{\"schemaVersion\":2}");
        migrationBuilder.AddColumn<string>(
            name: "category", table: "modifier_definitions",
            type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "round");
        migrationBuilder.AddColumn<string>(
            name: "description", table: "modifier_definitions",
            type: "character varying(2000)", maxLength: 2000, nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>(
            name: "icon_emoji", table: "modifier_definitions",
            type: "character varying(16)", maxLength: 16, nullable: true);
        migrationBuilder.AddColumn<int>(
            name: "max_activations_per_round", table: "modifier_definitions",
            type: "integer", nullable: true);
        migrationBuilder.AddColumn<string>(
            name: "name", table: "modifier_definitions",
            type: "character varying(128)", maxLength: 128, nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string[]>(
            name: "normalized_tags", table: "modifier_definitions",
            type: "text[]", nullable: false, defaultValue: Array.Empty<string>());
        migrationBuilder.AddColumn<int>(
            name: "revision", table: "modifier_definitions",
            type: "integer", nullable: false, defaultValue: 1);
        migrationBuilder.AddColumn<DateTime>(
            name: "updated_at_utc", table: "modifier_definitions",
            type: "timestamp with time zone", nullable: false,
            defaultValue: DateTime.UnixEpoch);

        migrationBuilder.Sql(
            """
            UPDATE modifier_definitions AS d
            SET revision = v.revision,
                name = v.name,
                description = v.description,
                category = v.category,
                icon_emoji = v.icon_emoji,
                activation_command = v.activation_command,
                activation_cost = v.activation_cost,
                max_activations_per_round = v.max_activations_per_round,
                normalized_tags = v.normalized_tags,
                behavior_v2_json = v.behavior_v2_json,
                updated_at_utc = v.created_at_utc
            FROM modifier_definition_versions AS v
            WHERE v.id = d.current_version_id AND v.modifier_id = d.id;
            """);

        migrationBuilder.CreateTable(
            name: "modifier_conflicts",
            columns: table => new
            {
                modifier_id = table.Column<Guid>(type: "uuid", nullable: false),
                conflicts_with_modifier_id = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_modifier_conflicts", x =>
                    new { x.modifier_id, x.conflicts_with_modifier_id });
                table.CheckConstraint(
                    "ck_modifier_conflicts_distinct_ids",
                    "modifier_id <> conflicts_with_modifier_id");
                table.ForeignKey(
                    name: "fk_modifier_conflicts_modifier",
                    column: x => x.modifier_id,
                    principalTable: "modifier_definitions",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_modifier_conflicts_conflicting_modifier",
                    column: x => x.conflicts_with_modifier_id,
                    principalTable: "modifier_definitions",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.Sql(
            """
            INSERT INTO modifier_conflicts (modifier_id, conflicts_with_modifier_id)
            SELECT DISTINCT
                LEAST(v.modifier_id, c.conflicting_modifier_id),
                GREATEST(v.modifier_id, c.conflicting_modifier_id)
            FROM modifier_definition_versions AS v
            JOIN modifier_definitions AS d ON d.current_version_id = v.id
            JOIN modifier_definition_version_conflicts AS c ON c.modifier_version_id = v.id
            WHERE v.modifier_id <> c.conflicting_modifier_id
            ON CONFLICT DO NOTHING;
            """);

        migrationBuilder.CreateIndex(
            name: "ix_modifier_conflicts_conflicts_with_modifier_id",
            table: "modifier_conflicts",
            column: "conflicts_with_modifier_id");
        migrationBuilder.CreateIndex(
            name: "ix_modifier_definitions_is_archived_id",
            table: "modifier_definitions",
            columns: new[] { "is_archived", "id" });

        migrationBuilder.AddCheckConstraint(
            name: "ck_modifier_definitions_behavior_v2_schema",
            table: "modifier_definitions",
            sql: "behavior_v2_json ->> 'schemaVersion' = '2'");
        migrationBuilder.AddCheckConstraint(
            name: "ck_modifier_definitions_category_allowed",
            table: "modifier_definitions",
            sql: "category IN ('preparation','round','result')");
        migrationBuilder.AddCheckConstraint(
            name: "ck_modifier_definitions_cost_non_negative",
            table: "modifier_definitions",
            sql: "activation_cost >= 0");
        migrationBuilder.AddCheckConstraint(
            name: "ck_modifier_definitions_limit_positive_or_null",
            table: "modifier_definitions",
            sql: "max_activations_per_round IS NULL OR max_activations_per_round > 0");
        migrationBuilder.AddCheckConstraint(
            name: "ck_modifier_definitions_revision_positive",
            table: "modifier_definitions",
            sql: "revision >= 1");
    }
}
