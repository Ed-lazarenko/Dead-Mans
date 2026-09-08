namespace backend.Application.Features.GameQuestions;

public static class GameQuestionImportLimits
{
    public const long MaxUploadBytes = 5 * 1024 * 1024;

    public const int MaxQuestionCount = 10_000;
}
