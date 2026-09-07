namespace backend.Data.Entities;

public class GameEnabledModifier
{
    public Guid GameId { get; set; }

    public Guid ModifierId { get; set; }

    public Guid? ModifierVersionId { get; set; }

    public DateTime? VersionPinnedAtUtc { get; set; }

    public DateTime EnabledAtUtc { get; set; }

    public DateTime? EmergencyDisabledAtUtc { get; set; }

    public Guid? EmergencyDisabledByUserId { get; set; }

    public string? EmergencyDisableReason { get; set; }

    public Game Game { get; set; } = default!;

    public ModifierDefinition ModifierDefinition { get; set; } = default!;

    public ModifierDefinitionVersion? ModifierVersion { get; set; }

    public User? EmergencyDisabledByUser { get; set; }
}
