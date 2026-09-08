using backend.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Data.Configurations;

public sealed class GameRoundCellMediaConfiguration : IEntityTypeConfiguration<GameRoundCellMedia>
{
    public void Configure(EntityTypeBuilder<GameRoundCellMedia> builder)
    {
        builder.ToTable(
            "game_round_cell_media",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "ck_game_round_cell_media_sort_order_non_negative",
                    "sort_order >= 0"
                );
                tableBuilder.HasCheckConstraint(
                    "ck_game_round_cell_media_storage_identity_not_blank",
                    "length(trim(bucket)) > 0 AND length(trim(object_key)) > 0"
                );
                tableBuilder.HasCheckConstraint(
                    "ck_game_round_cell_media_mime_type_not_blank",
                    "length(trim(mime_type)) > 0"
                );
                tableBuilder.HasCheckConstraint(
                    "ck_game_round_cell_media_role_not_blank",
                    "length(trim(role)) > 0"
                );
                tableBuilder.HasCheckConstraint(
                    "ck_game_round_cell_media_size_positive",
                    "size_bytes > 0"
                );
            }
        );

        builder.HasKey(x => x.Id);
        builder.Property(x => x.RoundId).HasColumnName("round_id");
        builder.Property(x => x.Bucket).HasMaxLength(128).IsRequired();
        builder.Property(x => x.ObjectKey).HasMaxLength(1024).IsRequired();
        builder.Property(x => x.MimeType).HasMaxLength(256).IsRequired();
        builder.Property(x => x.SizeBytes).IsRequired();
        builder.Property(x => x.Role).HasMaxLength(32).IsRequired();
        builder.Property(x => x.SortOrder).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();

        builder
            .HasIndex(x => new { x.RoundId, x.SortOrder })
            .IsUnique()
            .HasDatabaseName("ux_game_round_cell_media_round_sort_order");

        builder
            .HasOne(x => x.Round)
            .WithMany(x => x.CellMedia)
            .HasForeignKey(x => x.RoundId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
