using backend.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Data.Configurations;

public class GameEnabledQuestionConfiguration : IEntityTypeConfiguration<GameEnabledQuestion>
{
    public void Configure(EntityTypeBuilder<GameEnabledQuestion> builder)
    {
        builder.ToTable("game_enabled_questions");

        builder.HasKey(x => new { x.GameId, x.QuestionId });
        builder.Property(x => x.EnabledAtUtc).IsRequired();

        builder.HasIndex(x => x.GameId);
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
