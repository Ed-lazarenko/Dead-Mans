using Microsoft.EntityFrameworkCore;
using backend.Data.Configurations;
using backend.Data.Entities;

namespace backend.Data;

/// <summary>
/// EF Core entry point for the persistence-backed infrastructure.
/// Auth and user-role flows already use this DbContext, while game repositories
/// still rely on in-memory adapters until those slices move to database-backed storage.
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
    public DbSet<Game> Games => Set<Game>();
    public DbSet<GameBoard> GameBoards => Set<GameBoard>();
    public DbSet<BoardCell> BoardCells => Set<BoardCell>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new RoleConfiguration());
        modelBuilder.ApplyConfiguration(new UserRoleConfiguration());
        modelBuilder.ApplyConfiguration(new GameConfiguration());
        modelBuilder.ApplyConfiguration(new GameBoardConfiguration());
        modelBuilder.ApplyConfiguration(new BoardCellConfiguration());
    }
}
