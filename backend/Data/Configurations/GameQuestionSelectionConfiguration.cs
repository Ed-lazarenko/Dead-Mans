using backend.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Data.Configurations;

public class GameQuestionSelectionConfiguration : IEntityTypeConfiguration<GameQuestionSelection>
{
    public void Configure(EntityTypeBuilder<GameQuestionSelection> builder)
    {
        builder.ToTable("game_question_selections");

        builder.HasKey(x => new { x.GameId, x.QuestionId });
        builder.Property(x => x.EnabledAtUtc).IsRequired();

        builder.HasIndex(x => x.GameId);
        builder.HasIndex(x => x.QuestionId);

        builder.HasOne(x => x.Game)
            .WithMany(x => x.EnabledQuestions)
            .HasForeignKey(x => x.GameId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.QuestionDefinition)
            .WithMany(x => x.GameSelections)
            .HasForeignKey(x => x.QuestionId)
            .HasPrincipalKey(x => x.Id)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
