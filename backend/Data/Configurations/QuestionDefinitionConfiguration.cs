using backend.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Data.Configurations;

public class QuestionDefinitionConfiguration : IEntityTypeConfiguration<QuestionDefinition>
{
    public void Configure(EntityTypeBuilder<QuestionDefinition> builder)
    {
        builder.ToTable(
            "question_definitions",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "ck_question_definitions_reward_non_negative",
                    "reward >= 0"
                );
                tableBuilder.HasCheckConstraint(
                    "ck_question_definitions_revision_positive",
                    "revision > 0"
                );
                tableBuilder.HasCheckConstraint(
                    "ck_question_definitions_soft_delete_semantics",
                    "(is_deleted = FALSE AND deleted_at_utc IS NULL) OR "
                    + "(is_deleted = TRUE AND is_enabled = FALSE AND deleted_at_utc IS NOT NULL)"
                );
                tableBuilder.HasCheckConstraint(
                    "ck_question_definitions_content_not_blank",
                    "length(trim(external_code)) > 0 AND length(trim(text)) > 0"
                );
                tableBuilder.HasCheckConstraint(
                    "ck_question_definitions_timestamps",
                    "updated_at_utc >= created_at_utc "
                    + "AND (deleted_at_utc IS NULL OR deleted_at_utc >= created_at_utc)"
                );
            }
        );

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.ExternalCode).HasColumnType("citext").HasMaxLength(64).IsRequired();
        builder.Property(x => x.CategoryId).IsRequired();
        builder.Property(x => x.Text).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.Reward).IsRequired();
        builder.Property(x => x.Revision).IsRequired().HasDefaultValue(1);
        builder.Property(x => x.IsEnabled).HasDefaultValue(true);
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.Property(x => x.DeletedAtUtc);
        builder.Property(x => x.Priority).HasDefaultValue(0).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();

        builder.HasIndex(x => x.ExternalCode).IsUnique().HasDatabaseName("ux_questions_external_code");
        builder
            .HasIndex(x => new { x.CategoryId, x.IsEnabled })
            .HasDatabaseName("ix_questions_category_enabled");
        builder
            .HasIndex(x => new { x.IsDeleted, x.IsEnabled, x.Priority })
            .HasDatabaseName("ix_questions_active_pick_queue");
        builder.HasIndex(x => x.Priority).HasDatabaseName("ix_questions_priority");
        builder
            .HasIndex(x => x.Text, "ix_questions_text_trgm")
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");

        builder
            .HasOne(x => x.CategoryDefinition)
            .WithMany(x => x.Questions)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
