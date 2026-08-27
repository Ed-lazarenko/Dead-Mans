using backend.Domain.Persistence;
using backend.Application.Features.Scoring;
using backend.Application.Contracts;
using backend.Domain.GameModifiers;
using System.Text.Json;

namespace backend.Application.Features.GameRounds;

public static class GameRoundScoreCalculator
{
    public static GameRoundScoreBreakdown Calculate(GameRoundScoreInput input)
    {
        var normalizedStatus = input.Status.Trim().ToLowerInvariant();
        if (normalizedStatus != GameRoundStatusValue.Completed)
        {
            return EmptyBreakdown(input.BaseScore);
        }

        var rawModifierKillDelta = input.Modifiers.Sum(x => (decimal)x.KillDelta);
        var rawModifierScoreDelta = input.Modifiers.Sum(x => (decimal)x.ScoreDelta);
        var rawTotalKillCount = input.KillsCount + rawModifierKillDelta;
        var rawKillsScore = (decimal)input.KillsCount * input.BaseScore;
        var rawBountyScore = (decimal)input.BountyCount * input.BaseScore;
        var rawModifierKillScore = rawModifierKillDelta * input.BaseScore;
        var rawCardOutcomeScore = rawKillsScore + rawBountyScore + rawModifierKillScore;
        var emptyCardPenaltyApplied =
            input.BaseScore > 0 && rawCardOutcomeScore == 0 && rawModifierScoreDelta <= 0;
        var rawEmptyCardPenaltyScore = emptyCardPenaltyApplied ? -1m * input.BaseScore : 0m;

        var rawFinalScore = rawCardOutcomeScore + rawModifierScoreDelta + rawEmptyCardPenaltyScore;
        var rawBonusDelta = emptyCardPenaltyApplied ? rawFinalScore : rawFinalScore - input.BaseScore;

        var scalarBreakdown = new GameRoundScoreBreakdown(
            input.BaseScore,
            SaturatingInt32.From(rawKillsScore),
            SaturatingInt32.From(rawBountyScore),
            SaturatingInt32.From(rawModifierKillDelta),
            SaturatingInt32.From(rawModifierKillScore),
            SaturatingInt32.From(rawModifierScoreDelta),
            emptyCardPenaltyApplied,
            SaturatingInt32.From(rawEmptyCardPenaltyScore),
            rawFinalScore < 0 ? SaturatingInt32.From(decimal.Abs(rawFinalScore)) : 0,
            SaturatingInt32.From(rawBonusDelta),
            SaturatingInt32.From(rawTotalKillCount),
            SaturatingInt32.From(rawFinalScore),
            []
        );
        return scalarBreakdown with { CalculationLines = BuildCalculationLines(input, scalarBreakdown) };
    }

    private static GameRoundScoreBreakdown EmptyBreakdown(int scoreUnit) =>
        new(scoreUnit, 0, 0, 0, 0, 0, false, 0, 0, 0, 0, 0, []);

