using backend.Data.Entities;
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
                    "ck_media_assets_storage_identity_not_blank",
                    "length(trim(bucket)) > 0 AND length(trim(object_key)) > 0"
                );
                table.HasCheckConstraint(
                    "ck_media_assets_mime_type_not_blank",
                    "length(trim(mime_type)) > 0"
                );
                table.HasCheckConstraint(
                    "ck_media_assets_size_positive",
                    "size_bytes > 0"
                );
            }
        );

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Bucket).HasMaxLength(128).IsRequired();
        builder.Property(x => x.ObjectKey).HasMaxLength(1024).IsRequired();
        builder.Property(x => x.MimeType).HasMaxLength(256).IsRequired();
        builder.Property(x => x.SizeBytes).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();

        builder.HasIndex(x => new { x.Bucket, x.ObjectKey }).IsUnique();
    }
}
