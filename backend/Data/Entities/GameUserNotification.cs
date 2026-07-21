namespace backend.Data.Entities;

public sealed class GameUserNotification
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Type { get; set; } = string.Empty;

    public string? ModifierName { get; set; }

    public string? ActorDisplayName { get; set; }

    public int? QuizPointsDelta { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? ReadAtUtc { get; set; }

    public User User { get; set; } = default!;
}
