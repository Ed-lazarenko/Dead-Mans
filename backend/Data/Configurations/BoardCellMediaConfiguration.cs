using backend.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Data.Configurations;

public class BoardCellMediaConfiguration : IEntityTypeConfiguration<BoardCellMedia>
{
    public void Configure(EntityTypeBuilder<BoardCellMedia> builder)
    {
        builder.ToTable(
            "game_board_cell_media",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "ck_game_board_cell_media_sort_order_non_negative",
                    "sort_order >= 0"
                );
                tableBuilder.HasCheckConstraint(
                    "ck_game_board_cell_media_role_not_blank",
                    "length(trim(role)) > 0"
                );
            }
        );

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Role).HasMaxLength(32).IsRequired();
        builder.Property(x => x.SortOrder).IsRequired();

        builder.HasIndex(x => x.MediaAssetId);
        builder.HasIndex(x => new { x.CellId, x.SortOrder }).IsUnique();

        builder.HasOne(x => x.Cell)
            .WithMany(x => x.MediaLinks)
            .HasForeignKey(x => x.CellId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.MediaAsset)
            .WithMany(x => x.CellLinks)
            .HasForeignKey(x => x.MediaAssetId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
