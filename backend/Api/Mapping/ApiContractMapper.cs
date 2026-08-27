using backend.Api.Contracts;
using backend.Application.Abstractions.Auth;
using backend.Application.Contracts;
using backend.Domain.GameModifiers;

namespace backend.Api.Mapping;

public static class ApiContractMapper
{
    public static AuthSessionDto ToDto(this AuthSession session)
    {
        return new AuthSessionDto(
            session.UserId,
            session.DisplayName,
            session.Roles
                .Select(TryMapAuthRole)
                .Where(role => role.HasValue)
                .Select(role => role!.Value)
                .ToArray()
        );
    }

    public static GameSetupDraftUpdate ToUpdateModel(this UpdateGameSetupRequestDto request)
    {
        return new GameSetupDraftUpdate(
            request.ExpectedVersion,
            request.Title,
            request.RowLabels,
            request.ColLabels,
            request.Cells
                .Select(cell => new GameSetupCellUpdate(cell.Id, cell.Row, cell.Col, cell.Title, cell.Cost))
                .ToArray(),
            (request.EnabledModifierIds ?? Array.Empty<string>())
                .Select(id => Guid.TryParse(id, out var parsed) ? parsed : Guid.Empty)
                .ToArray(),
            (request.EnabledQuestionIds ?? Array.Empty<string>())
                .Select(id => Guid.TryParse(id, out var parsed) ? parsed : Guid.Empty)
                .ToArray()
        );
    }

    public static CreateGameModifierInput ToInput(this CreateGameModifierRequestDto request)
    {
        return new CreateGameModifierInput(
            request.Name,
            request.Description,
            request.Category,
            request.ActivationCost,
            request.ActivationLimit.ToModel(),
            (request.ConflictingModifierIds ?? Array.Empty<string>())
                .Select(id => Guid.TryParse(id, out var parsed) ? parsed : Guid.Empty)
                .ToArray(),
            request.IconEmoji,
            request.ActivationCommand,
            request.NormalizedTags,
            request.BehaviorV2.ToModel()
        );
    }

    public static UpdateGameModifierInput ToInput(this UpdateGameModifierRequestDto request)
    {
        return new UpdateGameModifierInput(
            request.Name,
            request.Description,
            request.Category,
            request.ActivationCost,
            request.ActivationLimit.ToModel(),
            (request.ConflictingModifierIds ?? Array.Empty<string>())
                .Select(id => Guid.TryParse(id, out var parsed) ? parsed : Guid.Empty)
                .ToArray(),
            request.IconEmoji,
            request.ActivationCommand,
            request.NormalizedTags,
            request.BehaviorV2.ToModel()
        );
    }

    public static GameModifierDraftPreviewDto ToDto(this GameModifierDraftPreview preview) => new(
        preview.Name,
        preview.Description,
        preview.IconEmoji,
        preview.ActivationCommand,
        preview.NormalizedTags.ToArray(),
        preview.BehaviorV2.ToDto(),
        new GameModifierDraftExampleDto(
            preview.Example.CardValue,
            preview.Example.KillsCount,
            preview.Example.BountyCount,
            preview.Example.ResolutionExample,
            preview.Example.PointsDelta,
            preview.Example.BonusKillsDelta,
            preview.Example.FinalScore
        )
    );

    public static CreateGameQuestionInput ToInput(
        this CreateGameQuestionRequestDto request,
        Guid categoryId
    )
    {
        return new CreateGameQuestionInput(
            request.ExternalCode,
            categoryId,
            request.Text,
            request.Answer,
            request.Reward,
            request.IsEnabled,
            request.Priority
        );
    }

    public static ImportGameQuestionInput ToInput(
        this ImportGameQuestionRequestDto request,
        int rowNumber,
        Guid categoryId
    )
    {
        return new ImportGameQuestionInput(
            rowNumber,
            categoryId,
            request.Text,
            request.Answer,
            request.Reward,
            request.ExternalCode,
            request.IsEnabled,
            request.Priority,
            request.ToSource()
        );
    }

