using System.Text.Json;
using System.Text.Json.Serialization;

namespace backend.Domain.GameModifiers;

public static class ModifierBehaviorSchemaVersions
{
    public const int V2 = 2;
}

public enum ModifierBehaviorKind { Rule, Scoring }
public enum ModifierPhase { Preparation, Round, Result }
public enum ModifierPerformer { ActiveTeam, Mentor }
public enum ModifierStackingPolicy { AggregateParameters, IndependentInstances }
public enum ModifierRewardKind { None, Points, BonusKills }
public enum ModifierRuleOutcome { Completed, Violated, NotTriggered }

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(RuleStatusResolution), "ruleStatus")]
[JsonDerivedType(typeof(BooleanResolution), "boolean")]
[JsonDerivedType(typeof(NonNegativeCountResolution), "nonNegativeCount")]
[JsonDerivedType(typeof(AutomaticRoundMetricResolution), "automaticRoundMetric")]
public abstract record ModifierResolution;
public sealed record RuleStatusResolution : ModifierResolution;
public sealed record BooleanResolution : ModifierResolution;
public sealed record NonNegativeCountResolution : ModifierResolution;
public sealed record AutomaticRoundMetricResolution(string Metric) : ModifierResolution;

public abstract record ModifierResolutionInput;
public sealed record RuleStatusInput(ModifierRuleOutcome Outcome) : ModifierResolutionInput;
public sealed record BooleanInput(bool Succeeded) : ModifierResolutionInput;
public sealed record NonNegativeCountInput(int Count) : ModifierResolutionInput;
public sealed record AutomaticRoundMetricInput : ModifierResolutionInput;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(GrowingKillValueParameters), "growingKillValue")]
[JsonDerivedType(typeof(BonusKillOnConditionParameters), "bonusKillOnCondition")]
[JsonDerivedType(typeof(BonusKillsByCountParameters), "bonusKillsByCount")]
[JsonDerivedType(typeof(WindowKillBonusPointsParameters), "windowKillBonusPoints")]
public abstract record ModifierFormulaParameters;
public sealed record GrowingKillValueParameters(
    int IncrementPointsPerKill,
    int ZeroKillPenaltyPoints
) : ModifierFormulaParameters;
public sealed record BonusKillOnConditionParameters(int SuccessBonusKills) : ModifierFormulaParameters;
public sealed record BonusKillsByCountParameters(int BonusKillsPerUnit) : ModifierFormulaParameters;
public sealed record WindowKillBonusPointsParameters(decimal BonusRate) : ModifierFormulaParameters;

public sealed record ModifierFormulaReference(
    string Code,
    int Version,
    ModifierFormulaParameters Parameters
);

public sealed record ModifierBehaviorV2(
    int SchemaVersion,
    ModifierBehaviorKind Kind,
    ModifierPhase Phase,
    ModifierPerformer Performer,
    bool RequiresHostMonitoring,
    string Rule,
    ModifierStackingPolicy StackingPolicy,
    ModifierResolution Resolution,
    ModifierRewardKind Reward,
    ModifierFormulaReference? FormulaReference,
    int? DurationSecondsPerActivation = null
);

public sealed record ModifierActivationSnapshotV2(
    Guid ActivationId,
    Guid ModifierId,
    int DefinitionRevision,
    string Name,
    ModifierBehaviorV2 Behavior
);

public static class ModifierBehaviorV2Json
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public static string Serialize(ModifierBehaviorV2 behavior)
    {
        ArgumentNullException.ThrowIfNull(behavior);
        var error = ModifierBehaviorValidator.Validate(behavior);
        if (error is not null)
        {
            throw new ArgumentException(error, nameof(behavior));
        }

        return JsonSerializer.Serialize(behavior, Options);
    }

    public static ModifierBehaviorV2 Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new JsonException("BehaviorV2 JSON is required.");
        }

        var behavior = JsonSerializer.Deserialize<ModifierBehaviorV2>(json, Options)
            ?? throw new JsonException("BehaviorV2 JSON is invalid.");
        var error = ModifierBehaviorValidator.Validate(behavior);
        return error is null ? behavior : throw new JsonException(error);
    }

    public static bool TryDeserialize(string? json, out ModifierBehaviorV2? behavior)
    {
        try
        {
            behavior = Deserialize(json ?? string.Empty);
            return true;
        }
        catch (JsonException)
        {
            behavior = null;
            return false;
        }
    }
}
