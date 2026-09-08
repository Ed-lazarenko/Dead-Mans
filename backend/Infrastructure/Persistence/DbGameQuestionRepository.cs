using backend.Application.Abstractions.Repositories;
using backend.Data;

namespace backend.Infrastructure.Persistence;

public sealed partial class DbGameQuestionRepository : IGameQuestionRepository
{
    private readonly ApplicationDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public DbGameQuestionRepository(ApplicationDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }
}
