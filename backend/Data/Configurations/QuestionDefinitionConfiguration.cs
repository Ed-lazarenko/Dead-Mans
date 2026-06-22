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
                    "CK_question_definitions_reward_non_negative",
                    "\"Reward\" >= 0"
                );
                tableBuilder.HasCheckConstraint(
                    "CK_question_definitions_sort_order_non_negative",
                    "\"SortOrder\" >= 0"
                );
                tableBuilder.HasCheckConstraint(
                    "CK_question_definitions_asked_total_non_negative",
                    "\"AskedTotalCount\" >= 0"
                );
                tableBuilder.HasCheckConstraint(
                    "CK_question_definitions_correct_total_non_negative",
                    "\"CorrectTotalCount\" >= 0"
                );
                tableBuilder.HasCheckConstraint(
                    "CK_question_definitions_counts_relation",
                    "\"CorrectTotalCount\" <= \"AskedTotalCount\""
                );
                tableBuilder.HasCheckConstraint(
                    "CK_question_definitions_soft_delete_semantics",
                    "(\"IsDeleted\" = FALSE AND \"DeletedAtUtc\" IS NULL) OR (\"IsDeleted\" = TRUE AND \"DeletedAtUtc\" IS NOT NULL)"
                );
            }
        );

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.ExternalCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Category).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Text).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.Answer).HasMaxLength(500).IsRequired();
        builder.Property(x => x.NormalizedAnswer).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Reward).IsRequired();
        builder.Property(x => x.IsEnabled).HasDefaultValue(true);
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.Property(x => x.DeletedAtUtc);
        builder.Property(x => x.SortOrder).IsRequired();
        builder.Property(x => x.AskedTotalCount).HasDefaultValue(0);
        builder.Property(x => x.CorrectTotalCount).HasDefaultValue(0);
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();

        builder.HasIndex(x => x.ExternalCode).IsUnique();
        builder.HasIndex(x => new { x.Category, x.IsEnabled });
        builder.HasIndex(x => new { x.IsDeleted, x.IsEnabled, x.AskedTotalCount, x.LastAskedAtUtc });
        builder.HasIndex(x => x.SortOrder);

        builder
            .HasOne(x => x.CategoryDefinition)
            .WithMany(x => x.Questions)
            .HasForeignKey(x => x.Category)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
