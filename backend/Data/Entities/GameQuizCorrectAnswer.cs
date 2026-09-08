namespace backend.Data.Entities;

public sealed class GameQuizCorrectAnswer
{
    public Guid Id { get; set; }

    public Guid GameId { get; set; }

    public Guid QuizRoundId { get; set; }

    public Guid AwardedToUserId { get; set; }

    public Guid? CapturedByUserId { get; set; }

    public string TwitchUserIdSnapshot { get; set; } = string.Empty;

    public string LoginSnapshot { get; set; } = string.Empty;

    public string DisplayNameSnapshot { get; set; } = string.Empty;

    public string SubmittedAnswer { get; set; } = string.Empty;

    public string NormalizedAnswer { get; set; } = string.Empty;

    public string SourceProvider { get; set; } = string.Empty;

    public string? SourceChannelId { get; set; }

    public string? SourceMessageId { get; set; }

    public DateTime AnsweredAtUtc { get; set; }

    public GameQuizRound QuizRound { get; set; } = default!;

    public User AwardedToUser { get; set; } = default!;

    public User? CapturedByUser { get; set; }

    public ICollection<GameQuizPointLedgerEntry> PointEntries { get; set; } =
        new List<GameQuizPointLedgerEntry>();
}
