using backend.Application.Contracts;
using backend.Application.Features.GameRounds;
using backend.Data.Entities;
using backend.Domain.GameModifiers;
using backend.Domain.Persistence;
using System.Text.Json;

namespace backend.Infrastructure.Persistence;

public sealed partial class DbGameRoundRepository
{
    private static GameRoundScoreBreakdown CalculateRoundScore(GameRound round, string normalizedStatus)
    {
        return GameRoundScoreCalculator.Calculate(CreateScoreInput(
            normalizedStatus,
            round.BaseScore,
            round.KillsCount,
            round.BountyCount,
            round.ModifierResults.Select(x => new GameRoundScoreModifierInput(x.ScoreDelta, x.KillDelta))
        ));
    }

    private static GameRoundScoreBreakdown CalculateRoundScoreSnapshot(
        string status,
        int baseScore,
        int killsCount,
        int bountyCount,
        IReadOnlyList<GameRoundModifierSnapshot> modifiers
    )
    {
        return GameRoundScoreCalculator.Calculate(CreateScoreInput(
            status,
            baseScore,
            killsCount,
            bountyCount,
            modifiers.Select(x => new GameRoundScoreModifierInput(x.ScoreDelta, x.KillDelta))
        ));
    }

    private static GameRoundScoreInput CreateScoreInput(
        string status,
        int baseScore,
        int killsCount,
        int bountyCount,
        IEnumerable<GameRoundScoreModifierInput> modifiers
    )
    {
        return new GameRoundScoreInput(
            status,
            baseScore,
            killsCount,
            bountyCount,
            modifiers.ToArray()
        );
    }

    private static GameRoundScoreDetails ToScoreDetails(GameRoundScoreBreakdown breakdown)
    {
        return new GameRoundScoreDetails(
            breakdown.ScoreUnit,
            breakdown.KillsScore,
            breakdown.BountyScore,
            breakdown.ModifierKillDelta,
            breakdown.ModifierKillScore,
            breakdown.ModifierScoreDelta,
            breakdown.EmptyCardPenaltyApplied,
            breakdown.EmptyCardPenaltyScore,
            breakdown.PenaltyTotal,
            breakdown.BonusDelta,
            breakdown.TotalKillCount,
            breakdown.FinalScore
        );
    }

    private static bool TryCreateModifierInputsById(
        IReadOnlyList<FinalizeGameRoundModifierInput> modifierInputs,
        out IReadOnlyDictionary<Guid, FinalizeGameRoundModifierInput> modifierInputsById,
        out string? errorCode
    )
    {
        var dictionary = new Dictionary<Guid, FinalizeGameRoundModifierInput>();
        foreach (var input in modifierInputs)
        {
            if (!dictionary.TryAdd(input.ModifierResultId, input))
            {
                modifierInputsById = dictionary;
                errorCode = "modifier_resolution.duplicate_result";
                return false;
            }
        }

        modifierInputsById = dictionary;
        errorCode = null;
        return true;
    }

    private static void ApplyModifierScoring(
        GameRound round,
        IReadOnlyDictionary<Guid, FinalizeGameRoundModifierInput> modifierInputsById,
        IReadOnlyList<FinalizeGameRoundRuleGroupInput> ruleGroupInputs,
        Guid resolvedByUserId,
        DateTime resolvedAtUtc
    )
    {
        var v2Results = new List<(GameRoundModifierResult Result, ModifierBehaviorV2 Behavior)>();
        foreach (var result in round.ModifierResults)
        {
            if (string.IsNullOrWhiteSpace(result.ModifierBehaviorV2SnapshotJson))
            {
                throw new ModifierScoringException("behavior.invalid", true);
            }

            try
            {
                v2Results.Add(
                    (
                        result,
                        ModifierBehaviorV2Json.Deserialize(
                            result.ModifierBehaviorV2SnapshotJson
                        )
                    )
                );
            }
            catch (JsonException)
            {
                throw new ModifierScoringException("behavior.invalid", true);
            }
        }
        ApplyBehaviorV2Scoring(
            round,
            v2Results,
            modifierInputsById,
            ruleGroupInputs,
            resolvedByUserId,
            resolvedAtUtc
        );
    }

