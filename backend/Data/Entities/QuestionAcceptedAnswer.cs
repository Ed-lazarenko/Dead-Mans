namespace backend.Data.Entities;

public sealed class QuestionAcceptedAnswer
{
    public Guid Id { get; set; }

    public Guid QuestionId { get; set; }

    public string AnswerText { get; set; } = string.Empty;

    public string NormalizedAnswer { get; set; } = string.Empty;

    public bool IsPrimary { get; set; }

    public int SortOrder { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public QuestionDefinition Question { get; set; } = default!;
}
