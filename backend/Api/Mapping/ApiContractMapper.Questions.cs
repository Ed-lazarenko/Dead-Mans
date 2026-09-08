using backend.Api.Contracts;
using backend.Application.Abstractions.Auth;
using backend.Application.Contracts;
using backend.Domain.GameModifiers;

namespace backend.Api.Mapping;

public static partial class ApiContractMapper
{
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
            question.AskedAtUtc,
            question.ClosesAtUtc
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
            round.ClosesAtUtc,
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

}