    public static ImportGameQuestionSource ToSource(this ImportGameQuestionRequestDto request)
    {
        return new ImportGameQuestionSource(
            request.Text,
            request.Answer,
            request.Reward,
            request.CategoryId,
            request.ExternalCode,
            request.IsEnabled,
            request.Priority
        );
    }

    public static UpdateGameQuestionInput ToInput(
        this UpdateGameQuestionRequestDto request,
        Guid categoryId
    )
    {
        return new UpdateGameQuestionInput(
            categoryId,
            request.Text,
            request.Answer,
            request.Reward,
            request.IsEnabled,
            request.Priority
        );
    }

    public static GameSetupSnapshotDto ToSetupDto(this GameBoardSnapshot snapshot)
    {
        return new GameSetupSnapshotDto(
            snapshot.GameId,
            snapshot.Title,
            snapshot.Description,
            snapshot.Status,
            snapshot.Version,
            snapshot.Rows,
            snapshot.Cols,
            snapshot.RowLabels.ToArray(),
            snapshot.ColLabels.ToArray(),
            snapshot.Cells.Select(ToDto).ToArray(),
            snapshot.EnabledModifierIds.Select(id => id.ToString()).ToArray(),
            snapshot.EnabledQuestionIds.ToArray()
        );
    }

    public static GameBoardCellMediaDto ToDto(this GameBoardCellMedia media)
    {
        return new GameBoardCellMediaDto(media.Url);
    }

    public static GameBoardSnapshotDto ToDto(this GameBoardSnapshot snapshot)
    {
        return new GameBoardSnapshotDto(
            snapshot.GameId,
            snapshot.Title,
            snapshot.Description,
            snapshot.Status,
            snapshot.Version,
            snapshot.Rows,
            snapshot.Cols,
            snapshot.RowLabels.ToArray(),
            snapshot.ColLabels.ToArray(),
            snapshot.Cells.Select(ToDto).ToArray(),
            snapshot.EnabledModifierIds.Select(id => id.ToString()).ToArray(),
            snapshot.ActiveModifiers.Select(ToDto).ToArray(),
            snapshot.ActiveTeamId
        );
    }

    public static GameUserNotificationDto ToDto(this GameUserNotification notification)
    {
        return new GameUserNotificationDto(
            notification.NotificationId.ToString(),
            notification.Type,
            notification.CreatedAtUtc,
            notification.ModifierName,
            notification.ActorDisplayName,
            notification.QuizPointsDelta
        );
    }

    public static GameTeamQueueItemDto ToDto(this GameTeamQueueItem item)
    {
        return new GameTeamQueueItemDto(
            item.TeamId.ToString(),
            item.TeamName,
            item.TeamSlotIndex,
            item.IsPlayed,
            item.PlayedAtUtc,
            item.Participants
                .Select(
                    participant =>
                        new GameTeamQueueParticipantDto(
                            participant.UserId.ToString(),
                            participant.DisplayName
                        )
                )
                .ToArray()
        );
    }

    public static GameTeamQueueSummaryDto ToDto(this GameTeamQueueSummary summary)
    {
        return new GameTeamQueueSummaryDto(
            summary.TotalTeams,
            summary.PlayedTeams,
            summary.RemainingTeams
        );
    }

    public static GameTeamQueueResultDto ToDto(this GameTeamQueueResult result)
    {
        return new GameTeamQueueResultDto(
            result.Summary.ToDto(),
            result.Teams.Select(x => x.ToDto()).ToArray()
        );
    }

    public static GameCellOpenedEventDto ToDto(this GameCellOpenedEvent @event)
    {
        return new GameCellOpenedEventDto(@event.GameId, @event.Version, ToDto(@event.Cell));
    }

    public static GameModifierDefinitionDto ToDto(this GameModifierDefinition definition)
    {
        return new GameModifierDefinitionDto(
            definition.Id.ToString(),
            definition.Category,
            definition.Name,
            definition.Description,
            definition.ActivationCost,
            definition.ActivationLimit.ToDto(),
            definition.ConflictingModifierIds.Select(id => id.ToString()).ToArray(),
            definition.IconEmoji,
            definition.ActivationCommand,
            definition.IsLockedByActiveGame,
            definition.Revision,
            definition.NormalizedTags.ToArray(),
            definition.BehaviorV2.ToDto()
        );
    }

