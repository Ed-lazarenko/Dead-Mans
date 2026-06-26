using backend.Application.Contracts;

namespace backend.Application.Features.GameModifiers;

internal static class GameModifierValidator
{
    public const int MaxNameLength = 128;
    public const int MaxDescriptionLength = 2000;
    public const int MaxScoringTypeLength = 64;
    public const int MaxIconEmojiLength = 16;
    public const int MaxActivationCommandLength = 128;
    public const int MaxMechanicTextLength = 512;

    private static readonly string[] AllowedMechanicTypes =
    {
        GameModifierMechanicTypes.RuleOnly,
        GameModifierMechanicTypes.RestrictionWithReward,
        GameModifierMechanicTypes.KillCounter,
        GameModifierMechanicTypes.Multiplier,
        GameModifierMechanicTypes.Mentor
    };

    private static readonly string[] AllowedScoringTypes =
    {
        GameModifierScoringTypes.ConditionalBonus,
        GameModifierScoringTypes.ConditionalBonusPenalty,
        GameModifierScoringTypes.Multiplier,
        GameModifierScoringTypes.NonScoring
    };

    private static readonly string[] AllowedCategories =
    {
        GameModifierCategories.Preparation,
        GameModifierCategories.Round,
        GameModifierCategories.Result
    };

    public static bool TryNormalizeCreate(
        CreateGameModifierInput input,
        out CreateGameModifierInput normalized
    )
    {
        normalized = input;
        if (!TryNormalizeShared(
                input.Name,
                input.Description,
                input.ScoringType,
                input.Category,
                input.RequiresHostControl,
                input.MechanicType,
                input.ActivationCost,
                input.DefaultLimitPerGame,
                input.ActivationLimit,
                input.Effect,
                input.ConflictingModifierIds,
                input.IconEmoji,
                input.ActivationCommand,
                out var shared
            ))
        {
            return false;
        }

        normalized = new CreateGameModifierInput(
            shared.Name,
            shared.Description,
            shared.ScoringType,
            shared.Category,
            shared.RequiresHostControl,
            shared.MechanicType,
            shared.ActivationCost,
            shared.DefaultLimitPerGame,
            shared.ActivationLimit,
            shared.Effect,
            shared.ConflictingModifierIds,
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
                input.ScoringType,
                input.Category,
                input.RequiresHostControl,
                input.MechanicType,
                input.ActivationCost,
                input.DefaultLimitPerGame,
                input.ActivationLimit,
                input.Effect,
                input.ConflictingModifierIds,
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
            shared.ScoringType,
            shared.Category,
            shared.RequiresHostControl,
            shared.MechanicType,
            shared.ActivationCost,
            shared.DefaultLimitPerGame,
            shared.ActivationLimit,
            shared.Effect,
            shared.ConflictingModifierIds,
            shared.IconEmoji,
            shared.ActivationCommand
        );
        return true;
    }

    private static bool TryNormalizeShared(
        string name,
        string description,
        string scoringType,
        string category,
        bool requiresHostControl,
        string mechanicType,
        int activationCost,
        int? defaultLimitPerGame,
        GameModifierActivationLimit activationLimit,
        GameModifierEffect effect,
        IReadOnlyList<Guid> conflictingModifierIds,
        string? iconEmoji,
        string? activationCommand,
        out UpdateGameModifierInput normalized
    )
    {
        normalized = default!;

        var normalizedName = (name ?? string.Empty).Trim();
        var normalizedDescription = (description ?? string.Empty).Trim();
        var normalizedScoringType = (scoringType ?? string.Empty).Trim();
        var normalizedCategory = (category ?? string.Empty).Trim().ToLowerInvariant();
        var normalizedMechanicType = (mechanicType ?? string.Empty).Trim().ToLowerInvariant();
        var normalizedIcon = NormalizeOptional(iconEmoji, MaxIconEmojiLength);
        var normalizedCommand = NormalizeOptional(activationCommand, MaxActivationCommandLength);
        var normalizedConflicts = (conflictingModifierIds ?? Array.Empty<Guid>())
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();

        if (normalizedName.Length is 0 or > MaxNameLength
            || normalizedDescription.Length is 0 or > MaxDescriptionLength
            || normalizedScoringType.Length is 0 or > MaxScoringTypeLength
            || !AllowedScoringTypes.Contains(normalizedScoringType)
            || !AllowedCategories.Contains(normalizedCategory)
            || !AllowedMechanicTypes.Contains(normalizedMechanicType)
            || !IsScoringTypeCompatible(normalizedMechanicType, normalizedScoringType)
            || activationCost < 0
            || defaultLimitPerGame is <= 0
            || normalizedIcon is { Length: > MaxIconEmojiLength }
            || normalizedCommand is { Length: > MaxActivationCommandLength })
        {
            return false;
        }

        if (!TryNormalizeActivationLimit(
                activationLimit,
                defaultLimitPerGame,
                out var normalizedActivationLimit
            )
            || !TryNormalizeEffect(effect, normalizedMechanicType, out var normalizedEffect))
        {
            return false;
        }

        normalized = new UpdateGameModifierInput(
            normalizedName,
            normalizedDescription,
            normalizedScoringType,
            normalizedCategory,
            requiresHostControl,
            normalizedMechanicType,
            activationCost,
            normalizedActivationLimit.Count,
            normalizedActivationLimit,
            normalizedEffect,
            normalizedConflicts,
            normalizedIcon,
            normalizedCommand
        );
        return true;
    }

