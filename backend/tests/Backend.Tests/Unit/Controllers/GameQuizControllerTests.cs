using backend.Controllers;
using backend.Application.Abstractions;
using backend.Application.Contracts;
using backend.Domain.Persistence;
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
        var service = new TrackingGameQuizService
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
                    GameQuizManualAdjustmentOperationValue.Award,
                    5,
                    "Manual correction",
                    10,
                    15,
                    Guid.NewGuid(),
                    awardedAtUtc
                )
            )
        };
        var controller = CreateController(service, awardedByUserId);

        var result = await controller.AwardManualPoints(
            new ApiContracts.ManualQuizAwardRequestDto(
                awardedToUserId.ToString(),
                GameQuizManualAdjustmentOperationValue.Award,
                5,
                "Manual correction",
                Guid.NewGuid().ToString()
            ),
            CancellationToken.None
        );

        var created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
        var dto = Assert.IsType<ApiContracts.ManualQuizAwardSummaryDto>(created.Value);
        Assert.Equal(awardedToUserId.ToString(), dto.AwardedToUserId);
        Assert.Equal(awardedByUserId.ToString(), dto.AwardedByUserId);
        Assert.Equal(5, dto.PointsDelta);
        Assert.True(service.AwardManualQuizPointsCalled);
        Assert.Equal(awardedByUserId, service.LastAwardedByUserId);
        Assert.Equal(awardedToUserId, service.LastManualQuizAwardInput?.AwardedToUserId);
        Assert.Equal(5, service.LastManualQuizAwardInput?.Points);
    }

    [Fact]
    public async Task AwardManualPoints_WhenModeratorClaimMissing_ReturnsBadRequestWithoutCallingService()
    {
        var service = new TrackingGameQuizService();
        var controller = CreateController(service);

        var result = await controller.AwardManualPoints(
            new ApiContracts.ManualQuizAwardRequestDto(
                Guid.NewGuid().ToString(),
                GameQuizManualAdjustmentOperationValue.Award,
                5,
                "Manual correction",
                Guid.NewGuid().ToString()
            ),
            CancellationToken.None
        );

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
        Assert.False(service.AwardManualQuizPointsCalled);
    }

    private static GameQuizController CreateController(
        IGameQuizService service,
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

    private sealed class TrackingGameQuizService : IGameQuizService
    {
        public bool AwardManualQuizPointsCalled { get; private set; }
        public ManualQuizAwardInput? LastManualQuizAwardInput { get; private set; }
        public Guid? LastAwardedByUserId { get; private set; }
        public ManualQuizAwardResult ManualQuizAwardResult { get; init; } =
            new(ManualQuizAwardOutcome.InvalidPoints);

        public Task<AskNextGameQuizQuestionResult> AskNextQuizQuestionAsync(Guid? askedByUserId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<AnswerGameQuizRoundResult> AnswerQuizRoundAsync(Guid roundId, string submittedAnswer, Guid? answeredByUserId, Guid? answeredForUserId, string? answeredByDisplayName, CancellationToken cancellationToken = default) =>
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
