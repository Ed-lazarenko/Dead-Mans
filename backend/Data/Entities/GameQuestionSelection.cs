namespace backend.Data.Entities;

public class GameQuestionSelection
{
    public Guid GameId { get; set; }

    public Guid QuestionId { get; set; }

    public DateTime EnabledAtUtc { get; set; }

    public Game Game { get; set; } = default!;

    public QuestionDefinition QuestionDefinition { get; set; } = default!;
}
