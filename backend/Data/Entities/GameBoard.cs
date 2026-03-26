namespace backend.Data.Entities;

public class GameBoard
{
    public Guid Id { get; set; }

    public Guid GameId { get; set; }

    public int Version { get; set; } = 1;

    public DateTime CreatedAtUtc { get; set; }

    public Game Game { get; set; } = default!;

    public ICollection<BoardCell> Cells { get; set; } = new List<BoardCell>();
}
