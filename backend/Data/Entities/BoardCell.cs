namespace backend.Data.Entities;

public enum BoardCellState
{
    Closed,
    Open
}

public class BoardCell
{
    public Guid Id { get; set; }

    public Guid BoardId { get; set; }

    public int RowIndex { get; set; }

    public int ColIndex { get; set; }

    public BoardCellState State { get; set; } = BoardCellState.Closed;

    public string CellType { get; set; } = "loadout";

    public string? Title { get; set; }

    public int Cost { get; set; }

    public string? Description { get; set; }

    public GameBoard Board { get; set; } = default!;

    public ICollection<BoardCellMedia> MediaLinks { get; set; } = new List<BoardCellMedia>();
}
