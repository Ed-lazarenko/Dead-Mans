using backend.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Data.Configurations;

public class GameBoardConfiguration : IEntityTypeConfiguration<GameBoard>
{
    public void Configure(EntityTypeBuilder<GameBoard> builder)
    {
        builder.ToTable("game_boards");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Version).IsRequired().HasDefaultValue(1);
        builder.Property(x => x.CreatedAtUtc).IsRequired();

        builder.HasIndex(x => x.GameId).IsUnique();

        builder.HasOne(x => x.Game)
            .WithOne(x => x.Board)
            .HasForeignKey<GameBoard>(x => x.GameId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
