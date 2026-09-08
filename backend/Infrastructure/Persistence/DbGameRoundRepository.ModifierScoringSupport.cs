using backend.Application.Contracts;
using backend.Data.Entities;
using backend.Domain.GameModifiers;
using System.Text.Json;

namespace backend.Infrastructure.Persistence;

public sealed partial class DbGameRoundRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static bool IsConfigurationError(string code) => code is
        "behavior.invalid"
        or "behavior.rule_incompatible"
        or "formula.unsupported"
        or "formula.incompatible"
        or "resolution.invalid"
        or "round_facts.invalid"
        or "activation.duplicate"
        or "modifier_calculation.failed";

    private static GameRoundModifierSnapshot ToModifierSnapshot(GameRoundModifierResult modifier)
    {
        return new GameRoundModifierSnapshot(
            modifier.Id,
            modifier.ModifierId,
            modifier.ModifierNameSnapshot,
            modifier.ModifierCategorySnapshot,
            modifier.ModifierDescriptionSnapshot,
            modifier.OutcomeStatus,
            modifier.ScoreDelta,
            modifier.KillDelta,
            modifier.MultiplierApplied,
            modifier.ResolutionDataJson,
            modifier.ResolvedByUserId,
            modifier.ResolvedAtUtc,
            modifier.GameModifierActivationId,
            modifier.DefinitionRevisionSnapshot,
            modifier.ResolutionGroupId,
            modifier.ResolutionKind,
            modifier.ViolationComment,
            string.IsNullOrWhiteSpace(modifier.ModifierBehaviorV2SnapshotJson)
                ? null
                : ModifierBehaviorV2Json.Deserialize(modifier.ModifierBehaviorV2SnapshotJson)
        );
    }

    private static string? ResolveResolutionKind(string? behaviorJson)
    {
        if (string.IsNullOrWhiteSpace(behaviorJson))
        {
            return null;
        }

        var behavior = ModifierBehaviorV2Json.Deserialize(behaviorJson);
        return behavior.Resolution switch
        {
            RuleStatusResolution => "ruleStatus",
            BooleanResolution => "boolean",
            NonNegativeCountResolution => "nonNegativeCount",
            AutomaticRoundMetricResolution => "automaticRoundMetric",
            PerActivationResolution => "perActivation",
            _ => throw new InvalidOperationException("Unsupported modifier resolution type.")
        };
    }

    private static string ResolveBehaviorSnapshotJson(
        Data.Entities.GameModifierActivation activation
    )
        => ModifierBehaviorV2Json.Serialize(
            ModifierBehaviorV2Json.Deserialize(activation.BehaviorV2SnapshotJson)
        );

    private sealed class ModifierScoringException(
        string code,
        bool isConfigurationError = false
    ) : Exception(code)
    {
        public string Code { get; } = code;
        public bool IsConfigurationError { get; } = isConfigurationError;
    }
}
