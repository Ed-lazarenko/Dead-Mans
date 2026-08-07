using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "media_assets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    bucket = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    object_key = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    mime_type = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    scope = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_media_assets", x => x.id);
                    table.CheckConstraint("ck_media_assets_scope_allowed", "scope IN ('private')");
                    table.CheckConstraint("ck_media_assets_status_allowed", "status IN ('pending','active')");
                });

            migrationBuilder.CreateTable(
                name: "modifier_definitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    scoring_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "round"),
                    requires_host_control = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    icon_emoji = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    activation_command = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    activation_cost = table.Column<int>(type: "integer", nullable: false),
                    default_limit_per_game = table.Column<int>(type: "integer", nullable: true),
                    metadata_json = table.Column<string>(type: "jsonb", nullable: true),
                    is_archived = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_modifier_definitions", x => x.id);
                    table.CheckConstraint("ck_modifier_definitions_category_allowed", "category IN ('preparation','round','result')");
                    table.CheckConstraint("ck_modifier_definitions_cost_non_negative", "activation_cost >= 0");
                    table.CheckConstraint("ck_modifier_definitions_limit_positive_or_null", "default_limit_per_game IS NULL OR default_limit_per_game > 0");
                });

            migrationBuilder.CreateTable(
                name: "question_categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_question_categories", x => x.id);
                    table.CheckConstraint("ck_question_categories_name_not_blank", "length(trim(name)) > 0");
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<short>(type: "smallint", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    description = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    twitch_user_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    login = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    display_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    email_verified = table.Column<bool>(type: "boolean", nullable: true),
                    profile_image_url = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    broadcaster_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    twitch_user_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    last_login_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "modifier_conflicts",
                columns: table => new
                {
                    modifier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    conflicts_with_modifier_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_modifier_conflicts", x => new { x.modifier_id, x.conflicts_with_modifier_id });
                    table.CheckConstraint("ck_modifier_conflicts_distinct_ids", "modifier_id <> conflicts_with_modifier_id");
                    table.ForeignKey(
                        name: "fk_modifier_conflicts_conflicting_modifier",
                        column: x => x.conflicts_with_modifier_id,
                        principalTable: "modifier_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_modifier_conflicts_modifier",
                        column: x => x.modifier_id,
                        principalTable: "modifier_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "question_definitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    external_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    text = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    answer = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    normalized_answer = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    reward = table.Column<int>(type: "integer", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    asked_total_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    correct_total_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    last_asked_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_question_definitions", x => x.id);
                    table.CheckConstraint("ck_question_definitions_asked_total_non_negative", "asked_total_count >= 0");
                    table.CheckConstraint("ck_question_definitions_correct_total_non_negative", "correct_total_count >= 0");
                    table.CheckConstraint("ck_question_definitions_counts_relation", "correct_total_count <= asked_total_count");
                    table.CheckConstraint("ck_question_definitions_reward_non_negative", "reward >= 0");
                    table.CheckConstraint("ck_question_definitions_soft_delete_semantics", "(is_deleted = FALSE AND deleted_at_utc IS NULL) OR (is_deleted = TRUE AND deleted_at_utc IS NOT NULL)");
                    table.ForeignKey(
                        name: "fk_question_definitions_question_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "question_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "game_user_notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    modifier_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    actor_display_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    quiz_points_delta = table.Column<int>(type: "integer", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    read_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_game_user_notifications", x => x.id);
                    table.CheckConstraint("ck_game_user_notifications_quiz_points_delta_non_negative", "quiz_points_delta IS NULL OR quiz_points_delta >= 0");
                    table.ForeignKey(
                        name: "fk_game_user_notifications_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<short>(type: "smallint", nullable: false),
                    assigned_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    assigned_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_roles", x => new { x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "fk_user_roles_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_roles_users_assigned_by_user_id",
                        column: x => x.assigned_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_user_roles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "game_board_cell_media",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    cell_id = table.Column<Guid>(type: "uuid", nullable: false),
                    media_asset_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_game_board_cell_media", x => x.id);
                    table.ForeignKey(
                        name: "fk_game_board_cell_media_media_assets_media_asset_id",
                        column: x => x.media_asset_id,
                        principalTable: "media_assets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "game_board_cells",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    board_id = table.Column<Guid>(type: "uuid", nullable: false),
                    row_index = table.Column<int>(type: "integer", nullable: false),
                    col_index = table.Column<int>(type: "integer", nullable: false),
                    state = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    cell_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    cost = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_game_board_cells", x => x.id);
                    table.CheckConstraint("ck_game_board_cells_state_allowed", "state IN ('open','closed')");
                });

            migrationBuilder.CreateTable(
                name: "game_boards",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    game_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    rows = table.Column<int>(type: "integer", nullable: false),
                    cols = table.Column<int>(type: "integer", nullable: false),
                    row_labels = table.Column<string[]>(type: "jsonb", nullable: false),
                    col_labels = table.Column<string[]>(type: "jsonb", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_game_boards", x => x.id);
                    table.CheckConstraint("ck_game_boards_dimensions_positive", "rows > 0 AND cols > 0");
                    table.CheckConstraint("ck_game_boards_labels_match_dimensions", "jsonb_array_length(row_labels) = rows AND jsonb_array_length(col_labels) = cols");
                });

            migrationBuilder.CreateTable(
                name: "game_enabled_modifiers",
                columns: table => new
                {
                    game_id = table.Column<Guid>(type: "uuid", nullable: false),
                    modifier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    enabled_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_game_enabled_modifiers", x => new { x.game_id, x.modifier_id });
                    table.ForeignKey(
                        name: "fk_game_enabled_modifiers_modifier_definitions_modifier_id",
                        column: x => x.modifier_id,
                        principalTable: "modifier_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "game_enabled_questions",
                columns: table => new
                {
                    game_id = table.Column<Guid>(type: "uuid", nullable: false),
                    question_id = table.Column<Guid>(type: "uuid", nullable: false),
                    enabled_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_game_enabled_questions", x => new { x.game_id, x.question_id });
                    table.ForeignKey(
                        name: "fk_game_enabled_questions_question_definitions_question_id",
                        column: x => x.question_id,
                        principalTable: "question_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "game_modifier_activations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    game_id = table.Column<Guid>(type: "uuid", nullable: false),
                    modifier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    activated_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    activation_cost_snapshot = table.Column<int>(type: "integer", nullable: false),
                    activated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    archived_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_game_modifier_activations", x => x.id);
                    table.CheckConstraint("ck_game_modifier_activations_cost_snapshot_non_negative", "activation_cost_snapshot >= 0");
                    table.ForeignKey(
                        name: "fk_game_modifier_activations_modifier_definitions_modifier_id",
                        column: x => x.modifier_id,
                        principalTable: "modifier_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_game_modifier_activations_users_activated_by_user_id",
                        column: x => x.activated_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "game_quiz_rounds",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    game_id = table.Column<Guid>(type: "uuid", nullable: false),
                    question_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ask_order = table.Column<int>(type: "integer", nullable: false),
                    asked_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    asked_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    answered_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    answered_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    answered_for_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    answered_by_display_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    submitted_answer = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_correct = table.Column<bool>(type: "boolean", nullable: true),
                    awarded_points = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_game_quiz_rounds", x => x.id);
                    table.CheckConstraint("ck_game_quiz_rounds_answer_semantics", "((status = 'asked') AND answered_at_utc IS NULL AND answered_by_user_id IS NULL AND answered_for_user_id IS NULL AND is_correct IS NULL AND awarded_points IS NULL) OR ((status = 'answered_correct') AND answered_at_utc IS NOT NULL AND answered_by_user_id IS NOT NULL AND answered_for_user_id IS NOT NULL AND is_correct = TRUE AND awarded_points IS NOT NULL) OR ((status = 'answered_wrong') AND answered_at_utc IS NOT NULL AND answered_by_user_id IS NOT NULL AND answered_for_user_id IS NOT NULL AND is_correct = FALSE AND awarded_points = 0) OR ((status IN ('timeout','skipped')) AND answered_at_utc IS NULL AND answered_by_user_id IS NULL AND answered_for_user_id IS NULL AND is_correct IS NULL AND awarded_points IS NULL)");
                    table.CheckConstraint("ck_game_quiz_rounds_ask_order_positive", "ask_order > 0");
                    table.CheckConstraint("ck_game_quiz_rounds_awarded_points_non_negative_or_null", "awarded_points IS NULL OR awarded_points >= 0");
                    table.CheckConstraint("ck_game_quiz_rounds_status_allowed", "status IN ('asked','answered_correct','answered_wrong','timeout','skipped')");
                    table.ForeignKey(
                        name: "fk_game_quiz_rounds_question_definitions_question_id",
                        column: x => x.question_id,
                        principalTable: "question_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_game_quiz_rounds_users_answered_by_user_id",
                        column: x => x.answered_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_game_quiz_rounds_users_answered_for_user_id",
                        column: x => x.answered_for_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_game_quiz_rounds_users_asked_by_user_id",
                        column: x => x.asked_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "game_quiz_manual_awards",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    game_id = table.Column<Guid>(type: "uuid", nullable: false),
                    awarded_to_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    awarded_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    points = table.Column<int>(type: "integer", nullable: false),
                    awarded_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_game_quiz_manual_awards", x => x.id);
                    table.CheckConstraint("ck_game_quiz_manual_awards_points_positive", "points > 0");
                    table.ForeignKey(
                        name: "fk_game_quiz_manual_awards_users_awarded_by_user_id",
                        column: x => x.awarded_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_game_quiz_manual_awards_users_awarded_to_user_id",
                        column: x => x.awarded_to_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "game_round_cell_media",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    round_id = table.Column<Guid>(type: "uuid", nullable: false),
                    url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_game_round_cell_media", x => x.id);
                    table.CheckConstraint("ck_game_round_cell_media_sort_order_non_negative", "sort_order >= 0");
                    table.CheckConstraint("ck_game_round_cell_media_url_not_blank", "length(trim(url)) > 0");
                });

            migrationBuilder.CreateTable(
                name: "game_round_modifier_results",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    round_id = table.Column<Guid>(type: "uuid", nullable: false),
                    modifier_activation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    modifier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    modifier_name_snapshot = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    modifier_category_snapshot = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    modifier_mechanic_type_snapshot = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    modifier_description_snapshot = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    modifier_scoring_type_snapshot = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    modifier_effect_snapshot_json = table.Column<string>(type: "jsonb", nullable: true),
                    outcome_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    score_delta = table.Column<int>(type: "integer", nullable: false),
                    kill_delta = table.Column<int>(type: "integer", nullable: false),
                    multiplier_applied = table.Column<decimal>(type: "numeric", nullable: true),
                    resolution_data_json = table.Column<string>(type: "jsonb", nullable: true),
                    resolved_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    resolved_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_game_round_modifier_results", x => x.id);
                    table.CheckConstraint("ck_game_round_modifier_results_resolution_semantics", "((outcome_status = 'pending') AND resolved_at_utc IS NULL AND resolved_by_user_id IS NULL) OR ((outcome_status <> 'pending') AND resolved_at_utc IS NOT NULL AND resolved_by_user_id IS NOT NULL)");
                    table.CheckConstraint("ck_game_round_modifier_results_status_allowed", "outcome_status IN ('pending','completed','failed','cancelled')");
                    table.ForeignKey(
                        name: "fk_game_round_modifier_results_modifier_definitions_modifier_id",
                        column: x => x.modifier_id,
                        principalTable: "modifier_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_game_round_modifier_results_users_resolved_by_user_id",
                        column: x => x.resolved_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_game_round_modifier_results_game_modifier_activations_modifier_activation_id",
                        column: x => x.modifier_activation_id,
                        principalTable: "game_modifier_activations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "game_round_participants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    round_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name_snapshot = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_game_round_participants", x => x.id);
                    table.ForeignKey(
                        name: "fk_game_round_participants_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "game_rounds",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    game_id = table.Column<Guid>(type: "uuid", nullable: false),
                    board_cell_id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    started_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    finished_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    base_score = table.Column<int>(type: "integer", nullable: false),
                    final_score = table.Column<int>(type: "integer", nullable: true),
                    kills_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    bounty_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    team_slot_index_snapshot = table.Column<int>(type: "integer", nullable: false),
                    cell_row_index = table.Column<int>(type: "integer", nullable: false),
                    cell_col_index = table.Column<int>(type: "integer", nullable: false),
                    cell_title_snapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    cell_description_snapshot = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    cell_cost_snapshot = table.Column<int>(type: "integer", nullable: false),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    resolved_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_game_rounds", x => x.id);
                    table.CheckConstraint("ck_game_rounds_base_score_non_negative", "base_score >= 0");
                    table.CheckConstraint("ck_game_rounds_bounty_count_non_negative", "bounty_count >= 0");
                    table.CheckConstraint("ck_game_rounds_cell_cost_non_negative", "cell_cost_snapshot >= 0");
                    table.CheckConstraint("ck_game_rounds_finished_at_semantics", "((status IN ('awaiting_modifiers','in_progress','reviewing_results')) AND finished_at_utc IS NULL) OR ((status IN ('completed','cancelled')) AND finished_at_utc IS NOT NULL)");
                    table.CheckConstraint("ck_game_rounds_kills_count_non_negative", "kills_count >= 0");
                    table.CheckConstraint("ck_game_rounds_resolution_semantics", "((status IN ('awaiting_modifiers','in_progress','reviewing_results')) AND final_score IS NULL AND resolved_by_user_id IS NULL) OR ((status = 'completed') AND final_score IS NOT NULL AND resolved_by_user_id IS NOT NULL) OR ((status = 'cancelled') AND final_score = 0 AND resolved_by_user_id IS NOT NULL)");
                    table.CheckConstraint("ck_game_rounds_row_col_non_negative", "cell_row_index >= 0 AND cell_col_index >= 0");
                    table.CheckConstraint("ck_game_rounds_status_allowed", "status IN ('awaiting_modifiers','in_progress','reviewing_results','completed','cancelled')");
                    table.CheckConstraint("ck_game_rounds_team_slot_non_negative", "team_slot_index_snapshot >= 0");
                    table.ForeignKey(
                        name: "fk_game_rounds_game_board_cells_board_cell_id",
                        column: x => x.board_cell_id,
                        principalTable: "game_board_cells",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_game_rounds_users_resolved_by_user_id",
                        column: x => x.resolved_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "game_team_invitations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    game_id = table.Column<Guid>(type: "uuid", nullable: false),
                    slot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_id = table.Column<Guid>(type: "uuid", nullable: true),
                    invited_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invited_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invited_by_kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    responded_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_game_team_invitations", x => x.id);
                    table.CheckConstraint("ck_game_team_invitations_invited_by_kind", "invited_by_kind IN ('admin','member')");
                    table.CheckConstraint("ck_game_team_invitations_response_timestamp_semantics", "((status = 'pending') AND responded_at_utc IS NULL) OR ((status <> 'pending') AND responded_at_utc IS NOT NULL)");
                    table.CheckConstraint("ck_game_team_invitations_status", "status IN ('pending','accepted','declined','cancelled','expired')");
                    table.ForeignKey(
                        name: "fk_game_team_invitations_users_invited_by_user_id",
                        column: x => x.invited_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_game_team_invitations_users_invited_user_id",
                        column: x => x.invited_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "game_team_members",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    game_id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    joined_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    left_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_game_team_members", x => x.id);
                    table.CheckConstraint("ck_game_team_members_left_after_join", "left_at_utc IS NULL OR left_at_utc >= joined_at_utc");
                    table.ForeignKey(
                        name: "fk_game_team_members_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "game_team_slots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    game_id = table.Column<Guid>(type: "uuid", nullable: false),
                    slot_index = table.Column<int>(type: "integer", nullable: false),
                    slot_type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    reserved_label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_game_team_slots", x => x.id);
                    table.UniqueConstraint("ak_game_team_slots_game_id_id", x => new { x.game_id, x.id });
                    table.CheckConstraint("ck_game_team_slots_slot_type", "slot_type IN ('public','reserved')");
                });

            migrationBuilder.CreateTable(
                name: "game_teams",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    game_id = table.Column<Guid>(type: "uuid", nullable: false),
                    slot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recruitment_open = table.Column<bool>(type: "boolean", nullable: false),
                    is_played = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    confirmed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    confirmed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    rejected_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rejected_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    disbanded_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    disbanded_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    disband_requested_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    disband_requested_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_game_teams", x => x.id);
                    table.UniqueConstraint("ak_game_teams_game_id_id", x => new { x.game_id, x.id });
                    table.CheckConstraint("ck_game_teams_disband_request_user_pair", "(disband_requested_at_utc IS NULL AND disband_requested_by_user_id IS NULL) OR (disband_requested_at_utc IS NOT NULL AND disband_requested_by_user_id IS NOT NULL)");
                    table.CheckConstraint("ck_game_teams_status_allowed", "status IN ('forming','confirmed','rejected','disbanded')");
                    table.CheckConstraint("ck_game_teams_status_timestamp_semantics", "((status = 'forming') AND confirmed_at_utc IS NULL AND rejected_at_utc IS NULL AND disbanded_at_utc IS NULL AND disband_requested_at_utc IS NULL) OR ((status = 'confirmed') AND confirmed_at_utc IS NOT NULL AND confirmed_by_user_id IS NOT NULL AND rejected_at_utc IS NULL AND disbanded_at_utc IS NULL) OR ((status = 'rejected') AND rejected_at_utc IS NOT NULL AND rejected_by_user_id IS NOT NULL AND disbanded_at_utc IS NULL AND disband_requested_at_utc IS NULL) OR ((status = 'disbanded') AND disbanded_at_utc IS NOT NULL AND disbanded_by_user_id IS NOT NULL AND disband_requested_at_utc IS NULL)");
                    table.ForeignKey(
                        name: "fk_game_teams_game_team_slots_game_id_slot_id",
                        columns: x => new { x.game_id, x.slot_id },
                        principalTable: "game_team_slots",
                        principalColumns: new[] { "game_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_game_teams_users_confirmed_by_user_id",
                        column: x => x.confirmed_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_game_teams_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_game_teams_users_disband_requested_by_user_id",
                        column: x => x.disband_requested_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_game_teams_users_disbanded_by_user_id",
                        column: x => x.disbanded_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_game_teams_users_rejected_by_user_id",
                        column: x => x.rejected_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "games",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ready_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    started_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    finished_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    min_players_per_team = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1),
                    max_players_per_team = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)2),
                    active_team_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_games", x => x.id);
                    table.CheckConstraint("ck_games_active_team_requires_active_game", "(active_team_id IS NULL) OR (status = 'active' AND is_deleted = FALSE)");
                    table.CheckConstraint("ck_games_finished_at_semantics", "((status IN ('draft','ready','active')) AND finished_at_utc IS NULL) OR ((status = 'finished') AND finished_at_utc IS NOT NULL)");
                    table.CheckConstraint("ck_games_lifecycle_timestamps", "((status = 'draft') AND ready_at_utc IS NULL AND started_at_utc IS NULL AND finished_at_utc IS NULL) OR ((status = 'ready') AND ready_at_utc IS NOT NULL AND started_at_utc IS NULL AND finished_at_utc IS NULL) OR ((status = 'active') AND ready_at_utc IS NOT NULL AND started_at_utc IS NOT NULL AND finished_at_utc IS NULL) OR ((status = 'finished') AND ready_at_utc IS NOT NULL AND started_at_utc IS NOT NULL AND finished_at_utc IS NOT NULL)");
                    table.CheckConstraint("ck_games_soft_delete_semantics", "(is_deleted = FALSE AND deleted_at_utc IS NULL) OR (is_deleted = TRUE AND deleted_at_utc IS NOT NULL)");
                    table.CheckConstraint("ck_games_status_allowed", "status IN ('draft','ready','active','finished')");
                    table.CheckConstraint("ck_games_team_size_limits", "min_players_per_team > 0 AND max_players_per_team >= min_players_per_team");
                    table.ForeignKey(
                        name: "fk_games_active_team_same_game",
                        columns: x => new { x.id, x.active_team_id },
                        principalTable: "game_teams",
                        principalColumns: new[] { "game_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "modifier_definitions",
                columns: new[] { "id", "activation_command", "activation_cost", "category", "created_at_utc", "default_limit_per_game", "description", "icon_emoji", "metadata_json", "name", "scoring_type", "updated_at_utc" },
                values: new object[] { new Guid("10000000-0000-0000-0000-000000000001"), "!активировать чирик", 3, "round", new DateTime(2026, 6, 7, 0, 0, 0, 0, DateTimeKind.Utc), 5, "Первые 60 секунд разрешено перемещаться только на корточках.", "💰", "{\"effect\":{\"mechanicType\":\"rule_only\",\"traits\":[],\"durationSeconds\":60,\"ruleText\":null,\"scoreImpact\":null,\"conditions\":[],\"resolutionInputs\":[],\"killEffect\":null,\"multiplierEffect\":null,\"mentorEffect\":null}}", "Чирик", "non_scoring", new DateTime(2026, 6, 7, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.InsertData(
                table: "modifier_definitions",
                columns: new[] { "id", "activation_command", "activation_cost", "category", "created_at_utc", "default_limit_per_game", "description", "icon_emoji", "metadata_json", "name", "requires_host_control", "scoring_type", "updated_at_utc" },
                values: new object[] { new Guid("10000000-0000-0000-0000-000000000002"), "!активировать жажда", 3, "result", new DateTime(2026, 6, 7, 0, 0, 0, 0, DateTimeKind.Utc), 2, "Убийства дают нарастающий бонус +5, миссия без убийств даёт штраф 25.", "💉", "{\"effect\":{\"mechanicType\":\"restriction_with_reward\",\"traits\":[\"requires_manual_resolution\"],\"durationSeconds\":null,\"ruleText\":null,\"scoreImpact\":{\"pointsDelta\":null,\"perKillBonus\":5,\"failurePenaltyPoints\":25,\"multiplierDelta\":null,\"killDelta\":null},\"conditions\":[{\"type\":\"at_least_one_kill\",\"source\":\"manual_input\"}],\"resolutionInputs\":[\"kills\"],\"killEffect\":null,\"multiplierEffect\":null,\"mentorEffect\":null}}", "Жажда", true, "conditional_bonus_penalty", new DateTime(2026, 6, 7, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.InsertData(
                table: "modifier_definitions",
                columns: new[] { "id", "activation_command", "activation_cost", "category", "created_at_utc", "default_limit_per_game", "description", "icon_emoji", "metadata_json", "name", "scoring_type", "updated_at_utc" },
                values: new object[] { new Guid("10000000-0000-0000-0000-000000000003"), "!активировать расходник", 4, "preparation", new DateTime(2026, 6, 7, 0, 0, 0, 0, DateTimeKind.Utc), 4, "Игроки могут заменить один расходник на свой выбор.", "🎯", "{\"effect\":{\"mechanicType\":\"rule_only\",\"traits\":[],\"durationSeconds\":null,\"ruleText\":null,\"scoreImpact\":null,\"conditions\":[],\"resolutionInputs\":[],\"killEffect\":null,\"multiplierEffect\":null,\"mentorEffect\":null}}", "Расходник", "non_scoring", new DateTime(2026, 6, 7, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.InsertData(
                table: "modifier_definitions",
                columns: new[] { "id", "activation_command", "activation_cost", "category", "created_at_utc", "default_limit_per_game", "description", "icon_emoji", "metadata_json", "name", "requires_host_control", "scoring_type", "updated_at_utc" },
                values: new object[] { new Guid("10000000-0000-0000-0000-000000000004"), "!активировать трупы", 4, "round", new DateTime(2026, 6, 7, 0, 0, 0, 0, DateTimeKind.Utc), 1, "Запрет на сжигание трупов.", "🔥", "{\"effect\":{\"mechanicType\":\"rule_only\",\"traits\":[],\"durationSeconds\":null,\"ruleText\":null,\"scoreImpact\":null,\"conditions\":[],\"resolutionInputs\":[],\"killEffect\":null,\"multiplierEffect\":null,\"mentorEffect\":null}}", "Трупы", true, "non_scoring", new DateTime(2026, 6, 7, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.InsertData(
                table: "modifier_definitions",
                columns: new[] { "id", "activation_command", "activation_cost", "category", "created_at_utc", "default_limit_per_game", "description", "icon_emoji", "metadata_json", "name", "scoring_type", "updated_at_utc" },
                values: new object[] { new Guid("10000000-0000-0000-0000-000000000005"), "!активировать навыки", 4, "preparation", new DateTime(2026, 6, 7, 0, 0, 0, 0, DateTimeKind.Utc), 5, "Количество доступных очков навыков уменьшено на 20% (-2 при 10).", "⚙️", "{\"effect\":{\"mechanicType\":\"rule_only\",\"traits\":[],\"durationSeconds\":null,\"ruleText\":null,\"scoreImpact\":null,\"conditions\":[],\"resolutionInputs\":[],\"killEffect\":null,\"multiplierEffect\":null,\"mentorEffect\":null}}", "Навыки", "non_scoring", new DateTime(2026, 6, 7, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.InsertData(
                table: "modifier_definitions",
                columns: new[] { "id", "activation_command", "activation_cost", "category", "created_at_utc", "default_limit_per_game", "description", "icon_emoji", "metadata_json", "name", "requires_host_control", "scoring_type", "updated_at_utc" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000006"), "!активировать патрон", 4, "result", new DateTime(2026, 6, 7, 0, 0, 0, 0, DateTimeKind.Utc), 1, "Если враг убит первой пулей, команда получает +1 убийство в счётчик.", "🔫", "{\"effect\":{\"mechanicType\":\"kill_counter\",\"traits\":[\"requires_manual_resolution\"],\"durationSeconds\":null,\"ruleText\":null,\"scoreImpact\":{\"pointsDelta\":null,\"perKillBonus\":null,\"failurePenaltyPoints\":null,\"multiplierDelta\":null,\"killDelta\":1},\"conditions\":[{\"type\":\"first_kill_first_bullet\",\"source\":\"manual_input\"}],\"resolutionInputs\":[\"kills\"],\"killEffect\":{\"killDeltaMode\":\"conditional_bonus_kill\",\"killDeltaValue\":1,\"condition\":\"first_kill_first_bullet\",\"excludedWeapons\":[\"лук\",\"арбалет\",\"дробовик\"]},\"multiplierEffect\":null,\"mentorEffect\":null}}", "Патрон", true, "conditional_bonus", new DateTime(2026, 6, 7, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("10000000-0000-0000-0000-000000000007"), "!активировать проказник", 6, "round", new DateTime(2026, 6, 7, 0, 0, 0, 0, DateTimeKind.Utc), 2, "Ментор пакостит 5 минут или пока не кончатся обманки.", "🙊", "{\"effect\":{\"mechanicType\":\"mentor\",\"traits\":[\"requires_manual_resolution\"],\"durationSeconds\":300,\"ruleText\":null,\"scoreImpact\":null,\"conditions\":[],\"resolutionInputs\":[\"mentorStatus\"],\"killEffect\":null,\"multiplierEffect\":null,\"mentorEffect\":{\"loadoutText\":\"Обманки и полтергейст\",\"durationSeconds\":300,\"canBeRevived\":false,\"canBeKilled\":false,\"killsCreditToTeam\":false}}}", "Проказник", true, "non_scoring", new DateTime(2026, 6, 7, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("10000000-0000-0000-0000-000000000008"), "!активировать диарея", 7, "round", new DateTime(2026, 6, 7, 0, 0, 0, 0, DateTimeKind.Utc), 1, "При упоминании/обнаружении туалета игрок обязан зайти в него (если нет врага в поле зрения).", "💩", "{\"effect\":{\"mechanicType\":\"rule_only\",\"traits\":[],\"durationSeconds\":null,\"ruleText\":null,\"scoreImpact\":null,\"conditions\":[],\"resolutionInputs\":[],\"killEffect\":null,\"multiplierEffect\":null,\"mentorEffect\":null}}", "Диарея", true, "non_scoring", new DateTime(2026, 6, 7, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("10000000-0000-0000-0000-000000000009"), "!активировать менторбайт", 8, "round", new DateTime(2026, 6, 7, 0, 0, 0, 0, DateTimeKind.Utc), 1, "Ментор с шумелками на 5 минут, команда решает как использовать.", "📣", "{\"effect\":{\"mechanicType\":\"mentor\",\"traits\":[\"requires_manual_resolution\"],\"durationSeconds\":300,\"ruleText\":null,\"scoreImpact\":null,\"conditions\":[],\"resolutionInputs\":[\"mentorStatus\"],\"killEffect\":null,\"multiplierEffect\":null,\"mentorEffect\":{\"loadoutText\":\"Набор шумелок\",\"durationSeconds\":300,\"canBeRevived\":false,\"canBeKilled\":true,\"killsCreditToTeam\":false}}}", "Менторбайт", true, "non_scoring", new DateTime(2026, 6, 7, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("10000000-0000-0000-0000-00000000000a"), "!активировать кэп", 10, "round", new DateTime(2026, 6, 7, 0, 0, 0, 0, DateTimeKind.Utc), 1, "Только капитан команды может пользоваться голосовым чатом.", "🔇", "{\"effect\":{\"mechanicType\":\"rule_only\",\"traits\":[],\"durationSeconds\":null,\"ruleText\":null,\"scoreImpact\":null,\"conditions\":[],\"resolutionInputs\":[],\"killEffect\":null,\"multiplierEffect\":null,\"mentorEffect\":null}}", "Кэп", true, "non_scoring", new DateTime(2026, 6, 7, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("10000000-0000-0000-0000-00000000000b"), "!активировать фейерверк", 11, "round", new DateTime(2026, 6, 7, 0, 0, 0, 0, DateTimeKind.Utc), 1, "Ментор раз в минуту стреляет осветительными снарядами в небо 5 минут.", "🎆", "{\"effect\":{\"mechanicType\":\"mentor\",\"traits\":[\"requires_manual_resolution\"],\"durationSeconds\":300,\"ruleText\":null,\"scoreImpact\":null,\"conditions\":[],\"resolutionInputs\":[\"mentorStatus\"],\"killEffect\":null,\"multiplierEffect\":null,\"mentorEffect\":{\"loadoutText\":\"Оружие с осветительными снарядами\",\"durationSeconds\":300,\"canBeRevived\":false,\"canBeKilled\":false,\"killsCreditToTeam\":false}}}", "Фейерверк", true, "non_scoring", new DateTime(2026, 6, 7, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("10000000-0000-0000-0000-00000000000c"), "!активировать крыса", 12, "result", new DateTime(2026, 6, 7, 0, 0, 0, 0, DateTimeKind.Utc), 1, "Ментор с полным набором ловушек; убийства ментора идут в счёт команды.", "🐀", "{\"effect\":{\"mechanicType\":\"mentor\",\"traits\":[\"requires_manual_resolution\",\"kill_counter\"],\"durationSeconds\":null,\"ruleText\":null,\"scoreImpact\":{\"pointsDelta\":null,\"perKillBonus\":null,\"failurePenaltyPoints\":null,\"multiplierDelta\":null,\"killDelta\":null},\"conditions\":[],\"resolutionInputs\":[\"mentorKills\"],\"killEffect\":{\"killDeltaMode\":\"mentor_kills_as_team_kills\",\"killDeltaValue\":1,\"condition\":null,\"excludedWeapons\":[]},\"multiplierEffect\":null,\"mentorEffect\":{\"loadoutText\":\"Менторское снаряжение\",\"durationSeconds\":null,\"canBeRevived\":false,\"canBeKilled\":true,\"killsCreditToTeam\":true}}}", "Крыса", true, "conditional_bonus", new DateTime(2026, 6, 7, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("10000000-0000-0000-0000-00000000000d"), "!активировать шот", 13, "result", new DateTime(2026, 6, 7, 0, 0, 0, 0, DateTimeKind.Utc), null, "Ментор получает оружие с одним выстрелом, убийство идёт в счёт команды.", "🥠", "{\"effect\":{\"mechanicType\":\"mentor\",\"traits\":[\"requires_manual_resolution\",\"kill_counter\"],\"durationSeconds\":null,\"ruleText\":null,\"scoreImpact\":{\"pointsDelta\":null,\"perKillBonus\":null,\"failurePenaltyPoints\":null,\"multiplierDelta\":null,\"killDelta\":null},\"conditions\":[],\"resolutionInputs\":[\"mentorKills\"],\"killEffect\":{\"killDeltaMode\":\"mentor_kills_as_team_kills\",\"killDeltaValue\":1,\"condition\":null,\"excludedWeapons\":[]},\"multiplierEffect\":null,\"mentorEffect\":{\"loadoutText\":\"Менторское снаряжение\",\"durationSeconds\":null,\"canBeRevived\":false,\"canBeKilled\":true,\"killsCreditToTeam\":true}}}", "Шот", true, "conditional_bonus", new DateTime(2026, 6, 7, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("10000000-0000-0000-0000-00000000000e"), "!активировать подъём", 14, "round", new DateTime(2026, 6, 7, 0, 0, 0, 0, DateTimeKind.Utc), 1, "Нельзя поднимать союзника, пока не убит враг.", "☠️", "{\"effect\":{\"mechanicType\":\"rule_only\",\"traits\":[],\"durationSeconds\":null,\"ruleText\":null,\"scoreImpact\":null,\"conditions\":[],\"resolutionInputs\":[],\"killEffect\":null,\"multiplierEffect\":null,\"mentorEffect\":null}}", "Подъём", true, "non_scoring", new DateTime(2026, 6, 7, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("10000000-0000-0000-0000-00000000000f"), "!активировать хард75", 18, "result", new DateTime(2026, 6, 7, 0, 0, 0, 0, DateTimeKind.Utc), 1, "Каждое убийство получает множитель +0.75 до восстановления полосок.", "💀", "{\"effect\":{\"mechanicType\":\"multiplier\",\"traits\":[\"requires_manual_resolution\"],\"durationSeconds\":null,\"ruleText\":null,\"scoreImpact\":{\"pointsDelta\":null,\"perKillBonus\":null,\"failurePenaltyPoints\":null,\"multiplierDelta\":0.75,\"killDelta\":null},\"conditions\":[{\"type\":\"until_health_restored\",\"source\":\"manual_input\"}],\"resolutionInputs\":[\"killsDuringWindow\"],\"killEffect\":null,\"multiplierEffect\":{\"target\":\"kills\",\"delta\":0.75,\"activeWindow\":\"until_condition\",\"stopCondition\":\"health_restored\"},\"mentorEffect\":null}}", "Хард75", true, "multiplier", new DateTime(2026, 6, 7, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "id", "code", "created_at_utc", "description", "name", "updated_at_utc" },
                values: new object[,]
                {
                    { (short)1, "viewer", new DateTime(2026, 3, 23, 0, 0, 0, 0, DateTimeKind.Utc), "Viewer role with basic registration capabilities.", "Viewer", new DateTime(2026, 3, 23, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { (short)2, "moderator", new DateTime(2026, 3, 23, 0, 0, 0, 0, DateTimeKind.Utc), "Moderator role that helps manage game operations.", "Moderator", new DateTime(2026, 3, 23, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { (short)3, "admin", new DateTime(2026, 3, 23, 0, 0, 0, 0, DateTimeKind.Utc), "Administrator role with full management access.", "Administrator", new DateTime(2026, 3, 23, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "modifier_conflicts",
                columns: new[] { "conflicts_with_modifier_id", "modifier_id" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000009"), new Guid("10000000-0000-0000-0000-000000000007") },
                    { new Guid("10000000-0000-0000-0000-00000000000c"), new Guid("10000000-0000-0000-0000-000000000007") },
                    { new Guid("10000000-0000-0000-0000-00000000000d"), new Guid("10000000-0000-0000-0000-000000000007") },
                    { new Guid("10000000-0000-0000-0000-00000000000c"), new Guid("10000000-0000-0000-0000-000000000009") }
                });

            migrationBuilder.CreateIndex(
                name: "ix_game_board_cell_media_cell_id",
                table: "game_board_cell_media",
                column: "cell_id");

            migrationBuilder.CreateIndex(
                name: "ix_game_board_cell_media_cell_id_sort_order",
                table: "game_board_cell_media",
                columns: new[] { "cell_id", "sort_order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_game_board_cell_media_media_asset_id",
                table: "game_board_cell_media",
                column: "media_asset_id");

            migrationBuilder.CreateIndex(
                name: "ix_game_board_cells_board_id",
                table: "game_board_cells",
                column: "board_id");

            migrationBuilder.CreateIndex(
                name: "ix_game_board_cells_board_id_row_index_col_index",
                table: "game_board_cells",
                columns: new[] { "board_id", "row_index", "col_index" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_game_board_cells_state",
                table: "game_board_cells",
                column: "state");

            migrationBuilder.CreateIndex(
                name: "ix_game_boards_game_id",
                table: "game_boards",
                column: "game_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_game_enabled_modifiers_game_id",
                table: "game_enabled_modifiers",
                column: "game_id");

            migrationBuilder.CreateIndex(
                name: "ix_game_enabled_modifiers_modifier_id",
                table: "game_enabled_modifiers",
                column: "modifier_id");

            migrationBuilder.CreateIndex(
                name: "ix_game_enabled_questions_game_id",
                table: "game_enabled_questions",
                column: "game_id");

            migrationBuilder.CreateIndex(
                name: "ix_game_enabled_questions_question_id",
                table: "game_enabled_questions",
                column: "question_id");

            migrationBuilder.CreateIndex(
                name: "ix_game_modifier_activations_modifier_id",
                table: "game_modifier_activations",
                column: "modifier_id");

            migrationBuilder.CreateIndex(
                name: "ix_game_modifier_activations_game_activated",
                table: "game_modifier_activations",
                columns: new[] { "game_id", "activated_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_game_modifier_activations_game_archived",
                table: "game_modifier_activations",
                columns: new[] { "game_id", "archived_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_game_modifier_activations_game_modifier",
                table: "game_modifier_activations",
                columns: new[] { "game_id", "modifier_id" });

            migrationBuilder.CreateIndex(
                name: "ix_game_modifier_activations_user_activated",
                table: "game_modifier_activations",
                columns: new[] { "activated_by_user_id", "activated_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_game_quiz_rounds_answered_by_user_id_answered_at_utc",
                table: "game_quiz_rounds",
                columns: new[] { "answered_by_user_id", "answered_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_game_quiz_rounds_answered_for_user_id_answered_at_utc",
                table: "game_quiz_rounds",
                columns: new[] { "answered_for_user_id", "answered_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_game_quiz_rounds_asked_by_user_id_asked_at_utc",
                table: "game_quiz_rounds",
                columns: new[] { "asked_by_user_id", "asked_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_game_quiz_rounds_game_id_ask_order",
                table: "game_quiz_rounds",
                columns: new[] { "game_id", "ask_order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_game_quiz_rounds_game_id_asked_at_utc",
                table: "game_quiz_rounds",
                columns: new[] { "game_id", "asked_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_game_quiz_rounds_game_id_question_id",
                table: "game_quiz_rounds",
                columns: new[] { "game_id", "question_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_game_quiz_rounds_game_id_status",
                table: "game_quiz_rounds",
                columns: new[] { "game_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_game_quiz_rounds_question_id",
                table: "game_quiz_rounds",
                column: "question_id");

            migrationBuilder.CreateIndex(
                name: "ix_game_quiz_manual_awards_awarded_by_user_id_awarded_at_utc",
                table: "game_quiz_manual_awards",
                columns: new[] { "awarded_by_user_id", "awarded_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_game_quiz_manual_awards_awarded_to_user_id_awarded_at_utc",
                table: "game_quiz_manual_awards",
                columns: new[] { "awarded_to_user_id", "awarded_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_game_quiz_manual_awards_game_id_awarded_at_utc",
                table: "game_quiz_manual_awards",
                columns: new[] { "game_id", "awarded_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ux_game_round_cell_media_round_sort_order",
                table: "game_round_cell_media",
                columns: new[] { "round_id", "sort_order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_game_round_modifier_results_modifier_activation_id",
                table: "game_round_modifier_results",
                column: "modifier_activation_id");

            migrationBuilder.CreateIndex(
                name: "ix_game_round_modifier_results_modifier_status",
                table: "game_round_modifier_results",
                columns: new[] { "modifier_id", "outcome_status" });

            migrationBuilder.CreateIndex(
                name: "ix_game_round_modifier_results_resolved_by_user_id",
                table: "game_round_modifier_results",
                column: "resolved_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_game_round_modifier_results_round_status",
                table: "game_round_modifier_results",
                columns: new[] { "round_id", "outcome_status" });

            migrationBuilder.CreateIndex(
                name: "ux_game_round_modifier_results_round_activation",
                table: "game_round_modifier_results",
                columns: new[] { "round_id", "modifier_activation_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_game_round_participants_user_created",
                table: "game_round_participants",
                columns: new[] { "user_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ux_game_round_participants_round_user",
                table: "game_round_participants",
                columns: new[] { "round_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_game_rounds_board_cell_id_started_at_utc",
                table: "game_rounds",
                columns: new[] { "board_cell_id", "started_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_game_rounds_game_id_started_at_utc",
                table: "game_rounds",
                columns: new[] { "game_id", "started_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_game_rounds_game_id_team_id_board_cell_id_started_at_utc",
                table: "game_rounds",
                columns: new[] { "game_id", "team_id", "board_cell_id", "started_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_game_rounds_resolved_by_user_id",
                table: "game_rounds",
                column: "resolved_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_game_rounds_team_id_started_at_utc",
                table: "game_rounds",
                columns: new[] { "team_id", "started_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_game_team_invitations_game_id_slot_id",
                table: "game_team_invitations",
                columns: new[] { "game_id", "slot_id" });

            migrationBuilder.CreateIndex(
                name: "ix_game_team_invitations_game_id_status",
                table: "game_team_invitations",
                columns: new[] { "game_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_game_team_invitations_game_id_team_id",
                table: "game_team_invitations",
                columns: new[] { "game_id", "team_id" });

            migrationBuilder.CreateIndex(
                name: "ix_game_team_invitations_invited_by_user_id",
                table: "game_team_invitations",
                column: "invited_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_game_team_invitations_invited_user_id_status",
                table: "game_team_invitations",
                columns: new[] { "invited_user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_game_team_invitations_one_pending_per_user",
                table: "game_team_invitations",
                columns: new[] { "game_id", "invited_user_id" },
                unique: true,
                filter: "status = 'pending'");

            migrationBuilder.CreateIndex(
                name: "ix_game_team_members_game_id_team_id",
                table: "game_team_members",
                columns: new[] { "game_id", "team_id" });

            migrationBuilder.CreateIndex(
                name: "ix_game_team_members_team_id_user_id",
                table: "game_team_members",
                columns: new[] { "team_id", "user_id" });

            migrationBuilder.CreateIndex(
                name: "ix_game_team_members_user_id",
                table: "game_team_members",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ux_game_team_members_active_game_user",
                table: "game_team_members",
                columns: new[] { "game_id", "user_id" },
                unique: true,
                filter: "left_at_utc IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_game_team_members_active_team_user",
                table: "game_team_members",
                columns: new[] { "team_id", "user_id" },
                unique: true,
                filter: "left_at_utc IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_game_team_slots_game_id_slot_type",
                table: "game_team_slots",
                columns: new[] { "game_id", "slot_type" });

            migrationBuilder.CreateIndex(
                name: "ix_game_team_slots_game_id_slot_index",
                table: "game_team_slots",
                columns: new[] { "game_id", "slot_index" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_game_teams_confirmed_by_user_id",
                table: "game_teams",
                column: "confirmed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_game_teams_created_by_user_id",
                table: "game_teams",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_game_teams_disband_requested_by_user_id",
                table: "game_teams",
                column: "disband_requested_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_game_teams_disbanded_by_user_id",
                table: "game_teams",
                column: "disbanded_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_game_teams_game_id_slot_id",
                table: "game_teams",
                columns: new[] { "game_id", "slot_id" });

            migrationBuilder.CreateIndex(
                name: "ix_game_teams_game_id_status",
                table: "game_teams",
                columns: new[] { "game_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_game_teams_rejected_by_user_id",
                table: "game_teams",
                column: "rejected_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ux_game_teams_active_slot",
                table: "game_teams",
                column: "slot_id",
                unique: true,
                filter: "status IN ('forming','confirmed')");

            migrationBuilder.CreateIndex(
                name: "ix_game_user_notifications_user_id_read_at_utc_created_at_utc",
                table: "game_user_notifications",
                columns: new[] { "user_id", "read_at_utc", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_game_user_notifications_user_id_type_created_at_utc",
                table: "game_user_notifications",
                columns: new[] { "user_id", "type", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_games_active_team_same_game",
                table: "games",
                columns: new[] { "id", "active_team_id" });

            migrationBuilder.CreateIndex(
                name: "ix_games_created_at_utc",
                table: "games",
                column: "created_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_games_is_deleted_status_created_at_utc",
                table: "games",
                columns: new[] { "is_deleted", "status", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ux_games_single_active",
                table: "games",
                column: "status",
                unique: true,
                filter: "status = 'active' AND is_deleted = FALSE");

            migrationBuilder.CreateIndex(
                name: "ux_games_single_draft",
                table: "games",
                column: "status",
                unique: true,
                filter: "status = 'draft' AND is_deleted = FALSE");

            migrationBuilder.CreateIndex(
                name: "ux_games_single_ready",
                table: "games",
                column: "status",
                unique: true,
                filter: "status = 'ready' AND is_deleted = FALSE");

            migrationBuilder.CreateIndex(
                name: "ix_media_assets_bucket_object_key",
                table: "media_assets",
                columns: new[] { "bucket", "object_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_media_assets_status",
                table: "media_assets",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_modifier_conflicts_conflicts_with_modifier_id",
                table: "modifier_conflicts",
                column: "conflicts_with_modifier_id");

            migrationBuilder.CreateIndex(
                name: "ix_question_categories_name",
                table: "question_categories",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_questions_active_pick_queue",
                table: "question_definitions",
                columns: new[] { "is_deleted", "is_enabled", "asked_total_count", "priority" });

            migrationBuilder.CreateIndex(
                name: "ix_questions_category_enabled",
                table: "question_definitions",
                columns: new[] { "category_id", "is_enabled" });

            migrationBuilder.CreateIndex(
                name: "ix_questions_priority",
                table: "question_definitions",
                column: "priority");

            migrationBuilder.CreateIndex(
                name: "ux_questions_external_code",
                table: "question_definitions",
                column: "external_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_roles_code",
                table: "roles",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_roles_assigned_by_user_id",
                table: "user_roles",
                column: "assigned_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_roles_expires_at_utc",
                table: "user_roles",
                column: "expires_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_user_roles_role_id",
                table: "user_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_login",
                table: "users",
                column: "login");

            migrationBuilder.CreateIndex(
                name: "ix_users_twitch_user_id",
                table: "users",
                column: "twitch_user_id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_game_board_cell_media_game_board_cells_cell_id",
                table: "game_board_cell_media",
                column: "cell_id",
                principalTable: "game_board_cells",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_game_board_cells_game_boards_board_id",
                table: "game_board_cells",
                column: "board_id",
                principalTable: "game_boards",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_game_boards_games_game_id",
                table: "game_boards",
                column: "game_id",
                principalTable: "games",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_game_enabled_modifiers_games_game_id",
                table: "game_enabled_modifiers",
                column: "game_id",
                principalTable: "games",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_game_enabled_questions_games_game_id",
                table: "game_enabled_questions",
                column: "game_id",
                principalTable: "games",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_game_modifier_activations_games_game_id",
                table: "game_modifier_activations",
                column: "game_id",
                principalTable: "games",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_game_quiz_rounds_games_game_id",
                table: "game_quiz_rounds",
                column: "game_id",
                principalTable: "games",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_game_quiz_manual_awards_games_game_id",
                table: "game_quiz_manual_awards",
                column: "game_id",
                principalTable: "games",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_game_round_cell_media_game_rounds_round_id",
                table: "game_round_cell_media",
                column: "round_id",
                principalTable: "game_rounds",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_game_round_modifier_results_game_rounds_round_id",
                table: "game_round_modifier_results",
                column: "round_id",
                principalTable: "game_rounds",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_game_round_participants_game_rounds_round_id",
                table: "game_round_participants",
                column: "round_id",
                principalTable: "game_rounds",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_game_rounds_game_teams_team_id",
                table: "game_rounds",
                column: "team_id",
                principalTable: "game_teams",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_game_rounds_games_game_id",
                table: "game_rounds",
                column: "game_id",
                principalTable: "games",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_game_team_invitations_game_team_slots_game_id_slot_id",
                table: "game_team_invitations",
                columns: new[] { "game_id", "slot_id" },
                principalTable: "game_team_slots",
                principalColumns: new[] { "game_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_game_team_invitations_game_teams_game_id_team_id",
                table: "game_team_invitations",
                columns: new[] { "game_id", "team_id" },
                principalTable: "game_teams",
                principalColumns: new[] { "game_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_game_team_invitations_games_game_id",
                table: "game_team_invitations",
                column: "game_id",
                principalTable: "games",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_game_team_members_game_teams_game_id_team_id",
                table: "game_team_members",
                columns: new[] { "game_id", "team_id" },
                principalTable: "game_teams",
                principalColumns: new[] { "game_id", "id" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_game_team_members_games_game_id",
                table: "game_team_members",
                column: "game_id",
                principalTable: "games",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_game_team_slots_games_game_id",
                table: "game_team_slots",
                column: "game_id",
                principalTable: "games",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_game_teams_games_game_id",
                table: "game_teams",
                column: "game_id",
                principalTable: "games",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_game_team_slots_games_game_id",
                table: "game_team_slots");

            migrationBuilder.DropForeignKey(
                name: "fk_game_teams_games_game_id",
                table: "game_teams");

            migrationBuilder.DropTable(
                name: "game_board_cell_media");

            migrationBuilder.DropTable(
                name: "game_enabled_modifiers");

            migrationBuilder.DropTable(
                name: "game_enabled_questions");

            migrationBuilder.DropTable(
                name: "game_quiz_rounds");

            migrationBuilder.DropTable(
                name: "game_quiz_manual_awards");

            migrationBuilder.DropTable(
                name: "game_round_cell_media");

            migrationBuilder.DropTable(
                name: "game_round_modifier_results");

            migrationBuilder.DropTable(
                name: "game_round_participants");

            migrationBuilder.DropTable(
                name: "game_team_invitations");

            migrationBuilder.DropTable(
                name: "game_team_members");

            migrationBuilder.DropTable(
                name: "game_user_notifications");

            migrationBuilder.DropTable(
                name: "modifier_conflicts");

            migrationBuilder.DropTable(
                name: "user_roles");

            migrationBuilder.DropTable(
                name: "media_assets");

            migrationBuilder.DropTable(
                name: "question_definitions");

            migrationBuilder.DropTable(
                name: "game_modifier_activations");

            migrationBuilder.DropTable(
                name: "game_rounds");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "question_categories");

            migrationBuilder.DropTable(
                name: "modifier_definitions");

            migrationBuilder.DropTable(
                name: "game_board_cells");

            migrationBuilder.DropTable(
                name: "game_boards");

            migrationBuilder.DropTable(
                name: "games");

            migrationBuilder.DropTable(
                name: "game_teams");

            migrationBuilder.DropTable(
                name: "game_team_slots");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
