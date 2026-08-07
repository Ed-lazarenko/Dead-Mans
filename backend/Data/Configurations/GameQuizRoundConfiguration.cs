using backend.Data.Entities;
using backend.Domain.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Data.Configurations;

public class GameQuizRoundConfiguration : IEntityTypeConfiguration<GameQuizRound>
{
    public void Configure(EntityTypeBuilder<GameQuizRound> builder)
    {
        builder.ToTable(
            "game_quiz_rounds",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "ck_game_quiz_rounds_status_allowed",
                    GameQuizRoundStatusValue.CheckSqlAllowedStatuses
                );
                tableBuilder.HasCheckConstraint(
                    "ck_game_quiz_rounds_ask_order_positive",
                    "ask_order > 0"
                );
                tableBuilder.HasCheckConstraint(
                    "ck_game_quiz_rounds_awarded_points_non_negative_or_null",
                    "awarded_points IS NULL OR awarded_points >= 0"
                );
                tableBuilder.HasCheckConstraint(
                    "ck_game_quiz_rounds_answer_semantics",
                    "((status = 'asked') AND answered_at_utc IS NULL AND answered_by_user_id IS NULL AND answered_for_user_id IS NULL AND is_correct IS NULL AND awarded_points IS NULL) "
                    + "OR ((status = 'answered_correct') AND answered_at_utc IS NOT NULL AND answered_by_user_id IS NOT NULL AND answered_for_user_id IS NOT NULL AND is_correct = TRUE AND awarded_points IS NOT NULL) "
                    + "OR ((status = 'answered_wrong') AND answered_at_utc IS NOT NULL AND answered_by_user_id IS NOT NULL AND answered_for_user_id IS NOT NULL AND is_correct = FALSE AND awarded_points = 0) "
                    + "OR ((status IN ('timeout','skipped')) AND answered_at_utc IS NULL AND answered_by_user_id IS NULL AND answered_for_user_id IS NULL AND is_correct IS NULL AND awarded_points IS NULL)"
                );
            }
        );

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
        builder.Property(x => x.AnsweredByDisplayName).HasMaxLength(128);
        builder.Property(x => x.SubmittedAnswer).HasMaxLength(500);
        builder.Property(x => x.AskedAtUtc).IsRequired();
        builder.Property(x => x.AskOrder).IsRequired();

        builder.HasIndex(x => new { x.GameId, x.QuestionId }).IsUnique();
        builder.HasIndex(x => new { x.GameId, x.AskOrder }).IsUnique();
        builder.HasIndex(x => new { x.GameId, x.AskedAtUtc });
        builder.HasIndex(x => new { x.GameId, x.Status });
        builder.HasIndex(x => new { x.AnsweredForUserId, x.AnsweredAtUtc });
        builder.HasIndex(x => new { x.AnsweredByUserId, x.AnsweredAtUtc });
        builder.HasIndex(x => new { x.AskedByUserId, x.AskedAtUtc });

        builder
            .HasOne(x => x.Game)
            .WithMany()
            .HasForeignKey(x => x.GameId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(x => x.Question)
            .WithMany(x => x.AskedInQuizRounds)
            .HasForeignKey(x => x.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(x => x.AskedByUser)
            .WithMany(x => x.AskedGameQuizRounds)
            .HasForeignKey(x => x.AskedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(x => x.AnsweredByUser)
            .WithMany(x => x.AnsweredGameQuizRounds)
            .HasForeignKey(x => x.AnsweredByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(x => x.AnsweredForUser)
            .WithMany(x => x.CreditedGameQuizRounds)
            .HasForeignKey(x => x.AnsweredForUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
