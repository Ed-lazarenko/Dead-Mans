using backend.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Data.Configurations;

public sealed class GameFinalizationConfiguration : IEntityTypeConfiguration<GameFinalization>
{
    public void Configure(EntityTypeBuilder<GameFinalization> builder)
    {
        builder.ToTable(
            "game_finalizations",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_game_finalizations_calculation_version_positive",
                    "calculation_version > 0"
                );
                table.HasCheckConstraint(
                    "ck_game_finalizations_counts_non_negative",
                    "completed_round_count >= 0 AND cancelled_round_count >= 0 AND skipped_quiz_question_count >= 0"
                );
            }
        );

        builder.HasKey(x => x.GameId);
        builder.HasIndex(x => x.RequestId).IsUnique();
        builder.Property(x => x.FinishedByDisplayNameSnapshot).HasMaxLength(128).IsRequired();
        builder.Property(x => x.PublicNote).HasMaxLength(2000);

        builder
            .HasOne(x => x.Game)
            .WithOne(x => x.Finalization)
            .HasForeignKey<GameFinalization>(x => x.GameId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(x => x.FinishedByUser)
            .WithMany()
            .HasForeignKey(x => x.FinishedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
