using backend.Application.Abstractions;
using backend.Application.Abstractions.Repositories;
using backend.Application.Contracts;

namespace backend.Application.Features.GameCardRuns;

public sealed class GameCardRunService : IGameCardRunService
{
    private readonly IGameCardRunRepository _repository;

    public GameCardRunService(IGameCardRunRepository repository)
    {
        _repository = repository;
    }

    public Task<StartGameCardRunResult> StartAsync(
        StartGameCardRunInput input,
        Guid startedByUserId,
        CancellationToken cancellationToken = default
    )
    {
        return _repository.StartAsync(input, startedByUserId, cancellationToken);
    }

    public Task<FinalizeGameCardRunResult> FinalizeAsync(
        Guid cardRunId,
        FinalizeGameCardRunInput input,
        Guid resolvedByUserId,
        CancellationToken cancellationToken = default
    )
    {
        return _repository.FinalizeAsync(cardRunId, input, resolvedByUserId, cancellationToken);
    }
}
