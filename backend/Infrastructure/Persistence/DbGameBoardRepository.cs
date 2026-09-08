using backend.Application.Abstractions.Repositories;
using backend.Data;
using backend.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace backend.Infrastructure.Persistence;

public sealed partial class DbGameBoardRepository : IGameBoardRepository
{
    private readonly ApplicationDbContext _dbContext;
    private readonly string _storagePublicBaseUrl;
    private readonly ILogger<DbGameBoardRepository> _logger;
    private readonly TimeProvider _timeProvider;

    public DbGameBoardRepository(
        ApplicationDbContext dbContext,
        IOptions<StorageOptions> storageOptions,
        ILogger<DbGameBoardRepository> logger,
        TimeProvider timeProvider
    )
    {
        _dbContext = dbContext;
        _storagePublicBaseUrl = storageOptions.Value.PublicBaseUrl.TrimEnd('/');
        _logger = logger;
        _timeProvider = timeProvider;
    }
}
