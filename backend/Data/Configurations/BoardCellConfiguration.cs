using backend.Data.Entities;
using backend.Domain.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Data.Configurations;

public class BoardCellConfiguration : IEntityTypeConfiguration<BoardCell>
{
    public void Configure(EntityTypeBuilder<BoardCell> builder)
    {
        builder.ToTable(
            "game_board_cells",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "ck_game_board_cells_state_allowed",
                    BoardCellPersistence.CheckSqlAllowedStates
                );
            }
        );

        builder.HasKey(x => x.Id);

        builder.Property(x => x.RowIndex).IsRequired();
        builder.Property(x => x.ColIndex).IsRequired();
        builder.Property(x => x.State)
            .HasConversion(
                value => value == BoardCellState.Open
                    ? BoardCellPersistence.StateOpen
                    : value == BoardCellState.Cancelled
                        ? BoardCellPersistence.StateCancelled
                        : BoardCellPersistence.StateClosed,
                value => value == BoardCellPersistence.StateOpen
                    ? BoardCellState.Open
                    : value == BoardCellPersistence.StateCancelled
                        ? BoardCellState.Cancelled
                        : BoardCellState.Closed
            )
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(x => x.CellType).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(200);
        builder.Property(x => x.Cost).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000);

        builder.HasIndex(x => new { x.BoardId, x.RowIndex, x.ColIndex }).IsUnique();
        builder.HasIndex(x => x.BoardId);
        builder.HasIndex(x => x.State);

        builder.HasOne(x => x.Board)
            .WithMany(x => x.Cells)
            .HasForeignKey(x => x.BoardId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
