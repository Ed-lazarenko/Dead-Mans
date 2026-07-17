using backend.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Data.Configurations;

public class GameQuizManualAwardConfiguration : IEntityTypeConfiguration<GameQuizManualAward>
{
    public void Configure(EntityTypeBuilder<GameQuizManualAward> builder)
    {
        builder.ToTable(
            "game_quiz_manual_awards",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_game_quiz_manual_awards_points_positive",
                    "\"Points\" > 0"
                );
            }
        );

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Points).IsRequired();
        builder.Property(x => x.AwardedAtUtc).IsRequired();

        builder.HasIndex(x => new { x.GameId, x.AwardedAtUtc });
        builder.HasIndex(x => new { x.AwardedToUserId, x.AwardedAtUtc });
        builder.HasIndex(x => new { x.AwardedByUserId, x.AwardedAtUtc });

        builder
            .HasOne(x => x.Game)
            .WithMany()
            .HasForeignKey(x => x.GameId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(x => x.AwardedToUser)
            .WithMany()
            .HasForeignKey(x => x.AwardedToUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(x => x.AwardedByUser)
            .WithMany()
            .HasForeignKey(x => x.AwardedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