    private static void ApplyBehaviorV2Scoring(
        GameRound round,
        IReadOnlyList<(GameRoundModifierResult Result, ModifierBehaviorV2 Behavior)> results,
        IReadOnlyDictionary<Guid, FinalizeGameRoundModifierInput> modifierInputsById,
        IReadOnlyList<FinalizeGameRoundRuleGroupInput> ruleGroupInputs,
        Guid resolvedByUserId,
        DateTime resolvedAtUtc
    )
    {
        var calculations = new List<ModifierInstanceCalculationInput>(results.Count);
        var inputByActivationId = new Dictionary<Guid, ModifierResolutionInput>();
        var violationCommentByActivationId = new Dictionary<Guid, string>();
        var ruleGroups = results
            .Where(item => item.Behavior.Kind == ModifierBehaviorKind.Rule)
            .GroupBy(item => item.Result.ResolutionGroupId ?? item.Result.ModifierId)
            .ToArray();
        var ruleGroupInputsById = new Dictionary<Guid, FinalizeGameRoundRuleGroupInput>();
        foreach (var groupInput in ruleGroupInputs)
        {
            if (!ruleGroupInputsById.TryAdd(groupInput.ResolutionGroupId, groupInput))
            {
                throw new ModifierScoringException("modifier_resolution.duplicate_group");
            }
        }
        if (ruleGroupInputsById.Count != ruleGroups.Length)
        {
            throw new ModifierScoringException("modifier_resolution.group_set_mismatch");
        }

        foreach (var group in ruleGroups)
        {
            if (!ruleGroupInputsById.TryGetValue(group.Key, out var provided)
                || !TryMapRuleOutcome(provided.OutcomeStatus, out var ruleOutcome))
            {
                throw new ModifierScoringException("modifier_resolution.group_missing");
            }

            var expectedMemberIds = group.Select(item => item.Result.Id).Order().ToArray();
            var providedMemberIds = provided.MemberResultIds.Distinct().Order().ToArray();
            if (provided.MemberResultIds.Count != providedMemberIds.Length
                || !expectedMemberIds.SequenceEqual(providedMemberIds)
                || expectedMemberIds.Any(modifierInputsById.ContainsKey))
            {
                throw new ModifierScoringException("modifier_resolution.group_members_mismatch");
            }

            var comment = string.IsNullOrWhiteSpace(provided.ViolationComment)
                ? null
                : provided.ViolationComment!.Trim();
            if (ruleOutcome == ModifierRuleOutcome.Violated
                && comment is not { Length: >= 1 and <= 1000 })
            {
                throw new ModifierScoringException("modifier_resolution.violation_comment_required");
            }

            foreach (var item in group)
            {
                inputByActivationId[item.Result.GameModifierActivationId] =
                    new RuleStatusInput(ruleOutcome);
                if (ruleOutcome == ModifierRuleOutcome.Violated)
                {
                    violationCommentByActivationId[item.Result.GameModifierActivationId] = comment!;
                }
            }
        }

        foreach (var item in results.Where(item => item.Behavior.Kind == ModifierBehaviorKind.Scoring))
        {
            var hasInput = modifierInputsById.TryGetValue(item.Result.Id, out var input);
            ModifierResolutionInput resolutionInput;
            switch (item.Behavior.Resolution)
            {
                case AutomaticRoundMetricResolution:
                    if (hasInput)
                    {
                        throw new ModifierScoringException("modifier_resolution.automatic_input_forbidden");
                    }
                    resolutionInput = new AutomaticRoundMetricInput();
                    break;
                case BooleanResolution:
                    if (!hasInput || !input!.IsConditionMet.HasValue)
                    {
                        throw new ModifierScoringException("modifier_resolution.boolean_required");
                    }
                    resolutionInput = new BooleanInput(input.IsConditionMet.Value);
                    break;
                case NonNegativeCountResolution:
                    if (!hasInput || input!.CountValue is null or < 0)
                    {
                        throw new ModifierScoringException("modifier_resolution.non_negative_count_required");
                    }
                    resolutionInput = new NonNegativeCountInput(input.CountValue.Value);
                    break;
                default:
                    throw new ModifierScoringException("modifier_resolution.unsupported");
            }

            inputByActivationId[item.Result.GameModifierActivationId] = resolutionInput;
        }

        foreach (var item in results)
        {
            if (!inputByActivationId.TryGetValue(
                    item.Result.GameModifierActivationId,
                    out var resolutionInput
                ))
            {
                throw new ModifierScoringException("modifier_resolution.missing");
            }

            calculations.Add(
                new ModifierInstanceCalculationInput(
                    new ModifierActivationSnapshotV2(
                        item.Result.GameModifierActivationId,
                        item.Result.ModifierId,
                        item.Result.DefinitionRevisionSnapshot,
                        item.Result.ModifierNameSnapshot,
                        item.Behavior
                    ),
                    resolutionInput
                )
            );
        }

        var calculation = ModifierDomainEngine.Calculate(
            new ModifierRoundFacts(round.BaseScore, round.KillsCount, round.BountyCount),
            calculations
        );
        if (!calculation.IsSuccess)
        {
            throw new ModifierScoringException(
                calculation.Errors.FirstOrDefault()?.Code ?? "modifier_calculation.failed",
                IsConfigurationError(
                    calculation.Errors.FirstOrDefault()?.Code ?? "modifier_calculation.failed"
                )
            );
        }

        foreach (var item in results)
        {
            var outcome = calculation.Calculation!.Instances.Single(
                value => value.ActivationId == item.Result.GameModifierActivationId
            );
            var input = inputByActivationId[item.Result.GameModifierActivationId];
            item.Result.OutcomeStatus = ToPersistenceOutcome(input, outcome);
            item.Result.ScoreDelta = outcome.PointsDelta;
            item.Result.KillDelta = outcome.BonusKillsDelta;
            item.Result.MultiplierApplied = null;
            item.Result.ResolutionDataJson = SerializeBehaviorV2Resolution(input);
            item.Result.ViolationComment = violationCommentByActivationId.GetValueOrDefault(
                item.Result.GameModifierActivationId
            );
            item.Result.CalculationBreakdownJson = JsonSerializer.Serialize(
                new
                {
                    SchemaVersion = ModifierBehaviorSchemaVersions.V2,
                    FormulaCode = item.Behavior.FormulaReference?.Code,
                    FormulaVersion = item.Behavior.FormulaReference?.Version,
                    outcome.PointsDelta,
                    outcome.BonusKillsDelta,
                    outcome.RuleOutcome,
                    outcome.CountInput,
                    outcome.BooleanInput
                },
                JsonOptions
            );
            item.Result.ResolvedByUserId = resolvedByUserId;
            item.Result.ResolvedAtUtc = resolvedAtUtc;
            item.Result.UpdatedAtUtc = resolvedAtUtc;
        }

    }