    private static bool TryNormalizeActivationLimit(
        GameModifierActivationLimit activationLimit,
        int? legacyLimit,
        out GameModifierActivationLimit normalized
    )
    {
        normalized = default!;
        var count = activationLimit?.Count ?? legacyLimit;

        if (count is <= 0)
        {
            return false;
        }

        normalized = new GameModifierActivationLimit(count);
        return true;
    }

    private static bool TryNormalizeEffect(
        GameModifierEffect effect,
        string mechanicType,
        out GameModifierEffect normalized
    )
    {
        normalized = default!;
        if (effect is null)
        {
            return false;
        }

        var effectMechanicType = (effect.MechanicType ?? string.Empty).Trim().ToLowerInvariant();
        if (effectMechanicType != mechanicType)
        {
            return false;
        }

        if (effect.DurationSeconds is <= 0
            || IsTooLong(effect.RuleText, MaxMechanicTextLength)
            || HasTooLongText(effect.Traits, MaxMechanicTextLength)
            || HasTooLongText(effect.ResolutionInputs, MaxMechanicTextLength)
            || HasInvalidConditions(effect.Conditions))
        {
            return false;
        }

        var traits = NormalizeTextArray(effect.Traits, MaxMechanicTextLength);
        var conditions = (effect.Conditions ?? Array.Empty<GameModifierCondition>())
            .Select(
                condition =>
                    new GameModifierCondition(
                        (condition.Type ?? string.Empty).Trim(),
                        (condition.Source ?? string.Empty).Trim()
                    )
            )
            .Where(condition => condition.Type.Length > 0 && condition.Source.Length > 0)
            .ToArray();
        var resolutionInputs = NormalizeTextArray(effect.ResolutionInputs, MaxMechanicTextLength);
        var ruleText = NormalizeOptional(effect.RuleText, MaxMechanicTextLength);
        var scoreImpact = effect.ScoreImpact;
        var killEffect = NormalizeKillEffect(effect.KillEffect);
        var multiplierEffect = NormalizeMultiplierEffect(effect.MultiplierEffect);
        var mentorEffect = NormalizeMentorEffect(effect.MentorEffect);

        if (!IsMechanicPayloadValid(
                mechanicType,
                scoreImpact,
                killEffect,
                multiplierEffect,
                mentorEffect
            ))
        {
            return false;
        }

        normalized = new GameModifierEffect(
            mechanicType,
            traits,
            effect.DurationSeconds,
            ruleText,
            scoreImpact,
            conditions,
            resolutionInputs,
            killEffect,
            multiplierEffect,
            mentorEffect
        );
        return true;
    }

