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

        var text = (input.Text ?? string.Empty).Trim();
        var answer = (input.Answer ?? string.Empty).Trim();
        var externalCode = (input.ExternalCode ?? string.Empty).Trim();

        if (input.CategoryId == Guid.Empty
            || !IsSharedValid(text, answer, input.Reward)
            || externalCode.Length > MaxExternalCodeLength)
        {
            return false;
        }

        normalized = new CreateGameQuestionInput(
            externalCode.Length == 0 ? null : externalCode,
            input.CategoryId,
            text,
            answer,
            input.Reward,
            input.IsEnabled,
            input.Priority
        );
        return true;
    }

    public static bool TryNormalizeUpdate(
        UpdateGameQuestionInput input,
        out UpdateGameQuestionInput normalized
    )
    {
        normalized = input;

        var text = (input.Text ?? string.Empty).Trim();
        var answer = (input.Answer ?? string.Empty).Trim();

        if (input.CategoryId == Guid.Empty
            || !IsSharedValid(text, answer, input.Reward))
        {
            return false;
        }

        normalized = new UpdateGameQuestionInput(
            input.CategoryId,
            text,
            answer,
            input.Reward,
            input.IsEnabled,
            input.Priority
        );
        return true;
    }

    private static bool IsSharedValid(string text, string answer, int reward)
    {
        return text.Length is > 0 and <= MaxTextLength
            && answer.Length is > 0 and <= MaxAnswerLength
            && reward >= 0;
    }
}
