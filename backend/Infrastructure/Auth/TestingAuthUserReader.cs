using backend.Application.Abstractions.Auth;

namespace backend.Infrastructure.Auth;

public sealed class TestingAuthUserReader : IAuthUserReader
{
    private readonly DbAuthUserReader _dbAuthUserReader;

    public TestingAuthUserReader(DbAuthUserReader dbAuthUserReader)
    {
        _dbAuthUserReader = dbAuthUserReader;
    }

    public async Task<AuthUserSummary?> FindByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _dbAuthUserReader.FindByIdAsync(userId, cancellationToken);
        if (user is not null)
        {
            return user;
        }

        return new AuthUserSummary(userId, "Test User", IsActive: true);
    }
}
