namespace backend.Data.Entities;

public class BoardCellMedia
{
    public Guid Id { get; set; }

    public Guid CellId { get; set; }

    public Guid MediaAssetId { get; set; }

    public string Role { get; set; } = "content";

    public int SortOrder { get; set; }

    public BoardCell Cell { get; set; } = default!;

    public MediaAsset MediaAsset { get; set; } = default!;
}
