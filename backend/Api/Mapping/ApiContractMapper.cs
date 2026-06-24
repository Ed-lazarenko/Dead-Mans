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
            request.Kind,
            ResolveScoringType(request.ScoringType, request.MechanicType),
            request.MechanicType,
            request.Tier,
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
            request.Kind,
            ResolveScoringType(request.ScoringType, request.MechanicType),
            request.MechanicType,
            request.Tier,
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
            snapshot.ActiveModifiers.Select(ToDto).ToArray()
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
            definition.Kind,
            definition.ScoringType,
            definition.MechanicType,
            definition.Tier,
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
            return new GameModifierActivationLimit(null, string.Empty);
        }

        return new GameModifierActivationLimit(dto.Count, dto.Scope);
    }

    private static GameModifierActivationLimitDto ToDto(this GameModifierActivationLimit model)
    {
        return new GameModifierActivationLimitDto(model.Count, model.Scope);
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
            dto.KillDelta
        );
    }

    private static GameModifierScoreImpactDto ToDto(this GameModifierScoreImpact model)
    {
        return new GameModifierScoreImpactDto(
            model.PointsDelta,
            model.PerKillBonus,
            model.FailurePenaltyPoints,
            model.MultiplierDelta,
            model.KillDelta
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
            activation.ModifierId.ToString(),
            activation.ActivatedByUserId,
            activation.ActivatedAtUtc
        );
    }

    public static GameModifierActivatedEventDto ToDto(this GameModifierActivatedEvent @event)
    {
        return new GameModifierActivatedEventDto(@event.GameId, @event.Version, @event.Activation.ToDto());
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

    public static AskedGameQuestionDto ToDto(this AskedGameQuestion question)
    {
        return new AskedGameQuestionDto(
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
        return new ImportGameQuestionSkippedItemDto(item.RowNumber, item.QuestionText, item.Reason);
    }

    public static GameQuestionRoundSummaryDto ToDto(this GameQuestionRoundSummary round)
    {
        return new GameQuestionRoundSummaryDto(
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
            item.QuestionAnswers.Select(ToDto).ToArray()
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
