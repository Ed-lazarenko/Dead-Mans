namespace backend.Data.Entities;

public class GameQuizManualAward
{
    public Guid Id { get; set; }

    public Guid GameId { get; set; }

    public Guid AwardedToUserId { get; set; }

    public Guid AwardedByUserId { get; set; }

    public int Points { get; set; }

    public DateTime AwardedAtUtc { get; set; }

    public Game? Game { get; set; }

    public User? AwardedToUser { get; set; }

    public User? AwardedByUser { get; set; }
}
