using backend.Application.Abstractions.Auth;
using backend.Data;
using Microsoft.EntityFrameworkCore;

namespace backend.Infrastructure.Auth;

public sealed class DbAuthUserReader : IAuthUserReader
{
    private readonly ApplicationDbContext _dbContext;

    public DbAuthUserReader(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AuthUserSummary?> FindByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _dbContext.Users
            .Where(x => x.Id == userId)
            .Select(x => new AuthUserSummary(x.Id, x.DisplayName, x.IsActive))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
