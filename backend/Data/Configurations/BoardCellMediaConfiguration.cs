using backend.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Data.Configurations;

public class BoardCellMediaConfiguration : IEntityTypeConfiguration<BoardCellMedia>
{
    public void Configure(EntityTypeBuilder<BoardCellMedia> builder)
    {
        builder.ToTable("board_cell_media");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Role).HasMaxLength(32).IsRequired();
        builder.Property(x => x.SortOrder).IsRequired();

        builder.HasIndex(x => x.CellId);
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
