using backend.Application.Abstractions.Repositories;
using backend.Application.Contracts;
using backend.Data;
using backend.Data.Entities;
using backend.Domain.Persistence;
using Microsoft.EntityFrameworkCore;

namespace backend.Infrastructure.Persistence;

public sealed partial class DbGameRegistrationPersistence : IGameRegistrationPersistence
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IGameRegistrationReadStore _reads;
    private readonly ILogger<DbGameRegistrationPersistence> _logger;

    public DbGameRegistrationPersistence(
        ApplicationDbContext dbContext,
        IGameRegistrationReadStore reads,
        ILogger<DbGameRegistrationPersistence> logger
    )
    {
        _dbContext = dbContext;
        _reads = reads;
        _logger = logger;
    }

    private async Task<GameRegistrationResult<RegistrationTeamDto>> LoadTeamResultAsync(
        Guid teamId,
        CancellationToken cancellationToken
    )
    {
        var dto = await _reads.LoadTeamDtoAsync(teamId, cancellationToken);
        if (dto is null)
        {
            return Fail<RegistrationTeamDto>(GameRegistrationErrorCode.OperationFailed);
        }

        return new GameRegistrationResult<RegistrationTeamDto>(true, dto, GameRegistrationErrorCode.None);
    }

    private static GameRegistrationResult<T> Fail<T>(GameRegistrationErrorCode error) =>
        new(false, default, error);
}
