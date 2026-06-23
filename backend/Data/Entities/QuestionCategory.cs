namespace backend.Data.Entities;

public class QuestionCategory
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public ICollection<QuestionDefinition> Questions { get; set; } = new List<QuestionDefinition>();
}
