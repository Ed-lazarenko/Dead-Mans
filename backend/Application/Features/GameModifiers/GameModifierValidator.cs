using System.Globalization;
using System.Text;
using backend.Application.Contracts;
using backend.Domain.GameModifiers;

namespace backend.Application.Features.GameModifiers;

internal static class GameModifierValidator
{
    public const int MaxNameLength = 128;
    public const int MaxDescriptionLength = 2000;
    public const int MaxIconEmojiLength = 16;
    public const int MaxActivationCommandLength = 128;
    public const int MaxNormalizedTags = 5;
    public const int MaxNormalizedTagGraphemes = 32;

    public static bool TryNormalizeCreate(CreateGameModifierInput input, out CreateGameModifierInput normalized)
    {
        normalized = input;
        if (!TryNormalizeShared(input.Name, input.Description, input.Category, input.ActivationCost,
                input.ActivationLimit, input.ConflictingModifierIds, input.IconEmoji,
                input.ActivationCommand, input.NormalizedTags, input.BehaviorV2, out var shared))
        {
            return false;
        }

        normalized = new CreateGameModifierInput(shared.Name, shared.Description, shared.Category,
            shared.ActivationCost, shared.ActivationLimit, shared.ConflictingModifierIds,
            shared.IconEmoji, shared.ActivationCommand, shared.NormalizedTags, shared.BehaviorV2);
        return true;
    }

    public static bool TryNormalizeUpdate(UpdateGameModifierInput input, out UpdateGameModifierInput normalized) =>
        TryNormalizeShared(input.Name, input.Description, input.Category, input.ActivationCost,
            input.ActivationLimit, input.ConflictingModifierIds, input.IconEmoji,
            input.ActivationCommand, input.NormalizedTags, input.BehaviorV2, out normalized);

    private static bool TryNormalizeShared(string name, string description, string category,
        int activationCost, GameModifierActivationLimit activationLimit,
        IReadOnlyList<Guid> conflictingModifierIds, string? iconEmoji, string? activationCommand,
        IReadOnlyList<string>? normalizedTags, ModifierBehaviorV2 behaviorV2,
        out UpdateGameModifierInput normalized)
    {
        normalized = default!;
        var normalizedName = (name ?? string.Empty).Trim();
        var normalizedDescription = (description ?? string.Empty).Trim();
        var normalizedCategory = (category ?? string.Empty).Trim().ToLowerInvariant();
        var normalizedIcon = NormalizeOptional(iconEmoji, MaxIconEmojiLength);
        var normalizedCommand = NormalizeOptional(activationCommand, MaxActivationCommandLength)
            ?? GenerateActivationCommand(normalizedName);
        var normalizedConflicts = (conflictingModifierIds ?? Array.Empty<Guid>())
            .Where(id => id != Guid.Empty).Distinct().ToArray();
        var normalizedTagValues = (normalizedTags ?? Array.Empty<string>())
            .Select(NormalizeTag).Where(value => value.Length > 0)
            .Distinct(StringComparer.InvariantCultureIgnoreCase).ToArray();
        var count = activationLimit?.Count;

        if (behaviorV2 is null)
        {
            return false;
        }

        var behaviorPhase = behaviorV2.Phase switch
        {
            ModifierPhase.Preparation => "preparation",
            ModifierPhase.Round => "round",
            ModifierPhase.Result => "result",
            _ => string.Empty
        };

        if (normalizedName.Length is 0 or > MaxNameLength
            || normalizedDescription.Length is 0 or > MaxDescriptionLength
            || normalizedCategory != behaviorPhase
            || activationCost < 0
            || count is <= 0
            || normalizedTagValues.Length > MaxNormalizedTags
            || normalizedTagValues.Any(value => StringInfo.ParseCombiningCharacters(value).Length > MaxNormalizedTagGraphemes)
            || ModifierBehaviorValidator.Validate(behaviorV2) is not null)
        {
            return false;
        }

        normalized = new UpdateGameModifierInput(normalizedName, normalizedDescription,
            normalizedCategory, activationCost, new GameModifierActivationLimit(count),
            normalizedConflicts, normalizedIcon, normalizedCommand, normalizedTagValues, behaviorV2);
        return true;
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        var trimmed = (value ?? string.Empty).Trim();
        if (trimmed.Length == 0) return null;
        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
    }

    private static string NormalizeTag(string? value)
    {
        var source = (value ?? string.Empty).Normalize(NormalizationForm.FormKC);
        var builder = new StringBuilder(source.Length);
        var pendingSpace = false;
        foreach (var character in source)
        {
            if (char.IsWhiteSpace(character))
            {
                pendingSpace = builder.Length > 0;
                continue;
            }
            if (pendingSpace)
            {
                builder.Append(' ');
                pendingSpace = false;
            }
            builder.Append(character);
        }
        return builder.ToString();
    }

    private static string GenerateActivationCommand(string normalizedName)
    {
        const string prefix = "!активировать ";
        var availableLength = MaxActivationCommandLength - prefix.Length;
        var loweredName = normalizedName.ToLowerInvariant();
        var textElements = StringInfo.ParseCombiningCharacters(loweredName);
        if (textElements.Length > availableLength)
        {
            loweredName = loweredName[..textElements[availableLength]];
        }
        return prefix + loweredName;
    }
}
