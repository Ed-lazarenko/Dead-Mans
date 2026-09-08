using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Data.Migrations;

[DbContext(typeof(ApplicationDbContext))]
internal class ApplicationDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
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
            b.Property<string>("Email").HasMaxLength(320).HasColumnType("character varying(320)")
                .HasColumnName("email");
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
                .HasConstraintName("fk_game_round_modifier_results_modifier_definitions_modifier_id");
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
