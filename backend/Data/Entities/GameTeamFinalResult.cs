namespace backend.Data.Entities;

public class GameTeamFinalResult
{
    public Guid GameId { get; set; }

    public Guid TeamId { get; set; }

    public string? TeamNameSnapshot { get; set; }

    public int TeamSlotIndexSnapshot { get; set; }

    public string[] ParticipantNamesSnapshot { get; set; } = [];

    public int RoundsPlayed { get; set; }

    public int? BestScore { get; set; }

    public int PenaltyTotal { get; set; }

    public int? FinalScore { get; set; }

    public int TotalScore { get; set; }

    public int TotalBonusDelta { get; set; }

    public int TotalKills { get; set; }

    public int TotalBounties { get; set; }

    public int? Placement { get; set; }

    public DateTime? LastFinishedAtUtc { get; set; }

    public GameFinalization Finalization { get; set; } = default!;

    public GameTeam Team { get; set; } = default!;
}
