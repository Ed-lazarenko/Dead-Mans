namespace backend.Data.Entities;

public sealed class GameRoundTransitionAudit
{
    public Guid RoundId { get; set; }

    public int Sequence { get; set; }

    public string? FromStatus { get; set; }

    public string ToStatus { get; set; } = string.Empty;

    public string ActionCode { get; set; } = string.Empty;

    public Guid InitiatedByUserId { get; set; }

    public DateTime OccurredAtUtc { get; set; }

    public string? Reason { get; set; }

    public int ResultingRoundVersion { get; set; }

    public GameRound Round { get; set; } = default!;

    public User InitiatedByUser { get; set; } = default!;
}
