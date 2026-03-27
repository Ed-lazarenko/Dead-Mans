using backend.Data.Entities;
using backend.Domain.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Data.Configurations;

public class MediaAssetConfiguration : IEntityTypeConfiguration<MediaAsset>
{
    public void Configure(EntityTypeBuilder<MediaAsset> builder)
    {
        builder.ToTable(
            "media_assets",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_media_assets_scope_allowed",
                    MediaAssetPersistence.CheckSqlAllowedScopes
                );
                table.HasCheckConstraint(
                    "CK_media_assets_status_allowed",
                    MediaAssetPersistence.CheckSqlAllowedStatuses
                );
            }
        );

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Bucket).HasMaxLength(128).IsRequired();
        builder.Property(x => x.ObjectKey).HasMaxLength(1024).IsRequired();
        builder.Property(x => x.MimeType).HasMaxLength(256).IsRequired();
        builder.Property(x => x.SizeBytes).IsRequired();
        builder.Property(x => x.Scope).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();

        builder.HasIndex(x => new { x.Bucket, x.ObjectKey }).IsUnique();
        builder.HasIndex(x => x.Status);
    }
}
