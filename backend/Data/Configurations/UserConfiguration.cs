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

        var seedTime = new DateTime(2026, 07, 10, 0, 0, 0, DateTimeKind.Utc);
        builder.HasData(
            new User
            {
                Id = UserSeedIds.TestPlayer1,
                TwitchUserId = "test-player-1",
                Login = "test_player_1",
                DisplayName = "Test Player 1",
                IsActive = true,
                CreatedAtUtc = seedTime,
                UpdatedAtUtc = seedTime
            },
            new User
            {
                Id = UserSeedIds.TestPlayer2,
                TwitchUserId = "test-player-2",
                Login = "test_player_2",
                DisplayName = "Test Player 2",
                IsActive = true,
                CreatedAtUtc = seedTime,
                UpdatedAtUtc = seedTime
            },
            new User
            {
                Id = UserSeedIds.TestPlayer3,
                TwitchUserId = "test-player-3",
                Login = "test_player_3",
                DisplayName = "Test Player 3",
                IsActive = true,
                CreatedAtUtc = seedTime,
                UpdatedAtUtc = seedTime
            },
            new User
            {
                Id = UserSeedIds.TestPlayer4,
                TwitchUserId = "test-player-4",
                Login = "test_player_4",
                DisplayName = "Test Player 4",
                IsActive = true,
                CreatedAtUtc = seedTime,
                UpdatedAtUtc = seedTime
            }
        );
    }
}
