namespace backend.Data.Entities;

public class GameTeam
{
    public Guid Id { get; set; }

    public Guid GameId { get; set; }

    public Guid SlotId { get; set; }

    public bool RecruitmentOpen { get; set; }

    public string Status { get; set; } = string.Empty;

    public Guid? CreatedByUserId { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public DateTime? ConfirmedAtUtc { get; set; }

    public Guid? ConfirmedByUserId { get; set; }

    public DateTime? RejectedAtUtc { get; set; }

    public Guid? RejectedByUserId { get; set; }

    public DateTime? DisbandedAtUtc { get; set; }

    public Guid? DisbandedByUserId { get; set; }

    public DateTime? DisbandRequestedAtUtc { get; set; }

    public Guid? DisbandRequestedByUserId { get; set; }

    public Game? Game { get; set; }

    public GameParticipationSlot? Slot { get; set; }

    public ICollection<GameTeamMember> Members { get; set; } = new List<GameTeamMember>();
}
