using backend.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Data.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable(
            "roles",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_roles_identity_not_blank",
                    "length(trim(code)) > 0 AND length(trim(name)) > 0"
                );
                table.HasCheckConstraint(
                    "ck_roles_timestamps",
                    "updated_at_utc >= created_at_utc"
                );
            }
        );

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Code).HasColumnType("citext").HasMaxLength(32).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(256);
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();

        builder.HasIndex(x => x.Code).IsUnique();

        var seedTime = new DateTime(2026, 03, 23, 0, 0, 0, DateTimeKind.Utc);
        builder.HasData(
            new Role
            {
                Id = 1,
                Code = "viewer",
                Name = "Viewer",
                Description = "Viewer role with basic registration capabilities.",
                CreatedAtUtc = seedTime,
                UpdatedAtUtc = seedTime
            },
            new Role
            {
                Id = 2,
                Code = "moderator",
                Name = "Moderator",
                Description = "Moderator role that helps manage game operations.",
                CreatedAtUtc = seedTime,
                UpdatedAtUtc = seedTime
            },
            new Role
            {
                Id = 3,
                Code = "admin",
                Name = "Administrator",
                Description = "Administrator role with full management access.",
                CreatedAtUtc = seedTime,
                UpdatedAtUtc = seedTime
            }
        );
    }
}
