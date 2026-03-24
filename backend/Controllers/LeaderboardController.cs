using backend.Application.Abstractions;
using backend.Api.Contracts;
using backend.Api.Mapping;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/leaderboard")]
public sealed class LeaderboardController : ControllerBase
{
    private readonly ILeaderboardService _leaderboardService;

    public LeaderboardController(ILeaderboardService leaderboardService)
    {
        _leaderboardService = leaderboardService;
    }

    [HttpGet]
    public async Task<ActionResult<LeaderboardSummaryDto>> Get(CancellationToken cancellationToken)
    {
        var summary = await _leaderboardService.GetLeaderboardAsync(cancellationToken);
        return Ok(summary.ToDto());
    }
}
