using backend.Data.Entities;
using backend.Domain.Persistence;
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
                    "ck_game_quiz_manual_awards_points_nonzero",
                    "points <> 0"
                );
                tableBuilder.HasCheckConstraint(
                    "ck_game_quiz_manual_awards_operation_type",
                    "operation_type IN ('award', 'deduct')"
                );
                tableBuilder.HasCheckConstraint(
                    "ck_game_quiz_manual_awards_operation_sign",
                    "(operation_type = 'award' AND points > 0) OR (operation_type = 'deduct' AND points < 0)"
                );
                tableBuilder.HasCheckConstraint(
                    "ck_game_quiz_manual_awards_adjustment_audit",
                    "request_id IS NULL OR (reason IS NOT NULL AND length(trim(reason)) BETWEEN 3 AND 500 AND available_points_before IS NOT NULL AND available_points_after IS NOT NULL AND available_points_after >= 0)"
                );
            }
        );

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Points).IsRequired();
        builder.Property(x => x.OperationType)
            .HasMaxLength(16)
            .HasDefaultValue(GameQuizManualAdjustmentOperationValue.Award)
            .IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(500);
        builder.Property(x => x.AwardedAtUtc).IsRequired();

        builder.HasIndex(x => new { x.GameId, x.AwardedAtUtc });
        builder.HasIndex(x => new { x.AwardedToUserId, x.AwardedAtUtc });
        builder.HasIndex(x => new { x.AwardedByUserId, x.AwardedAtUtc });
        builder.HasIndex(x => x.RequestId).IsUnique().HasFilter("request_id IS NOT NULL");

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
