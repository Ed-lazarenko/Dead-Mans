using backend.Application.Contracts;

namespace backend.Application.Abstractions;

public interface IGameRoundService
{
    Task<IReadOnlyList<GameRoundTeamOption>> GetEligibleTeamsAsync(
        CancellationToken cancellationToken = default
    );

    Task<GameRoundDetails?> GetActiveAsync(CancellationToken cancellationToken = default);

    Task<StartGameRoundResult> StartAsync(
        StartGameRoundInput input,
        Guid startedByUserId,
        CancellationToken cancellationToken = default
    );

    Task<ReviewGameRoundResult> ReviewAsync(
        Guid roundId,
        Guid reviewedByUserId,
        CancellationToken cancellationToken = default
    );

    Task<FinalizeGameRoundResult> FinalizeAsync(
        Guid roundId,
        FinalizeGameRoundInput input,
        Guid resolvedByUserId,
        CancellationToken cancellationToken = default
    );
}
