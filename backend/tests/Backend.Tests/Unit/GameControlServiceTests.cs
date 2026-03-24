using backend.Application.Abstractions.Repositories;
using backend.Application.Features.GameControl;
using backend.Domain.Models;
using GamePhase = backend.Domain.Models.GamePhase;

namespace Backend.Tests.Unit;

public sealed class GameControlServiceTests
{
    [Fact]
    public async Task StartAsync_TransitionsIdleToRunning()
    {
        var state = new GameControlState();
        var repository = new FakeGameControlRepository(state);
        var service = new GameControlService(repository);

        var dto = await service.StartAsync(CancellationToken.None);

        Assert.Equal(GamePhase.Running, dto.Phase);
        Assert.True(repository.SaveCount >= 1);
    }

    [Fact]
    public async Task PauseAsync_FromRunning_SetsPaused()
    {
        var state = new GameControlState();
        state.Start();
        var repository = new FakeGameControlRepository(state);
        var service = new GameControlService(repository);

        var dto = await service.PauseAsync(CancellationToken.None);

        Assert.Equal(GamePhase.Paused, dto.Phase);
    }

    private sealed class FakeGameControlRepository : IGameControlRepository
    {
        private readonly GameControlState _state;

        public int SaveCount { get; private set; }

        public FakeGameControlRepository(GameControlState state)
        {
            _state = state;
        }

        public Task<GameControlState> GetStateAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_state);
        }

        public Task SaveStateAsync(GameControlState state, CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.CompletedTask;
        }
    }
}
