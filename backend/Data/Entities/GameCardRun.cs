using backend.Domain.Persistence;

namespace backend.Data.Entities;

public class GameCardRun
{
    public Guid Id { get; set; }

    public Guid GameId { get; set; }

    public Guid BoardCellId { get; set; }

    public Guid TeamId { get; set; }

    public string Status { get; set; } = GameCardRunStatusValue.InProgress;

    public DateTime StartedAtUtc { get; set; }

    public DateTime? FinishedAtUtc { get; set; }

    public int BaseScore { get; set; }

    public int? FinalScore { get; set; }

    public int KillsCount { get; set; }

    public int BountyCount { get; set; }

    public int TeamSlotIndexSnapshot { get; set; }

    public int CellRowIndex { get; set; }

    public int CellColIndex { get; set; }

    public string? CellTitleSnapshot { get; set; }

    public string? CellDescriptionSnapshot { get; set; }

    public int CellCostSnapshot { get; set; }

    public string? Notes { get; set; }

    public Guid? ResolvedByUserId { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public Game Game { get; set; } = default!;

    public BoardCell BoardCell { get; set; } = default!;

    public GameTeam Team { get; set; } = default!;

    public User? ResolvedByUser { get; set; }

    public ICollection<GameCardRunParticipant> Participants { get; set; } =
        new List<GameCardRunParticipant>();

    public ICollection<GameCardRunCellMedia> CellMedia { get; set; } =
        new List<GameCardRunCellMedia>();

    public ICollection<GameCardRunModifierResult> ModifierResults { get; set; } =
        new List<GameCardRunModifierResult>();
}
