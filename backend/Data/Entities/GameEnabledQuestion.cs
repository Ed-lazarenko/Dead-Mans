namespace backend.Data.Entities;

public class GameEnabledQuestion
{
    public Guid GameId { get; set; }

    public Guid QuestionId { get; set; }

    public DateTime EnabledAtUtc { get; set; }

    public int QuestionRevisionSnapshot { get; set; }

    public string QuestionCodeSnapshot { get; set; } = string.Empty;

    public string CategoryNameSnapshot { get; set; } = string.Empty;

    public string QuestionTextSnapshot { get; set; } = string.Empty;

    public string[] AcceptedAnswersSnapshot { get; set; } = Array.Empty<string>();

    public string[] NormalizedAnswersSnapshot { get; set; } = Array.Empty<string>();

    public int RewardSnapshot { get; set; }

    public int PrioritySnapshot { get; set; }

    public DateTime SnapshotAtUtc { get; set; }

    public Game Game { get; set; } = default!;

    public QuestionDefinition QuestionDefinition { get; set; } = default!;
}
