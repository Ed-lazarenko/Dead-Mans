using backend.Api.Contracts;
using backend.Api.Http;
using backend.Api.Mapping;
using backend.Application.Abstractions;
using backend.Application.Abstractions.Auth;
using backend.Messaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/game/rounds")]
[Authorize]
public sealed partial class GameRoundController : ControllerBase
{
    private readonly IGameRoundService _service;

    public GameRoundController(IGameRoundService service)
    {
        _service = service;
    }
}
