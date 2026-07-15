using backend.Application.Abstractions.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace backend.Infrastructure.Realtime;

[Authorize(Roles = AuthRoleCodes.Admin)]
public sealed class GameSetupHub : Hub
{
    private readonly ILogger<GameSetupHub> _logger;

    public GameSetupHub(ILogger<GameSetupHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, RealtimeGroupNames.GameSetupAudience);
        _logger.LogDebug("Game setup hub client connected. ConnectionId: {ConnectionId}.", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception is null)
        {
            _logger.LogDebug(
                "Game setup hub client disconnected. ConnectionId: {ConnectionId}.",
                Context.ConnectionId
            );
        }
        else
        {
            _logger.LogDebug(
                exception,
                "Game setup hub client disconnected with error. ConnectionId: {ConnectionId}.",
                Context.ConnectionId
            );
        }

        await base.OnDisconnectedAsync(exception);
    }
}
