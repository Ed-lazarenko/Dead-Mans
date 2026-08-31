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

public static class ModifierCountMaximumKinds
{
    public const string None = "none";
    public const string ResolvedKills = "resolvedKills";
    public const string Activations = "activations";
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(RuleStatusResolution), "ruleStatus")]
[JsonDerivedType(typeof(BooleanResolution), "boolean")]
[JsonDerivedType(typeof(NonNegativeCountResolution), "nonNegativeCount")]
[JsonDerivedType(typeof(AutomaticRoundMetricResolution), "automaticRoundMetric")]
[JsonDerivedType(typeof(PerActivationResolution), "perActivation")]
public abstract record ModifierResolution;
public sealed record RuleStatusResolution : ModifierResolution;
public sealed record BooleanResolution(string? InputLabel = null) : ModifierResolution;
public sealed record NonNegativeCountResolution(
    string? InputLabel = null,
    string? MaximumKind = null,
    int? MaximumPerActivation = null
) : ModifierResolution;
public sealed record AutomaticRoundMetricResolution(string Metric) : ModifierResolution;
public sealed record PerActivationResolution : ModifierResolution;

public abstract record ModifierResolutionInput;
public sealed record RuleStatusInput(ModifierRuleOutcome Outcome) : ModifierResolutionInput;
public sealed record BooleanInput(bool Succeeded) : ModifierResolutionInput;
public sealed record NonNegativeCountInput(int Count) : ModifierResolutionInput;
public sealed record AutomaticRoundMetricInput : ModifierResolutionInput;
public sealed record PerActivationInput : ModifierResolutionInput;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(GrowingKillValueParameters), "growingKillValue")]
[JsonDerivedType(typeof(BonusKillOnConditionParameters), "bonusKillOnCondition")]
[JsonDerivedType(typeof(BonusKillsByCountParameters), "bonusKillsByCount")]
[JsonDerivedType(typeof(WindowKillBonusPointsParameters), "windowKillBonusPoints")]
[JsonDerivedType(typeof(FixedPointsPerUnitParameters), "fixedPointsPerUnit")]
[JsonDerivedType(typeof(CardPercentPerUnitParameters), "cardPercentPerUnit")]
[JsonDerivedType(typeof(BonusKillsPerUnitParameters), "bonusKillsPerUnit")]
[JsonDerivedType(typeof(KillValueIncreasePerUnitParameters), "killValueIncreasePerUnit")]
public abstract record ModifierFormulaParameters;
public sealed record GrowingKillValueParameters(
    int IncrementPointsPerKill,
    int ZeroKillPenaltyPoints
) : ModifierFormulaParameters;
public sealed record BonusKillOnConditionParameters(int SuccessBonusKills) : ModifierFormulaParameters;
public sealed record BonusKillsByCountParameters(int BonusKillsPerUnit) : ModifierFormulaParameters;
public sealed record WindowKillBonusPointsParameters(decimal BonusRate) : ModifierFormulaParameters;
public sealed record FixedPointsPerUnitParameters(int PointsPerUnit) : ModifierFormulaParameters;
public sealed record CardPercentPerUnitParameters(decimal Rate) : ModifierFormulaParameters;
public sealed record BonusKillsPerUnitParameters(int BonusKillsPerUnit) : ModifierFormulaParameters;
public sealed record KillValueIncreasePerUnitParameters(
    int IncrementPointsPerUnit,
    int ZeroCountPenaltyPoints
) : ModifierFormulaParameters;

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

        var normalizedJson = NormalizeMetadataPropertyOrder(json);
        var behavior = JsonSerializer.Deserialize<ModifierBehaviorV2>(normalizedJson, Options)
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

    private static string NormalizeMetadataPropertyOrder(string json)
    {
        using var document = JsonDocument.Parse(json);
        using var payload = new MemoryStream();
        using (var writer = new Utf8JsonWriter(payload))
        {
            WriteWithMetadataFirst(writer, document.RootElement);
        }

        return System.Text.Encoding.UTF8.GetString(payload.ToArray());
    }

    private static void WriteWithMetadataFirst(Utf8JsonWriter writer, JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                JsonProperty? discriminator = null;
                foreach (var property in element.EnumerateObject())
                {
                    if (!property.NameEquals("type"))
                    {
                        continue;
                    }

                    if (discriminator is not null)
                    {
                        throw new JsonException("Duplicate 'type' discriminator is not allowed.");
                    }

                    discriminator = property;
                }

                if (discriminator is { } discriminatorProperty)
                {
                    writer.WritePropertyName(discriminatorProperty.Name);
                    WriteWithMetadataFirst(writer, discriminatorProperty.Value);
                }

                foreach (var property in element.EnumerateObject())
                {
                    if (property.NameEquals("type"))
                    {
                        continue;
                    }

                    writer.WritePropertyName(property.Name);
                    WriteWithMetadataFirst(writer, property.Value);
                }
                writer.WriteEndObject();
                return;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                {
                    WriteWithMetadataFirst(writer, item);
                }
                writer.WriteEndArray();
                return;
            default:
                element.WriteTo(writer);
                return;
        }
    }
}