    private static IReadOnlyList<GameRoundScoreCalculationLine> BuildCalculationLines(
        GameRoundScoreInput input,
        GameRoundScoreBreakdown breakdown
    )
    {
        var lines = new List<GameRoundScoreCalculationLine>();
        decimal running = 0;
        Add("kills", null, null, 0, breakdown.KillsScore, null, null,
            [new("killsCount", input.KillsCount), new("cardValue", input.BaseScore)]);
        Add("bounties", null, null, 0, breakdown.BountyScore, null, null,
            [new("bountyCount", input.BountyCount), new("cardValue", input.BaseScore)]);

        foreach (var group in input.Modifiers
            .Where(x => x.Behavior?.Kind == ModifierBehaviorKind.Scoring || x.ScoreDelta != 0 || x.KillDelta != 0)
            .GroupBy(x => new
            {
                x.ModifierId,
                x.ModifierName,
                x.DefinitionRevision,
                FormulaCode = x.Behavior?.FormulaReference?.Code,
                FormulaVersion = x.Behavior?.FormulaReference?.Version
            }))
        {
            var items = group.ToArray();
            var activationCount = items.Length;
            var bonusKills = SaturatingInt32.From(items.Sum(x => (decimal)x.KillDelta));
            var pointsDelta = SaturatingInt32.From(items.Sum(x => (decimal)x.ScoreDelta));
            var formula = items.Select(x => x.Behavior?.FormulaReference).FirstOrDefault(x => x is not null);

            if (bonusKills != 0 || items.Any(x => x.Behavior?.Reward == ModifierRewardKind.BonusKills))
            {
                var operands = new List<GameRoundScoreCalculationOperand>
                {
                    new("activationCount", activationCount),
                    new("bonusKills", bonusKills),
                    new("cardValue", input.BaseScore)
                };
                AddResolutionOperands(operands, items, input);
                AddFormulaParameters(operands, formula?.Parameters);
                Add("modifierBonusKills", group.Key.ModifierId?.ToString(), group.Key.ModifierName,
                    activationCount, SaturatingInt32.From((decimal)bonusKills * input.BaseScore),
                    formula?.Code, formula?.Version, operands);
            }

            if (pointsDelta != 0 || items.Any(x => x.Behavior?.Reward == ModifierRewardKind.Points))
            {
                var operands = new List<GameRoundScoreCalculationOperand>
                {
                    new("activationCount", activationCount),
                    new("pointsDelta", pointsDelta),
                    new("cardValue", input.BaseScore),
                    new("killsCount", input.KillsCount)
                };
                AddResolutionOperands(operands, items, input);
                AddFormulaParameters(operands, formula?.Parameters);
                if (formula?.Parameters is KillValueIncreasePerUnitParameters genericIncrease)
                {
                    var sourceUnits = Operand(operands, "sourceUnits");
                    var zeroSourceActivations = Operand(operands, "zeroSourceActivations");
                    operands.Add(new(
                        "killValueIncreasePoints",
                        sourceUnits * genericIncrease.IncrementPointsPerUnit * input.KillsCount
                    ));
                    operands.Add(new(
                        "zeroSourcePenaltyPoints",
                        zeroSourceActivations * genericIncrease.ZeroCountPenaltyPoints
                    ));
                }
                if (formula?.Parameters is GrowingKillValueParameters growing)
                {
                    var bonusPerKill = (decimal)growing.IncrementPointsPerKill * input.KillsCount * activationCount;
                    operands.Add(new("bonusPerKill", bonusPerKill));
                    operands.Add(new("adjustedKillValue", input.BaseScore + bonusPerKill));
                    operands.Add(new("baseKillsScore", (decimal)input.BaseScore * input.KillsCount));
                    operands.Add(new("adjustedKillsScore", (input.BaseScore + bonusPerKill) * input.KillsCount));
                }
                if (formula?.Parameters is KillValueIncreasePerUnitParameters increase
                    && items.All(x => x.Behavior?.Resolution is AutomaticRoundMetricResolution { Metric: "killsCount" }))
                {
                    var bonusPerKill = (decimal)increase.IncrementPointsPerUnit * input.KillsCount * activationCount;
                    operands.Add(new("bonusPerKill", bonusPerKill));
                    operands.Add(new("adjustedKillValue", input.BaseScore + bonusPerKill));
                    operands.Add(new("baseKillsScore", (decimal)input.BaseScore * input.KillsCount));
                    operands.Add(new("adjustedKillsScore", (input.BaseScore + bonusPerKill) * input.KillsCount));
                }
                Add("modifierPoints", group.Key.ModifierId?.ToString(), group.Key.ModifierName,
                    activationCount, pointsDelta, formula?.Code, formula?.Version, operands);
            }
        }

        if (breakdown.EmptyCardPenaltyApplied)
        {
            Add("emptyCardPenalty", null, null, 0, breakdown.EmptyCardPenaltyScore, null, null,
                [new("cardValue", input.BaseScore), new("outcomeUnits", 0)]);
        }
        return lines;

        void Add(string kind, string? modifierId, string? modifierName, int activationCount,
            int delta, string? formulaCode, int? formulaVersion,
            IReadOnlyList<GameRoundScoreCalculationOperand> operands)
        {
            running += delta;
            lines.Add(new(kind, modifierId, modifierName, activationCount, delta,
                SaturatingInt32.From(running), formulaCode, formulaVersion, operands));
        }
    }

    private static void AddFormulaParameters(
        ICollection<GameRoundScoreCalculationOperand> target,
        ModifierFormulaParameters? parameters
    )
    {
        switch (parameters)
        {
            case GrowingKillValueParameters value:
                target.Add(new("incrementPointsPerKill", value.IncrementPointsPerKill));
                target.Add(new("zeroKillPenaltyPoints", value.ZeroKillPenaltyPoints));
                break;
            case BonusKillOnConditionParameters value:
                target.Add(new("successBonusKills", value.SuccessBonusKills));
                break;
            case BonusKillsByCountParameters value:
                target.Add(new("bonusKillsPerUnit", value.BonusKillsPerUnit));
                break;
            case WindowKillBonusPointsParameters value:
                target.Add(new("bonusRate", value.BonusRate));
                break;
            case FixedPointsPerUnitParameters value:
                target.Add(new("pointsPerUnit", value.PointsPerUnit));
                break;
            case CardPercentPerUnitParameters value:
                target.Add(new("rate", value.Rate));
                break;
            case BonusKillsPerUnitParameters value:
                target.Add(new("bonusKillsPerUnit", value.BonusKillsPerUnit));
                break;
            case KillValueIncreasePerUnitParameters value:
                target.Add(new("incrementPointsPerUnit", value.IncrementPointsPerUnit));
                target.Add(new("zeroCountPenaltyPoints", value.ZeroCountPenaltyPoints));
                break;
        }
    }

