using backend.Controllers;
using backend.Application.Abstractions;
using backend.Application.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ApiContracts = backend.Api.Contracts;

namespace Backend.Tests.Unit.Controllers;

public sealed class GameQuizControllerTests
{
    [Fact]
    public async Task AwardManualPoints_WhenValid_ReturnsCreatedAndPassesModeratorId()
    {
        var awardedToUserId = Guid.NewGuid();
        var awardedByUserId = Guid.NewGuid();
        var awardedAtUtc = DateTime.UtcNow;
        var service = new TrackingGameQuestionService
        {
            ManualQuizAwardResult = new ManualQuizAwardResult(
                ManualQuizAwardOutcome.Awarded,
                new ManualQuizAwardSummary(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    awardedToUserId,
                    "Player One",
                    awardedByUserId,
                    "Moderator One",
                    5,
                    awardedAtUtc
                )
            )
        };
        var controller = CreateController(service, awardedByUserId);

        var result = await controller.AwardManualPoints(
            new ApiContracts.ManualQuizAwardRequestDto(awardedToUserId.ToString(), 5),
            CancellationToken.None
        );

        var created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
        var dto = Assert.IsType<ApiContracts.ManualQuizAwardSummaryDto>(created.Value);
        Assert.Equal(awardedToUserId.ToString(), dto.AwardedToUserId);
        Assert.Equal(awardedByUserId.ToString(), dto.AwardedByUserId);
        Assert.Equal(5, dto.Points);
        Assert.True(service.AwardManualQuizPointsCalled);
        Assert.Equal(awardedByUserId, service.LastAwardedByUserId);
        Assert.Equal(awardedToUserId, service.LastManualQuizAwardInput?.AwardedToUserId);
        Assert.Equal(5, service.LastManualQuizAwardInput?.Points);
    }

    [Fact]
    public async Task AwardManualPoints_WhenModeratorClaimMissing_ReturnsBadRequestWithoutCallingService()
    {
        var service = new TrackingGameQuestionService();
        var controller = CreateController(service);

        var result = await controller.AwardManualPoints(
            new ApiContracts.ManualQuizAwardRequestDto(Guid.NewGuid().ToString(), 5),
            CancellationToken.None
        );

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
        Assert.False(service.AwardManualQuizPointsCalled);
    }

    private static GameQuizController CreateController(
        IGameQuestionService service,
        Guid? userId = null
    )
    {
        var httpContext = new DefaultHttpContext();
        if (userId.HasValue)
        {
            httpContext.User = new ClaimsPrincipal(
                new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString())],
                    "Test"
                )
            );
        }

        return new GameQuizController(service)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
    }

    private sealed class TrackingGameQuestionService : IGameQuestionService
    {
        public bool AwardManualQuizPointsCalled { get; private set; }
        public ManualQuizAwardInput? LastManualQuizAwardInput { get; private set; }
        public Guid? LastAwardedByUserId { get; private set; }
        public ManualQuizAwardResult ManualQuizAwardResult { get; init; } =
            new(ManualQuizAwardOutcome.InvalidPoints);

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

        public Task<ImportGameQuestionsResult> ImportQuestionsAsync(IReadOnlyList<ImportGameQuestionInput> inputs, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> SetQuestionEnabledAsync(Guid questionId, bool isEnabled, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> SoftDeleteQuestionAsync(Guid questionId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> SetCategoryEnabledAsync(Guid categoryId, bool isEnabled, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<AskNextGameQuizQuestionResult> AskNextQuizQuestionAsync(Guid? askedByUserId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<AnswerGameQuestionResult> AnswerQuizRoundAsync(Guid roundId, string submittedAnswer, Guid? answeredByUserId, Guid? answeredForUserId, string? answeredByDisplayName, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<ManualQuizAwardResult> AwardManualQuizPointsAsync(ManualQuizAwardInput input, Guid awardedByUserId, CancellationToken cancellationToken = default)
        {
            AwardManualQuizPointsCalled = true;
            LastManualQuizAwardInput = input;
            LastAwardedByUserId = awardedByUserId;
            return Task.FromResult(ManualQuizAwardResult);
        }

        public Task<IReadOnlyList<ManualQuizAwardPlayer>> GetManualQuizAwardPlayersAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
