using backend.Api.Contracts;
using backend.Application.Abstractions.Auth;
using backend.Application.Contracts;

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
            ResolveScoringType(request.ScoringType, request.MechanicType),
            request.Category,
            request.RequiresHostControl,
            request.MechanicType,
            request.ActivationCost,
            request.DefaultLimitPerGame,
            request.ActivationLimit.ToModel(),
            request.Effect.ToModel(),
            (request.ConflictingModifierIds ?? Array.Empty<string>())
                .Select(id => Guid.TryParse(id, out var parsed) ? parsed : Guid.Empty)
                .ToArray(),
            request.IconEmoji,
            request.ActivationCommand
        );
    }

    public static UpdateGameModifierInput ToInput(this UpdateGameModifierRequestDto request)
    {
        return new UpdateGameModifierInput(
            request.Name,
            request.Description,
            ResolveScoringType(request.ScoringType, request.MechanicType),
            request.Category,
            request.RequiresHostControl,
            request.MechanicType,
            request.ActivationCost,
            request.DefaultLimitPerGame,
            request.ActivationLimit.ToModel(),
            request.Effect.ToModel(),
            (request.ConflictingModifierIds ?? Array.Empty<string>())
                .Select(id => Guid.TryParse(id, out var parsed) ? parsed : Guid.Empty)
                .ToArray(),
            request.IconEmoji,
            request.ActivationCommand
        );
    }

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
            item.TeamSlotIndex,
            item.IsPlayed,
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

    public static GameCellOpenedEventDto ToDto(this GameCellOpenedEvent @event)
    {
        return new GameCellOpenedEventDto(@event.GameId, @event.Version, ToDto(@event.Cell));
    }

    public static GameModifierDefinitionDto ToDto(this GameModifierDefinition definition)
    {
        return new GameModifierDefinitionDto(
            definition.Id.ToString(),
            definition.ScoringType,
            definition.Category,
            definition.RequiresHostControl,
            definition.MechanicType,
            definition.Name,
            definition.Description,
            definition.ActivationCost,
            definition.DefaultLimitPerGame,
            definition.ActivationLimit.ToDto(),
            definition.Effect.ToDto(),
            definition.ConflictingModifierIds.Select(id => id.ToString()).ToArray(),
            definition.IconEmoji,
            definition.ActivationCommand
        );
    }

    private static string ResolveScoringType(string? scoringType, string mechanicType)
    {
        var normalizedMechanicType = (mechanicType ?? string.Empty).Trim().ToLowerInvariant();
        var derived = normalizedMechanicType switch
        {
            GameModifierMechanicTypes.RestrictionWithReward =>
                GameModifierScoringTypes.ConditionalBonusPenalty,
            GameModifierMechanicTypes.KillCounter => GameModifierScoringTypes.ConditionalBonus,
            GameModifierMechanicTypes.Multiplier => GameModifierScoringTypes.Multiplier,
            _ => GameModifierScoringTypes.NonScoring
        };

        if (string.IsNullOrWhiteSpace(scoringType))
        {
            return derived;
        }

        var normalizedScoringType = scoringType.Trim().ToLowerInvariant();
        return normalizedScoringType == derived ? normalizedScoringType : derived;
    }

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

    private static GameModifierEffect ToModel(this GameModifierEffectDto? dto)
    {
        if (dto is null)
        {
            return new GameModifierEffect(
                string.Empty,
                [],
                null,
                null,
                null,
                [],
                [],
                null,
                null,
                null
            );
        }

        return new GameModifierEffect(
            dto.MechanicType,
            dto.Traits ?? [],
            dto.DurationSeconds,
            dto.RuleText,
            dto.ScoreImpact?.ToModel(),
            (dto.Conditions ?? [])
                .Where(x => x is not null)
                .Select(x => x!.ToModel())
                .ToArray(),
            dto.ResolutionInputs ?? [],
            dto.KillEffect?.ToModel(),
            dto.MultiplierEffect?.ToModel(),
            dto.MentorEffect?.ToModel()
        );
    }

    private static GameModifierEffectDto ToDto(this GameModifierEffect model)
    {
        return new GameModifierEffectDto(
            model.MechanicType,
            model.Traits,
            model.DurationSeconds,
            model.RuleText,
            model.ScoreImpact?.ToDto(),
            model.Conditions.Select(x => x.ToDto()).ToArray(),
            model.ResolutionInputs,
            model.KillEffect?.ToDto(),
            model.MultiplierEffect?.ToDto(),
            model.MentorEffect?.ToDto()
        );
    }

    private static GameModifierScoreImpact ToModel(this GameModifierScoreImpactDto dto)
    {
        return new GameModifierScoreImpact(
            dto.PointsDelta,
            dto.PerKillBonus,
            dto.FailurePenaltyPoints,
            dto.MultiplierDelta,
            dto.KillDelta,
            dto.ScoreFormula?.ToModel()
        );
    }

    private static GameModifierScoreImpactDto ToDto(this GameModifierScoreImpact model)
    {
        return new GameModifierScoreImpactDto(
            model.PointsDelta,
            model.PerKillBonus,
            model.FailurePenaltyPoints,
            model.MultiplierDelta,
            model.KillDelta,
            model.ScoreFormula?.ToDto()
        );
    }

    private static GameModifierScoreFormula ToModel(this GameModifierScoreFormulaDto dto)
    {
        return new GameModifierScoreFormula(
            dto.Mode,
            dto.SuccessExpression,
            dto.FailureExpression
        );
    }

    private static GameModifierScoreFormulaDto ToDto(this GameModifierScoreFormula model)
    {
        return new GameModifierScoreFormulaDto(
            model.Mode,
            model.SuccessExpression,
            model.FailureExpression
        );
    }

    private static GameModifierCondition ToModel(this GameModifierConditionDto dto)
    {
        return new GameModifierCondition(dto.Type, dto.Source);
    }

    private static GameModifierConditionDto ToDto(this GameModifierCondition model)
    {
        return new GameModifierConditionDto(model.Type, model.Source);
    }

    private static GameModifierKillEffect ToModel(this GameModifierKillEffectDto dto)
    {
        return new GameModifierKillEffect(
            dto.KillDeltaMode,
            dto.KillDeltaValue,
            dto.Condition,
            dto.ExcludedWeapons ?? []
        );
    }

    private static GameModifierKillEffectDto ToDto(this GameModifierKillEffect model)
    {
        return new GameModifierKillEffectDto(
            model.KillDeltaMode,
            model.KillDeltaValue,
            model.Condition,
            model.ExcludedWeapons
        );
    }

    private static GameModifierMultiplierEffect ToModel(this GameModifierMultiplierEffectDto dto)
    {
        return new GameModifierMultiplierEffect(
            dto.Target,
            dto.Delta,
            dto.ActiveWindow,
            dto.StopCondition
        );
    }

    private static GameModifierMultiplierEffectDto ToDto(this GameModifierMultiplierEffect model)
    {
        return new GameModifierMultiplierEffectDto(
            model.Target,
            model.Delta,
            model.ActiveWindow,
            model.StopCondition
        );
    }

    private static GameModifierMentorEffect ToModel(this GameModifierMentorEffectDto dto)
    {
        return new GameModifierMentorEffect(
            dto.LoadoutText,
            dto.DurationSeconds,
            dto.CanBeRevived,
            dto.CanBeKilled,
            dto.KillsCreditToTeam
        );
    }

    private static GameModifierMentorEffectDto ToDto(this GameModifierMentorEffect model)
    {
        return new GameModifierMentorEffectDto(
            model.LoadoutText,
            model.DurationSeconds,
            model.CanBeRevived,
            model.CanBeKilled,
            model.KillsCreditToTeam
        );
    }

    public static GameModifierActivationDto ToDto(this GameModifierActivation activation)
    {
        return new GameModifierActivationDto(
            activation.ActivationId.ToString(),
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
            availability.Limit
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
            award.Points,
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
            @event.OccurredAtUtc
        );
    }

    public static ManualQuizAwardPlayerDto ToDto(this ManualQuizAwardPlayer player)
    {
        return new ManualQuizAwardPlayerDto(
            player.UserId.ToString(),
            player.Login,
            player.DisplayName
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
            item.AwardedByDisplayName
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
            item.ModifierActivations.Select(ToDto).ToArray(),
            item.Rounds.Select(ToDto).ToArray()
        );
    }

    public static GameHistoryQuizSectionDto ToDto(this GameHistoryQuizSection item)
    {
        return new GameHistoryQuizSectionDto(
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
            item.TeamSlotIndex,
            item.Status,
            item.StartedAtUtc,
            item.FinishedAtUtc,
            item.BaseScore,
            item.FinalScore,
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
            item.ModifierMechanicType,
            item.OutcomeStatus,
            item.ScoreDelta,
            item.KillDelta,
            item.MultiplierApplied,
            item.ResolvedByUserId?.ToString(),
            item.ResolvedAtUtc
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
        IReadOnlyList<FinalizeGameRoundModifierInput> modifierResults
    )
    {
        return new FinalizeGameRoundInput(
            request.Status.Trim(),
            request.FinalScore,
            request.KillsCount,
            request.BountyCount,
            string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            modifierResults
        );
    }

    public static FinalizeGameRoundModifierInput ToInput(
        this FinalizeGameRoundModifierRequestDto request,
        Guid modifierResultId
    )
    {
        return new FinalizeGameRoundModifierInput(
            modifierResultId,
            request.OutcomeStatus.Trim(),
            request.ScoreDelta,
            request.KillDelta,
            request.MultiplierApplied,
            request.ResolutionDataJson
        );
    }

    public static GameRoundDetailsDto ToDto(this GameRoundDetails item)
    {
        return new GameRoundDetailsDto(
            item.RoundId.ToString(),
            item.GameId.ToString(),
            item.CellId.ToString(),
            item.TeamId.ToString(),
            item.TeamSlotIndex,
            item.Status,
            item.StartedAtUtc,
            item.FinishedAtUtc,
            item.BaseScore,
            item.FinalScore,
            item.KillsCount,
            item.BountyCount,
            item.Notes,
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
            item.ModifierMechanicType,
            item.ModifierDescription,
            item.ModifierScoringType,
            item.ModifierEffect?.ToDto(),
            item.OutcomeStatus,
            item.ScoreDelta,
            item.KillDelta,
            item.MultiplierApplied,
            item.ResolutionDataJson,
            item.ResolvedByUserId?.ToString(),
            item.ResolvedAtUtc
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
