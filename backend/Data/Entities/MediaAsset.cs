namespace backend.Data.Entities;

public class MediaAsset
{
    public Guid Id { get; set; }

    public string Bucket { get; set; } = string.Empty;

    public string ObjectKey { get; set; } = string.Empty;

    public string MimeType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public string Scope { get; set; } = "private";

    public string Status { get; set; } = "pending";

    public DateTime CreatedAtUtc { get; set; }

    public ICollection<BoardCellMedia> CellLinks { get; set; } = new List<BoardCellMedia>();
}
