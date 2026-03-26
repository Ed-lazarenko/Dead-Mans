namespace backend.Data.Entities;

public class BoardCell
{
    public Guid Id { get; set; }

    public Guid BoardId { get; set; }

    public int RowIndex { get; set; }

    public int ColIndex { get; set; }

    public string State { get; set; } = "closed";

    public string CellType { get; set; } = "loadout";

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public GameBoard Board { get; set; } = default!;

    public ICollection<BoardCellMedia> MediaLinks { get; set; } = new List<BoardCellMedia>();
}
