using backend.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Data.Configurations;

public class QuestionCategoryConfiguration : IEntityTypeConfiguration<QuestionCategory>
{
    public void Configure(EntityTypeBuilder<QuestionCategory> builder)
    {
        builder.ToTable(
            "question_categories",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_question_categories_name_not_blank",
                    "length(trim(\"Name\")) > 0"
                );
            }
        );

        builder.HasKey(x => x.Name);

        builder.Property(x => x.Name).HasMaxLength(64).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();
    }
}
