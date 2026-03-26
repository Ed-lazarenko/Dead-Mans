namespace backend.Data.Entities;

public class GameBoard
{
    public Guid Id { get; set; }

    public Guid GameId { get; set; }

    public int Version { get; set; } = 1;

    public int Rows { get; set; }

    public int Cols { get; set; }

    public string[] RowLabels { get; set; } = Array.Empty<string>();

    public string[] ColLabels { get; set; } = Array.Empty<string>();

    public DateTime CreatedAtUtc { get; set; }

    public Game Game { get; set; } = default!;

    public ICollection<BoardCell> Cells { get; set; } = new List<BoardCell>();
}
