using backend.Domain.Models;

namespace Backend.Tests.Unit.Domain;

public sealed class GameModelsTests
{
    [Fact]
    public void LeaderboardTeam_TotalScore_IsScoreMinusPenalty()
    {
        var team = new LeaderboardTeam { Id = "a", Name = "A", ColorHex = "#fff" };
        team.ApplyDelta(10, 3);
        Assert.Equal(7, team.TotalScore);
    }

    [Fact]
    public void LeaderboardTeam_ApplyDelta_ClampPenaltyAtZero()
    {
        var team = new LeaderboardTeam { Id = "a", Name = "A", ColorHex = "#fff" };
        team.ApplyDelta(5, 2);
        team.ApplyDelta(0, -5);
        Assert.Equal(5, team.Score);
        Assert.Equal(0, team.Penalty);
    }

    [Fact]
    public void LeaderboardTeam_ApplyDelta_AllowsPositivePenalty()
    {
        var team = new LeaderboardTeam { Id = "a", Name = "A", ColorHex = "#fff" };
        team.ApplyDelta(10, 1);
        team.ApplyDelta(2, 4);
        Assert.Equal(12, team.Score);
        Assert.Equal(5, team.Penalty);
        Assert.Equal(7, team.TotalScore);
    }

    [Fact]
    public void LoadoutCell_ToggleOpen_TogglesBetweenClosedAndOpen()
    {
        var cell = new LoadoutCell
        {
            Id = "c1",
            Row = 0,
            Col = 0,
            Label = "X",
            Points = 1
        };

        cell.ToggleOpen();
        Assert.Equal(LoadoutCellState.Open, cell.State);

        cell.ToggleOpen();
        Assert.Equal(LoadoutCellState.Closed, cell.State);
    }

    [Fact]
    public void GameControlState_Start_SetsRunningAndRoundOne_AndLastActionTouched()
    {
        var state = new GameControlState();

        state.Start();

        Assert.Equal(GamePhase.Running, state.Phase);
        Assert.Equal(1, state.CurrentRound);
        Assert.NotNull(state.LastActionAt);
    }

    [Fact]
    public void GameControlState_Pause_WorksOnlyFromRunning()
    {
        var state = new GameControlState();

        state.Pause();
        Assert.Equal(GamePhase.Idle, state.Phase);

        state.Start();
        state.Pause();
        Assert.Equal(GamePhase.Paused, state.Phase);
    }

    [Fact]
    public void GameControlState_NextRound_OnFinalRound_SetsFinishedAndClampsRound()
    {
        var state = new GameControlState();
        state.Start();
        state.NextRound();
        state.NextRound();

        Assert.Equal(3, state.CurrentRound);
        Assert.Equal(GamePhase.Finished, state.Phase);
    }

    [Fact]
    public void GameControlState_Reset_RestoresDefaults()
    {
        var state = new GameControlState();
        state.Start();
        state.NextRound();

        state.Reset();

        Assert.Equal(GamePhase.Idle, state.Phase);
        Assert.Equal(1, state.CurrentRound);
        Assert.Equal(3, state.TotalRounds);
    }
}
