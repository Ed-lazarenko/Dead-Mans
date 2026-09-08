using backend.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Data.Configurations;

public class GameEnabledQuestionConfiguration : IEntityTypeConfiguration<GameEnabledQuestion>
{
    public void Configure(EntityTypeBuilder<GameEnabledQuestion> builder)
    {
        builder.ToTable(
            "game_enabled_questions",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_game_enabled_questions_revision_positive",
                    "question_revision_snapshot > 0"
                );
                table.HasCheckConstraint(
                    "ck_game_enabled_questions_reward_non_negative",
                    "reward_snapshot >= 0"
                );
                table.HasCheckConstraint(
                    "ck_game_enabled_questions_content_not_blank",
                    "length(trim(question_code_snapshot)) > 0 "
                    + "AND length(trim(category_name_snapshot)) > 0 "
                    + "AND length(trim(question_text_snapshot)) > 0"
                );
                table.HasCheckConstraint(
                    "ck_game_enabled_questions_answers_present",
                    "cardinality(accepted_answers_snapshot) > 0 "
                    + "AND cardinality(accepted_answers_snapshot) = cardinality(normalized_answers_snapshot)"
                );
            }
        );

        builder.HasKey(x => new { x.GameId, x.QuestionId });
        builder.Property(x => x.EnabledAtUtc).IsRequired();
        builder.Property(x => x.QuestionRevisionSnapshot).IsRequired();
        builder.Property(x => x.QuestionCodeSnapshot).HasMaxLength(64).IsRequired();
        builder.Property(x => x.CategoryNameSnapshot).HasMaxLength(64).IsRequired();
        builder.Property(x => x.QuestionTextSnapshot).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.AcceptedAnswersSnapshot).HasColumnType("text[]").IsRequired();
        builder.Property(x => x.NormalizedAnswersSnapshot).HasColumnType("text[]").IsRequired();
        builder.Property(x => x.RewardSnapshot).IsRequired();
        builder.Property(x => x.PrioritySnapshot).IsRequired();
        builder.Property(x => x.SnapshotAtUtc).IsRequired();

        builder.HasIndex(x => x.QuestionId);

        builder.HasOne(x => x.Game)
            .WithMany(x => x.EnabledQuestions)
            .HasForeignKey(x => x.GameId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.QuestionDefinition)
            .WithMany(x => x.EnabledInGames)
            .HasForeignKey(x => x.QuestionId)
            .HasPrincipalKey(x => x.Id)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
