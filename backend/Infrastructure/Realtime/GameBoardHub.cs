using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace backend.Infrastructure.Realtime;

[Authorize]
public sealed class GameBoardHub : Hub
{
}
