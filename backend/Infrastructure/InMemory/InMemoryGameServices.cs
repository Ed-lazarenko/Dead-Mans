using backend.Application.Abstractions.Repositories;
using backend.Domain.Models;

namespace backend.Infrastructure.InMemory;

public sealed class InMemoryLeaderboardRepository : ILeaderboardRepository
{
    private readonly List<LeaderboardTeam> _teams =
    [
        new() { Id = "t1", Name = "Team Alpha", ColorHex = "#ff7043" },
        new() { Id = "t2", Name = "Team Bravo", ColorHex = "#42a5f5" },
        new() { Id = "t3", Name = "Team Charlie", ColorHex = "#66bb6a" },
    ];

    public InMemoryLeaderboardRepository()
    {
        _teams[0].ApplyDelta(120, 0);
        _teams[1].ApplyDelta(95, 10);
        _teams[2].ApplyDelta(80, 0);
    }

    public Task<IReadOnlyList<LeaderboardTeam>> GetTeamsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<LeaderboardTeam>>(_teams.ToArray());
    }
}

public sealed class InMemoryLoadoutRepository : ILoadoutRepository
{
    private static readonly string[] RowLabels = ["100", "125", "150", "175", "200"];
    private static readonly string[] ColLabels =
    [
        "Бомбардир",
        "Пиромант",
        "Токсик",
        "Вампир",
        "Аватар",
        "Всё могу x2",
    ];

    private readonly LoadoutBoard _board = CreateBoard();

    public Task<LoadoutBoard> GetBoardAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_board);
    }

    private static LoadoutBoard CreateBoard()
    {
        var cells = new List<LoadoutCell>();

        for (var row = 0; row < RowLabels.Length; row += 1)
        {
            for (var col = 0; col < ColLabels.Length; col += 1)
            {
                cells.Add(new LoadoutCell
                {
                    Id = $"r{row + 1}c{col + 1}",
                    Row = row,
                    Col = col,
                    Label = $"{RowLabels[row]} • {ColLabels[col]}",
                    Points = int.TryParse(RowLabels[row], out var points) ? points : 0,
                    ImageUrl = $"/mock-loadouts/elements/{col + 1}-{row + 1}.png",
                });
            }
        }

        return new LoadoutBoard
        {
            Rows = RowLabels.Length,
            Cols = ColLabels.Length,
            RowLabels = RowLabels,
            ColLabels = ColLabels,
            Cells = cells,
        };
    }
}

public sealed class InMemoryModifiersRepository : IModifiersRepository
{
    private readonly List<ModifierDefinition> _definitions =
    [
        new() { Id = "petarda", Name = "петарда", Cost = 3, Description = "💥 Мощный взрыв на экране" },
        new() { Id = "steps", Name = "шаги", Cost = 2, Description = "👣 Кто-то крадётся за спиной" },
        new() { Id = "flash", Name = "вспышка", Cost = 1, Description = "⚡ Краткая вспышка экрана" },
    ];

    private readonly List<ActiveModifier> _activeModifiers = [];
    private readonly object _syncRoot = new();

    public Task<IReadOnlyList<ModifierDefinition>> GetDefinitionsAsync(CancellationToken cancellationToken = default)
    {
        lock (_syncRoot)
        {
            return Task.FromResult<IReadOnlyList<ModifierDefinition>>(_definitions.ToArray());
        }
    }

    public Task<IReadOnlyList<ActiveModifier>> GetActiveModifiersAsync(CancellationToken cancellationToken = default)
    {
        lock (_syncRoot)
        {
            return Task.FromResult<IReadOnlyList<ActiveModifier>>(_activeModifiers.ToArray());
        }
    }

    public Task SaveActiveModifiersAsync(
        IReadOnlyList<ActiveModifier> activeModifiers,
        CancellationToken cancellationToken = default
    )
    {
        lock (_syncRoot)
        {
            _activeModifiers.Clear();
            _activeModifiers.AddRange(activeModifiers);
        }

        return Task.CompletedTask;
    }
}

public sealed class InMemoryGameControlRepository : IGameControlRepository
{
    private readonly GameControlState _state = new();
    private readonly object _syncRoot = new();

    public Task<GameControlState> GetStateAsync(CancellationToken cancellationToken = default)
    {
        lock (_syncRoot)
        {
            return Task.FromResult(_state);
        }
    }

    public Task SaveStateAsync(GameControlState state, CancellationToken cancellationToken = default)
    {
        lock (_syncRoot)
        {
            return Task.CompletedTask;
        }
    }
}
