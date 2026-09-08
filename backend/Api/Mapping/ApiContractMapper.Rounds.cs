using backend.Api.Contracts;
using backend.Application.Abstractions.Auth;
using backend.Application.Contracts;
using backend.Domain.GameModifiers;

namespace backend.Api.Mapping;

public static partial class ApiContractMapper
{
    public static StartGameRoundInput ToInput(
        this StartGameRoundRequestDto request,
        Guid cellId,
        Guid teamId
    )
    {
        return new StartGameRoundInput(cellId, teamId);
    }

    public static FinalizeGameRoundInput ToInput(
        this FinalizeGameRoundRequestDto request,
        IReadOnlyList<FinalizeGameRoundModifierInput> modifierResults,
        IReadOnlyList<FinalizeGameRoundRuleGroupInput> ruleGroups
    )
    {
        return new FinalizeGameRoundInput(
            request.Status.Trim(),
            request.KillsCount,
            request.BountyCount,
            string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            modifierResults,
            ruleGroups,
            request.ExpectedRoundVersion
        );
    }

    public static FinalizeGameRoundRuleGroupInput ToInput(
        this FinalizeGameRoundRuleGroupRequestDto request,
        Guid resolutionGroupId,
        IReadOnlyList<Guid> memberResultIds
    ) => new(
        resolutionGroupId,
        memberResultIds,
        request.OutcomeStatus.Trim(),
        string.IsNullOrWhiteSpace(request.ViolationComment)
            ? null
            : request.ViolationComment.Trim()
    );

    public static FinalizeGameRoundModifierInput ToInput(
        this FinalizeGameRoundModifierRequestDto request,
        Guid modifierResultId
    )
    {
        return new FinalizeGameRoundModifierInput(
            modifierResultId,
            request.CountValue,
            request.IsConditionMet
        );
    }

    public static GameRoundDetailsDto ToDto(this GameRoundDetails item)
    {
        return new GameRoundDetailsDto(
            item.RoundId.ToString(),
            item.GameId.ToString(),
            item.CellId.ToString(),
            item.CellTitle,
            item.CellDescription,
            item.TeamId.ToString(),
            item.TeamName,
            item.TeamSlotIndex,
            item.Status,
            item.RoundVersion,
            item.StartedAtUtc,
            item.PreparedAtUtc,
            item.GameplayStartedAtUtc,
            item.ReviewedAtUtc,
            item.FinishedAtUtc,
            item.BaseScore,
            item.FinalScore,
            item.EmptyCardPenaltyApplied,
            item.ScoreDetails.ToDto(),
            item.KillsCount,
            item.BountyCount,
            item.Notes,
            item.TechnicalCancellationReasonCode,
            item.PublicCancellationSummary,
            item.ServerNowUtc,
            item.Participants.Select(ToDto).ToArray(),
            item.ModifierResults.Select(ToDto).ToArray()
        );
    }

    public static GameRoundParticipantDto ToDto(this GameRoundParticipantSnapshot item)
    {
        return new GameRoundParticipantDto(item.UserId.ToString(), item.DisplayName);
    }

    public static GameRoundTeamOptionDto ToDto(this GameRoundTeamOption item)
    {
        return new GameRoundTeamOptionDto(
            item.TeamId.ToString(),
            item.TeamName,
            item.TeamSlotIndex,
            item.Participants.Select(ToDto).ToArray()
        );
    }

