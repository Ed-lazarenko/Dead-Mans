using backend.Domain.Persistence;

namespace backend.Data.Entities;

public class GameRound
{
    public Guid Id { get; set; }

    public Guid GameId { get; set; }

    public Guid BoardCellId { get; set; }

    public Guid TeamId { get; set; }

    public string Status { get; set; } = GameRoundStatusValue.InProgress;

    public int Version { get; set; } = 1;

    public DateTime StartedAtUtc { get; set; }

    public DateTime? PreparedAtUtc { get; set; }

    public DateTime? GameplayStartedAtUtc { get; set; }

    public DateTime? ReviewedAtUtc { get; set; }

    public DateTime? FinishedAtUtc { get; set; }

    public int BaseScore { get; set; }

    public int? FinalScore { get; set; }

    public bool EmptyCardPenaltyApplied { get; set; }

    public int KillsCount { get; set; }

    public int BountyCount { get; set; }

    public int TeamSlotIndexSnapshot { get; set; }

    public int CellRowIndex { get; set; }

    public int CellColIndex { get; set; }

    public string? CellTitleSnapshot { get; set; }

    public string? CellDescriptionSnapshot { get; set; }

    public int CellCostSnapshot { get; set; }

    public string? Notes { get; set; }

    public string? TechnicalCancellationReasonCode { get; set; }

    public string? PublicCancellationSummary { get; set; }

    public string? InternalCancellationDetail { get; set; }

    public Guid? ResolvedByUserId { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public Game Game { get; set; } = default!;

    public BoardCell BoardCell { get; set; } = default!;

    public GameTeam Team { get; set; } = default!;

    public User? ResolvedByUser { get; set; }

    public ICollection<GameRoundParticipant> Participants { get; set; } =
        new List<GameRoundParticipant>();

    public ICollection<GameRoundCellMedia> CellMedia { get; set; } =
        new List<GameRoundCellMedia>();

    public ICollection<GameRoundModifierResult> ModifierResults { get; set; } =
        new List<GameRoundModifierResult>();

    public ICollection<GameRoundTransitionAudit> TransitionAudits { get; set; } =
        new List<GameRoundTransitionAudit>();
}
