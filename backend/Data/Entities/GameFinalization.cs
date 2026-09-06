namespace backend.Data.Entities;

public class GameFinalization
{
    public Guid GameId { get; set; }

    public Guid RequestId { get; set; }

    public Guid FinishedByUserId { get; set; }

    public string FinishedByDisplayNameSnapshot { get; set; } = string.Empty;

    public DateTime FinishedAtUtc { get; set; }

    public string? PublicNote { get; set; }

    public int CalculationVersion { get; set; }

    public int CompletedRoundCount { get; set; }

    public int CancelledRoundCount { get; set; }

    public int TotalKills { get; set; }

    public int TotalBounties { get; set; }

    public int QuizTotalPoints { get; set; }

    public int SkippedQuizQuestionCount { get; set; }

    public Game Game { get; set; } = default!;

    public User FinishedByUser { get; set; } = default!;

    public ICollection<GameTeamFinalResult> TeamResults { get; set; } =
        new List<GameTeamFinalResult>();
}
