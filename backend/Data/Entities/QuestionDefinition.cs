namespace backend.Data.Entities;

public class QuestionDefinition
{
    public Guid Id { get; set; }

    public string ExternalCode { get; set; } = string.Empty;

    public Guid CategoryId { get; set; }

    public string Text { get; set; } = string.Empty;

    public string Answer { get; set; } = string.Empty;

    public string NormalizedAnswer { get; set; } = string.Empty;

    public int Reward { get; set; }

    public bool IsEnabled { get; set; } = true;

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAtUtc { get; set; }

    public int SortOrder { get; set; }

    public int AskedTotalCount { get; set; }

    public int CorrectTotalCount { get; set; }

    public DateTime? LastAskedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public QuestionCategory? CategoryDefinition { get; set; }

    public ICollection<GameQuestionRound> AskedInGames { get; set; } = new List<GameQuestionRound>();

    public ICollection<GameQuestionSelection> GameSelections { get; set; } =
        new List<GameQuestionSelection>();
}
