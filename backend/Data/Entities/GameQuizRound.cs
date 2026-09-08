using backend.Domain.Persistence;

namespace backend.Data.Entities;

public class GameQuizRound
{
    public Guid Id { get; set; }

    public Guid GameId { get; set; }

    public Guid QuestionId { get; set; }

    public int AskOrder { get; set; }

    public DateTime AskedAtUtc { get; set; }

    public DateTime ClosesAtUtc { get; set; }

    public DateTime? ClosedAtUtc { get; set; }

    public Guid? AskedByUserId { get; set; }

    public string Status { get; set; } = GameQuizRoundStatusValue.Asked;

    public int QuestionRevisionSnapshot { get; set; }

    public string QuestionCodeSnapshot { get; set; } = string.Empty;

    public string CategoryNameSnapshot { get; set; } = string.Empty;

    public string QuestionTextSnapshot { get; set; } = string.Empty;

    public string[] AcceptedAnswersSnapshot { get; set; } = Array.Empty<string>();

    public string[] NormalizedAnswersSnapshot { get; set; } = Array.Empty<string>();

    public int RewardSnapshot { get; set; }

    public string DeliveryKind { get; set; } = string.Empty;

    public string? SourceChannelId { get; set; }

    public string? SourceMessageId { get; set; }

    public Game? Game { get; set; }

    public QuestionDefinition? Question { get; set; }

    public GameEnabledQuestion? EnabledQuestion { get; set; }

    public User? AskedByUser { get; set; }

    public GameQuizCorrectAnswer? CorrectAnswer { get; set; }
}
