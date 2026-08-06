using backend.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Data.Configurations;

public class GameBoardConfiguration : IEntityTypeConfiguration<GameBoard>
{
    public void Configure(EntityTypeBuilder<GameBoard> builder)
    {
        builder.ToTable(
            "game_boards",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "ck_game_boards_dimensions_positive",
                    "rows > 0 AND cols > 0"
                );
                tableBuilder.HasCheckConstraint(
                    "ck_game_boards_labels_match_dimensions",
                    "jsonb_array_length(row_labels) = rows AND jsonb_array_length(col_labels) = cols"
                );
            }
        );

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Version).IsRequired().HasDefaultValue(1);
        builder.Property(x => x.Rows).IsRequired();
        builder.Property(x => x.Cols).IsRequired();
        builder.Property(x => x.RowLabels).IsRequired().HasColumnType("jsonb");
        builder.Property(x => x.ColLabels).IsRequired().HasColumnType("jsonb");
        builder.Property(x => x.CreatedAtUtc).IsRequired();

        builder.HasIndex(x => x.GameId).IsUnique();

        builder.HasOne(x => x.Game)
            .WithOne(x => x.Board)
            .HasForeignKey<GameBoard>(x => x.GameId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
