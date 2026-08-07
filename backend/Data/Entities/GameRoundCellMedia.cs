namespace backend.Data.Entities;

public class GameRoundCellMedia
{
    public Guid Id { get; set; }

    public Guid RoundId { get; set; }

    public string Url { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public GameRound Round { get; set; } = default!;
}
