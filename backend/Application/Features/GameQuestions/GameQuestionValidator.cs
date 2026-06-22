using backend.Application.Contracts;

namespace backend.Application.Features.GameQuestions;

internal static class GameQuestionValidator
{
    public const int MaxExternalCodeLength = 64;
    public const int MaxCategoryLength = 64;
    public const int MaxTextLength = 2000;
    public const int MaxAnswerLength = 500;

    public static bool TryNormalizeCreate(
        CreateGameQuestionInput input,
        out CreateGameQuestionInput normalized
    )
    {
        normalized = input;

        var category = (input.Category ?? string.Empty).Trim();
        var text = (input.Text ?? string.Empty).Trim();
        var answer = (input.Answer ?? string.Empty).Trim();
        var externalCode = (input.ExternalCode ?? string.Empty).Trim();

        if (!IsSharedValid(category, text, answer, input.Reward, input.SortOrder)
            || externalCode.Length > MaxExternalCodeLength)
        {
            return false;
        }

        normalized = new CreateGameQuestionInput(
            externalCode.Length == 0 ? null : externalCode,
            category,
            text,
            answer,
            input.Reward,
            input.IsEnabled,
            Math.Max(0, input.SortOrder)
        );
        return true;
    }

    public static bool TryNormalizeUpdate(
        UpdateGameQuestionInput input,
        out UpdateGameQuestionInput normalized
    )
    {
        normalized = input;

        var category = (input.Category ?? string.Empty).Trim();
        var text = (input.Text ?? string.Empty).Trim();
        var answer = (input.Answer ?? string.Empty).Trim();

        if (!IsSharedValid(category, text, answer, input.Reward, input.SortOrder))
        {
            return false;
        }

        normalized = new UpdateGameQuestionInput(
            category,
            text,
            answer,
            input.Reward,
            input.IsEnabled,
            Math.Max(0, input.SortOrder)
        );
        return true;
    }

    private static bool IsSharedValid(
        string category,
        string text,
        string answer,
        int reward,
        int sortOrder
    )
    {
        return category.Length is > 0 and <= MaxCategoryLength
            && text.Length is > 0 and <= MaxTextLength
            && answer.Length is > 0 and <= MaxAnswerLength
            && reward >= 0
            && sortOrder >= 0;
    }
}
