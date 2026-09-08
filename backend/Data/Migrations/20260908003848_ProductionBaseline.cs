using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace backend.Data.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260908003848_ProductionBaseline")]
public class ProductionBaseline : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterDatabase().Annotation("Npgsql:PostgresExtension:citext", ",,").Annotation("Npgsql:PostgresExtension:pg_trgm", ",,");
        migrationBuilder.CreateTable("media_assets", delegate (ColumnsBuilder table)
        {
            OperationBuilder<AddColumnOperation> id = table.Column<Guid>("uuid");
            int? maxLength = 128;
            OperationBuilder<AddColumnOperation> bucket = table.Column<string>("character varying(128)", null, maxLength);
            maxLength = 1024;
            OperationBuilder<AddColumnOperation> object_key = table.Column<string>("character varying(1024)", null, maxLength);
            maxLength = 256;
            return new
            {
                id = id,
                bucket = bucket,
                object_key = object_key,
                mime_type = table.Column<string>("character varying(256)", null, maxLength),
                size_bytes = table.Column<long>("bigint"),
                created_at_utc = table.Column<DateTime>("timestamp with time zone")
            };
        }, null, table =>
        {
            table.PrimaryKey("pk_media_assets", x => x.id);
            table.CheckConstraint("ck_media_assets_mime_type_not_blank", "length(trim(mime_type)) > 0");
            table.CheckConstraint("ck_media_assets_size_positive", "size_bytes > 0");
            table.CheckConstraint("ck_media_assets_storage_identity_not_blank", "length(trim(bucket)) > 0 AND length(trim(object_key)) > 0");
        });
        migrationBuilder.CreateTable("question_categories", delegate (ColumnsBuilder table)
        {
            OperationBuilder<AddColumnOperation> id = table.Column<Guid>("uuid");
            int? maxLength = 64;
            return new
            {
                id = id,
                name = table.Column<string>("citext", null, maxLength),
                created_at_utc = table.Column<DateTime>("timestamp with time zone"),
                updated_at_utc = table.Column<DateTime>("timestamp with time zone")
            };
        }, null, table =>
        {
            table.PrimaryKey("pk_question_categories", x => x.id);
            table.CheckConstraint("ck_question_categories_name_not_blank", "length(trim(name)) > 0");
            table.CheckConstraint("ck_question_categories_timestamps", "updated_at_utc >= created_at_utc");
        });
        migrationBuilder.CreateTable("roles", delegate (ColumnsBuilder table)
        {
            OperationBuilder<AddColumnOperation> id = table.Column<short>("smallint");
            int? maxLength = 32;
            OperationBuilder<AddColumnOperation> code = table.Column<string>("citext", null, maxLength);
            maxLength = 64;
            OperationBuilder<AddColumnOperation> name = table.Column<string>("character varying(64)", null, maxLength);
            maxLength = 256;
            return new
            {
                id = id,
                code = code,
                name = name,
                description = table.Column<string>("character varying(256)", null, maxLength, rowVersion: false, null, nullable: true),
                created_at_utc = table.Column<DateTime>("timestamp with time zone"),
                updated_at_utc = table.Column<DateTime>("timestamp with time zone")
            };
        }, null, table =>
        {
            table.PrimaryKey("pk_roles", x => x.id);
            table.CheckConstraint("ck_roles_identity_not_blank", "length(trim(code)) > 0 AND length(trim(name)) > 0");
            table.CheckConstraint("ck_roles_timestamps", "updated_at_utc >= created_at_utc");
        });
        migrationBuilder.CreateTable("users", delegate (ColumnsBuilder table)
        {
            OperationBuilder<AddColumnOperation> id = table.Column<Guid>("uuid");
            int? maxLength = 64;
            OperationBuilder<AddColumnOperation> twitch_user_id = table.Column<string>("character varying(64)", null, maxLength);
            maxLength = 64;
            OperationBuilder<AddColumnOperation> login = table.Column<string>("citext", null, maxLength);
            maxLength = 64;
            OperationBuilder<AddColumnOperation> display_name = table.Column<string>("character varying(64)", null, maxLength);
            maxLength = 1024;
            OperationBuilder<AddColumnOperation> profile_image_url = table.Column<string>("character varying(1024)", null, maxLength, rowVersion: false, null, nullable: true);
            maxLength = 32;
            OperationBuilder<AddColumnOperation> broadcaster_type = table.Column<string>("character varying(32)", null, maxLength, rowVersion: false, null, nullable: true);
            maxLength = 32;
            return new
            {
                id = id,
                twitch_user_id = twitch_user_id,
                login = login,
                display_name = display_name,
                profile_image_url = profile_image_url,
                broadcaster_type = broadcaster_type,
                twitch_user_type = table.Column<string>("character varying(32)", null, maxLength, rowVersion: false, null, nullable: true),
                is_active = table.Column<bool>("boolean", null, null, rowVersion: false, null, nullable: false, true),
                last_login_at_utc = table.Column<DateTime>("timestamp with time zone", null, null, rowVersion: false, null, nullable: true),
                created_at_utc = table.Column<DateTime>("timestamp with time zone"),
                updated_at_utc = table.Column<DateTime>("timestamp with time zone")
            };
        }, null, table =>
        {
            table.PrimaryKey("pk_users", x => x.id);
            table.CheckConstraint("ck_users_timestamps", "updated_at_utc >= created_at_utc AND (last_login_at_utc IS NULL OR last_login_at_utc >= created_at_utc)");
            table.CheckConstraint("ck_users_twitch_identity_not_blank", "length(trim(twitch_user_id)) > 0 AND length(trim(login)) > 0 AND length(trim(display_name)) > 0");
        });
        migrationBuilder.CreateTable("question_definitions", delegate (ColumnsBuilder table)
        {
            OperationBuilder<AddColumnOperation> id = table.Column<Guid>("uuid");
            int? maxLength = 64;
            OperationBuilder<AddColumnOperation> external_code = table.Column<string>("citext", null, maxLength);
            OperationBuilder<AddColumnOperation> category_id = table.Column<Guid>("uuid");
            maxLength = 2000;
            return new
            {
                id = id,
                external_code = external_code,
                category_id = category_id,
                text = table.Column<string>("character varying(2000)", null, maxLength),
                reward = table.Column<int>("integer"),
                revision = table.Column<int>("integer", null, null, rowVersion: false, null, nullable: false, 1),
                is_enabled = table.Column<bool>("boolean", null, null, rowVersion: false, null, nullable: false, true),
                is_deleted = table.Column<bool>("boolean", null, null, rowVersion: false, null, nullable: false, false),
                deleted_at_utc = table.Column<DateTime>("timestamp with time zone", null, null, rowVersion: false, null, nullable: true),
                priority = table.Column<int>("integer", null, null, rowVersion: false, null, nullable: false, 0),
                created_at_utc = table.Column<DateTime>("timestamp with time zone"),
                updated_at_utc = table.Column<DateTime>("timestamp with time zone")
            };
        }, null, table =>
        {
            table.PrimaryKey("pk_question_definitions", x => x.id);
            table.CheckConstraint("ck_question_definitions_content_not_blank", "length(trim(external_code)) > 0 AND length(trim(text)) > 0");
            table.CheckConstraint("ck_question_definitions_revision_positive", "revision > 0");
            table.CheckConstraint("ck_question_definitions_reward_non_negative", "reward >= 0");
            table.CheckConstraint("ck_question_definitions_soft_delete_semantics", "(is_deleted = FALSE AND deleted_at_utc IS NULL) OR (is_deleted = TRUE AND is_enabled = FALSE AND deleted_at_utc IS NOT NULL)");
            table.CheckConstraint("ck_question_definitions_timestamps", "updated_at_utc >= created_at_utc AND (deleted_at_utc IS NULL OR deleted_at_utc >= created_at_utc)");
            table.ForeignKey("fk_question_definitions_question_categories_category_id", x => x.category_id, "question_categories", "id", null, ReferentialAction.NoAction, ReferentialAction.Restrict);
        });
        migrationBuilder.CreateTable("user_roles", (ColumnsBuilder table) => new
        {
            user_id = table.Column<Guid>("uuid"),
            role_id = table.Column<short>("smallint"),
            assigned_by_user_id = table.Column<Guid>("uuid", null, null, rowVersion: false, null, nullable: true),
            assigned_at_utc = table.Column<DateTime>("timestamp with time zone"),
            expires_at_utc = table.Column<DateTime>("timestamp with time zone", null, null, rowVersion: false, null, nullable: true)
        }, null, table =>
        {
            table.PrimaryKey("pk_user_roles", x => new { x.user_id, x.role_id });
            table.CheckConstraint("ck_user_roles_expiry_after_assignment", "expires_at_utc IS NULL OR expires_at_utc > assigned_at_utc");
            table.ForeignKey("fk_user_roles_roles_role_id", x => x.role_id, "roles", "id", null, ReferentialAction.NoAction, ReferentialAction.Restrict);
            table.ForeignKey("fk_user_roles_users_assigned_by_user_id", x => x.assigned_by_user_id, "users", "id", null, ReferentialAction.NoAction, ReferentialAction.Restrict);
            table.ForeignKey("fk_user_roles_users_user_id", x => x.user_id, "users", "id", null, ReferentialAction.NoAction, ReferentialAction.Cascade);
        });
        migrationBuilder.CreateTable("question_accepted_answers", delegate (ColumnsBuilder table)
        {
            OperationBuilder<AddColumnOperation> id = table.Column<Guid>("uuid");
            OperationBuilder<AddColumnOperation> question_id = table.Column<Guid>("uuid");
            int? maxLength = 500;
            OperationBuilder<AddColumnOperation> answer_text = table.Column<string>("character varying(500)", null, maxLength);
            maxLength = 500;
            return new
            {
                id = id,
                question_id = question_id,
                answer_text = answer_text,
                normalized_answer = table.Column<string>("character varying(500)", null, maxLength),
                is_primary = table.Column<bool>("boolean"),
                sort_order = table.Column<int>("integer"),
                created_at_utc = table.Column<DateTime>("timestamp with time zone")
            };
        }, null, table =>
        {
            table.PrimaryKey("pk_question_accepted_answers", x => x.id);
            table.CheckConstraint("ck_question_accepted_answers_sort_order_non_negative", "sort_order >= 0");
            table.CheckConstraint("ck_question_accepted_answers_text_not_blank", "length(trim(answer_text)) > 0 AND length(trim(normalized_answer)) > 0");
            table.ForeignKey("fk_question_accepted_answers_question_definitions_question_id", x => x.question_id, "question_definitions", "id", null, ReferentialAction.NoAction, ReferentialAction.Cascade);
        });
        migrationBuilder.CreateTable("game_board_cell_media", delegate (ColumnsBuilder table)
        {
            OperationBuilder<AddColumnOperation> id = table.Column<Guid>("uuid");
            OperationBuilder<AddColumnOperation> cell_id = table.Column<Guid>("uuid");
            OperationBuilder<AddColumnOperation> media_asset_id = table.Column<Guid>("uuid");
            int? maxLength = 32;
            return new
            {
                id = id,
                cell_id = cell_id,
                media_asset_id = media_asset_id,
                role = table.Column<string>("character varying(32)", null, maxLength),
                sort_order = table.Column<int>("integer")
            };
        }, null, table =>
        {
            table.PrimaryKey("pk_game_board_cell_media", x => x.id);
            table.CheckConstraint("ck_game_board_cell_media_role_not_blank", "length(trim(role)) > 0");
            table.CheckConstraint("ck_game_board_cell_media_sort_order_non_negative", "sort_order >= 0");
            table.ForeignKey("fk_game_board_cell_media_media_assets_media_asset_id", x => x.media_asset_id, "media_assets", "id", null, ReferentialAction.NoAction, ReferentialAction.Cascade);
        });
        migrationBuilder.CreateTable("game_board_cells", delegate (ColumnsBuilder table)
        {
            OperationBuilder<AddColumnOperation> id = table.Column<Guid>("uuid");
            OperationBuilder<AddColumnOperation> board_id = table.Column<Guid>("uuid");
            OperationBuilder<AddColumnOperation> row_index = table.Column<int>("integer");
            OperationBuilder<AddColumnOperation> col_index = table.Column<int>("integer");
            int? maxLength = 32;
            OperationBuilder<AddColumnOperation> state = table.Column<string>("character varying(32)", null, maxLength);
            maxLength = 32;
            OperationBuilder<AddColumnOperation> cell_type = table.Column<string>("character varying(32)", null, maxLength);
            maxLength = 200;
            OperationBuilder<AddColumnOperation> title = table.Column<string>("character varying(200)", null, maxLength, rowVersion: false, null, nullable: true);
            OperationBuilder<AddColumnOperation> cost = table.Column<int>("integer");
            maxLength = 2000;
            return new
            {
                id = id,
                board_id = board_id,
                row_index = row_index,
                col_index = col_index,
                state = state,
                cell_type = cell_type,
                title = title,
                cost = cost,
                description = table.Column<string>("character varying(2000)", null, maxLength, rowVersion: false, null, nullable: true)
            };
        }, null, table =>
        {
            table.PrimaryKey("pk_game_board_cells", x => x.id);
            table.UniqueConstraint("ak_game_board_cells_board_id_id", x => new { x.board_id, x.id });
            table.CheckConstraint("ck_game_board_cells_coordinates_non_negative", "row_index >= 0 AND col_index >= 0");
            table.CheckConstraint("ck_game_board_cells_cost_non_negative", "cost >= 0");
            table.CheckConstraint("ck_game_board_cells_state_allowed", "state IN ('open','closed','cancelled')");
            table.CheckConstraint("ck_game_board_cells_type_not_blank", "length(trim(cell_type)) > 0");
        });
        migrationBuilder.CreateTable("game_boards", (ColumnsBuilder table) => new
        {
            id = table.Column<Guid>("uuid"),
            game_id = table.Column<Guid>("uuid"),
            version = table.Column<int>("integer", null, null, rowVersion: false, null, nullable: false, 1),
            rows = table.Column<int>("integer"),
            cols = table.Column<int>("integer"),
            row_labels = table.Column<string[]>("text[]"),
            col_labels = table.Column<string[]>("text[]"),
            created_at_utc = table.Column<DateTime>("timestamp with time zone")
        }, null, table =>
        {
            table.PrimaryKey("pk_game_boards", x => x.id);
            table.UniqueConstraint("ak_game_boards_game_id_id", x => new { x.game_id, x.id });
            table.CheckConstraint("ck_game_boards_dimensions_positive", "rows BETWEEN 1 AND 20 AND cols BETWEEN 1 AND 12");
            table.CheckConstraint("ck_game_boards_labels_match_dimensions", "cardinality(row_labels) = rows AND cardinality(col_labels) = cols");
            table.CheckConstraint("ck_game_boards_version_positive", "version > 0");
        });
        migrationBuilder.CreateTable("game_enabled_modifiers", delegate (ColumnsBuilder table)
        {
            OperationBuilder<AddColumnOperation> game_id = table.Column<Guid>("uuid");
            OperationBuilder<AddColumnOperation> modifier_id = table.Column<Guid>("uuid");
            OperationBuilder<AddColumnOperation> modifier_version_id = table.Column<Guid>("uuid", null, null, rowVersion: false, null, nullable: true);
            OperationBuilder<AddColumnOperation> version_pinned_at_utc = table.Column<DateTime>("timestamp with time zone", null, null, rowVersion: false, null, nullable: true);
            OperationBuilder<AddColumnOperation> enabled_at_utc = table.Column<DateTime>("timestamp with time zone");
            OperationBuilder<AddColumnOperation> emergency_disabled_at_utc = table.Column<DateTime>("timestamp with time zone", null, null, rowVersion: false, null, nullable: true);
            OperationBuilder<AddColumnOperation> emergency_disabled_by_user_id = table.Column<Guid>("uuid", null, null, rowVersion: false, null, nullable: true);
            int? maxLength = 1000;
            return new
            {
                game_id = game_id,
                modifier_id = modifier_id,
                modifier_version_id = modifier_version_id,
                version_pinned_at_utc = version_pinned_at_utc,
                enabled_at_utc = enabled_at_utc,
                emergency_disabled_at_utc = emergency_disabled_at_utc,
                emergency_disabled_by_user_id = emergency_disabled_by_user_id,
                emergency_disable_reason = table.Column<string>("character varying(1000)", null, maxLength, rowVersion: false, null, nullable: true)
            };
        }, null, table =>
        {
            table.PrimaryKey("pk_game_enabled_modifiers", x => new { x.game_id, x.modifier_id });
            table.CheckConstraint("ck_game_enabled_modifiers_emergency_disable_audit", "(emergency_disabled_at_utc IS NULL AND emergency_disabled_by_user_id IS NULL AND emergency_disable_reason IS NULL) OR (emergency_disabled_at_utc IS NOT NULL AND emergency_disabled_by_user_id IS NOT NULL AND emergency_disable_reason IS NOT NULL AND length(btrim(emergency_disable_reason)) BETWEEN 1 AND 1000 AND emergency_disabled_at_utc >= enabled_at_utc)");
            table.CheckConstraint("ck_game_enabled_modifiers_version_pin_pair", "(modifier_version_id IS NULL AND version_pinned_at_utc IS NULL) OR (modifier_version_id IS NOT NULL AND version_pinned_at_utc IS NOT NULL AND version_pinned_at_utc >= enabled_at_utc)");
            table.ForeignKey("fk_game_enabled_modifiers_users_emergency_disabled_by_user_id", x => x.emergency_disabled_by_user_id, "users", "id", null, ReferentialAction.NoAction, ReferentialAction.Restrict);
        });
        migrationBuilder.CreateTable("game_enabled_questions", delegate (ColumnsBuilder table)
        {
            OperationBuilder<AddColumnOperation> game_id = table.Column<Guid>("uuid");
            OperationBuilder<AddColumnOperation> question_id = table.Column<Guid>("uuid");
            OperationBuilder<AddColumnOperation> enabled_at_utc = table.Column<DateTime>("timestamp with time zone");
            OperationBuilder<AddColumnOperation> question_revision_snapshot = table.Column<int>("integer");
            int? maxLength = 64;
            OperationBuilder<AddColumnOperation> question_code_snapshot = table.Column<string>("character varying(64)", null, maxLength);
            maxLength = 64;
            OperationBuilder<AddColumnOperation> category_name_snapshot = table.Column<string>("character varying(64)", null, maxLength);
            maxLength = 2000;
            return new
            {
                game_id = game_id,
                question_id = question_id,
                enabled_at_utc = enabled_at_utc,
                question_revision_snapshot = question_revision_snapshot,
                question_code_snapshot = question_code_snapshot,
                category_name_snapshot = category_name_snapshot,
                question_text_snapshot = table.Column<string>("character varying(2000)", null, maxLength),
                accepted_answers_snapshot = table.Column<string[]>("text[]"),
                normalized_answers_snapshot = table.Column<string[]>("text[]"),
                reward_snapshot = table.Column<int>("integer"),
                priority_snapshot = table.Column<int>("integer"),
                snapshot_at_utc = table.Column<DateTime>("timestamp with time zone")
            };
        }, null, table =>
        {
            table.PrimaryKey("pk_game_enabled_questions", x => new { x.game_id, x.question_id });
            table.CheckConstraint("ck_game_enabled_questions_answers_present", "cardinality(accepted_answers_snapshot) > 0 AND cardinality(accepted_answers_snapshot) = cardinality(normalized_answers_snapshot)");
            table.CheckConstraint("ck_game_enabled_questions_content_not_blank", "length(trim(question_code_snapshot)) > 0 AND length(trim(category_name_snapshot)) > 0 AND length(trim(question_text_snapshot)) > 0");
            table.CheckConstraint("ck_game_enabled_questions_revision_positive", "question_revision_snapshot > 0");
            table.CheckConstraint("ck_game_enabled_questions_reward_non_negative", "reward_snapshot >= 0");
            table.ForeignKey("fk_game_enabled_questions_question_definitions_question_id", x => x.question_id, "question_definitions", "id", null, ReferentialAction.NoAction, ReferentialAction.Restrict);
        });
        migrationBuilder.CreateTable("game_finalizations", delegate (ColumnsBuilder table)
        {
            OperationBuilder<AddColumnOperation> game_id = table.Column<Guid>("uuid");
            OperationBuilder<AddColumnOperation> request_id = table.Column<Guid>("uuid");
            OperationBuilder<AddColumnOperation> finished_by_user_id = table.Column<Guid>("uuid");
            int? maxLength = 128;
            OperationBuilder<AddColumnOperation> finished_by_display_name_snapshot = table.Column<string>("character varying(128)", null, maxLength);
            OperationBuilder<AddColumnOperation> finished_at_utc = table.Column<DateTime>("timestamp with time zone");
            maxLength = 2000;
            return new
            {
                game_id = game_id,
                request_id = request_id,
                finished_by_user_id = finished_by_user_id,
                finished_by_display_name_snapshot = finished_by_display_name_snapshot,
                finished_at_utc = finished_at_utc,
                public_note = table.Column<string>("character varying(2000)", null, maxLength, rowVersion: false, null, nullable: true),
                calculation_version = table.Column<int>("integer"),
                completed_round_count = table.Column<int>("integer"),
                cancelled_round_count = table.Column<int>("integer"),
                total_kills = table.Column<int>("integer"),
                total_bounties = table.Column<int>("integer"),
                quiz_total_points = table.Column<int>("integer"),
                skipped_quiz_question_count = table.Column<int>("integer")
            };
        }, null, table =>
        {
            table.PrimaryKey("pk_game_finalizations", x => x.game_id);
            table.CheckConstraint("ck_game_finalizations_calculation_version_positive", "calculation_version > 0");
            table.CheckConstraint("ck_game_finalizations_counts_non_negative", "completed_round_count >= 0 AND cancelled_round_count >= 0 AND total_kills >= 0 AND total_bounties >= 0 AND quiz_total_points >= 0 AND skipped_quiz_question_count >= 0");
            table.CheckConstraint("ck_game_finalizations_display_name_not_blank", "length(trim(finished_by_display_name_snapshot)) > 0");
            table.ForeignKey("fk_game_finalizations_users_finished_by_user_id", x => x.finished_by_user_id, "users", "id", null, ReferentialAction.NoAction, ReferentialAction.Restrict);
        });
        migrationBuilder.CreateTable("game_modifier_activations", delegate (ColumnsBuilder table)
        {
            OperationBuilder<AddColumnOperation> id = table.Column<Guid>("uuid");
            OperationBuilder<AddColumnOperation> game_id = table.Column<Guid>("uuid");
            OperationBuilder<AddColumnOperation> round_id = table.Column<Guid>("uuid");
            OperationBuilder<AddColumnOperation> modifier_id = table.Column<Guid>("uuid");
            OperationBuilder<AddColumnOperation> modifier_version_id = table.Column<Guid>("uuid");
            OperationBuilder<AddColumnOperation> activated_by_user_id = table.Column<Guid>("uuid");
            OperationBuilder<AddColumnOperation> initiated_by_user_id = table.Column<Guid>("uuid");
            OperationBuilder<AddColumnOperation> activation_cost_snapshot = table.Column<int>("integer");
            OperationBuilder<AddColumnOperation> definition_revision_snapshot = table.Column<int>("integer");
            int? maxLength = 128;
            OperationBuilder<AddColumnOperation> modifier_name_snapshot = table.Column<string>("character varying(128)", null, maxLength);
            maxLength = 2000;
            OperationBuilder<AddColumnOperation> modifier_description_snapshot = table.Column<string>("character varying(2000)", null, maxLength);
            maxLength = 32;
            OperationBuilder<AddColumnOperation> modifier_category_snapshot = table.Column<string>("character varying(32)", null, maxLength);
            maxLength = 16;
            OperationBuilder<AddColumnOperation> modifier_icon_emoji_snapshot = table.Column<string>("character varying(16)", null, maxLength, rowVersion: false, null, nullable: true);
            maxLength = 128;
            OperationBuilder<AddColumnOperation> activation_command_snapshot = table.Column<string>("character varying(128)", null, maxLength, rowVersion: false, null, nullable: true);
            OperationBuilder<AddColumnOperation> normalized_tags_snapshot = table.Column<string[]>("text[]");
            OperationBuilder<AddColumnOperation> behavior_v2_snapshot_json = table.Column<string>("jsonb");
            OperationBuilder<AddColumnOperation> activated_at_utc = table.Column<DateTime>("timestamp with time zone");
            maxLength = 16;
            OperationBuilder<AddColumnOperation> status = table.Column<string>("character varying(16)", null, maxLength);
            OperationBuilder<AddColumnOperation> archived_at_utc = table.Column<DateTime>("timestamp with time zone", null, null, rowVersion: false, null, nullable: true);
            OperationBuilder<AddColumnOperation> cancelled_by_user_id = table.Column<Guid>("uuid", null, null, rowVersion: false, null, nullable: true);
            OperationBuilder<AddColumnOperation> cancelled_at_utc = table.Column<DateTime>("timestamp with time zone", null, null, rowVersion: false, null, nullable: true);
            maxLength = 1000;
            return new
            {
                id = id,
                game_id = game_id,
                round_id = round_id,
                modifier_id = modifier_id,
                modifier_version_id = modifier_version_id,
                activated_by_user_id = activated_by_user_id,
                initiated_by_user_id = initiated_by_user_id,
                activation_cost_snapshot = activation_cost_snapshot,
                definition_revision_snapshot = definition_revision_snapshot,
                modifier_name_snapshot = modifier_name_snapshot,
                modifier_description_snapshot = modifier_description_snapshot,
                modifier_category_snapshot = modifier_category_snapshot,
                modifier_icon_emoji_snapshot = modifier_icon_emoji_snapshot,
                activation_command_snapshot = activation_command_snapshot,
                normalized_tags_snapshot = normalized_tags_snapshot,
                behavior_v2_snapshot_json = behavior_v2_snapshot_json,
                activated_at_utc = activated_at_utc,
                status = status,
                archived_at_utc = archived_at_utc,
                cancelled_by_user_id = cancelled_by_user_id,
                cancelled_at_utc = cancelled_at_utc,
                cancellation_reason = table.Column<string>("character varying(1000)", null, maxLength, rowVersion: false, null, nullable: true),
                refund_amount = table.Column<int>("integer")
            };
        }, null, table =>
        {
            table.PrimaryKey("pk_game_modifier_activations", x => x.id);
            table.UniqueConstraint("ak_game_modifier_activations_game_id_id", x => new { x.game_id, x.id });
            table.UniqueConstraint("ak_game_modifier_activations_round_id_id_modifier_id", x => new { x.round_id, x.id, x.modifier_id });
            table.CheckConstraint("ck_game_modifier_activations_behavior_v2_schema", "jsonb_typeof(behavior_v2_snapshot_json) = 'object' AND behavior_v2_snapshot_json ->> 'schemaVersion' = '2'");
            table.CheckConstraint("ck_game_modifier_activations_cost_snapshot_non_negative", "activation_cost_snapshot >= 0");
            table.CheckConstraint("ck_game_modifier_activations_definition_revision_positive", "definition_revision_snapshot >= 1");
            table.CheckConstraint("ck_game_modifier_activations_lifecycle_semantics", "(status = 'active' AND archived_at_utc IS NULL AND cancelled_at_utc IS NULL AND cancelled_by_user_id IS NULL AND cancellation_reason IS NULL AND refund_amount = 0) OR (status = 'consumed' AND cancelled_at_utc IS NULL AND cancelled_by_user_id IS NULL AND cancellation_reason IS NULL AND refund_amount = 0) OR (status = 'cancelled' AND archived_at_utc IS NOT NULL AND cancelled_at_utc IS NOT NULL AND cancelled_by_user_id IS NOT NULL AND refund_amount = activation_cost_snapshot)");
            table.CheckConstraint("ck_game_modifier_activations_refund_range", "refund_amount >= 0 AND refund_amount <= activation_cost_snapshot");
            table.CheckConstraint("ck_game_modifier_activations_snapshot_not_blank", "length(trim(modifier_name_snapshot)) > 0 AND length(trim(modifier_description_snapshot)) > 0 AND length(trim(modifier_category_snapshot)) > 0");
            table.CheckConstraint("ck_game_modifier_activations_status_allowed", "status IN ('active','consumed','cancelled')");
            table.CheckConstraint("ck_game_modifier_activations_timestamp_order", "(archived_at_utc IS NULL OR archived_at_utc >= activated_at_utc) AND (cancelled_at_utc IS NULL OR (cancelled_at_utc >= activated_at_utc AND archived_at_utc = cancelled_at_utc))");
            table.ForeignKey("fk_game_modifier_activations_enabled_modifier", x => new { x.game_id, x.modifier_id }, "game_enabled_modifiers", new string[2] { "game_id", "modifier_id" }, null, ReferentialAction.NoAction, ReferentialAction.Restrict);
            table.ForeignKey("fk_game_modifier_activations_users_activated_by_user_id", x => x.activated_by_user_id, "users", "id", null, ReferentialAction.NoAction, ReferentialAction.Restrict);
            table.ForeignKey("fk_game_modifier_activations_users_cancelled_by_user_id", x => x.cancelled_by_user_id, "users", "id", null, ReferentialAction.NoAction, ReferentialAction.Restrict);
            table.ForeignKey("fk_game_modifier_activations_users_initiated_by_user_id", x => x.initiated_by_user_id, "users", "id", null, ReferentialAction.NoAction, ReferentialAction.Restrict);
        });
        migrationBuilder.CreateTable("game_quiz_correct_answers", delegate (ColumnsBuilder table)
        {
            OperationBuilder<AddColumnOperation> id = table.Column<Guid>("uuid");
            OperationBuilder<AddColumnOperation> game_id = table.Column<Guid>("uuid");
            OperationBuilder<AddColumnOperation> quiz_round_id = table.Column<Guid>("uuid");
            OperationBuilder<AddColumnOperation> awarded_to_user_id = table.Column<Guid>("uuid");
            OperationBuilder<AddColumnOperation> captured_by_user_id = table.Column<Guid>("uuid", null, null, rowVersion: false, null, nullable: true);
            int? maxLength = 64;
            OperationBuilder<AddColumnOperation> twitch_user_id_snapshot = table.Column<string>("character varying(64)", null, maxLength);
            maxLength = 64;
            OperationBuilder<AddColumnOperation> login_snapshot = table.Column<string>("character varying(64)", null, maxLength);
            maxLength = 128;
            OperationBuilder<AddColumnOperation> display_name_snapshot = table.Column<string>("character varying(128)", null, maxLength);
            maxLength = 500;
            OperationBuilder<AddColumnOperation> submitted_answer = table.Column<string>("character varying(500)", null, maxLength);
            maxLength = 500;
            OperationBuilder<AddColumnOperation> normalized_answer = table.Column<string>("character varying(500)", null, maxLength);
            maxLength = 32;
            OperationBuilder<AddColumnOperation> source_provider = table.Column<string>("character varying(32)", null, maxLength);
            maxLength = 128;
            OperationBuilder<AddColumnOperation> source_channel_id = table.Column<string>("character varying(128)", null, maxLength, rowVersion: false, null, nullable: true);
            maxLength = 128;
            return new
            {
                id = id,
                game_id = game_id,
                quiz_round_id = quiz_round_id,
                awarded_to_user_id = awarded_to_user_id,
                captured_by_user_id = captured_by_user_id,
                twitch_user_id_snapshot = twitch_user_id_snapshot,
                login_snapshot = login_snapshot,
                display_name_snapshot = display_name_snapshot,
                submitted_answer = submitted_answer,
                normalized_answer = normalized_answer,
                source_provider = source_provider,
                source_channel_id = source_channel_id,
                source_message_id = table.Column<string>("character varying(128)", null, maxLength, rowVersion: false, null, nullable: true),
                answered_at_utc = table.Column<DateTime>("timestamp with time zone")
            };
        }, null, table =>
        {
            table.PrimaryKey("pk_game_quiz_correct_answers", x => x.id);
            table.UniqueConstraint("ak_game_quiz_correct_answers_game_id_id", x => new { x.game_id, x.id });
            table.CheckConstraint("ck_game_quiz_correct_answers_answer_not_blank", "length(trim(submitted_answer)) > 0 AND length(trim(normalized_answer)) > 0");
            table.CheckConstraint("ck_game_quiz_correct_answers_identity_snapshots_not_blank", "length(trim(twitch_user_id_snapshot)) > 0 AND length(trim(login_snapshot)) > 0 AND length(trim(display_name_snapshot)) > 0");
            table.CheckConstraint("ck_game_quiz_correct_answers_source_allowed", "source_provider IN ('manual','twitch')");
            table.CheckConstraint("ck_game_quiz_correct_answers_source_semantics", "(source_provider = 'manual' AND source_channel_id IS NULL AND source_message_id IS NULL) OR (source_provider = 'twitch' AND source_channel_id IS NOT NULL AND source_message_id IS NOT NULL AND length(trim(source_channel_id)) > 0 AND length(trim(source_message_id)) > 0)");
            table.ForeignKey("fk_game_quiz_correct_answers_users_awarded_to_user_id", x => x.awarded_to_user_id, "users", "id", null, ReferentialAction.NoAction, ReferentialAction.Restrict);
            table.ForeignKey("fk_game_quiz_correct_answers_users_captured_by_user_id", x => x.captured_by_user_id, "users", "id", null, ReferentialAction.NoAction, ReferentialAction.Restrict);
        });
        migrationBuilder.CreateTable("game_quiz_point_ledger_entries", delegate (ColumnsBuilder table)
        {
            OperationBuilder<AddColumnOperation> id = table.Column<Guid>("uuid");
            OperationBuilder<AddColumnOperation> sequence_number = table.Column<long>("bigint").Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
            OperationBuilder<AddColumnOperation> game_id = table.Column<Guid>("uuid");
            OperationBuilder<AddColumnOperation> user_id = table.Column<Guid>("uuid");
            int? maxLength = 32;
            OperationBuilder<AddColumnOperation> entry_type = table.Column<string>("character varying(32)", null, maxLength);
            OperationBuilder<AddColumnOperation> points_delta = table.Column<int>("integer");
            OperationBuilder<AddColumnOperation> correct_answer_id = table.Column<Guid>("uuid", null, null, rowVersion: false, null, nullable: true);
            OperationBuilder<AddColumnOperation> modifier_activation_id = table.Column<Guid>("uuid", null, null, rowVersion: false, null, nullable: true);
            OperationBuilder<AddColumnOperation> manual_request_id = table.Column<Guid>("uuid", null, null, rowVersion: false, null, nullable: true);
            OperationBuilder<AddColumnOperation> created_by_user_id = table.Column<Guid>("uuid", null, null, rowVersion: false, null, nullable: true);
            maxLength = 500;
            return new
            {
                id = id,
                sequence_number = sequence_number,
                game_id = game_id,
                user_id = user_id,
                entry_type = entry_type,
                points_delta = points_delta,
                correct_answer_id = correct_answer_id,
                modifier_activation_id = modifier_activation_id,
                manual_request_id = manual_request_id,
                created_by_user_id = created_by_user_id,
                reason = table.Column<string>("character varying(500)", null, maxLength, rowVersion: false, null, nullable: true),
                available_points_before = table.Column<long>("bigint"),
                available_points_after = table.Column<long>("bigint"),
                occurred_at_utc = table.Column<DateTime>("timestamp with time zone")
            };
        }, null, table =>
        {
            table.PrimaryKey("pk_game_quiz_point_ledger_entries", x => x.id);
            table.CheckConstraint("ck_quiz_point_ledger_balance_audit", "available_points_before >= 0 AND available_points_after >= 0 AND available_points_after = available_points_before + points_delta");
            table.CheckConstraint("ck_quiz_point_ledger_entry_type_allowed", "entry_type IN ('quiz_reward','manual_adjustment','modifier_purchase','modifier_refund')");
            table.CheckConstraint("ck_quiz_point_ledger_nonzero_delta", "points_delta <> 0");
            table.CheckConstraint("ck_quiz_point_ledger_source_semantics", "(entry_type = 'quiz_reward' AND points_delta > 0 AND correct_answer_id IS NOT NULL AND modifier_activation_id IS NULL AND manual_request_id IS NULL AND created_by_user_id IS NULL AND reason IS NULL) OR (entry_type = 'manual_adjustment' AND correct_answer_id IS NULL AND modifier_activation_id IS NULL AND manual_request_id IS NOT NULL AND created_by_user_id IS NOT NULL AND reason IS NOT NULL AND length(trim(reason)) BETWEEN 3 AND 500) OR (entry_type = 'modifier_purchase' AND points_delta < 0 AND correct_answer_id IS NULL AND modifier_activation_id IS NOT NULL AND manual_request_id IS NULL AND created_by_user_id IS NOT NULL AND reason IS NULL) OR (entry_type = 'modifier_refund' AND points_delta > 0 AND correct_answer_id IS NULL AND modifier_activation_id IS NOT NULL AND manual_request_id IS NULL AND created_by_user_id IS NOT NULL AND (reason IS NULL OR length(trim(reason)) BETWEEN 3 AND 500))");
            table.ForeignKey("fk_game_quiz_point_ledger_entries_users_created_by_user_id", x => x.created_by_user_id, "users", "id", null, ReferentialAction.NoAction, ReferentialAction.Restrict);
            table.ForeignKey("fk_game_quiz_point_ledger_entries_users_user_id", x => x.user_id, "users", "id", null, ReferentialAction.NoAction, ReferentialAction.Restrict);
            table.ForeignKey("fk_quiz_point_ledger_correct_answer_same_game", x => new { x.game_id, x.correct_answer_id }, "game_quiz_correct_answers", new string[2] { "game_id", "id" }, null, ReferentialAction.NoAction, ReferentialAction.Restrict);
            table.ForeignKey("fk_quiz_point_ledger_modifier_activation_same_game", x => new { x.game_id, x.modifier_activation_id }, "game_modifier_activations", new string[2] { "game_id", "id" }, null, ReferentialAction.NoAction, ReferentialAction.Restrict);
        });
        migrationBuilder.CreateTable("game_quiz_rounds", delegate (ColumnsBuilder table)
        {
            OperationBuilder<AddColumnOperation> id = table.Column<Guid>("uuid");
            OperationBuilder<AddColumnOperation> game_id = table.Column<Guid>("uuid");
            OperationBuilder<AddColumnOperation> question_id = table.Column<Guid>("uuid");
            OperationBuilder<AddColumnOperation> ask_order = table.Column<int>("integer");
            OperationBuilder<AddColumnOperation> asked_at_utc = table.Column<DateTime>("timestamp with time zone");
            OperationBuilder<AddColumnOperation> closes_at_utc = table.Column<DateTime>("timestamp with time zone");
            OperationBuilder<AddColumnOperation> closed_at_utc = table.Column<DateTime>("timestamp with time zone", null, null, rowVersion: false, null, nullable: true);
            OperationBuilder<AddColumnOperation> asked_by_user_id = table.Column<Guid>("uuid", null, null, rowVersion: false, null, nullable: true);
            int? maxLength = 32;
            OperationBuilder<AddColumnOperation> status = table.Column<string>("character varying(32)", null, maxLength);
            OperationBuilder<AddColumnOperation> question_revision_snapshot = table.Column<int>("integer");
            maxLength = 64;
            OperationBuilder<AddColumnOperation> question_code_snapshot = table.Column<string>("character varying(64)", null, maxLength);
            maxLength = 64;
            OperationBuilder<AddColumnOperation> category_name_snapshot = table.Column<string>("character varying(64)", null, maxLength);
            maxLength = 2000;
            OperationBuilder<AddColumnOperation> question_text_snapshot = table.Column<string>("character varying(2000)", null, maxLength);
            OperationBuilder<AddColumnOperation> accepted_answers_snapshot = table.Column<string[]>("text[]");
            OperationBuilder<AddColumnOperation> normalized_answers_snapshot = table.Column<string[]>("text[]");
            OperationBuilder<AddColumnOperation> reward_snapshot = table.Column<int>("integer");
            maxLength = 32;
            OperationBuilder<AddColumnOperation> delivery_kind = table.Column<string>("character varying(32)", null, maxLength);
            maxLength = 128;
            OperationBuilder<AddColumnOperation> source_channel_id = table.Column<string>("character varying(128)", null, maxLength, rowVersion: false, null, nullable: true);
            maxLength = 128;
            return new
            {
                id = id,
                game_id = game_id,
                question_id = question_id,
                ask_order = ask_order,
                asked_at_utc = asked_at_utc,
                closes_at_utc = closes_at_utc,
                closed_at_utc = closed_at_utc,
                asked_by_user_id = asked_by_user_id,
                status = status,
                question_revision_snapshot = question_revision_snapshot,
                question_code_snapshot = question_code_snapshot,
                category_name_snapshot = category_name_snapshot,
                question_text_snapshot = question_text_snapshot,
                accepted_answers_snapshot = accepted_answers_snapshot,
                normalized_answers_snapshot = normalized_answers_snapshot,
                reward_snapshot = reward_snapshot,
                delivery_kind = delivery_kind,
                source_channel_id = source_channel_id,
                source_message_id = table.Column<string>("character varying(128)", null, maxLength, rowVersion: false, null, nullable: true)
            };
        }, null, table =>
        {
            table.PrimaryKey("pk_game_quiz_rounds", x => x.id);
            table.UniqueConstraint("ak_game_quiz_rounds_game_id_id", x => new { x.game_id, x.id });
            table.CheckConstraint("ck_game_quiz_rounds_ask_order_positive", "ask_order > 0");
            table.CheckConstraint("ck_game_quiz_rounds_close_semantics", "((status = 'asked') AND closed_at_utc IS NULL) OR ((status IN ('answered_correct','timeout','skipped')) AND closed_at_utc IS NOT NULL)");
            table.CheckConstraint("ck_game_quiz_rounds_delivery_kind_allowed", "delivery_kind IN ('manual','twitch')");
            table.CheckConstraint("ck_game_quiz_rounds_delivery_source_semantics", "(delivery_kind = 'manual' AND source_channel_id IS NULL AND source_message_id IS NULL) OR (delivery_kind = 'twitch' AND source_channel_id IS NOT NULL AND length(trim(source_channel_id)) > 0 AND (source_message_id IS NULL OR length(trim(source_message_id)) > 0))");
            table.CheckConstraint("ck_game_quiz_rounds_snapshot", "question_revision_snapshot > 0 AND reward_snapshot >= 0 AND length(trim(question_code_snapshot)) > 0 AND length(trim(category_name_snapshot)) > 0 AND length(trim(question_text_snapshot)) > 0 AND cardinality(accepted_answers_snapshot) > 0 AND cardinality(accepted_answers_snapshot) = cardinality(normalized_answers_snapshot)");
            table.CheckConstraint("ck_game_quiz_rounds_status_allowed", "status IN ('asked','answered_correct','timeout','skipped')");
            table.CheckConstraint("ck_game_quiz_rounds_window", "closes_at_utc > asked_at_utc AND (closed_at_utc IS NULL OR (closed_at_utc >= asked_at_utc AND closed_at_utc <= closes_at_utc))");
            table.ForeignKey("fk_game_quiz_rounds_enabled_question", x => new { x.game_id, x.question_id }, "game_enabled_questions", new string[2] { "game_id", "question_id" }, null, ReferentialAction.NoAction, ReferentialAction.Restrict);
            table.ForeignKey("fk_game_quiz_rounds_question_definitions_question_id", x => x.question_id, "question_definitions", "id", null, ReferentialAction.NoAction, ReferentialAction.Restrict);
            table.ForeignKey("fk_game_quiz_rounds_users_asked_by_user_id", x => x.asked_by_user_id, "users", "id", null, ReferentialAction.NoAction, ReferentialAction.Restrict);
        });
        migrationBuilder.CreateTable("game_round_cell_media", delegate (ColumnsBuilder table)
        {
            OperationBuilder<AddColumnOperation> id = table.Column<Guid>("uuid");
            OperationBuilder<AddColumnOperation> round_id = table.Column<Guid>("uuid");
            int? maxLength = 128;
            OperationBuilder<AddColumnOperation> bucket = table.Column<string>("character varying(128)", null, maxLength);
            maxLength = 1024;
            OperationBuilder<AddColumnOperation> object_key = table.Column<string>("character varying(1024)", null, maxLength);
            maxLength = 256;
            OperationBuilder<AddColumnOperation> mime_type = table.Column<string>("character varying(256)", null, maxLength);
            OperationBuilder<AddColumnOperation> size_bytes = table.Column<long>("bigint");
            maxLength = 32;
            return new
            {
                id = id,
                round_id = round_id,
                bucket = bucket,
                object_key = object_key,
                mime_type = mime_type,
                size_bytes = size_bytes,
                role = table.Column<string>("character varying(32)", null, maxLength),
                sort_order = table.Column<int>("integer"),
                created_at_utc = table.Column<DateTime>("timestamp with time zone")
            };
        }, null, table =>
        {
            table.PrimaryKey("pk_game_round_cell_media", x => x.id);
            table.CheckConstraint("ck_game_round_cell_media_mime_type_not_blank", "length(trim(mime_type)) > 0");
            table.CheckConstraint("ck_game_round_cell_media_role_not_blank", "length(trim(role)) > 0");
            table.CheckConstraint("ck_game_round_cell_media_size_positive", "size_bytes > 0");
            table.CheckConstraint("ck_game_round_cell_media_sort_order_non_negative", "sort_order >= 0");
            table.CheckConstraint("ck_game_round_cell_media_storage_identity_not_blank", "length(trim(bucket)) > 0 AND length(trim(object_key)) > 0");
        });
        migrationBuilder.CreateTable("game_round_modifier_results", delegate (ColumnsBuilder table)
        {
            OperationBuilder<AddColumnOperation> id = table.Column<Guid>("uuid");
            OperationBuilder<AddColumnOperation> round_id = table.Column<Guid>("uuid");
            OperationBuilder<AddColumnOperation> modifier_activation_id = table.Column<Guid>("uuid");
            OperationBuilder<AddColumnOperation> modifier_id = table.Column<Guid>("uuid");
            int? maxLength = 128;
            OperationBuilder<AddColumnOperation> modifier_name_snapshot = table.Column<string>("character varying(128)", null, maxLength);
            maxLength = 32;
            OperationBuilder<AddColumnOperation> modifier_category_snapshot = table.Column<string>("character varying(32)", null, maxLength);
            maxLength = 2000;
            OperationBuilder<AddColumnOperation> modifier_description_snapshot = table.Column<string>("character varying(2000)", null, maxLength);
            OperationBuilder<AddColumnOperation> definition_revision_snapshot = table.Column<int>("integer");
            maxLength = 128;
            OperationBuilder<AddColumnOperation> modifier_activation_command_snapshot = table.Column<string>("character varying(128)", null, maxLength, rowVersion: false, null, nullable: true);
            OperationBuilder<AddColumnOperation> modifier_normalized_tags_snapshot = table.Column<string[]>("text[]");
            OperationBuilder<AddColumnOperation> modifier_behavior_v2_snapshot_json = table.Column<string>("jsonb");
            maxLength = 32;
            OperationBuilder<AddColumnOperation> outcome_status = table.Column<string>("character varying(32)", null, maxLength);
            OperationBuilder<AddColumnOperation> score_delta = table.Column<int>("integer");
            OperationBuilder<AddColumnOperation> kill_delta = table.Column<int>("integer");
            OperationBuilder<AddColumnOperation> multiplier_applied = table.Column<decimal>("numeric", null, null, rowVersion: false, null, nullable: true);
            OperationBuilder<AddColumnOperation> resolution_data_json = table.Column<string>("jsonb", null, null, rowVersion: false, null, nullable: true);
            OperationBuilder<AddColumnOperation> resolution_group_id = table.Column<Guid>("uuid", null, null, rowVersion: false, null, nullable: true);
            maxLength = 32;
            OperationBuilder<AddColumnOperation> resolution_kind = table.Column<string>("character varying(32)", null, maxLength, rowVersion: false, null, nullable: true);
            maxLength = 1000;
            return new
            {
                id = id,
                round_id = round_id,
                modifier_activation_id = modifier_activation_id,
                modifier_id = modifier_id,
                modifier_name_snapshot = modifier_name_snapshot,
                modifier_category_snapshot = modifier_category_snapshot,
                modifier_description_snapshot = modifier_description_snapshot,
                definition_revision_snapshot = definition_revision_snapshot,
                modifier_activation_command_snapshot = modifier_activation_command_snapshot,
                modifier_normalized_tags_snapshot = modifier_normalized_tags_snapshot,
                modifier_behavior_v2_snapshot_json = modifier_behavior_v2_snapshot_json,
                outcome_status = outcome_status,
                score_delta = score_delta,
                kill_delta = kill_delta,
                multiplier_applied = multiplier_applied,
                resolution_data_json = resolution_data_json,
                resolution_group_id = resolution_group_id,
                resolution_kind = resolution_kind,
                violation_comment = table.Column<string>("character varying(1000)", null, maxLength, rowVersion: false, null, nullable: true),
                calculation_breakdown_json = table.Column<string>("jsonb", null, null, rowVersion: false, null, nullable: true),
                resolved_by_user_id = table.Column<Guid>("uuid", null, null, rowVersion: false, null, nullable: true),
                resolved_at_utc = table.Column<DateTime>("timestamp with time zone", null, null, rowVersion: false, null, nullable: true),
                created_at_utc = table.Column<DateTime>("timestamp with time zone"),
                updated_at_utc = table.Column<DateTime>("timestamp with time zone")
            };
        }, null, table =>
        {
            table.PrimaryKey("pk_game_round_modifier_results", x => x.id);
            table.CheckConstraint("ck_game_round_modifier_results_behavior_v2_schema", "jsonb_typeof(modifier_behavior_v2_snapshot_json) = 'object' AND modifier_behavior_v2_snapshot_json ->> 'schemaVersion' = '2'");
            table.CheckConstraint("ck_game_round_modifier_results_json_objects", "(resolution_data_json IS NULL OR jsonb_typeof(resolution_data_json) = 'object') AND (calculation_breakdown_json IS NULL OR jsonb_typeof(calculation_breakdown_json) = 'object')");
            table.CheckConstraint("ck_game_round_modifier_results_definition_revision_positive", "definition_revision_snapshot >= 1");
            table.CheckConstraint("ck_game_round_modifier_results_resolution_semantics", "((outcome_status = 'pending') AND resolved_at_utc IS NULL AND resolved_by_user_id IS NULL) OR ((outcome_status <> 'pending') AND resolved_at_utc IS NOT NULL AND resolved_by_user_id IS NOT NULL)");
            table.CheckConstraint("ck_game_round_modifier_results_snapshot_not_blank", "length(trim(modifier_name_snapshot)) > 0 AND length(trim(modifier_description_snapshot)) > 0 AND length(trim(modifier_category_snapshot)) > 0");
            table.CheckConstraint("ck_game_round_modifier_results_status_allowed", "outcome_status IN ('pending','completed','failed','cancelled','violated','not_triggered','succeeded','not_succeeded','calculated')");
            table.ForeignKey("fk_game_round_modifier_results_users_resolved_by_user_id", x => x.resolved_by_user_id, "users", "id", null, ReferentialAction.NoAction, ReferentialAction.Restrict);
            table.ForeignKey("fk_modifier_results_activation_same_round_modifier", x => new { x.round_id, x.modifier_activation_id, x.modifier_id }, "game_modifier_activations", new string[3] { "round_id", "id", "modifier_id" }, null, ReferentialAction.NoAction, ReferentialAction.Restrict);
        });
        migrationBuilder.CreateTable("game_round_participants", delegate (ColumnsBuilder table)
        {
            OperationBuilder<AddColumnOperation> id = table.Column<Guid>("uuid");
            OperationBuilder<AddColumnOperation> round_id = table.Column<Guid>("uuid");
            OperationBuilder<AddColumnOperation> user_id = table.Column<Guid>("uuid");
            int? maxLength = 128;
            return new
            {
                id = id,
                round_id = round_id,
                user_id = user_id,
                display_name_snapshot = table.Column<string>("character varying(128)", null, maxLength),
                created_at_utc = table.Column<DateTime>("timestamp with time zone")
            };
        }, null, table =>
        {
            table.PrimaryKey("pk_game_round_participants", x => x.id);
            table.CheckConstraint("ck_game_round_participants_display_name_not_blank", "length(trim(display_name_snapshot)) > 0");
            table.ForeignKey("fk_game_round_participants_users_user_id", x => x.user_id, "users", "id", null, ReferentialAction.NoAction, ReferentialAction.Restrict);
        });
        migrationBuilder.CreateTable("game_round_transition_audits", delegate (ColumnsBuilder table)
        {
            OperationBuilder<AddColumnOperation> round_id = table.Column<Guid>("uuid");
            OperationBuilder<AddColumnOperation> sequence = table.Column<int>("integer");
            int? maxLength = 32;
            OperationBuilder<AddColumnOperation> from_status = table.Column<string>("character varying(32)", null, maxLength, rowVersion: false, null, nullable: true);
            maxLength = 32;
            OperationBuilder<AddColumnOperation> to_status = table.Column<string>("character varying(32)", null, maxLength);
            maxLength = 64;
            OperationBuilder<AddColumnOperation> action_code = table.Column<string>("character varying(64)", null, maxLength);
            OperationBuilder<AddColumnOperation> initiated_by_user_id = table.Column<Guid>("uuid");
            OperationBuilder<AddColumnOperation> occurred_at_utc = table.Column<DateTime>("timestamp with time zone");
            maxLength = 2000;
            return new
            {
                round_id = round_id,
                sequence = sequence,
                from_status = from_status,
                to_status = to_status,
                action_code = action_code,
                initiated_by_user_id = initiated_by_user_id,
                occurred_at_utc = occurred_at_utc,
                reason = table.Column<string>("character varying(2000)", null, maxLength, rowVersion: false, null, nullable: true),
                resulting_round_version = table.Column<int>("integer")
            };
        }, null, table =>
        {
            table.PrimaryKey("pk_game_round_transition_audits", x => new { x.round_id, x.sequence });
            table.CheckConstraint("ck_game_round_transition_audits_action_allowed", "action_code IN ('prepare','rebuild','begin_gameplay','review','resume_gameplay','finalize','technical_cancel')");
            table.CheckConstraint("ck_game_round_transition_audits_action_semantics", "(action_code = 'prepare' AND from_status = 'awaiting_modifiers' AND to_status = 'preparing') OR (action_code = 'rebuild' AND from_status = 'preparing' AND to_status = 'awaiting_modifiers') OR (action_code = 'begin_gameplay' AND from_status IN ('awaiting_modifiers','preparing') AND to_status = 'in_progress') OR (action_code = 'review' AND from_status = 'in_progress' AND to_status = 'reviewing_results') OR (action_code = 'resume_gameplay' AND from_status = 'reviewing_results' AND to_status = 'in_progress') OR (action_code = 'finalize' AND from_status = 'reviewing_results' AND to_status = 'completed') OR (action_code = 'technical_cancel' AND from_status IN ('awaiting_modifiers','preparing','in_progress','reviewing_results') AND to_status = 'cancelled')");
            table.CheckConstraint("ck_game_round_transition_audits_resulting_version_positive", "resulting_round_version > 0");
            table.CheckConstraint("ck_game_round_transition_audits_sequence_positive", "sequence > 0");
            table.CheckConstraint("ck_game_round_transition_audits_statuses_allowed", "(from_status IS NULL OR from_status IN ('awaiting_modifiers','preparing','in_progress','reviewing_results','completed','cancelled')) AND to_status IN ('awaiting_modifiers','preparing','in_progress','reviewing_results','completed','cancelled')");
            table.ForeignKey("fk_game_round_transition_audits_users_initiated_by_user_id", x => x.initiated_by_user_id, "users", "id", null, ReferentialAction.NoAction, ReferentialAction.Restrict);
        });
        migrationBuilder.CreateTable("game_rounds", delegate (ColumnsBuilder table)
        {
            OperationBuilder<AddColumnOperation> id = table.Column<Guid>("uuid");
            OperationBuilder<AddColumnOperation> game_id = table.Column<Guid>("uuid");
            OperationBuilder<AddColumnOperation> board_id = table.Column<Guid>("uuid");
            OperationBuilder<AddColumnOperation> board_cell_id = table.Column<Guid>("uuid");
            OperationBuilder<AddColumnOperation> team_id = table.Column<Guid>("uuid");
            int? maxLength = 32;
            OperationBuilder<AddColumnOperation> status = table.Column<string>("character varying(32)", null, maxLength);
            OperationBuilder<AddColumnOperation> version = table.Column<int>("integer", null, null, rowVersion: false, null, nullable: false, 1);
            OperationBuilder<AddColumnOperation> prepared_at_utc = table.Column<DateTime>("timestamp with time zone", null, null, rowVersion: false, null, nullable: true);
            OperationBuilder<AddColumnOperation> gameplay_started_at_utc = table.Column<DateTime>("timestamp with time zone", null, null, rowVersion: false, null, nullable: true);
            OperationBuilder<AddColumnOperation> reviewed_at_utc = table.Column<DateTime>("timestamp with time zone", null, null, rowVersion: false, null, nullable: true);
            OperationBuilder<AddColumnOperation> finished_at_utc = table.Column<DateTime>("timestamp with time zone", null, null, rowVersion: false, null, nullable: true);
            OperationBuilder<AddColumnOperation> base_score = table.Column<int>("integer");
            OperationBuilder<AddColumnOperation> final_score = table.Column<int>("integer", null, null, rowVersion: false, null, nullable: true);
            OperationBuilder<AddColumnOperation> empty_card_penalty_applied = table.Column<bool>("boolean", null, null, rowVersion: false, null, nullable: false, false);
            OperationBuilder<AddColumnOperation> kills_count = table.Column<int>("integer", null, null, rowVersion: false, null, nullable: false, 0);
            OperationBuilder<AddColumnOperation> bounty_count = table.Column<int>("integer", null, null, rowVersion: false, null, nullable: false, 0);
            OperationBuilder<AddColumnOperation> team_slot_index_snapshot = table.Column<int>("integer");
            OperationBuilder<AddColumnOperation> cell_row_index = table.Column<int>("integer");
            OperationBuilder<AddColumnOperation> cell_col_index = table.Column<int>("integer");
            maxLength = 200;
            OperationBuilder<AddColumnOperation> cell_title_snapshot = table.Column<string>("character varying(200)", null, maxLength, rowVersion: false, null, nullable: true);
            maxLength = 2000;
            OperationBuilder<AddColumnOperation> cell_description_snapshot = table.Column<string>("character varying(2000)", null, maxLength, rowVersion: false, null, nullable: true);
            OperationBuilder<AddColumnOperation> cell_cost_snapshot = table.Column<int>("integer");
            maxLength = 2000;
            OperationBuilder<AddColumnOperation> notes = table.Column<string>("character varying(2000)", null, maxLength, rowVersion: false, null, nullable: true);
            maxLength = 64;
            OperationBuilder<AddColumnOperation> technical_cancellation_reason_code = table.Column<string>("character varying(64)", null, maxLength, rowVersion: false, null, nullable: true);
            maxLength = 500;
            OperationBuilder<AddColumnOperation> public_cancellation_summary = table.Column<string>("character varying(500)", null, maxLength, rowVersion: false, null, nullable: true);
            maxLength = 2000;
            return new
            {
                id = id,
                game_id = game_id,
                board_id = board_id,
                board_cell_id = board_cell_id,
                team_id = team_id,
                status = status,
                version = version,
                prepared_at_utc = prepared_at_utc,
                gameplay_started_at_utc = gameplay_started_at_utc,
                reviewed_at_utc = reviewed_at_utc,
                finished_at_utc = finished_at_utc,
                base_score = base_score,
                final_score = final_score,
                empty_card_penalty_applied = empty_card_penalty_applied,
                kills_count = kills_count,
                bounty_count = bounty_count,
                team_slot_index_snapshot = team_slot_index_snapshot,
                cell_row_index = cell_row_index,
                cell_col_index = cell_col_index,
                cell_title_snapshot = cell_title_snapshot,
                cell_description_snapshot = cell_description_snapshot,
                cell_cost_snapshot = cell_cost_snapshot,
                notes = notes,
                technical_cancellation_reason_code = technical_cancellation_reason_code,
                public_cancellation_summary = public_cancellation_summary,
                internal_cancellation_detail = table.Column<string>("character varying(2000)", null, maxLength, rowVersion: false, null, nullable: true),
                resolved_by_user_id = table.Column<Guid>("uuid", null, null, rowVersion: false, null, nullable: true),
                created_at_utc = table.Column<DateTime>("timestamp with time zone"),
                updated_at_utc = table.Column<DateTime>("timestamp with time zone")
            };
        }, null, table =>
        {
            table.PrimaryKey("pk_game_rounds", x => x.id);
            table.UniqueConstraint("ak_game_rounds_game_id_id", x => new { x.game_id, x.id });
            table.CheckConstraint("ck_game_rounds_base_score_non_negative", "base_score >= 0");
            table.CheckConstraint("ck_game_rounds_bounty_count_non_negative", "bounty_count >= 0");
            table.CheckConstraint("ck_game_rounds_cell_cost_non_negative", "cell_cost_snapshot >= 0");
            table.CheckConstraint("ck_game_rounds_empty_card_penalty_semantics", "(empty_card_penalty_applied = false) OR (status = 'completed' AND final_score IS NOT NULL)");
            table.CheckConstraint("ck_game_rounds_finished_at_semantics", "((status IN ('awaiting_modifiers','preparing','in_progress','reviewing_results')) AND finished_at_utc IS NULL) OR ((status IN ('completed','cancelled')) AND finished_at_utc IS NOT NULL)");
            table.CheckConstraint("ck_game_rounds_kills_count_non_negative", "kills_count >= 0");
            table.CheckConstraint("ck_game_rounds_lifecycle_timestamps", "(status = 'awaiting_modifiers' AND prepared_at_utc IS NULL AND gameplay_started_at_utc IS NULL AND reviewed_at_utc IS NULL) OR (status = 'preparing' AND prepared_at_utc IS NOT NULL AND gameplay_started_at_utc IS NULL AND reviewed_at_utc IS NULL) OR (status = 'in_progress' AND prepared_at_utc IS NOT NULL AND gameplay_started_at_utc IS NOT NULL AND reviewed_at_utc IS NULL) OR (status = 'reviewing_results' AND prepared_at_utc IS NOT NULL AND gameplay_started_at_utc IS NOT NULL AND reviewed_at_utc IS NOT NULL) OR (status = 'completed' AND prepared_at_utc IS NOT NULL AND gameplay_started_at_utc IS NOT NULL AND reviewed_at_utc IS NOT NULL) OR (status = 'cancelled')");
            table.CheckConstraint("ck_game_rounds_resolution_semantics", "((status IN ('awaiting_modifiers','preparing','in_progress','reviewing_results')) AND final_score IS NULL AND resolved_by_user_id IS NULL) OR ((status = 'completed') AND final_score IS NOT NULL AND resolved_by_user_id IS NOT NULL) OR ((status = 'cancelled') AND final_score = 0 AND resolved_by_user_id IS NOT NULL)");
            table.CheckConstraint("ck_game_rounds_row_col_non_negative", "cell_row_index >= 0 AND cell_col_index >= 0");
            table.CheckConstraint("ck_game_rounds_status_allowed", "status IN ('awaiting_modifiers','preparing','in_progress','reviewing_results','completed','cancelled')");
            table.CheckConstraint("ck_game_rounds_team_slot_positive", "team_slot_index_snapshot > 0");
            table.CheckConstraint("ck_game_rounds_technical_cancellation_reason_allowed", "technical_cancellation_reason_code IS NULL OR technical_cancellation_reason_code IN ('external_game_failure','stream_or_infrastructure_failure','application_error','operator_error','other')");
            table.CheckConstraint("ck_game_rounds_technical_cancellation_semantics", "(status = 'cancelled' AND technical_cancellation_reason_code IS NOT NULL AND internal_cancellation_detail IS NOT NULL AND (technical_cancellation_reason_code <> 'other' OR public_cancellation_summary IS NOT NULL)) OR (status <> 'cancelled' AND technical_cancellation_reason_code IS NULL AND public_cancellation_summary IS NULL AND internal_cancellation_detail IS NULL)");
            table.CheckConstraint("ck_game_rounds_timestamp_order", "(prepared_at_utc IS NULL OR prepared_at_utc >= created_at_utc) AND (gameplay_started_at_utc IS NULL OR (prepared_at_utc IS NOT NULL AND gameplay_started_at_utc >= prepared_at_utc)) AND (reviewed_at_utc IS NULL OR (gameplay_started_at_utc IS NOT NULL AND reviewed_at_utc >= gameplay_started_at_utc)) AND (finished_at_utc IS NULL OR finished_at_utc >= created_at_utc) AND (finished_at_utc IS NULL OR prepared_at_utc IS NULL OR finished_at_utc >= prepared_at_utc) AND (finished_at_utc IS NULL OR gameplay_started_at_utc IS NULL OR finished_at_utc >= gameplay_started_at_utc) AND (finished_at_utc IS NULL OR reviewed_at_utc IS NULL OR finished_at_utc >= reviewed_at_utc) AND updated_at_utc >= created_at_utc");
            table.CheckConstraint("ck_game_rounds_version_positive", "version > 0");
            table.ForeignKey("fk_game_rounds_board_cells_same_board", x => new { x.board_id, x.board_cell_id }, "game_board_cells", new string[2] { "board_id", "id" }, null, ReferentialAction.NoAction, ReferentialAction.Restrict);
            table.ForeignKey("fk_game_rounds_game_boards_same_game", x => new { x.game_id, x.board_id }, "game_boards", new string[2] { "game_id", "id" }, null, ReferentialAction.NoAction, ReferentialAction.Restrict);
            table.ForeignKey("fk_game_rounds_users_resolved_by_user_id", x => x.resolved_by_user_id, "users", "id", null, ReferentialAction.NoAction, ReferentialAction.Restrict);
        });
        migrationBuilder.CreateTable("game_team_final_results", delegate (ColumnsBuilder table)
        {
            OperationBuilder<AddColumnOperation> game_id = table.Column<Guid>("uuid");
            OperationBuilder<AddColumnOperation> team_id = table.Column<Guid>("uuid");
            int? maxLength = 128;
            return new
            {
                game_id = game_id,
                team_id = team_id,
                team_name_snapshot = table.Column<string>("character varying(128)", null, maxLength, rowVersion: false, null, nullable: true),
                team_slot_index_snapshot = table.Column<int>("integer"),
                participant_names_snapshot = table.Column<string[]>("text[]"),
                rounds_played = table.Column<int>("integer"),
                best_score = table.Column<int>("integer", null, null, rowVersion: false, null, nullable: true),
                penalty_total = table.Column<int>("integer"),
                final_score = table.Column<int>("integer", null, null, rowVersion: false, null, nullable: true),
                total_score = table.Column<int>("integer"),
                total_bonus_delta = table.Column<int>("integer"),
                total_kills = table.Column<int>("integer"),
                total_bounties = table.Column<int>("integer"),
                placement = table.Column<int>("integer", null, null, rowVersion: false, null, nullable: true),
                last_finished_at_utc = table.Column<DateTime>("timestamp with time zone", null, null, rowVersion: false, null, nullable: true)
            };
        }, null, table =>
        {
            table.PrimaryKey("pk_game_team_final_results", x => new { x.game_id, x.team_id });
            table.CheckConstraint("ck_game_team_final_results_rounds_non_negative", "rounds_played >= 0 AND penalty_total >= 0 AND total_kills >= 0 AND total_bounties >= 0");
            table.CheckConstraint("ck_game_team_final_results_team_slot_positive", "team_slot_index_snapshot > 0");
            table.CheckConstraint("ck_game_team_final_results_unplayed_semantics", "(rounds_played = 0 AND best_score IS NULL AND final_score IS NULL AND placement IS NULL AND last_finished_at_utc IS NULL) OR (rounds_played > 0 AND best_score IS NOT NULL AND final_score IS NOT NULL AND placement IS NOT NULL AND placement > 0 AND last_finished_at_utc IS NOT NULL)");
            table.ForeignKey("fk_game_team_final_results_game_finalizations_game_id", x => x.game_id, "game_finalizations", "game_id", null, ReferentialAction.NoAction, ReferentialAction.Cascade);
        });
        migrationBuilder.CreateTable("game_team_invitations", delegate (ColumnsBuilder table)
        {
            OperationBuilder<AddColumnOperation> id = table.Column<Guid>("uuid");
            OperationBuilder<AddColumnOperation> game_id = table.Column<Guid>("uuid");
            OperationBuilder<AddColumnOperation> slot_id = table.Column<Guid>("uuid");
            OperationBuilder<AddColumnOperation> team_id = table.Column<Guid>("uuid", null, null, rowVersion: false, null, nullable: true);
            OperationBuilder<AddColumnOperation> invited_user_id = table.Column<Guid>("uuid");
            OperationBuilder<AddColumnOperation> invited_by_user_id = table.Column<Guid>("uuid");
            int? maxLength = 16;
            OperationBuilder<AddColumnOperation> invited_by_kind = table.Column<string>("character varying(16)", null, maxLength);
            maxLength = 16;
            return new
            {
                id = id,
                game_id = game_id,
                slot_id = slot_id,
                team_id = team_id,
                invited_user_id = invited_user_id,
                invited_by_user_id = invited_by_user_id,
                invited_by_kind = invited_by_kind,
                status = table.Column<string>("character varying(16)", null, maxLength),
                created_at_utc = table.Column<DateTime>("timestamp with time zone"),
                responded_at_utc = table.Column<DateTime>("timestamp with time zone", null, null, rowVersion: false, null, nullable: true)
            };
        }, null, table =>
        {
            table.PrimaryKey("pk_game_team_invitations", x => x.id);
            table.CheckConstraint("ck_game_team_invitations_invited_by_kind", "invited_by_kind IN ('admin','member')");
            table.CheckConstraint("ck_game_team_invitations_response_timestamp_semantics", "((status = 'pending') AND responded_at_utc IS NULL) OR ((status <> 'pending') AND responded_at_utc IS NOT NULL AND responded_at_utc >= created_at_utc)");
            table.CheckConstraint("ck_game_team_invitations_source_team_semantics", "invited_by_kind = 'admin' OR team_id IS NOT NULL");
            table.CheckConstraint("ck_game_team_invitations_status", "status IN ('pending','accepted','declined','cancelled','expired')");
            table.ForeignKey("fk_game_team_invitations_users_invited_by_user_id", x => x.invited_by_user_id, "users", "id", null, ReferentialAction.NoAction, ReferentialAction.Restrict);
            table.ForeignKey("fk_game_team_invitations_users_invited_user_id", x => x.invited_user_id, "users", "id", null, ReferentialAction.NoAction, ReferentialAction.Restrict);
        });
        migrationBuilder.CreateTable("game_team_members", (ColumnsBuilder table) => new
        {
            id = table.Column<Guid>("uuid"),
            game_id = table.Column<Guid>("uuid"),
            team_id = table.Column<Guid>("uuid"),
            user_id = table.Column<Guid>("uuid"),
            joined_at_utc = table.Column<DateTime>("timestamp with time zone"),
            left_at_utc = table.Column<DateTime>("timestamp with time zone", null, null, rowVersion: false, null, nullable: true)
        }, null, table =>
        {
            table.PrimaryKey("pk_game_team_members", x => x.id);
            table.CheckConstraint("ck_game_team_members_left_after_join", "left_at_utc IS NULL OR left_at_utc >= joined_at_utc");
            table.ForeignKey("fk_game_team_members_users_user_id", x => x.user_id, "users", "id", null, ReferentialAction.NoAction, ReferentialAction.Restrict);
        });
        migrationBuilder.CreateTable("game_team_slots", delegate (ColumnsBuilder table)
        {
            OperationBuilder<AddColumnOperation> id = table.Column<Guid>("uuid");
            OperationBuilder<AddColumnOperation> game_id = table.Column<Guid>("uuid");
            OperationBuilder<AddColumnOperation> slot_index = table.Column<int>("integer");
            int? maxLength = 16;
            OperationBuilder<AddColumnOperation> slot_type = table.Column<string>("character varying(16)", null, maxLength);
            maxLength = 200;
            return new
            {
                id = id,
                game_id = game_id,
                slot_index = slot_index,
                slot_type = slot_type,
                reserved_label = table.Column<string>("character varying(200)", null, maxLength, rowVersion: false, null, nullable: true),
                created_at_utc = table.Column<DateTime>("timestamp with time zone")
            };
        }, null, table =>
        {
            table.PrimaryKey("pk_game_team_slots", x => x.id);
            table.UniqueConstraint("ak_game_team_slots_game_id_id", x => new { x.game_id, x.id });
            table.CheckConstraint("ck_game_team_slots_reserved_label_semantics", "(slot_type = 'public' AND reserved_label IS NULL) OR (slot_type = 'reserved' AND reserved_label IS NOT NULL AND length(trim(reserved_label)) > 0)");
            table.CheckConstraint("ck_game_team_slots_slot_index_positive", "slot_index > 0");
            table.CheckConstraint("ck_game_team_slots_slot_type", "slot_type IN ('public','reserved')");
        });
        migrationBuilder.CreateTable("game_teams", delegate (ColumnsBuilder table)
        {
            OperationBuilder<AddColumnOperation> id = table.Column<Guid>("uuid");
            OperationBuilder<AddColumnOperation> game_id = table.Column<Guid>("uuid");
            OperationBuilder<AddColumnOperation> slot_id = table.Column<Guid>("uuid");
            int? maxLength = 48;
            OperationBuilder<AddColumnOperation> name = table.Column<string>("character varying(48)", null, maxLength, rowVersion: false, null, nullable: true);
            OperationBuilder<AddColumnOperation> recruitment_open = table.Column<bool>("boolean");
            OperationBuilder<AddColumnOperation> is_played = table.Column<bool>("boolean", null, null, rowVersion: false, null, nullable: false, false);
            OperationBuilder<AddColumnOperation> played_at_utc = table.Column<DateTime>("timestamp with time zone", null, null, rowVersion: false, null, nullable: true);
            maxLength = 32;
            return new
            {
                id = id,
                game_id = game_id,
                slot_id = slot_id,
                name = name,
                recruitment_open = recruitment_open,
                is_played = is_played,
                played_at_utc = played_at_utc,
                status = table.Column<string>("character varying(32)", null, maxLength),
                created_by_user_id = table.Column<Guid>("uuid", null, null, rowVersion: false, null, nullable: true),
                created_at_utc = table.Column<DateTime>("timestamp with time zone"),
                updated_at_utc = table.Column<DateTime>("timestamp with time zone"),
                confirmed_at_utc = table.Column<DateTime>("timestamp with time zone", null, null, rowVersion: false, null, nullable: true),
                confirmed_by_user_id = table.Column<Guid>("uuid", null, null, rowVersion: false, null, nullable: true),
                rejected_at_utc = table.Column<DateTime>("timestamp with time zone", null, null, rowVersion: false, null, nullable: true),
                rejected_by_user_id = table.Column<Guid>("uuid", null, null, rowVersion: false, null, nullable: true),
                disbanded_at_utc = table.Column<DateTime>("timestamp with time zone", null, null, rowVersion: false, null, nullable: true),
                disbanded_by_user_id = table.Column<Guid>("uuid", null, null, rowVersion: false, null, nullable: true),
                disband_requested_at_utc = table.Column<DateTime>("timestamp with time zone", null, null, rowVersion: false, null, nullable: true),
                disband_requested_by_user_id = table.Column<Guid>("uuid", null, null, rowVersion: false, null, nullable: true)
            };
        }, null, table =>
        {
            table.PrimaryKey("pk_game_teams", x => x.id);
            table.UniqueConstraint("ak_game_teams_game_id_id", x => new { x.game_id, x.id });
            table.CheckConstraint("ck_game_teams_content_and_timestamps", "(name IS NULL OR length(trim(name)) > 0) AND updated_at_utc >= created_at_utc AND (played_at_utc IS NULL OR played_at_utc >= created_at_utc) AND (confirmed_at_utc IS NULL OR confirmed_at_utc >= created_at_utc) AND (rejected_at_utc IS NULL OR rejected_at_utc >= created_at_utc) AND (disbanded_at_utc IS NULL OR disbanded_at_utc >= created_at_utc) AND (disband_requested_at_utc IS NULL OR disband_requested_at_utc >= created_at_utc)");
            table.CheckConstraint("ck_game_teams_disband_request_user_pair", "(disband_requested_at_utc IS NULL AND disband_requested_by_user_id IS NULL) OR (disband_requested_at_utc IS NOT NULL AND disband_requested_by_user_id IS NOT NULL)");
            table.CheckConstraint("ck_game_teams_played_timestamp_semantics", "(is_played = true AND played_at_utc IS NOT NULL) OR (is_played = false AND played_at_utc IS NULL)");
            table.CheckConstraint("ck_game_teams_status_allowed", "status IN ('forming','confirmed','rejected','disbanded')");
            table.CheckConstraint("ck_game_teams_status_timestamp_semantics", "((status = 'forming') AND confirmed_at_utc IS NULL AND rejected_at_utc IS NULL AND disbanded_at_utc IS NULL AND disband_requested_at_utc IS NULL) OR ((status = 'confirmed') AND confirmed_at_utc IS NOT NULL AND confirmed_by_user_id IS NOT NULL AND rejected_at_utc IS NULL AND disbanded_at_utc IS NULL) OR ((status = 'rejected') AND rejected_at_utc IS NOT NULL AND rejected_by_user_id IS NOT NULL AND disbanded_at_utc IS NULL AND disband_requested_at_utc IS NULL) OR ((status = 'disbanded') AND disbanded_at_utc IS NOT NULL AND disbanded_by_user_id IS NOT NULL AND disband_requested_at_utc IS NULL)");
            table.CheckConstraint("ck_game_teams_terminal_recruitment_closed", "status NOT IN ('rejected','disbanded') OR recruitment_open = FALSE");
            table.ForeignKey("fk_game_teams_game_team_slots_game_id_slot_id", x => new { x.game_id, x.slot_id }, "game_team_slots", new string[2] { "game_id", "id" }, null, ReferentialAction.NoAction, ReferentialAction.Restrict);
            table.ForeignKey("fk_game_teams_users_confirmed_by_user_id", x => x.confirmed_by_user_id, "users", "id", null, ReferentialAction.NoAction, ReferentialAction.Restrict);
            table.ForeignKey("fk_game_teams_users_created_by_user_id", x => x.created_by_user_id, "users", "id", null, ReferentialAction.NoAction, ReferentialAction.SetNull);
            table.ForeignKey("fk_game_teams_users_disband_requested_by_user_id", x => x.disband_requested_by_user_id, "users", "id", null, ReferentialAction.NoAction, ReferentialAction.Restrict);
            table.ForeignKey("fk_game_teams_users_disbanded_by_user_id", x => x.disbanded_by_user_id, "users", "id", null, ReferentialAction.NoAction, ReferentialAction.Restrict);
            table.ForeignKey("fk_game_teams_users_rejected_by_user_id", x => x.rejected_by_user_id, "users", "id", null, ReferentialAction.NoAction, ReferentialAction.Restrict);
        });
        migrationBuilder.CreateTable("games", delegate (ColumnsBuilder table)
        {
            OperationBuilder<AddColumnOperation> id = table.Column<Guid>("uuid");
            int? maxLength = 200;
            OperationBuilder<AddColumnOperation> title = table.Column<string>("character varying(200)", null, maxLength);
            maxLength = 2000;
            OperationBuilder<AddColumnOperation> description = table.Column<string>("character varying(2000)", null, maxLength, rowVersion: false, null, nullable: true);
            maxLength = 32;
            return new
            {
                id = id,
                title = title,
                description = description,
                status = table.Column<string>("character varying(32)", null, maxLength),
                created_at_utc = table.Column<DateTime>("timestamp with time zone"),
                ready_at_utc = table.Column<DateTime>("timestamp with time zone", null, null, rowVersion: false, null, nullable: true),
                started_at_utc = table.Column<DateTime>("timestamp with time zone", null, null, rowVersion: false, null, nullable: true),
                finished_at_utc = table.Column<DateTime>("timestamp with time zone", null, null, rowVersion: false, null, nullable: true),
                is_deleted = table.Column<bool>("boolean", null, null, rowVersion: false, null, nullable: false, false),
                deleted_at_utc = table.Column<DateTime>("timestamp with time zone", null, null, rowVersion: false, null, nullable: true),
                min_players_per_team = table.Column<short>("smallint", null, null, rowVersion: false, null, nullable: false, (short)1),
                max_players_per_team = table.Column<short>("smallint", null, null, rowVersion: false, null, nullable: false, (short)2),
                quiz_answer_duration_seconds = table.Column<int>("integer", null, null, rowVersion: false, null, nullable: false, 60),
                active_team_id = table.Column<Guid>("uuid", null, null, rowVersion: false, null, nullable: true)
            };
        }, null, table =>
        {
            table.PrimaryKey("pk_games", x => x.id);
            table.CheckConstraint("ck_games_active_team_requires_active_game", "(active_team_id IS NULL) OR (status = 'active' AND is_deleted = FALSE)");
            table.CheckConstraint("ck_games_finished_at_semantics", "((status IN ('draft','ready','active')) AND finished_at_utc IS NULL) OR ((status = 'finished') AND finished_at_utc IS NOT NULL)");
            table.CheckConstraint("ck_games_lifecycle_timestamps", "((status = 'draft') AND ready_at_utc IS NULL AND started_at_utc IS NULL AND finished_at_utc IS NULL) OR ((status = 'ready') AND ready_at_utc IS NOT NULL AND started_at_utc IS NULL AND finished_at_utc IS NULL) OR ((status = 'active') AND ready_at_utc IS NOT NULL AND started_at_utc IS NOT NULL AND finished_at_utc IS NULL) OR ((status = 'finished') AND ready_at_utc IS NOT NULL AND started_at_utc IS NOT NULL AND finished_at_utc IS NOT NULL)");
            table.CheckConstraint("ck_games_quiz_answer_duration", "quiz_answer_duration_seconds BETWEEN 5 AND 3600");
            table.CheckConstraint("ck_games_soft_delete_semantics", "(is_deleted = FALSE AND deleted_at_utc IS NULL) OR (is_deleted = TRUE AND deleted_at_utc IS NOT NULL)");
            table.CheckConstraint("ck_games_status_allowed", "status IN ('draft','ready','active','finished')");
            table.CheckConstraint("ck_games_team_size_limits", "min_players_per_team > 0 AND max_players_per_team >= min_players_per_team");
            table.CheckConstraint("ck_games_timestamp_order", "(ready_at_utc IS NULL OR ready_at_utc >= created_at_utc) AND (started_at_utc IS NULL OR started_at_utc >= ready_at_utc) AND (finished_at_utc IS NULL OR finished_at_utc >= started_at_utc) AND (deleted_at_utc IS NULL OR deleted_at_utc >= created_at_utc)");
            table.CheckConstraint("ck_games_title_not_blank", "length(trim(title)) > 0");
            table.ForeignKey("fk_games_active_team_same_game", x => new { x.id, x.active_team_id }, "game_teams", new string[2] { "game_id", "id" }, null, ReferentialAction.NoAction, ReferentialAction.Restrict);
        });
        migrationBuilder.CreateTable("game_user_notifications", delegate (ColumnsBuilder table)
        {
            OperationBuilder<AddColumnOperation> id = table.Column<Guid>("uuid");
            OperationBuilder<AddColumnOperation> user_id = table.Column<Guid>("uuid");
            OperationBuilder<AddColumnOperation> game_id = table.Column<Guid>("uuid");
            int? maxLength = 64;
            OperationBuilder<AddColumnOperation> type = table.Column<string>("character varying(64)", null, maxLength);
            OperationBuilder<AddColumnOperation> schema_version = table.Column<int>("integer");
            OperationBuilder<AddColumnOperation> payload_json = table.Column<string>("jsonb");
            maxLength = 160;
            return new
            {
                id = id,
                user_id = user_id,
                game_id = game_id,
                type = type,
                schema_version = schema_version,
                payload_json = payload_json,
                deduplication_key = table.Column<string>("character varying(160)", null, maxLength),
                created_at_utc = table.Column<DateTime>("timestamp with time zone"),
                read_at_utc = table.Column<DateTime>("timestamp with time zone", null, null, rowVersion: false, null, nullable: true)
            };
        }, null, table =>
        {
            table.PrimaryKey("pk_game_user_notifications", x => x.id);
            table.CheckConstraint("ck_game_user_notifications_identity_not_blank", "length(trim(type)) > 0 AND length(trim(deduplication_key)) > 0");
            table.CheckConstraint("ck_game_user_notifications_modifier_cancelled_v1_payload", "type <> 'modifier_cancelled' OR (schema_version = 1 AND jsonb_typeof(payload_json -> 'modifierActivationId') = 'string' AND length(trim(payload_json ->> 'modifierActivationId')) > 0 AND jsonb_typeof(payload_json -> 'modifierName') = 'string' AND length(trim(payload_json ->> 'modifierName')) > 0 AND jsonb_typeof(payload_json -> 'actorDisplayName') = 'string' AND length(trim(payload_json ->> 'actorDisplayName')) > 0 AND jsonb_typeof(payload_json -> 'quizPointsDelta') = 'number' AND (payload_json ->> 'quizPointsDelta')::integer >= 0)");
            table.CheckConstraint("ck_game_user_notifications_payload_envelope", "schema_version > 0 AND jsonb_typeof(payload_json) = 'object'");
            table.CheckConstraint("ck_game_user_notifications_read_after_create", "read_at_utc IS NULL OR read_at_utc >= created_at_utc");
            table.ForeignKey("fk_game_user_notifications_games_game_id", x => x.game_id, "games", "id", null, ReferentialAction.NoAction, ReferentialAction.Restrict);
            table.ForeignKey("fk_game_user_notifications_users_user_id", x => x.user_id, "users", "id", null, ReferentialAction.NoAction, ReferentialAction.Cascade);
        });
        migrationBuilder.CreateTable("modifier_definition_version_conflicts", delegate (ColumnsBuilder table)
        {
            OperationBuilder<AddColumnOperation> modifier_version_id = table.Column<Guid>("uuid");
            OperationBuilder<AddColumnOperation> conflicting_modifier_id = table.Column<Guid>("uuid");
            int? maxLength = 128;
            return new
            {
                modifier_version_id = modifier_version_id,
                conflicting_modifier_id = conflicting_modifier_id,
                conflicting_modifier_name_snapshot = table.Column<string>("character varying(128)", null, maxLength)
            };
        }, null, table =>
        {
            table.PrimaryKey("pk_modifier_definition_version_conflicts", x => new { x.modifier_version_id, x.conflicting_modifier_id });
            table.CheckConstraint("ck_modifier_definition_version_conflicts_name_not_blank", "length(trim(conflicting_modifier_name_snapshot)) > 0");
        });
        migrationBuilder.CreateTable("modifier_definition_versions", delegate (ColumnsBuilder table)
        {
            OperationBuilder<AddColumnOperation> id = table.Column<Guid>("uuid");
            OperationBuilder<AddColumnOperation> modifier_id = table.Column<Guid>("uuid");
            OperationBuilder<AddColumnOperation> revision = table.Column<int>("integer");
            int? maxLength = 128;
            OperationBuilder<AddColumnOperation> name = table.Column<string>("character varying(128)", null, maxLength);
            maxLength = 2000;
            OperationBuilder<AddColumnOperation> description = table.Column<string>("character varying(2000)", null, maxLength);
            maxLength = 32;
            OperationBuilder<AddColumnOperation> category = table.Column<string>("character varying(32)", null, maxLength);
            maxLength = 16;
            OperationBuilder<AddColumnOperation> icon_emoji = table.Column<string>("character varying(16)", null, maxLength, rowVersion: false, null, nullable: true);
            maxLength = 128;
            OperationBuilder<AddColumnOperation> activation_command = table.Column<string>("character varying(128)", null, maxLength, rowVersion: false, null, nullable: true);
            OperationBuilder<AddColumnOperation> activation_cost = table.Column<int>("integer");
            OperationBuilder<AddColumnOperation> max_activations_per_round = table.Column<int>("integer", null, null, rowVersion: false, null, nullable: true);
            OperationBuilder<AddColumnOperation> normalized_tags = table.Column<string[]>("text[]");
            OperationBuilder<AddColumnOperation> behavior_v2_json = table.Column<string>("jsonb");
            OperationBuilder<AddColumnOperation> created_at_utc = table.Column<DateTime>("timestamp with time zone");
            OperationBuilder<AddColumnOperation> created_by_user_id = table.Column<Guid>("uuid", null, null, rowVersion: false, null, nullable: true);
            maxLength = 128;
            OperationBuilder<AddColumnOperation> created_by_display_name_snapshot = table.Column<string>("character varying(128)", null, maxLength);
            maxLength = 500;
            OperationBuilder<AddColumnOperation> change_note = table.Column<string>("character varying(500)", null, maxLength, rowVersion: false, null, nullable: true);
            maxLength = 32;
            return new
            {
                id = id,
                modifier_id = modifier_id,
                revision = revision,
                name = name,
                description = description,
                category = category,
                icon_emoji = icon_emoji,
                activation_command = activation_command,
                activation_cost = activation_cost,
                max_activations_per_round = max_activations_per_round,
                normalized_tags = normalized_tags,
                behavior_v2_json = behavior_v2_json,
                created_at_utc = created_at_utc,
                created_by_user_id = created_by_user_id,
                created_by_display_name_snapshot = created_by_display_name_snapshot,
                change_note = change_note,
                change_type = table.Column<string>("character varying(32)", null, maxLength),
                changed_fields = table.Column<string[]>("text[]"),
                cascade_source_modifier_id = table.Column<Guid>("uuid", null, null, rowVersion: false, null, nullable: true)
            };
        }, null, table =>
        {
            table.PrimaryKey("pk_modifier_definition_versions", x => x.id);
            table.UniqueConstraint("ak_modifier_definition_versions_modifier_id_id", x => new { x.modifier_id, x.id });
            table.CheckConstraint("ck_modifier_definition_versions_behavior_v2_schema", "jsonb_typeof(behavior_v2_json) = 'object' AND behavior_v2_json ->> 'schemaVersion' = '2'");
            table.CheckConstraint("ck_modifier_definition_versions_category_allowed", "category IN ('preparation','round','result')");
            table.CheckConstraint("ck_modifier_definition_versions_change_note", "change_note IS NULL OR length(btrim(change_note)) BETWEEN 1 AND 500");
            table.CheckConstraint("ck_modifier_definition_versions_change_type", "change_type IN ('created','edited','compatibility_cascade','migration_baseline')");
            table.CheckConstraint("ck_modifier_definition_versions_content_not_blank", "length(btrim(name)) > 0 AND length(btrim(description)) > 0 AND length(btrim(created_by_display_name_snapshot)) > 0");
            table.CheckConstraint("ck_modifier_definition_versions_cost_non_negative", "activation_cost >= 0");
            table.CheckConstraint("ck_modifier_definition_versions_limit_positive_or_null", "max_activations_per_round IS NULL OR max_activations_per_round > 0");
            table.CheckConstraint("ck_modifier_definition_versions_revision_positive", "revision >= 1");
            table.ForeignKey("fk_modifier_definition_versions_users_created_by_user_id", x => x.created_by_user_id, "users", "id", null, ReferentialAction.NoAction, ReferentialAction.Restrict);
        });
        migrationBuilder.CreateTable("modifier_definitions", (ColumnsBuilder table) => new
        {
            id = table.Column<Guid>("uuid"),
            current_version_id = table.Column<Guid>("uuid", null, null, rowVersion: false, null, nullable: true),
            is_archived = table.Column<bool>("boolean", null, null, rowVersion: false, null, nullable: false, false),
            created_by_user_id = table.Column<Guid>("uuid", null, null, rowVersion: false, null, nullable: true),
            archived_at_utc = table.Column<DateTime>("timestamp with time zone", null, null, rowVersion: false, null, nullable: true),
            archived_by_user_id = table.Column<Guid>("uuid", null, null, rowVersion: false, null, nullable: true),
            created_at_utc = table.Column<DateTime>("timestamp with time zone")
        }, null, table =>
        {
            table.PrimaryKey("pk_modifier_definitions", x => x.id);
            table.CheckConstraint("ck_modifier_definitions_archive_semantics", "(is_archived = FALSE AND archived_at_utc IS NULL AND archived_by_user_id IS NULL) OR (is_archived = TRUE AND archived_at_utc IS NOT NULL AND archived_by_user_id IS NOT NULL AND archived_at_utc >= created_at_utc)");
            table.ForeignKey("fk_modifier_definitions_current_version", x => new { x.id, x.current_version_id }, "modifier_definition_versions", new string[2] { "modifier_id", "id" }, null, ReferentialAction.NoAction, ReferentialAction.Restrict);
            table.ForeignKey("fk_modifier_definitions_users_archived_by_user_id", x => x.archived_by_user_id, "users", "id", null, ReferentialAction.NoAction, ReferentialAction.Restrict);
            table.ForeignKey("fk_modifier_definitions_users_created_by_user_id", x => x.created_by_user_id, "users", "id", null, ReferentialAction.NoAction, ReferentialAction.Restrict);
        });
        migrationBuilder.InsertData("roles", new string[6] { "id", "code", "created_at_utc", "description", "name", "updated_at_utc" }, new object[3, 6]
        {
            {
                (short)1,
                "viewer",
                new DateTime(2026, 3, 23, 0, 0, 0, 0, DateTimeKind.Utc),
                "Viewer role with basic registration capabilities.",
                "Viewer",
                new DateTime(2026, 3, 23, 0, 0, 0, 0, DateTimeKind.Utc)
            },
            {
                (short)2,
                "moderator",
                new DateTime(2026, 3, 23, 0, 0, 0, 0, DateTimeKind.Utc),
                "Moderator role that helps manage game operations.",
                "Moderator",
                new DateTime(2026, 3, 23, 0, 0, 0, 0, DateTimeKind.Utc)
            },
            {
                (short)3,
                "admin",
                new DateTime(2026, 3, 23, 0, 0, 0, 0, DateTimeKind.Utc),
                "Administrator role with full management access.",
                "Administrator",
                new DateTime(2026, 3, 23, 0, 0, 0, 0, DateTimeKind.Utc)
            }
        });
        migrationBuilder.CreateIndex("ix_game_board_cell_media_cell_id_sort_order", "game_board_cell_media", new string[2] { "cell_id", "sort_order" }, null, unique: true);
        migrationBuilder.CreateIndex("ix_game_board_cell_media_media_asset_id", "game_board_cell_media", "media_asset_id");
        migrationBuilder.CreateIndex("ix_game_board_cells_board_id_row_index_col_index", "game_board_cells", new string[3] { "board_id", "row_index", "col_index" }, null, unique: true);
        migrationBuilder.CreateIndex("ix_game_board_cells_state", "game_board_cells", "state");
        migrationBuilder.CreateIndex("ix_game_boards_game_id", "game_boards", "game_id", null, unique: true);
        migrationBuilder.CreateIndex("ix_game_enabled_modifiers_emergency_disabled_by_user_id", "game_enabled_modifiers", "emergency_disabled_by_user_id");
        migrationBuilder.CreateIndex("ix_game_enabled_modifiers_modifier_id_modifier_version_id", "game_enabled_modifiers", new string[2] { "modifier_id", "modifier_version_id" });
        migrationBuilder.CreateIndex("ix_game_enabled_modifiers_modifier_version_id_game_id", "game_enabled_modifiers", new string[2] { "modifier_version_id", "game_id" });
        migrationBuilder.CreateIndex("ix_game_enabled_questions_question_id", "game_enabled_questions", "question_id");
        migrationBuilder.CreateIndex("ix_game_finalizations_finished_by_user_id", "game_finalizations", "finished_by_user_id");
        migrationBuilder.CreateIndex("ix_game_finalizations_request_id", "game_finalizations", "request_id", null, unique: true);
        migrationBuilder.CreateIndex("ix_game_modifier_activations_cancelled_by_user_id", "game_modifier_activations", "cancelled_by_user_id");
        migrationBuilder.CreateIndex("ix_game_modifier_activations_game_activated", "game_modifier_activations", new string[2] { "game_id", "activated_at_utc" });
        migrationBuilder.CreateIndex("ix_game_modifier_activations_game_archived", "game_modifier_activations", new string[2] { "game_id", "archived_at_utc" });
        migrationBuilder.CreateIndex("ix_game_modifier_activations_game_id_round_id", "game_modifier_activations", new string[2] { "game_id", "round_id" });
        migrationBuilder.CreateIndex("ix_game_modifier_activations_game_modifier", "game_modifier_activations", new string[2] { "game_id", "modifier_id" });
        migrationBuilder.CreateIndex("ix_game_modifier_activations_initiated_by_user_id", "game_modifier_activations", "initiated_by_user_id");
        migrationBuilder.CreateIndex("ix_game_modifier_activations_modifier_id_modifier_version_id", "game_modifier_activations", new string[2] { "modifier_id", "modifier_version_id" });
        migrationBuilder.CreateIndex("ix_game_modifier_activations_round_status_activated", "game_modifier_activations", new string[3] { "round_id", "status", "activated_at_utc" });
        migrationBuilder.CreateIndex("ix_game_modifier_activations_user_activated", "game_modifier_activations", new string[2] { "activated_by_user_id", "activated_at_utc" });
        migrationBuilder.CreateIndex("ix_game_modifier_activations_version_game", "game_modifier_activations", new string[2] { "modifier_version_id", "game_id" });
        migrationBuilder.CreateIndex("ix_game_quiz_correct_answers_awarded_to_user_id", "game_quiz_correct_answers", "awarded_to_user_id");
        migrationBuilder.CreateIndex("ix_game_quiz_correct_answers_captured_by_user_id", "game_quiz_correct_answers", "captured_by_user_id");
        migrationBuilder.CreateIndex("ix_quiz_answers_game_user_time", "game_quiz_correct_answers", new string[3] { "game_id", "awarded_to_user_id", "answered_at_utc" });
        migrationBuilder.CreateIndex("ix_game_quiz_correct_answers_game_id_quiz_round_id", "game_quiz_correct_answers", new string[2] { "game_id", "quiz_round_id" }, null, unique: true);
        migrationBuilder.CreateIndex("ix_game_quiz_correct_answers_quiz_round_id", "game_quiz_correct_answers", "quiz_round_id", null, unique: true);
        migrationBuilder.CreateIndex("ux_game_quiz_correct_answers_source_message", "game_quiz_correct_answers", new string[3] { "source_provider", "source_channel_id", "source_message_id" }, null, unique: true, "source_channel_id IS NOT NULL AND source_message_id IS NOT NULL");
        migrationBuilder.CreateIndex("ix_game_quiz_point_ledger_entries_correct_answer_id", "game_quiz_point_ledger_entries", "correct_answer_id", null, unique: true, "correct_answer_id IS NOT NULL");
        migrationBuilder.CreateIndex("ix_game_quiz_point_ledger_entries_created_by_user_id", "game_quiz_point_ledger_entries", "created_by_user_id");
        migrationBuilder.CreateIndex("ix_game_quiz_point_ledger_entries_game_id_correct_answer_id", "game_quiz_point_ledger_entries", new string[2] { "game_id", "correct_answer_id" });
        migrationBuilder.CreateIndex("ix_quiz_ledger_game_activation", "game_quiz_point_ledger_entries", new string[2] { "game_id", "modifier_activation_id" });
        migrationBuilder.CreateIndex("ix_quiz_ledger_game_user_sequence", "game_quiz_point_ledger_entries", new string[3] { "game_id", "user_id", "sequence_number" });
        migrationBuilder.CreateIndex("ix_game_quiz_point_ledger_entries_manual_request_id", "game_quiz_point_ledger_entries", "manual_request_id", null, unique: true, "manual_request_id IS NOT NULL");
        migrationBuilder.CreateIndex("ix_game_quiz_point_ledger_entries_sequence_number", "game_quiz_point_ledger_entries", "sequence_number", null, unique: true);
        migrationBuilder.CreateIndex("ix_game_quiz_point_ledger_entries_user_id_game_id", "game_quiz_point_ledger_entries", new string[2] { "user_id", "game_id" });
        migrationBuilder.CreateIndex("ux_quiz_point_ledger_modifier_event", "game_quiz_point_ledger_entries", new string[2] { "modifier_activation_id", "entry_type" }, null, unique: true, "modifier_activation_id IS NOT NULL");
        migrationBuilder.CreateIndex("ix_game_quiz_rounds_asked_by_user_id_asked_at_utc", "game_quiz_rounds", new string[2] { "asked_by_user_id", "asked_at_utc" });
        migrationBuilder.CreateIndex("ix_game_quiz_rounds_game_id_ask_order", "game_quiz_rounds", new string[2] { "game_id", "ask_order" }, null, unique: true);
        migrationBuilder.CreateIndex("ix_game_quiz_rounds_game_id_asked_at_utc", "game_quiz_rounds", new string[2] { "game_id", "asked_at_utc" });
        migrationBuilder.CreateIndex("ix_game_quiz_rounds_game_id_question_id", "game_quiz_rounds", new string[2] { "game_id", "question_id" }, null, unique: true);
        migrationBuilder.CreateIndex("ix_game_quiz_rounds_game_id_status", "game_quiz_rounds", new string[2] { "game_id", "status" });
        migrationBuilder.CreateIndex("ix_game_quiz_rounds_question_id", "game_quiz_rounds", "question_id");
        migrationBuilder.CreateIndex("ux_game_quiz_rounds_one_open", "game_quiz_rounds", "game_id", null, unique: true, "status = 'asked'");
        migrationBuilder.CreateIndex("ux_game_round_cell_media_round_sort_order", "game_round_cell_media", new string[2] { "round_id", "sort_order" }, null, unique: true);
        migrationBuilder.CreateIndex("ix_game_round_modifier_results_modifier_status", "game_round_modifier_results", new string[2] { "modifier_id", "outcome_status" });
        migrationBuilder.CreateIndex("ix_game_round_modifier_results_resolved_by_user_id", "game_round_modifier_results", "resolved_by_user_id");
        migrationBuilder.CreateIndex("ix_round_modifier_results_activation_fk", "game_round_modifier_results", new string[3] { "round_id", "modifier_activation_id", "modifier_id" });
        migrationBuilder.CreateIndex("ix_game_round_modifier_results_round_status", "game_round_modifier_results", new string[2] { "round_id", "outcome_status" });
        migrationBuilder.CreateIndex("ux_game_round_modifier_results_round_activation", "game_round_modifier_results", new string[2] { "round_id", "modifier_activation_id" }, null, unique: true);
        migrationBuilder.CreateIndex("ix_game_round_participants_user_created", "game_round_participants", new string[2] { "user_id", "created_at_utc" });
        migrationBuilder.CreateIndex("ux_game_round_participants_round_user", "game_round_participants", new string[2] { "round_id", "user_id" }, null, unique: true);
        migrationBuilder.CreateIndex("ix_game_round_transition_audits_initiated_by_user_id", "game_round_transition_audits", "initiated_by_user_id");
        migrationBuilder.CreateIndex("ux_round_transition_version", "game_round_transition_audits", new string[2] { "round_id", "resulting_round_version" }, null, unique: true);
        migrationBuilder.CreateIndex("ix_game_rounds_board_cell_id_created_at_utc", "game_rounds", new string[2] { "board_cell_id", "created_at_utc" });
        migrationBuilder.CreateIndex("ix_game_rounds_board_id_board_cell_id", "game_rounds", new string[2] { "board_id", "board_cell_id" });
        migrationBuilder.CreateIndex("ix_game_rounds_game_id_board_id", "game_rounds", new string[2] { "game_id", "board_id" });
        migrationBuilder.CreateIndex("ix_game_rounds_game_id_created_at_utc", "game_rounds", new string[2] { "game_id", "created_at_utc" });
        migrationBuilder.CreateIndex("ix_game_rounds_game_id_team_id_board_cell_id_created_at_utc", "game_rounds", new string[4] { "game_id", "team_id", "board_cell_id", "created_at_utc" });
        migrationBuilder.CreateIndex("ix_game_rounds_resolved_by_user_id", "game_rounds", "resolved_by_user_id");
        migrationBuilder.CreateIndex("ix_game_rounds_team_id_created_at_utc", "game_rounds", new string[2] { "team_id", "created_at_utc" });
        migrationBuilder.CreateIndex("ux_game_rounds_one_effective_cell", "game_rounds", new string[2] { "game_id", "board_cell_id" }, null, unique: true, "status <> 'cancelled'");
        migrationBuilder.CreateIndex("ux_game_rounds_single_nonterminal_game", "game_rounds", "game_id", null, unique: true, "status IN ('awaiting_modifiers','preparing','in_progress','reviewing_results')");
        migrationBuilder.CreateIndex("ix_game_team_final_results_game_id_placement", "game_team_final_results", new string[2] { "game_id", "placement" });
        migrationBuilder.CreateIndex("ix_game_team_invitations_game_slot", "game_team_invitations", new string[2] { "game_id", "slot_id" });
        migrationBuilder.CreateIndex("ix_game_team_invitations_game_team", "game_team_invitations", new string[2] { "game_id", "team_id" });
        migrationBuilder.CreateIndex("ix_game_team_invitations_game_id_status", "game_team_invitations", new string[2] { "game_id", "status" });
        migrationBuilder.CreateIndex("ix_game_team_invitations_invited_by_user_id", "game_team_invitations", "invited_by_user_id");
        migrationBuilder.CreateIndex("ix_game_team_invitations_invited_user_id_status", "game_team_invitations", new string[2] { "invited_user_id", "status" });
        migrationBuilder.CreateIndex("ux_game_team_invitations_one_pending_per_user", "game_team_invitations", new string[2] { "game_id", "invited_user_id" }, null, unique: true, "status = 'pending'");
        migrationBuilder.CreateIndex("ix_game_team_members_game_id_team_id", "game_team_members", new string[2] { "game_id", "team_id" });
        migrationBuilder.CreateIndex("ix_game_team_members_team_id_user_id", "game_team_members", new string[2] { "team_id", "user_id" });
        migrationBuilder.CreateIndex("ix_game_team_members_user_id", "game_team_members", "user_id");
        migrationBuilder.CreateIndex("ux_game_team_members_active_game_user", "game_team_members", new string[2] { "game_id", "user_id" }, null, unique: true, "left_at_utc IS NULL");
        migrationBuilder.CreateIndex("ux_game_team_members_active_team_user", "game_team_members", new string[2] { "team_id", "user_id" }, null, unique: true, "left_at_utc IS NULL");
        migrationBuilder.CreateIndex("ix_game_team_slots_game_id_slot_index", "game_team_slots", new string[2] { "game_id", "slot_index" }, null, unique: true);
        migrationBuilder.CreateIndex("ix_game_team_slots_game_id_slot_type", "game_team_slots", new string[2] { "game_id", "slot_type" });
        migrationBuilder.CreateIndex("ix_game_teams_confirmed_by_user_id", "game_teams", "confirmed_by_user_id");
        migrationBuilder.CreateIndex("ix_game_teams_created_by_user_id", "game_teams", "created_by_user_id");
        migrationBuilder.CreateIndex("ix_game_teams_disband_requested_by_user_id", "game_teams", "disband_requested_by_user_id");
        migrationBuilder.CreateIndex("ix_game_teams_disbanded_by_user_id", "game_teams", "disbanded_by_user_id");
        migrationBuilder.CreateIndex("ix_game_teams_game_slot", "game_teams", new string[2] { "game_id", "slot_id" });
        migrationBuilder.CreateIndex("ix_game_teams_game_id_status", "game_teams", new string[2] { "game_id", "status" });
        migrationBuilder.CreateIndex("ix_game_teams_rejected_by_user_id", "game_teams", "rejected_by_user_id");
        migrationBuilder.CreateIndex("ux_game_teams_active_slot", "game_teams", "slot_id", null, unique: true, "status IN ('forming','confirmed')");
        migrationBuilder.CreateIndex("ix_game_user_notifications_game_id", "game_user_notifications", "game_id");
        migrationBuilder.CreateIndex("ix_game_user_notifications_user_id_read_at_utc_created_at_utc", "game_user_notifications", new string[3] { "user_id", "read_at_utc", "created_at_utc" });
        migrationBuilder.CreateIndex("ix_game_user_notifications_user_id_type_created_at_utc", "game_user_notifications", new string[3] { "user_id", "type", "created_at_utc" });
        migrationBuilder.CreateIndex("ux_game_user_notifications_deduplication", "game_user_notifications", new string[2] { "user_id", "deduplication_key" }, null, unique: true);
        migrationBuilder.CreateIndex("ix_games_active_team_same_game", "games", new string[2] { "id", "active_team_id" });
        migrationBuilder.CreateIndex("ix_games_created_at_utc", "games", "created_at_utc");
        migrationBuilder.CreateIndex("ix_games_is_deleted_status_created_at_utc", "games", new string[3] { "is_deleted", "status", "created_at_utc" });
        migrationBuilder.CreateIndex("ux_games_single_current", "games", "is_deleted", null, unique: true, "is_deleted = FALSE AND status IN ('ready','active')");
        migrationBuilder.CreateIndex("ux_games_single_draft", "games", "is_deleted", null, unique: true, "is_deleted = FALSE AND status = 'draft'");
        migrationBuilder.CreateIndex("ix_media_assets_bucket_object_key", "media_assets", new string[2] { "bucket", "object_key" }, null, unique: true);
        migrationBuilder.CreateIndex("ix_modifier_conflicts_definition", "modifier_definition_version_conflicts", "conflicting_modifier_id");
        migrationBuilder.CreateIndex("ix_modifier_definition_versions_cascade_source_modifier_id", "modifier_definition_versions", "cascade_source_modifier_id");
        migrationBuilder.CreateIndex("ix_modifier_definition_versions_created_by_user_id", "modifier_definition_versions", "created_by_user_id");
        migrationBuilder.CreateIndex("ix_modifier_definition_versions_modifier_id_created_at_utc_id", "modifier_definition_versions", new string[3] { "modifier_id", "created_at_utc", "id" });
        migrationBuilder.CreateIndex("ix_modifier_definition_versions_modifier_id_revision", "modifier_definition_versions", new string[2] { "modifier_id", "revision" }, null, unique: true);
        migrationBuilder.CreateIndex("ix_modifier_versions_category_trgm", "modifier_definition_versions", "category").Annotation("Npgsql:IndexMethod", "gin").Annotation("Npgsql:IndexOperators", new string[1] { "gin_trgm_ops" });
        migrationBuilder.CreateIndex("ix_modifier_versions_name_trgm", "modifier_definition_versions", "name").Annotation("Npgsql:IndexMethod", "gin").Annotation("Npgsql:IndexOperators", new string[1] { "gin_trgm_ops" });
        migrationBuilder.CreateIndex("ix_modifier_definitions_archived_by_user_id", "modifier_definitions", "archived_by_user_id");
        migrationBuilder.CreateIndex("ix_modifier_definitions_created_at_utc_id", "modifier_definitions", new string[2] { "created_at_utc", "id" }, null, unique: false, null, new bool[0]);
        migrationBuilder.CreateIndex("ix_modifier_definitions_created_by_user_id", "modifier_definitions", "created_by_user_id");
        migrationBuilder.CreateIndex("ix_modifier_definitions_current_version_id", "modifier_definitions", "current_version_id", null, unique: true);
        migrationBuilder.CreateIndex("ix_modifier_definitions_id_current_version_id", "modifier_definitions", new string[2] { "id", "current_version_id" });
        migrationBuilder.CreateIndex("ix_modifier_definitions_is_archived_created_at_utc_id", "modifier_definitions", new string[3] { "is_archived", "created_at_utc", "id" }, null, unique: false, null, new bool[3] { false, true, true });
        migrationBuilder.CreateIndex("ix_question_accepted_answers_question_id_normalized_answer", "question_accepted_answers", new string[2] { "question_id", "normalized_answer" }, null, unique: true);
        migrationBuilder.CreateIndex("ix_question_accepted_answers_question_id_sort_order", "question_accepted_answers", new string[2] { "question_id", "sort_order" }, null, unique: true);
        migrationBuilder.CreateIndex("ix_question_accepted_answers_text_trgm", "question_accepted_answers", "answer_text").Annotation("Npgsql:IndexMethod", "gin").Annotation("Npgsql:IndexOperators", new string[1] { "gin_trgm_ops" });
        migrationBuilder.CreateIndex("ux_question_accepted_answers_one_primary", "question_accepted_answers", "question_id", null, unique: true, "is_primary = TRUE");
        migrationBuilder.CreateIndex("ix_question_categories_name", "question_categories", "name", null, unique: true);
        migrationBuilder.CreateIndex("ix_questions_active_pick_queue", "question_definitions", new string[3] { "is_deleted", "is_enabled", "priority" });
        migrationBuilder.CreateIndex("ix_questions_category_enabled", "question_definitions", new string[2] { "category_id", "is_enabled" });
        migrationBuilder.CreateIndex("ix_questions_priority", "question_definitions", "priority");
        migrationBuilder.CreateIndex("ix_questions_text_trgm", "question_definitions", "text").Annotation("Npgsql:IndexMethod", "gin").Annotation("Npgsql:IndexOperators", new string[1] { "gin_trgm_ops" });
        migrationBuilder.CreateIndex("ux_questions_external_code", "question_definitions", "external_code", null, unique: true);
        migrationBuilder.CreateIndex("ix_roles_code", "roles", "code", null, unique: true);
        migrationBuilder.CreateIndex("ix_user_roles_assigned_by_user_id", "user_roles", "assigned_by_user_id");
        migrationBuilder.CreateIndex("ix_user_roles_expires_at_utc", "user_roles", "expires_at_utc", null, unique: false, "expires_at_utc IS NOT NULL");
        migrationBuilder.CreateIndex("ix_user_roles_role_id", "user_roles", "role_id");
        migrationBuilder.CreateIndex("ix_users_login", "users", "login");
        migrationBuilder.CreateIndex("ix_users_twitch_user_id", "users", "twitch_user_id", null, unique: true);
        migrationBuilder.AddForeignKey("fk_game_board_cell_media_game_board_cells_cell_id", "game_board_cell_media", "cell_id", "game_board_cells", null, null, "id", ReferentialAction.NoAction, ReferentialAction.Cascade);
        migrationBuilder.AddForeignKey("fk_game_board_cells_game_boards_board_id", "game_board_cells", "board_id", "game_boards", null, null, "id", ReferentialAction.NoAction, ReferentialAction.Cascade);
        migrationBuilder.AddForeignKey("fk_game_boards_games_game_id", "game_boards", "game_id", "games", null, null, "id", ReferentialAction.NoAction, ReferentialAction.Cascade);
        migrationBuilder.AddForeignKey("fk_game_enabled_modifiers_games_game_id", "game_enabled_modifiers", "game_id", "games", null, null, "id", ReferentialAction.NoAction, ReferentialAction.Cascade);
        migrationBuilder.AddForeignKey("fk_game_enabled_modifiers_modifier_version", "game_enabled_modifiers", new string[2] { "modifier_id", "modifier_version_id" }, "modifier_definition_versions", null, null, new string[2] { "modifier_id", "id" }, ReferentialAction.NoAction, ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey("fk_game_enabled_modifiers_modifier_definitions_modifier_id", "game_enabled_modifiers", "modifier_id", "modifier_definitions", null, null, "id", ReferentialAction.NoAction, ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey("fk_game_enabled_questions_games_game_id", "game_enabled_questions", "game_id", "games", null, null, "id", ReferentialAction.NoAction, ReferentialAction.Cascade);
        migrationBuilder.AddForeignKey("fk_game_finalizations_games_game_id", "game_finalizations", "game_id", "games", null, null, "id", ReferentialAction.NoAction, ReferentialAction.Cascade);
        migrationBuilder.AddForeignKey("fk_game_modifier_activations_games_game_id", "game_modifier_activations", "game_id", "games", null, null, "id", ReferentialAction.NoAction, ReferentialAction.Cascade);
        migrationBuilder.AddForeignKey("fk_game_modifier_activations_modifier_version", "game_modifier_activations", new string[2] { "modifier_id", "modifier_version_id" }, "modifier_definition_versions", null, null, new string[2] { "modifier_id", "id" }, ReferentialAction.NoAction, ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey("fk_game_modifier_activations_modifier_definitions_modifier_id", "game_modifier_activations", "modifier_id", "modifier_definitions", null, null, "id", ReferentialAction.NoAction, ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey("fk_modifier_activations_game_rounds_same_game", "game_modifier_activations", new string[2] { "game_id", "round_id" }, "game_rounds", null, null, new string[2] { "game_id", "id" }, ReferentialAction.NoAction, ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey("fk_quiz_correct_answers_round_same_game", "game_quiz_correct_answers", new string[2] { "game_id", "quiz_round_id" }, "game_quiz_rounds", null, null, new string[2] { "game_id", "id" }, ReferentialAction.NoAction, ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey("fk_game_quiz_point_ledger_entries_games_game_id", "game_quiz_point_ledger_entries", "game_id", "games", null, null, "id", ReferentialAction.NoAction, ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey("fk_game_quiz_rounds_games_game_id", "game_quiz_rounds", "game_id", "games", null, null, "id", ReferentialAction.NoAction, ReferentialAction.Cascade);
        migrationBuilder.AddForeignKey("fk_game_round_cell_media_game_rounds_round_id", "game_round_cell_media", "round_id", "game_rounds", null, null, "id", ReferentialAction.NoAction, ReferentialAction.Cascade);
        migrationBuilder.AddForeignKey("fk_game_round_modifier_results_game_rounds_round_id", "game_round_modifier_results", "round_id", "game_rounds", null, null, "id", ReferentialAction.NoAction, ReferentialAction.Cascade);
        migrationBuilder.AddForeignKey("fk_modifier_results_definition", "game_round_modifier_results", "modifier_id", "modifier_definitions", null, null, "id", ReferentialAction.NoAction, ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey("fk_game_round_participants_game_rounds_round_id", "game_round_participants", "round_id", "game_rounds", null, null, "id", ReferentialAction.NoAction, ReferentialAction.Cascade);
        migrationBuilder.AddForeignKey("fk_game_round_transition_audits_game_rounds_round_id", "game_round_transition_audits", "round_id", "game_rounds", null, null, "id", ReferentialAction.NoAction, ReferentialAction.Cascade);
        migrationBuilder.AddForeignKey("fk_game_rounds_game_teams_same_game", "game_rounds", new string[2] { "game_id", "team_id" }, "game_teams", null, null, new string[2] { "game_id", "id" }, ReferentialAction.NoAction, ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey("fk_game_rounds_games_game_id", "game_rounds", "game_id", "games", null, null, "id", ReferentialAction.NoAction, ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey("fk_game_team_final_results_team_same_game", "game_team_final_results", new string[2] { "game_id", "team_id" }, "game_teams", null, null, new string[2] { "game_id", "id" }, ReferentialAction.NoAction, ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey("fk_game_team_invitations_game_team_slots_game_id_slot_id", "game_team_invitations", new string[2] { "game_id", "slot_id" }, "game_team_slots", null, null, new string[2] { "game_id", "id" }, ReferentialAction.NoAction, ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey("fk_game_team_invitations_games_game_id", "game_team_invitations", "game_id", "games", null, null, "id", ReferentialAction.NoAction, ReferentialAction.Cascade);
        migrationBuilder.AddForeignKey("fk_game_team_invitations_team_same_game", "game_team_invitations", new string[2] { "game_id", "team_id" }, "game_teams", null, null, new string[2] { "game_id", "id" }, ReferentialAction.NoAction, ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey("fk_game_team_members_game_teams_game_id_team_id", "game_team_members", new string[2] { "game_id", "team_id" }, "game_teams", null, null, new string[2] { "game_id", "id" }, ReferentialAction.NoAction, ReferentialAction.Cascade);
        migrationBuilder.AddForeignKey("fk_game_team_members_games_game_id", "game_team_members", "game_id", "games", null, null, "id", ReferentialAction.NoAction, ReferentialAction.Cascade);
        migrationBuilder.AddForeignKey("fk_game_team_slots_games_game_id", "game_team_slots", "game_id", "games", null, null, "id", ReferentialAction.NoAction, ReferentialAction.Cascade);
        migrationBuilder.AddForeignKey("fk_game_teams_games_game_id", "game_teams", "game_id", "games", null, null, "id", ReferentialAction.NoAction, ReferentialAction.Cascade);
        migrationBuilder.AddForeignKey("fk_modifier_conflicts_version", "modifier_definition_version_conflicts", "modifier_version_id", "modifier_definition_versions", null, null, "id", ReferentialAction.NoAction, ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey("fk_modifier_conflicts_definition", "modifier_definition_version_conflicts", "conflicting_modifier_id", "modifier_definitions", null, null, "id", ReferentialAction.NoAction, ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey("fk_modifier_versions_cascade_source", "modifier_definition_versions", "cascade_source_modifier_id", "modifier_definitions", null, null, "id", ReferentialAction.NoAction, ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey("fk_modifier_versions_definition", "modifier_definition_versions", "modifier_id", "modifier_definitions", null, null, "id", ReferentialAction.NoAction, ReferentialAction.Restrict);
        migrationBuilder.Sql("CREATE FUNCTION deadmans_text_array_is_clean(\n    p_values text[],\n    p_min_count integer,\n    p_max_count integer,\n    p_max_item_length integer,\n    p_unique_case_insensitive boolean,\n    p_require_trimmed boolean\n)\nRETURNS boolean\nLANGUAGE sql\nIMMUTABLE\nSTRICT\nPARALLEL SAFE\nSET search_path = public, pg_temp\nAS $$\n    SELECT cardinality(p_values) BETWEEN p_min_count AND p_max_count\n       AND NOT EXISTS (\n           SELECT 1\n           FROM unnest(p_values) value\n           WHERE value IS NULL\n              OR length(btrim(value)) = 0\n              OR length(value) > p_max_item_length\n              OR (p_require_trimmed AND value <> btrim(value))\n       )\n       AND (\n           NOT p_unique_case_insensitive\n           OR cardinality(p_values) = (\n               SELECT count(DISTINCT lower(value))\n               FROM unnest(p_values) value\n           )\n       );\n$$;\n\nALTER TABLE game_enabled_questions\n    ADD CONSTRAINT ck_game_enabled_questions_accepted_answers_clean\n    CHECK (deadmans_text_array_is_clean(accepted_answers_snapshot, 1, 32767, 500, FALSE, FALSE)),\n    ADD CONSTRAINT ck_game_enabled_questions_normalized_answers_clean\n    CHECK (deadmans_text_array_is_clean(normalized_answers_snapshot, 1, 32767, 500, TRUE, TRUE));\nALTER TABLE game_quiz_rounds\n    ADD CONSTRAINT ck_game_quiz_rounds_accepted_answers_clean\n    CHECK (deadmans_text_array_is_clean(accepted_answers_snapshot, 1, 32767, 500, FALSE, FALSE)),\n    ADD CONSTRAINT ck_game_quiz_rounds_normalized_answers_clean\n    CHECK (deadmans_text_array_is_clean(normalized_answers_snapshot, 1, 32767, 500, TRUE, TRUE));\nALTER TABLE modifier_definition_versions\n    ADD CONSTRAINT ck_modifier_definition_versions_tags_clean\n    CHECK (deadmans_text_array_is_clean(normalized_tags, 0, 5, 128, TRUE, TRUE)),\n    ADD CONSTRAINT ck_modifier_definition_versions_changed_fields_clean\n    CHECK (\n        deadmans_text_array_is_clean(changed_fields, 0, 11, 32, TRUE, TRUE)\n        AND changed_fields <@ ARRAY[\n            'created', 'name', 'description', 'category', 'iconEmoji',\n            'activationCommand', 'activationCost', 'activationLimit',\n            'normalizedTags', 'behaviorV2', 'compatibility'\n        ]::text[]\n    );\nALTER TABLE game_modifier_activations\n    ADD CONSTRAINT ck_game_modifier_activations_tags_clean\n    CHECK (deadmans_text_array_is_clean(normalized_tags_snapshot, 0, 5, 128, TRUE, TRUE));\nALTER TABLE game_round_modifier_results\n    ADD CONSTRAINT ck_game_round_modifier_results_tags_clean\n    CHECK (deadmans_text_array_is_clean(modifier_normalized_tags_snapshot, 0, 5, 128, TRUE, TRUE));\nALTER TABLE game_team_final_results\n    ADD CONSTRAINT ck_game_team_final_results_participant_names_clean\n    CHECK (deadmans_text_array_is_clean(participant_names_snapshot, 0, 32767, 128, FALSE, FALSE));\n\nCREATE FUNCTION deadmans_reject_immutable_change()\nRETURNS trigger\nLANGUAGE plpgsql\nSET search_path = public, pg_temp\nAS $$\nBEGIN\n    RAISE EXCEPTION 'Rows in table % are immutable.', TG_TABLE_NAME\n        USING ERRCODE = '55000';\nEND;\n$$;\n\nCREATE TRIGGER trg_modifier_definition_versions_immutable\n    BEFORE UPDATE OR DELETE ON modifier_definition_versions\n    FOR EACH ROW EXECUTE FUNCTION deadmans_reject_immutable_change();\nCREATE TRIGGER trg_modifier_definition_version_conflicts_immutable\n    BEFORE UPDATE OR DELETE ON modifier_definition_version_conflicts\n    FOR EACH ROW EXECUTE FUNCTION deadmans_reject_immutable_change();\nCREATE TRIGGER trg_game_quiz_correct_answers_immutable\n    BEFORE UPDATE OR DELETE ON game_quiz_correct_answers\n    FOR EACH ROW EXECUTE FUNCTION deadmans_reject_immutable_change();\nCREATE TRIGGER trg_game_quiz_point_ledger_immutable\n    BEFORE UPDATE OR DELETE ON game_quiz_point_ledger_entries\n    FOR EACH ROW EXECUTE FUNCTION deadmans_reject_immutable_change();\nCREATE TRIGGER trg_game_round_transition_audits_immutable\n    BEFORE UPDATE OR DELETE ON game_round_transition_audits\n    FOR EACH ROW EXECUTE FUNCTION deadmans_reject_immutable_change();\nCREATE TRIGGER trg_game_finalizations_immutable\n    BEFORE UPDATE OR DELETE ON game_finalizations\n    FOR EACH ROW EXECUTE FUNCTION deadmans_reject_immutable_change();\nCREATE TRIGGER trg_game_team_final_results_immutable\n    BEFORE UPDATE OR DELETE ON game_team_final_results\n    FOR EACH ROW EXECUTE FUNCTION deadmans_reject_immutable_change();\nCREATE TRIGGER trg_game_round_participants_immutable\n    BEFORE UPDATE OR DELETE ON game_round_participants\n    FOR EACH ROW EXECUTE FUNCTION deadmans_reject_immutable_change();\nCREATE TRIGGER trg_game_round_cell_media_immutable\n    BEFORE UPDATE OR DELETE ON game_round_cell_media\n    FOR EACH ROW EXECUTE FUNCTION deadmans_reject_immutable_change();\n\nCREATE FUNCTION deadmans_validate_round_update()\nRETURNS trigger\nLANGUAGE plpgsql\nSET search_path = public, pg_temp\nAS $$\nBEGIN\n    IF TG_OP = 'DELETE' THEN\n        RAISE EXCEPTION 'Played round history cannot be deleted.'\n            USING ERRCODE = '55000';\n    END IF;\n\n    IF OLD.status IN ('completed', 'cancelled') THEN\n        RAISE EXCEPTION 'A terminal round is immutable.'\n            USING ERRCODE = '55000';\n    END IF;\n\n    IF ROW(\n        OLD.id, OLD.game_id, OLD.board_id, OLD.board_cell_id, OLD.team_id,\n        OLD.base_score, OLD.team_slot_index_snapshot,\n        OLD.cell_row_index, OLD.cell_col_index, OLD.cell_title_snapshot,\n        OLD.cell_description_snapshot, OLD.cell_cost_snapshot, OLD.created_at_utc\n    ) IS DISTINCT FROM ROW(\n        NEW.id, NEW.game_id, NEW.board_id, NEW.board_cell_id, NEW.team_id,\n        NEW.base_score, NEW.team_slot_index_snapshot,\n        NEW.cell_row_index, NEW.cell_col_index, NEW.cell_title_snapshot,\n        NEW.cell_description_snapshot, NEW.cell_cost_snapshot, NEW.created_at_utc\n    ) THEN\n        RAISE EXCEPTION 'Round ownership and frozen snapshots are immutable.'\n            USING ERRCODE = '55000';\n    END IF;\n\n    IF OLD.status IS NOT DISTINCT FROM NEW.status THEN\n        IF NEW.version <> OLD.version + 1\n           OR ROW(\n                OLD.id, OLD.game_id, OLD.board_id, OLD.board_cell_id, OLD.team_id,\n                OLD.status, OLD.prepared_at_utc,\n                OLD.gameplay_started_at_utc, OLD.reviewed_at_utc, OLD.finished_at_utc,\n                OLD.base_score, OLD.final_score, OLD.empty_card_penalty_applied,\n                OLD.kills_count, OLD.bounty_count, OLD.team_slot_index_snapshot,\n                OLD.cell_row_index, OLD.cell_col_index, OLD.cell_title_snapshot,\n                OLD.cell_description_snapshot, OLD.cell_cost_snapshot, OLD.notes,\n                OLD.technical_cancellation_reason_code, OLD.public_cancellation_summary,\n                OLD.internal_cancellation_detail, OLD.resolved_by_user_id, OLD.created_at_utc\n           ) IS DISTINCT FROM ROW(\n                NEW.id, NEW.game_id, NEW.board_id, NEW.board_cell_id, NEW.team_id,\n                NEW.status, NEW.prepared_at_utc,\n                NEW.gameplay_started_at_utc, NEW.reviewed_at_utc, NEW.finished_at_utc,\n                NEW.base_score, NEW.final_score, NEW.empty_card_penalty_applied,\n                NEW.kills_count, NEW.bounty_count, NEW.team_slot_index_snapshot,\n                NEW.cell_row_index, NEW.cell_col_index, NEW.cell_title_snapshot,\n                NEW.cell_description_snapshot, NEW.cell_cost_snapshot, NEW.notes,\n                NEW.technical_cancellation_reason_code, NEW.public_cancellation_summary,\n                NEW.internal_cancellation_detail, NEW.resolved_by_user_id, NEW.created_at_utc\n           ) THEN\n            RAISE EXCEPTION 'A same-state round update may only advance its concurrency version.'\n                USING ERRCODE = '23514',\n                      CONSTRAINT = 'ck_game_rounds_same_state_version_update';\n        END IF;\n        RETURN NEW;\n    END IF;\n\n    IF NEW.version <> OLD.version + 1\n       OR NOT (\n            (OLD.status = 'awaiting_modifiers' AND NEW.status IN ('preparing', 'in_progress', 'cancelled'))\n            OR (OLD.status = 'preparing' AND NEW.status IN ('awaiting_modifiers', 'in_progress', 'cancelled'))\n            OR (OLD.status = 'in_progress' AND NEW.status IN ('reviewing_results', 'cancelled'))\n            OR (OLD.status = 'reviewing_results' AND NEW.status IN ('in_progress', 'completed', 'cancelled'))\n       ) THEN\n        RAISE EXCEPTION 'Invalid round lifecycle transition or version.'\n            USING ERRCODE = '23514',\n                  CONSTRAINT = 'ck_game_rounds_lifecycle_transition';\n    END IF;\n\n    RETURN NEW;\nEND;\n$$;\n\nCREATE TRIGGER trg_game_rounds_lifecycle_transition\n    BEFORE UPDATE OR DELETE ON game_rounds\n    FOR EACH ROW EXECUTE FUNCTION deadmans_validate_round_update();\n\nCREATE FUNCTION deadmans_assert_round_transition_audit(p_round_id uuid)\nRETURNS void\nLANGUAGE plpgsql\nSET search_path = public, pg_temp\nAS $$\nDECLARE\n    current_round game_rounds%ROWTYPE;\nBEGIN\n    SELECT * INTO current_round FROM game_rounds WHERE id = p_round_id;\n    IF NOT FOUND OR current_round.version = 1 THEN\n        RETURN;\n    END IF;\n\n    IF NOT EXISTS (\n        SELECT 1\n        FROM game_round_transition_audits audit\n        WHERE audit.round_id = current_round.id\n          AND audit.resulting_round_version = current_round.version\n          AND audit.to_status = current_round.status\n    ) THEN\n        RAISE EXCEPTION 'Every round transition must have a matching audit row.'\n            USING ERRCODE = '23514',\n                  CONSTRAINT = 'ck_game_rounds_transition_audit_required';\n    END IF;\nEND;\n$$;\n\nCREATE FUNCTION deadmans_validate_round_transition_audit_trigger()\nRETURNS trigger\nLANGUAGE plpgsql\nSET search_path = public, pg_temp\nAS $$\nDECLARE\n    affected_round_id uuid := (to_jsonb(NEW) ->> 'round_id')::uuid;\nBEGIN\n    IF TG_TABLE_NAME = 'game_rounds' THEN\n        affected_round_id := (to_jsonb(NEW) ->> 'id')::uuid;\n    END IF;\n    PERFORM deadmans_assert_round_transition_audit(affected_round_id);\n    RETURN NULL;\nEND;\n$$;\n\nCREATE CONSTRAINT TRIGGER trg_game_rounds_transition_audit_consistency\n    AFTER UPDATE ON game_rounds\n    DEFERRABLE INITIALLY DEFERRED\n    FOR EACH ROW\n    WHEN (OLD.status IS DISTINCT FROM NEW.status)\n    EXECUTE FUNCTION deadmans_validate_round_transition_audit_trigger();\nCREATE CONSTRAINT TRIGGER trg_game_round_transition_audits_consistency\n    AFTER INSERT ON game_round_transition_audits\n    DEFERRABLE INITIALLY DEFERRED\n    FOR EACH ROW EXECUTE FUNCTION deadmans_validate_round_transition_audit_trigger();\n\nCREATE FUNCTION deadmans_validate_modifier_activation_update()\nRETURNS trigger\nLANGUAGE plpgsql\nSET search_path = public, pg_temp\nAS $$\nBEGIN\n    IF TG_OP = 'DELETE' THEN\n        RAISE EXCEPTION 'Modifier activation history cannot be deleted.'\n            USING ERRCODE = '55000';\n    END IF;\n\n    IF ROW(\n        OLD.id, OLD.game_id, OLD.round_id, OLD.modifier_id, OLD.modifier_version_id,\n        OLD.activated_by_user_id, OLD.initiated_by_user_id,\n        OLD.activation_cost_snapshot, OLD.definition_revision_snapshot,\n        OLD.modifier_name_snapshot, OLD.modifier_description_snapshot,\n        OLD.modifier_category_snapshot, OLD.modifier_icon_emoji_snapshot,\n        OLD.activation_command_snapshot, OLD.normalized_tags_snapshot,\n        OLD.behavior_v2_snapshot_json, OLD.activated_at_utc\n    ) IS DISTINCT FROM ROW(\n        NEW.id, NEW.game_id, NEW.round_id, NEW.modifier_id, NEW.modifier_version_id,\n        NEW.activated_by_user_id, NEW.initiated_by_user_id,\n        NEW.activation_cost_snapshot, NEW.definition_revision_snapshot,\n        NEW.modifier_name_snapshot, NEW.modifier_description_snapshot,\n        NEW.modifier_category_snapshot, NEW.modifier_icon_emoji_snapshot,\n        NEW.activation_command_snapshot, NEW.normalized_tags_snapshot,\n        NEW.behavior_v2_snapshot_json, NEW.activated_at_utc\n    ) THEN\n        RAISE EXCEPTION 'Modifier activation identity and snapshots are immutable.'\n            USING ERRCODE = '55000';\n    END IF;\n\n    IF OLD.status = 'cancelled'\n       OR (OLD.archived_at_utc IS NOT NULL AND OLD.archived_at_utc IS DISTINCT FROM NEW.archived_at_utc)\n       OR (OLD.status IS DISTINCT FROM NEW.status AND NOT (\n            (OLD.status = 'active' AND NEW.status IN ('consumed', 'cancelled'))\n            OR (OLD.status = 'consumed' AND OLD.archived_at_utc IS NULL AND NEW.status = 'cancelled')\n       )) THEN\n        RAISE EXCEPTION 'Invalid modifier activation lifecycle transition.'\n            USING ERRCODE = '23514',\n                  CONSTRAINT = 'ck_game_modifier_activations_lifecycle_transition';\n    END IF;\n\n    RETURN NEW;\nEND;\n$$;\n\nCREATE TRIGGER trg_game_modifier_activations_lifecycle_transition\n    BEFORE UPDATE OR DELETE ON game_modifier_activations\n    FOR EACH ROW EXECUTE FUNCTION deadmans_validate_modifier_activation_update();\n\nCREATE FUNCTION deadmans_validate_modifier_result_update()\nRETURNS trigger\nLANGUAGE plpgsql\nSET search_path = public, pg_temp\nAS $$\nBEGIN\n    IF TG_OP = 'DELETE' THEN\n        RAISE EXCEPTION 'Modifier result history cannot be deleted.'\n            USING ERRCODE = '55000';\n    END IF;\n\n    IF OLD.outcome_status <> 'pending' THEN\n        RAISE EXCEPTION 'A resolved modifier result is immutable.'\n            USING ERRCODE = '55000';\n    END IF;\n\n    IF ROW(\n        OLD.id, OLD.round_id, OLD.modifier_activation_id, OLD.modifier_id,\n        OLD.modifier_name_snapshot, OLD.modifier_category_snapshot,\n        OLD.modifier_description_snapshot, OLD.definition_revision_snapshot,\n        OLD.modifier_activation_command_snapshot, OLD.modifier_normalized_tags_snapshot,\n        OLD.modifier_behavior_v2_snapshot_json, OLD.created_at_utc\n    ) IS DISTINCT FROM ROW(\n        NEW.id, NEW.round_id, NEW.modifier_activation_id, NEW.modifier_id,\n        NEW.modifier_name_snapshot, NEW.modifier_category_snapshot,\n        NEW.modifier_description_snapshot, NEW.definition_revision_snapshot,\n        NEW.modifier_activation_command_snapshot, NEW.modifier_normalized_tags_snapshot,\n        NEW.modifier_behavior_v2_snapshot_json, NEW.created_at_utc\n    ) OR NEW.outcome_status = 'pending' THEN\n        RAISE EXCEPTION 'Modifier result snapshots are immutable and may resolve only once.'\n            USING ERRCODE = '23514',\n                  CONSTRAINT = 'ck_game_round_modifier_results_lifecycle_transition';\n    END IF;\n\n    RETURN NEW;\nEND;\n$$;\n\nCREATE TRIGGER trg_game_round_modifier_results_lifecycle_transition\n    BEFORE UPDATE OR DELETE ON game_round_modifier_results\n    FOR EACH ROW EXECUTE FUNCTION deadmans_validate_modifier_result_update();\n\nCREATE FUNCTION deadmans_validate_ledger_insert()\nRETURNS trigger\nLANGUAGE plpgsql\nSET search_path = public, pg_temp\nAS $$\nDECLARE\n    expected_balance bigint;\n    ledger_game games%ROWTYPE;\nBEGIN\n    -- Keep lifecycle transitions mutually exclusive with point writes without\n    -- serializing every viewer in the game behind one exclusive row lock.\n    SELECT * INTO ledger_game FROM games WHERE id = NEW.game_id FOR SHARE;\n    IF NOT FOUND THEN\n        RAISE EXCEPTION 'Ledger entry references an unknown game.'\n            USING ERRCODE = '23503';\n    END IF;\n\n    IF ledger_game.status <> 'active'\n       OR ledger_game.is_deleted\n       OR NEW.occurred_at_utc < ledger_game.started_at_utc THEN\n        RAISE EXCEPTION 'Quiz points may only change during the active game.'\n            USING ERRCODE = '23514',\n                  CONSTRAINT = 'ck_quiz_point_ledger_active_game';\n    END IF;\n\n    -- Only one writer may advance a particular per-game viewer balance at once.\n    -- The 64-bit key is transaction-scoped and does not require another table.\n    PERFORM pg_advisory_xact_lock(\n        hashtextextended(NEW.game_id::text || ':' || NEW.user_id::text, 0)\n    );\n\n    SELECT COALESCE(sum(points_delta), 0)\n    INTO expected_balance\n    FROM game_quiz_point_ledger_entries\n    WHERE game_id = NEW.game_id AND user_id = NEW.user_id;\n\n    IF NEW.available_points_before <> expected_balance THEN\n        RAISE EXCEPTION\n            'Ledger balance mismatch for game %, user %: expected %, received %.',\n            NEW.game_id, NEW.user_id, expected_balance, NEW.available_points_before\n            USING ERRCODE = '23514',\n                  CONSTRAINT = 'ck_quiz_point_ledger_running_balance';\n    END IF;\n\n    RETURN NEW;\nEND;\n$$;\n\nCREATE TRIGGER trg_game_quiz_point_ledger_running_balance\n    BEFORE INSERT ON game_quiz_point_ledger_entries\n    FOR EACH ROW EXECUTE FUNCTION deadmans_validate_ledger_insert();\n\nCREATE FUNCTION deadmans_assert_question_answers(p_question_id uuid)\nRETURNS void\nLANGUAGE plpgsql\nSET search_path = public, pg_temp\nAS $$\nDECLARE\n    answer_count bigint;\n    primary_count bigint;\nBEGIN\n    IF NOT EXISTS (SELECT 1 FROM question_definitions WHERE id = p_question_id) THEN\n        RETURN;\n    END IF;\n\n    SELECT count(*), count(*) FILTER (WHERE is_primary)\n    INTO answer_count, primary_count\n    FROM question_accepted_answers\n    WHERE question_id = p_question_id;\n\n    IF answer_count = 0 OR primary_count <> 1 THEN\n        RAISE EXCEPTION\n            'Question % must have accepted answers and exactly one primary answer.',\n            p_question_id\n            USING ERRCODE = '23514',\n                  CONSTRAINT = 'ck_question_accepted_answers_complete_set';\n    END IF;\nEND;\n$$;\n\nCREATE FUNCTION deadmans_validate_question_answers_trigger()\nRETURNS trigger\nLANGUAGE plpgsql\nSET search_path = public, pg_temp\nAS $$\nDECLARE\n    new_row jsonb := to_jsonb(NEW);\n    old_row jsonb := to_jsonb(OLD);\n    new_question_id uuid;\n    old_question_id uuid;\nBEGIN\n    IF TG_TABLE_NAME = 'question_definitions' THEN\n        IF TG_OP <> 'DELETE' THEN\n            PERFORM deadmans_assert_question_answers((new_row ->> 'id')::uuid);\n        END IF;\n    ELSE\n        IF TG_OP <> 'DELETE' THEN\n            new_question_id := (new_row ->> 'question_id')::uuid;\n            PERFORM deadmans_assert_question_answers(new_question_id);\n        END IF;\n        IF TG_OP <> 'INSERT' THEN\n            old_question_id := (old_row ->> 'question_id')::uuid;\n        END IF;\n        IF TG_OP <> 'INSERT'\n           AND (TG_OP = 'DELETE' OR old_question_id IS DISTINCT FROM new_question_id) THEN\n            PERFORM deadmans_assert_question_answers(old_question_id);\n        END IF;\n    END IF;\n    RETURN NULL;\nEND;\n$$;\n\nCREATE CONSTRAINT TRIGGER trg_question_definitions_answer_set\n    AFTER INSERT OR UPDATE ON question_definitions\n    DEFERRABLE INITIALLY DEFERRED\n    FOR EACH ROW EXECUTE FUNCTION deadmans_validate_question_answers_trigger();\nCREATE CONSTRAINT TRIGGER trg_question_accepted_answers_complete_set\n    AFTER INSERT OR UPDATE OR DELETE ON question_accepted_answers\n    DEFERRABLE INITIALLY DEFERRED\n    FOR EACH ROW EXECUTE FUNCTION deadmans_validate_question_answers_trigger();\n\nCREATE FUNCTION deadmans_assert_modifier_catalog()\nRETURNS void\nLANGUAGE plpgsql\nSET search_path = public, pg_temp\nAS $$\nBEGIN\n    IF EXISTS (\n        SELECT 1 FROM modifier_definitions WHERE current_version_id IS NULL\n    ) THEN\n        RAISE EXCEPTION 'Every modifier must reference a current immutable version.'\n            USING ERRCODE = '23514',\n                  CONSTRAINT = 'ck_modifier_definitions_current_version_required';\n    END IF;\n\n    IF EXISTS (\n        SELECT 1\n        FROM modifier_definitions d\n        LEFT JOIN modifier_definition_versions v ON v.modifier_id = d.id\n        GROUP BY d.id\n        HAVING count(v.id) = 0\n            OR min(v.revision) <> 1\n            OR max(v.revision) <> count(v.id)\n    ) THEN\n        RAISE EXCEPTION 'Modifier revisions must be contiguous and start at 1.'\n            USING ERRCODE = '23514',\n                  CONSTRAINT = 'ck_modifier_definition_versions_contiguous';\n    END IF;\n\n    IF EXISTS (\n        SELECT 1\n        FROM modifier_definitions d\n        JOIN modifier_definition_versions current_version\n          ON current_version.id = d.current_version_id\n        WHERE current_version.revision <> (\n            SELECT max(candidate.revision)\n            FROM modifier_definition_versions candidate\n            WHERE candidate.modifier_id = d.id\n        )\n    ) THEN\n        RAISE EXCEPTION 'A modifier current version must be its latest revision.'\n            USING ERRCODE = '23514',\n                  CONSTRAINT = 'ck_modifier_definitions_current_is_latest';\n    END IF;\n\n    IF EXISTS (\n        SELECT 1\n        FROM modifier_definition_version_conflicts conflict\n        JOIN modifier_definition_versions version\n          ON version.id = conflict.modifier_version_id\n        WHERE version.modifier_id = conflict.conflicting_modifier_id\n    ) THEN\n        RAISE EXCEPTION 'A modifier cannot conflict with itself.'\n            USING ERRCODE = '23514',\n                  CONSTRAINT = 'ck_modifier_definition_conflicts_no_self_reference';\n    END IF;\n\n    IF EXISTS (\n        SELECT 1\n        FROM modifier_definitions source_definition\n        JOIN modifier_definition_version_conflicts source_conflict\n          ON source_conflict.modifier_version_id = source_definition.current_version_id\n        JOIN modifier_definitions target_definition\n          ON target_definition.id = source_conflict.conflicting_modifier_id\n        JOIN modifier_definition_versions target_version\n          ON target_version.id = target_definition.current_version_id\n        WHERE source_conflict.conflicting_modifier_name_snapshot\n                  IS DISTINCT FROM target_version.name\n           OR NOT EXISTS (\n                SELECT 1\n                FROM modifier_definition_version_conflicts reciprocal\n                WHERE reciprocal.modifier_version_id = target_definition.current_version_id\n                  AND reciprocal.conflicting_modifier_id = source_definition.id\n           )\n    ) THEN\n        RAISE EXCEPTION\n            'Current modifier conflicts must be symmetric and contain current name snapshots.'\n            USING ERRCODE = '23514',\n                  CONSTRAINT = 'ck_modifier_definition_conflicts_current_symmetry';\n    END IF;\nEND;\n$$;\n\nCREATE FUNCTION deadmans_validate_modifier_catalog_trigger()\nRETURNS trigger\nLANGUAGE plpgsql\nSET search_path = public, pg_temp\nAS $$\nBEGIN\n    PERFORM deadmans_assert_modifier_catalog();\n    RETURN NULL;\nEND;\n$$;\n\nCREATE CONSTRAINT TRIGGER trg_modifier_definitions_catalog_consistency\n    AFTER INSERT OR UPDATE ON modifier_definitions\n    DEFERRABLE INITIALLY DEFERRED\n    FOR EACH ROW EXECUTE FUNCTION deadmans_validate_modifier_catalog_trigger();\nCREATE CONSTRAINT TRIGGER trg_modifier_definition_versions_catalog_consistency\n    AFTER INSERT ON modifier_definition_versions\n    DEFERRABLE INITIALLY DEFERRED\n    FOR EACH ROW EXECUTE FUNCTION deadmans_validate_modifier_catalog_trigger();\nCREATE CONSTRAINT TRIGGER trg_modifier_definition_conflicts_catalog_consistency\n    AFTER INSERT ON modifier_definition_version_conflicts\n    DEFERRABLE INITIALLY DEFERRED\n    FOR EACH ROW EXECUTE FUNCTION deadmans_validate_modifier_catalog_trigger();\n\nCREATE FUNCTION deadmans_assert_game_publication(p_game_id uuid)\nRETURNS void\nLANGUAGE plpgsql\nSET search_path = public, pg_temp\nAS $$\nDECLARE\n    published_game games%ROWTYPE;\nBEGIN\n    SELECT * INTO published_game FROM games WHERE id = p_game_id;\n    IF NOT FOUND OR published_game.status = 'draft' THEN\n        RETURN;\n    END IF;\n\n    IF published_game.ready_at_utc IS NULL THEN\n        RAISE EXCEPTION 'A published game requires its publication timestamp.'\n            USING ERRCODE = '23514',\n                  CONSTRAINT = 'ck_games_publication_timestamp_required';\n    END IF;\n\n    IF (SELECT count(*) FROM game_boards board WHERE board.game_id = p_game_id) <> 1\n       OR NOT EXISTS (\n           SELECT 1 FROM game_team_slots slot WHERE slot.game_id = p_game_id\n       ) THEN\n        RAISE EXCEPTION 'A published game requires exactly one board and at least one team slot.'\n            USING ERRCODE = '23514',\n                  CONSTRAINT = 'ck_games_publication_setup_complete';\n    END IF;\n\n    IF published_game.status = 'ready' AND EXISTS (\n        SELECT 1\n        FROM game_board_cells cell\n        JOIN game_boards board ON board.id = cell.board_id\n        WHERE board.game_id = p_game_id\n          AND cell.state <> 'closed'\n    ) THEN\n        RAISE EXCEPTION 'Every board cell must be closed when a game is published.'\n            USING ERRCODE = '23514',\n                  CONSTRAINT = 'ck_games_publication_cells_closed';\n    END IF;\n\n    IF EXISTS (\n        SELECT 1\n        FROM game_enabled_modifiers enabled\n        JOIN modifier_definitions definition\n          ON definition.id = enabled.modifier_id\n        LEFT JOIN modifier_definition_versions version\n          ON version.id = enabled.modifier_version_id\n         AND version.modifier_id = enabled.modifier_id\n        WHERE enabled.game_id = p_game_id\n          AND (\n              enabled.modifier_version_id IS NULL\n              OR (published_game.status = 'ready' AND definition.is_archived)\n              OR enabled.version_pinned_at_utc IS DISTINCT FROM published_game.ready_at_utc\n              OR enabled.enabled_at_utc > published_game.ready_at_utc\n              OR version.id IS NULL\n              OR version.created_at_utc > published_game.ready_at_utc\n              OR version.revision <> (\n                  SELECT max(candidate.revision)\n                  FROM modifier_definition_versions candidate\n                  WHERE candidate.modifier_id = enabled.modifier_id\n                    AND candidate.created_at_utc <= published_game.ready_at_utc\n              )\n          )\n    ) THEN\n        RAISE EXCEPTION 'Every published modifier must be pinned at publication.'\n            USING ERRCODE = '23514',\n                  CONSTRAINT = 'ck_game_enabled_modifiers_published_pin';\n    END IF;\n\n    IF EXISTS (\n        SELECT 1\n        FROM game_enabled_questions enabled\n        JOIN question_definitions question ON question.id = enabled.question_id\n        JOIN question_categories category ON category.id = question.category_id\n        LEFT JOIN LATERAL (\n            SELECT\n                array_agg(answer.answer_text::text ORDER BY answer.is_primary DESC, answer.sort_order) AS accepted,\n                array_agg(answer.normalized_answer::text ORDER BY answer.is_primary DESC, answer.sort_order) AS normalized\n            FROM question_accepted_answers answer\n            WHERE answer.question_id = question.id\n        ) answers ON TRUE\n        WHERE enabled.game_id = p_game_id\n          AND (\n              enabled.snapshot_at_utc IS DISTINCT FROM published_game.ready_at_utc\n              OR enabled.enabled_at_utc > published_game.ready_at_utc\n              OR (\n                  published_game.status = 'ready'\n                  AND (\n                      question.is_deleted\n                      OR NOT question.is_enabled\n                      OR enabled.question_revision_snapshot <> question.revision\n                      OR enabled.question_code_snapshot IS DISTINCT FROM question.external_code::text\n                      OR enabled.category_name_snapshot IS DISTINCT FROM category.name::text\n                      OR enabled.question_text_snapshot IS DISTINCT FROM question.text\n                      OR enabled.accepted_answers_snapshot IS DISTINCT FROM answers.accepted\n                      OR enabled.normalized_answers_snapshot IS DISTINCT FROM answers.normalized\n                      OR enabled.reward_snapshot <> question.reward\n                      OR enabled.priority_snapshot <> question.priority\n                  )\n              )\n          )\n    ) THEN\n        RAISE EXCEPTION 'Every published question must exactly match its source at publication.'\n            USING ERRCODE = '23514',\n                  CONSTRAINT = 'ck_game_enabled_questions_published_snapshot';\n    END IF;\nEND;\n$$;\n\nCREATE FUNCTION deadmans_validate_game_publication_trigger()\nRETURNS trigger\nLANGUAGE plpgsql\nSET search_path = public, pg_temp\nAS $$\nDECLARE\n    affected_game_id uuid;\n    new_row jsonb := to_jsonb(NEW);\n    old_row jsonb := to_jsonb(OLD);\nBEGIN\n    IF TG_TABLE_NAME = 'games' THEN\n        affected_game_id := COALESCE(\n            (new_row ->> 'id')::uuid,\n            (old_row ->> 'id')::uuid\n        );\n    ELSE\n        affected_game_id := COALESCE(\n            (new_row ->> 'game_id')::uuid,\n            (old_row ->> 'game_id')::uuid\n        );\n    END IF;\n    IF affected_game_id IS NOT NULL THEN\n        PERFORM deadmans_assert_game_publication(affected_game_id);\n    END IF;\n    RETURN NULL;\nEND;\n$$;\n\nCREATE FUNCTION deadmans_protect_published_game_configuration()\nRETURNS trigger\nLANGUAGE plpgsql\nSET search_path = public, pg_temp\nAS $$\nDECLARE\n    game_status text;\n    publication_time timestamp with time zone;\n    new_row jsonb := to_jsonb(NEW);\n    old_row jsonb := to_jsonb(OLD);\nBEGIN\n    SELECT status, ready_at_utc INTO game_status, publication_time\n    FROM games\n    WHERE id = COALESCE(\n        (new_row ->> 'game_id')::uuid,\n        (old_row ->> 'game_id')::uuid\n    );\n\n    IF NOT FOUND OR game_status = 'draft' THEN\n        IF TG_OP = 'DELETE' THEN\n            RETURN OLD;\n        END IF;\n        RETURN NEW;\n    END IF;\n\n    IF TG_OP = 'UPDATE' AND TG_TABLE_NAME = 'game_enabled_questions'\n       AND game_status = 'ready'\n       AND (old_row ->> 'snapshot_at_utc')::timestamptz IS DISTINCT FROM publication_time\n       AND (new_row ->> 'snapshot_at_utc')::timestamptz IS NOT DISTINCT FROM publication_time\n       AND (old_row -> 'game_id') = (new_row -> 'game_id')\n       AND (old_row -> 'question_id') = (new_row -> 'question_id')\n       AND (old_row -> 'enabled_at_utc') = (new_row -> 'enabled_at_utc') THEN\n        RETURN NEW;\n    END IF;\n\n    IF TG_OP = 'UPDATE' AND TG_TABLE_NAME = 'game_enabled_modifiers'\n       AND game_status = 'ready'\n       AND old_row -> 'modifier_version_id' = 'null'::jsonb\n       AND old_row -> 'version_pinned_at_utc' = 'null'::jsonb\n       AND new_row -> 'modifier_version_id' <> 'null'::jsonb\n       AND (new_row ->> 'version_pinned_at_utc')::timestamptz IS NOT DISTINCT FROM publication_time\n       AND (new_row - ARRAY['modifier_version_id', 'version_pinned_at_utc'])\n           = (old_row - ARRAY['modifier_version_id', 'version_pinned_at_utc']) THEN\n        RETURN NEW;\n    END IF;\n\n    IF TG_OP = 'UPDATE' AND TG_TABLE_NAME = 'game_enabled_modifiers'\n       AND game_status = 'active'\n       AND old_row -> 'emergency_disabled_at_utc' = 'null'::jsonb\n       AND old_row -> 'emergency_disabled_by_user_id' = 'null'::jsonb\n       AND old_row -> 'emergency_disable_reason' = 'null'::jsonb\n       AND new_row -> 'emergency_disabled_at_utc' <> 'null'::jsonb\n       AND new_row -> 'emergency_disabled_by_user_id' <> 'null'::jsonb\n       AND new_row -> 'emergency_disable_reason' <> 'null'::jsonb\n       AND (\n           new_row - ARRAY[\n               'emergency_disabled_at_utc',\n               'emergency_disabled_by_user_id',\n               'emergency_disable_reason'\n           ]\n       ) = (\n           old_row - ARRAY[\n               'emergency_disabled_at_utc',\n               'emergency_disabled_by_user_id',\n               'emergency_disable_reason'\n           ]\n       ) THEN\n        RETURN NEW;\n    END IF;\n\n    RAISE EXCEPTION 'Published game configuration is immutable.'\n        USING ERRCODE = '55000';\nEND;\n$$;\n\nCREATE TRIGGER trg_game_enabled_modifiers_published_configuration\n    BEFORE INSERT OR UPDATE OR DELETE ON game_enabled_modifiers\n    FOR EACH ROW EXECUTE FUNCTION deadmans_protect_published_game_configuration();\nCREATE TRIGGER trg_game_enabled_questions_published_configuration\n    BEFORE INSERT OR UPDATE OR DELETE ON game_enabled_questions\n    FOR EACH ROW EXECUTE FUNCTION deadmans_protect_published_game_configuration();\n\nCREATE CONSTRAINT TRIGGER trg_games_publication_consistency\n    AFTER INSERT OR UPDATE ON games\n    DEFERRABLE INITIALLY DEFERRED\n    FOR EACH ROW EXECUTE FUNCTION deadmans_validate_game_publication_trigger();\nCREATE CONSTRAINT TRIGGER trg_game_enabled_modifiers_publication_consistency\n    AFTER INSERT OR UPDATE OR DELETE ON game_enabled_modifiers\n    DEFERRABLE INITIALLY DEFERRED\n    FOR EACH ROW EXECUTE FUNCTION deadmans_validate_game_publication_trigger();\nCREATE CONSTRAINT TRIGGER trg_game_enabled_questions_publication_consistency\n    AFTER INSERT OR UPDATE OR DELETE ON game_enabled_questions\n    DEFERRABLE INITIALLY DEFERRED\n    FOR EACH ROW EXECUTE FUNCTION deadmans_validate_game_publication_trigger();\n\nCREATE FUNCTION deadmans_assert_quiz_round(p_round_id uuid)\nRETURNS void\nLANGUAGE plpgsql\nSET search_path = public, pg_temp\nAS $$\nDECLARE\n    quiz_round game_quiz_rounds%ROWTYPE;\n    enabled_question game_enabled_questions%ROWTYPE;\n    quiz_game games%ROWTYPE;\n    correct_answer game_quiz_correct_answers%ROWTYPE;\n    has_correct_answer boolean;\n    reward_entry_count bigint;\nBEGIN\n    SELECT * INTO quiz_round FROM game_quiz_rounds WHERE id = p_round_id;\n    IF NOT FOUND THEN\n        RETURN;\n    END IF;\n\n    SELECT * INTO enabled_question\n    FROM game_enabled_questions\n    WHERE game_id = quiz_round.game_id\n      AND question_id = quiz_round.question_id;\n    IF NOT FOUND OR ROW(\n        quiz_round.question_revision_snapshot,\n        quiz_round.question_code_snapshot,\n        quiz_round.category_name_snapshot,\n        quiz_round.question_text_snapshot,\n        quiz_round.accepted_answers_snapshot,\n        quiz_round.normalized_answers_snapshot,\n        quiz_round.reward_snapshot\n    ) IS DISTINCT FROM ROW(\n        enabled_question.question_revision_snapshot,\n        enabled_question.question_code_snapshot,\n        enabled_question.category_name_snapshot,\n        enabled_question.question_text_snapshot,\n        enabled_question.accepted_answers_snapshot,\n        enabled_question.normalized_answers_snapshot,\n        enabled_question.reward_snapshot\n    ) THEN\n        RAISE EXCEPTION 'An asked question must match the game-frozen question snapshot.'\n            USING ERRCODE = '23514',\n                  CONSTRAINT = 'ck_game_quiz_rounds_enabled_snapshot';\n    END IF;\n\n    SELECT * INTO quiz_game FROM games WHERE id = quiz_round.game_id;\n    IF NOT FOUND\n       OR quiz_game.status NOT IN ('active', 'finished')\n       OR quiz_game.is_deleted\n       OR quiz_round.asked_at_utc < quiz_game.started_at_utc\n       OR (\n           quiz_game.finished_at_utc IS NOT NULL\n           AND COALESCE(quiz_round.closed_at_utc, quiz_round.closes_at_utc)\n               > quiz_game.finished_at_utc\n       ) THEN\n        RAISE EXCEPTION 'Quiz history must fit inside an active game lifetime.'\n            USING ERRCODE = '23514',\n                  CONSTRAINT = 'ck_game_quiz_rounds_game_lifetime';\n    END IF;\n\n    IF quiz_round.closes_at_utc IS DISTINCT FROM\n       quiz_round.asked_at_utc\n           + make_interval(secs => quiz_game.quiz_answer_duration_seconds) THEN\n        RAISE EXCEPTION 'A quiz window must use the game answer duration.'\n            USING ERRCODE = '23514',\n                  CONSTRAINT = 'ck_game_quiz_rounds_game_duration';\n    END IF;\n\n    SELECT * INTO correct_answer\n    FROM game_quiz_correct_answers\n    WHERE quiz_round_id = p_round_id;\n    has_correct_answer := FOUND;\n\n    IF quiz_round.status = 'asked' AND NOT EXISTS (\n        SELECT 1 FROM games\n        WHERE id = quiz_round.game_id AND status = 'active' AND is_deleted = FALSE\n    ) THEN\n        RAISE EXCEPTION 'An open quiz round requires an active game.'\n            USING ERRCODE = '23514',\n                  CONSTRAINT = 'ck_game_quiz_rounds_open_game_active';\n    END IF;\n\n    IF quiz_round.status = 'answered_correct' THEN\n        IF NOT has_correct_answer THEN\n            RAISE EXCEPTION 'A correctly answered quiz round requires its winner fact.'\n                USING ERRCODE = '23514',\n                      CONSTRAINT = 'ck_game_quiz_rounds_winner_required';\n        END IF;\n        IF correct_answer.answered_at_utc < quiz_round.asked_at_utc\n           OR correct_answer.answered_at_utc >= quiz_round.closes_at_utc\n           OR quiz_round.closed_at_utc IS DISTINCT FROM correct_answer.answered_at_utc\n           OR NOT (\n                correct_answer.normalized_answer\n                = ANY(quiz_round.normalized_answers_snapshot)\n           ) THEN\n            RAISE EXCEPTION 'The quiz winner must match the frozen answer set and window.'\n                USING ERRCODE = '23514',\n                      CONSTRAINT = 'ck_game_quiz_correct_answers_round_consistency';\n        END IF;\n\n        SELECT count(*) INTO reward_entry_count\n        FROM game_quiz_point_ledger_entries\n        WHERE correct_answer_id = correct_answer.id\n          AND entry_type = 'quiz_reward';\n\n        IF quiz_round.reward_snapshot > 0 THEN\n            IF reward_entry_count <> 1 OR NOT EXISTS (\n                SELECT 1\n                FROM game_quiz_point_ledger_entries reward\n                WHERE reward.correct_answer_id = correct_answer.id\n                  AND reward.entry_type = 'quiz_reward'\n                  AND reward.game_id = quiz_round.game_id\n                  AND reward.user_id = correct_answer.awarded_to_user_id\n                  AND reward.points_delta = quiz_round.reward_snapshot\n                  AND reward.occurred_at_utc = correct_answer.answered_at_utc\n            ) THEN\n                RAISE EXCEPTION 'The quiz winner reward ledger entry is missing or invalid.'\n                    USING ERRCODE = '23514',\n                          CONSTRAINT = 'ck_game_quiz_correct_answers_reward_consistency';\n            END IF;\n        ELSIF reward_entry_count <> 0 THEN\n            RAISE EXCEPTION 'A zero-reward quiz round cannot create a reward entry.'\n                USING ERRCODE = '23514',\n                      CONSTRAINT = 'ck_game_quiz_correct_answers_zero_reward';\n        END IF;\n    ELSIF has_correct_answer THEN\n        RAISE EXCEPTION 'Only an answered_correct quiz round may have a winner fact.'\n            USING ERRCODE = '23514',\n                  CONSTRAINT = 'ck_game_quiz_correct_answers_terminal_state';\n    END IF;\nEND;\n$$;\n\nCREATE FUNCTION deadmans_validate_quiz_round_trigger()\nRETURNS trigger\nLANGUAGE plpgsql\nSET search_path = public, pg_temp\nAS $$\nDECLARE\n    affected_round_id uuid;\n    new_row jsonb := to_jsonb(NEW);\nBEGIN\n    IF TG_TABLE_NAME = 'game_quiz_rounds' THEN\n        affected_round_id := (new_row ->> 'id')::uuid;\n    ELSIF TG_TABLE_NAME = 'game_quiz_correct_answers' THEN\n        affected_round_id := (new_row ->> 'quiz_round_id')::uuid;\n    ELSE\n        SELECT quiz_round_id INTO affected_round_id\n        FROM game_quiz_correct_answers\n        WHERE id = (new_row ->> 'correct_answer_id')::uuid;\n    END IF;\n\n    IF affected_round_id IS NOT NULL THEN\n        PERFORM deadmans_assert_quiz_round(affected_round_id);\n    END IF;\n    RETURN NULL;\nEND;\n$$;\n\nCREATE CONSTRAINT TRIGGER trg_game_quiz_rounds_consistency\n    AFTER INSERT OR UPDATE ON game_quiz_rounds\n    DEFERRABLE INITIALLY DEFERRED\n    FOR EACH ROW EXECUTE FUNCTION deadmans_validate_quiz_round_trigger();\nCREATE CONSTRAINT TRIGGER trg_game_quiz_correct_answers_consistency\n    AFTER INSERT ON game_quiz_correct_answers\n    DEFERRABLE INITIALLY DEFERRED\n    FOR EACH ROW EXECUTE FUNCTION deadmans_validate_quiz_round_trigger();\nCREATE CONSTRAINT TRIGGER trg_game_quiz_reward_consistency\n    AFTER INSERT ON game_quiz_point_ledger_entries\n    DEFERRABLE INITIALLY DEFERRED\n    FOR EACH ROW\n    WHEN (NEW.correct_answer_id IS NOT NULL)\n    EXECUTE FUNCTION deadmans_validate_quiz_round_trigger();\n\nCREATE FUNCTION deadmans_validate_quiz_round_update()\nRETURNS trigger\nLANGUAGE plpgsql\nSET search_path = public, pg_temp\nAS $$\nBEGIN\n    IF OLD.status <> 'asked' THEN\n        RAISE EXCEPTION 'A terminal quiz round is immutable.'\n            USING ERRCODE = '55000';\n    END IF;\n    IF ROW(\n        OLD.id, OLD.game_id, OLD.question_id, OLD.ask_order,\n        OLD.asked_at_utc, OLD.closes_at_utc, OLD.asked_by_user_id,\n        OLD.question_revision_snapshot, OLD.question_code_snapshot,\n        OLD.category_name_snapshot, OLD.question_text_snapshot,\n        OLD.accepted_answers_snapshot, OLD.normalized_answers_snapshot,\n        OLD.reward_snapshot, OLD.delivery_kind, OLD.source_channel_id,\n        OLD.source_message_id\n    ) IS DISTINCT FROM ROW(\n        NEW.id, NEW.game_id, NEW.question_id, NEW.ask_order,\n        NEW.asked_at_utc, NEW.closes_at_utc, NEW.asked_by_user_id,\n        NEW.question_revision_snapshot, NEW.question_code_snapshot,\n        NEW.category_name_snapshot, NEW.question_text_snapshot,\n        NEW.accepted_answers_snapshot, NEW.normalized_answers_snapshot,\n        NEW.reward_snapshot, NEW.delivery_kind, NEW.source_channel_id,\n        NEW.source_message_id\n    ) THEN\n        RAISE EXCEPTION 'Quiz round identity, delivery, window and snapshots are immutable.'\n            USING ERRCODE = '55000';\n    END IF;\n    RETURN NEW;\nEND;\n$$;\n\nCREATE TRIGGER trg_game_quiz_rounds_immutable_snapshot\n    BEFORE UPDATE ON game_quiz_rounds\n    FOR EACH ROW EXECUTE FUNCTION deadmans_validate_quiz_round_update();\n\nCREATE FUNCTION deadmans_assert_game_quiz_state()\nRETURNS trigger\nLANGUAGE plpgsql\nSET search_path = public, pg_temp\nAS $$\nBEGIN\n    IF (NEW.status <> 'active' OR NEW.is_deleted)\n       AND EXISTS (\n            SELECT 1 FROM game_quiz_rounds\n            WHERE game_id = NEW.id AND status = 'asked'\n       ) THEN\n        RAISE EXCEPTION 'A non-active game cannot retain an open quiz round.'\n            USING ERRCODE = '23514',\n                  CONSTRAINT = 'ck_games_no_open_quiz_outside_active';\n    END IF;\n    RETURN NULL;\nEND;\n$$;\n\nCREATE CONSTRAINT TRIGGER trg_games_quiz_state_consistency\n    AFTER UPDATE ON games\n    DEFERRABLE INITIALLY DEFERRED\n    FOR EACH ROW EXECUTE FUNCTION deadmans_assert_game_quiz_state();\n\nCREATE FUNCTION deadmans_validate_game_update()\nRETURNS trigger\nLANGUAGE plpgsql\nSET search_path = public, pg_temp\nAS $$\nBEGIN\n    IF OLD.is_deleted THEN\n        RAISE EXCEPTION 'An archived game is immutable.'\n            USING ERRCODE = '55000';\n    END IF;\n\n    IF OLD.id IS DISTINCT FROM NEW.id\n       OR OLD.created_at_utc IS DISTINCT FROM NEW.created_at_utc THEN\n        RAISE EXCEPTION 'Game identity and creation time are immutable.'\n            USING ERRCODE = '55000';\n    END IF;\n\n    IF OLD.status IS DISTINCT FROM NEW.status\n       AND NOT (\n           (OLD.status = 'draft' AND NEW.status = 'ready')\n           OR (OLD.status = 'ready' AND NEW.status = 'active')\n           OR (OLD.status = 'active' AND NEW.status = 'finished')\n       ) THEN\n        RAISE EXCEPTION 'A game must follow draft, ready, active, finished in order.'\n            USING ERRCODE = '23514',\n                  CONSTRAINT = 'ck_games_lifecycle_transition';\n    END IF;\n\n    IF OLD.ready_at_utc IS DISTINCT FROM NEW.ready_at_utc\n       AND NOT (OLD.status = 'draft' AND NEW.status = 'ready') THEN\n        RAISE EXCEPTION 'ready_at_utc may only be set by draft to ready.'\n            USING ERRCODE = '23514',\n                  CONSTRAINT = 'ck_games_ready_transition_timestamp';\n    END IF;\n    IF OLD.started_at_utc IS DISTINCT FROM NEW.started_at_utc\n       AND NOT (OLD.status = 'ready' AND NEW.status = 'active') THEN\n        RAISE EXCEPTION 'started_at_utc may only be set by ready to active.'\n            USING ERRCODE = '23514',\n                  CONSTRAINT = 'ck_games_active_transition_timestamp';\n    END IF;\n    IF OLD.finished_at_utc IS DISTINCT FROM NEW.finished_at_utc\n       AND NOT (OLD.status = 'active' AND NEW.status = 'finished') THEN\n        RAISE EXCEPTION 'finished_at_utc may only be set by active to finished.'\n            USING ERRCODE = '23514',\n                  CONSTRAINT = 'ck_games_finished_transition_timestamp';\n    END IF;\n\n    IF OLD.status <> 'draft' AND ROW(\n        OLD.title,\n        OLD.description,\n        OLD.min_players_per_team,\n        OLD.max_players_per_team,\n        OLD.quiz_answer_duration_seconds\n    ) IS DISTINCT FROM ROW(\n        NEW.title,\n        NEW.description,\n        NEW.min_players_per_team,\n        NEW.max_players_per_team,\n        NEW.quiz_answer_duration_seconds\n    ) THEN\n        RAISE EXCEPTION 'Published game settings are immutable.'\n            USING ERRCODE = '55000';\n    END IF;\n\n    IF OLD.is_deleted IS DISTINCT FROM NEW.is_deleted THEN\n        IF OLD.is_deleted OR NOT NEW.is_deleted\n           OR OLD.status <> 'finished' OR NEW.status <> 'finished' THEN\n            RAISE EXCEPTION 'Only an already finished game may be archived.'\n                USING ERRCODE = '23514',\n                      CONSTRAINT = 'ck_games_archive_finished_only';\n        END IF;\n    END IF;\n\n    RETURN NEW;\nEND;\n$$;\n\nCREATE TRIGGER trg_games_lifecycle_transition\n    BEFORE UPDATE ON games\n    FOR EACH ROW EXECUTE FUNCTION deadmans_validate_game_update();\n\nCREATE FUNCTION deadmans_protect_game_delete()\nRETURNS trigger\nLANGUAGE plpgsql\nSET search_path = public, pg_temp\nAS $$\nBEGIN\n    IF OLD.status <> 'draft' THEN\n        RAISE EXCEPTION 'Only a draft game may be physically deleted.'\n            USING ERRCODE = '23514',\n                  CONSTRAINT = 'ck_games_hard_delete_draft_only';\n    END IF;\n    RETURN OLD;\nEND;\n$$;\n\nCREATE TRIGGER trg_games_hard_delete_draft_only\n    BEFORE DELETE ON games\n    FOR EACH ROW EXECUTE FUNCTION deadmans_protect_game_delete();\n\nCREATE FUNCTION deadmans_protect_published_board_configuration()\nRETURNS trigger\nLANGUAGE plpgsql\nSET search_path = public, pg_temp\nAS $$\nDECLARE\n    affected_game_id uuid;\n    game_status text;\n    new_row jsonb := to_jsonb(NEW);\n    old_row jsonb := to_jsonb(OLD);\n    entity_id uuid;\nBEGIN\n    IF TG_TABLE_NAME = 'game_boards' THEN\n        affected_game_id := COALESCE(\n            (new_row ->> 'game_id')::uuid,\n            (old_row ->> 'game_id')::uuid\n        );\n    ELSIF TG_TABLE_NAME = 'game_team_slots' THEN\n        affected_game_id := COALESCE(\n            (new_row ->> 'game_id')::uuid,\n            (old_row ->> 'game_id')::uuid\n        );\n    ELSIF TG_TABLE_NAME = 'game_board_cells' THEN\n        entity_id := COALESCE(\n            (new_row ->> 'board_id')::uuid,\n            (old_row ->> 'board_id')::uuid\n        );\n        SELECT game_id INTO affected_game_id\n        FROM game_boards WHERE id = entity_id;\n    ELSE\n        entity_id := COALESCE(\n            (new_row ->> 'cell_id')::uuid,\n            (old_row ->> 'cell_id')::uuid\n        );\n        SELECT board.game_id INTO affected_game_id\n        FROM game_board_cells cell\n        JOIN game_boards board ON board.id = cell.board_id\n        WHERE cell.id = entity_id;\n    END IF;\n\n    SELECT status INTO game_status FROM games WHERE id = affected_game_id;\n    IF NOT FOUND OR game_status = 'draft' THEN\n        IF TG_OP = 'DELETE' THEN\n            RETURN OLD;\n        END IF;\n        RETURN NEW;\n    END IF;\n\n    IF TG_TABLE_NAME = 'game_boards'\n       AND TG_OP = 'UPDATE'\n       AND game_status IN ('active', 'finished')\n       AND (new_row ->> 'version')::integer = (old_row ->> 'version')::integer + 1\n       AND (new_row - ARRAY['version', 'updated_at_utc'])\n           = (old_row - ARRAY['version', 'updated_at_utc']) THEN\n        RETURN NEW;\n    END IF;\n\n    IF TG_TABLE_NAME = 'game_board_cells'\n       AND TG_OP = 'UPDATE'\n       AND game_status = 'active'\n       AND (\n           (old_row ->> 'state' = 'closed' AND new_row ->> 'state' = 'open')\n           OR (old_row ->> 'state' = 'open' AND new_row ->> 'state' = 'cancelled')\n       )\n       AND (new_row - 'state') = (old_row - 'state') THEN\n        RETURN NEW;\n    END IF;\n\n    RAISE EXCEPTION 'Published % row cannot be changed by % while game % is %.',\n        TG_TABLE_NAME, TG_OP, affected_game_id, game_status\n        USING ERRCODE = '55000';\nEND;\n$$;\n\nCREATE TRIGGER trg_game_boards_published_configuration\n    BEFORE INSERT OR UPDATE OR DELETE ON game_boards\n    FOR EACH ROW EXECUTE FUNCTION deadmans_protect_published_board_configuration();\nCREATE TRIGGER trg_game_board_cells_published_configuration\n    BEFORE INSERT OR UPDATE OR DELETE ON game_board_cells\n    FOR EACH ROW EXECUTE FUNCTION deadmans_protect_published_board_configuration();\nCREATE TRIGGER trg_game_board_cell_media_published_configuration\n    BEFORE INSERT OR UPDATE OR DELETE ON game_board_cell_media\n    FOR EACH ROW EXECUTE FUNCTION deadmans_protect_published_board_configuration();\nCREATE TRIGGER trg_game_team_slots_published_configuration\n    BEFORE INSERT OR UPDATE OR DELETE ON game_team_slots\n    FOR EACH ROW EXECUTE FUNCTION deadmans_protect_published_board_configuration();\n\nCREATE FUNCTION deadmans_protect_published_media_asset()\nRETURNS trigger\nLANGUAGE plpgsql\nSET search_path = public, pg_temp\nAS $$\nBEGIN\n    IF EXISTS (\n        SELECT 1\n        FROM game_board_cell_media link\n        JOIN game_board_cells cell ON cell.id = link.cell_id\n        JOIN game_boards board ON board.id = cell.board_id\n        JOIN games game ON game.id = board.game_id\n        WHERE link.media_asset_id = OLD.id\n          AND game.status <> 'draft'\n    ) THEN\n        RAISE EXCEPTION 'Media referenced by a published game is immutable.'\n            USING ERRCODE = '55000';\n    END IF;\n    IF TG_OP = 'DELETE' THEN\n        RETURN OLD;\n    END IF;\n    RETURN NEW;\nEND;\n$$;\n\nCREATE TRIGGER trg_media_assets_published_configuration\n    BEFORE UPDATE OR DELETE ON media_assets\n    FOR EACH ROW EXECUTE FUNCTION deadmans_protect_published_media_asset();\n\nCREATE FUNCTION deadmans_assert_round_origin(p_round_id uuid)\nRETURNS void\nLANGUAGE plpgsql\nSET search_path = public, pg_temp\nAS $$\nDECLARE\n    origin_round game_rounds%ROWTYPE;\nBEGIN\n    SELECT * INTO origin_round FROM game_rounds WHERE id = p_round_id;\n    IF NOT FOUND THEN RETURN; END IF;\n\n    IF NOT EXISTS (\n        SELECT 1\n        FROM games game\n        JOIN game_boards board\n          ON board.game_id = game.id AND board.id = origin_round.board_id\n        JOIN game_board_cells cell\n          ON cell.board_id = board.id AND cell.id = origin_round.board_cell_id\n        JOIN game_teams team\n          ON team.game_id = game.id AND team.id = origin_round.team_id\n        JOIN game_team_slots slot\n          ON slot.game_id = game.id AND slot.id = team.slot_id\n        WHERE game.id = origin_round.game_id\n          AND game.status = 'active'\n          AND game.is_deleted = FALSE\n          AND origin_round.created_at_utc >= game.started_at_utc\n          AND cell.state = 'open'\n          AND team.status = 'confirmed'\n          AND team.is_played = FALSE\n          AND origin_round.team_slot_index_snapshot = slot.slot_index\n          AND origin_round.cell_row_index = cell.row_index\n          AND origin_round.cell_col_index = cell.col_index\n          AND origin_round.cell_title_snapshot IS NOT DISTINCT FROM cell.title\n          AND origin_round.cell_description_snapshot IS NOT DISTINCT FROM cell.description\n          AND origin_round.cell_cost_snapshot = cell.cost\n          AND origin_round.base_score = cell.cost\n    ) THEN\n        RAISE EXCEPTION 'A round must originate from the active game and exact board/team snapshot.'\n            USING ERRCODE = '23514',\n                  CONSTRAINT = 'ck_game_rounds_origin_snapshot';\n    END IF;\nEND;\n$$;\n\nCREATE FUNCTION deadmans_validate_round_origin_trigger()\nRETURNS trigger\nLANGUAGE plpgsql\nSET search_path = public, pg_temp\nAS $$\nBEGIN\n    PERFORM deadmans_assert_round_origin(NEW.id);\n    RETURN NULL;\nEND;\n$$;\n\nCREATE CONSTRAINT TRIGGER trg_game_rounds_origin_consistency\n    AFTER INSERT ON game_rounds\n    DEFERRABLE INITIALLY DEFERRED\n    FOR EACH ROW EXECUTE FUNCTION deadmans_validate_round_origin_trigger();\n\nCREATE FUNCTION deadmans_assert_game_roster(p_game_id uuid)\nRETURNS void\nLANGUAGE plpgsql\nSET search_path = public, pg_temp\nAS $$\nDECLARE\n    roster_game games%ROWTYPE;\nBEGIN\n    SELECT * INTO roster_game FROM games WHERE id = p_game_id;\n    IF NOT FOUND OR roster_game.status NOT IN ('active', 'finished') THEN\n        RETURN;\n    END IF;\n\n    IF NOT EXISTS (\n        SELECT 1 FROM game_teams team\n        WHERE team.game_id = p_game_id AND team.status = 'confirmed'\n    )\n       OR EXISTS (\n           SELECT 1 FROM game_teams team\n           WHERE team.game_id = p_game_id AND team.status = 'forming'\n       )\n       OR EXISTS (\n           SELECT 1 FROM game_team_invitations invitation\n           WHERE invitation.game_id = p_game_id AND invitation.status = 'pending'\n       )\n       OR EXISTS (\n           SELECT 1 FROM game_teams team\n           WHERE team.game_id = p_game_id\n             AND team.status = 'confirmed'\n             AND team.disband_requested_at_utc IS NOT NULL\n       ) THEN\n        RAISE EXCEPTION 'An active game requires a settled confirmed roster.'\n            USING ERRCODE = '23514',\n                  CONSTRAINT = 'ck_games_active_roster_settled';\n    END IF;\n\n    IF EXISTS (\n        SELECT 1\n        FROM game_team_members member\n        JOIN game_teams team\n          ON team.game_id = member.game_id AND team.id = member.team_id\n        WHERE member.game_id = p_game_id\n          AND member.left_at_utc IS NULL\n          AND team.status <> 'confirmed'\n    ) THEN\n        RAISE EXCEPTION 'Only confirmed teams may retain active members in an active game.'\n            USING ERRCODE = '23514',\n                  CONSTRAINT = 'ck_games_active_roster_confirmed_members';\n    END IF;\n\n    IF EXISTS (\n        SELECT 1\n        FROM game_teams team\n        LEFT JOIN game_team_members member\n          ON member.game_id = team.game_id\n         AND member.team_id = team.id\n         AND member.left_at_utc IS NULL\n        WHERE team.game_id = p_game_id\n          AND team.status = 'confirmed'\n        GROUP BY team.id\n        HAVING count(member.id) < roster_game.min_players_per_team\n            OR count(member.id) > roster_game.max_players_per_team\n    ) THEN\n        RAISE EXCEPTION 'Every confirmed team must satisfy the game roster limits.'\n            USING ERRCODE = '23514',\n                  CONSTRAINT = 'ck_games_active_roster_size';\n    END IF;\nEND;\n$$;\n\nCREATE FUNCTION deadmans_validate_game_roster_trigger()\nRETURNS trigger\nLANGUAGE plpgsql\nSET search_path = public, pg_temp\nAS $$\nDECLARE\n    new_row jsonb := to_jsonb(NEW);\n    old_row jsonb := to_jsonb(OLD);\n    affected_game_id uuid;\nBEGIN\n    affected_game_id := CASE\n        WHEN TG_TABLE_NAME = 'games' THEN COALESCE(\n            (new_row ->> 'id')::uuid,\n            (old_row ->> 'id')::uuid\n        )\n        ELSE COALESCE(\n            (new_row ->> 'game_id')::uuid,\n            (old_row ->> 'game_id')::uuid\n        )\n    END;\n    IF affected_game_id IS NOT NULL THEN\n        PERFORM deadmans_assert_game_roster(affected_game_id);\n    END IF;\n    RETURN NULL;\nEND;\n$$;\n\nCREATE FUNCTION deadmans_protect_active_game_roster()\nRETURNS trigger\nLANGUAGE plpgsql\nSET search_path = public, pg_temp\nAS $$\nDECLARE\n    new_row jsonb := to_jsonb(NEW);\n    old_row jsonb := to_jsonb(OLD);\n    affected_game_id uuid := COALESCE(\n        (new_row ->> 'game_id')::uuid,\n        (old_row ->> 'game_id')::uuid\n    );\n    game_status text;\nBEGIN\n    SELECT status INTO game_status FROM games WHERE id = affected_game_id;\n    IF NOT FOUND OR game_status NOT IN ('active', 'finished') THEN\n        IF TG_OP = 'DELETE' THEN RETURN OLD; END IF;\n        RETURN NEW;\n    END IF;\n\n    IF TG_TABLE_NAME = 'game_teams'\n       AND TG_OP = 'UPDATE'\n       AND game_status = 'active'\n       AND (new_row - ARRAY['is_played', 'played_at_utc', 'updated_at_utc'])\n           = (old_row - ARRAY['is_played', 'played_at_utc', 'updated_at_utc']) THEN\n        RETURN NEW;\n    END IF;\n\n    RAISE EXCEPTION 'The roster of an active or finished game is immutable.'\n        USING ERRCODE = '55000';\nEND;\n$$;\n\nCREATE TRIGGER trg_game_teams_active_roster_immutable\n    BEFORE INSERT OR UPDATE OR DELETE ON game_teams\n    FOR EACH ROW EXECUTE FUNCTION deadmans_protect_active_game_roster();\nCREATE TRIGGER trg_game_team_members_active_roster_immutable\n    BEFORE INSERT OR UPDATE OR DELETE ON game_team_members\n    FOR EACH ROW EXECUTE FUNCTION deadmans_protect_active_game_roster();\nCREATE TRIGGER trg_game_team_invitations_active_roster_immutable\n    BEFORE INSERT OR UPDATE OR DELETE ON game_team_invitations\n    FOR EACH ROW EXECUTE FUNCTION deadmans_protect_active_game_roster();\n\nCREATE CONSTRAINT TRIGGER trg_games_roster_consistency\n    AFTER INSERT OR UPDATE ON games\n    DEFERRABLE INITIALLY DEFERRED\n    FOR EACH ROW EXECUTE FUNCTION deadmans_validate_game_roster_trigger();\nCREATE CONSTRAINT TRIGGER trg_game_teams_roster_consistency\n    AFTER INSERT OR UPDATE OR DELETE ON game_teams\n    DEFERRABLE INITIALLY DEFERRED\n    FOR EACH ROW EXECUTE FUNCTION deadmans_validate_game_roster_trigger();\nCREATE CONSTRAINT TRIGGER trg_game_team_members_roster_consistency\n    AFTER INSERT OR UPDATE OR DELETE ON game_team_members\n    DEFERRABLE INITIALLY DEFERRED\n    FOR EACH ROW EXECUTE FUNCTION deadmans_validate_game_roster_trigger();\nCREATE CONSTRAINT TRIGGER trg_game_team_invitations_roster_consistency\n    AFTER INSERT OR UPDATE OR DELETE ON game_team_invitations\n    DEFERRABLE INITIALLY DEFERRED\n    FOR EACH ROW EXECUTE FUNCTION deadmans_validate_game_roster_trigger();\n\nCREATE FUNCTION deadmans_assert_pending_team_invitation(\n    p_game_id uuid,\n    p_team_id uuid\n)\nRETURNS void\nLANGUAGE plpgsql\nSET search_path = public, pg_temp\nAS $$\nBEGIN\n    IF p_team_id IS NULL THEN RETURN; END IF;\n    IF EXISTS (\n        SELECT 1\n        FROM game_team_invitations invitation\n        JOIN game_teams team\n          ON team.game_id = invitation.game_id AND team.id = invitation.team_id\n        WHERE invitation.game_id = p_game_id\n          AND invitation.team_id = p_team_id\n          AND invitation.status = 'pending'\n          AND invitation.slot_id <> team.slot_id\n    ) THEN\n        RAISE EXCEPTION 'A pending team invitation must follow its team slot.'\n            USING ERRCODE = '23514',\n                  CONSTRAINT = 'ck_game_team_invitations_pending_team_slot';\n    END IF;\nEND;\n$$;\n\nCREATE FUNCTION deadmans_validate_pending_team_invitation_trigger()\nRETURNS trigger\nLANGUAGE plpgsql\nSET search_path = public, pg_temp\nAS $$\nDECLARE\n    new_row jsonb := to_jsonb(NEW);\n    old_row jsonb := to_jsonb(OLD);\n    new_game_id uuid;\n    new_team_id uuid;\n    old_game_id uuid;\n    old_team_id uuid;\nBEGIN\n    IF TG_TABLE_NAME = 'game_teams' THEN\n        PERFORM deadmans_assert_pending_team_invitation(\n            (new_row ->> 'game_id')::uuid,\n            (new_row ->> 'id')::uuid\n        );\n    ELSE\n        new_game_id := (new_row ->> 'game_id')::uuid;\n        new_team_id := (new_row ->> 'team_id')::uuid;\n        PERFORM deadmans_assert_pending_team_invitation(new_game_id, new_team_id);\n\n        IF TG_OP = 'UPDATE' THEN\n            old_game_id := (old_row ->> 'game_id')::uuid;\n            old_team_id := (old_row ->> 'team_id')::uuid;\n            IF ROW(old_game_id, old_team_id)\n               IS DISTINCT FROM ROW(new_game_id, new_team_id) THEN\n                PERFORM deadmans_assert_pending_team_invitation(\n                    old_game_id,\n                    old_team_id\n                );\n            END IF;\n        END IF;\n    END IF;\n    RETURN NULL;\nEND;\n$$;\n\nCREATE CONSTRAINT TRIGGER trg_game_teams_pending_invitation_slot\n    AFTER UPDATE ON game_teams\n    DEFERRABLE INITIALLY DEFERRED\n    FOR EACH ROW EXECUTE FUNCTION deadmans_validate_pending_team_invitation_trigger();\nCREATE CONSTRAINT TRIGGER trg_game_team_invitations_pending_team_slot\n    AFTER INSERT OR UPDATE ON game_team_invitations\n    DEFERRABLE INITIALLY DEFERRED\n    FOR EACH ROW EXECUTE FUNCTION deadmans_validate_pending_team_invitation_trigger();\n\nCREATE FUNCTION deadmans_assert_game_finalization(p_game_id uuid)\nRETURNS void\nLANGUAGE plpgsql\nSET search_path = public, pg_temp\nAS $$\nDECLARE\n    lifecycle_game games%ROWTYPE;\n    final_snapshot game_finalizations%ROWTYPE;\n    has_final_snapshot boolean;\n    completed_rounds bigint;\n    cancelled_rounds bigint;\n    skipped_quiz_rounds bigint;\n    result_rounds bigint;\n    result_kills bigint;\n    result_bounties bigint;\n    expected_quiz_points bigint;\nBEGIN\n    SELECT * INTO lifecycle_game FROM games WHERE id = p_game_id;\n    IF NOT FOUND THEN RETURN; END IF;\n\n    SELECT * INTO final_snapshot\n    FROM game_finalizations\n    WHERE game_id = p_game_id;\n    has_final_snapshot := FOUND;\n\n    IF lifecycle_game.status <> 'finished' THEN\n        IF has_final_snapshot THEN\n            RAISE EXCEPTION 'Only a finished game may have a finalization snapshot.'\n                USING ERRCODE = '23514',\n                      CONSTRAINT = 'ck_game_finalizations_finished_game_only';\n        END IF;\n        RETURN;\n    END IF;\n\n    IF NOT has_final_snapshot\n       OR final_snapshot.finished_at_utc IS DISTINCT FROM lifecycle_game.finished_at_utc THEN\n        RAISE EXCEPTION 'A finished game requires one timestamp-aligned finalization snapshot.'\n            USING ERRCODE = '23514',\n                  CONSTRAINT = 'ck_games_finalization_required';\n    END IF;\n\n    IF EXISTS (\n        SELECT 1 FROM game_rounds round\n        WHERE round.game_id = p_game_id\n          AND round.status IN (\n              'awaiting_modifiers', 'preparing', 'in_progress', 'reviewing_results'\n          )\n    ) OR EXISTS (\n        SELECT 1 FROM game_quiz_rounds quiz_round\n        WHERE quiz_round.game_id = p_game_id AND quiz_round.status = 'asked'\n    ) OR EXISTS (\n        SELECT 1 FROM game_modifier_activations activation\n        WHERE activation.game_id = p_game_id\n          AND activation.archived_at_utc IS NULL\n    ) THEN\n        RAISE EXCEPTION 'A finished game cannot retain open runtime state.'\n            USING ERRCODE = '23514',\n                  CONSTRAINT = 'ck_games_finished_runtime_settled';\n    END IF;\n\n    IF EXISTS (\n        SELECT 1\n        FROM game_teams team\n        LEFT JOIN game_team_final_results result\n          ON result.game_id = team.game_id AND result.team_id = team.id\n        WHERE team.game_id = p_game_id\n          AND team.status = 'confirmed'\n          AND result.team_id IS NULL\n    ) OR EXISTS (\n        SELECT 1\n        FROM game_team_final_results result\n        JOIN game_teams team\n          ON team.game_id = result.game_id AND team.id = result.team_id\n        WHERE result.game_id = p_game_id\n          AND team.status <> 'confirmed'\n    ) THEN\n        RAISE EXCEPTION 'Final results must cover exactly the confirmed game teams.'\n            USING ERRCODE = '23514',\n                  CONSTRAINT = 'ck_game_finalizations_complete_team_set';\n    END IF;\n\n    SELECT\n        count(*) FILTER (WHERE status = 'completed'),\n        count(*) FILTER (WHERE status = 'cancelled')\n    INTO completed_rounds, cancelled_rounds\n    FROM game_rounds\n    WHERE game_id = p_game_id;\n\n    SELECT count(*) INTO skipped_quiz_rounds\n    FROM game_quiz_rounds\n    WHERE game_id = p_game_id AND status = 'skipped';\n\n    SELECT\n        COALESCE(sum(rounds_played), 0),\n        COALESCE(sum(total_kills), 0),\n        COALESCE(sum(total_bounties), 0)\n    INTO result_rounds, result_kills, result_bounties\n    FROM game_team_final_results\n    WHERE game_id = p_game_id;\n\n    SELECT LEAST(\n        2147483647::bigint,\n        GREATEST(0::bigint, COALESCE(sum(points_delta), 0))\n    ) INTO expected_quiz_points\n    FROM game_quiz_point_ledger_entries\n    WHERE game_id = p_game_id\n      AND entry_type IN ('quiz_reward', 'manual_adjustment');\n\n    IF final_snapshot.completed_round_count <> completed_rounds\n       OR final_snapshot.cancelled_round_count <> cancelled_rounds\n       OR final_snapshot.skipped_quiz_question_count <> skipped_quiz_rounds\n       OR result_rounds <> completed_rounds\n       OR final_snapshot.total_kills <> result_kills\n       OR final_snapshot.total_bounties <> result_bounties\n       OR final_snapshot.quiz_total_points <> expected_quiz_points THEN\n        RAISE EXCEPTION 'The finalization aggregates do not match immutable game facts.'\n            USING ERRCODE = '23514',\n                  CONSTRAINT = 'ck_game_finalizations_aggregate_consistency';\n    END IF;\nEND;\n$$;\n\nCREATE FUNCTION deadmans_validate_game_finalization_trigger()\nRETURNS trigger\nLANGUAGE plpgsql\nSET search_path = public, pg_temp\nAS $$\nDECLARE\n    new_row jsonb := to_jsonb(NEW);\n    old_row jsonb := to_jsonb(OLD);\n    affected_game_id uuid;\nBEGIN\n    affected_game_id := CASE\n        WHEN TG_TABLE_NAME = 'games' THEN COALESCE(\n            (new_row ->> 'id')::uuid,\n            (old_row ->> 'id')::uuid\n        )\n        ELSE COALESCE(\n            (new_row ->> 'game_id')::uuid,\n            (old_row ->> 'game_id')::uuid\n        )\n    END;\n    IF affected_game_id IS NOT NULL THEN\n        PERFORM deadmans_assert_game_finalization(affected_game_id);\n    END IF;\n    RETURN NULL;\nEND;\n$$;\n\nCREATE CONSTRAINT TRIGGER trg_games_finalization_consistency\n    AFTER INSERT OR UPDATE ON games\n    DEFERRABLE INITIALLY DEFERRED\n    FOR EACH ROW EXECUTE FUNCTION deadmans_validate_game_finalization_trigger();\nCREATE CONSTRAINT TRIGGER trg_game_finalizations_consistency\n    AFTER INSERT ON game_finalizations\n    DEFERRABLE INITIALLY DEFERRED\n    FOR EACH ROW EXECUTE FUNCTION deadmans_validate_game_finalization_trigger();\nCREATE CONSTRAINT TRIGGER trg_game_team_final_results_consistency\n    AFTER INSERT ON game_team_final_results\n    DEFERRABLE INITIALLY DEFERRED\n    FOR EACH ROW EXECUTE FUNCTION deadmans_validate_game_finalization_trigger();\n\nCREATE FUNCTION deadmans_validate_modifier_activation_insert()\nRETURNS trigger\nLANGUAGE plpgsql\nSET search_path = public, pg_temp\nAS $$\nBEGIN\n    IF NOT EXISTS (\n        SELECT 1\n        FROM games game\n        JOIN game_rounds round\n          ON round.game_id = game.id AND round.id = NEW.round_id\n        JOIN game_enabled_modifiers enabled\n          ON enabled.game_id = game.id AND enabled.modifier_id = NEW.modifier_id\n        WHERE game.id = NEW.game_id\n          AND game.status = 'active'\n          AND game.is_deleted = FALSE\n          AND round.status = 'awaiting_modifiers'\n          AND NEW.activated_at_utc >= round.created_at_utc\n          AND enabled.emergency_disabled_at_utc IS NULL\n    ) THEN\n        RAISE EXCEPTION 'A modifier may only be activated for the open ordering phase of an active game.'\n            USING ERRCODE = '23514',\n                  CONSTRAINT = 'ck_game_modifier_activations_active_round';\n    END IF;\n    RETURN NEW;\nEND;\n$$;\n\nCREATE TRIGGER trg_game_modifier_activations_active_round\n    BEFORE INSERT ON game_modifier_activations\n    FOR EACH ROW EXECUTE FUNCTION deadmans_validate_modifier_activation_insert();\n\nCREATE FUNCTION deadmans_assert_modifier_activation(p_activation_id uuid)\nRETURNS void\nLANGUAGE plpgsql\nSET search_path = public, pg_temp\nAS $$\nDECLARE\n    activation game_modifier_activations%ROWTYPE;\n    purchase_count bigint;\n    refund_count bigint;\nBEGIN\n    SELECT * INTO activation\n    FROM game_modifier_activations\n    WHERE id = p_activation_id;\n    IF NOT FOUND THEN\n        RETURN;\n    END IF;\n\n    IF NOT EXISTS (\n        SELECT 1\n        FROM game_enabled_modifiers enabled\n        WHERE enabled.game_id = activation.game_id\n          AND enabled.modifier_id = activation.modifier_id\n          AND enabled.modifier_version_id = activation.modifier_version_id\n    ) THEN\n        RAISE EXCEPTION 'An activation must use the game-pinned modifier revision.'\n            USING ERRCODE = '23514',\n                  CONSTRAINT = 'ck_modifier_activations_enabled_version';\n    END IF;\n\n    IF EXISTS (\n        SELECT 1\n        FROM game_round_modifier_results result\n        WHERE result.modifier_activation_id = activation.id\n          AND ROW(\n              result.modifier_id,\n              result.definition_revision_snapshot,\n              result.modifier_name_snapshot,\n              result.modifier_description_snapshot,\n              result.modifier_category_snapshot,\n              result.modifier_activation_command_snapshot,\n              result.modifier_normalized_tags_snapshot,\n              result.modifier_behavior_v2_snapshot_json\n          ) IS DISTINCT FROM ROW(\n              activation.modifier_id,\n              activation.definition_revision_snapshot,\n              activation.modifier_name_snapshot,\n              activation.modifier_description_snapshot,\n              activation.modifier_category_snapshot,\n              activation.activation_command_snapshot,\n              activation.normalized_tags_snapshot,\n              activation.behavior_v2_snapshot_json\n          )\n    ) THEN\n        RAISE EXCEPTION 'Modifier result snapshots must match their activation.'\n            USING ERRCODE = '23514',\n                  CONSTRAINT = 'ck_modifier_results_activation_snapshot';\n    END IF;\n\n    SELECT count(*) FILTER (WHERE entry_type = 'modifier_purchase'),\n           count(*) FILTER (WHERE entry_type = 'modifier_refund')\n    INTO purchase_count, refund_count\n    FROM game_quiz_point_ledger_entries\n    WHERE modifier_activation_id = p_activation_id;\n\n    IF activation.activation_cost_snapshot > 0 THEN\n        IF purchase_count <> 1 OR NOT EXISTS (\n            SELECT 1 FROM game_quiz_point_ledger_entries purchase\n            WHERE purchase.modifier_activation_id = activation.id\n              AND purchase.entry_type = 'modifier_purchase'\n              AND purchase.game_id = activation.game_id\n              AND purchase.user_id = activation.activated_by_user_id\n              AND purchase.created_by_user_id = activation.initiated_by_user_id\n              AND purchase.points_delta = -activation.activation_cost_snapshot\n              AND purchase.occurred_at_utc = activation.activated_at_utc\n        ) THEN\n            RAISE EXCEPTION 'Paid modifier activation requires its purchase ledger entry.'\n                USING ERRCODE = '23514',\n                      CONSTRAINT = 'ck_modifier_activations_purchase_consistency';\n        END IF;\n    ELSIF purchase_count <> 0 THEN\n        RAISE EXCEPTION 'A free modifier activation cannot have a purchase entry.'\n            USING ERRCODE = '23514',\n                  CONSTRAINT = 'ck_modifier_activations_free_purchase';\n    END IF;\n\n    IF activation.refund_amount > 0 THEN\n        IF refund_count <> 1 OR NOT EXISTS (\n            SELECT 1 FROM game_quiz_point_ledger_entries refund\n            WHERE refund.modifier_activation_id = activation.id\n              AND refund.entry_type = 'modifier_refund'\n              AND refund.game_id = activation.game_id\n              AND refund.user_id = activation.activated_by_user_id\n              AND refund.created_by_user_id = activation.cancelled_by_user_id\n              AND refund.points_delta = activation.refund_amount\n              AND refund.occurred_at_utc = activation.cancelled_at_utc\n        ) THEN\n            RAISE EXCEPTION 'Refunded modifier activation requires its refund ledger entry.'\n                USING ERRCODE = '23514',\n                      CONSTRAINT = 'ck_modifier_activations_refund_consistency';\n        END IF;\n    ELSIF refund_count <> 0 THEN\n        RAISE EXCEPTION 'A non-refunded modifier activation cannot have a refund entry.'\n            USING ERRCODE = '23514',\n                  CONSTRAINT = 'ck_modifier_activations_unexpected_refund';\n    END IF;\nEND;\n$$;\n\nCREATE FUNCTION deadmans_validate_modifier_activation_trigger()\nRETURNS trigger\nLANGUAGE plpgsql\nSET search_path = public, pg_temp\nAS $$\nDECLARE\n    affected_activation_id uuid;\n    new_row jsonb := to_jsonb(NEW);\nBEGIN\n    IF TG_TABLE_NAME = 'game_modifier_activations' THEN\n        affected_activation_id := (new_row ->> 'id')::uuid;\n    ELSE\n        affected_activation_id := (new_row ->> 'modifier_activation_id')::uuid;\n    END IF;\n    IF affected_activation_id IS NOT NULL THEN\n        PERFORM deadmans_assert_modifier_activation(affected_activation_id);\n    END IF;\n    RETURN NULL;\nEND;\n$$;\n\nCREATE CONSTRAINT TRIGGER trg_game_modifier_activations_ledger_consistency\n    AFTER INSERT OR UPDATE ON game_modifier_activations\n    DEFERRABLE INITIALLY DEFERRED\n    FOR EACH ROW EXECUTE FUNCTION deadmans_validate_modifier_activation_trigger();\nCREATE CONSTRAINT TRIGGER trg_game_modifier_ledger_consistency\n    AFTER INSERT ON game_quiz_point_ledger_entries\n    DEFERRABLE INITIALLY DEFERRED\n    FOR EACH ROW\n    WHEN (NEW.modifier_activation_id IS NOT NULL)\n    EXECUTE FUNCTION deadmans_validate_modifier_activation_trigger();\nCREATE CONSTRAINT TRIGGER trg_game_modifier_results_activation_consistency\n    AFTER INSERT OR UPDATE ON game_round_modifier_results\n    DEFERRABLE INITIALLY DEFERRED\n    FOR EACH ROW EXECUTE FUNCTION deadmans_validate_modifier_activation_trigger();\n\nCREATE FUNCTION deadmans_assert_board_cell_bounds(p_board_id uuid)\nRETURNS void\nLANGUAGE plpgsql\nSET search_path = public, pg_temp\nAS $$\nBEGIN\n    IF EXISTS (\n        SELECT 1\n        FROM game_boards board\n        WHERE board.id = p_board_id\n          AND (\n              NOT deadmans_text_array_is_clean(\n                  board.row_labels, board.rows, board.rows, 100, TRUE, TRUE\n              )\n              OR NOT deadmans_text_array_is_clean(\n                  board.col_labels, board.cols, board.cols, 100, TRUE, TRUE\n              )\n          )\n    ) THEN\n        RAISE EXCEPTION 'Board labels must be unique, trimmed, nonblank and at most 100 characters.'\n            USING ERRCODE = '23514',\n                  CONSTRAINT = 'ck_game_boards_label_content';\n    END IF;\n\n    IF EXISTS (\n        SELECT 1\n        FROM game_board_cells cell\n        JOIN game_boards board ON board.id = cell.board_id\n        WHERE board.id = p_board_id\n          AND (cell.row_index >= board.rows OR cell.col_index >= board.cols)\n    ) THEN\n        RAISE EXCEPTION 'Board cells must remain inside their board dimensions.'\n            USING ERRCODE = '23514',\n                  CONSTRAINT = 'ck_game_board_cells_within_board_bounds';\n    END IF;\n\n    IF EXISTS (\n        SELECT 1\n        FROM game_boards board\n        WHERE board.id = p_board_id\n          AND (\n              SELECT count(*)\n              FROM game_board_cells cell\n              WHERE cell.board_id = board.id\n          ) <> board.rows::bigint * board.cols::bigint\n    ) THEN\n        RAISE EXCEPTION 'A board must contain exactly one cell for every coordinate.'\n            USING ERRCODE = '23514',\n                  CONSTRAINT = 'ck_game_board_cells_complete_grid';\n    END IF;\nEND;\n$$;\n\nCREATE FUNCTION deadmans_validate_board_cell_bounds_trigger()\nRETURNS trigger\nLANGUAGE plpgsql\nSET search_path = public, pg_temp\nAS $$\nDECLARE\n    new_row jsonb := to_jsonb(NEW);\n    old_row jsonb := to_jsonb(OLD);\n    new_board_id uuid;\n    old_board_id uuid;\nBEGIN\n    IF TG_TABLE_NAME = 'game_boards' THEN\n        new_board_id := (new_row ->> 'id')::uuid;\n    ELSIF TG_OP = 'DELETE' THEN\n        old_board_id := (old_row ->> 'board_id')::uuid;\n    ELSE\n        new_board_id := (new_row ->> 'board_id')::uuid;\n        IF TG_OP = 'UPDATE' THEN\n            old_board_id := (old_row ->> 'board_id')::uuid;\n        END IF;\n    END IF;\n\n    IF new_board_id IS NOT NULL THEN\n        PERFORM deadmans_assert_board_cell_bounds(new_board_id);\n    END IF;\n    IF old_board_id IS NOT NULL AND old_board_id IS DISTINCT FROM new_board_id THEN\n        PERFORM deadmans_assert_board_cell_bounds(old_board_id);\n    END IF;\n    RETURN NULL;\nEND;\n$$;\n\nCREATE CONSTRAINT TRIGGER trg_game_boards_cell_bounds\n    AFTER INSERT OR UPDATE ON game_boards\n    DEFERRABLE INITIALLY DEFERRED\n    FOR EACH ROW EXECUTE FUNCTION deadmans_validate_board_cell_bounds_trigger();\nCREATE CONSTRAINT TRIGGER trg_game_board_cells_board_bounds\n    AFTER INSERT OR UPDATE OR DELETE ON game_board_cells\n    DEFERRABLE INITIALLY DEFERRED\n    FOR EACH ROW EXECUTE FUNCTION deadmans_validate_board_cell_bounds_trigger();");
        migrationBuilder.Sql(
            """
            CREATE FUNCTION deadmans_protect_user_identity()
            RETURNS trigger
            LANGUAGE plpgsql
            SET search_path = public, pg_temp
            AS $$
            BEGIN
                IF TG_OP = 'DELETE' THEN
                    RAISE EXCEPTION 'Users must be deactivated instead of deleted.'
                        USING ERRCODE = '23514',
                              CONSTRAINT = 'ck_users_deactivate_only';
                END IF;

                IF OLD.twitch_user_id IS DISTINCT FROM NEW.twitch_user_id THEN
                    RAISE EXCEPTION 'A Twitch subject identifier is immutable.'
                        USING ERRCODE = '23514',
                              CONSTRAINT = 'ck_users_twitch_subject_immutable';
                END IF;

                RETURN NEW;
            END;
            $$;

            CREATE TRIGGER trg_users_twitch_subject_immutable
                BEFORE UPDATE OF twitch_user_id ON users
                FOR EACH ROW EXECUTE FUNCTION deadmans_protect_user_identity();
            CREATE TRIGGER trg_users_deactivate_only
                BEFORE DELETE ON users
                FOR EACH ROW EXECUTE FUNCTION deadmans_protect_user_identity();

            CREATE FUNCTION deadmans_validate_quiz_answer_principal()
            RETURNS trigger
            LANGUAGE plpgsql
            SET search_path = public, pg_temp
            AS $$
            DECLARE
                principal users%ROWTYPE;
            BEGIN
                SELECT * INTO principal
                FROM users
                WHERE id = NEW.awarded_to_user_id;

                IF NOT FOUND THEN
                    RETURN NEW;
                END IF;

                IF NOT principal.is_active
                   OR principal.twitch_user_id IS DISTINCT FROM NEW.twitch_user_id_snapshot
                   OR principal.login::text IS DISTINCT FROM NEW.login_snapshot
                   OR (
                       NEW.source_provider = 'twitch'
                       AND principal.display_name IS DISTINCT FROM NEW.display_name_snapshot
                   ) THEN
                    RAISE EXCEPTION 'A quiz winner snapshot must match its active Twitch principal.'
                        USING ERRCODE = '23514',
                              CONSTRAINT = 'ck_game_quiz_correct_answers_principal_snapshot';
                END IF;

                RETURN NEW;
            END;
            $$;

            CREATE TRIGGER trg_game_quiz_correct_answers_principal_snapshot
                BEFORE INSERT ON game_quiz_correct_answers
                FOR EACH ROW EXECUTE FUNCTION deadmans_validate_quiz_answer_principal();
            """
        );
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP FUNCTION IF EXISTS deadmans_validate_quiz_answer_principal() CASCADE;
            DROP FUNCTION IF EXISTS deadmans_protect_user_identity() CASCADE;
            """
        );
        migrationBuilder.Sql("DROP FUNCTION IF EXISTS deadmans_validate_modifier_result_update() CASCADE;\nDROP FUNCTION IF EXISTS deadmans_validate_modifier_activation_update() CASCADE;\nDROP FUNCTION IF EXISTS deadmans_validate_modifier_activation_trigger() CASCADE;\nDROP FUNCTION IF EXISTS deadmans_assert_modifier_activation(uuid) CASCADE;\nDROP FUNCTION IF EXISTS deadmans_validate_round_transition_audit_trigger() CASCADE;\nDROP FUNCTION IF EXISTS deadmans_assert_round_transition_audit(uuid) CASCADE;\nDROP FUNCTION IF EXISTS deadmans_validate_round_update() CASCADE;\nDROP FUNCTION IF EXISTS deadmans_validate_board_cell_bounds_trigger() CASCADE;\nDROP FUNCTION IF EXISTS deadmans_assert_board_cell_bounds(uuid) CASCADE;\nDROP FUNCTION IF EXISTS deadmans_assert_game_quiz_state() CASCADE;\nDROP FUNCTION IF EXISTS deadmans_validate_game_update() CASCADE;\nDROP FUNCTION IF EXISTS deadmans_protect_game_delete() CASCADE;\nDROP FUNCTION IF EXISTS deadmans_protect_published_board_configuration() CASCADE;\nDROP FUNCTION IF EXISTS deadmans_protect_published_media_asset() CASCADE;\nDROP FUNCTION IF EXISTS deadmans_protect_active_game_roster() CASCADE;\nDROP FUNCTION IF EXISTS deadmans_validate_game_roster_trigger() CASCADE;\nDROP FUNCTION IF EXISTS deadmans_assert_game_roster(uuid) CASCADE;\nDROP FUNCTION IF EXISTS deadmans_validate_pending_team_invitation_trigger() CASCADE;\nDROP FUNCTION IF EXISTS deadmans_assert_pending_team_invitation(uuid, uuid) CASCADE;\nDROP FUNCTION IF EXISTS deadmans_validate_game_finalization_trigger() CASCADE;\nDROP FUNCTION IF EXISTS deadmans_assert_game_finalization(uuid) CASCADE;\nDROP FUNCTION IF EXISTS deadmans_validate_round_origin_trigger() CASCADE;\nDROP FUNCTION IF EXISTS deadmans_assert_round_origin(uuid) CASCADE;\nDROP FUNCTION IF EXISTS deadmans_validate_modifier_activation_insert() CASCADE;\nDROP FUNCTION IF EXISTS deadmans_validate_quiz_round_update() CASCADE;\nDROP FUNCTION IF EXISTS deadmans_validate_quiz_round_trigger() CASCADE;\nDROP FUNCTION IF EXISTS deadmans_assert_quiz_round(uuid) CASCADE;\nDROP FUNCTION IF EXISTS deadmans_validate_modifier_catalog_trigger() CASCADE;\nDROP FUNCTION IF EXISTS deadmans_assert_modifier_catalog() CASCADE;\nDROP FUNCTION IF EXISTS deadmans_protect_published_game_configuration() CASCADE;\nDROP FUNCTION IF EXISTS deadmans_validate_game_publication_trigger() CASCADE;\nDROP FUNCTION IF EXISTS deadmans_assert_game_publication(uuid) CASCADE;\nDROP FUNCTION IF EXISTS deadmans_validate_question_answers_trigger() CASCADE;\nDROP FUNCTION IF EXISTS deadmans_assert_question_answers(uuid) CASCADE;\nDROP FUNCTION IF EXISTS deadmans_validate_ledger_insert() CASCADE;\nDROP FUNCTION IF EXISTS deadmans_reject_immutable_change() CASCADE;\nDROP FUNCTION IF EXISTS deadmans_text_array_is_clean(text[], integer, integer, integer, boolean, boolean) CASCADE;");
        migrationBuilder.DropForeignKey("fk_game_team_slots_games_game_id", "game_team_slots");
        migrationBuilder.DropForeignKey("fk_game_teams_games_game_id", "game_teams");
        migrationBuilder.DropForeignKey("fk_modifier_definitions_current_version", "modifier_definitions");
        migrationBuilder.DropTable("game_board_cell_media");
        migrationBuilder.DropTable("game_quiz_point_ledger_entries");
        migrationBuilder.DropTable("game_round_cell_media");
        migrationBuilder.DropTable("game_round_modifier_results");
        migrationBuilder.DropTable("game_round_participants");
        migrationBuilder.DropTable("game_round_transition_audits");
        migrationBuilder.DropTable("game_team_final_results");
        migrationBuilder.DropTable("game_team_invitations");
        migrationBuilder.DropTable("game_team_members");
        migrationBuilder.DropTable("game_user_notifications");
        migrationBuilder.DropTable("modifier_definition_version_conflicts");
        migrationBuilder.DropTable("question_accepted_answers");
        migrationBuilder.DropTable("user_roles");
        migrationBuilder.DropTable("media_assets");
        migrationBuilder.DropTable("game_quiz_correct_answers");
        migrationBuilder.DropTable("game_finalizations");
        migrationBuilder.DropTable("game_modifier_activations");
        migrationBuilder.DropTable("game_enabled_modifiers");
        migrationBuilder.DropTable("roles");
        migrationBuilder.DropTable("game_quiz_rounds");
        migrationBuilder.DropTable("game_enabled_questions");
        migrationBuilder.DropTable("game_rounds");
        migrationBuilder.DropTable("question_definitions");
        migrationBuilder.DropTable("game_board_cells");
        migrationBuilder.DropTable("question_categories");
        migrationBuilder.DropTable("game_boards");
        migrationBuilder.DropTable("games");
        migrationBuilder.DropTable("game_teams");
        migrationBuilder.DropTable("game_team_slots");
        migrationBuilder.DropTable("modifier_definition_versions");
        migrationBuilder.DropTable("modifier_definitions");
        migrationBuilder.DropTable("users");
    }

    protected override void BuildTargetModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "8.0.28").HasAnnotation("Relational:MaxIdentifierLength", 63);
        modelBuilder.HasPostgresExtension("citext");
        modelBuilder.HasPostgresExtension("pg_trgm");
        modelBuilder.UseIdentityByDefaultColumns();
        modelBuilder.Entity("backend.Data.Entities.BoardCell", delegate (EntityTypeBuilder b)
        {
            b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uuid")
                .HasColumnName("id");
            b.Property<Guid>("BoardId").HasColumnType("uuid").HasColumnName("board_id");
            b.Property<string>("CellType").IsRequired().HasMaxLength(32)
                .HasColumnType("character varying(32)")
                .HasColumnName("cell_type");
            b.Property<int>("ColIndex").HasColumnType("integer").HasColumnName("col_index");
            b.Property<int>("Cost").HasColumnType("integer").HasColumnName("cost");
            b.Property<string>("Description").HasMaxLength(2000).HasColumnType("character varying(2000)")
                .HasColumnName("description");
            b.Property<int>("RowIndex").HasColumnType("integer").HasColumnName("row_index");
            b.Property<string>("State").IsRequired().HasMaxLength(32)
                .HasColumnType("character varying(32)")
                .HasColumnName("state");
            b.Property<string>("Title").HasMaxLength(200).HasColumnType("character varying(200)")
                .HasColumnName("title");
            b.HasKey("Id").HasName("pk_game_board_cells");
            b.HasAlternateKey("BoardId", "Id").HasName("ak_game_board_cells_board_id_id");
            b.HasIndex("State").HasDatabaseName("ix_game_board_cells_state");
            b.HasIndex("BoardId", "RowIndex", "ColIndex").IsUnique().HasDatabaseName("ix_game_board_cells_board_id_row_index_col_index");
            b.ToTable("game_board_cells", null, delegate (TableBuilder t)
            {
                t.HasCheckConstraint("ck_game_board_cells_coordinates_non_negative", "row_index >= 0 AND col_index >= 0");
                t.HasCheckConstraint("ck_game_board_cells_cost_non_negative", "cost >= 0");
                t.HasCheckConstraint("ck_game_board_cells_state_allowed", "state IN ('open','closed','cancelled')");
                t.HasCheckConstraint("ck_game_board_cells_type_not_blank", "length(trim(cell_type)) > 0");
            });
        });
        modelBuilder.Entity("backend.Data.Entities.BoardCellMedia", delegate (EntityTypeBuilder b)
        {
            b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uuid")
                .HasColumnName("id");
            b.Property<Guid>("CellId").HasColumnType("uuid").HasColumnName("cell_id");
            b.Property<Guid>("MediaAssetId").HasColumnType("uuid").HasColumnName("media_asset_id");
            b.Property<string>("Role").IsRequired().HasMaxLength(32)
                .HasColumnType("character varying(32)")
                .HasColumnName("role");
            b.Property<int>("SortOrder").HasColumnType("integer").HasColumnName("sort_order");
            b.HasKey("Id").HasName("pk_game_board_cell_media");
            b.HasIndex("MediaAssetId").HasDatabaseName("ix_game_board_cell_media_media_asset_id");
            b.HasIndex("CellId", "SortOrder").IsUnique().HasDatabaseName("ix_game_board_cell_media_cell_id_sort_order");
            b.ToTable("game_board_cell_media", null, delegate (TableBuilder t)
            {
                t.HasCheckConstraint("ck_game_board_cell_media_role_not_blank", "length(trim(role)) > 0");
                t.HasCheckConstraint("ck_game_board_cell_media_sort_order_non_negative", "sort_order >= 0");
            });
        });
        modelBuilder.Entity("backend.Data.Entities.Game", delegate (EntityTypeBuilder b)
        {
            b.Property<Guid>("Id").HasColumnType("uuid").HasColumnName("id");
            b.Property<Guid?>("ActiveTeamId").HasColumnType("uuid").HasColumnName("active_team_id");
            b.Property<DateTime>("CreatedAtUtc").HasColumnType("timestamp with time zone").HasColumnName("created_at_utc");
            b.Property<DateTime?>("DeletedAtUtc").HasColumnType("timestamp with time zone").HasColumnName("deleted_at_utc");
            b.Property<string>("Description").HasMaxLength(2000).HasColumnType("character varying(2000)")
                .HasColumnName("description");
            b.Property<DateTime?>("FinishedAtUtc").HasColumnType("timestamp with time zone").HasColumnName("finished_at_utc");
            b.Property<bool>("IsDeleted").ValueGeneratedOnAdd().HasColumnType("boolean")
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            b.Property<short>("MaxPlayersPerTeam").ValueGeneratedOnAdd().HasColumnType("smallint")
                .HasDefaultValue((short)2)
                .HasColumnName("max_players_per_team");
            b.Property<short>("MinPlayersPerTeam").ValueGeneratedOnAdd().HasColumnType("smallint")
                .HasDefaultValue((short)1)
                .HasColumnName("min_players_per_team");
            b.Property<int>("QuizAnswerDurationSeconds").ValueGeneratedOnAdd().HasColumnType("integer")
                .HasDefaultValue(60)
                .HasColumnName("quiz_answer_duration_seconds");
            b.Property<DateTime?>("ReadyAtUtc").HasColumnType("timestamp with time zone").HasColumnName("ready_at_utc");
            b.Property<DateTime?>("StartedAtUtc").HasColumnType("timestamp with time zone").HasColumnName("started_at_utc");
            b.Property<string>("Status").IsRequired().HasMaxLength(32)
                .HasColumnType("character varying(32)")
                .HasColumnName("status");
            b.Property<string>("Title").IsRequired().HasMaxLength(200)
                .HasColumnType("character varying(200)")
                .HasColumnName("title");
            b.HasKey("Id").HasName("pk_games");
            b.HasIndex("CreatedAtUtc").HasDatabaseName("ix_games_created_at_utc");
            b.HasIndex("Id", "ActiveTeamId").HasDatabaseName("ix_games_active_team_same_game");
            b.HasIndex("IsDeleted", "Status", "CreatedAtUtc").HasDatabaseName("ix_games_is_deleted_status_created_at_utc");
            b.HasIndex(new string[1] { "IsDeleted" }, "ux_games_single_current").IsUnique().HasDatabaseName("ux_games_single_current")
                .HasFilter("is_deleted = FALSE AND status IN ('ready','active')");
            b.HasIndex(new string[1] { "IsDeleted" }, "ux_games_single_draft").IsUnique().HasDatabaseName("ux_games_single_draft")
                .HasFilter("is_deleted = FALSE AND status = 'draft'");
            b.ToTable("games", null, delegate (TableBuilder t)
            {
                t.HasCheckConstraint("ck_games_active_team_requires_active_game", "(active_team_id IS NULL) OR (status = 'active' AND is_deleted = FALSE)");
                t.HasCheckConstraint("ck_games_finished_at_semantics", "((status IN ('draft','ready','active')) AND finished_at_utc IS NULL) OR ((status = 'finished') AND finished_at_utc IS NOT NULL)");
                t.HasCheckConstraint("ck_games_lifecycle_timestamps", "((status = 'draft') AND ready_at_utc IS NULL AND started_at_utc IS NULL AND finished_at_utc IS NULL) OR ((status = 'ready') AND ready_at_utc IS NOT NULL AND started_at_utc IS NULL AND finished_at_utc IS NULL) OR ((status = 'active') AND ready_at_utc IS NOT NULL AND started_at_utc IS NOT NULL AND finished_at_utc IS NULL) OR ((status = 'finished') AND ready_at_utc IS NOT NULL AND started_at_utc IS NOT NULL AND finished_at_utc IS NOT NULL)");
                t.HasCheckConstraint("ck_games_quiz_answer_duration", "quiz_answer_duration_seconds BETWEEN 5 AND 3600");
                t.HasCheckConstraint("ck_games_soft_delete_semantics", "(is_deleted = FALSE AND deleted_at_utc IS NULL) OR (is_deleted = TRUE AND deleted_at_utc IS NOT NULL)");
                t.HasCheckConstraint("ck_games_status_allowed", "status IN ('draft','ready','active','finished')");
                t.HasCheckConstraint("ck_games_team_size_limits", "min_players_per_team > 0 AND max_players_per_team >= min_players_per_team");
                t.HasCheckConstraint("ck_games_timestamp_order", "(ready_at_utc IS NULL OR ready_at_utc >= created_at_utc) AND (started_at_utc IS NULL OR started_at_utc >= ready_at_utc) AND (finished_at_utc IS NULL OR finished_at_utc >= started_at_utc) AND (deleted_at_utc IS NULL OR deleted_at_utc >= created_at_utc)");
                t.HasCheckConstraint("ck_games_title_not_blank", "length(trim(title)) > 0");
            });
        });
        modelBuilder.Entity("backend.Data.Entities.GameBoard", delegate (EntityTypeBuilder b)
        {
            b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uuid")
                .HasColumnName("id");
            b.Property<string[]>("ColLabels").IsRequired().HasColumnType("text[]")
                .HasColumnName("col_labels");
            b.Property<int>("Cols").HasColumnType("integer").HasColumnName("cols");
            b.Property<DateTime>("CreatedAtUtc").HasColumnType("timestamp with time zone").HasColumnName("created_at_utc");
            b.Property<Guid>("GameId").HasColumnType("uuid").HasColumnName("game_id");
            b.Property<string[]>("RowLabels").IsRequired().HasColumnType("text[]")
                .HasColumnName("row_labels");
            b.Property<int>("Rows").HasColumnType("integer").HasColumnName("rows");
            b.Property<int>("Version").ValueGeneratedOnAdd().HasColumnType("integer")
                .HasDefaultValue(1)
                .HasColumnName("version");
            b.HasKey("Id").HasName("pk_game_boards");
            b.HasAlternateKey("GameId", "Id").HasName("ak_game_boards_game_id_id");
            b.HasIndex("GameId").IsUnique().HasDatabaseName("ix_game_boards_game_id");
            b.ToTable("game_boards", null, delegate (TableBuilder t)
            {
                t.HasCheckConstraint("ck_game_boards_dimensions_positive", "rows BETWEEN 1 AND 20 AND cols BETWEEN 1 AND 12");
                t.HasCheckConstraint("ck_game_boards_labels_match_dimensions", "cardinality(row_labels) = rows AND cardinality(col_labels) = cols");
                t.HasCheckConstraint("ck_game_boards_version_positive", "version > 0");
            });
        });
        modelBuilder.Entity("backend.Data.Entities.GameEnabledModifier", delegate (EntityTypeBuilder b)
        {
            b.Property<Guid>("GameId").HasColumnType("uuid").HasColumnName("game_id");
            b.Property<Guid>("ModifierId").HasColumnType("uuid").HasColumnName("modifier_id");
            b.Property<string>("EmergencyDisableReason").HasMaxLength(1000).HasColumnType("character varying(1000)")
                .HasColumnName("emergency_disable_reason");
            b.Property<DateTime?>("EmergencyDisabledAtUtc").HasColumnType("timestamp with time zone").HasColumnName("emergency_disabled_at_utc");
            b.Property<Guid?>("EmergencyDisabledByUserId").HasColumnType("uuid").HasColumnName("emergency_disabled_by_user_id");
            b.Property<DateTime>("EnabledAtUtc").HasColumnType("timestamp with time zone").HasColumnName("enabled_at_utc");
            b.Property<Guid?>("ModifierVersionId").HasColumnType("uuid").HasColumnName("modifier_version_id");
            b.Property<DateTime?>("VersionPinnedAtUtc").HasColumnType("timestamp with time zone").HasColumnName("version_pinned_at_utc");
            b.HasKey("GameId", "ModifierId").HasName("pk_game_enabled_modifiers");
            b.HasIndex("EmergencyDisabledByUserId").HasDatabaseName("ix_game_enabled_modifiers_emergency_disabled_by_user_id");
            b.HasIndex("ModifierId", "ModifierVersionId").HasDatabaseName("ix_game_enabled_modifiers_modifier_id_modifier_version_id");
            b.HasIndex("ModifierVersionId", "GameId").HasDatabaseName("ix_game_enabled_modifiers_modifier_version_id_game_id");
            b.ToTable("game_enabled_modifiers", null, delegate (TableBuilder t)
            {
                t.HasCheckConstraint("ck_game_enabled_modifiers_emergency_disable_audit", "(emergency_disabled_at_utc IS NULL AND emergency_disabled_by_user_id IS NULL AND emergency_disable_reason IS NULL) OR (emergency_disabled_at_utc IS NOT NULL AND emergency_disabled_by_user_id IS NOT NULL AND emergency_disable_reason IS NOT NULL AND length(btrim(emergency_disable_reason)) BETWEEN 1 AND 1000 AND emergency_disabled_at_utc >= enabled_at_utc)");
                t.HasCheckConstraint("ck_game_enabled_modifiers_version_pin_pair", "(modifier_version_id IS NULL AND version_pinned_at_utc IS NULL) OR (modifier_version_id IS NOT NULL AND version_pinned_at_utc IS NOT NULL AND version_pinned_at_utc >= enabled_at_utc)");
            });
        });
        modelBuilder.Entity("backend.Data.Entities.GameEnabledQuestion", delegate (EntityTypeBuilder b)
        {
            b.Property<Guid>("GameId").HasColumnType("uuid").HasColumnName("game_id");
            b.Property<Guid>("QuestionId").HasColumnType("uuid").HasColumnName("question_id");
            b.Property<string[]>("AcceptedAnswersSnapshot").IsRequired().HasColumnType("text[]")
                .HasColumnName("accepted_answers_snapshot");
            b.Property<string>("CategoryNameSnapshot").IsRequired().HasMaxLength(64)
                .HasColumnType("character varying(64)")
                .HasColumnName("category_name_snapshot");
            b.Property<DateTime>("EnabledAtUtc").HasColumnType("timestamp with time zone").HasColumnName("enabled_at_utc");
            b.Property<string[]>("NormalizedAnswersSnapshot").IsRequired().HasColumnType("text[]")
                .HasColumnName("normalized_answers_snapshot");
            b.Property<int>("PrioritySnapshot").HasColumnType("integer").HasColumnName("priority_snapshot");
            b.Property<string>("QuestionCodeSnapshot").IsRequired().HasMaxLength(64)
                .HasColumnType("character varying(64)")
                .HasColumnName("question_code_snapshot");
            b.Property<int>("QuestionRevisionSnapshot").HasColumnType("integer").HasColumnName("question_revision_snapshot");
            b.Property<string>("QuestionTextSnapshot").IsRequired().HasMaxLength(2000)
                .HasColumnType("character varying(2000)")
                .HasColumnName("question_text_snapshot");
            b.Property<int>("RewardSnapshot").HasColumnType("integer").HasColumnName("reward_snapshot");
            b.Property<DateTime>("SnapshotAtUtc").HasColumnType("timestamp with time zone").HasColumnName("snapshot_at_utc");
            b.HasKey("GameId", "QuestionId").HasName("pk_game_enabled_questions");
            b.HasIndex("QuestionId").HasDatabaseName("ix_game_enabled_questions_question_id");
            b.ToTable("game_enabled_questions", null, delegate (TableBuilder t)
            {
                t.HasCheckConstraint("ck_game_enabled_questions_answers_present", "cardinality(accepted_answers_snapshot) > 0 AND cardinality(accepted_answers_snapshot) = cardinality(normalized_answers_snapshot)");
                t.HasCheckConstraint("ck_game_enabled_questions_content_not_blank", "length(trim(question_code_snapshot)) > 0 AND length(trim(category_name_snapshot)) > 0 AND length(trim(question_text_snapshot)) > 0");
                t.HasCheckConstraint("ck_game_enabled_questions_revision_positive", "question_revision_snapshot > 0");
                t.HasCheckConstraint("ck_game_enabled_questions_reward_non_negative", "reward_snapshot >= 0");
            });
        });
        modelBuilder.Entity("backend.Data.Entities.GameFinalization", delegate (EntityTypeBuilder b)
        {
            b.Property<Guid>("GameId").HasColumnType("uuid").HasColumnName("game_id");
            b.Property<int>("CalculationVersion").HasColumnType("integer").HasColumnName("calculation_version");
            b.Property<int>("CancelledRoundCount").HasColumnType("integer").HasColumnName("cancelled_round_count");
            b.Property<int>("CompletedRoundCount").HasColumnType("integer").HasColumnName("completed_round_count");
            b.Property<DateTime>("FinishedAtUtc").HasColumnType("timestamp with time zone").HasColumnName("finished_at_utc");
            b.Property<string>("FinishedByDisplayNameSnapshot").IsRequired().HasMaxLength(128)
                .HasColumnType("character varying(128)")
                .HasColumnName("finished_by_display_name_snapshot");
            b.Property<Guid>("FinishedByUserId").HasColumnType("uuid").HasColumnName("finished_by_user_id");
            b.Property<string>("PublicNote").HasMaxLength(2000).HasColumnType("character varying(2000)")
                .HasColumnName("public_note");
            b.Property<int>("QuizTotalPoints").HasColumnType("integer").HasColumnName("quiz_total_points");
            b.Property<Guid>("RequestId").HasColumnType("uuid").HasColumnName("request_id");
            b.Property<int>("SkippedQuizQuestionCount").HasColumnType("integer").HasColumnName("skipped_quiz_question_count");
            b.Property<int>("TotalBounties").HasColumnType("integer").HasColumnName("total_bounties");
            b.Property<int>("TotalKills").HasColumnType("integer").HasColumnName("total_kills");
            b.HasKey("GameId").HasName("pk_game_finalizations");
            b.HasIndex("FinishedByUserId").HasDatabaseName("ix_game_finalizations_finished_by_user_id");
            b.HasIndex("RequestId").IsUnique().HasDatabaseName("ix_game_finalizations_request_id");
            b.ToTable("game_finalizations", null, delegate (TableBuilder t)
            {
                t.HasCheckConstraint("ck_game_finalizations_calculation_version_positive", "calculation_version > 0");
                t.HasCheckConstraint("ck_game_finalizations_counts_non_negative", "completed_round_count >= 0 AND cancelled_round_count >= 0 AND total_kills >= 0 AND total_bounties >= 0 AND quiz_total_points >= 0 AND skipped_quiz_question_count >= 0");
                t.HasCheckConstraint("ck_game_finalizations_display_name_not_blank", "length(trim(finished_by_display_name_snapshot)) > 0");
            });
        });
        modelBuilder.Entity("backend.Data.Entities.GameModifierActivation", delegate (EntityTypeBuilder b)
        {
            b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uuid")
                .HasColumnName("id");
            b.Property<DateTime>("ActivatedAtUtc").HasColumnType("timestamp with time zone").HasColumnName("activated_at_utc");
            b.Property<Guid>("ActivatedByUserId").HasColumnType("uuid").HasColumnName("activated_by_user_id");
            b.Property<string>("ActivationCommandSnapshot").HasMaxLength(128).HasColumnType("character varying(128)")
                .HasColumnName("activation_command_snapshot");
            b.Property<int>("ActivationCostSnapshot").HasColumnType("integer").HasColumnName("activation_cost_snapshot");
            b.Property<DateTime?>("ArchivedAtUtc").HasColumnType("timestamp with time zone").HasColumnName("archived_at_utc");
            b.Property<string>("BehaviorV2SnapshotJson").IsRequired().HasColumnType("jsonb")
                .HasColumnName("behavior_v2_snapshot_json");
            b.Property<string>("CancellationReason").HasMaxLength(1000).HasColumnType("character varying(1000)")
                .HasColumnName("cancellation_reason");
            b.Property<DateTime?>("CancelledAtUtc").HasColumnType("timestamp with time zone").HasColumnName("cancelled_at_utc");
            b.Property<Guid?>("CancelledByUserId").HasColumnType("uuid").HasColumnName("cancelled_by_user_id");
            b.Property<int>("DefinitionRevisionSnapshot").HasColumnType("integer").HasColumnName("definition_revision_snapshot");
            b.Property<Guid>("GameId").HasColumnType("uuid").HasColumnName("game_id");
            b.Property<Guid>("InitiatedByUserId").HasColumnType("uuid").HasColumnName("initiated_by_user_id");
            b.Property<string>("ModifierCategorySnapshot").IsRequired().HasMaxLength(32)
                .HasColumnType("character varying(32)")
                .HasColumnName("modifier_category_snapshot");
            b.Property<string>("ModifierDescriptionSnapshot").IsRequired().HasMaxLength(2000)
                .HasColumnType("character varying(2000)")
                .HasColumnName("modifier_description_snapshot");
            b.Property<string>("ModifierIconEmojiSnapshot").HasMaxLength(16).HasColumnType("character varying(16)")
                .HasColumnName("modifier_icon_emoji_snapshot");
            b.Property<Guid>("ModifierId").HasColumnType("uuid").HasColumnName("modifier_id");
            b.Property<string>("ModifierNameSnapshot").IsRequired().HasMaxLength(128)
                .HasColumnType("character varying(128)")
                .HasColumnName("modifier_name_snapshot");
            b.Property<Guid>("ModifierVersionId").HasColumnType("uuid").HasColumnName("modifier_version_id");
            b.Property<string[]>("NormalizedTagsSnapshot").IsRequired().HasColumnType("text[]")
                .HasColumnName("normalized_tags_snapshot");
            b.Property<int>("RefundAmount").HasColumnType("integer").HasColumnName("refund_amount");
            b.Property<Guid>("RoundId").HasColumnType("uuid").HasColumnName("round_id");
            b.Property<string>("Status").IsRequired().HasMaxLength(16)
                .HasColumnType("character varying(16)")
                .HasColumnName("status");
            b.HasKey("Id").HasName("pk_game_modifier_activations");
            b.HasAlternateKey("GameId", "Id").HasName("ak_game_modifier_activations_game_id_id");
            b.HasAlternateKey("RoundId", "Id", "ModifierId").HasName("ak_game_modifier_activations_round_id_id_modifier_id");
            b.HasIndex("CancelledByUserId").HasDatabaseName("ix_game_modifier_activations_cancelled_by_user_id");
            b.HasIndex("InitiatedByUserId").HasDatabaseName("ix_game_modifier_activations_initiated_by_user_id");
            b.HasIndex("ActivatedByUserId", "ActivatedAtUtc").HasDatabaseName("ix_game_modifier_activations_user_activated");
            b.HasIndex("GameId", "ActivatedAtUtc").HasDatabaseName("ix_game_modifier_activations_game_activated");
            b.HasIndex("GameId", "ArchivedAtUtc").HasDatabaseName("ix_game_modifier_activations_game_archived");
            b.HasIndex("GameId", "ModifierId").HasDatabaseName("ix_game_modifier_activations_game_modifier");
            b.HasIndex("GameId", "RoundId").HasDatabaseName("ix_game_modifier_activations_game_id_round_id");
            b.HasIndex("ModifierId", "ModifierVersionId").HasDatabaseName("ix_game_modifier_activations_modifier_id_modifier_version_id");
            b.HasIndex("ModifierVersionId", "GameId").HasDatabaseName("ix_game_modifier_activations_version_game");
            b.HasIndex("RoundId", "Status", "ActivatedAtUtc").HasDatabaseName("ix_game_modifier_activations_round_status_activated");
            b.ToTable("game_modifier_activations", null, delegate (TableBuilder t)
            {
                t.HasCheckConstraint("ck_game_modifier_activations_behavior_v2_schema", "jsonb_typeof(behavior_v2_snapshot_json) = 'object' AND behavior_v2_snapshot_json ->> 'schemaVersion' = '2'");
                t.HasCheckConstraint("ck_game_modifier_activations_cost_snapshot_non_negative", "activation_cost_snapshot >= 0");
                t.HasCheckConstraint("ck_game_modifier_activations_definition_revision_positive", "definition_revision_snapshot >= 1");
                t.HasCheckConstraint("ck_game_modifier_activations_lifecycle_semantics", "(status = 'active' AND archived_at_utc IS NULL AND cancelled_at_utc IS NULL AND cancelled_by_user_id IS NULL AND cancellation_reason IS NULL AND refund_amount = 0) OR (status = 'consumed' AND cancelled_at_utc IS NULL AND cancelled_by_user_id IS NULL AND cancellation_reason IS NULL AND refund_amount = 0) OR (status = 'cancelled' AND archived_at_utc IS NOT NULL AND cancelled_at_utc IS NOT NULL AND cancelled_by_user_id IS NOT NULL AND refund_amount = activation_cost_snapshot)");
                t.HasCheckConstraint("ck_game_modifier_activations_refund_range", "refund_amount >= 0 AND refund_amount <= activation_cost_snapshot");
                t.HasCheckConstraint("ck_game_modifier_activations_snapshot_not_blank", "length(trim(modifier_name_snapshot)) > 0 AND length(trim(modifier_description_snapshot)) > 0 AND length(trim(modifier_category_snapshot)) > 0");
                t.HasCheckConstraint("ck_game_modifier_activations_status_allowed", "status IN ('active','consumed','cancelled')");
                t.HasCheckConstraint("ck_game_modifier_activations_timestamp_order", "(archived_at_utc IS NULL OR archived_at_utc >= activated_at_utc) AND (cancelled_at_utc IS NULL OR (cancelled_at_utc >= activated_at_utc AND archived_at_utc = cancelled_at_utc))");
            });
        });
        modelBuilder.Entity("backend.Data.Entities.GameQuizCorrectAnswer", delegate (EntityTypeBuilder b)
        {
            b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uuid")
                .HasColumnName("id");
            b.Property<DateTime>("AnsweredAtUtc").HasColumnType("timestamp with time zone").HasColumnName("answered_at_utc");
            b.Property<Guid>("AwardedToUserId").HasColumnType("uuid").HasColumnName("awarded_to_user_id");
            b.Property<Guid?>("CapturedByUserId").HasColumnType("uuid").HasColumnName("captured_by_user_id");
            b.Property<string>("DisplayNameSnapshot").IsRequired().HasMaxLength(128)
                .HasColumnType("character varying(128)")
                .HasColumnName("display_name_snapshot");
            b.Property<Guid>("GameId").HasColumnType("uuid").HasColumnName("game_id");
            b.Property<string>("LoginSnapshot").IsRequired().HasMaxLength(64)
                .HasColumnType("character varying(64)")
                .HasColumnName("login_snapshot");
            b.Property<string>("NormalizedAnswer").IsRequired().HasMaxLength(500)
                .HasColumnType("character varying(500)")
                .HasColumnName("normalized_answer");
            b.Property<Guid>("QuizRoundId").HasColumnType("uuid").HasColumnName("quiz_round_id");
            b.Property<string>("SourceChannelId").HasMaxLength(128).HasColumnType("character varying(128)")
                .HasColumnName("source_channel_id");
            b.Property<string>("SourceMessageId").HasMaxLength(128).HasColumnType("character varying(128)")
                .HasColumnName("source_message_id");
            b.Property<string>("SourceProvider").IsRequired().HasMaxLength(32)
                .HasColumnType("character varying(32)")
                .HasColumnName("source_provider");
            b.Property<string>("SubmittedAnswer").IsRequired().HasMaxLength(500)
                .HasColumnType("character varying(500)")
                .HasColumnName("submitted_answer");
            b.Property<string>("TwitchUserIdSnapshot").IsRequired().HasMaxLength(64)
                .HasColumnType("character varying(64)")
                .HasColumnName("twitch_user_id_snapshot");
            b.HasKey("Id").HasName("pk_game_quiz_correct_answers");
            b.HasAlternateKey("GameId", "Id").HasName("ak_game_quiz_correct_answers_game_id_id");
            b.HasIndex("AwardedToUserId").HasDatabaseName("ix_game_quiz_correct_answers_awarded_to_user_id");
            b.HasIndex("CapturedByUserId").HasDatabaseName("ix_game_quiz_correct_answers_captured_by_user_id");
            b.HasIndex("QuizRoundId").IsUnique().HasDatabaseName("ix_game_quiz_correct_answers_quiz_round_id");
            b.HasIndex("GameId", "QuizRoundId").IsUnique().HasDatabaseName("ix_game_quiz_correct_answers_game_id_quiz_round_id");
            b.HasIndex("GameId", "AwardedToUserId", "AnsweredAtUtc").HasDatabaseName("ix_quiz_answers_game_user_time");
            b.HasIndex(new string[3] { "SourceProvider", "SourceChannelId", "SourceMessageId" }, "ux_game_quiz_correct_answers_source_message").IsUnique().HasDatabaseName("ux_game_quiz_correct_answers_source_message")
                .HasFilter("source_channel_id IS NOT NULL AND source_message_id IS NOT NULL");
            b.ToTable("game_quiz_correct_answers", null, delegate (TableBuilder t)
            {
                t.HasCheckConstraint("ck_game_quiz_correct_answers_answer_not_blank", "length(trim(submitted_answer)) > 0 AND length(trim(normalized_answer)) > 0");
                t.HasCheckConstraint("ck_game_quiz_correct_answers_identity_snapshots_not_blank", "length(trim(twitch_user_id_snapshot)) > 0 AND length(trim(login_snapshot)) > 0 AND length(trim(display_name_snapshot)) > 0");
                t.HasCheckConstraint("ck_game_quiz_correct_answers_source_allowed", "source_provider IN ('manual','twitch')");
                t.HasCheckConstraint("ck_game_quiz_correct_answers_source_semantics", "(source_provider = 'manual' AND source_channel_id IS NULL AND source_message_id IS NULL) OR (source_provider = 'twitch' AND source_channel_id IS NOT NULL AND source_message_id IS NOT NULL AND length(trim(source_channel_id)) > 0 AND length(trim(source_message_id)) > 0)");
            });
        });
        modelBuilder.Entity("backend.Data.Entities.GameQuizPointLedgerEntry", delegate (EntityTypeBuilder b)
        {
            b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uuid")
                .HasColumnName("id");
            b.Property<long>("AvailablePointsAfter").HasColumnType("bigint").HasColumnName("available_points_after");
            b.Property<long>("AvailablePointsBefore").HasColumnType("bigint").HasColumnName("available_points_before");
            b.Property<Guid?>("CorrectAnswerId").HasColumnType("uuid").HasColumnName("correct_answer_id");
            b.Property<Guid?>("CreatedByUserId").HasColumnType("uuid").HasColumnName("created_by_user_id");
            b.Property<string>("EntryType").IsRequired().HasMaxLength(32)
                .HasColumnType("character varying(32)")
                .HasColumnName("entry_type");
            b.Property<Guid>("GameId").HasColumnType("uuid").HasColumnName("game_id");
            b.Property<Guid?>("ManualRequestId").HasColumnType("uuid").HasColumnName("manual_request_id");
            b.Property<Guid?>("ModifierActivationId").HasColumnType("uuid").HasColumnName("modifier_activation_id");
            b.Property<DateTime>("OccurredAtUtc").HasColumnType("timestamp with time zone").HasColumnName("occurred_at_utc");
            b.Property<int>("PointsDelta").HasColumnType("integer").HasColumnName("points_delta");
            b.Property<string>("Reason").HasMaxLength(500).HasColumnType("character varying(500)")
                .HasColumnName("reason");
            b.Property<long>("SequenceNumber").ValueGeneratedOnAdd().HasColumnType("bigint")
                .HasColumnName("sequence_number");
            b.Property<long>("SequenceNumber").UseIdentityByDefaultColumn();
            b.Property<Guid>("UserId").HasColumnType("uuid").HasColumnName("user_id");
            b.HasKey("Id").HasName("pk_game_quiz_point_ledger_entries");
            b.HasIndex("CorrectAnswerId").IsUnique().HasDatabaseName("ix_game_quiz_point_ledger_entries_correct_answer_id")
                .HasFilter("correct_answer_id IS NOT NULL");
            b.HasIndex("CreatedByUserId").HasDatabaseName("ix_game_quiz_point_ledger_entries_created_by_user_id");
            b.HasIndex("ManualRequestId").IsUnique().HasDatabaseName("ix_game_quiz_point_ledger_entries_manual_request_id")
                .HasFilter("manual_request_id IS NOT NULL");
            b.HasIndex("SequenceNumber").IsUnique().HasDatabaseName("ix_game_quiz_point_ledger_entries_sequence_number");
            b.HasIndex("GameId", "CorrectAnswerId").HasDatabaseName("ix_game_quiz_point_ledger_entries_game_id_correct_answer_id");
            b.HasIndex("GameId", "ModifierActivationId").HasDatabaseName("ix_quiz_ledger_game_activation");
            b.HasIndex("UserId", "GameId").HasDatabaseName("ix_game_quiz_point_ledger_entries_user_id_game_id");
            b.HasIndex("GameId", "UserId", "SequenceNumber").HasDatabaseName("ix_quiz_ledger_game_user_sequence");
            b.HasIndex(new string[2] { "ModifierActivationId", "EntryType" }, "ux_quiz_point_ledger_modifier_event").IsUnique().HasDatabaseName("ux_quiz_point_ledger_modifier_event")
                .HasFilter("modifier_activation_id IS NOT NULL");
            b.ToTable("game_quiz_point_ledger_entries", null, delegate (TableBuilder t)
            {
                t.HasCheckConstraint("ck_quiz_point_ledger_balance_audit", "available_points_before >= 0 AND available_points_after >= 0 AND available_points_after = available_points_before + points_delta");
                t.HasCheckConstraint("ck_quiz_point_ledger_entry_type_allowed", "entry_type IN ('quiz_reward','manual_adjustment','modifier_purchase','modifier_refund')");
                t.HasCheckConstraint("ck_quiz_point_ledger_nonzero_delta", "points_delta <> 0");
                t.HasCheckConstraint("ck_quiz_point_ledger_source_semantics", "(entry_type = 'quiz_reward' AND points_delta > 0 AND correct_answer_id IS NOT NULL AND modifier_activation_id IS NULL AND manual_request_id IS NULL AND created_by_user_id IS NULL AND reason IS NULL) OR (entry_type = 'manual_adjustment' AND correct_answer_id IS NULL AND modifier_activation_id IS NULL AND manual_request_id IS NOT NULL AND created_by_user_id IS NOT NULL AND reason IS NOT NULL AND length(trim(reason)) BETWEEN 3 AND 500) OR (entry_type = 'modifier_purchase' AND points_delta < 0 AND correct_answer_id IS NULL AND modifier_activation_id IS NOT NULL AND manual_request_id IS NULL AND created_by_user_id IS NOT NULL AND reason IS NULL) OR (entry_type = 'modifier_refund' AND points_delta > 0 AND correct_answer_id IS NULL AND modifier_activation_id IS NOT NULL AND manual_request_id IS NULL AND created_by_user_id IS NOT NULL AND (reason IS NULL OR length(trim(reason)) BETWEEN 3 AND 500))");
            });
        });
        modelBuilder.Entity("backend.Data.Entities.GameQuizRound", delegate (EntityTypeBuilder b)
        {
            b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uuid")
                .HasColumnName("id");
            b.Property<string[]>("AcceptedAnswersSnapshot").IsRequired().HasColumnType("text[]")
                .HasColumnName("accepted_answers_snapshot");
            b.Property<int>("AskOrder").HasColumnType("integer").HasColumnName("ask_order");
            b.Property<DateTime>("AskedAtUtc").HasColumnType("timestamp with time zone").HasColumnName("asked_at_utc");
            b.Property<Guid?>("AskedByUserId").HasColumnType("uuid").HasColumnName("asked_by_user_id");
            b.Property<string>("CategoryNameSnapshot").IsRequired().HasMaxLength(64)
                .HasColumnType("character varying(64)")
                .HasColumnName("category_name_snapshot");
            b.Property<DateTime?>("ClosedAtUtc").HasColumnType("timestamp with time zone").HasColumnName("closed_at_utc");
            b.Property<DateTime>("ClosesAtUtc").HasColumnType("timestamp with time zone").HasColumnName("closes_at_utc");
            b.Property<string>("DeliveryKind").IsRequired().HasMaxLength(32)
                .HasColumnType("character varying(32)")
                .HasColumnName("delivery_kind");
            b.Property<Guid>("GameId").HasColumnType("uuid").HasColumnName("game_id");
            b.Property<string[]>("NormalizedAnswersSnapshot").IsRequired().HasColumnType("text[]")
                .HasColumnName("normalized_answers_snapshot");
            b.Property<string>("QuestionCodeSnapshot").IsRequired().HasMaxLength(64)
                .HasColumnType("character varying(64)")
                .HasColumnName("question_code_snapshot");
            b.Property<Guid>("QuestionId").HasColumnType("uuid").HasColumnName("question_id");
            b.Property<int>("QuestionRevisionSnapshot").HasColumnType("integer").HasColumnName("question_revision_snapshot");
            b.Property<string>("QuestionTextSnapshot").IsRequired().HasMaxLength(2000)
                .HasColumnType("character varying(2000)")
                .HasColumnName("question_text_snapshot");
            b.Property<int>("RewardSnapshot").HasColumnType("integer").HasColumnName("reward_snapshot");
            b.Property<string>("SourceChannelId").HasMaxLength(128).HasColumnType("character varying(128)")
                .HasColumnName("source_channel_id");
            b.Property<string>("SourceMessageId").HasMaxLength(128).HasColumnType("character varying(128)")
                .HasColumnName("source_message_id");
            b.Property<string>("Status").IsRequired().HasMaxLength(32)
                .HasColumnType("character varying(32)")
                .HasColumnName("status");
            b.HasKey("Id").HasName("pk_game_quiz_rounds");
            b.HasAlternateKey("GameId", "Id").HasName("ak_game_quiz_rounds_game_id_id");
            b.HasIndex("QuestionId").HasDatabaseName("ix_game_quiz_rounds_question_id");
            b.HasIndex("AskedByUserId", "AskedAtUtc").HasDatabaseName("ix_game_quiz_rounds_asked_by_user_id_asked_at_utc");
            b.HasIndex("GameId", "AskOrder").IsUnique().HasDatabaseName("ix_game_quiz_rounds_game_id_ask_order");
            b.HasIndex("GameId", "AskedAtUtc").HasDatabaseName("ix_game_quiz_rounds_game_id_asked_at_utc");
            b.HasIndex("GameId", "QuestionId").IsUnique().HasDatabaseName("ix_game_quiz_rounds_game_id_question_id");
            b.HasIndex("GameId", "Status").HasDatabaseName("ix_game_quiz_rounds_game_id_status");
            b.HasIndex(new string[1] { "GameId" }, "ux_game_quiz_rounds_one_open").IsUnique().HasDatabaseName("ux_game_quiz_rounds_one_open")
                .HasFilter("status = 'asked'");
            b.ToTable("game_quiz_rounds", null, delegate (TableBuilder t)
            {
                t.HasCheckConstraint("ck_game_quiz_rounds_ask_order_positive", "ask_order > 0");
                t.HasCheckConstraint("ck_game_quiz_rounds_close_semantics", "((status = 'asked') AND closed_at_utc IS NULL) OR ((status IN ('answered_correct','timeout','skipped')) AND closed_at_utc IS NOT NULL)");
                t.HasCheckConstraint("ck_game_quiz_rounds_delivery_kind_allowed", "delivery_kind IN ('manual','twitch')");
                t.HasCheckConstraint("ck_game_quiz_rounds_delivery_source_semantics", "(delivery_kind = 'manual' AND source_channel_id IS NULL AND source_message_id IS NULL) OR (delivery_kind = 'twitch' AND source_channel_id IS NOT NULL AND length(trim(source_channel_id)) > 0 AND (source_message_id IS NULL OR length(trim(source_message_id)) > 0))");
                t.HasCheckConstraint("ck_game_quiz_rounds_snapshot", "question_revision_snapshot > 0 AND reward_snapshot >= 0 AND length(trim(question_code_snapshot)) > 0 AND length(trim(category_name_snapshot)) > 0 AND length(trim(question_text_snapshot)) > 0 AND cardinality(accepted_answers_snapshot) > 0 AND cardinality(accepted_answers_snapshot) = cardinality(normalized_answers_snapshot)");
                t.HasCheckConstraint("ck_game_quiz_rounds_status_allowed", "status IN ('asked','answered_correct','timeout','skipped')");
                t.HasCheckConstraint("ck_game_quiz_rounds_window", "closes_at_utc > asked_at_utc AND (closed_at_utc IS NULL OR (closed_at_utc >= asked_at_utc AND closed_at_utc <= closes_at_utc))");
            });
        });
        modelBuilder.Entity("backend.Data.Entities.GameRound", delegate (EntityTypeBuilder b)
        {
            b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uuid")
                .HasColumnName("id");
            b.Property<int>("BaseScore").HasColumnType("integer").HasColumnName("base_score");
            b.Property<Guid>("BoardCellId").HasColumnType("uuid").HasColumnName("board_cell_id");
            b.Property<Guid>("BoardId").HasColumnType("uuid").HasColumnName("board_id");
            b.Property<int>("BountyCount").ValueGeneratedOnAdd().HasColumnType("integer")
                .HasDefaultValue(0)
                .HasColumnName("bounty_count");
            b.Property<int>("CellColIndex").HasColumnType("integer").HasColumnName("cell_col_index");
            b.Property<int>("CellCostSnapshot").HasColumnType("integer").HasColumnName("cell_cost_snapshot");
            b.Property<string>("CellDescriptionSnapshot").HasMaxLength(2000).HasColumnType("character varying(2000)")
                .HasColumnName("cell_description_snapshot");
            b.Property<int>("CellRowIndex").HasColumnType("integer").HasColumnName("cell_row_index");
            b.Property<string>("CellTitleSnapshot").HasMaxLength(200).HasColumnType("character varying(200)")
                .HasColumnName("cell_title_snapshot");
            b.Property<DateTime>("CreatedAtUtc").HasColumnType("timestamp with time zone").HasColumnName("created_at_utc");
            b.Property<bool>("EmptyCardPenaltyApplied").ValueGeneratedOnAdd().HasColumnType("boolean")
                .HasDefaultValue(false)
                .HasColumnName("empty_card_penalty_applied");
            b.Property<int?>("FinalScore").HasColumnType("integer").HasColumnName("final_score");
            b.Property<DateTime?>("FinishedAtUtc").HasColumnType("timestamp with time zone").HasColumnName("finished_at_utc");
            b.Property<Guid>("GameId").HasColumnType("uuid").HasColumnName("game_id");
            b.Property<DateTime?>("GameplayStartedAtUtc").HasColumnType("timestamp with time zone").HasColumnName("gameplay_started_at_utc");
            b.Property<string>("InternalCancellationDetail").HasMaxLength(2000).HasColumnType("character varying(2000)")
                .HasColumnName("internal_cancellation_detail");
            b.Property<int>("KillsCount").ValueGeneratedOnAdd().HasColumnType("integer")
                .HasDefaultValue(0)
                .HasColumnName("kills_count");
            b.Property<string>("Notes").HasMaxLength(2000).HasColumnType("character varying(2000)")
                .HasColumnName("notes");
            b.Property<DateTime?>("PreparedAtUtc").HasColumnType("timestamp with time zone").HasColumnName("prepared_at_utc");
            b.Property<string>("PublicCancellationSummary").HasMaxLength(500).HasColumnType("character varying(500)")
                .HasColumnName("public_cancellation_summary");
            b.Property<Guid?>("ResolvedByUserId").HasColumnType("uuid").HasColumnName("resolved_by_user_id");
            b.Property<DateTime?>("ReviewedAtUtc").HasColumnType("timestamp with time zone").HasColumnName("reviewed_at_utc");
            b.Property<string>("Status").IsRequired().HasMaxLength(32)
                .HasColumnType("character varying(32)")
                .HasColumnName("status");
            b.Property<Guid>("TeamId").HasColumnType("uuid").HasColumnName("team_id");
            b.Property<int>("TeamSlotIndexSnapshot").HasColumnType("integer").HasColumnName("team_slot_index_snapshot");
            b.Property<string>("TechnicalCancellationReasonCode").HasMaxLength(64).HasColumnType("character varying(64)")
                .HasColumnName("technical_cancellation_reason_code");
            b.Property<DateTime>("UpdatedAtUtc").HasColumnType("timestamp with time zone").HasColumnName("updated_at_utc");
            b.Property<int>("Version").ValueGeneratedOnAdd().HasColumnType("integer")
                .HasDefaultValue(1)
                .HasColumnName("version");
            b.HasKey("Id").HasName("pk_game_rounds");
            b.HasAlternateKey("GameId", "Id").HasName("ak_game_rounds_game_id_id");
            b.HasIndex("ResolvedByUserId").HasDatabaseName("ix_game_rounds_resolved_by_user_id");
            b.HasIndex("BoardCellId", "CreatedAtUtc").HasDatabaseName("ix_game_rounds_board_cell_id_created_at_utc");
            b.HasIndex("BoardId", "BoardCellId").HasDatabaseName("ix_game_rounds_board_id_board_cell_id");
            b.HasIndex("GameId", "BoardId").HasDatabaseName("ix_game_rounds_game_id_board_id");
            b.HasIndex("GameId", "CreatedAtUtc").HasDatabaseName("ix_game_rounds_game_id_created_at_utc");
            b.HasIndex("TeamId", "CreatedAtUtc").HasDatabaseName("ix_game_rounds_team_id_created_at_utc");
            b.HasIndex("GameId", "TeamId", "BoardCellId", "CreatedAtUtc").HasDatabaseName("ix_game_rounds_game_id_team_id_board_cell_id_created_at_utc");
            b.HasIndex(new string[2] { "GameId", "BoardCellId" }, "ux_game_rounds_one_effective_cell").IsUnique().HasDatabaseName("ux_game_rounds_one_effective_cell")
                .HasFilter("status <> 'cancelled'");
            b.HasIndex(new string[1] { "GameId" }, "ux_game_rounds_single_nonterminal_game").IsUnique().HasDatabaseName("ux_game_rounds_single_nonterminal_game")
                .HasFilter("status IN ('awaiting_modifiers','preparing','in_progress','reviewing_results')");
            b.ToTable("game_rounds", null, delegate (TableBuilder t)
            {
                t.HasCheckConstraint("ck_game_rounds_base_score_non_negative", "base_score >= 0");
                t.HasCheckConstraint("ck_game_rounds_bounty_count_non_negative", "bounty_count >= 0");
                t.HasCheckConstraint("ck_game_rounds_cell_cost_non_negative", "cell_cost_snapshot >= 0");
                t.HasCheckConstraint("ck_game_rounds_empty_card_penalty_semantics", "(empty_card_penalty_applied = false) OR (status = 'completed' AND final_score IS NOT NULL)");
                t.HasCheckConstraint("ck_game_rounds_finished_at_semantics", "((status IN ('awaiting_modifiers','preparing','in_progress','reviewing_results')) AND finished_at_utc IS NULL) OR ((status IN ('completed','cancelled')) AND finished_at_utc IS NOT NULL)");
                t.HasCheckConstraint("ck_game_rounds_kills_count_non_negative", "kills_count >= 0");
                t.HasCheckConstraint("ck_game_rounds_lifecycle_timestamps", "(status = 'awaiting_modifiers' AND prepared_at_utc IS NULL AND gameplay_started_at_utc IS NULL AND reviewed_at_utc IS NULL) OR (status = 'preparing' AND prepared_at_utc IS NOT NULL AND gameplay_started_at_utc IS NULL AND reviewed_at_utc IS NULL) OR (status = 'in_progress' AND prepared_at_utc IS NOT NULL AND gameplay_started_at_utc IS NOT NULL AND reviewed_at_utc IS NULL) OR (status = 'reviewing_results' AND prepared_at_utc IS NOT NULL AND gameplay_started_at_utc IS NOT NULL AND reviewed_at_utc IS NOT NULL) OR (status = 'completed' AND prepared_at_utc IS NOT NULL AND gameplay_started_at_utc IS NOT NULL AND reviewed_at_utc IS NOT NULL) OR (status = 'cancelled')");
                t.HasCheckConstraint("ck_game_rounds_resolution_semantics", "((status IN ('awaiting_modifiers','preparing','in_progress','reviewing_results')) AND final_score IS NULL AND resolved_by_user_id IS NULL) OR ((status = 'completed') AND final_score IS NOT NULL AND resolved_by_user_id IS NOT NULL) OR ((status = 'cancelled') AND final_score = 0 AND resolved_by_user_id IS NOT NULL)");
                t.HasCheckConstraint("ck_game_rounds_row_col_non_negative", "cell_row_index >= 0 AND cell_col_index >= 0");
                t.HasCheckConstraint("ck_game_rounds_status_allowed", "status IN ('awaiting_modifiers','preparing','in_progress','reviewing_results','completed','cancelled')");
                t.HasCheckConstraint("ck_game_rounds_team_slot_positive", "team_slot_index_snapshot > 0");
                t.HasCheckConstraint("ck_game_rounds_technical_cancellation_reason_allowed", "technical_cancellation_reason_code IS NULL OR technical_cancellation_reason_code IN ('external_game_failure','stream_or_infrastructure_failure','application_error','operator_error','other')");
                t.HasCheckConstraint("ck_game_rounds_technical_cancellation_semantics", "(status = 'cancelled' AND technical_cancellation_reason_code IS NOT NULL AND internal_cancellation_detail IS NOT NULL AND (technical_cancellation_reason_code <> 'other' OR public_cancellation_summary IS NOT NULL)) OR (status <> 'cancelled' AND technical_cancellation_reason_code IS NULL AND public_cancellation_summary IS NULL AND internal_cancellation_detail IS NULL)");
                t.HasCheckConstraint("ck_game_rounds_timestamp_order", "(prepared_at_utc IS NULL OR prepared_at_utc >= created_at_utc) AND (gameplay_started_at_utc IS NULL OR (prepared_at_utc IS NOT NULL AND gameplay_started_at_utc >= prepared_at_utc)) AND (reviewed_at_utc IS NULL OR (gameplay_started_at_utc IS NOT NULL AND reviewed_at_utc >= gameplay_started_at_utc)) AND (finished_at_utc IS NULL OR finished_at_utc >= created_at_utc) AND (finished_at_utc IS NULL OR prepared_at_utc IS NULL OR finished_at_utc >= prepared_at_utc) AND (finished_at_utc IS NULL OR gameplay_started_at_utc IS NULL OR finished_at_utc >= gameplay_started_at_utc) AND (finished_at_utc IS NULL OR reviewed_at_utc IS NULL OR finished_at_utc >= reviewed_at_utc) AND updated_at_utc >= created_at_utc");
                t.HasCheckConstraint("ck_game_rounds_version_positive", "version > 0");
            });
        });
        modelBuilder.Entity("backend.Data.Entities.GameRoundCellMedia", delegate (EntityTypeBuilder b)
        {
            b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uuid")
                .HasColumnName("id");
            b.Property<string>("Bucket").IsRequired().HasMaxLength(128)
                .HasColumnType("character varying(128)")
                .HasColumnName("bucket");
            b.Property<DateTime>("CreatedAtUtc").HasColumnType("timestamp with time zone").HasColumnName("created_at_utc");
            b.Property<string>("MimeType").IsRequired().HasMaxLength(256)
                .HasColumnType("character varying(256)")
                .HasColumnName("mime_type");
            b.Property<string>("ObjectKey").IsRequired().HasMaxLength(1024)
                .HasColumnType("character varying(1024)")
                .HasColumnName("object_key");
            b.Property<string>("Role").IsRequired().HasMaxLength(32)
                .HasColumnType("character varying(32)")
                .HasColumnName("role");
            b.Property<Guid>("RoundId").HasColumnType("uuid").HasColumnName("round_id");
            b.Property<long>("SizeBytes").HasColumnType("bigint").HasColumnName("size_bytes");
            b.Property<int>("SortOrder").HasColumnType("integer").HasColumnName("sort_order");
            b.HasKey("Id").HasName("pk_game_round_cell_media");
            b.HasIndex("RoundId", "SortOrder").IsUnique().HasDatabaseName("ux_game_round_cell_media_round_sort_order");
            b.ToTable("game_round_cell_media", null, delegate (TableBuilder t)
            {
                t.HasCheckConstraint("ck_game_round_cell_media_mime_type_not_blank", "length(trim(mime_type)) > 0");
                t.HasCheckConstraint("ck_game_round_cell_media_role_not_blank", "length(trim(role)) > 0");
                t.HasCheckConstraint("ck_game_round_cell_media_size_positive", "size_bytes > 0");
                t.HasCheckConstraint("ck_game_round_cell_media_sort_order_non_negative", "sort_order >= 0");
                t.HasCheckConstraint("ck_game_round_cell_media_storage_identity_not_blank", "length(trim(bucket)) > 0 AND length(trim(object_key)) > 0");
            });
        });
        modelBuilder.Entity("backend.Data.Entities.GameRoundModifierResult", delegate (EntityTypeBuilder b)
        {
            b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uuid")
                .HasColumnName("id");
            b.Property<string>("CalculationBreakdownJson").HasColumnType("jsonb").HasColumnName("calculation_breakdown_json");
            b.Property<DateTime>("CreatedAtUtc").HasColumnType("timestamp with time zone").HasColumnName("created_at_utc");
            b.Property<int>("DefinitionRevisionSnapshot").HasColumnType("integer").HasColumnName("definition_revision_snapshot");
            b.Property<Guid>("GameModifierActivationId").HasColumnType("uuid").HasColumnName("modifier_activation_id");
            b.Property<int>("KillDelta").HasColumnType("integer").HasColumnName("kill_delta");
            b.Property<string>("ModifierActivationCommandSnapshot").HasMaxLength(128).HasColumnType("character varying(128)")
                .HasColumnName("modifier_activation_command_snapshot");
            b.Property<string>("ModifierBehaviorV2SnapshotJson").IsRequired().HasColumnType("jsonb")
                .HasColumnName("modifier_behavior_v2_snapshot_json");
            b.Property<string>("ModifierCategorySnapshot").IsRequired().HasMaxLength(32)
                .HasColumnType("character varying(32)")
                .HasColumnName("modifier_category_snapshot");
            b.Property<string>("ModifierDescriptionSnapshot").IsRequired().HasMaxLength(2000)
                .HasColumnType("character varying(2000)")
                .HasColumnName("modifier_description_snapshot");
            b.Property<Guid>("ModifierId").HasColumnType("uuid").HasColumnName("modifier_id");
            b.Property<string>("ModifierNameSnapshot").IsRequired().HasMaxLength(128)
                .HasColumnType("character varying(128)")
                .HasColumnName("modifier_name_snapshot");
            b.Property<string[]>("ModifierNormalizedTagsSnapshot").IsRequired().HasColumnType("text[]")
                .HasColumnName("modifier_normalized_tags_snapshot");
            b.Property<decimal?>("MultiplierApplied").HasColumnType("numeric").HasColumnName("multiplier_applied");
            b.Property<string>("OutcomeStatus").IsRequired().HasMaxLength(32)
                .HasColumnType("character varying(32)")
                .HasColumnName("outcome_status");
            b.Property<string>("ResolutionDataJson").HasColumnType("jsonb").HasColumnName("resolution_data_json");
            b.Property<Guid?>("ResolutionGroupId").HasColumnType("uuid").HasColumnName("resolution_group_id");
            b.Property<string>("ResolutionKind").HasMaxLength(32).HasColumnType("character varying(32)")
                .HasColumnName("resolution_kind");
            b.Property<DateTime?>("ResolvedAtUtc").HasColumnType("timestamp with time zone").HasColumnName("resolved_at_utc");
            b.Property<Guid?>("ResolvedByUserId").HasColumnType("uuid").HasColumnName("resolved_by_user_id");
            b.Property<Guid>("RoundId").HasColumnType("uuid").HasColumnName("round_id");
            b.Property<int>("ScoreDelta").HasColumnType("integer").HasColumnName("score_delta");
            b.Property<DateTime>("UpdatedAtUtc").HasColumnType("timestamp with time zone").HasColumnName("updated_at_utc");
            b.Property<string>("ViolationComment").HasMaxLength(1000).HasColumnType("character varying(1000)")
                .HasColumnName("violation_comment");
            b.HasKey("Id").HasName("pk_game_round_modifier_results");
            b.HasIndex("ResolvedByUserId").HasDatabaseName("ix_game_round_modifier_results_resolved_by_user_id");
            b.HasIndex("ModifierId", "OutcomeStatus").HasDatabaseName("ix_game_round_modifier_results_modifier_status");
            b.HasIndex("RoundId", "GameModifierActivationId").IsUnique().HasDatabaseName("ux_game_round_modifier_results_round_activation");
            b.HasIndex("RoundId", "OutcomeStatus").HasDatabaseName("ix_game_round_modifier_results_round_status");
            b.HasIndex("RoundId", "GameModifierActivationId", "ModifierId").HasDatabaseName("ix_round_modifier_results_activation_fk");
            b.ToTable("game_round_modifier_results", null, delegate (TableBuilder t)
            {
                t.HasCheckConstraint("ck_game_round_modifier_results_behavior_v2_schema", "jsonb_typeof(modifier_behavior_v2_snapshot_json) = 'object' AND modifier_behavior_v2_snapshot_json ->> 'schemaVersion' = '2'");
                t.HasCheckConstraint("ck_game_round_modifier_results_definition_revision_positive", "definition_revision_snapshot >= 1");
                t.HasCheckConstraint("ck_game_round_modifier_results_json_objects", "(resolution_data_json IS NULL OR jsonb_typeof(resolution_data_json) = 'object') AND (calculation_breakdown_json IS NULL OR jsonb_typeof(calculation_breakdown_json) = 'object')");
                t.HasCheckConstraint("ck_game_round_modifier_results_resolution_semantics", "((outcome_status = 'pending') AND resolved_at_utc IS NULL AND resolved_by_user_id IS NULL) OR ((outcome_status <> 'pending') AND resolved_at_utc IS NOT NULL AND resolved_by_user_id IS NOT NULL)");
                t.HasCheckConstraint("ck_game_round_modifier_results_snapshot_not_blank", "length(trim(modifier_name_snapshot)) > 0 AND length(trim(modifier_description_snapshot)) > 0 AND length(trim(modifier_category_snapshot)) > 0");
                t.HasCheckConstraint("ck_game_round_modifier_results_status_allowed", "outcome_status IN ('pending','completed','failed','cancelled','violated','not_triggered','succeeded','not_succeeded','calculated')");
            });
        });
        modelBuilder.Entity("backend.Data.Entities.GameRoundParticipant", delegate (EntityTypeBuilder b)
        {
            b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uuid")
                .HasColumnName("id");
            b.Property<DateTime>("CreatedAtUtc").HasColumnType("timestamp with time zone").HasColumnName("created_at_utc");
            b.Property<string>("DisplayNameSnapshot").IsRequired().HasMaxLength(128)
                .HasColumnType("character varying(128)")
                .HasColumnName("display_name_snapshot");
            b.Property<Guid>("RoundId").HasColumnType("uuid").HasColumnName("round_id");
            b.Property<Guid>("UserId").HasColumnType("uuid").HasColumnName("user_id");
            b.HasKey("Id").HasName("pk_game_round_participants");
            b.HasIndex("RoundId", "UserId").IsUnique().HasDatabaseName("ux_game_round_participants_round_user");
            b.HasIndex("UserId", "CreatedAtUtc").HasDatabaseName("ix_game_round_participants_user_created");
            b.ToTable("game_round_participants", null, delegate (TableBuilder t)
            {
                t.HasCheckConstraint("ck_game_round_participants_display_name_not_blank", "length(trim(display_name_snapshot)) > 0");
            });
        });
        modelBuilder.Entity("backend.Data.Entities.GameRoundTransitionAudit", delegate (EntityTypeBuilder b)
        {
            b.Property<Guid>("RoundId").HasColumnType("uuid").HasColumnName("round_id");
            b.Property<int>("Sequence").HasColumnType("integer").HasColumnName("sequence");
            b.Property<string>("ActionCode").IsRequired().HasMaxLength(64)
                .HasColumnType("character varying(64)")
                .HasColumnName("action_code");
            b.Property<string>("FromStatus").HasMaxLength(32).HasColumnType("character varying(32)")
                .HasColumnName("from_status");
            b.Property<Guid>("InitiatedByUserId").HasColumnType("uuid").HasColumnName("initiated_by_user_id");
            b.Property<DateTime>("OccurredAtUtc").HasColumnType("timestamp with time zone").HasColumnName("occurred_at_utc");
            b.Property<string>("Reason").HasMaxLength(2000).HasColumnType("character varying(2000)")
                .HasColumnName("reason");
            b.Property<int>("ResultingRoundVersion").HasColumnType("integer").HasColumnName("resulting_round_version");
            b.Property<string>("ToStatus").IsRequired().HasMaxLength(32)
                .HasColumnType("character varying(32)")
                .HasColumnName("to_status");
            b.HasKey("RoundId", "Sequence").HasName("pk_game_round_transition_audits");
            b.HasIndex("InitiatedByUserId").HasDatabaseName("ix_game_round_transition_audits_initiated_by_user_id");
            b.HasIndex("RoundId", "ResultingRoundVersion").IsUnique().HasDatabaseName("ux_round_transition_version");
            b.ToTable("game_round_transition_audits", null, delegate (TableBuilder t)
            {
                t.HasCheckConstraint("ck_game_round_transition_audits_action_allowed", "action_code IN ('prepare','rebuild','begin_gameplay','review','resume_gameplay','finalize','technical_cancel')");
                t.HasCheckConstraint("ck_game_round_transition_audits_action_semantics", "(action_code = 'prepare' AND from_status = 'awaiting_modifiers' AND to_status = 'preparing') OR (action_code = 'rebuild' AND from_status = 'preparing' AND to_status = 'awaiting_modifiers') OR (action_code = 'begin_gameplay' AND from_status IN ('awaiting_modifiers','preparing') AND to_status = 'in_progress') OR (action_code = 'review' AND from_status = 'in_progress' AND to_status = 'reviewing_results') OR (action_code = 'resume_gameplay' AND from_status = 'reviewing_results' AND to_status = 'in_progress') OR (action_code = 'finalize' AND from_status = 'reviewing_results' AND to_status = 'completed') OR (action_code = 'technical_cancel' AND from_status IN ('awaiting_modifiers','preparing','in_progress','reviewing_results') AND to_status = 'cancelled')");
                t.HasCheckConstraint("ck_game_round_transition_audits_resulting_version_positive", "resulting_round_version > 0");
                t.HasCheckConstraint("ck_game_round_transition_audits_sequence_positive", "sequence > 0");
                t.HasCheckConstraint("ck_game_round_transition_audits_statuses_allowed", "(from_status IS NULL OR from_status IN ('awaiting_modifiers','preparing','in_progress','reviewing_results','completed','cancelled')) AND to_status IN ('awaiting_modifiers','preparing','in_progress','reviewing_results','completed','cancelled')");
            });
        });
        modelBuilder.Entity("backend.Data.Entities.GameTeam", delegate (EntityTypeBuilder b)
        {
            b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uuid")
                .HasColumnName("id");
            b.Property<DateTime?>("ConfirmedAtUtc").HasColumnType("timestamp with time zone").HasColumnName("confirmed_at_utc");
            b.Property<Guid?>("ConfirmedByUserId").HasColumnType("uuid").HasColumnName("confirmed_by_user_id");
            b.Property<DateTime>("CreatedAtUtc").HasColumnType("timestamp with time zone").HasColumnName("created_at_utc");
            b.Property<Guid?>("CreatedByUserId").HasColumnType("uuid").HasColumnName("created_by_user_id");
            b.Property<DateTime?>("DisbandRequestedAtUtc").HasColumnType("timestamp with time zone").HasColumnName("disband_requested_at_utc");
            b.Property<Guid?>("DisbandRequestedByUserId").HasColumnType("uuid").HasColumnName("disband_requested_by_user_id");
            b.Property<DateTime?>("DisbandedAtUtc").HasColumnType("timestamp with time zone").HasColumnName("disbanded_at_utc");
            b.Property<Guid?>("DisbandedByUserId").HasColumnType("uuid").HasColumnName("disbanded_by_user_id");
            b.Property<Guid>("GameId").HasColumnType("uuid").HasColumnName("game_id");
            b.Property<bool>("IsPlayed").ValueGeneratedOnAdd().HasColumnType("boolean")
                .HasDefaultValue(false)
                .HasColumnName("is_played");
            b.Property<string>("Name").HasMaxLength(48).HasColumnType("character varying(48)")
                .HasColumnName("name");
            b.Property<DateTime?>("PlayedAtUtc").HasColumnType("timestamp with time zone").HasColumnName("played_at_utc");
            b.Property<bool>("RecruitmentOpen").HasColumnType("boolean").HasColumnName("recruitment_open");
            b.Property<DateTime?>("RejectedAtUtc").HasColumnType("timestamp with time zone").HasColumnName("rejected_at_utc");
            b.Property<Guid?>("RejectedByUserId").HasColumnType("uuid").HasColumnName("rejected_by_user_id");
            b.Property<Guid>("SlotId").HasColumnType("uuid").HasColumnName("slot_id");
            b.Property<string>("Status").IsRequired().HasMaxLength(32)
                .HasColumnType("character varying(32)")
                .HasColumnName("status");
            b.Property<DateTime>("UpdatedAtUtc").HasColumnType("timestamp with time zone").HasColumnName("updated_at_utc");
            b.HasKey("Id").HasName("pk_game_teams");
            b.HasAlternateKey("GameId", "Id").HasName("ak_game_teams_game_id_id");
            b.HasIndex("ConfirmedByUserId").HasDatabaseName("ix_game_teams_confirmed_by_user_id");
            b.HasIndex("CreatedByUserId").HasDatabaseName("ix_game_teams_created_by_user_id");
            b.HasIndex("DisbandRequestedByUserId").HasDatabaseName("ix_game_teams_disband_requested_by_user_id");
            b.HasIndex("DisbandedByUserId").HasDatabaseName("ix_game_teams_disbanded_by_user_id");
            b.HasIndex("RejectedByUserId").HasDatabaseName("ix_game_teams_rejected_by_user_id");
            b.HasIndex("GameId", "SlotId").HasDatabaseName("ix_game_teams_game_slot");
            b.HasIndex("GameId", "Status").HasDatabaseName("ix_game_teams_game_id_status");
            b.HasIndex(new string[1] { "SlotId" }, "ux_game_teams_active_slot").IsUnique().HasDatabaseName("ux_game_teams_active_slot")
                .HasFilter("status IN ('forming','confirmed')");
            b.ToTable("game_teams", null, delegate (TableBuilder t)
            {
                t.HasCheckConstraint("ck_game_teams_content_and_timestamps", "(name IS NULL OR length(trim(name)) > 0) AND updated_at_utc >= created_at_utc AND (played_at_utc IS NULL OR played_at_utc >= created_at_utc) AND (confirmed_at_utc IS NULL OR confirmed_at_utc >= created_at_utc) AND (rejected_at_utc IS NULL OR rejected_at_utc >= created_at_utc) AND (disbanded_at_utc IS NULL OR disbanded_at_utc >= created_at_utc) AND (disband_requested_at_utc IS NULL OR disband_requested_at_utc >= created_at_utc)");
                t.HasCheckConstraint("ck_game_teams_disband_request_user_pair", "(disband_requested_at_utc IS NULL AND disband_requested_by_user_id IS NULL) OR (disband_requested_at_utc IS NOT NULL AND disband_requested_by_user_id IS NOT NULL)");
                t.HasCheckConstraint("ck_game_teams_played_timestamp_semantics", "(is_played = true AND played_at_utc IS NOT NULL) OR (is_played = false AND played_at_utc IS NULL)");
                t.HasCheckConstraint("ck_game_teams_status_allowed", "status IN ('forming','confirmed','rejected','disbanded')");
                t.HasCheckConstraint("ck_game_teams_status_timestamp_semantics", "((status = 'forming') AND confirmed_at_utc IS NULL AND rejected_at_utc IS NULL AND disbanded_at_utc IS NULL AND disband_requested_at_utc IS NULL) OR ((status = 'confirmed') AND confirmed_at_utc IS NOT NULL AND confirmed_by_user_id IS NOT NULL AND rejected_at_utc IS NULL AND disbanded_at_utc IS NULL) OR ((status = 'rejected') AND rejected_at_utc IS NOT NULL AND rejected_by_user_id IS NOT NULL AND disbanded_at_utc IS NULL AND disband_requested_at_utc IS NULL) OR ((status = 'disbanded') AND disbanded_at_utc IS NOT NULL AND disbanded_by_user_id IS NOT NULL AND disband_requested_at_utc IS NULL)");
                t.HasCheckConstraint("ck_game_teams_terminal_recruitment_closed", "status NOT IN ('rejected','disbanded') OR recruitment_open = FALSE");
            });
        });
        modelBuilder.Entity("backend.Data.Entities.GameTeamFinalResult", delegate (EntityTypeBuilder b)
        {
            b.Property<Guid>("GameId").HasColumnType("uuid").HasColumnName("game_id");
            b.Property<Guid>("TeamId").HasColumnType("uuid").HasColumnName("team_id");
            b.Property<int?>("BestScore").HasColumnType("integer").HasColumnName("best_score");
            b.Property<int?>("FinalScore").HasColumnType("integer").HasColumnName("final_score");
            b.Property<DateTime?>("LastFinishedAtUtc").HasColumnType("timestamp with time zone").HasColumnName("last_finished_at_utc");
            b.Property<string[]>("ParticipantNamesSnapshot").IsRequired().HasColumnType("text[]")
                .HasColumnName("participant_names_snapshot");
            b.Property<int>("PenaltyTotal").HasColumnType("integer").HasColumnName("penalty_total");
            b.Property<int?>("Placement").HasColumnType("integer").HasColumnName("placement");
            b.Property<int>("RoundsPlayed").HasColumnType("integer").HasColumnName("rounds_played");
            b.Property<string>("TeamNameSnapshot").HasMaxLength(128).HasColumnType("character varying(128)")
                .HasColumnName("team_name_snapshot");
            b.Property<int>("TeamSlotIndexSnapshot").HasColumnType("integer").HasColumnName("team_slot_index_snapshot");
            b.Property<int>("TotalBonusDelta").HasColumnType("integer").HasColumnName("total_bonus_delta");
            b.Property<int>("TotalBounties").HasColumnType("integer").HasColumnName("total_bounties");
            b.Property<int>("TotalKills").HasColumnType("integer").HasColumnName("total_kills");
            b.Property<int>("TotalScore").HasColumnType("integer").HasColumnName("total_score");
            b.HasKey("GameId", "TeamId").HasName("pk_game_team_final_results");
            b.HasIndex("GameId", "Placement").HasDatabaseName("ix_game_team_final_results_game_id_placement");
            b.ToTable("game_team_final_results", null, delegate (TableBuilder t)
            {
                t.HasCheckConstraint("ck_game_team_final_results_rounds_non_negative", "rounds_played >= 0 AND penalty_total >= 0 AND total_kills >= 0 AND total_bounties >= 0");
                t.HasCheckConstraint("ck_game_team_final_results_team_slot_positive", "team_slot_index_snapshot > 0");
                t.HasCheckConstraint("ck_game_team_final_results_unplayed_semantics", "(rounds_played = 0 AND best_score IS NULL AND final_score IS NULL AND placement IS NULL AND last_finished_at_utc IS NULL) OR (rounds_played > 0 AND best_score IS NOT NULL AND final_score IS NOT NULL AND placement IS NOT NULL AND placement > 0 AND last_finished_at_utc IS NOT NULL)");
            });
        });
        modelBuilder.Entity("backend.Data.Entities.GameTeamInvitation", delegate (EntityTypeBuilder b)
        {
            b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uuid")
                .HasColumnName("id");
            b.Property<DateTime>("CreatedAtUtc").HasColumnType("timestamp with time zone").HasColumnName("created_at_utc");
            b.Property<Guid>("GameId").HasColumnType("uuid").HasColumnName("game_id");
            b.Property<string>("InvitedByKind").IsRequired().HasMaxLength(16)
                .HasColumnType("character varying(16)")
                .HasColumnName("invited_by_kind");
            b.Property<Guid>("InvitedByUserId").HasColumnType("uuid").HasColumnName("invited_by_user_id");
            b.Property<Guid>("InvitedUserId").HasColumnType("uuid").HasColumnName("invited_user_id");
            b.Property<DateTime?>("RespondedAtUtc").HasColumnType("timestamp with time zone").HasColumnName("responded_at_utc");
            b.Property<Guid>("SlotId").HasColumnType("uuid").HasColumnName("slot_id");
            b.Property<string>("Status").IsRequired().HasMaxLength(16)
                .HasColumnType("character varying(16)")
                .HasColumnName("status");
            b.Property<Guid?>("TeamId").HasColumnType("uuid").HasColumnName("team_id");
            b.HasKey("Id").HasName("pk_game_team_invitations");
            b.HasIndex("InvitedByUserId").HasDatabaseName("ix_game_team_invitations_invited_by_user_id");
            b.HasIndex("GameId", "InvitedUserId").IsUnique().HasDatabaseName("ux_game_team_invitations_one_pending_per_user")
                .HasFilter("status = 'pending'");
            b.HasIndex("GameId", "SlotId").HasDatabaseName("ix_game_team_invitations_game_slot");
            b.HasIndex("GameId", "Status").HasDatabaseName("ix_game_team_invitations_game_id_status");
            b.HasIndex("InvitedUserId", "Status").HasDatabaseName("ix_game_team_invitations_invited_user_id_status");
            b.HasIndex("GameId", "TeamId").HasDatabaseName("ix_game_team_invitations_game_team");
            b.ToTable("game_team_invitations", null, delegate (TableBuilder t)
            {
                t.HasCheckConstraint("ck_game_team_invitations_invited_by_kind", "invited_by_kind IN ('admin','member')");
                t.HasCheckConstraint("ck_game_team_invitations_response_timestamp_semantics", "((status = 'pending') AND responded_at_utc IS NULL) OR ((status <> 'pending') AND responded_at_utc IS NOT NULL AND responded_at_utc >= created_at_utc)");
                t.HasCheckConstraint("ck_game_team_invitations_source_team_semantics", "invited_by_kind = 'admin' OR team_id IS NOT NULL");
                t.HasCheckConstraint("ck_game_team_invitations_status", "status IN ('pending','accepted','declined','cancelled','expired')");
            });
        });
        modelBuilder.Entity("backend.Data.Entities.GameTeamMember", delegate (EntityTypeBuilder b)
        {
            b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uuid")
                .HasColumnName("id");
            b.Property<Guid>("GameId").HasColumnType("uuid").HasColumnName("game_id");
            b.Property<DateTime>("JoinedAtUtc").HasColumnType("timestamp with time zone").HasColumnName("joined_at_utc");
            b.Property<DateTime?>("LeftAtUtc").HasColumnType("timestamp with time zone").HasColumnName("left_at_utc");
            b.Property<Guid>("TeamId").HasColumnType("uuid").HasColumnName("team_id");
            b.Property<Guid>("UserId").HasColumnType("uuid").HasColumnName("user_id");
            b.HasKey("Id").HasName("pk_game_team_members");
            b.HasIndex("UserId").HasDatabaseName("ix_game_team_members_user_id");
            b.HasIndex("GameId", "TeamId").HasDatabaseName("ix_game_team_members_game_id_team_id");
            b.HasIndex("TeamId", "UserId").HasDatabaseName("ix_game_team_members_team_id_user_id");
            b.HasIndex(new string[2] { "GameId", "UserId" }, "ux_game_team_members_active_game_user").IsUnique().HasDatabaseName("ux_game_team_members_active_game_user")
                .HasFilter("left_at_utc IS NULL");
            b.HasIndex(new string[2] { "TeamId", "UserId" }, "ux_game_team_members_active_team_user").IsUnique().HasDatabaseName("ux_game_team_members_active_team_user")
                .HasFilter("left_at_utc IS NULL");
            b.ToTable("game_team_members", null, delegate (TableBuilder t)
            {
                t.HasCheckConstraint("ck_game_team_members_left_after_join", "left_at_utc IS NULL OR left_at_utc >= joined_at_utc");
            });
        });
        modelBuilder.Entity("backend.Data.Entities.GameTeamSlot", delegate (EntityTypeBuilder b)
        {
            b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uuid")
                .HasColumnName("id");
            b.Property<DateTime>("CreatedAtUtc").HasColumnType("timestamp with time zone").HasColumnName("created_at_utc");
            b.Property<Guid>("GameId").HasColumnType("uuid").HasColumnName("game_id");
            b.Property<string>("ReservedLabel").HasMaxLength(200).HasColumnType("character varying(200)")
                .HasColumnName("reserved_label");
            b.Property<int>("SlotIndex").HasColumnType("integer").HasColumnName("slot_index");
            b.Property<string>("SlotType").IsRequired().HasMaxLength(16)
                .HasColumnType("character varying(16)")
                .HasColumnName("slot_type");
            b.HasKey("Id").HasName("pk_game_team_slots");
            b.HasAlternateKey("GameId", "Id").HasName("ak_game_team_slots_game_id_id");
            b.HasIndex("GameId", "SlotIndex").IsUnique().HasDatabaseName("ix_game_team_slots_game_id_slot_index");
            b.HasIndex("GameId", "SlotType").HasDatabaseName("ix_game_team_slots_game_id_slot_type");
            b.ToTable("game_team_slots", null, delegate (TableBuilder t)
            {
                t.HasCheckConstraint("ck_game_team_slots_reserved_label_semantics", "(slot_type = 'public' AND reserved_label IS NULL) OR (slot_type = 'reserved' AND reserved_label IS NOT NULL AND length(trim(reserved_label)) > 0)");
                t.HasCheckConstraint("ck_game_team_slots_slot_index_positive", "slot_index > 0");
                t.HasCheckConstraint("ck_game_team_slots_slot_type", "slot_type IN ('public','reserved')");
            });
        });
        modelBuilder.Entity("backend.Data.Entities.GameUserNotification", delegate (EntityTypeBuilder b)
        {
            b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uuid")
                .HasColumnName("id");
            b.Property<DateTime>("CreatedAtUtc").HasColumnType("timestamp with time zone").HasColumnName("created_at_utc");
            b.Property<string>("DeduplicationKey").IsRequired().HasMaxLength(160)
                .HasColumnType("character varying(160)")
                .HasColumnName("deduplication_key");
            b.Property<Guid>("GameId").HasColumnType("uuid").HasColumnName("game_id");
            b.Property<string>("PayloadJson").IsRequired().HasColumnType("jsonb")
                .HasColumnName("payload_json");
            b.Property<DateTime?>("ReadAtUtc").HasColumnType("timestamp with time zone").HasColumnName("read_at_utc");
            b.Property<int>("SchemaVersion").HasColumnType("integer").HasColumnName("schema_version");
            b.Property<string>("Type").IsRequired().HasMaxLength(64)
                .HasColumnType("character varying(64)")
                .HasColumnName("type");
            b.Property<Guid>("UserId").HasColumnType("uuid").HasColumnName("user_id");
            b.HasKey("Id").HasName("pk_game_user_notifications");
            b.HasIndex("GameId").HasDatabaseName("ix_game_user_notifications_game_id");
            b.HasIndex("UserId", "ReadAtUtc", "CreatedAtUtc").HasDatabaseName("ix_game_user_notifications_user_id_read_at_utc_created_at_utc");
            b.HasIndex("UserId", "Type", "CreatedAtUtc").HasDatabaseName("ix_game_user_notifications_user_id_type_created_at_utc");
            b.HasIndex(new string[2] { "UserId", "DeduplicationKey" }, "ux_game_user_notifications_deduplication").IsUnique().HasDatabaseName("ux_game_user_notifications_deduplication");
            b.ToTable("game_user_notifications", null, delegate (TableBuilder t)
            {
                t.HasCheckConstraint("ck_game_user_notifications_identity_not_blank", "length(trim(type)) > 0 AND length(trim(deduplication_key)) > 0");
                t.HasCheckConstraint("ck_game_user_notifications_modifier_cancelled_v1_payload", "type <> 'modifier_cancelled' OR (schema_version = 1 AND jsonb_typeof(payload_json -> 'modifierActivationId') = 'string' AND length(trim(payload_json ->> 'modifierActivationId')) > 0 AND jsonb_typeof(payload_json -> 'modifierName') = 'string' AND length(trim(payload_json ->> 'modifierName')) > 0 AND jsonb_typeof(payload_json -> 'actorDisplayName') = 'string' AND length(trim(payload_json ->> 'actorDisplayName')) > 0 AND jsonb_typeof(payload_json -> 'quizPointsDelta') = 'number' AND (payload_json ->> 'quizPointsDelta')::integer >= 0)");
                t.HasCheckConstraint("ck_game_user_notifications_payload_envelope", "schema_version > 0 AND jsonb_typeof(payload_json) = 'object'");
                t.HasCheckConstraint("ck_game_user_notifications_read_after_create", "read_at_utc IS NULL OR read_at_utc >= created_at_utc");
            });
        });
        modelBuilder.Entity("backend.Data.Entities.MediaAsset", delegate (EntityTypeBuilder b)
        {
            b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uuid")
                .HasColumnName("id");
            b.Property<string>("Bucket").IsRequired().HasMaxLength(128)
                .HasColumnType("character varying(128)")
                .HasColumnName("bucket");
            b.Property<DateTime>("CreatedAtUtc").HasColumnType("timestamp with time zone").HasColumnName("created_at_utc");
            b.Property<string>("MimeType").IsRequired().HasMaxLength(256)
                .HasColumnType("character varying(256)")
                .HasColumnName("mime_type");
            b.Property<string>("ObjectKey").IsRequired().HasMaxLength(1024)
                .HasColumnType("character varying(1024)")
                .HasColumnName("object_key");
            b.Property<long>("SizeBytes").HasColumnType("bigint").HasColumnName("size_bytes");
            b.HasKey("Id").HasName("pk_media_assets");
            b.HasIndex("Bucket", "ObjectKey").IsUnique().HasDatabaseName("ix_media_assets_bucket_object_key");
            b.ToTable("media_assets", null, delegate (TableBuilder t)
            {
                t.HasCheckConstraint("ck_media_assets_mime_type_not_blank", "length(trim(mime_type)) > 0");
                t.HasCheckConstraint("ck_media_assets_size_positive", "size_bytes > 0");
                t.HasCheckConstraint("ck_media_assets_storage_identity_not_blank", "length(trim(bucket)) > 0 AND length(trim(object_key)) > 0");
            });
        });
        modelBuilder.Entity("backend.Data.Entities.ModifierDefinition", delegate (EntityTypeBuilder b)
        {
            b.Property<Guid>("Id").HasColumnType("uuid").HasColumnName("id");
            b.Property<DateTime?>("ArchivedAtUtc").HasColumnType("timestamp with time zone").HasColumnName("archived_at_utc");
            b.Property<Guid?>("ArchivedByUserId").HasColumnType("uuid").HasColumnName("archived_by_user_id");
            b.Property<DateTime>("CreatedAtUtc").HasColumnType("timestamp with time zone").HasColumnName("created_at_utc");
            b.Property<Guid?>("CreatedByUserId").HasColumnType("uuid").HasColumnName("created_by_user_id");
            b.Property<Guid?>("CurrentVersionId").HasColumnType("uuid").HasColumnName("current_version_id");
            b.Property<bool>("IsArchived").ValueGeneratedOnAdd().HasColumnType("boolean")
                .HasDefaultValue(false)
                .HasColumnName("is_archived");
            b.HasKey("Id").HasName("pk_modifier_definitions");
            b.HasIndex("ArchivedByUserId").HasDatabaseName("ix_modifier_definitions_archived_by_user_id");
            b.HasIndex("CreatedByUserId").HasDatabaseName("ix_modifier_definitions_created_by_user_id");
            b.HasIndex("CurrentVersionId").IsUnique().HasDatabaseName("ix_modifier_definitions_current_version_id");
            b.HasIndex("CreatedAtUtc", "Id").IsDescending().HasDatabaseName("ix_modifier_definitions_created_at_utc_id");
            b.HasIndex("Id", "CurrentVersionId").HasDatabaseName("ix_modifier_definitions_id_current_version_id");
            b.HasIndex("IsArchived", "CreatedAtUtc", "Id").IsDescending(false, true, true).HasDatabaseName("ix_modifier_definitions_is_archived_created_at_utc_id");
            b.ToTable("modifier_definitions", null, delegate (TableBuilder t)
            {
                t.HasCheckConstraint("ck_modifier_definitions_archive_semantics", "(is_archived = FALSE AND archived_at_utc IS NULL AND archived_by_user_id IS NULL) OR (is_archived = TRUE AND archived_at_utc IS NOT NULL AND archived_by_user_id IS NOT NULL AND archived_at_utc >= created_at_utc)");
            });
        });
        modelBuilder.Entity("backend.Data.Entities.ModifierDefinitionVersion", delegate (EntityTypeBuilder b)
        {
            b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uuid")
                .HasColumnName("id");
            b.Property<string>("ActivationCommand").HasMaxLength(128).HasColumnType("character varying(128)")
                .HasColumnName("activation_command");
            b.Property<int>("ActivationCost").HasColumnType("integer").HasColumnName("activation_cost");
            b.Property<string>("BehaviorV2Json").IsRequired().HasColumnType("jsonb")
                .HasColumnName("behavior_v2_json");
            b.Property<Guid?>("CascadeSourceModifierId").HasColumnType("uuid").HasColumnName("cascade_source_modifier_id");
            b.Property<string>("Category").IsRequired().HasMaxLength(32)
                .HasColumnType("character varying(32)")
                .HasColumnName("category");
            b.Property<string>("ChangeNote").HasMaxLength(500).HasColumnType("character varying(500)")
                .HasColumnName("change_note");
            b.Property<string>("ChangeType").IsRequired().HasMaxLength(32)
                .HasColumnType("character varying(32)")
                .HasColumnName("change_type");
            b.Property<string[]>("ChangedFields").IsRequired().HasColumnType("text[]")
                .HasColumnName("changed_fields");
            b.Property<DateTime>("CreatedAtUtc").HasColumnType("timestamp with time zone").HasColumnName("created_at_utc");
            b.Property<string>("CreatedByDisplayNameSnapshot").IsRequired().HasMaxLength(128)
                .HasColumnType("character varying(128)")
                .HasColumnName("created_by_display_name_snapshot");
            b.Property<Guid?>("CreatedByUserId").HasColumnType("uuid").HasColumnName("created_by_user_id");
            b.Property<string>("Description").IsRequired().HasMaxLength(2000)
                .HasColumnType("character varying(2000)")
                .HasColumnName("description");
            b.Property<string>("IconEmoji").HasMaxLength(16).HasColumnType("character varying(16)")
                .HasColumnName("icon_emoji");
            b.Property<int?>("MaxActivationsPerRound").HasColumnType("integer").HasColumnName("max_activations_per_round");
            b.Property<Guid>("ModifierId").HasColumnType("uuid").HasColumnName("modifier_id");
            b.Property<string>("Name").IsRequired().HasMaxLength(128)
                .HasColumnType("character varying(128)")
                .HasColumnName("name");
            b.Property<string[]>("NormalizedTags").IsRequired().HasColumnType("text[]")
                .HasColumnName("normalized_tags");
            b.Property<int>("Revision").HasColumnType("integer").HasColumnName("revision");
            b.HasKey("Id").HasName("pk_modifier_definition_versions");
            b.HasAlternateKey("ModifierId", "Id").HasName("ak_modifier_definition_versions_modifier_id_id");
            b.HasIndex("CascadeSourceModifierId").HasDatabaseName("ix_modifier_definition_versions_cascade_source_modifier_id");
            b.HasIndex("CreatedByUserId").HasDatabaseName("ix_modifier_definition_versions_created_by_user_id");
            b.HasIndex("ModifierId", "Revision").IsUnique().HasDatabaseName("ix_modifier_definition_versions_modifier_id_revision");
            b.HasIndex("ModifierId", "CreatedAtUtc", "Id").HasDatabaseName("ix_modifier_definition_versions_modifier_id_created_at_utc_id");
            b.HasIndex(new string[1] { "Category" }, "ix_modifier_versions_category_trgm").HasDatabaseName("ix_modifier_versions_category_trgm");
            b.HasIndex(new string[1] { "Category" }, "ix_modifier_versions_category_trgm").HasMethod("gin");
            b.HasIndex(new string[1] { "Category" }, "ix_modifier_versions_category_trgm").HasOperators("gin_trgm_ops");
            b.HasIndex(new string[1] { "Name" }, "ix_modifier_versions_name_trgm").HasDatabaseName("ix_modifier_versions_name_trgm");
            b.HasIndex(new string[1] { "Name" }, "ix_modifier_versions_name_trgm").HasMethod("gin");
            b.HasIndex(new string[1] { "Name" }, "ix_modifier_versions_name_trgm").HasOperators("gin_trgm_ops");
            b.ToTable("modifier_definition_versions", null, delegate (TableBuilder t)
            {
                t.HasCheckConstraint("ck_modifier_definition_versions_behavior_v2_schema", "jsonb_typeof(behavior_v2_json) = 'object' AND behavior_v2_json ->> 'schemaVersion' = '2'");
                t.HasCheckConstraint("ck_modifier_definition_versions_category_allowed", "category IN ('preparation','round','result')");
                t.HasCheckConstraint("ck_modifier_definition_versions_change_note", "change_note IS NULL OR length(btrim(change_note)) BETWEEN 1 AND 500");
                t.HasCheckConstraint("ck_modifier_definition_versions_change_type", "change_type IN ('created','edited','compatibility_cascade','migration_baseline')");
                t.HasCheckConstraint("ck_modifier_definition_versions_content_not_blank", "length(btrim(name)) > 0 AND length(btrim(description)) > 0 AND length(btrim(created_by_display_name_snapshot)) > 0");
                t.HasCheckConstraint("ck_modifier_definition_versions_cost_non_negative", "activation_cost >= 0");
                t.HasCheckConstraint("ck_modifier_definition_versions_limit_positive_or_null", "max_activations_per_round IS NULL OR max_activations_per_round > 0");
                t.HasCheckConstraint("ck_modifier_definition_versions_revision_positive", "revision >= 1");
            });
        });
        modelBuilder.Entity("backend.Data.Entities.ModifierDefinitionVersionConflict", delegate (EntityTypeBuilder b)
        {
            b.Property<Guid>("ModifierVersionId").HasColumnType("uuid").HasColumnName("modifier_version_id");
            b.Property<Guid>("ConflictingModifierId").HasColumnType("uuid").HasColumnName("conflicting_modifier_id");
            b.Property<string>("ConflictingModifierNameSnapshot").IsRequired().HasMaxLength(128)
                .HasColumnType("character varying(128)")
                .HasColumnName("conflicting_modifier_name_snapshot");
            b.HasKey("ModifierVersionId", "ConflictingModifierId").HasName("pk_modifier_definition_version_conflicts");
            b.HasIndex("ConflictingModifierId").HasDatabaseName("ix_modifier_conflicts_definition");
            b.ToTable("modifier_definition_version_conflicts", null, delegate (TableBuilder t)
            {
                t.HasCheckConstraint("ck_modifier_definition_version_conflicts_name_not_blank", "length(trim(conflicting_modifier_name_snapshot)) > 0");
            });
        });
        modelBuilder.Entity("backend.Data.Entities.QuestionAcceptedAnswer", delegate (EntityTypeBuilder b)
        {
            b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uuid")
                .HasColumnName("id");
            b.Property<string>("AnswerText").IsRequired().HasMaxLength(500)
                .HasColumnType("character varying(500)")
                .HasColumnName("answer_text");
            b.Property<DateTime>("CreatedAtUtc").HasColumnType("timestamp with time zone").HasColumnName("created_at_utc");
            b.Property<bool>("IsPrimary").HasColumnType("boolean").HasColumnName("is_primary");
            b.Property<string>("NormalizedAnswer").IsRequired().HasMaxLength(500)
                .HasColumnType("character varying(500)")
                .HasColumnName("normalized_answer");
            b.Property<Guid>("QuestionId").HasColumnType("uuid").HasColumnName("question_id");
            b.Property<int>("SortOrder").HasColumnType("integer").HasColumnName("sort_order");
            b.HasKey("Id").HasName("pk_question_accepted_answers");
            b.HasIndex("QuestionId", "NormalizedAnswer").IsUnique().HasDatabaseName("ix_question_accepted_answers_question_id_normalized_answer");
            b.HasIndex("QuestionId", "SortOrder").IsUnique().HasDatabaseName("ix_question_accepted_answers_question_id_sort_order");
            b.HasIndex(new string[1] { "AnswerText" }, "ix_question_accepted_answers_text_trgm").HasDatabaseName("ix_question_accepted_answers_text_trgm");
            b.HasIndex(new string[1] { "AnswerText" }, "ix_question_accepted_answers_text_trgm").HasMethod("gin");
            b.HasIndex(new string[1] { "AnswerText" }, "ix_question_accepted_answers_text_trgm").HasOperators("gin_trgm_ops");
            b.HasIndex(new string[1] { "QuestionId" }, "ux_question_accepted_answers_one_primary").IsUnique().HasDatabaseName("ux_question_accepted_answers_one_primary")
                .HasFilter("is_primary = TRUE");
            b.ToTable("question_accepted_answers", null, delegate (TableBuilder t)
            {
                t.HasCheckConstraint("ck_question_accepted_answers_sort_order_non_negative", "sort_order >= 0");
                t.HasCheckConstraint("ck_question_accepted_answers_text_not_blank", "length(trim(answer_text)) > 0 AND length(trim(normalized_answer)) > 0");
            });
        });
        modelBuilder.Entity("backend.Data.Entities.QuestionCategory", delegate (EntityTypeBuilder b)
        {
            b.Property<Guid>("Id").HasColumnType("uuid").HasColumnName("id");
            b.Property<DateTime>("CreatedAtUtc").HasColumnType("timestamp with time zone").HasColumnName("created_at_utc");
            b.Property<string>("Name").IsRequired().HasMaxLength(64)
                .HasColumnType("citext")
                .HasColumnName("name");
            b.Property<DateTime>("UpdatedAtUtc").HasColumnType("timestamp with time zone").HasColumnName("updated_at_utc");
            b.HasKey("Id").HasName("pk_question_categories");
            b.HasIndex("Name").IsUnique().HasDatabaseName("ix_question_categories_name");
            b.ToTable("question_categories", null, delegate (TableBuilder t)
            {
                t.HasCheckConstraint("ck_question_categories_name_not_blank", "length(trim(name)) > 0");
                t.HasCheckConstraint("ck_question_categories_timestamps", "updated_at_utc >= created_at_utc");
            });
        });
        modelBuilder.Entity("backend.Data.Entities.QuestionDefinition", delegate (EntityTypeBuilder b)
        {
            b.Property<Guid>("Id").HasColumnType("uuid").HasColumnName("id");
            b.Property<Guid>("CategoryId").HasColumnType("uuid").HasColumnName("category_id");
            b.Property<DateTime>("CreatedAtUtc").HasColumnType("timestamp with time zone").HasColumnName("created_at_utc");
            b.Property<DateTime?>("DeletedAtUtc").HasColumnType("timestamp with time zone").HasColumnName("deleted_at_utc");
            b.Property<string>("ExternalCode").IsRequired().HasMaxLength(64)
                .HasColumnType("citext")
                .HasColumnName("external_code");
            b.Property<bool>("IsDeleted").ValueGeneratedOnAdd().HasColumnType("boolean")
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            b.Property<bool>("IsEnabled").ValueGeneratedOnAdd().HasColumnType("boolean")
                .HasDefaultValue(true)
                .HasColumnName("is_enabled");
            b.Property<int>("Priority").ValueGeneratedOnAdd().HasColumnType("integer")
                .HasDefaultValue(0)
                .HasColumnName("priority");
            b.Property<int>("Revision").ValueGeneratedOnAdd().HasColumnType("integer")
                .HasDefaultValue(1)
                .HasColumnName("revision");
            b.Property<int>("Reward").HasColumnType("integer").HasColumnName("reward");
            b.Property<string>("Text").IsRequired().HasMaxLength(2000)
                .HasColumnType("character varying(2000)")
                .HasColumnName("text");
            b.Property<DateTime>("UpdatedAtUtc").HasColumnType("timestamp with time zone").HasColumnName("updated_at_utc");
            b.HasKey("Id").HasName("pk_question_definitions");
            b.HasIndex("ExternalCode").IsUnique().HasDatabaseName("ux_questions_external_code");
            b.HasIndex("Priority").HasDatabaseName("ix_questions_priority");
            b.HasIndex("CategoryId", "IsEnabled").HasDatabaseName("ix_questions_category_enabled");
            b.HasIndex("IsDeleted", "IsEnabled", "Priority").HasDatabaseName("ix_questions_active_pick_queue");
            b.HasIndex(new string[1] { "Text" }, "ix_questions_text_trgm").HasDatabaseName("ix_questions_text_trgm");
            b.HasIndex(new string[1] { "Text" }, "ix_questions_text_trgm").HasMethod("gin");
            b.HasIndex(new string[1] { "Text" }, "ix_questions_text_trgm").HasOperators("gin_trgm_ops");
            b.ToTable("question_definitions", null, delegate (TableBuilder t)
            {
                t.HasCheckConstraint("ck_question_definitions_content_not_blank", "length(trim(external_code)) > 0 AND length(trim(text)) > 0");
                t.HasCheckConstraint("ck_question_definitions_revision_positive", "revision > 0");
                t.HasCheckConstraint("ck_question_definitions_reward_non_negative", "reward >= 0");
                t.HasCheckConstraint("ck_question_definitions_soft_delete_semantics", "(is_deleted = FALSE AND deleted_at_utc IS NULL) OR (is_deleted = TRUE AND is_enabled = FALSE AND deleted_at_utc IS NOT NULL)");
                t.HasCheckConstraint("ck_question_definitions_timestamps", "updated_at_utc >= created_at_utc AND (deleted_at_utc IS NULL OR deleted_at_utc >= created_at_utc)");
            });
        });
        modelBuilder.Entity("backend.Data.Entities.Role", delegate (EntityTypeBuilder b)
        {
            b.Property<short>("Id").HasColumnType("smallint").HasColumnName("id");
            b.Property<string>("Code").IsRequired().HasMaxLength(32)
                .HasColumnType("citext")
                .HasColumnName("code");
            b.Property<DateTime>("CreatedAtUtc").HasColumnType("timestamp with time zone").HasColumnName("created_at_utc");
            b.Property<string>("Description").HasMaxLength(256).HasColumnType("character varying(256)")
                .HasColumnName("description");
            b.Property<string>("Name").IsRequired().HasMaxLength(64)
                .HasColumnType("character varying(64)")
                .HasColumnName("name");
            b.Property<DateTime>("UpdatedAtUtc").HasColumnType("timestamp with time zone").HasColumnName("updated_at_utc");
            b.HasKey("Id").HasName("pk_roles");
            b.HasIndex("Code").IsUnique().HasDatabaseName("ix_roles_code");
            b.ToTable("roles", null, delegate (TableBuilder t)
            {
                t.HasCheckConstraint("ck_roles_identity_not_blank", "length(trim(code)) > 0 AND length(trim(name)) > 0");
                t.HasCheckConstraint("ck_roles_timestamps", "updated_at_utc >= created_at_utc");
            });
            b.HasData(new
            {
                Id = (short)1,
                Code = "viewer",
                CreatedAtUtc = new DateTime(2026, 3, 23, 0, 0, 0, 0, DateTimeKind.Utc),
                Description = "Viewer role with basic registration capabilities.",
                Name = "Viewer",
                UpdatedAtUtc = new DateTime(2026, 3, 23, 0, 0, 0, 0, DateTimeKind.Utc)
            }, new
            {
                Id = (short)2,
                Code = "moderator",
                CreatedAtUtc = new DateTime(2026, 3, 23, 0, 0, 0, 0, DateTimeKind.Utc),
                Description = "Moderator role that helps manage game operations.",
                Name = "Moderator",
                UpdatedAtUtc = new DateTime(2026, 3, 23, 0, 0, 0, 0, DateTimeKind.Utc)
            }, new
            {
                Id = (short)3,
                Code = "admin",
                CreatedAtUtc = new DateTime(2026, 3, 23, 0, 0, 0, 0, DateTimeKind.Utc),
                Description = "Administrator role with full management access.",
                Name = "Administrator",
                UpdatedAtUtc = new DateTime(2026, 3, 23, 0, 0, 0, 0, DateTimeKind.Utc)
            });
        });
        modelBuilder.Entity("backend.Data.Entities.User", delegate (EntityTypeBuilder b)
        {
            b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uuid")
                .HasColumnName("id");
            b.Property<string>("BroadcasterType").HasMaxLength(32).HasColumnType("character varying(32)")
                .HasColumnName("broadcaster_type");
            b.Property<DateTime>("CreatedAtUtc").HasColumnType("timestamp with time zone").HasColumnName("created_at_utc");
            b.Property<string>("DisplayName").IsRequired().HasMaxLength(64)
                .HasColumnType("character varying(64)")
                .HasColumnName("display_name");
            b.Property<bool>("IsActive").ValueGeneratedOnAdd().HasColumnType("boolean")
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            b.Property<DateTime?>("LastLoginAtUtc").HasColumnType("timestamp with time zone").HasColumnName("last_login_at_utc");
            b.Property<string>("Login").IsRequired().HasMaxLength(64)
                .HasColumnType("citext")
                .HasColumnName("login");
            b.Property<string>("ProfileImageUrl").HasMaxLength(1024).HasColumnType("character varying(1024)")
                .HasColumnName("profile_image_url");
            b.Property<string>("TwitchUserId").IsRequired().HasMaxLength(64)
                .HasColumnType("character varying(64)")
                .HasColumnName("twitch_user_id");
            b.Property<string>("TwitchUserType").HasMaxLength(32).HasColumnType("character varying(32)")
                .HasColumnName("twitch_user_type");
            b.Property<DateTime>("UpdatedAtUtc").HasColumnType("timestamp with time zone").HasColumnName("updated_at_utc");
            b.HasKey("Id").HasName("pk_users");
            b.HasIndex("Login").HasDatabaseName("ix_users_login");
            b.HasIndex("TwitchUserId").IsUnique().HasDatabaseName("ix_users_twitch_user_id");
            b.ToTable("users", null, delegate (TableBuilder t)
            {
                t.HasCheckConstraint("ck_users_timestamps", "updated_at_utc >= created_at_utc AND (last_login_at_utc IS NULL OR last_login_at_utc >= created_at_utc)");
                t.HasCheckConstraint("ck_users_twitch_identity_not_blank", "length(trim(twitch_user_id)) > 0 AND length(trim(login)) > 0 AND length(trim(display_name)) > 0");
            });
        });
        modelBuilder.Entity("backend.Data.Entities.UserRole", delegate (EntityTypeBuilder b)
        {
            b.Property<Guid>("UserId").HasColumnType("uuid").HasColumnName("user_id");
            b.Property<short>("RoleId").HasColumnType("smallint").HasColumnName("role_id");
            b.Property<DateTime>("AssignedAtUtc").HasColumnType("timestamp with time zone").HasColumnName("assigned_at_utc");
            b.Property<Guid?>("AssignedByUserId").HasColumnType("uuid").HasColumnName("assigned_by_user_id");
            b.Property<DateTime?>("ExpiresAtUtc").HasColumnType("timestamp with time zone").HasColumnName("expires_at_utc");
            b.HasKey("UserId", "RoleId").HasName("pk_user_roles");
            b.HasIndex("AssignedByUserId").HasDatabaseName("ix_user_roles_assigned_by_user_id");
            b.HasIndex("ExpiresAtUtc").HasDatabaseName("ix_user_roles_expires_at_utc").HasFilter("expires_at_utc IS NOT NULL");
            b.HasIndex("RoleId").HasDatabaseName("ix_user_roles_role_id");
            b.ToTable("user_roles", null, delegate (TableBuilder t)
            {
                t.HasCheckConstraint("ck_user_roles_expiry_after_assignment", "expires_at_utc IS NULL OR expires_at_utc > assigned_at_utc");
            });
        });
        modelBuilder.Entity("backend.Data.Entities.BoardCell", delegate (EntityTypeBuilder b)
        {
            b.HasOne("backend.Data.Entities.GameBoard", "Board").WithMany("Cells").HasForeignKey("BoardId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired()
                .HasConstraintName("fk_game_board_cells_game_boards_board_id");
            b.Navigation("Board");
        });
        modelBuilder.Entity("backend.Data.Entities.BoardCellMedia", delegate (EntityTypeBuilder b)
        {
            b.HasOne("backend.Data.Entities.BoardCell", "Cell").WithMany("MediaLinks").HasForeignKey("CellId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired()
                .HasConstraintName("fk_game_board_cell_media_game_board_cells_cell_id");
            b.HasOne("backend.Data.Entities.MediaAsset", "MediaAsset").WithMany("CellLinks").HasForeignKey("MediaAssetId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired()
                .HasConstraintName("fk_game_board_cell_media_media_assets_media_asset_id");
            b.Navigation("Cell");
            b.Navigation("MediaAsset");
        });
        modelBuilder.Entity("backend.Data.Entities.Game", delegate (EntityTypeBuilder b)
        {
            b.HasOne("backend.Data.Entities.GameTeam", "ActiveTeam").WithMany().HasForeignKey("Id", "ActiveTeamId")
                .HasPrincipalKey("GameId", "Id")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_games_active_team_same_game");
            b.Navigation("ActiveTeam");
        });
        modelBuilder.Entity("backend.Data.Entities.GameBoard", delegate (EntityTypeBuilder b)
        {
            b.HasOne("backend.Data.Entities.Game", "Game").WithOne("Board").HasForeignKey("backend.Data.Entities.GameBoard", "GameId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired()
                .HasConstraintName("fk_game_boards_games_game_id");
            b.Navigation("Game");
        });
        modelBuilder.Entity("backend.Data.Entities.GameEnabledModifier", delegate (EntityTypeBuilder b)
        {
            b.HasOne("backend.Data.Entities.User", "EmergencyDisabledByUser").WithMany().HasForeignKey("EmergencyDisabledByUserId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_game_enabled_modifiers_users_emergency_disabled_by_user_id");
            b.HasOne("backend.Data.Entities.Game", "Game").WithMany("EnabledModifiers").HasForeignKey("GameId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired()
                .HasConstraintName("fk_game_enabled_modifiers_games_game_id");
            b.HasOne("backend.Data.Entities.ModifierDefinition", "ModifierDefinition").WithMany("EnabledInGames").HasForeignKey("ModifierId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("fk_game_enabled_modifiers_modifier_definitions_modifier_id");
            b.HasOne("backend.Data.Entities.ModifierDefinitionVersion", "ModifierVersion").WithMany().HasForeignKey("ModifierId", "ModifierVersionId")
                .HasPrincipalKey("ModifierId", "Id")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_game_enabled_modifiers_modifier_version");
            b.Navigation("EmergencyDisabledByUser");
            b.Navigation("Game");
            b.Navigation("ModifierDefinition");
            b.Navigation("ModifierVersion");
        });
        modelBuilder.Entity("backend.Data.Entities.GameEnabledQuestion", delegate (EntityTypeBuilder b)
        {
            b.HasOne("backend.Data.Entities.Game", "Game").WithMany("EnabledQuestions").HasForeignKey("GameId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired()
                .HasConstraintName("fk_game_enabled_questions_games_game_id");
            b.HasOne("backend.Data.Entities.QuestionDefinition", "QuestionDefinition").WithMany("EnabledInGames").HasForeignKey("QuestionId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("fk_game_enabled_questions_question_definitions_question_id");
            b.Navigation("Game");
            b.Navigation("QuestionDefinition");
        });
        modelBuilder.Entity("backend.Data.Entities.GameFinalization", delegate (EntityTypeBuilder b)
        {
            b.HasOne("backend.Data.Entities.User", "FinishedByUser").WithMany().HasForeignKey("FinishedByUserId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("fk_game_finalizations_users_finished_by_user_id");
            b.HasOne("backend.Data.Entities.Game", "Game").WithOne("Finalization").HasForeignKey("backend.Data.Entities.GameFinalization", "GameId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired()
                .HasConstraintName("fk_game_finalizations_games_game_id");
            b.Navigation("FinishedByUser");
            b.Navigation("Game");
        });
        modelBuilder.Entity("backend.Data.Entities.GameModifierActivation", delegate (EntityTypeBuilder b)
        {
            b.HasOne("backend.Data.Entities.User", "ActivatedByUser").WithMany("ActivatedGameModifiers").HasForeignKey("ActivatedByUserId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("fk_game_modifier_activations_users_activated_by_user_id");
            b.HasOne("backend.Data.Entities.User", "CancelledByUser").WithMany().HasForeignKey("CancelledByUserId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_game_modifier_activations_users_cancelled_by_user_id");
            b.HasOne("backend.Data.Entities.GameEnabledModifier", "EnabledModifier").WithMany().HasForeignKey("GameId", "ModifierId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("fk_game_modifier_activations_enabled_modifier");
            b.HasOne("backend.Data.Entities.Game", "Game").WithMany("ModifierActivations").HasForeignKey("GameId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired()
                .HasConstraintName("fk_game_modifier_activations_games_game_id");
            b.HasOne("backend.Data.Entities.User", "InitiatedByUser").WithMany().HasForeignKey("InitiatedByUserId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("fk_game_modifier_activations_users_initiated_by_user_id");
            b.HasOne("backend.Data.Entities.ModifierDefinition", "ModifierDefinition").WithMany("GameActivations").HasForeignKey("ModifierId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("fk_game_modifier_activations_modifier_definitions_modifier_id");
            b.HasOne("backend.Data.Entities.GameRound", "Round").WithMany().HasForeignKey("GameId", "RoundId")
                .HasPrincipalKey("GameId", "Id")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("fk_modifier_activations_game_rounds_same_game");
            b.HasOne("backend.Data.Entities.ModifierDefinitionVersion", "ModifierVersion").WithMany().HasForeignKey("ModifierId", "ModifierVersionId")
                .HasPrincipalKey("ModifierId", "Id")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("fk_game_modifier_activations_modifier_version");
            b.Navigation("ActivatedByUser");
            b.Navigation("CancelledByUser");
            b.Navigation("EnabledModifier");
            b.Navigation("Game");
            b.Navigation("InitiatedByUser");
            b.Navigation("ModifierDefinition");
            b.Navigation("ModifierVersion");
            b.Navigation("Round");
        });
        modelBuilder.Entity("backend.Data.Entities.GameQuizCorrectAnswer", delegate (EntityTypeBuilder b)
        {
            b.HasOne("backend.Data.Entities.User", "AwardedToUser").WithMany("CorrectQuizAnswers").HasForeignKey("AwardedToUserId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("fk_game_quiz_correct_answers_users_awarded_to_user_id");
            b.HasOne("backend.Data.Entities.User", "CapturedByUser").WithMany().HasForeignKey("CapturedByUserId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_game_quiz_correct_answers_users_captured_by_user_id");
            b.HasOne("backend.Data.Entities.GameQuizRound", "QuizRound").WithOne("CorrectAnswer").HasForeignKey("backend.Data.Entities.GameQuizCorrectAnswer", "GameId", "QuizRoundId")
                .HasPrincipalKey("backend.Data.Entities.GameQuizRound", "GameId", "Id")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("fk_quiz_correct_answers_round_same_game");
            b.Navigation("AwardedToUser");
            b.Navigation("CapturedByUser");
            b.Navigation("QuizRound");
        });
        modelBuilder.Entity("backend.Data.Entities.GameQuizPointLedgerEntry", delegate (EntityTypeBuilder b)
        {
            b.HasOne("backend.Data.Entities.User", "CreatedByUser").WithMany().HasForeignKey("CreatedByUserId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_game_quiz_point_ledger_entries_users_created_by_user_id");
            b.HasOne("backend.Data.Entities.Game", "Game").WithMany().HasForeignKey("GameId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("fk_game_quiz_point_ledger_entries_games_game_id");
            b.HasOne("backend.Data.Entities.User", "User").WithMany("QuizPointLedgerEntries").HasForeignKey("UserId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("fk_game_quiz_point_ledger_entries_users_user_id");
            b.HasOne("backend.Data.Entities.GameQuizCorrectAnswer", "CorrectAnswer").WithMany("PointEntries").HasForeignKey("GameId", "CorrectAnswerId")
                .HasPrincipalKey("GameId", "Id")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_quiz_point_ledger_correct_answer_same_game");
            b.HasOne("backend.Data.Entities.GameModifierActivation", "ModifierActivation").WithMany().HasForeignKey("GameId", "ModifierActivationId")
                .HasPrincipalKey("GameId", "Id")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_quiz_point_ledger_modifier_activation_same_game");
            b.Navigation("CorrectAnswer");
            b.Navigation("CreatedByUser");
            b.Navigation("Game");
            b.Navigation("ModifierActivation");
            b.Navigation("User");
        });
        modelBuilder.Entity("backend.Data.Entities.GameQuizRound", delegate (EntityTypeBuilder b)
        {
            b.HasOne("backend.Data.Entities.GameEnabledQuestion", "EnabledQuestion").WithMany().HasForeignKey("GameId", "QuestionId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("fk_game_quiz_rounds_enabled_question");
            b.HasOne("backend.Data.Entities.User", "AskedByUser").WithMany("AskedGameQuizRounds").HasForeignKey("AskedByUserId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_game_quiz_rounds_users_asked_by_user_id");
            b.HasOne("backend.Data.Entities.Game", "Game").WithMany().HasForeignKey("GameId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired()
                .HasConstraintName("fk_game_quiz_rounds_games_game_id");
            b.HasOne("backend.Data.Entities.QuestionDefinition", "Question").WithMany("AskedInQuizRounds").HasForeignKey("QuestionId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("fk_game_quiz_rounds_question_definitions_question_id");
            b.Navigation("AskedByUser");
            b.Navigation("EnabledQuestion");
            b.Navigation("Game");
            b.Navigation("Question");
        });
        modelBuilder.Entity("backend.Data.Entities.GameRound", delegate (EntityTypeBuilder b)
        {
            b.HasOne("backend.Data.Entities.Game", "Game").WithMany().HasForeignKey("GameId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("fk_game_rounds_games_game_id");
            b.HasOne("backend.Data.Entities.User", "ResolvedByUser").WithMany().HasForeignKey("ResolvedByUserId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_game_rounds_users_resolved_by_user_id");
            b.HasOne("backend.Data.Entities.BoardCell", "BoardCell").WithMany().HasForeignKey("BoardId", "BoardCellId")
                .HasPrincipalKey("BoardId", "Id")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("fk_game_rounds_board_cells_same_board");
            b.HasOne("backend.Data.Entities.GameBoard", null).WithMany().HasForeignKey("GameId", "BoardId")
                .HasPrincipalKey("GameId", "Id")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("fk_game_rounds_game_boards_same_game");
            b.HasOne("backend.Data.Entities.GameTeam", "Team").WithMany().HasForeignKey("GameId", "TeamId")
                .HasPrincipalKey("GameId", "Id")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("fk_game_rounds_game_teams_same_game");
            b.Navigation("BoardCell");
            b.Navigation("Game");
            b.Navigation("ResolvedByUser");
            b.Navigation("Team");
        });
        modelBuilder.Entity("backend.Data.Entities.GameRoundCellMedia", delegate (EntityTypeBuilder b)
        {
            b.HasOne("backend.Data.Entities.GameRound", "Round").WithMany("CellMedia").HasForeignKey("RoundId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired()
                .HasConstraintName("fk_game_round_cell_media_game_rounds_round_id");
            b.Navigation("Round");
        });
        modelBuilder.Entity("backend.Data.Entities.GameRoundModifierResult", delegate (EntityTypeBuilder b)
        {
            b.HasOne("backend.Data.Entities.ModifierDefinition", "ModifierDefinition").WithMany().HasForeignKey("ModifierId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("fk_modifier_results_definition");
            b.HasOne("backend.Data.Entities.User", "ResolvedByUser").WithMany().HasForeignKey("ResolvedByUserId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_game_round_modifier_results_users_resolved_by_user_id");
            b.HasOne("backend.Data.Entities.GameRound", "Round").WithMany("ModifierResults").HasForeignKey("RoundId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired()
                .HasConstraintName("fk_game_round_modifier_results_game_rounds_round_id");
            b.HasOne("backend.Data.Entities.GameModifierActivation", "GameModifierActivation").WithMany().HasForeignKey("RoundId", "GameModifierActivationId", "ModifierId")
                .HasPrincipalKey("RoundId", "Id", "ModifierId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("fk_modifier_results_activation_same_round_modifier");
            b.Navigation("GameModifierActivation");
            b.Navigation("ModifierDefinition");
            b.Navigation("ResolvedByUser");
            b.Navigation("Round");
        });
        modelBuilder.Entity("backend.Data.Entities.GameRoundParticipant", delegate (EntityTypeBuilder b)
        {
            b.HasOne("backend.Data.Entities.GameRound", "Round").WithMany("Participants").HasForeignKey("RoundId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired()
                .HasConstraintName("fk_game_round_participants_game_rounds_round_id");
            b.HasOne("backend.Data.Entities.User", "User").WithMany().HasForeignKey("UserId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("fk_game_round_participants_users_user_id");
            b.Navigation("Round");
            b.Navigation("User");
        });
        modelBuilder.Entity("backend.Data.Entities.GameRoundTransitionAudit", delegate (EntityTypeBuilder b)
        {
            b.HasOne("backend.Data.Entities.User", "InitiatedByUser").WithMany().HasForeignKey("InitiatedByUserId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("fk_game_round_transition_audits_users_initiated_by_user_id");
            b.HasOne("backend.Data.Entities.GameRound", "Round").WithMany("TransitionAudits").HasForeignKey("RoundId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired()
                .HasConstraintName("fk_game_round_transition_audits_game_rounds_round_id");
            b.Navigation("InitiatedByUser");
            b.Navigation("Round");
        });
        modelBuilder.Entity("backend.Data.Entities.GameTeam", delegate (EntityTypeBuilder b)
        {
            b.HasOne("backend.Data.Entities.User", null).WithMany().HasForeignKey("ConfirmedByUserId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_game_teams_users_confirmed_by_user_id");
            b.HasOne("backend.Data.Entities.User", null).WithMany().HasForeignKey("CreatedByUserId")
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_game_teams_users_created_by_user_id");
            b.HasOne("backend.Data.Entities.User", null).WithMany().HasForeignKey("DisbandRequestedByUserId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_game_teams_users_disband_requested_by_user_id");
            b.HasOne("backend.Data.Entities.User", null).WithMany().HasForeignKey("DisbandedByUserId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_game_teams_users_disbanded_by_user_id");
            b.HasOne("backend.Data.Entities.Game", "Game").WithMany().HasForeignKey("GameId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired()
                .HasConstraintName("fk_game_teams_games_game_id");
            b.HasOne("backend.Data.Entities.User", null).WithMany().HasForeignKey("RejectedByUserId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_game_teams_users_rejected_by_user_id");
            b.HasOne("backend.Data.Entities.GameTeamSlot", "Slot").WithMany().HasForeignKey("GameId", "SlotId")
                .HasPrincipalKey("GameId", "Id")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("fk_game_teams_game_team_slots_game_id_slot_id");
            b.Navigation("Game");
            b.Navigation("Slot");
        });
        modelBuilder.Entity("backend.Data.Entities.GameTeamFinalResult", delegate (EntityTypeBuilder b)
        {
            b.HasOne("backend.Data.Entities.GameFinalization", "Finalization").WithMany("TeamResults").HasForeignKey("GameId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired()
                .HasConstraintName("fk_game_team_final_results_game_finalizations_game_id");
            b.HasOne("backend.Data.Entities.GameTeam", "Team").WithMany().HasForeignKey("GameId", "TeamId")
                .HasPrincipalKey("GameId", "Id")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("fk_game_team_final_results_team_same_game");
            b.Navigation("Finalization");
            b.Navigation("Team");
        });
        modelBuilder.Entity("backend.Data.Entities.GameTeamInvitation", delegate (EntityTypeBuilder b)
        {
            b.HasOne("backend.Data.Entities.Game", "Game").WithMany().HasForeignKey("GameId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired()
                .HasConstraintName("fk_game_team_invitations_games_game_id");
            b.HasOne("backend.Data.Entities.User", null).WithMany().HasForeignKey("InvitedByUserId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("fk_game_team_invitations_users_invited_by_user_id");
            b.HasOne("backend.Data.Entities.User", null).WithMany().HasForeignKey("InvitedUserId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("fk_game_team_invitations_users_invited_user_id");
            b.HasOne("backend.Data.Entities.GameTeamSlot", "Slot").WithMany().HasForeignKey("GameId", "SlotId")
                .HasPrincipalKey("GameId", "Id")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("fk_game_team_invitations_game_team_slots_game_id_slot_id");
            b.HasOne("backend.Data.Entities.GameTeam", "Team").WithMany().HasForeignKey("GameId", "TeamId")
                .HasPrincipalKey("GameId", "Id")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_game_team_invitations_team_same_game");
            b.Navigation("Game");
            b.Navigation("Slot");
            b.Navigation("Team");
        });
        modelBuilder.Entity("backend.Data.Entities.GameTeamMember", delegate (EntityTypeBuilder b)
        {
            b.HasOne("backend.Data.Entities.Game", null).WithMany().HasForeignKey("GameId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired()
                .HasConstraintName("fk_game_team_members_games_game_id");
            b.HasOne("backend.Data.Entities.User", "User").WithMany().HasForeignKey("UserId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("fk_game_team_members_users_user_id");
            b.HasOne("backend.Data.Entities.GameTeam", "Team").WithMany("Members").HasForeignKey("GameId", "TeamId")
                .HasPrincipalKey("GameId", "Id")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired()
                .HasConstraintName("fk_game_team_members_game_teams_game_id_team_id");
            b.Navigation("Team");
            b.Navigation("User");
        });
        modelBuilder.Entity("backend.Data.Entities.GameTeamSlot", delegate (EntityTypeBuilder b)
        {
            b.HasOne("backend.Data.Entities.Game", "Game").WithMany("TeamSlots").HasForeignKey("GameId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired()
                .HasConstraintName("fk_game_team_slots_games_game_id");
            b.Navigation("Game");
        });
        modelBuilder.Entity("backend.Data.Entities.GameUserNotification", delegate (EntityTypeBuilder b)
        {
            b.HasOne("backend.Data.Entities.Game", "Game").WithMany().HasForeignKey("GameId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("fk_game_user_notifications_games_game_id");
            b.HasOne("backend.Data.Entities.User", "User").WithMany("GameNotifications").HasForeignKey("UserId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired()
                .HasConstraintName("fk_game_user_notifications_users_user_id");
            b.Navigation("Game");
            b.Navigation("User");
        });
        modelBuilder.Entity("backend.Data.Entities.ModifierDefinition", delegate (EntityTypeBuilder b)
        {
            b.HasOne("backend.Data.Entities.User", "ArchivedByUser").WithMany().HasForeignKey("ArchivedByUserId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_modifier_definitions_users_archived_by_user_id");
            b.HasOne("backend.Data.Entities.User", "CreatedByUser").WithMany().HasForeignKey("CreatedByUserId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_modifier_definitions_users_created_by_user_id");
            b.HasOne("backend.Data.Entities.ModifierDefinitionVersion", "CurrentVersion").WithMany().HasForeignKey("Id", "CurrentVersionId")
                .HasPrincipalKey("ModifierId", "Id")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_modifier_definitions_current_version");
            b.Navigation("ArchivedByUser");
            b.Navigation("CreatedByUser");
            b.Navigation("CurrentVersion");
        });
        modelBuilder.Entity("backend.Data.Entities.ModifierDefinitionVersion", delegate (EntityTypeBuilder b)
        {
            b.HasOne("backend.Data.Entities.ModifierDefinition", "CascadeSourceModifier").WithMany().HasForeignKey("CascadeSourceModifierId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_modifier_versions_cascade_source");
            b.HasOne("backend.Data.Entities.User", "CreatedByUser").WithMany().HasForeignKey("CreatedByUserId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_modifier_definition_versions_users_created_by_user_id");
            b.HasOne("backend.Data.Entities.ModifierDefinition", "Modifier").WithMany("Versions").HasForeignKey("ModifierId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("fk_modifier_versions_definition");
            b.Navigation("CascadeSourceModifier");
            b.Navigation("CreatedByUser");
            b.Navigation("Modifier");
        });
        modelBuilder.Entity("backend.Data.Entities.ModifierDefinitionVersionConflict", delegate (EntityTypeBuilder b)
        {
            b.HasOne("backend.Data.Entities.ModifierDefinition", "ConflictingModifier").WithMany().HasForeignKey("ConflictingModifierId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("fk_modifier_conflicts_definition");
            b.HasOne("backend.Data.Entities.ModifierDefinitionVersion", "ModifierVersion").WithMany("Conflicts").HasForeignKey("ModifierVersionId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("fk_modifier_conflicts_version");
            b.Navigation("ConflictingModifier");
            b.Navigation("ModifierVersion");
        });
        modelBuilder.Entity("backend.Data.Entities.QuestionAcceptedAnswer", delegate (EntityTypeBuilder b)
        {
            b.HasOne("backend.Data.Entities.QuestionDefinition", "Question").WithMany("AcceptedAnswers").HasForeignKey("QuestionId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired()
                .HasConstraintName("fk_question_accepted_answers_question_definitions_question_id");
            b.Navigation("Question");
        });
        modelBuilder.Entity("backend.Data.Entities.QuestionDefinition", delegate (EntityTypeBuilder b)
        {
            b.HasOne("backend.Data.Entities.QuestionCategory", "CategoryDefinition").WithMany("Questions").HasForeignKey("CategoryId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("fk_question_definitions_question_categories_category_id");
            b.Navigation("CategoryDefinition");
        });
        modelBuilder.Entity("backend.Data.Entities.UserRole", delegate (EntityTypeBuilder b)
        {
            b.HasOne("backend.Data.Entities.User", "AssignedByUser").WithMany("AssignedRoles").HasForeignKey("AssignedByUserId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_user_roles_users_assigned_by_user_id");
            b.HasOne("backend.Data.Entities.Role", "Role").WithMany("UserRoles").HasForeignKey("RoleId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired()
                .HasConstraintName("fk_user_roles_roles_role_id");
            b.HasOne("backend.Data.Entities.User", "User").WithMany("UserRoles").HasForeignKey("UserId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired()
                .HasConstraintName("fk_user_roles_users_user_id");
            b.Navigation("AssignedByUser");
            b.Navigation("Role");
            b.Navigation("User");
        });
        modelBuilder.Entity("backend.Data.Entities.BoardCell", delegate (EntityTypeBuilder b)
        {
            b.Navigation("MediaLinks");
        });
        modelBuilder.Entity("backend.Data.Entities.Game", delegate (EntityTypeBuilder b)
        {
            b.Navigation("Board");
            b.Navigation("EnabledModifiers");
            b.Navigation("EnabledQuestions");
            b.Navigation("Finalization");
            b.Navigation("ModifierActivations");
            b.Navigation("TeamSlots");
        });
        modelBuilder.Entity("backend.Data.Entities.GameBoard", delegate (EntityTypeBuilder b)
        {
            b.Navigation("Cells");
        });
        modelBuilder.Entity("backend.Data.Entities.GameFinalization", delegate (EntityTypeBuilder b)
        {
            b.Navigation("TeamResults");
        });
        modelBuilder.Entity("backend.Data.Entities.GameQuizCorrectAnswer", delegate (EntityTypeBuilder b)
        {
            b.Navigation("PointEntries");
        });
        modelBuilder.Entity("backend.Data.Entities.GameQuizRound", delegate (EntityTypeBuilder b)
        {
            b.Navigation("CorrectAnswer");
        });
        modelBuilder.Entity("backend.Data.Entities.GameRound", delegate (EntityTypeBuilder b)
        {
            b.Navigation("CellMedia");
            b.Navigation("ModifierResults");
            b.Navigation("Participants");
            b.Navigation("TransitionAudits");
        });
        modelBuilder.Entity("backend.Data.Entities.GameTeam", delegate (EntityTypeBuilder b)
        {
            b.Navigation("Members");
        });
        modelBuilder.Entity("backend.Data.Entities.MediaAsset", delegate (EntityTypeBuilder b)
        {
            b.Navigation("CellLinks");
        });
        modelBuilder.Entity("backend.Data.Entities.ModifierDefinition", delegate (EntityTypeBuilder b)
        {
            b.Navigation("EnabledInGames");
            b.Navigation("GameActivations");
            b.Navigation("Versions");
        });
        modelBuilder.Entity("backend.Data.Entities.ModifierDefinitionVersion", delegate (EntityTypeBuilder b)
        {
            b.Navigation("Conflicts");
        });
        modelBuilder.Entity("backend.Data.Entities.QuestionCategory", delegate (EntityTypeBuilder b)
        {
            b.Navigation("Questions");
        });
        modelBuilder.Entity("backend.Data.Entities.QuestionDefinition", delegate (EntityTypeBuilder b)
        {
            b.Navigation("AcceptedAnswers");
            b.Navigation("AskedInQuizRounds");
            b.Navigation("EnabledInGames");
        });
        modelBuilder.Entity("backend.Data.Entities.Role", delegate (EntityTypeBuilder b)
        {
            b.Navigation("UserRoles");
        });
        modelBuilder.Entity("backend.Data.Entities.User", delegate (EntityTypeBuilder b)
        {
            b.Navigation("ActivatedGameModifiers");
            b.Navigation("AskedGameQuizRounds");
            b.Navigation("AssignedRoles");
            b.Navigation("CorrectQuizAnswers");
            b.Navigation("GameNotifications");
            b.Navigation("QuizPointLedgerEntries");
            b.Navigation("UserRoles");
        });
    }
}
