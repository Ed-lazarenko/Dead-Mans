using backend.Domain.Persistence;

namespace backend.Data.Entities;

public class GameQuizManualAward
{
    public Guid Id { get; set; }

    public Guid GameId { get; set; }

    public Guid AwardedToUserId { get; set; }

    public Guid AwardedByUserId { get; set; }

    public int Points { get; set; }

    public string OperationType { get; set; } = GameQuizManualAdjustmentOperationValue.Award;

    public string? Reason { get; set; }

    public Guid? RequestId { get; set; }

    public int? AvailablePointsBefore { get; set; }

    public int? AvailablePointsAfter { get; set; }

    public DateTime AwardedAtUtc { get; set; }

    public Game? Game { get; set; }

    public User? AwardedToUser { get; set; }

    public User? AwardedByUser { get; set; }
}
