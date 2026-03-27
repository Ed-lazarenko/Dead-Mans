using backend.Application.Abstractions.Auth;
using backend.Data;
using backend.Messaging;
using Microsoft.EntityFrameworkCore;

namespace backend.Infrastructure.Auth;

public sealed class DbAuthUserReader : IAuthUserReader
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<DbAuthUserReader> _logger;

    public DbAuthUserReader(ApplicationDbContext dbContext, ILogger<DbAuthUserReader> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<AuthUserSummary?> FindByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            return await _dbContext.Users
                .Where(x => x.Id == userId)
                .Select(x => new AuthUserSummary(x.Id, x.DisplayName, x.IsActive))
                .FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, AppMessages.Logs.DbAuthUserResolveError, userId);
            throw;
        }
    }
}
