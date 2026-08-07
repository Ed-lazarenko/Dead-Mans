using backend.Domain.Persistence;

namespace backend.Data.Entities;

public class Game
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Status { get; set; } = GameStatusValue.Draft;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? ReadyAtUtc { get; set; }

    public DateTime? StartedAtUtc { get; set; }

    public DateTime? FinishedAtUtc { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAtUtc { get; set; }

    public short MinPlayersPerTeam { get; set; } = 1;

    public short MaxPlayersPerTeam { get; set; } = 2;

    public Guid? ActiveTeamId { get; set; }

    public GameTeam? ActiveTeam { get; set; }

    public GameBoard? Board { get; set; }

    public ICollection<GameTeamSlot> TeamSlots { get; set; } =
        new List<GameTeamSlot>();

    public ICollection<GameEnabledModifier> EnabledModifiers { get; set; } =
        new List<GameEnabledModifier>();

    public ICollection<GameModifierActivation> ModifierActivations { get; set; } =
        new List<GameModifierActivation>();

    public ICollection<GameEnabledQuestion> EnabledQuestions { get; set; } =
        new List<GameEnabledQuestion>();
}