    public static GameRoundModifierResultDto ToDto(this GameRoundModifierSnapshot item)
    {
        return new GameRoundModifierResultDto(
            item.ModifierResultId.ToString(),
            item.ModifierId.ToString(),
            item.ModifierName,
            item.ModifierCategory,
            item.ModifierDescription,
            item.OutcomeStatus,
            item.ScoreDelta,
            item.KillDelta,
            item.MultiplierApplied,
            item.ResolutionDataJson,
            item.ResolvedByUserId?.ToString(),
            item.ResolvedAtUtc,
            item.GameModifierActivationId.ToString(),
            item.DefinitionRevision,
            item.ResolutionGroupId?.ToString(),
            item.ResolutionKind,
            item.ViolationComment,
            item.RuntimeBehavior is null
                ? null
                : new GameRoundModifierRuntimeBehaviorDto(
                    item.RuntimeBehavior.Phase switch
                    {
                        ModifierPhase.Preparation => "preparation",
                        ModifierPhase.Round => "round",
                        ModifierPhase.Result => "result",
                        _ => throw new ArgumentOutOfRangeException(
                            nameof(item),
                            item.RuntimeBehavior.Phase,
                            "Unsupported modifier phase."
                        )
                    },
                    item.RuntimeBehavior.Performer == ModifierPerformer.ActiveTeam
                        ? "activeTeam"
                        : "mentor",
                    item.RuntimeBehavior.RequiresHostMonitoring,
                    item.RuntimeBehavior.Rule,
                    item.RuntimeBehavior.StackingPolicy
                        == ModifierStackingPolicy.AggregateParameters
                        ? "aggregateParameters"
                        : "independentInstances",
                    item.RuntimeBehavior.DurationSecondsPerActivation,
                    item.RuntimeBehavior.Resolution switch
                    {
                        BooleanResolution value => value.InputLabel,
                        NonNegativeCountResolution value => value.InputLabel,
                        _ => null
                    },
                    (item.RuntimeBehavior.Resolution as NonNegativeCountResolution)?.MaximumKind,
                    (item.RuntimeBehavior.Resolution as NonNegativeCountResolution)?.MaximumPerActivation,
                    item.RuntimeBehavior.FormulaReference?.Code
                )
        );
    }

    public static GameHistoryTeamLeaderboardEntryDto ToDto(
        this GameHistoryTeamLeaderboardEntry item
    )
    {
        return new GameHistoryTeamLeaderboardEntryDto(
            item.TeamId.ToString(),
            item.TeamName,
            item.TeamSlotIndex,
            item.RoundsPlayed,
            item.BestScore,
            item.PenaltyTotal,
            item.FinalScore,
            item.BestRound.ToDto(),
            item.LatestRound.ToDto(),
            item.Rounds.Select(ToDto).ToArray(),
            item.TotalScore,
            item.AverageScore,
            item.TotalBonusDelta,
            item.TotalKills,
            item.TotalBounties,
            item.ParticipantNames.ToArray(),
            item.LastFinishedAtUtc
        );
    }

    public static GameRoundScorePreviewDto ToDto(this PreviewGameRoundScoreResult item)
    {
        return new GameRoundScorePreviewDto(
            item.ScoreDetails!.ToDto(),
            item.ModifierResults.Select(ToDto).ToArray(),
            item.RoundVersion!.Value,
            item.NormalizedInputHash!,
            (item.CalculationTrace ?? [])
                .Select(
                    value => new GameRoundModifierCalculationTraceDto(
                        value.ModifierResultId.ToString(),
                        value.ActivationId.ToString(),
                        value.FormulaCode,
                        value.FormulaVersion,
                        value.ResolutionKind,
                        value.PointsDelta,
                        value.BonusKillsDelta
                    )
                )
                .ToArray()
        );
    }

    public static GameRoundScoreDetailsDto ToDto(this GameRoundScoreDetails item)
    {
        return new GameRoundScoreDetailsDto(
            item.ScoreUnit,
            item.KillsScore,
            item.BountyScore,
            item.ModifierKillDelta,
            item.ModifierKillScore,
            item.ModifierScoreDelta,
            item.EmptyCardPenaltyApplied,
            item.EmptyCardPenaltyScore,
            item.PenaltyTotal,
            item.BonusDelta,
            item.TotalKillCount,
            item.FinalScore,
            item.CalculationLines.Select(
                line => new GameRoundScoreCalculationLineDto(
                    line.Kind,
                    line.ModifierId,
                    line.ModifierName,
                    line.ActivationCount,
                    line.PointsDelta,
                    line.RunningTotal,
                    line.FormulaCode,
                    line.FormulaVersion,
                    line.Operands.Select(
                        operand => new GameRoundScoreCalculationOperandDto(
                            operand.Code,
                            operand.Value
                        )
                    ).ToArray()
                )
            ).ToArray()
        );
    }

}
