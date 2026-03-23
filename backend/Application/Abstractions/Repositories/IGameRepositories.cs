using backend.Domain.Models;

namespace backend.Application.Abstractions.Repositories;

public interface ILeaderboardRepository
{
    Task<IReadOnlyList<LeaderboardTeam>> GetTeamsAsync(CancellationToken cancellationToken = default);
}

public interface ILoadoutRepository
{
    Task<LoadoutBoard> GetBoardAsync(CancellationToken cancellationToken = default);
}

public interface IModifiersRepository
{
    Task<IReadOnlyList<ModifierDefinition>> GetDefinitionsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ActiveModifier>> GetActiveModifiersAsync(CancellationToken cancellationToken = default);

    Task SaveActiveModifiersAsync(
        IReadOnlyList<ActiveModifier> activeModifiers,
        CancellationToken cancellationToken = default
    );
}

public interface IGameControlRepository
{
    Task<GameControlState> GetStateAsync(CancellationToken cancellationToken = default);

    Task SaveStateAsync(GameControlState state, CancellationToken cancellationToken = default);
}
