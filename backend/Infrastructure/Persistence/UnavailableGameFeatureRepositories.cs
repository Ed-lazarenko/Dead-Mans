using backend.Application.Abstractions.Repositories;
using backend.Domain.Models;
using backend.Messaging;

namespace backend.Infrastructure.Persistence;

public sealed class UnavailableLeaderboardRepository : ILeaderboardRepository
{
    private readonly ILogger<UnavailableLeaderboardRepository> _logger;

    public UnavailableLeaderboardRepository(ILogger<UnavailableLeaderboardRepository> logger)
    {
        _logger = logger;
    }

    public Task<IReadOnlyList<LeaderboardTeam>> GetTeamsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogWarning(AppMessages.Logs.UnavailableLeaderboard);
        throw new InvalidOperationException(AppMessages.Exceptions.UnavailableLeaderboard);
    }
}

public sealed class UnavailableModifiersRepository : IModifiersRepository
{
    private readonly ILogger<UnavailableModifiersRepository> _logger;

    public UnavailableModifiersRepository(ILogger<UnavailableModifiersRepository> logger)
    {
        _logger = logger;
    }

    public Task<IReadOnlyList<ModifierDefinition>> GetDefinitionsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogWarning(AppMessages.Logs.UnavailableModifiersRequest);
        throw new InvalidOperationException(AppMessages.Exceptions.UnavailableModifiers);
    }

    public Task<IReadOnlyList<ActiveModifier>> GetActiveModifiersAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogWarning(AppMessages.Logs.UnavailableModifiersRequest);
        throw new InvalidOperationException(AppMessages.Exceptions.UnavailableModifiers);
    }

    public Task SaveActiveModifiersAsync(
        IReadOnlyList<ActiveModifier> activeModifiers,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogWarning(AppMessages.Logs.UnavailableModifiersSave);
        throw new InvalidOperationException(AppMessages.Exceptions.UnavailableModifiers);
    }
}

public sealed class UnavailableGameControlRepository : IGameControlRepository
{
    private readonly ILogger<UnavailableGameControlRepository> _logger;

    public UnavailableGameControlRepository(ILogger<UnavailableGameControlRepository> logger)
    {
        _logger = logger;
    }

    public Task<GameControlState> GetStateAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogWarning(AppMessages.Logs.UnavailableGameControlRequest);
        throw new InvalidOperationException(AppMessages.Exceptions.UnavailableGameControl);
    }

    public Task SaveStateAsync(GameControlState state, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning(AppMessages.Logs.UnavailableGameControlSave);
        throw new InvalidOperationException(AppMessages.Exceptions.UnavailableGameControl);
    }
}
