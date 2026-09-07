using backend.Data;
using Microsoft.EntityFrameworkCore;

namespace backend.Infrastructure.Persistence;

/// <summary>
/// Serializes catalog mutations and ready-to-active pinning in PostgreSQL.
/// The lock is transaction-scoped and must only be acquired inside the owning transaction.
/// </summary>
internal static class ModifierCatalogTransactionLock
{
    private const long LockKey = 4_921_671_620_642_911_570L;

    public static Task AcquireAsync(
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken) =>
        dbContext.Database.IsRelational()
            ? dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock({LockKey})",
                cancellationToken)
            : Task.CompletedTask;
}