    private static void AddResolutionOperands(
        ICollection<GameRoundScoreCalculationOperand> target,
        IReadOnlyList<GameRoundScoreModifierInput> items,
        GameRoundScoreInput round
    )
    {
        decimal sourceUnits = 0;
        decimal count = 0;
        decimal successful = 0;
        decimal zeroSourceActivations = 0;
        var hasCountResolution = false;
        var hasBooleanResolution = false;
        foreach (var item in items)
        {
            var quantity = ResolveSourceQuantity(item, round, out var isCount, out var isBoolean);
            sourceUnits += quantity;
            if (quantity == 0) zeroSourceActivations++;
            if (isCount)
            {
                hasCountResolution = true;
                count += quantity;
            }
            if (isBoolean)
            {
                hasBooleanResolution = true;
                successful += quantity;
            }
        }
        target.Add(new("sourceUnits", sourceUnits));
        target.Add(new("zeroSourceActivations", zeroSourceActivations));
        if (hasCountResolution) target.Add(new("inputCount", count));
        if (hasBooleanResolution) target.Add(new("successfulActivations", successful));
    }

    private static decimal ResolveSourceQuantity(
        GameRoundScoreModifierInput item,
        GameRoundScoreInput round,
        out bool isCount,
        out bool isBoolean
    )
    {
        isCount = item.Behavior?.Resolution is NonNegativeCountResolution;
        isBoolean = item.Behavior?.Resolution is BooleanResolution;
        if (item.Behavior?.Resolution is AutomaticRoundMetricResolution automatic)
        {
            return automatic.Metric switch
            {
                "killsCount" => round.KillsCount,
                "bountyCount" => round.BountyCount,
                _ => 0
            };
        }
        if (item.Behavior?.Resolution is PerActivationResolution) return 1;
        if (string.IsNullOrWhiteSpace(item.ResolutionDataJson)) return 0;

        try
        {
            using var document = JsonDocument.Parse(item.ResolutionDataJson);
            var root = document.RootElement;
            if (isCount)
            {
                foreach (var propertyName in new[] { "count", "countValue", "mentorKills", "killsDuringWindow" })
                {
                    if (root.TryGetProperty(propertyName, out var node)
                        && node.TryGetInt32(out var value))
                    {
                        return value;
                    }
                }
            }
            if (isBoolean)
            {
                foreach (var propertyName in new[] { "succeeded", "conditionMet" })
                {
                    if (root.TryGetProperty(propertyName, out var node)
                        && node.ValueKind is JsonValueKind.True or JsonValueKind.False)
                    {
                        return node.GetBoolean() ? 1 : 0;
                    }
                }
            }
        }
        catch (JsonException)
        {
            // Historical score rows remain readable even if an old resolution payload is malformed.
        }
        return 0;
    }

    private static decimal Operand(
        IEnumerable<GameRoundScoreCalculationOperand> operands,
        string code
    ) => operands.FirstOrDefault(x => x.Code == code)?.Value ?? 0;

}

public sealed record GameRoundScoreInput(
    string Status,
    int BaseScore,
    int KillsCount,
    int BountyCount,
    IReadOnlyList<GameRoundScoreModifierInput> Modifiers
);

public sealed record GameRoundScoreModifierInput(
    int ScoreDelta,
    int KillDelta,
    Guid? ModifierId = null,
    string? ModifierName = null,
    int DefinitionRevision = 0,
    ModifierBehaviorV2? Behavior = null,
    string? ResolutionDataJson = null
);

public sealed record GameRoundScoreBreakdown(
    int ScoreUnit,
    int KillsScore,
    int BountyScore,
    int ModifierKillDelta,
    int ModifierKillScore,
    int ModifierScoreDelta,
    bool EmptyCardPenaltyApplied,
    int EmptyCardPenaltyScore,
    int PenaltyTotal,
    int BonusDelta,
    int TotalKillCount,
    int FinalScore,
    IReadOnlyList<GameRoundScoreCalculationLine> CalculationLines
);
