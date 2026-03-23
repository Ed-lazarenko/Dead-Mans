using backend.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TwitchUserId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Login).HasMaxLength(64).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(320);
        builder.Property(x => x.ProfileImageUrl).HasMaxLength(1024);
        builder.Property(x => x.BroadcasterType).HasMaxLength(32);
        builder.Property(x => x.TwitchUserType).HasMaxLength(32);
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();

        builder.HasIndex(x => x.TwitchUserId).IsUnique();
        builder.HasIndex(x => x.Login);
    }
}
