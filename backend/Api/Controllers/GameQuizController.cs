using backend.Api.Contracts;
using backend.Api.Http;
using backend.Api.Mapping;
using backend.Application.Abstractions;
using backend.Application.Abstractions.Auth;
using backend.Application.Contracts;
using backend.Messaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/game/quiz")]
[Authorize]
public sealed class GameQuizController : ControllerBase
{
    private readonly IGameQuizService _gameQuizService;

    public GameQuizController(IGameQuizService gameQuizService)
    {
        _gameQuizService = gameQuizService;
    }

    [HttpPost("questions/ask-next")]
    [Authorize(Roles = AuthRoleCodes.ModeratorOrAdmin)]
    [ProducesResponseType(typeof(AskedQuizQuestionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AskNextQuestion(CancellationToken cancellationToken)
    {
        var askedByUserId = HttpContext.TryGetUserId();
        if (!askedByUserId.HasValue)
        {
            return this.BadRequestError(AppMessages.Client.AuthCookieMissingClaims);
        }

        var result = await _gameQuizService.AskNextQuizQuestionAsync(
            new ManualGameQuizQuestionDelivery(askedByUserId.Value),
            cancellationToken
        );
        return result.Outcome switch
        {
            AskNextGameQuizQuestionOutcome.Asked when result.AskedQuestion is not null =>
                Ok(result.AskedQuestion.ToDto()),
            AskNextGameQuizQuestionOutcome.NoActiveGame => this.NotFoundError(
                AppMessages.Client.GameQuizNoActiveGame,
                AppMessages.ErrorCodes.GameQuizNoActiveGame
            ),
            AskNextGameQuizQuestionOutcome.NoAvailableQuestions => this.NotFoundError(
                AppMessages.Client.GameQuizNoAvailableQuestions,
                AppMessages.ErrorCodes.GameQuizNoAvailableQuestions
            ),
            _ => this.StatusError(
                StatusCodes.Status500InternalServerError,
                AppMessages.Client.UnexpectedServerError
            )
        };
    }

    [HttpPost("rounds/{roundId:guid}/answer")]
    [Authorize(Roles = AuthRoleCodes.ModeratorOrAdmin)]
    [ProducesResponseType(typeof(GameQuizRoundSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AnswerRound(
        Guid roundId,
        [FromBody] AnswerQuizRoundRequestDto? request,
        CancellationToken cancellationToken
    )
    {
        if (request is null)
        {
            return this.BadRequestError(
                AppMessages.Client.GameQuestionInvalidRequest,
                AppMessages.ErrorCodes.GameQuestionInvalidRequest
            );
        }

        var answeredByUserId = HttpContext.TryGetUserId();
        if (!answeredByUserId.HasValue)
        {
            return this.BadRequestError(AppMessages.Client.AuthCookieMissingClaims);
        }

        Guid? answeredForUserId = null;
        if (!string.IsNullOrWhiteSpace(request.AnsweredForUserId))
        {
            if (!Guid.TryParse(request.AnsweredForUserId, out var parsedAnsweredForUserId))
            {
                return this.BadRequestError(
                    AppMessages.Client.GameQuestionInvalidRequest,
                    AppMessages.ErrorCodes.GameQuestionInvalidRequest
                );
            }

            answeredForUserId = parsedAnsweredForUserId;
        }

        var result = await _gameQuizService.AnswerQuizRoundAsync(
            roundId,
            new SubmitGameQuizAnswerInput(
                request.Answer,
                new ManualGameQuizAnswerSource(
                    answeredByUserId.Value,
                    answeredForUserId ?? answeredByUserId.Value,
                    request.AnsweredByDisplayName
                )
            ),
            cancellationToken
        );

        return result.Outcome switch
        {
            AnswerGameQuizRoundOutcome.Answered when result.QuizRound is not null =>
                Ok(result.QuizRound.ToDto()),
            AnswerGameQuizRoundOutcome.Incorrect when result.QuizRound is not null =>
                Ok(result.QuizRound.ToDto()),
            AnswerGameQuizRoundOutcome.InvalidAnswer => this.BadRequestError(
                AppMessages.Client.GameQuestionInvalidRequest,
                AppMessages.ErrorCodes.GameQuestionInvalidRequest
            ),
            AnswerGameQuizRoundOutcome.InvalidSource => this.BadRequestError(
                AppMessages.Client.GameQuestionInvalidRequest,
                AppMessages.ErrorCodes.GameQuestionInvalidRequest
            ),
            AnswerGameQuizRoundOutcome.QuizRoundNotFound => this.NotFoundError(
                AppMessages.Client.GameQuizRoundNotFound,
                AppMessages.ErrorCodes.GameQuizRoundNotFound
            ),
            AnswerGameQuizRoundOutcome.PlayerNotFound => this.NotFoundError(
                AppMessages.Client.GameQuizAnswerPlayerNotFound,
                AppMessages.ErrorCodes.GameQuizAnswerPlayerNotFound
            ),
            AnswerGameQuizRoundOutcome.QuizRoundNotPending => this.ConflictError(
                AppMessages.Client.GameQuizRoundNotPending,
                AppMessages.ErrorCodes.GameQuizRoundNotPending
            ),
            _ => this.StatusError(
                StatusCodes.Status500InternalServerError,
                AppMessages.Client.UnexpectedServerError
            )
        };
    }

    [HttpGet("manual-awards/players")]
    [Authorize(Roles = AuthRoleCodes.ModeratorOrAdmin)]
    [ProducesResponseType(typeof(IReadOnlyList<ManualQuizAwardPlayerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetManualAwardPlayers(CancellationToken cancellationToken)
    {
        var players = await _gameQuizService.GetManualQuizAwardPlayersAsync(cancellationToken);
        return Ok(players.Select(player => player.ToDto()).ToArray());
    }

    [HttpPost("manual-awards")]
    [Authorize(Roles = AuthRoleCodes.ModeratorOrAdmin)]
    [ProducesResponseType(typeof(ManualQuizAwardSummaryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AwardManualPoints(
        [FromBody] ManualQuizAwardRequestDto? request,
        CancellationToken cancellationToken
    )
    {
        var awardedByUserId = HttpContext.TryGetUserId();
        if (!awardedByUserId.HasValue)
        {
            return this.BadRequestError(AppMessages.Client.AuthCookieMissingClaims);
        }

        if (
            request is null
            || string.IsNullOrWhiteSpace(request.AwardedToUserId)
            || !Guid.TryParse(request.AwardedToUserId, out var awardedToUserId)
            || string.IsNullOrWhiteSpace(request.OperationType)
            || string.IsNullOrWhiteSpace(request.Reason)
            || string.IsNullOrWhiteSpace(request.RequestId)
            || !Guid.TryParse(request.RequestId, out var requestId)
        )
        {
            return this.BadRequestError(
                AppMessages.Client.GameQuestionInvalidRequest,
                AppMessages.ErrorCodes.GameQuestionInvalidRequest
            );
        }

        var result = await _gameQuizService.AwardManualQuizPointsAsync(
            new ManualQuizAwardInput(
                awardedToUserId,
                request.OperationType.Trim().ToLowerInvariant(),
                request.Points,
                request.Reason.Trim(),
                requestId
            ),
            awardedByUserId.Value,
            cancellationToken
        );

        return result.Outcome switch
        {
            ManualQuizAwardOutcome.Awarded when result.Award is not null =>
                StatusCode(StatusCodes.Status201Created, result.Award.ToDto()),
            ManualQuizAwardOutcome.NoActiveGame => this.NotFoundError(
                AppMessages.Client.GameQuizNoActiveGame,
                AppMessages.ErrorCodes.GameQuizNoActiveGame
            ),
            ManualQuizAwardOutcome.PlayerNotFound => this.NotFoundError(
                AppMessages.Client.GameQuizManualAwardPlayerNotFound,
                AppMessages.ErrorCodes.GameQuizManualAwardPlayerNotFound
            ),
            ManualQuizAwardOutcome.InvalidPoints => this.BadRequestError(
                AppMessages.Client.GameQuizManualAwardInvalidPoints,
                AppMessages.ErrorCodes.GameQuizManualAwardInvalidPoints
            ),
            ManualQuizAwardOutcome.InvalidOperation => this.BadRequestError(
                AppMessages.Client.GameQuizManualAwardInvalidOperation,
                AppMessages.ErrorCodes.GameQuizManualAwardInvalidOperation
            ),
            ManualQuizAwardOutcome.InvalidReason => this.BadRequestError(
                AppMessages.Client.GameQuizManualAwardInvalidReason,
                AppMessages.ErrorCodes.GameQuizManualAwardInvalidReason
            ),
            ManualQuizAwardOutcome.InsufficientPoints => this.StatusError(
                StatusCodes.Status409Conflict,
                AppMessages.Client.GameQuizManualAwardInsufficientPoints,
                AppMessages.ErrorCodes.GameQuizManualAwardInsufficientPoints
            ),
            ManualQuizAwardOutcome.DuplicateRequestConflict => this.StatusError(
                StatusCodes.Status409Conflict,
                AppMessages.Client.GameQuizManualAwardDuplicateRequestConflict,
                AppMessages.ErrorCodes.GameQuizManualAwardDuplicateRequestConflict
            ),
            _ => this.StatusError(
                StatusCodes.Status500InternalServerError,
                AppMessages.Client.UnexpectedServerError
            )
        };
    }
}
