namespace backend.Data.Entities;

public class GameRoundCellMedia
{
    public Guid Id { get; set; }

    public Guid RoundId { get; set; }

    public string Bucket { get; set; } = string.Empty;

    public string ObjectKey { get; set; } = string.Empty;

    public string MimeType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public string Role { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public GameRound Round { get; set; } = default!;
}
