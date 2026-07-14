using backend.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace backend.Data.Configurations;

public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("user_roles");

        builder.HasKey(x => new { x.UserId, x.RoleId });

        builder.Property(x => x.AssignedAtUtc).IsRequired();

        builder.HasOne(x => x.User)
            .WithMany(x => x.UserRoles)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Role)
            .WithMany(x => x.UserRoles)
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.AssignedByUser)
            .WithMany(x => x.AssignedRoles)
            .HasForeignKey(x => x.AssignedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.RoleId);
        builder.HasIndex(x => x.AssignedByUserId);
        builder.HasIndex(x => x.ExpiresAtUtc);

        var seedTime = new DateTime(2026, 07, 10, 0, 0, 0, DateTimeKind.Utc);
        builder.HasData(
            new UserRole
            {
                UserId = UserSeedIds.TestPlayer1,
                RoleId = 1,
                AssignedAtUtc = seedTime
            },
            new UserRole
            {
                UserId = UserSeedIds.TestPlayer2,
                RoleId = 1,
                AssignedAtUtc = seedTime
            },
            new UserRole
            {
                UserId = UserSeedIds.TestPlayer3,
                RoleId = 1,
                AssignedAtUtc = seedTime
            },
            new UserRole
            {
                UserId = UserSeedIds.TestPlayer4,
                RoleId = 1,
                AssignedAtUtc = seedTime
            }
        );
    }
}
