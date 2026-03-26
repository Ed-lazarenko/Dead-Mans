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
    private readonly ILogger<LeaderboardController> _logger;

    public LeaderboardController(ILeaderboardService leaderboardService, ILogger<LeaderboardController> logger)
    {
        _leaderboardService = leaderboardService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<LeaderboardSummaryDto>> Get(CancellationToken cancellationToken)
    {
        try
        {
            var summary = await _leaderboardService.GetLeaderboardAsync(cancellationToken);
            return Ok(summary.ToDto());
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Leaderboard load failed (configuration or domain rule).");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error loading leaderboard.");
            throw;
        }
    }
}
