using backend.Application.Contracts;

namespace backend.Application.Abstractions;

public interface ILeaderboardService
{
    Task<LeaderboardSummary> GetLeaderboardAsync(CancellationToken cancellationToken = default);
}

public interface ILoadoutService
{
    Task<LoadoutBoard> GetBoardAsync(CancellationToken cancellationToken = default);

    Task<LoadoutBoard> ToggleCellStateAsync(string cellId, CancellationToken cancellationToken = default);
}

public interface IModifiersService
{
    Task<ModifiersSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default);

    Task<ModifiersSnapshot> ActivateAsync(
        ActivateModifierCommand command,
        CancellationToken cancellationToken = default
    );
}

public interface IGameControlService
{
    Task<GameControlState> GetStateAsync(CancellationToken cancellationToken = default);

    Task<GameControlState> StartAsync(CancellationToken cancellationToken = default);

    Task<GameControlState> PauseAsync(CancellationToken cancellationToken = default);

    Task<GameControlState> ResumeAsync(CancellationToken cancellationToken = default);

    Task<GameControlState> NextRoundAsync(CancellationToken cancellationToken = default);

    Task<GameControlState> ResetAsync(CancellationToken cancellationToken = default);
}

public interface IGameBoardService
{
    Task<GameBoardSnapshot?> GetCurrentBoardAsync(CancellationToken cancellationToken = default);
}