    private static bool TryMapRuleOutcome(string value, out ModifierRuleOutcome outcome)
    {
        outcome = value.Trim().ToLowerInvariant() switch
        {
            "completed" => ModifierRuleOutcome.Completed,
            "violated" or "failed" => ModifierRuleOutcome.Violated,
            "nottriggered" or "not_triggered" or "cancelled" =>
                ModifierRuleOutcome.NotTriggered,
            _ => (ModifierRuleOutcome)(-1)
        };
        return Enum.IsDefined(outcome);
    }

    private static string ToPersistenceOutcome(
        ModifierResolutionInput input,
        ModifierInstanceOutcome outcome
    ) => input switch
    {
        RuleStatusInput { Outcome: ModifierRuleOutcome.Completed } =>
            GameRoundModifierOutcomeValue.Completed,
        RuleStatusInput { Outcome: ModifierRuleOutcome.Violated } =>
            GameRoundModifierOutcomeValue.Violated,
        RuleStatusInput => GameRoundModifierOutcomeValue.NotTriggered,
        BooleanInput { Succeeded: true } => GameRoundModifierOutcomeValue.Succeeded,
        BooleanInput => GameRoundModifierOutcomeValue.NotSucceeded,
        NonNegativeCountInput or AutomaticRoundMetricInput =>
            GameRoundModifierOutcomeValue.Calculated,
        _ => throw new ArgumentOutOfRangeException(nameof(input))
    };

    private static string SerializeBehaviorV2Resolution(ModifierResolutionInput input)
    {
        object payload = input switch
        {
            RuleStatusInput value => new
            {
                Type = "ruleStatus",
                Outcome = value.Outcome.ToString()
            },
            BooleanInput value => new { Type = "boolean", value.Succeeded },
            NonNegativeCountInput value => new { Type = "nonNegativeCount", value.Count },
            AutomaticRoundMetricInput => new { Type = "automaticRoundMetric" },
            _ => throw new ArgumentOutOfRangeException(nameof(input))
        };
        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    private static bool TryApplyModifierScoring(
        GameRound round,
        IReadOnlyDictionary<Guid, FinalizeGameRoundModifierInput> modifierInputsById,
        IReadOnlyList<FinalizeGameRoundRuleGroupInput> ruleGroupInputs,
        Guid resolvedByUserId,
        DateTime resolvedAtUtc,
        out string? errorCode,
        out bool isConfigurationError
    )
    {
        try
        {
            ApplyModifierScoring(
                round,
                modifierInputsById,
                ruleGroupInputs,
                resolvedByUserId,
                resolvedAtUtc
            );
            errorCode = null;
            isConfigurationError = false;
            return true;
        }
        catch (ModifierScoringException exception)
        {
            errorCode = exception.Code;
            isConfigurationError = exception.IsConfigurationError;
            return false;
        }
        catch (InvalidOperationException)
        {
            errorCode = "modifier_calculation.failed";
            isConfigurationError = true;
            return false;
        }
    }

    private static bool IsConfigurationError(string code) => code is
        "behavior.invalid"
        or "behavior.rule_incompatible"
        or "formula.unsupported"
        or "formula.incompatible"
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
            _ => throw new InvalidOperationException("Unsupported modifier resolution type.")
        };
    }

    private static string ResolveBehaviorSnapshotJson(
        Data.Entities.GameModifierActivation activation
    )
        => ModifierBehaviorV2Json.Serialize(
            ModifierBehaviorV2Json.Deserialize(activation.BehaviorV2SnapshotJson)
        );

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private sealed class ModifierScoringException(
        string code,
        bool isConfigurationError = false
    ) : Exception(code)
    {
        public string Code { get; } = code;
        public bool IsConfigurationError { get; } = isConfigurationError;
    }

}
