using backend.Application.Abstractions.Repositories;
using backend.Domain.Models;

namespace backend.Infrastructure.Persistence;

public sealed class UnavailableLeaderboardRepository : ILeaderboardRepository
{
    public Task<IReadOnlyList<LeaderboardTeam>> GetTeamsAsync(CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException(
            "Leaderboard repository is no longer backed by in-memory test data. Configure a persistence-backed implementation before using this endpoint."
        );
    }
}

public sealed class UnavailableModifiersRepository : IModifiersRepository
{
    public Task<IReadOnlyList<ModifierDefinition>> GetDefinitionsAsync(CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException(
            "Modifiers repository is no longer backed by in-memory test data. Configure a persistence-backed implementation before using this endpoint."
        );
    }

    public Task<IReadOnlyList<ActiveModifier>> GetActiveModifiersAsync(CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException(
            "Modifiers repository is no longer backed by in-memory test data. Configure a persistence-backed implementation before using this endpoint."
        );
    }

    public Task SaveActiveModifiersAsync(
        IReadOnlyList<ActiveModifier> activeModifiers,
        CancellationToken cancellationToken = default
    )
    {
        throw new InvalidOperationException(
            "Modifiers repository is no longer backed by in-memory test data. Configure a persistence-backed implementation before using this endpoint."
        );
    }
}

public sealed class UnavailableGameControlRepository : IGameControlRepository
{
    public Task<GameControlState> GetStateAsync(CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException(
            "Game control repository is no longer backed by in-memory test data. Configure a persistence-backed implementation before using this endpoint."
        );
    }

    public Task SaveStateAsync(GameControlState state, CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException(
            "Game control repository is no longer backed by in-memory test data. Configure a persistence-backed implementation before using this endpoint."
        );
    }
}