    private static ModifierBehaviorV2 ToModel(this GameModifierBehaviorV2Dto dto) => new(
        dto.SchemaVersion,
        dto.Kind switch
        {
            "rule" => ModifierBehaviorKind.Rule,
            "scoring" => ModifierBehaviorKind.Scoring,
            _ => (ModifierBehaviorKind)(-1)
        },
        dto.Phase switch
        {
            "preparation" => ModifierPhase.Preparation,
            "round" => ModifierPhase.Round,
            "result" => ModifierPhase.Result,
            _ => (ModifierPhase)(-1)
        },
        dto.Performer switch
        {
            "activeTeam" => ModifierPerformer.ActiveTeam,
            "mentor" => ModifierPerformer.Mentor,
            _ => (ModifierPerformer)(-1)
        },
        dto.RequiresHostMonitoring,
        dto.Rule,
        dto.StackingPolicy switch
        {
            "aggregateParameters" => ModifierStackingPolicy.AggregateParameters,
            "independentInstances" => ModifierStackingPolicy.IndependentInstances,
            _ => (ModifierStackingPolicy)(-1)
        },
        dto.Resolution.ToModel(),
        dto.Reward switch
        {
            "none" => ModifierRewardKind.None,
            "points" => ModifierRewardKind.Points,
            "bonusKills" => ModifierRewardKind.BonusKills,
            _ => (ModifierRewardKind)(-1)
        },
        dto.FormulaReference?.ToModel(),
        dto.DurationSecondsPerActivation
    );

    private static ModifierResolution ToModel(this GameModifierResolutionDto dto) => dto switch
    {
        GameModifierRuleStatusResolutionDto => new RuleStatusResolution(),
        GameModifierBooleanResolutionDto value => new BooleanResolution(value.InputLabel),
        GameModifierNonNegativeCountResolutionDto value => new NonNegativeCountResolution(
            value.InputLabel,
            value.MaximumKind,
            value.MaximumPerActivation
        ),
        GameModifierAutomaticRoundMetricResolutionDto value =>
            new AutomaticRoundMetricResolution(value.Metric),
        GameModifierPerActivationResolutionDto => new PerActivationResolution(),
        _ => throw new ArgumentOutOfRangeException(nameof(dto))
    };

    private static ModifierFormulaReference ToModel(this GameModifierFormulaReferenceV2Dto dto) =>
        new(dto.Code, dto.Version, dto.Parameters.ToModel());

    private static ModifierFormulaParameters ToModel(this GameModifierFormulaParametersDto dto) =>
        dto switch
        {
            GameModifierGrowingKillValueParametersDto value =>
                new GrowingKillValueParameters(
                    value.IncrementPointsPerKill,
                    value.ZeroKillPenaltyPoints
                ),
            GameModifierBonusKillOnConditionParametersDto value =>
                new BonusKillOnConditionParameters(value.SuccessBonusKills),
            GameModifierBonusKillsByCountParametersDto value =>
                new BonusKillsByCountParameters(value.BonusKillsPerUnit),
            GameModifierWindowKillBonusPointsParametersDto value =>
                new WindowKillBonusPointsParameters(value.BonusRate),
            GameModifierFixedPointsPerUnitParametersDto value =>
                new FixedPointsPerUnitParameters(value.PointsPerUnit),
            GameModifierCardPercentPerUnitParametersDto value =>
                new CardPercentPerUnitParameters(value.Rate),
            GameModifierBonusKillsPerUnitParametersDto value =>
                new BonusKillsPerUnitParameters(value.BonusKillsPerUnit),
            GameModifierKillValueIncreasePerUnitParametersDto value =>
                new KillValueIncreasePerUnitParameters(
                    value.IncrementPointsPerUnit,
                    value.ZeroCountPenaltyPoints
                ),
            _ => throw new ArgumentOutOfRangeException(nameof(dto))
        };

