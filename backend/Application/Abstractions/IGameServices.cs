using backend.Application.Contracts;

namespace backend.Application.Abstractions;

public interface ILeaderboardService
{
    Task<LeaderboardSummaryDto> GetLeaderboardAsync(CancellationToken cancellationToken = default);
}

public interface ILoadoutService
{
    Task<LoadoutBoardDto> GetBoardAsync(CancellationToken cancellationToken = default);
}

public interface IModifiersService
{
    Task<ModifiersSnapshotDto> GetSnapshotAsync(CancellationToken cancellationToken = default);

    Task<ModifiersSnapshotDto> ActivateAsync(
        ActivateModifierRequest request,
        CancellationToken cancellationToken = default
    );
}

public interface IGameControlService
{
    Task<GameControlStateDto> GetStateAsync(CancellationToken cancellationToken = default);

    Task<GameControlStateDto> StartAsync(CancellationToken cancellationToken = default);

    Task<GameControlStateDto> PauseAsync(CancellationToken cancellationToken = default);

    Task<GameControlStateDto> ResumeAsync(CancellationToken cancellationToken = default);

    Task<GameControlStateDto> NextRoundAsync(CancellationToken cancellationToken = default);

    Task<GameControlStateDto> ResetAsync(CancellationToken cancellationToken = default);
}
