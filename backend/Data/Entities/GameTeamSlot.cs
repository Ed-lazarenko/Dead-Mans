namespace backend.Data.Entities;

public class GameTeamSlot
{
    public Guid Id { get; set; }

    public Guid GameId { get; set; }

    public int SlotIndex { get; set; }

    public string Availability { get; set; } = string.Empty;

    public string? ReservedLabel { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public Game? Game { get; set; }
}