    private static GameModifierBehaviorV2Dto ToDto(this ModifierBehaviorV2 behavior) => new(
        behavior.SchemaVersion,
        behavior.Kind == ModifierBehaviorKind.Rule ? "rule" : "scoring",
        behavior.Phase switch
        {
            ModifierPhase.Preparation => "preparation",
            ModifierPhase.Round => "round",
            ModifierPhase.Result => "result",
            _ => throw new ArgumentOutOfRangeException(nameof(behavior))
        },
        behavior.Performer == ModifierPerformer.ActiveTeam ? "activeTeam" : "mentor",
        behavior.RequiresHostMonitoring,
        behavior.Rule,
        behavior.StackingPolicy == ModifierStackingPolicy.AggregateParameters
            ? "aggregateParameters"
            : "independentInstances",
        behavior.Resolution.ToDto(),
        behavior.Reward switch
        {
            ModifierRewardKind.None => "none",
            ModifierRewardKind.Points => "points",
            ModifierRewardKind.BonusKills => "bonusKills",
            _ => throw new ArgumentOutOfRangeException(nameof(behavior))
        },
        behavior.FormulaReference?.ToDto(),
        behavior.DurationSecondsPerActivation
    );

    private static GameModifierResolutionDto ToDto(this ModifierResolution resolution) =>
        resolution switch
        {
            RuleStatusResolution => new GameModifierRuleStatusResolutionDto(),
            BooleanResolution value => new GameModifierBooleanResolutionDto(value.InputLabel),
            NonNegativeCountResolution value => new GameModifierNonNegativeCountResolutionDto(
                value.InputLabel,
                value.MaximumKind,
                value.MaximumPerActivation
            ),
            AutomaticRoundMetricResolution value =>
                new GameModifierAutomaticRoundMetricResolutionDto(value.Metric),
            PerActivationResolution => new GameModifierPerActivationResolutionDto(),
            _ => throw new ArgumentOutOfRangeException(nameof(resolution))
        };

    private static GameModifierFormulaReferenceV2Dto ToDto(
        this ModifierFormulaReference reference
    ) => new(reference.Code, reference.Version, reference.Parameters.ToDto());

    private static GameModifierFormulaParametersDto ToDto(
        this ModifierFormulaParameters parameters
    ) => parameters switch
    {
        GrowingKillValueParameters value => new GameModifierGrowingKillValueParametersDto(
            value.IncrementPointsPerKill,
            value.ZeroKillPenaltyPoints
        ),
        BonusKillOnConditionParameters value =>
            new GameModifierBonusKillOnConditionParametersDto(value.SuccessBonusKills),
        BonusKillsByCountParameters value =>
            new GameModifierBonusKillsByCountParametersDto(value.BonusKillsPerUnit),
        WindowKillBonusPointsParameters value =>
            new GameModifierWindowKillBonusPointsParametersDto(value.BonusRate),
        FixedPointsPerUnitParameters value =>
            new GameModifierFixedPointsPerUnitParametersDto(value.PointsPerUnit),
        CardPercentPerUnitParameters value =>
            new GameModifierCardPercentPerUnitParametersDto(value.Rate),
        BonusKillsPerUnitParameters value =>
            new GameModifierBonusKillsPerUnitParametersDto(value.BonusKillsPerUnit),
        KillValueIncreasePerUnitParameters value =>
            new GameModifierKillValueIncreasePerUnitParametersDto(
                value.IncrementPointsPerUnit,
                value.ZeroCountPenaltyPoints
            ),
        _ => throw new ArgumentOutOfRangeException(nameof(parameters))
    };

    private static GameModifierActivationLimit ToModel(this GameModifierActivationLimitDto? dto)
    {
        if (dto is null)
        {
            return new GameModifierActivationLimit(null);
        }

        return new GameModifierActivationLimit(dto.Count);
    }

