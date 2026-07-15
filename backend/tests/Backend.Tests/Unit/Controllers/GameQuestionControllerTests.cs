using backend.Controllers;
using backend.Application.Abstractions;
using backend.Application.Contracts;
using backend.Application.Features.GameQuestions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Tests.Unit.Controllers;

public sealed class GameQuestionControllerTests
{
    [Fact]
    public async Task ImportQuestions_WhenFileExceedsLimit_ReturnsBadRequestWithoutCallingService()
    {
        var service = new TrackingGameQuestionService();
        var controller = CreateController(service);
        var file = CreateFile(GameQuestionImportLimits.MaxUploadBytes + 1);

        var result = await controller.ImportQuestions(file, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
        Assert.False(service.ImportQuestionsCalled);
    }

    private static GameQuestionController CreateController(IGameQuestionService service)
    {
        var controller = new GameQuestionController(service)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        return controller;
    }

    private static IFormFile CreateFile(long length)
    {
        var stream = new MemoryStream(new byte[Math.Min(length, 16)]);
        return new FormFile(stream, 0, length, "file", "questions.jsonc")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/json",
        };
    }

    private sealed class TrackingGameQuestionService : IGameQuestionService
    {
        public bool ImportQuestionsCalled { get; private set; }

        public Task<IReadOnlyList<GameQuestionCatalogItem>> GetCatalogAsync(Guid? categoryId, string? search, bool includeDisabled, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<GameQuestionCategoryItem>> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<GameQuestionCategoryItem> EnsureFallbackCategoryAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<CreateGameQuestionCategoryResult> CreateCategoryAsync(string categoryName, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<DeleteGameQuestionCategoryResult> DeleteCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<UpdateGameQuestionCategoryResult> UpdateCategoryAsync(Guid categoryId, string categoryName, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<CreateGameQuestionResult> CreateQuestionAsync(CreateGameQuestionInput input, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<UpdateGameQuestionResult> UpdateQuestionAsync(Guid questionId, UpdateGameQuestionInput input, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<ImportGameQuestionsResult> ImportQuestionsAsync(IReadOnlyList<ImportGameQuestionInput> inputs, CancellationToken cancellationToken = default)
        {
            ImportQuestionsCalled = true;
            throw new NotSupportedException();
        }

        public Task<bool> SetQuestionEnabledAsync(Guid questionId, bool isEnabled, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> SoftDeleteQuestionAsync(Guid questionId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> SetCategoryEnabledAsync(Guid categoryId, bool isEnabled, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<AskNextGameQuestionResult> AskNextAsync(Guid? askedByUserId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<AnswerGameQuestionResult> AnswerRoundAsync(Guid roundId, string submittedAnswer, Guid? answeredByUserId, Guid? answeredForUserId, string? answeredByDisplayName, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<GameQuestionRoundSummary>> GetGameHistoryAsync(Guid gameId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
