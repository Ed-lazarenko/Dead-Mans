namespace backend.Data.Entities;

public class QuestionDefinition
{
    public Guid Id { get; set; }

    public string ExternalCode { get; set; } = string.Empty;

    public Guid CategoryId { get; set; }

    public string Text { get; set; } = string.Empty;

    public int Reward { get; set; }

    public int Revision { get; set; } = 1;

    public bool IsEnabled { get; set; } = true;

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAtUtc { get; set; }

    public int Priority { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public QuestionCategory? CategoryDefinition { get; set; }

    public ICollection<GameQuizRound> AskedInQuizRounds { get; set; } = new List<GameQuizRound>();

    public ICollection<GameEnabledQuestion> EnabledInGames { get; set; } =
        new List<GameEnabledQuestion>();

    public ICollection<QuestionAcceptedAnswer> AcceptedAnswers { get; set; } =
        new List<QuestionAcceptedAnswer>();
}
