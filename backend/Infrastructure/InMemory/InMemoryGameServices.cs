using backend.Application.Abstractions;
using backend.Application.Contracts;
using backend.Domain.Models;

namespace backend.Infrastructure.InMemory;

public sealed class InMemoryLeaderboardService : ILeaderboardService
{
    private readonly List<LeaderboardTeam> _teams =
    [
        new() { Id = "t1", Name = "Team Alpha", ColorHex = "#ff7043" },
        new() { Id = "t2", Name = "Team Bravo", ColorHex = "#42a5f5" },
        new() { Id = "t3", Name = "Team Charlie", ColorHex = "#66bb6a" },
    ];

    public InMemoryLeaderboardService()
    {
        _teams[0].ApplyDelta(120, 0);
        _teams[1].ApplyDelta(95, 10);
        _teams[2].ApplyDelta(80, 0);
    }

    public Task<LeaderboardSummaryDto> GetLeaderboardAsync(CancellationToken cancellationToken = default)
    {
        var teams = _teams
            .OrderByDescending(team => team.TotalScore)
            .Select(team => new LeaderboardTeamDto(
                team.Id,
                team.Name,
                team.ColorHex,
                team.Score,
                team.Penalty
            ))
            .ToArray();

        return Task.FromResult(new LeaderboardSummaryDto(DateTimeOffset.UtcNow, teams));
    }
}

public sealed class InMemoryLoadoutService : ILoadoutService
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

    public Task<LoadoutBoardDto> GetBoardAsync(CancellationToken cancellationToken = default)
    {
        var board = new LoadoutBoardDto(
            _board.Rows,
            _board.Cols,
            _board.RowLabels,
            _board.ColLabels,
            _board.Cells
                .Select(cell => new LoadoutCellDto(
                    cell.Id,
                    cell.Row,
                    cell.Col,
                    cell.Label,
                    cell.Points,
                    cell.ImageUrl,
                    cell.State.ToString().ToLowerInvariant()
                ))
                .ToArray()
        );

        return Task.FromResult(board);
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

public sealed class InMemoryModifiersService : IModifiersService
{
    private readonly List<ModifierDefinition> _definitions =
    [
        new() { Id = "petarda", Name = "петарда", Cost = 3, Description = "💥 Мощный взрыв на экране" },
        new() { Id = "steps", Name = "шаги", Cost = 2, Description = "👣 Кто-то крадётся за спиной" },
        new() { Id = "flash", Name = "вспышка", Cost = 1, Description = "⚡ Краткая вспышка экрана" },
    ];

    private readonly List<ActiveModifier> _activeModifiers = [];
    private readonly object _syncRoot = new();

    public Task<ModifiersSnapshotDto> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        lock (_syncRoot)
        {
            return Task.FromResult(BuildSnapshot());
        }
    }

    public Task<ModifiersSnapshotDto> ActivateAsync(
        ActivateModifierRequest request,
        CancellationToken cancellationToken = default
    )
    {
        lock (_syncRoot)
        {
            var definitionExists = _definitions.Any(definition => definition.Id == request.ModifierId);
            if (definitionExists)
            {
                _activeModifiers.Insert(0, new ActiveModifier
                {
                    Id = $"{request.ModifierId}-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
                    ModifierId = request.ModifierId,
                    TriggeredBy = request.TriggeredBy,
                    ActivatedAt = DateTimeOffset.UtcNow,
                });
            }

            if (_activeModifiers.Count > 10)
            {
                _activeModifiers.RemoveRange(10, _activeModifiers.Count - 10);
            }

            return Task.FromResult(BuildSnapshot());
        }
    }

    private ModifiersSnapshotDto BuildSnapshot()
    {
        var available = _definitions
            .Select(definition => new ModifierDefinitionDto(
                definition.Id,
                definition.Name,
                definition.Cost,
                definition.Description
            ))
            .ToArray();

        var active = _activeModifiers
            .Select(modifier => new ActiveModifierDto(
                modifier.Id,
                modifier.ModifierId,
                modifier.ActivatedAt,
                modifier.TriggeredBy
            ))
            .ToArray();

        return new ModifiersSnapshotDto(available, active);
    }
}

public sealed class InMemoryGameControlService : IGameControlService
{
    private readonly GameControlState _state = new();
    private readonly object _syncRoot = new();

    public Task<GameControlStateDto> GetStateAsync(CancellationToken cancellationToken = default)
    {
        lock (_syncRoot)
        {
            return Task.FromResult(ToDto());
        }
    }

    public Task<GameControlStateDto> StartAsync(CancellationToken cancellationToken = default)
    {
        lock (_syncRoot)
        {
            _state.Start();
            return Task.FromResult(ToDto());
        }
    }

    public Task<GameControlStateDto> PauseAsync(CancellationToken cancellationToken = default)
    {
        lock (_syncRoot)
        {
            _state.Pause();
            return Task.FromResult(ToDto());
        }
    }

    public Task<GameControlStateDto> ResumeAsync(CancellationToken cancellationToken = default)
    {
        lock (_syncRoot)
        {
            _state.Resume();
            return Task.FromResult(ToDto());
        }
    }

    public Task<GameControlStateDto> NextRoundAsync(CancellationToken cancellationToken = default)
    {
        lock (_syncRoot)
        {
            _state.NextRound();
            return Task.FromResult(ToDto());
        }
    }

    public Task<GameControlStateDto> ResetAsync(CancellationToken cancellationToken = default)
    {
        lock (_syncRoot)
        {
            _state.Reset();
            return Task.FromResult(ToDto());
        }
    }

    private GameControlStateDto ToDto()
    {
        return new GameControlStateDto(
            _state.Phase.ToString().ToLowerInvariant(),
            _state.CurrentRound,
            _state.TotalRounds,
            _state.LastActionAt
        );
    }
}