    private static GameModifierActivationLimitDto ToDto(this GameModifierActivationLimit model)
    {
        return new GameModifierActivationLimitDto(model.Count);
    }

    public static GameModifierActivationDto ToDto(this GameModifierActivation activation)
    {
        return new GameModifierActivationDto(
            activation.ActivationId.ToString(),
            activation.RoundId.ToString(),
            activation.RoundVersion,
            activation.ModifierId.ToString(),
            activation.ModifierName,
            activation.ActivatedByUserId,
            activation.ActivatedByDisplayName,
            activation.ActivationCost,
            activation.ActivatedAtUtc
        );
    }

    public static GameModifierAvailabilityDto ToDto(this GameModifierAvailability availability)
    {
        return new GameModifierAvailabilityDto(
            availability.Modifier.ToDto(),
            availability.IsActive,
            availability.CanActivate,
            availability.BlockedReason,
            availability.ActivationsCount,
            availability.Limit,
            availability.IsEmergencyDisabled,
            availability.EmergencyDisabledAtUtc
        );
    }

    public static GameModifierStateDto ToDto(this GameModifierState state)
    {
        return new GameModifierStateDto(
            state.GameId.ToString(),
            state.AvailableQuizPoints,
            state.EarnedQuizPoints,
            state.SpentQuizPoints,
            state.IsOrderingOpen,
            state.ActiveModifiers.Select(x => x.ToDto()).ToArray(),
            state.AvailableModifiers.Select(x => x.ToDto()).ToArray()
        );
    }

    public static GameModifierAdminPlayerDto ToDto(this GameModifierAdminPlayer player)
    {
        return new GameModifierAdminPlayerDto(
            player.UserId.ToString(),
            player.Login,
            player.DisplayName,
            player.AvailableQuizPoints,
            player.EarnedQuizPoints,
            player.SpentQuizPoints
        );
    }

    public static GameModifierAdminPlayersSummaryDto ToDto(this GameModifierAdminPlayersSummary summary)
    {
        return new GameModifierAdminPlayersSummaryDto(
            summary.PlayersCount,
            summary.TotalAvailableQuizPoints,
            summary.TotalEarnedQuizPoints,
            summary.TotalSpentQuizPoints
        );
    }

    public static GameModifierAdminPlayersResultDto ToDto(this GameModifierAdminPlayersResult result)
    {
        return new GameModifierAdminPlayersResultDto(
            result.Summary.ToDto(),
            result.Players.Select(x => x.ToDto()).ToArray()
        );
    }

    public static GameModifierActivatedEventDto ToDto(this GameModifierActivatedEvent @event)
    {
        return new GameModifierActivatedEventDto(@event.GameId, @event.Version, @event.Activation.ToDto());
    }

    public static GameModifierActivationCancelledEventDto ToDto(
        this GameModifierActivationCancelledEvent @event
    )
    {
        return new GameModifierActivationCancelledEventDto(
            @event.GameId,
            @event.Version,
            @event.ActivationId.ToString()
        );
    }

    public static GameModifierAvailabilityChangedEventDto ToDto(
        this GameModifierAvailabilityChangedEvent @event
    )
    {
        return new GameModifierAvailabilityChangedEventDto(
            @event.GameId,
            @event.Version,
            @event.ModifierId.ToString()
        );
    }

    public static GameUserNotificationCreatedEventDto ToDto(
        this GameUserNotificationCreatedEvent @event
    )
    {
        return new GameUserNotificationCreatedEventDto(@event.Notification.ToDto());
    }

    public static GameQuestionCatalogItemDto ToDto(this GameQuestionCatalogItem item)
    {
        return new GameQuestionCatalogItemDto(
            item.QuestionId.ToString(),
            item.QuestionCode,
            item.CategoryId.ToString(),
            item.CategoryName,
            item.Text,
            item.Answer,
            item.Reward,
            item.Priority,
            item.IsEnabled,
            item.AskedTotalCount,
            item.CorrectTotalCount,
            item.LastAskedAtUtc
        );
    }

