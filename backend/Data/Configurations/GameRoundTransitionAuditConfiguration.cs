using backend.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Data.Configurations;

public sealed class GameRoundTransitionAuditConfiguration
    : IEntityTypeConfiguration<GameRoundTransitionAudit>
{
    public void Configure(EntityTypeBuilder<GameRoundTransitionAudit> builder)
    {
        builder.ToTable(
            "game_round_transition_audits",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "ck_game_round_transition_audits_sequence_positive",
                    "sequence > 0"
                );
                tableBuilder.HasCheckConstraint(
                    "ck_game_round_transition_audits_resulting_version_positive",
                    "resulting_round_version > 0"
                );
            }
        );

        builder.HasKey(x => new { x.RoundId, x.Sequence });
        builder.Property(x => x.FromStatus).HasMaxLength(32);
        builder.Property(x => x.ToStatus).HasMaxLength(32).IsRequired();
        builder.Property(x => x.ActionCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(2000);
        builder.Property(x => x.OccurredAtUtc).IsRequired();
        builder.Property(x => x.ResultingRoundVersion).IsRequired();

        builder.HasIndex(x => new { x.RoundId, x.ResultingRoundVersion }).IsUnique();
        builder.HasIndex(x => x.InitiatedByUserId);

        builder
            .HasOne(x => x.Round)
            .WithMany(x => x.TransitionAudits)
            .HasForeignKey(x => x.RoundId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(x => x.InitiatedByUser)
            .WithMany()
            .HasForeignKey(x => x.InitiatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
