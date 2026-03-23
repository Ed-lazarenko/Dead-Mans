using Microsoft.EntityFrameworkCore;
using backend.Data.Configurations;
using backend.Data.Entities;

namespace backend.Data;

/// <summary>
/// EF Core entry point for future persistent storage.
/// Current application services use in-memory implementations, but the DbContext
/// already serves as the infrastructure boundary where real entities/mappings will live.
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new RoleConfiguration());
        modelBuilder.ApplyConfiguration(new UserRoleConfiguration());
    }
}