    public static AskedQuizQuestionDto ToDto(this AskedQuizQuestion question)
    {
        return new AskedQuizQuestionDto(
            question.RoundId.ToString(),
            question.GameId.ToString(),
            question.AskOrder,
            question.QuestionId.ToString(),
            question.QuestionCode,
            question.CategoryName,
            question.Text,
            question.Reward,
            question.AskedAtUtc
        );
    }

    public static GameQuestionCategoryItemDto ToDto(this GameQuestionCategoryItem item)
    {
        return new GameQuestionCategoryItemDto(
            item.Id.ToString(),
            item.Name,
            item.QuestionCount,
            item.IsProtected
        );
    }

    public static ImportGameQuestionSkippedItemDto ToDto(this ImportGameQuestionSkippedItem item)
    {
        return new ImportGameQuestionSkippedItemDto(
            item.RowNumber,
            item.QuestionText,
            item.ReasonCode,
            item.Reason,
            item.SourceQuestion?.ToDto()
        );
    }

    public static ImportGameQuestionSourceDto ToDto(this ImportGameQuestionSource source)
    {
        return new ImportGameQuestionSourceDto(
            source.Text,
            source.Answer,
            source.Reward,
            source.CategoryId,
            source.ExternalCode,
            source.IsEnabled,
            source.Priority
        );
    }

    public static GameQuizRoundSummaryDto ToDto(this GameQuizRoundSummary round)
    {
        return new GameQuizRoundSummaryDto(
            round.RoundId.ToString(),
            round.GameId.ToString(),
            round.AskOrder,
            round.QuestionId.ToString(),
            round.QuestionText,
            round.CategoryName,
            round.Reward,
            round.Status,
            round.AskedAtUtc,
            round.AnsweredAtUtc,
            round.AnsweredByDisplayName,
            round.AnsweredByUserId?.ToString(),
            round.AnsweredForUserId?.ToString(),
            round.SubmittedAnswer,
            round.IsCorrect,
            round.AwardedPoints
        );
    }

    public static ManualQuizAwardSummaryDto ToDto(this ManualQuizAwardSummary award)
    {
        return new ManualQuizAwardSummaryDto(
            award.AwardId.ToString(),
            award.GameId.ToString(),
            award.AwardedToUserId.ToString(),
            award.AwardedToDisplayName,
            award.AwardedByUserId.ToString(),
            award.AwardedByDisplayName,
            award.OperationType,
            award.PointsDelta,
            award.Reason,
            award.AvailablePointsBefore,
            award.AvailablePointsAfter,
            award.RequestId.ToString(),
            award.AwardedAtUtc
        );
    }

    public static GameQuizStateChangedEventDto ToDto(this GameQuizStateChangedEvent @event)
    {
        return new GameQuizStateChangedEventDto(
            @event.GameId.ToString(),
            @event.ChangeKind,
            @event.OccurredAtUtc
        );
    }

    public static GameRoundStateChangedEventDto ToDto(this GameRoundStateChangedEvent @event)
    {
        return new GameRoundStateChangedEventDto(
            @event.GameId.ToString(),
            @event.RoundId.ToString(),
            @event.Status,
            @event.RoundVersion,
            @event.OccurredAtUtc
        );
    }

    public static ManualQuizAwardPlayerDto ToDto(this ManualQuizAwardPlayer player)
    {
        return new ManualQuizAwardPlayerDto(
            player.UserId.ToString(),
            player.Login,
            player.DisplayName,
            player.EarnedQuizPoints,
            player.SpentQuizPoints,
            player.AvailableQuizPoints
        );
    }

    public static UserGameHistoryItemDto ToDto(this UserGameHistoryItem item)
    {
        return new UserGameHistoryItemDto(
            item.GameId.ToString(),
            item.GameTitle,
            item.GameStatus,
            item.CreatedAtUtc,
            item.StartedAtUtc,
            item.FinishedAtUtc,
            item.ModifierActivations.Select(ToDto).ToArray(),
            item.QuestionAnswers.Select(ToDto).ToArray(),
            item.ManualQuizAwards.Select(ToDto).ToArray()
        );
    }