    private static bool IsMechanicPayloadValid(
        string mechanicType,
        GameModifierScoreImpact? scoreImpact,
        GameModifierKillEffect? killEffect,
        GameModifierMultiplierEffect? multiplierEffect,
        GameModifierMentorEffect? mentorEffect
    )
    {
        return mechanicType switch
        {
            GameModifierMechanicTypes.RuleOnly => true,
            GameModifierMechanicTypes.RestrictionWithReward =>
                scoreImpact is not null
                && (scoreImpact.PointsDelta.HasValue
                    || scoreImpact.PerKillBonus.HasValue
                    || scoreImpact.FailurePenaltyPoints.HasValue
                    || scoreImpact.KillDelta.HasValue
                    || scoreImpact.MultiplierDelta.HasValue),
            GameModifierMechanicTypes.KillCounter =>
                killEffect is not null
                && !string.IsNullOrWhiteSpace(killEffect.KillDeltaMode)
                && killEffect.KillDeltaValue is > 0,
            GameModifierMechanicTypes.Multiplier =>
                multiplierEffect is not null
                && !string.IsNullOrWhiteSpace(multiplierEffect.Target)
                && multiplierEffect.Delta is not null and not 0,
            GameModifierMechanicTypes.Mentor =>
                mentorEffect is not null && mentorEffect.DurationSeconds is null or > 0,
            _ => false
        };
    }

    private static bool IsScoringTypeCompatible(string mechanicType, string scoringType)
    {
        return mechanicType switch
        {
            GameModifierMechanicTypes.RestrictionWithReward =>
                scoringType == GameModifierScoringTypes.ConditionalBonusPenalty,
            GameModifierMechanicTypes.KillCounter =>
                scoringType == GameModifierScoringTypes.ConditionalBonus,
            GameModifierMechanicTypes.Multiplier =>
                scoringType == GameModifierScoringTypes.Multiplier,
            _ => scoringType == GameModifierScoringTypes.NonScoring
        };
    }

    private static GameModifierKillEffect? NormalizeKillEffect(GameModifierKillEffect? effect)
    {
        if (effect is not null
            && (IsTooLong(effect.KillDeltaMode, MaxMechanicTextLength)
                || IsTooLong(effect.Condition, MaxMechanicTextLength)
                || HasTooLongText(effect.ExcludedWeapons, MaxMechanicTextLength)))
        {
            return null;
        }

        return effect is null
            ? null
            : new GameModifierKillEffect(
                NormalizeOptional(effect.KillDeltaMode, MaxMechanicTextLength),
                effect.KillDeltaValue,
                NormalizeOptional(effect.Condition, MaxMechanicTextLength),
                NormalizeTextArray(effect.ExcludedWeapons, MaxMechanicTextLength)
            );
    }

    private static GameModifierMultiplierEffect? NormalizeMultiplierEffect(
        GameModifierMultiplierEffect? effect
    )
    {
        if (effect is not null
            && (IsTooLong(effect.Target, MaxMechanicTextLength)
                || IsTooLong(effect.ActiveWindow, MaxMechanicTextLength)
                || IsTooLong(effect.StopCondition, MaxMechanicTextLength)))
        {
            return null;
        }

        return effect is null
            ? null
            : new GameModifierMultiplierEffect(
                NormalizeOptional(effect.Target, MaxMechanicTextLength),
                effect.Delta,
                NormalizeOptional(effect.ActiveWindow, MaxMechanicTextLength),
                NormalizeOptional(effect.StopCondition, MaxMechanicTextLength)
            );
    }

    private static GameModifierMentorEffect? NormalizeMentorEffect(GameModifierMentorEffect? effect)
    {
        if (effect is not null
            && IsTooLong(effect.LoadoutText, MaxMechanicTextLength))
        {
            return null;
        }

        return effect is null
            ? null
            : new GameModifierMentorEffect(
                NormalizeOptional(effect.LoadoutText, MaxMechanicTextLength),
                effect.DurationSeconds,
                effect.CanBeRevived,
                effect.CanBeKilled,
                effect.KillsCreditToTeam
            );
    }

    private static string[] NormalizeTextArray(string[]? values, int maxLength)
    {
        return (values ?? Array.Empty<string>())
            .Select(value => (value ?? string.Empty).Trim())
            .Where(value => value.Length > 0)
            .Select(value => value.Length > maxLength ? value[..maxLength] : value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool HasInvalidConditions(GameModifierCondition[]? conditions)
    {
        return (conditions ?? Array.Empty<GameModifierCondition>())
            .Any(
                condition =>
                    IsTooLong(condition.Type, MaxMechanicTextLength)
                    || IsTooLong(condition.Source, MaxMechanicTextLength)
            );
    }

    private static bool HasTooLongText(string[]? values, int maxLength)
    {
        return (values ?? Array.Empty<string>()).Any(value => IsTooLong(value, maxLength));
    }

    private static bool IsTooLong(string? value, int maxLength)
    {
        return (value ?? string.Empty).Trim().Length > maxLength;
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
