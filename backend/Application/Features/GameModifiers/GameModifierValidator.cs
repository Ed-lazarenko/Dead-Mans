using System.Text.RegularExpressions;
using backend.Application.Contracts;

namespace backend.Application.Features.GameModifiers;

internal static class GameModifierValidator
{
    public const int MaxCodeLength = 64;
    public const int MaxNameLength = 128;
    public const int MaxDescriptionLength = 2000;
    public const int MaxCategoryLength = 64;
    public const int MaxScoringTypeLength = 64;
    public const int MaxTierLength = 16;
    public const int MaxIconEmojiLength = 16;
    public const int MaxActivationCommandLength = 128;

    private static readonly Regex CodePattern = new("^[a-z0-9_]+$", RegexOptions.Compiled);

    private static readonly string[] AllowedKinds =
    {
        GameModifierKinds.Active,
        GameModifierKinds.Passive
    };

    private static readonly string[] AllowedTiers =
    {
        GameModifierTiers.Low,
        GameModifierTiers.Mid,
        GameModifierTiers.High
    };

    public static bool TryNormalizeCode(string? code, out string normalizedCode)
    {
        normalizedCode = (code ?? string.Empty).Trim().ToLowerInvariant();
        return normalizedCode.Length is > 0 and <= MaxCodeLength && CodePattern.IsMatch(normalizedCode);
    }

    public static bool TryNormalizeCreate(
        CreateGameModifierInput input,
        out CreateGameModifierInput normalized
    )
    {
        normalized = input;
        if (!TryNormalizeCode(input.Code, out var code))
        {
            return false;
        }

        if (!TryNormalizeShared(
                input.Name,
                input.Description,
                input.Kind,
                input.Category,
                input.ScoringType,
                input.Tier,
                input.ActivationCost,
                input.DefaultLimitPerGame,
                input.IconEmoji,
                input.ActivationCommand,
                out var shared
            ))
        {
            return false;
        }

        normalized = new CreateGameModifierInput(
            code,
            shared.Name,
            shared.Description,
            shared.Kind,
            shared.Category,
            shared.ScoringType,
            shared.Tier,
            shared.ActivationCost,
            shared.DefaultLimitPerGame,
            shared.IconEmoji,
            shared.ActivationCommand
        );
        return true;
    }

    public static bool TryNormalizeUpdate(
        UpdateGameModifierInput input,
        out UpdateGameModifierInput normalized
    )
    {
        normalized = input;
        if (!TryNormalizeShared(
                input.Name,
                input.Description,
                input.Kind,
                input.Category,
                input.ScoringType,
                input.Tier,
                input.ActivationCost,
                input.DefaultLimitPerGame,
                input.IconEmoji,
                input.ActivationCommand,
                out var shared
            ))
        {
            return false;
        }

        normalized = new UpdateGameModifierInput(
            shared.Name,
            shared.Description,
            shared.Kind,
            shared.Category,
            shared.ScoringType,
            shared.Tier,
            shared.ActivationCost,
            shared.DefaultLimitPerGame,
            shared.IconEmoji,
            shared.ActivationCommand
        );
        return true;
    }

    private static bool TryNormalizeShared(
        string name,
        string description,
        string kind,
        string category,
        string scoringType,
        string tier,
        int activationCost,
        int? defaultLimitPerGame,
        string? iconEmoji,
        string? activationCommand,
        out UpdateGameModifierInput normalized
    )
    {
        normalized = default!;

        var normalizedName = (name ?? string.Empty).Trim();
        var normalizedDescription = (description ?? string.Empty).Trim();
        var normalizedKind = (kind ?? string.Empty).Trim().ToLowerInvariant();
        var normalizedCategory = (category ?? string.Empty).Trim();
        var normalizedScoringType = (scoringType ?? string.Empty).Trim();
        var normalizedTier = (tier ?? string.Empty).Trim().ToLowerInvariant();
        var normalizedIcon = NormalizeOptional(iconEmoji, MaxIconEmojiLength);
        var normalizedCommand = NormalizeOptional(activationCommand, MaxActivationCommandLength);

        if (normalizedName.Length is 0 or > MaxNameLength
            || normalizedDescription.Length is 0 or > MaxDescriptionLength
            || normalizedCategory.Length is 0 or > MaxCategoryLength
            || normalizedScoringType.Length is 0 or > MaxScoringTypeLength
            || normalizedTier.Length > MaxTierLength
            || !AllowedKinds.Contains(normalizedKind)
            || !AllowedTiers.Contains(normalizedTier)
            || activationCost < 0
            || defaultLimitPerGame is <= 0
            || normalizedIcon is { Length: > MaxIconEmojiLength }
            || normalizedCommand is { Length: > MaxActivationCommandLength })
        {
            return false;
        }

        normalized = new UpdateGameModifierInput(
            normalizedName,
            normalizedDescription,
            normalizedKind,
            normalizedCategory,
            normalizedScoringType,
            normalizedTier,
            activationCost,
            defaultLimitPerGame,
            normalizedIcon,
            normalizedCommand
        );
        return true;
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        var trimmed = (value ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            return null;
        }

        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
    }
}
