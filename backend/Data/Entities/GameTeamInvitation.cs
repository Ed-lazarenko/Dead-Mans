namespace backend.Data.Entities;

public class GameTeamInvitation
{
    public Guid Id { get; set; }

    public Guid GameId { get; set; }

    public Guid SlotId { get; set; }

    public Guid? TeamId { get; set; }

    public Guid InvitedUserId { get; set; }

    public Guid InvitedByUserId { get; set; }

    public string InvitedByKind { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? RespondedAtUtc { get; set; }

    public Game? Game { get; set; }

    public GameTeamSlot? Slot { get; set; }

    public GameTeam? Team { get; set; }
}
