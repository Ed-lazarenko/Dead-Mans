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
                    "ck_game_user_notifications_identity_not_blank",
                    "length(trim(type)) > 0 AND length(trim(deduplication_key)) > 0"
                );
                tableBuilder.HasCheckConstraint(
                    "ck_game_user_notifications_read_after_create",
                    "read_at_utc IS NULL OR read_at_utc >= created_at_utc"
                );
                tableBuilder.HasCheckConstraint(
                    "ck_game_user_notifications_payload_envelope",
                    "schema_version > 0 AND jsonb_typeof(payload_json) = 'object'"
                );
                tableBuilder.HasCheckConstraint(
                    "ck_game_user_notifications_modifier_cancelled_v1_payload",
                    "type <> 'modifier_cancelled' OR (schema_version = 1 "
                    + "AND jsonb_typeof(payload_json -> 'modifierActivationId') = 'string' "
                    + "AND length(trim(payload_json ->> 'modifierActivationId')) > 0 "
                    + "AND jsonb_typeof(payload_json -> 'modifierName') = 'string' "
                    + "AND length(trim(payload_json ->> 'modifierName')) > 0 "
                    + "AND jsonb_typeof(payload_json -> 'actorDisplayName') = 'string' "
                    + "AND length(trim(payload_json ->> 'actorDisplayName')) > 0 "
                    + "AND jsonb_typeof(payload_json -> 'quizPointsDelta') = 'number' "
                    + "AND (payload_json ->> 'quizPointsDelta')::integer >= 0)"
                );
            }
        );

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Type).HasMaxLength(64).IsRequired();
        builder.Property(x => x.SchemaVersion).IsRequired();
        builder.Property(x => x.PayloadJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.DeduplicationKey).HasMaxLength(160).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();

        builder.HasIndex(x => new { x.UserId, x.ReadAtUtc, x.CreatedAtUtc });
        builder.HasIndex(x => new { x.UserId, x.Type, x.CreatedAtUtc });
        builder
            .HasIndex(
                x => new { x.UserId, x.DeduplicationKey },
                "ux_game_user_notifications_deduplication"
            )
            .IsUnique();

        builder.HasOne(x => x.User)
            .WithMany(x => x.GameNotifications)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Game)
            .WithMany()
            .HasForeignKey(x => x.GameId)
            .OnDelete(DeleteBehavior.Restrict);

    }
}
