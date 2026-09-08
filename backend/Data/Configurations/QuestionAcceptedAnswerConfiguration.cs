using backend.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Data.Configurations;

public sealed class QuestionAcceptedAnswerConfiguration
    : IEntityTypeConfiguration<QuestionAcceptedAnswer>
{
    public void Configure(EntityTypeBuilder<QuestionAcceptedAnswer> builder)
    {
        builder.ToTable(
            "question_accepted_answers",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_question_accepted_answers_text_not_blank",
                    "length(trim(answer_text)) > 0 AND length(trim(normalized_answer)) > 0"
                );
                table.HasCheckConstraint(
                    "ck_question_accepted_answers_sort_order_non_negative",
                    "sort_order >= 0"
                );
            }
        );

        builder.HasKey(x => x.Id);
        builder.Property(x => x.AnswerText).HasMaxLength(500).IsRequired();
        builder.Property(x => x.NormalizedAnswer).HasMaxLength(500).IsRequired();
        builder.Property(x => x.IsPrimary).IsRequired();
        builder.Property(x => x.SortOrder).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();

        builder.HasIndex(x => new { x.QuestionId, x.NormalizedAnswer }).IsUnique();
        builder.HasIndex(x => new { x.QuestionId, x.SortOrder }).IsUnique();
        builder
            .HasIndex(x => x.QuestionId, "ux_question_accepted_answers_one_primary")
            .IsUnique()
            .HasFilter("is_primary = TRUE");
        builder
            .HasIndex(x => x.AnswerText, "ix_question_accepted_answers_text_trgm")
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");

        builder
            .HasOne(x => x.Question)
            .WithMany(x => x.AcceptedAnswers)
            .HasForeignKey(x => x.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
