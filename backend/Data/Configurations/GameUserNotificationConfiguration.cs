using backend.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Data.Configurations;

public sealed class GameUserNotificationConfiguration : IEntityTypeConfiguration<GameUserNotification>
{
    public void Configure(EntityTypeBuilder<GameUserNotification> builder)
    {
        builder.ToTable(
            "game_user_notifications",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "ck_game_user_notifications_quiz_points_delta_non_negative",
                    "quiz_points_delta IS NULL OR quiz_points_delta >= 0"
                );
            }
        );

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Type).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ModifierName).HasMaxLength(128);
        builder.Property(x => x.ActorDisplayName).HasMaxLength(128);
        builder.Property(x => x.CreatedAtUtc).IsRequired();

        builder.HasIndex(x => new { x.UserId, x.ReadAtUtc, x.CreatedAtUtc });
        builder.HasIndex(x => new { x.UserId, x.Type, x.CreatedAtUtc });

        builder.HasOne(x => x.User)
            .WithMany(x => x.GameNotifications)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