    public static UserGameModifierActivationHistoryItemDto ToDto(
        this UserGameModifierActivationHistoryItem item
    )
    {
        return new UserGameModifierActivationHistoryItemDto(item.ModifierId.ToString(), item.ActivatedAtUtc);
    }

    public static UserGameQuestionAnswerHistoryItemDto ToDto(
        this UserGameQuestionAnswerHistoryItem item
    )
    {
        return new UserGameQuestionAnswerHistoryItemDto(
            item.RoundId.ToString(),
            item.QuestionId.ToString(),
            item.QuestionText,
            item.CategoryName,
            item.AnsweredAtUtc,
            item.IsCorrect,
            item.AwardedPoints,
            item.SubmittedAnswer,
            item.AnsweredByUserId?.ToString()
        );
    }

    public static UserGameQuizManualAwardHistoryItemDto ToDto(
        this UserGameQuizManualAwardHistoryItem item
    )
    {
        return new UserGameQuizManualAwardHistoryItemDto(
            item.AwardId.ToString(),
            item.AwardedAtUtc,
            item.AwardedPoints,
            item.AwardedByUserId.ToString(),
            item.AwardedByDisplayName,
            item.OperationType,
            item.Reason
        );
    }

    public static GameHistoryLeaderboardEntryDto ToDto(this GameHistoryLeaderboardEntry item)
    {
        return new GameHistoryLeaderboardEntryDto(
            item.UserId.ToString(),
            item.DisplayName,
            item.MainGamePoints,
            item.QuizPoints,
            item.TotalPoints,
            item.GamesPlayed,
            item.MainGameRoundsPlayed,
            item.QuizRoundsAnswered,
            item.CorrectQuizAnswers,
            item.ModifiersActivated,
            item.LastActivityAtUtc
        );
    }

    public static GameHistoryGameSummaryDto ToDto(this GameHistoryGameSummary item)
    {
        return new GameHistoryGameSummaryDto(
            item.GameId.ToString(),
            item.GameTitle,
            item.GameStatus,
            item.CreatedAtUtc,
            item.StartedAtUtc,
            item.FinishedAtUtc,
            item.MainGameRoundCount,
            item.QuizRoundCount,
            item.UniquePlayerCount
        );
    }

    public static GameHistoryGameDetailsDto ToDto(this GameHistoryGameDetails item)
    {
        return new GameHistoryGameDetailsDto(
            item.GameId.ToString(),
            item.GameTitle,
            item.GameStatus,
            item.CreatedAtUtc,
            item.StartedAtUtc,
            item.FinishedAtUtc,
            item.MainGame.ToDto(),
            item.Quiz.ToDto()
        );
    }

    public static GameHistoryMainGameSectionDto ToDto(this GameHistoryMainGameSection item)
    {
        return new GameHistoryMainGameSectionDto(
            item.PlayerStats.Select(ToDto).ToArray(),
            item.TeamStats.Select(ToDto).ToArray(),
            item.ModifierActivations.Select(ToDto).ToArray(),
            item.Rounds.Select(ToDto).ToArray()
        );
    }

    public static GameHistoryQuizSectionDto ToDto(this GameHistoryQuizSection item)
    {
        return new GameHistoryQuizSectionDto(
            item.TotalPoints,
            item.PlayerStats.Select(ToDto).ToArray(),
            item.Rounds.Select(ToDto).ToArray(),
            item.ManualAwards.Select(ToDto).ToArray()
        );
    }

    public static GameHistoryPlayerSummaryDto ToDto(this GameHistoryPlayerSummary item)
    {
        return new GameHistoryPlayerSummaryDto(
            item.UserId.ToString(),
            item.DisplayName,
            item.Points,
            item.EventCount,
            item.LastActivityAtUtc
        );
    }

