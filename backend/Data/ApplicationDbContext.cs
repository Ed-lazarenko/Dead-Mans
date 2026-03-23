using Microsoft.EntityFrameworkCore;

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

    // TODO: add DbSet<TEntity> properties here, e.g.:
    // public DbSet<LoadoutBoard> LoadoutBoards { get; set; } = default!;
    // public DbSet<LoadoutCell> LoadoutCells { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Entity mappings will be added incrementally as the in-memory services
        // are replaced with EF-backed repositories/use cases.
    }
}
