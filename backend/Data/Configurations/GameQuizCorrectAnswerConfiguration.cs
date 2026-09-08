using backend.Data.Entities;
using backend.Domain.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Data.Configurations;

public sealed class GameQuizCorrectAnswerConfiguration
    : IEntityTypeConfiguration<GameQuizCorrectAnswer>
{
    public void Configure(EntityTypeBuilder<GameQuizCorrectAnswer> builder)
    {
        builder.ToTable(
            "game_quiz_correct_answers",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_game_quiz_correct_answers_identity_snapshots_not_blank",
                    "length(trim(twitch_user_id_snapshot)) > 0 "
                    + "AND length(trim(login_snapshot)) > 0 "
                    + "AND length(trim(display_name_snapshot)) > 0"
                );
                table.HasCheckConstraint(
                    "ck_game_quiz_correct_answers_answer_not_blank",
                    "length(trim(submitted_answer)) > 0 AND length(trim(normalized_answer)) > 0"
                );
                table.HasCheckConstraint(
                    "ck_game_quiz_correct_answers_source_allowed",
                    GameQuizAnswerSourceValue.CheckSqlAllowed
                );
                table.HasCheckConstraint(
                    "ck_game_quiz_correct_answers_source_semantics",
                    "(source_provider = 'manual' AND source_channel_id IS NULL "
                    + "AND source_message_id IS NULL) OR "
                    + "(source_provider = 'twitch' AND source_channel_id IS NOT NULL "
                    + "AND source_message_id IS NOT NULL "
                    + "AND length(trim(source_channel_id)) > 0 "
                    + "AND length(trim(source_message_id)) > 0)"
                );
            }
        );

        builder.HasKey(x => x.Id);
        builder.HasAlternateKey(x => new { x.GameId, x.Id });
        builder.Property(x => x.TwitchUserIdSnapshot).HasMaxLength(64).IsRequired();
        builder.Property(x => x.LoginSnapshot).HasMaxLength(64).IsRequired();
        builder.Property(x => x.DisplayNameSnapshot).HasMaxLength(128).IsRequired();
        builder.Property(x => x.SubmittedAnswer).HasMaxLength(500).IsRequired();
        builder.Property(x => x.NormalizedAnswer).HasMaxLength(500).IsRequired();
        builder.Property(x => x.SourceProvider).HasMaxLength(32).IsRequired();
        builder.Property(x => x.SourceChannelId).HasMaxLength(128);
        builder.Property(x => x.SourceMessageId).HasMaxLength(128);
        builder.Property(x => x.AnsweredAtUtc).IsRequired();

        builder.HasIndex(x => x.QuizRoundId).IsUnique();
        builder
            .HasIndex(x => new { x.GameId, x.AwardedToUserId, x.AnsweredAtUtc })
            .HasDatabaseName("ix_quiz_answers_game_user_time");
        builder
            .HasIndex(
                x => new { x.SourceProvider, x.SourceChannelId, x.SourceMessageId },
                "ux_game_quiz_correct_answers_source_message"
            )
            .IsUnique()
            .HasFilter("source_channel_id IS NOT NULL AND source_message_id IS NOT NULL");

        builder
            .HasOne(x => x.QuizRound)
            .WithOne(x => x.CorrectAnswer)
            .HasForeignKey<GameQuizCorrectAnswer>(x => new { x.GameId, x.QuizRoundId })
            .HasPrincipalKey<GameQuizRound>(x => new { x.GameId, x.Id })
            .HasConstraintName("fk_quiz_correct_answers_round_same_game")
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne(x => x.AwardedToUser)
            .WithMany(x => x.CorrectQuizAnswers)
            .HasForeignKey(x => x.AwardedToUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne(x => x.CapturedByUser)
            .WithMany()
            .HasForeignKey(x => x.CapturedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