    public static GameHistoryModifierActivationItemDto ToDto(
        this GameHistoryModifierActivationItem item
    )
    {
        return new GameHistoryModifierActivationItemDto(
            item.ActivationId.ToString(),
            item.ModifierId.ToString(),
            item.ModifierName,
            item.ActivatedByUserId.ToString(),
            item.ActivatedByDisplayName,
            item.ActivatedAtUtc
        );
    }

    public static GameHistoryRoundItemDto ToDto(this GameHistoryRoundItem item)
    {
        return new GameHistoryRoundItemDto(
            item.RoundId.ToString(),
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
            item.CellId.ToString(),
            item.CellRowIndex,
            item.CellColIndex,
            item.CellType,
            item.CellTitle,
            item.CellDescription,
            item.CellCost,
            item.Notes,
            item.TechnicalCancellationReasonCode,
            item.PublicCancellationSummary,
            item.TechnicalCancellationStage,
            item.PurchasesRefunded,
            item.CellMedia.Select(ToDto).ToArray(),
            item.Participants.Select(ToDto).ToArray(),
            item.Modifiers.Select(ToDto).ToArray()
        );
    }

    public static GameHistoryRoundParticipantItemDto ToDto(
        this GameHistoryRoundParticipantItem item
    )
    {
        return new GameHistoryRoundParticipantItemDto(
            item.UserId.ToString(),
            item.DisplayName,
            item.CreatedAtUtc
        );
    }

    public static GameHistoryRoundModifierItemDto ToDto(
        this GameHistoryRoundModifierItem item
    )
    {
        return new GameHistoryRoundModifierItemDto(
            item.ModifierResultId.ToString(),
            item.ModifierId.ToString(),
            item.ModifierName,
            item.ModifierDescription,
            item.ModifierCategory,
            item.OutcomeStatus,
            item.ScoreDelta,
            item.KillDelta,
            item.MultiplierApplied,
            item.ResolutionDataJson,
            item.ResolvedByUserId?.ToString(),
            item.ResolvedAtUtc,
            item.ActivationId.ToString(),
            item.DefinitionRevision,
            item.ResolutionKind,
            item.ViolationComment
        );
    }

    public static GameHistoryQuizRoundItemDto ToDto(this GameHistoryQuizRoundItem item)
    {
        return new GameHistoryQuizRoundItemDto(
            item.RoundId.ToString(),
            item.QuestionId.ToString(),
            item.QuestionCode,
            item.QuestionText,
            item.CategoryName,
            item.Reward,
            item.Status,
            item.AskedAtUtc,
            item.AnsweredAtUtc,
            item.AnsweredByDisplayName,
            item.AnsweredByUserId?.ToString(),
            item.AnsweredForUserId?.ToString(),
            item.AnsweredForDisplayName,
            item.SubmittedAnswer,
            item.IsCorrect,
            item.AwardedPoints
        );
    }

    public static GameHistoryQuizManualAwardItemDto ToDto(this GameHistoryQuizManualAwardItem item)
    {
        return new GameHistoryQuizManualAwardItemDto(
            item.AwardId.ToString(),
            item.AwardedToUserId.ToString(),
            item.AwardedToDisplayName,
            item.AwardedByUserId.ToString(),
            item.AwardedByDisplayName,
            item.AwardedPoints,
            item.OperationType,
            item.Reason,
            item.AwardedAtUtc
        );
    }

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
                        _ => throw new ArgumentOutOfRangeException()
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

    private static GameBoardCellDto ToDto(GameBoardCell cell)
    {
        return new GameBoardCellDto(
            cell.Id,
            cell.Row,
            cell.Col,
            cell.CellType,
            cell.Title,
            cell.Description,
            cell.Cost,
            cell.State.ToString().ToLowerInvariant(),
            cell.Media.Select(media => new GameBoardCellMediaDto(media.Url)).ToArray()
        );
    }

    private static AuthRole? TryMapAuthRole(string role)
    {
        return role switch
        {
            AuthRoleCodes.Admin => AuthRole.Admin,
            AuthRoleCodes.Moderator => AuthRole.Moderator,
            AuthRoleCodes.Viewer => AuthRole.Viewer,
            _ => null
        };
    }
}
