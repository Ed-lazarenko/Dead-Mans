using backend.Application.Contracts;

namespace backend.Application.Abstractions.Repositories;

public interface IGameCardRunRepository
{
    Task<IReadOnlyList<GameCardRunTeamOption>> GetEligibleTeamsAsync(
        CancellationToken cancellationToken = default
    );

    Task<GameCardRunDetails?> GetActiveAsync(CancellationToken cancellationToken = default);

    Task<StartGameCardRunResult> StartAsync(
        StartGameCardRunInput input,
        Guid startedByUserId,
        CancellationToken cancellationToken = default
    );

    Task<ReviewGameCardRunResult> ReviewAsync(
        Guid cardRunId,
        Guid reviewedByUserId,
        CancellationToken cancellationToken = default
    );

    Task<FinalizeGameCardRunResult> FinalizeAsync(
        Guid cardRunId,
        FinalizeGameCardRunInput input,
        Guid resolvedByUserId,
        CancellationToken cancellationToken = default
    );
}
