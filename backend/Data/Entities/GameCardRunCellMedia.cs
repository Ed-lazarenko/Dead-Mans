namespace backend.Data.Entities;

public class GameCardRunCellMedia
{
    public Guid Id { get; set; }

    public Guid CardRunId { get; set; }

    public string Url { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public GameCardRun CardRun { get; set; } = default!;
}
