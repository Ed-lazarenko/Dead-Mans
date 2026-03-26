namespace backend.Domain.Models;

public sealed class LeaderboardTeam
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string ColorHex { get; init; } = string.Empty;
    public int Score { get; private set; }
    public int Penalty { get; private set; }

    public int TotalScore => Score - Penalty;

    public void ApplyDelta(int scoreDelta, int penaltyDelta)
    {
        Score += scoreDelta;
        Penalty = Math.Max(0, Penalty + penaltyDelta);
    }
}

public enum LoadoutCellState
{
    Closed,
    Open
}

public sealed class LoadoutCell
{
    public string Id { get; init; } = string.Empty;
    public int Row { get; init; }
    public int Col { get; init; }
    public string Label { get; init; } = string.Empty;
    public int Points { get; init; }
    public string? ImageUrl { get; init; }
    public LoadoutCellState State { get; private set; } = LoadoutCellState.Closed;

    public void ToggleOpen()
    {
        State = State == LoadoutCellState.Open
            ? LoadoutCellState.Closed
            : LoadoutCellState.Open;
    }
}

public sealed class LoadoutBoard
{
    public int Rows { get; init; }
    public int Cols { get; init; }
    public IReadOnlyList<string> RowLabels { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> ColLabels { get; init; } = Array.Empty<string>();
    public IReadOnlyList<LoadoutCell> Cells { get; init; } = Array.Empty<LoadoutCell>();
}

public sealed class ModifierDefinition
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int Cost { get; init; }
    public string Description { get; init; } = string.Empty;
}

public sealed class ActiveModifier
{
    public string Id { get; init; } = string.Empty;
    public string ModifierId { get; init; } = string.Empty;
    public DateTimeOffset ActivatedAt { get; init; }
    public string TriggeredBy { get; init; } = string.Empty;
}

public enum GamePhase
{
    Idle,
    Running,
    Paused,
    Finished
}

public enum GameStatus
{
    /// <summary>
    /// Служебный статус для внутренних работ (не показывается в витрине "текущей игры").
    /// </summary>
    Draft,
    /// <summary>
    /// Готова к проведению или уже проводится.
    /// </summary>
    Active,
    /// <summary>
    /// Отыграна и доступна для просмотра результатов.
    /// </summary>
    Finished
}

public sealed class GameControlState
{
    public GamePhase Phase { get; private set; } = GamePhase.Idle;
    public int CurrentRound { get; private set; } = 1;
    public int TotalRounds { get; private set; } = 3;
    public DateTimeOffset? LastActionAt { get; private set; }

    public void Start()
    {
        Phase = GamePhase.Running;
        CurrentRound = 1;
        Touch();
    }

    public void Pause()
    {
        if (Phase != GamePhase.Running)
        {
            return;
        }

        Phase = GamePhase.Paused;
        Touch();
    }

    public void Resume()
    {
        if (Phase != GamePhase.Paused)
        {
            return;
        }

        Phase = GamePhase.Running;
        Touch();
    }

    public void NextRound()
    {
        CurrentRound = Math.Min(CurrentRound + 1, TotalRounds);
        if (CurrentRound >= TotalRounds)
        {
            Phase = GamePhase.Finished;
        }

        Touch();
    }

    public void Reset()
    {
        Phase = GamePhase.Idle;
        CurrentRound = 1;
        TotalRounds = 3;
        Touch();
    }

    private void Touch()
    {
        LastActionAt = DateTimeOffset.UtcNow;
    }
}
