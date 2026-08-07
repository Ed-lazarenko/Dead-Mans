using backend.Domain.Persistence;

namespace backend.Data.Entities;

public class GameQuizRound
{
    public Guid Id { get; set; }

    public Guid GameId { get; set; }

    public Guid QuestionId { get; set; }

    public int AskOrder { get; set; }

    public DateTime AskedAtUtc { get; set; }

    public Guid? AskedByUserId { get; set; }

    public string Status { get; set; } = GameQuizRoundStatusValue.Asked;

    public DateTime? AnsweredAtUtc { get; set; }

    public Guid? AnsweredByUserId { get; set; }

    public Guid? AnsweredForUserId { get; set; }

    public string? AnsweredByDisplayName { get; set; }

    public string? SubmittedAnswer { get; set; }

    public bool? IsCorrect { get; set; }

    public int? AwardedPoints { get; set; }

    public Game? Game { get; set; }

    public QuestionDefinition? Question { get; set; }

    public User? AskedByUser { get; set; }

    public User? AnsweredByUser { get; set; }

    public User? AnsweredForUser { get; set; }
}
