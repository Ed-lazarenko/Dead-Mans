using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddImmutableModifierRevisions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_game_modifier_activations_modifier_id",
                table: "game_modifier_activations");

            migrationBuilder.DropIndex(
                name: "ix_game_enabled_modifiers_modifier_id",
                table: "game_enabled_modifiers");

            migrationBuilder.AddColumn<DateTime>(
                name: "archived_at_utc",
                table: "modifier_definitions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "archived_by_user_id",
                table: "modifier_definitions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_user_id",
                table: "modifier_definitions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "current_version_id",
                table: "modifier_definitions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "modifier_version_id",
                table: "game_modifier_activations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "modifier_version_id",
                table: "game_enabled_modifiers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "version_pinned_at_utc",
                table: "game_enabled_modifiers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "modifier_definition_versions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    modifier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    revision = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    icon_emoji = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    activation_command = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    activation_cost = table.Column<int>(type: "integer", nullable: false),
                    max_activations_per_round = table.Column<int>(type: "integer", nullable: true),
                    normalized_tags = table.Column<string[]>(type: "text[]", nullable: false),
                    behavior_v2_json = table.Column<string>(type: "jsonb", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_by_display_name_snapshot = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    change_note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    change_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    cascade_source_modifier_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_modifier_definition_versions", x => x.id);
                    table.UniqueConstraint("ak_modifier_definition_versions_modifier_id_id", x => new { x.modifier_id, x.id });
                    table.CheckConstraint("ck_modifier_definition_versions_behavior_v2_schema", "behavior_v2_json ->> 'schemaVersion' = '2'");
                    table.CheckConstraint("ck_modifier_definition_versions_category_allowed", "category IN ('preparation','round','result')");
                    table.CheckConstraint("ck_modifier_definition_versions_change_note", "change_note IS NULL OR length(change_note) BETWEEN 1 AND 500");
                    table.CheckConstraint("ck_modifier_definition_versions_change_type", "change_type IN ('created','edited','compatibility_cascade','migration_baseline')");
                    table.CheckConstraint("ck_modifier_definition_versions_cost_non_negative", "activation_cost >= 0");
                    table.CheckConstraint("ck_modifier_definition_versions_limit_positive_or_null", "max_activations_per_round IS NULL OR max_activations_per_round > 0");
                    table.CheckConstraint("ck_modifier_definition_versions_revision_positive", "revision >= 1");
                    table.ForeignKey(
                        name: "fk_modifier_definition_versions_modifier_definitions_cascade_s~",
                        column: x => x.cascade_source_modifier_id,
                        principalTable: "modifier_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_modifier_definition_versions_modifier_definitions_modifier_~",
                        column: x => x.modifier_id,
                        principalTable: "modifier_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_modifier_definition_versions_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "modifier_definition_version_conflicts",
                columns: table => new
                {
                    modifier_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    conflicting_modifier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    conflicting_modifier_name_snapshot = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_modifier_definition_version_conflicts", x => new { x.modifier_version_id, x.conflicting_modifier_id });
                    table.ForeignKey(
                        name: "fk_modifier_definition_version_conflicts_modifier_definition_v~",
                        column: x => x.modifier_version_id,
                        principalTable: "modifier_definition_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_modifier_definition_version_conflicts_modifier_definitions_~",
                        column: x => x.conflicting_modifier_id,
                        principalTable: "modifier_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO modifier_definition_versions (
                    id, modifier_id, revision, name, description, category, icon_emoji,
                    activation_command, activation_cost, max_activations_per_round,
                    normalized_tags, behavior_v2_json, created_at_utc,
                    created_by_user_id, created_by_display_name_snapshot, change_note,
                    change_type, cascade_source_modifier_id)
                SELECT
                    md5(d.id::text || ':baseline:' || d.revision::text)::uuid,
                    d.id, d.revision, d.name, d.description, d.category, d.icon_emoji,
                    d.activation_command, d.activation_cost, d.max_activations_per_round,
                    d.normalized_tags, d.behavior_v2_json, d.updated_at_utc,
                    NULL, 'System migration', NULL, 'migration_baseline', NULL
                FROM modifier_definitions d;

                INSERT INTO modifier_definition_version_conflicts (
                    modifier_version_id, conflicting_modifier_id,
                    conflicting_modifier_name_snapshot)
                SELECT DISTINCT
                    md5(d.id::text || ':baseline:' || d.revision::text)::uuid,
                    CASE WHEN c.modifier_id = d.id
                         THEN c.conflicts_with_modifier_id ELSE c.modifier_id END,
                    other.name
                FROM modifier_definitions d
                JOIN modifier_conflicts c
                  ON c.modifier_id = d.id OR c.conflicts_with_modifier_id = d.id
                JOIN modifier_definitions other
                  ON other.id = CASE WHEN c.modifier_id = d.id
                                     THEN c.conflicts_with_modifier_id ELSE c.modifier_id END;

                UPDATE modifier_definitions d
                SET current_version_id = md5(d.id::text || ':baseline:' || d.revision::text)::uuid;

                UPDATE game_enabled_modifiers e
                SET modifier_version_id = d.current_version_id,
                    version_pinned_at_utc = COALESCE(g.started_at_utc, CURRENT_TIMESTAMP)
                FROM modifier_definitions d, games g
                WHERE e.modifier_id = d.id
                  AND e.game_id = g.id
                  AND g.status = 'active'
                  AND NOT g.is_deleted;

                UPDATE game_modifier_activations a
                SET modifier_version_id = e.modifier_version_id
                FROM game_enabled_modifiers e, games g
                WHERE a.game_id = e.game_id
                  AND a.modifier_id = e.modifier_id
                  AND e.game_id = g.id
                  AND g.status = 'active'
                  AND NOT g.is_deleted;
                """
            );

            migrationBuilder.CreateIndex(
                name: "ix_modifier_definitions_archived_by_user_id",
                table: "modifier_definitions",
                column: "archived_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_modifier_definitions_created_by_user_id",
                table: "modifier_definitions",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_modifier_definitions_current_version_id",
                table: "modifier_definitions",
                column: "current_version_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_modifier_definitions_id_current_version_id",
                table: "modifier_definitions",
                columns: new[] { "id", "current_version_id" });

            migrationBuilder.CreateIndex(
                name: "ix_modifier_definitions_is_archived_id",
                table: "modifier_definitions",
                columns: new[] { "is_archived", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_game_modifier_activations_modifier_id_modifier_version_id",
                table: "game_modifier_activations",
                columns: new[] { "modifier_id", "modifier_version_id" });

            migrationBuilder.CreateIndex(
                name: "ix_game_modifier_activations_version_game",
                table: "game_modifier_activations",
                columns: new[] { "modifier_version_id", "game_id" });

            migrationBuilder.CreateIndex(
                name: "ix_game_enabled_modifiers_modifier_id_modifier_version_id",
                table: "game_enabled_modifiers",
                columns: new[] { "modifier_id", "modifier_version_id" });

            migrationBuilder.CreateIndex(
                name: "ix_game_enabled_modifiers_modifier_version_id_game_id",
                table: "game_enabled_modifiers",
                columns: new[] { "modifier_version_id", "game_id" });

            migrationBuilder.CreateIndex(
                name: "ix_modifier_definition_version_conflicts_conflicting_modifier_~",
                table: "modifier_definition_version_conflicts",
                column: "conflicting_modifier_id");

            migrationBuilder.CreateIndex(
                name: "ix_modifier_definition_versions_cascade_source_modifier_id",
                table: "modifier_definition_versions",
                column: "cascade_source_modifier_id");

            migrationBuilder.CreateIndex(
                name: "ix_modifier_definition_versions_created_by_user_id",
                table: "modifier_definition_versions",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_modifier_definition_versions_modifier_id_created_at_utc_id",
                table: "modifier_definition_versions",
                columns: new[] { "modifier_id", "created_at_utc", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_modifier_definition_versions_modifier_id_revision",
                table: "modifier_definition_versions",
                columns: new[] { "modifier_id", "revision" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_game_enabled_modifiers_modifier_definition_versions_modifie~",
                table: "game_enabled_modifiers",
                columns: new[] { "modifier_id", "modifier_version_id" },
                principalTable: "modifier_definition_versions",
                principalColumns: new[] { "modifier_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_game_modifier_activations_modifier_definition_versions_modi~",
                table: "game_modifier_activations",
                columns: new[] { "modifier_id", "modifier_version_id" },
                principalTable: "modifier_definition_versions",
                principalColumns: new[] { "modifier_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_modifier_definitions_modifier_definition_versions_id_curren~",
                table: "modifier_definitions",
                columns: new[] { "id", "current_version_id" },
                principalTable: "modifier_definition_versions",
                principalColumns: new[] { "modifier_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_modifier_definitions_users_archived_by_user_id",
                table: "modifier_definitions",
                column: "archived_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_modifier_definitions_users_created_by_user_id",
                table: "modifier_definitions",
                column: "created_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                """
                CREATE OR REPLACE FUNCTION reject_modifier_revision_mutation()
                RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN
                    RAISE EXCEPTION 'modifier revision rows are immutable'
                        USING ERRCODE = '55000';
                END;
                $$;

                CREATE TRIGGER trg_modifier_definition_versions_immutable
                BEFORE UPDATE OR DELETE ON modifier_definition_versions
                FOR EACH ROW EXECUTE FUNCTION reject_modifier_revision_mutation();

                CREATE TRIGGER trg_modifier_definition_version_conflicts_immutable
                BEFORE UPDATE OR DELETE ON modifier_definition_version_conflicts
                FOR EACH ROW EXECUTE FUNCTION reject_modifier_revision_mutation();
                """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TRIGGER IF EXISTS trg_modifier_definition_version_conflicts_immutable
                    ON modifier_definition_version_conflicts;
                DROP TRIGGER IF EXISTS trg_modifier_definition_versions_immutable
                    ON modifier_definition_versions;
                DROP FUNCTION IF EXISTS reject_modifier_revision_mutation();

                UPDATE modifier_definitions d
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
                FROM modifier_definition_versions v
                WHERE d.id = v.modifier_id AND d.current_version_id = v.id;
                """
            );
            migrationBuilder.DropForeignKey(
                name: "fk_game_enabled_modifiers_modifier_definition_versions_modifie~",
                table: "game_enabled_modifiers");

            migrationBuilder.DropForeignKey(
                name: "fk_game_modifier_activations_modifier_definition_versions_modi~",
                table: "game_modifier_activations");

            migrationBuilder.DropForeignKey(
                name: "fk_modifier_definitions_modifier_definition_versions_id_curren~",
                table: "modifier_definitions");

            migrationBuilder.DropForeignKey(
                name: "fk_modifier_definitions_users_archived_by_user_id",
                table: "modifier_definitions");

            migrationBuilder.DropForeignKey(
                name: "fk_modifier_definitions_users_created_by_user_id",
                table: "modifier_definitions");

            migrationBuilder.DropTable(
                name: "modifier_definition_version_conflicts");

            migrationBuilder.DropTable(
                name: "modifier_definition_versions");

            migrationBuilder.DropIndex(
                name: "ix_modifier_definitions_archived_by_user_id",
                table: "modifier_definitions");

            migrationBuilder.DropIndex(
                name: "ix_modifier_definitions_created_by_user_id",
                table: "modifier_definitions");

            migrationBuilder.DropIndex(
                name: "ix_modifier_definitions_current_version_id",
                table: "modifier_definitions");

            migrationBuilder.DropIndex(
                name: "ix_modifier_definitions_id_current_version_id",
                table: "modifier_definitions");

            migrationBuilder.DropIndex(
                name: "ix_modifier_definitions_is_archived_id",
                table: "modifier_definitions");

            migrationBuilder.DropIndex(
                name: "ix_game_modifier_activations_modifier_id_modifier_version_id",
                table: "game_modifier_activations");

            migrationBuilder.DropIndex(
                name: "ix_game_modifier_activations_version_game",
                table: "game_modifier_activations");

            migrationBuilder.DropIndex(
                name: "ix_game_enabled_modifiers_modifier_id_modifier_version_id",
                table: "game_enabled_modifiers");

            migrationBuilder.DropIndex(
                name: "ix_game_enabled_modifiers_modifier_version_id_game_id",
                table: "game_enabled_modifiers");

            migrationBuilder.DropColumn(
                name: "archived_at_utc",
                table: "modifier_definitions");

            migrationBuilder.DropColumn(
                name: "archived_by_user_id",
                table: "modifier_definitions");

            migrationBuilder.DropColumn(
                name: "created_by_user_id",
                table: "modifier_definitions");

            migrationBuilder.DropColumn(
                name: "current_version_id",
                table: "modifier_definitions");

            migrationBuilder.DropColumn(
                name: "modifier_version_id",
                table: "game_modifier_activations");

            migrationBuilder.DropColumn(
                name: "modifier_version_id",
                table: "game_enabled_modifiers");

            migrationBuilder.DropColumn(
                name: "version_pinned_at_utc",
                table: "game_enabled_modifiers");

            migrationBuilder.CreateIndex(
                name: "ix_game_modifier_activations_modifier_id",
                table: "game_modifier_activations",
                column: "modifier_id");

            migrationBuilder.CreateIndex(
                name: "ix_game_enabled_modifiers_modifier_id",
                table: "game_enabled_modifiers",
                column: "modifier_id");
        }
    }
}
