using backend.Data.Entities;
using backend.Domain.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Data.Configurations;

public sealed class GameQuizPointLedgerEntryConfiguration
    : IEntityTypeConfiguration<GameQuizPointLedgerEntry>
{
    public void Configure(EntityTypeBuilder<GameQuizPointLedgerEntry> builder)
    {
        builder.ToTable(
            "game_quiz_point_ledger_entries",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_quiz_point_ledger_entry_type_allowed",
                    GameQuizPointEntryTypeValue.CheckSqlAllowed
                );
                table.HasCheckConstraint(
                    "ck_quiz_point_ledger_nonzero_delta",
                    "points_delta <> 0"
                );
                table.HasCheckConstraint(
                    "ck_quiz_point_ledger_balance_audit",
                    "available_points_before >= 0 AND available_points_after >= 0 "
                    + "AND available_points_after = available_points_before + points_delta"
                );
                table.HasCheckConstraint(
                    "ck_quiz_point_ledger_source_semantics",
                    "(entry_type = 'quiz_reward' AND points_delta > 0 "
                    + "AND correct_answer_id IS NOT NULL AND modifier_activation_id IS NULL "
                    + "AND manual_request_id IS NULL AND created_by_user_id IS NULL "
                    + "AND reason IS NULL) OR "
                    + "(entry_type = 'manual_adjustment' AND correct_answer_id IS NULL "
                    + "AND modifier_activation_id IS NULL AND manual_request_id IS NOT NULL "
                    + "AND created_by_user_id IS NOT NULL AND reason IS NOT NULL "
                    + "AND length(trim(reason)) BETWEEN 3 AND 500) OR "
                    + "(entry_type = 'modifier_purchase' AND points_delta < 0 "
                    + "AND correct_answer_id IS NULL AND modifier_activation_id IS NOT NULL "
                    + "AND manual_request_id IS NULL AND created_by_user_id IS NOT NULL "
                    + "AND reason IS NULL) OR "
                    + "(entry_type = 'modifier_refund' AND points_delta > 0 "
                    + "AND correct_answer_id IS NULL AND modifier_activation_id IS NOT NULL "
                    + "AND manual_request_id IS NULL AND created_by_user_id IS NOT NULL "
                    + "AND (reason IS NULL OR length(trim(reason)) BETWEEN 3 AND 500))"
                );
            }
        );

        builder.HasKey(x => x.Id);
        builder.Property(x => x.SequenceNumber).ValueGeneratedOnAdd();
        builder.Property(x => x.EntryType).HasMaxLength(32).IsRequired();
        builder.Property(x => x.PointsDelta).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(500);
        builder.Property(x => x.AvailablePointsBefore).HasColumnType("bigint").IsRequired();
        builder.Property(x => x.AvailablePointsAfter).HasColumnType("bigint").IsRequired();
        builder.Property(x => x.OccurredAtUtc).IsRequired();

        builder.HasIndex(x => x.SequenceNumber).IsUnique();
        builder
            .HasIndex(x => new { x.GameId, x.UserId, x.SequenceNumber })
            .HasDatabaseName("ix_quiz_ledger_game_user_sequence");
        builder.HasIndex(x => new { x.UserId, x.GameId });
        builder
            .HasIndex(x => x.CorrectAnswerId)
            .IsUnique()
            .HasFilter("correct_answer_id IS NOT NULL");
        builder
            .HasIndex(x => x.ManualRequestId)
            .IsUnique()
            .HasFilter("manual_request_id IS NOT NULL");
        builder
            .HasIndex(
                x => new { x.ModifierActivationId, x.EntryType },
                "ux_quiz_point_ledger_modifier_event"
            )
            .IsUnique()
            .HasFilter("modifier_activation_id IS NOT NULL");
        builder
            .HasIndex(x => new { x.GameId, x.ModifierActivationId })
            .HasDatabaseName("ix_quiz_ledger_game_activation");

        builder
            .HasOne(x => x.Game)
            .WithMany()
            .HasForeignKey(x => x.GameId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne(x => x.User)
            .WithMany(x => x.QuizPointLedgerEntries)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne(x => x.CreatedByUser)
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne(x => x.CorrectAnswer)
            .WithMany(x => x.PointEntries)
            .HasForeignKey(x => new { x.GameId, x.CorrectAnswerId })
            .HasPrincipalKey(x => new { x.GameId, x.Id })
            .HasConstraintName("fk_quiz_point_ledger_correct_answer_same_game")
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne(x => x.ModifierActivation)
            .WithMany()
            .HasForeignKey(x => new { x.GameId, x.ModifierActivationId })
            .HasPrincipalKey(x => new { x.GameId, x.Id })
            .HasConstraintName("fk_quiz_point_ledger_modifier_activation_same_game")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
