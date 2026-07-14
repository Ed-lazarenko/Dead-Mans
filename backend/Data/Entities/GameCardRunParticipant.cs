namespace backend.Data.Entities;

public class GameCardRunParticipant
{
    public Guid Id { get; set; }

    public Guid CardRunId { get; set; }

    public Guid UserId { get; set; }

    public string DisplayNameSnapshot { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public GameCardRun CardRun { get; set; } = default!;

    public User User { get; set; } = default!;
}
