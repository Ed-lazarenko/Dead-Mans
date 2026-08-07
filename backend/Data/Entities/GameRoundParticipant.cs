namespace backend.Data.Entities;

public class GameRoundParticipant
{
    public Guid Id { get; set; }

    public Guid RoundId { get; set; }

    public Guid UserId { get; set; }

    public string DisplayNameSnapshot { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public GameRound Round { get; set; } = default!;

    public User User { get; set; } = default!;
}
