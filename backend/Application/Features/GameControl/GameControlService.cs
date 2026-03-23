using backend.Application.Abstractions;
using backend.Application.Abstractions.Repositories;
using backend.Application.Contracts;

namespace backend.Application.Features.GameControl;

public sealed class GameControlService : IGameControlService
{
    private readonly IGameControlRepository _repository;

    public GameControlService(IGameControlRepository repository)
    {
        _repository = repository;
    }

    public async Task<Contracts.GameControlState> GetStateAsync(CancellationToken cancellationToken = default)
    {
        var state = await _repository.GetStateAsync(cancellationToken);
        return MapState(state);
    }

    public Task<Contracts.GameControlState> StartAsync(CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(state => state.Start(), cancellationToken);
    }

    public Task<Contracts.GameControlState> PauseAsync(CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(state => state.Pause(), cancellationToken);
    }

    public Task<Contracts.GameControlState> ResumeAsync(CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(state => state.Resume(), cancellationToken);
    }

    public Task<Contracts.GameControlState> NextRoundAsync(CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(state => state.NextRound(), cancellationToken);
    }

    public Task<Contracts.GameControlState> ResetAsync(CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(state => state.Reset(), cancellationToken);
    }

    private async Task<Contracts.GameControlState> ExecuteAsync(
        Action<Domain.Models.GameControlState> action,
        CancellationToken cancellationToken
    )
    {
        var state = await _repository.GetStateAsync(cancellationToken);
        action(state);
        await _repository.SaveStateAsync(state, cancellationToken);

        return MapState(state);
    }

    private static Contracts.GameControlState MapState(Domain.Models.GameControlState state)
    {
        return new Contracts.GameControlState(
            state.Phase,
            state.CurrentRound,
            state.TotalRounds,
            state.LastActionAt
        );
    }
}
