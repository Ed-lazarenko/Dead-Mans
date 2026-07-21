using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace backend.Infrastructure.Realtime;

[Authorize]
public sealed class GameBoardHub : Hub
{
    private readonly ILogger<GameBoardHub> _logger;

    public GameBoardHub(ILogger<GameBoardHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, RealtimeGroupNames.GameBoardAudience);
        if (TryGetUserId(out var userId))
        {
            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                RealtimeGroupNames.GameBoardUserAudience(userId)
            );
        }

        _logger.LogDebug("Game board hub client connected. ConnectionId: {ConnectionId}.", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception is null)
        {
            _logger.LogDebug(
                "Game board hub client disconnected. ConnectionId: {ConnectionId}.",
                Context.ConnectionId
            );
        }
        else
        {
            _logger.LogDebug(
                exception,
                "Game board hub client disconnected with error. ConnectionId: {ConnectionId}.",
                Context.ConnectionId
            );
        }

        await base.OnDisconnectedAsync(exception);
    }

    private bool TryGetUserId(out Guid userId)
    {
        var rawUserId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(rawUserId, out userId);
    }
}
