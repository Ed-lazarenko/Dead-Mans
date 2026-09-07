using backend.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Data.Configurations;

/// <summary>
/// Maps the stable modifier identity. Mutable game content is stored only in immutable versions.
/// </summary>
public sealed class ModifierDefinitionConfiguration : IEntityTypeConfiguration<ModifierDefinition>
{
    private static readonly DateTime SeedTimestamp = new(2026, 6, 7, 0, 0, 0, DateTimeKind.Utc);

    public void Configure(EntityTypeBuilder<ModifierDefinition> builder)
    {
        builder.ToTable("modifier_definitions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.IsArchived).HasDefaultValue(false);
        builder.Property(x => x.CurrentVersionId);
        builder.Property(x => x.CreatedByUserId);
        builder.Property(x => x.ArchivedAtUtc);
        builder.Property(x => x.ArchivedByUserId);
        builder.Property(x => x.CreatedAtUtc).IsRequired();

        builder.HasIndex(x => x.CurrentVersionId).IsUnique();
        builder.HasIndex(x => new { x.IsArchived, x.CreatedAtUtc, x.Id })
            .IsDescending(false, true, true);
        builder.HasIndex(x => new { x.CreatedAtUtc, x.Id })
            .IsDescending(true, true);
        builder.HasOne(x => x.CurrentVersion)
            .WithMany()
            .HasForeignKey(x => new { x.Id, x.CurrentVersionId })
            .HasPrincipalKey(x => new { x.ModifierId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.CreatedByUser)
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ArchivedByUser)
            .WithMany()
            .HasForeignKey(x => x.ArchivedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasData(
            StableSeed(ModifierDefinitionSeedIds.Chirik),
            StableSeed(ModifierDefinitionSeedIds.Zhazhda),
            StableSeed(ModifierDefinitionSeedIds.Rashodnik),
            StableSeed(ModifierDefinitionSeedIds.Trupy),
            StableSeed(ModifierDefinitionSeedIds.Navyki),
            StableSeed(ModifierDefinitionSeedIds.Patron),
            StableSeed(ModifierDefinitionSeedIds.Prokaznik),
            StableSeed(ModifierDefinitionSeedIds.Diareya),
            StableSeed(ModifierDefinitionSeedIds.Mentorbait),
            StableSeed(ModifierDefinitionSeedIds.Kep),
            StableSeed(ModifierDefinitionSeedIds.Feyerverk),
            StableSeed(ModifierDefinitionSeedIds.Krysa),
            StableSeed(ModifierDefinitionSeedIds.Shot),
            StableSeed(ModifierDefinitionSeedIds.Podem),
            StableSeed(ModifierDefinitionSeedIds.Hard75)
        );
    }

    private static ModifierDefinition StableSeed(Guid id) =>
        new()
        {
            Id = id,
            CreatedAtUtc = SeedTimestamp
        };
}
