namespace backend.Data.Entities;

public sealed class GameQuizPointLedgerEntry
{
    public Guid Id { get; set; }

    /// <summary>Database-assigned order of immutable ledger events.</summary>
    public long SequenceNumber { get; set; }

    public Guid GameId { get; set; }

    public Guid UserId { get; set; }

    public string EntryType { get; set; } = string.Empty;

    public int PointsDelta { get; set; }

    public Guid? CorrectAnswerId { get; set; }

    public Guid? ModifierActivationId { get; set; }

    public Guid? ManualRequestId { get; set; }

    public Guid? CreatedByUserId { get; set; }

    public string? Reason { get; set; }

    public long AvailablePointsBefore { get; set; }

    public long AvailablePointsAfter { get; set; }

    public DateTime OccurredAtUtc { get; set; }

    public Game Game { get; set; } = default!;

    public User User { get; set; } = default!;

    public User? CreatedByUser { get; set; }

    public GameQuizCorrectAnswer? CorrectAnswer { get; set; }

    public GameModifierActivation? ModifierActivation { get; set; }
}
