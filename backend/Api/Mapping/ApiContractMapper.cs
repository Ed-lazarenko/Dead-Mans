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
            request.EnabledModifierCodes ?? Array.Empty<string>(),
            (request.EnabledQuestionIds ?? Array.Empty<string>())
                .Select(id => Guid.TryParse(id, out var parsed) ? parsed : Guid.Empty)
                .ToArray()
        );
    }

    public static CreateGameModifierInput ToInput(this CreateGameModifierRequestDto request)
    {
        return new CreateGameModifierInput(
            request.Code,
            request.Name,
            request.Description,
            request.Kind,
            request.Category,
            request.ScoringType,
            request.Tier,
            request.ActivationCost,
            request.DefaultLimitPerGame,
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
            request.Category,
            request.ScoringType,
            request.Tier,
            request.ActivationCost,
            request.DefaultLimitPerGame,
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
            request.SortOrder
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
            request.SortOrder
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
            snapshot.EnabledModifierCodes.ToArray(),
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
            snapshot.EnabledModifierCodes.ToArray(),
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
            definition.Code,
            definition.Kind,
            definition.Category,
            definition.ScoringType,
            definition.Tier,
            definition.Name,
            definition.Description,
            definition.ActivationCost,
            definition.DefaultLimitPerGame,
            definition.IconEmoji,
            definition.ActivationCommand
        );
    }

    public static GameModifierActivationDto ToDto(this GameModifierActivation activation)
    {
        return new GameModifierActivationDto(
            activation.ModifierCode,
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
        return new GameQuestionCategoryItemDto(item.Id.ToString(), item.Name, item.QuestionCount);
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
        return new UserGameModifierActivationHistoryItemDto(item.ModifierCode, item.ActivatedAtUtc);
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
