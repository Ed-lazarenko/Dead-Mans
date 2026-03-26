using backend.Application.Abstractions.Repositories;
using backend.Domain.Models;

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
        _logger.LogWarning(
            "Leaderboard persistence is not configured; rejecting request. Implement ILeaderboardRepository."
        );
        throw new InvalidOperationException(
            "Leaderboard repository is no longer backed by in-memory test data. Configure a persistence-backed implementation before using this endpoint."
        );
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
        _logger.LogWarning(
            "Modifiers persistence is not configured; rejecting request. Implement IModifiersRepository."
        );
        throw new InvalidOperationException(
            "Modifiers repository is no longer backed by in-memory test data. Configure a persistence-backed implementation before using this endpoint."
        );
    }

    public Task<IReadOnlyList<ActiveModifier>> GetActiveModifiersAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogWarning(
            "Modifiers persistence is not configured; rejecting request. Implement IModifiersRepository."
        );
        throw new InvalidOperationException(
            "Modifiers repository is no longer backed by in-memory test data. Configure a persistence-backed implementation before using this endpoint."
        );
    }

    public Task SaveActiveModifiersAsync(
        IReadOnlyList<ActiveModifier> activeModifiers,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogWarning(
            "Modifiers persistence is not configured; rejecting save. Implement IModifiersRepository."
        );
        throw new InvalidOperationException(
            "Modifiers repository is no longer backed by in-memory test data. Configure a persistence-backed implementation before using this endpoint."
        );
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
        _logger.LogWarning(
            "Game control persistence is not configured; rejecting request. Implement IGameControlRepository."
        );
        throw new InvalidOperationException(
            "Game control repository is no longer backed by in-memory test data. Configure a persistence-backed implementation before using this endpoint."
        );
    }

    public Task SaveStateAsync(GameControlState state, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning(
            "Game control persistence is not configured; rejecting save. Implement IGameControlRepository."
        );
        throw new InvalidOperationException(
            "Game control repository is no longer backed by in-memory test data. Configure a persistence-backed implementation before using this endpoint."
        );
    }
}
