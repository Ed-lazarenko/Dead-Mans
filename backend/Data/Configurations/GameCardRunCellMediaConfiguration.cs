using backend.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Data.Configurations;

public sealed class GameCardRunCellMediaConfiguration : IEntityTypeConfiguration<GameCardRunCellMedia>
{
    public void Configure(EntityTypeBuilder<GameCardRunCellMedia> builder)
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
                    "ck_game_round_cell_media_url_not_blank",
                    "length(trim(url)) > 0"
                );
            }
        );

        builder.HasKey(x => x.Id);
        builder.Property(x => x.CardRunId).HasColumnName("round_id");
        builder.Property(x => x.Url).HasMaxLength(2048).IsRequired();
        builder.Property(x => x.SortOrder).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();

        builder
            .HasIndex(x => new { x.CardRunId, x.SortOrder })
            .IsUnique()
            .HasDatabaseName("ux_game_round_cell_media_round_sort_order");

        builder
            .HasOne(x => x.CardRun)
            .WithMany(x => x.CellMedia)
            .HasForeignKey(x => x.CardRunId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
