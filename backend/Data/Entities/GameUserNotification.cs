namespace backend.Data.Entities;

public sealed class GameUserNotification
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid GameId { get; set; }

    public string Type { get; set; } = string.Empty;

    public int SchemaVersion { get; set; } = 1;

    public string PayloadJson { get; set; } = "{}";

    public string DeduplicationKey { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? ReadAtUtc { get; set; }

    public User User { get; set; } = default!;

    public Game Game { get; set; } = default!;

}
