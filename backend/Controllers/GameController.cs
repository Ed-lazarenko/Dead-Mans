using backend.Application.Abstractions;
using backend.Application.Abstractions.Auth;
using backend.Api.Contracts;
using backend.Api.Http;
using backend.Api.Mapping;
using backend.Messaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;
[ApiController]
[Route("api/game")]
[Authorize]
public sealed class GameController : ControllerBase
{
    private readonly IGameBoardService _gameBoardService;
    private readonly ILogger<GameController> _logger;

    public GameController(IGameBoardService gameBoardService, ILogger<GameController> logger)
    {
        _gameBoardService = gameBoardService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(GameBoardSnapshotDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var board = await _gameBoardService.GetCurrentBoardAsync(cancellationToken);
        if (board is null)
        {
            _logger.LogInformation(AppMessages.Logs.GameNoBoardForGet);
            return this.NotFoundError(
                AppMessages.Client.NoCurrentGameBoard,
                AppMessages.ErrorCodes.GameBoardNotFound
            );
        }

        return Ok(board.ToDto());
    }

    [HttpGet("team-queue")]
    [ProducesResponseType(typeof(IReadOnlyList<GameTeamQueueItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetTeamQueue(CancellationToken cancellationToken)
    {
        var teamQueue = await _gameBoardService.GetCurrentTeamQueueAsync(cancellationToken);
        return Ok(teamQueue.Select(x => x.ToDto()).ToArray());
    }

    [HttpPut("active-team")]
    [Authorize(Roles = AuthRoleCodes.ModeratorOrAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SetActiveTeam(
        [FromBody] SetActiveGameTeamRequestDto request,
        CancellationToken cancellationToken
    )
    {
        Guid? teamId = null;
        if (!string.IsNullOrWhiteSpace(request.TeamId))
        {
            if (!Guid.TryParse(request.TeamId, out var parsedTeamId))
            {
                return this.BadRequestError(
                    AppMessages.Client.GameActiveTeamNotFound,
                    AppMessages.ErrorCodes.GameBoardActiveTeamNotFound
                );
            }

            teamId = parsedTeamId;
        }

        var result = await _gameBoardService.SetCurrentActiveTeamAsync(teamId, cancellationToken);
        return result switch
        {
            Application.Contracts.SetActiveGameTeamOutcome.Updated => NoContent(),
            Application.Contracts.SetActiveGameTeamOutcome.NoActiveGame => this.NotFoundError(
                AppMessages.Client.GameActiveTeamNoActiveGame,
                AppMessages.ErrorCodes.GameBoardActiveTeamNoActiveGame
            ),
            Application.Contracts.SetActiveGameTeamOutcome.TeamNotFound => this.NotFoundError(
                AppMessages.Client.GameActiveTeamNotFound,
                AppMessages.ErrorCodes.GameBoardActiveTeamNotFound
            ),
            Application.Contracts.SetActiveGameTeamOutcome.TeamNotConfirmed => this.ConflictError(
                AppMessages.Client.GameActiveTeamNotConfirmed,
                AppMessages.ErrorCodes.GameBoardActiveTeamNotConfirmed
            ),
            Application.Contracts.SetActiveGameTeamOutcome.TeamAlreadyPlayed => this.ConflictError(
                AppMessages.Client.GameActiveTeamAlreadyPlayed,
                AppMessages.ErrorCodes.GameBoardActiveTeamAlreadyPlayed
            ),
            Application.Contracts.SetActiveGameTeamOutcome.TeamHasNoActiveMembers => this.ConflictError(
                AppMessages.Client.GameActiveTeamHasNoActiveMembers,
                AppMessages.ErrorCodes.GameBoardActiveTeamHasNoActiveMembers
            ),
            Application.Contracts.SetActiveGameTeamOutcome.RoundInProgress => this.ConflictError(
                AppMessages.Client.GameActiveTeamRoundInProgress,
                AppMessages.ErrorCodes.GameBoardActiveTeamRoundInProgress
            ),
            _ => this.BadRequestError(
                AppMessages.Client.UnableToOpenGameCell,
                AppMessages.ErrorCodes.GameLifecycleOperationFailed
            )
        };
    }

    [HttpPut("teams/{teamId:guid}/played-state")]
    [Authorize(Roles = AuthRoleCodes.ModeratorOrAdmin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SetTeamPlayedState(
        Guid teamId,
        [FromBody] SetGameTeamPlayedStateRequestDto request,
        CancellationToken cancellationToken
    )
    {
        var result = await _gameBoardService.SetGameTeamPlayedStateAsync(
            teamId,
            request.IsPlayed,
            cancellationToken
        );

        return result switch
        {
            Application.Contracts.SetGameTeamPlayedStateOutcome.Updated => NoContent(),
            Application.Contracts.SetGameTeamPlayedStateOutcome.NoActiveGame => this.NotFoundError(
                AppMessages.Client.GameTeamPlayedStateNoActiveGame,
                AppMessages.ErrorCodes.GameBoardTeamPlayedStateNoActiveGame
            ),
            Application.Contracts.SetGameTeamPlayedStateOutcome.TeamNotFound => this.NotFoundError(
                AppMessages.Client.GameTeamPlayedStateNotFound,
                AppMessages.ErrorCodes.GameBoardTeamPlayedStateNotFound
            ),
            Application.Contracts.SetGameTeamPlayedStateOutcome.TeamNotConfirmed => this.ConflictError(
                AppMessages.Client.GameTeamPlayedStateNotConfirmed,
                AppMessages.ErrorCodes.GameBoardTeamPlayedStateNotConfirmed
            ),
            Application.Contracts.SetGameTeamPlayedStateOutcome.RoundInProgress => this.ConflictError(
                AppMessages.Client.GameTeamPlayedStateRoundInProgress,
                AppMessages.ErrorCodes.GameBoardTeamPlayedStateRoundInProgress
            ),
            _ => this.BadRequestError(
                AppMessages.Client.UnableToOpenGameCell,
                AppMessages.ErrorCodes.GameLifecycleOperationFailed
            )
        };
    }

    [HttpPost("cells/{cellId:guid}/open")]
    [Authorize(Roles = AuthRoleCodes.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> OpenCell(Guid cellId, CancellationToken cancellationToken)
    {
        if (!await _gameBoardService.CurrentActiveGameHasSelectedTeamAsync(cancellationToken))
        {
            if (await _gameBoardService.IsCurrentActiveGameCellAsync(cellId, cancellationToken))
            {
                return this.ConflictError(
                    AppMessages.Client.GameActiveTeamRequired,
                    AppMessages.ErrorCodes.GameBoardActiveTeamRequired
                );
            }
        }
        else if (await _gameBoardService.CurrentActiveGameHasActiveRoundAsync(cancellationToken)
            && await _gameBoardService.IsCurrentActiveGameCellAsync(cellId, cancellationToken))
        {
            return this.ConflictError(
                AppMessages.Client.GameCardRunAlreadyInProgress,
                AppMessages.ErrorCodes.GameCardRunAlreadyInProgress
            );
        }

        var openResult = await _gameBoardService.TryOpenCellAsync(cellId, cancellationToken);
        if (openResult is null)
        {
            return this.NotFoundError(
                AppMessages.Client.GameCellNotFound,
                AppMessages.ErrorCodes.GameBoardCellNotFound
            );
        }

        return NoContent();
    }
}
