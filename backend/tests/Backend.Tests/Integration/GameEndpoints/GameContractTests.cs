using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using backend.Api.Contracts;
using backend.Api.Realtime;
using backend.Application.Abstractions;
using backend.Application.Abstractions.Auth;
using backend.Application.Abstractions.Realtime;
using backend.Application.Contracts;
using backend.Application.Abstractions.Repositories;
using backend.Application.Features.GameQuestions;
using backend.Data;
using backend.Data.Entities;
using backend.Domain.GameModifiers;
using backend.Domain.Persistence;
using backend.Messaging;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Backend.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Backend.Tests.Integration.GameEndpoints;

public sealed class GameContractTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public GameContractTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetGame_WhenAnonymous_ReturnsJsonUnauthorizedError()
    {
        var response = await _client.GetAsync("/api/game");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType?.ToString());

        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.Client.AuthenticationRequired, payload.Error);
    }

    [Fact]
    public async Task GetGame_WhenAnonymous_ReturnsSecurityHeaders()
    {
        var response = await _client.GetAsync("/api/game");

        Assert.Equal("nosniff", Assert.Single(response.Headers.GetValues("X-Content-Type-Options")));
        Assert.Equal("DENY", Assert.Single(response.Headers.GetValues("X-Frame-Options")));
        Assert.Equal("no-referrer", Assert.Single(response.Headers.GetValues("Referrer-Policy")));
        Assert.Equal(
            "default-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'self'",
            Assert.Single(response.Headers.GetValues("Content-Security-Policy"))
        );
    }

    [Fact]
    public async Task CorsPreflight_WhenOriginAllowed_ReturnsAllowOriginHeader()
    {
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/game");
        request.Headers.Add("Origin", "http://localhost:5180");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("http://localhost:5180", Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
        Assert.Contains("Origin", response.Headers.Vary.SelectMany(value => value.Split(',')));
    }

    [Fact]
    public async Task CorsPreflight_WhenOriginDisallowed_DoesNotReturnAllowOriginHeader()
    {
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/game");
        request.Headers.Add("Origin", "https://evil.example");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task GetGame_WhenAuthenticatedButInactive_ReturnsUnauthorized()
    {
        var inactiveUserId = Guid.NewGuid();
        await SeedInactiveUserAsync(inactiveUserId);
        using var authenticatedClient = CreateAuthenticatedClient(
            [AuthRoleCodes.Viewer],
            userId: inactiveUserId
        );

        var response = await authenticatedClient.GetAsync("/api/game");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.Client.UserMissingOrInactive, payload.Error);
    }

    [Fact]
    public async Task GetGame_WhenAuthenticated_ReturnsBoardSnapshot()
    {
        var finishedGameId = await SeedGamesAsync();
        await AssertRepositoryFallbackAsync(finishedGameId);
        using var authenticatedClient = CreateAuthenticatedClient([AuthRoleCodes.Viewer]);

        var response = await authenticatedClient.GetAsync("/api/game");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<GameBoardSnapshotDto>();
        Assert.NotNull(payload);
        Assert.Equal(finishedGameId.ToString(), payload.GameId);
        Assert.Equal(GameStatusValue.Finished, payload.Status);
        Assert.True(payload.Version >= 1);
        Assert.Single(payload.Cells);
    }

    [Fact]
    public async Task Repository_WhenActiveGameHasNoBoard_FallsBackToLatestFinishedGameWithBoard()
    {
        var finishedGameId = await SeedGamesAsync();
        await AssertRepositoryFallbackAsync(finishedGameId);
    }

    [Fact]
    public async Task Repository_WhenNoBoardsExist_ReturnsNull()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var gameBoardService = scope.ServiceProvider.GetRequiredService<IGameBoardService>();

        dbContext.BoardCells.RemoveRange(dbContext.BoardCells);
        dbContext.GameBoards.RemoveRange(dbContext.GameBoards);
        dbContext.Games.RemoveRange(dbContext.Games);
        await dbContext.SaveChangesAsync();

        var snapshot = await gameBoardService.GetCurrentBoardAsync();
        Assert.Null(snapshot);
    }

    [Fact]
    public async Task OpenCell_WhenAuthenticatedButNotAdmin_ReturnsForbidden()
    {
        var cellId = await SeedSingleCellAsync();
        using var authenticatedClient = CreateAuthenticatedClient([AuthRoleCodes.Viewer]);

        var response = await authenticatedClient.PostAsync($"/api/game/cells/{cellId}/open", content: null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task OpenCell_WhenAdmin_OpensCellAndReturnsNoContent()
    {
        var cellId = await SeedSingleCellAsync();
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);

        var response = await adminClient.PostAsync($"/api/game/cells/{cellId}/open", content: null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var cell = await dbContext.BoardCells.FindAsync(cellId);
        Assert.NotNull(cell);
        Assert.Equal(BoardCellState.Open, cell!.State);

        var round = await dbContext.GameRounds
            .Include(x => x.Participants)
            .SingleAsync(x => x.BoardCellId == cellId);
        var activeTeamId = await dbContext.BoardCells
            .Where(x => x.Id == cellId)
            .Select(x => x.Board.Game.ActiveTeamId)
            .SingleAsync();
        Assert.Equal(GameRoundStatusValue.AwaitingModifiers, round.Status);
        Assert.Equal(activeTeamId!.Value, round.TeamId);
        Assert.Single(round.Participants);
    }

    [Fact]
    public async Task OpenCell_WhenAdminAndNoActiveTeamSelected_ReturnsConflict()
    {
        var cellId = await SeedSingleCellAsync(selectActiveTeam: false);
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);

        var response = await adminClient.PostAsync($"/api/game/cells/{cellId}/open", content: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.Client.GameActiveTeamRequired, payload.Error);
        Assert.Equal(AppMessages.ErrorCodes.GameBoardActiveTeamRequired, payload.Code);
    }

    [Fact]
    public async Task SetActiveTeam_WhenModeratorAndConfirmedTeam_UpdatesCurrentGameSnapshot()
    {
        var cellId = await SeedSingleCellAsync(selectActiveTeam: false);
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var teamId = await dbContext.BoardCells
            .Where(cell => cell.Id == cellId)
            .SelectMany(cell => dbContext.GameTeams.Where(team => team.GameId == cell.Board.GameId))
            .Select(team => team.Id)
            .SingleAsync();
        using var moderatorClient = CreateAuthenticatedClient([AuthRoleCodes.Moderator]);

        var response = await moderatorClient.PutAsJsonAsync(
            "/api/game/active-team",
            new SetActiveGameTeamRequestDto(teamId.ToString())
        );

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var snapshotResponse = await moderatorClient.GetAsync("/api/game");
        Assert.Equal(HttpStatusCode.OK, snapshotResponse.StatusCode);
        var snapshot = await snapshotResponse.Content.ReadFromJsonAsync<GameBoardSnapshotDto>();
        Assert.NotNull(snapshot);
        Assert.Equal(teamId.ToString(), snapshot.ActiveTeamId);
    }

    [Theory]
    [InlineData(GameRoundStatusValue.AwaitingModifiers)]
    [InlineData(GameRoundStatusValue.Preparing)]
    public async Task SetActiveTeam_WhenCardHasActiveRound_ReturnsConflictAndKeepsActiveTeam(
        string roundStatus
    )
    {
        var cellId = await SeedSingleCellAsync();
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);
        var openResponse = await adminClient.PostAsync($"/api/game/cells/{cellId}/open", content: null);
        Assert.Equal(HttpStatusCode.NoContent, openResponse.StatusCode);
        Guid originalTeamId;
        Guid otherTeamId;
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var game = await dbContext.BoardCells
                .Where(cell => cell.Id == cellId)
                .Select(cell => cell.Board.Game)
                .SingleAsync();
            if (roundStatus == GameRoundStatusValue.Preparing)
            {
                var round = await dbContext.GameRounds.SingleAsync(
                    candidate => candidate.BoardCellId == cellId
                );
                round.Status = GameRoundStatusValue.Preparing;
                round.PreparedAtUtc = DateTime.UtcNow;
                round.Version += 1;
            }
            Assert.NotNull(game.ActiveTeamId);
            originalTeamId = game.ActiveTeamId.Value;
            otherTeamId = Guid.NewGuid();
            var otherSlotId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            var now = DateTime.UtcNow;
            dbContext.Users.Add(
                new User
                {
                    Id = otherUserId,
                    TwitchUserId = $"locked-team-{otherUserId:N}",
                    Login = $"locked-team-{otherUserId:N}"[..32],
                    DisplayName = "Other team member",
                    IsActive = true,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                }
            );
            dbContext.GameTeamSlots.Add(
                new GameTeamSlot
                {
                    Id = otherSlotId,
                    GameId = game.Id,
                    SlotIndex = 2,
                    SlotType = TeamSlotTypeValue.Public,
                    CreatedAtUtc = now
                }
            );
            dbContext.GameTeams.Add(
                new GameTeam
                {
                    Id = otherTeamId,
                    GameId = game.Id,
                    SlotId = otherSlotId,
                    Status = TeamStatusValue.Confirmed,
                    RecruitmentOpen = false,
                    CreatedByUserId = otherUserId,
                    ConfirmedByUserId = otherUserId,
                    ConfirmedAtUtc = now,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                }
            );
            dbContext.GameTeamMembers.Add(
                new GameTeamMember
                {
                    Id = Guid.NewGuid(),
                    GameId = game.Id,
                    TeamId = otherTeamId,
                    UserId = otherUserId,
                    JoinedAtUtc = now
                }
            );
            await dbContext.SaveChangesAsync();
        }
        using var moderatorClient = CreateAuthenticatedClient([AuthRoleCodes.Moderator]);

        var response = await moderatorClient.PutAsJsonAsync(
            "/api/game/active-team",
            new SetActiveGameTeamRequestDto(otherTeamId.ToString())
        );

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.Client.GameActiveTeamRoundInProgress, payload.Error);
        Assert.Equal(AppMessages.ErrorCodes.GameBoardActiveTeamRoundInProgress, payload.Code);

        var clearResponse = await moderatorClient.PutAsJsonAsync(
            "/api/game/active-team",
            new SetActiveGameTeamRequestDto(null)
        );
        Assert.Equal(HttpStatusCode.Conflict, clearResponse.StatusCode);
        var clearPayload = await clearResponse.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(clearPayload);
        Assert.Equal(AppMessages.ErrorCodes.GameBoardActiveTeamRoundInProgress, clearPayload.Code);

        var playedStateResponse = await moderatorClient.PutAsJsonAsync(
            $"/api/game/teams/{originalTeamId}/played-state",
            new SetGameTeamPlayedStateRequestDto(true)
        );
        Assert.Equal(HttpStatusCode.Conflict, playedStateResponse.StatusCode);
        var playedStatePayload =
            await playedStateResponse.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(playedStatePayload);
        Assert.Equal(
            AppMessages.ErrorCodes.GameBoardTeamPlayedStateRoundInProgress,
            playedStatePayload.Code
        );

        using var verificationScope = _factory.Services.CreateScope();
        var verificationDbContext =
            verificationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var persistedActiveTeamId = await verificationDbContext.BoardCells
            .Where(cell => cell.Id == cellId)
            .Select(cell => cell.Board.Game.ActiveTeamId)
            .SingleAsync();
        Assert.Equal(originalTeamId, persistedActiveTeamId);
        Assert.False(
            await verificationDbContext.GameTeams
                .Where(team => team.Id == originalTeamId)
                .Select(team => team.IsPlayed)
                .SingleAsync()
        );
    }

    [Fact]
    public async Task SetTeamPlayedState_WhenModerator_MarksTeamAndClearsActiveSelection()
    {
        var cellId = await SeedSingleCellAsync();
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var teamId = await dbContext.BoardCells
            .Where(cell => cell.Id == cellId)
            .SelectMany(cell => dbContext.GameTeams.Where(team => team.GameId == cell.Board.GameId))
            .Select(team => team.Id)
            .SingleAsync();
        using var moderatorClient = CreateAuthenticatedClient([AuthRoleCodes.Moderator]);

        var response = await moderatorClient.PutAsJsonAsync(
            $"/api/game/teams/{teamId}/played-state",
            new SetGameTeamPlayedStateRequestDto(true)
        );

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var queueResponse = await moderatorClient.GetAsync("/api/game/team-queue");
        Assert.Equal(HttpStatusCode.OK, queueResponse.StatusCode);
        var queue = await queueResponse.Content.ReadFromJsonAsync<GameTeamQueueResultDto>();
        Assert.NotNull(queue);
        Assert.Equal(1, queue.Summary.TotalTeams);
        Assert.Equal(1, queue.Summary.PlayedTeams);
        Assert.Equal(0, queue.Summary.RemainingTeams);
        var queueItem = Assert.Single(queue.Teams);
        Assert.True(queueItem.IsPlayed);
        Assert.NotNull(queueItem.PlayedAtUtc);

        var snapshotResponse = await moderatorClient.GetAsync("/api/game");
        var snapshot = await snapshotResponse.Content.ReadFromJsonAsync<GameBoardSnapshotDto>();
        Assert.NotNull(snapshot);
        Assert.Null(snapshot.ActiveTeamId);
    }

    [Fact]
    public async Task SetTeamPlayedState_WhenModerator_ReturnsTeamToQueue_KeepsCompletedRound()
    {
        var cellId = await SeedSingleCellAsync(selectActiveTeam: false);
        var roundId = Guid.NewGuid();
        Guid teamId;

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var cell = await dbContext.BoardCells
                .Include(candidate => candidate.Board)
                .SingleAsync(candidate => candidate.Id == cellId);
            var team = await dbContext.GameTeams.SingleAsync(
                candidate => candidate.GameId == cell.Board.GameId
            );
            var resolvedByUserId = await dbContext.GameTeamMembers
                .Where(member => member.TeamId == team.Id)
                .Select(member => member.UserId)
                .SingleAsync();
            var now = DateTime.UtcNow;

            teamId = team.Id;
            team.IsPlayed = true;
            team.PlayedAtUtc = now.AddMinutes(-1);
            team.UpdatedAtUtc = now.AddMinutes(-1);
            cell.State = BoardCellState.Open;
            dbContext.GameRounds.Add(
                new GameRound
                {
                    Id = roundId,
                    GameId = cell.Board.GameId,
                    BoardId = cell.BoardId,
                    BoardCellId = cell.Id,
                    TeamId = team.Id,
                    Status = GameRoundStatusValue.Completed,
                    FinishedAtUtc = now.AddMinutes(-1),
                    BaseScore = 100,
                    FinalScore = 100,
                    TeamSlotIndexSnapshot = 1,
                    CellRowIndex = cell.RowIndex,
                    CellColIndex = cell.ColIndex,
                    CellTitleSnapshot = cell.Title,
                    CellCostSnapshot = cell.Cost,
                    ResolvedByUserId = resolvedByUserId,
                    CreatedAtUtc = now.AddMinutes(-10),
                    UpdatedAtUtc = now.AddMinutes(-1)
                }
            );
            await dbContext.SaveChangesAsync();
        }

        using var moderatorClient = CreateAuthenticatedClient([AuthRoleCodes.Moderator]);
        var response = await moderatorClient.PutAsJsonAsync(
            $"/api/game/teams/{teamId}/played-state",
            new SetGameTeamPlayedStateRequestDto(false)
        );

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var queueResponse = await moderatorClient.GetAsync("/api/game/team-queue");
        Assert.Equal(HttpStatusCode.OK, queueResponse.StatusCode);
        var queue = await queueResponse.Content.ReadFromJsonAsync<GameTeamQueueResultDto>();
        Assert.NotNull(queue);
        Assert.Equal(0, queue.Summary.PlayedTeams);
        Assert.Equal(1, queue.Summary.RemainingTeams);
        var queueItem = Assert.Single(queue.Teams);
        Assert.False(queueItem.IsPlayed);
        Assert.Null(queueItem.PlayedAtUtc);

        using var verificationScope = _factory.Services.CreateScope();
        var verificationDbContext =
            verificationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.True(await verificationDbContext.GameRounds.AnyAsync(round => round.Id == roundId));
    }

    [Fact]
    public async Task SetTeamPlayedState_WhenAnotherTeamHasActiveRound_ReturnsPlayedTeamToQueue()
    {
        var cellId = await SeedSingleCellAsync();
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);
        var openResponse = await adminClient.PostAsync($"/api/game/cells/{cellId}/open", content: null);
        Assert.Equal(HttpStatusCode.NoContent, openResponse.StatusCode);

        Guid activeTeamId;
        Guid returnedTeamId;
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var game = await dbContext.BoardCells
                .Where(cell => cell.Id == cellId)
                .Select(cell => cell.Board.Game)
                .SingleAsync();
            Assert.NotNull(game.ActiveTeamId);
            activeTeamId = game.ActiveTeamId.Value;

            var now = DateTime.UtcNow;
            var userId = Guid.NewGuid();
            var slotId = Guid.NewGuid();
            returnedTeamId = Guid.NewGuid();
            dbContext.Users.Add(
                new User
                {
                    Id = userId,
                    TwitchUserId = $"returned-team-{userId:N}",
                    Login = $"returned-team-{userId:N}"[..32],
                    DisplayName = "Returned team member",
                    IsActive = true,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                }
            );
            dbContext.GameTeamSlots.Add(
                new GameTeamSlot
                {
                    Id = slotId,
                    GameId = game.Id,
                    SlotIndex = 2,
                    SlotType = TeamSlotTypeValue.Public,
                    CreatedAtUtc = now
                }
            );
            dbContext.GameTeams.Add(
                new GameTeam
                {
                    Id = returnedTeamId,
                    GameId = game.Id,
                    SlotId = slotId,
                    Status = TeamStatusValue.Confirmed,
                    RecruitmentOpen = false,
                    IsPlayed = true,
                    PlayedAtUtc = now.AddMinutes(-5),
                    CreatedByUserId = userId,
                    ConfirmedByUserId = userId,
                    ConfirmedAtUtc = now.AddMinutes(-10),
                    CreatedAtUtc = now.AddMinutes(-10),
                    UpdatedAtUtc = now.AddMinutes(-5)
                }
            );
            dbContext.GameTeamMembers.Add(
                new GameTeamMember
                {
                    Id = Guid.NewGuid(),
                    GameId = game.Id,
                    TeamId = returnedTeamId,
                    UserId = userId,
                    JoinedAtUtc = now.AddMinutes(-10)
                }
            );
            await dbContext.SaveChangesAsync();
        }

        using var moderatorClient = CreateAuthenticatedClient([AuthRoleCodes.Moderator]);
        var response = await moderatorClient.PutAsJsonAsync(
            $"/api/game/teams/{returnedTeamId}/played-state",
            new SetGameTeamPlayedStateRequestDto(false)
        );

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var queue = await moderatorClient.GetFromJsonAsync<GameTeamQueueResultDto>(
            "/api/game/team-queue"
        );
        Assert.NotNull(queue);
        var returnedTeam = Assert.Single(queue.Teams, team => team.TeamId == returnedTeamId.ToString());
        Assert.False(returnedTeam.IsPlayed);
        Assert.Null(returnedTeam.PlayedAtUtc);

        var snapshot = await moderatorClient.GetFromJsonAsync<GameBoardSnapshotDto>("/api/game");
        Assert.NotNull(snapshot);
        Assert.Equal(activeTeamId.ToString(), snapshot.ActiveTeamId);
        Assert.NotNull(
            await moderatorClient.GetFromJsonAsync<GameRoundDetailsDto>("/api/game/rounds/active")
        );
    }

    [Fact]
    public async Task GetTeamQueue_WhenConfirmedTeamHasName_ReturnsTeamName()
    {
        await SeedSingleCellAsync(teamName: "Named Crew");
        using var moderatorClient = CreateAuthenticatedClient([AuthRoleCodes.Moderator]);

        var queueResponse = await moderatorClient.GetAsync("/api/game/team-queue");

        Assert.Equal(HttpStatusCode.OK, queueResponse.StatusCode);
        var queue = await queueResponse.Content.ReadFromJsonAsync<GameTeamQueueResultDto>();
        Assert.NotNull(queue);
        Assert.Equal(1, queue.Summary.TotalTeams);
        Assert.Equal(0, queue.Summary.PlayedTeams);
        Assert.Equal(1, queue.Summary.RemainingTeams);
        var queueItem = Assert.Single(queue.Teams);
        Assert.Equal("Named Crew", queueItem.TeamName);
        Assert.False(queueItem.IsPlayed);
        Assert.Null(queueItem.PlayedAtUtc);
    }

    [Fact]
    public async Task SetActiveTeam_WhenTeamMarkedPlayed_ReturnsConflict()
    {
        var cellId = await SeedSingleCellAsync();
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var teamId = await dbContext.BoardCells
            .Where(cell => cell.Id == cellId)
            .SelectMany(cell => dbContext.GameTeams.Where(team => team.GameId == cell.Board.GameId))
            .Select(team => team.Id)
            .SingleAsync();
        using var moderatorClient = CreateAuthenticatedClient([AuthRoleCodes.Moderator]);

        var markResponse = await moderatorClient.PutAsJsonAsync(
            $"/api/game/teams/{teamId}/played-state",
            new SetGameTeamPlayedStateRequestDto(true)
        );
        Assert.Equal(HttpStatusCode.NoContent, markResponse.StatusCode);

        var response = await moderatorClient.PutAsJsonAsync(
            "/api/game/active-team",
            new SetActiveGameTeamRequestDto(teamId.ToString())
        );

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.Client.GameActiveTeamAlreadyPlayed, payload.Error);
        Assert.Equal(AppMessages.ErrorCodes.GameBoardActiveTeamAlreadyPlayed, payload.Code);
    }

    [Fact]
    public async Task SetTeamPlayedState_WhenMultipleActiveGamesExist_UsesLatestActiveGame()
    {
        var teamId = await SeedTwoActiveGamesAndReturnLatestTeamIdAsync();
        using var moderatorClient = CreateAuthenticatedClient([AuthRoleCodes.Moderator]);

        var response = await moderatorClient.PutAsJsonAsync(
            $"/api/game/teams/{teamId}/played-state",
            new SetGameTeamPlayedStateRequestDto(true)
        );

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var queueResponse = await moderatorClient.GetAsync("/api/game/team-queue");
        Assert.Equal(HttpStatusCode.OK, queueResponse.StatusCode);
        var queue = await queueResponse.Content.ReadFromJsonAsync<GameTeamQueueResultDto>();
        Assert.NotNull(queue);
        Assert.Equal(1, queue.Summary.TotalTeams);
        Assert.Equal(1, queue.Summary.PlayedTeams);
        Assert.Equal(0, queue.Summary.RemainingTeams);
        var queueItem = Assert.Single(queue.Teams);
        Assert.Equal(teamId.ToString(), queueItem.TeamId);
        Assert.True(queueItem.IsPlayed);
        Assert.NotNull(queueItem.PlayedAtUtc);
    }

    [Theory]
    [InlineData(GameRoundStatusValue.AwaitingModifiers)]
    [InlineData(GameRoundStatusValue.Preparing)]
    public async Task OpenCell_WhenRoundIsActive_ReturnsConflict(string roundStatus)
    {
        var cellId = await SeedSingleCellAsync();
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);
        var openResponse = await adminClient.PostAsync($"/api/game/cells/{cellId}/open", content: null);
        Assert.Equal(HttpStatusCode.NoContent, openResponse.StatusCode);
        if (roundStatus == GameRoundStatusValue.Preparing)
        {
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var round = await dbContext.GameRounds.SingleAsync(
                candidate => candidate.BoardCellId == cellId
            );
            round.Status = GameRoundStatusValue.Preparing;
            round.PreparedAtUtc = DateTime.UtcNow;
            round.Version += 1;
            await dbContext.SaveChangesAsync();
        }

        var response = await adminClient.PostAsync($"/api/game/cells/{cellId}/open", content: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.Client.GameRoundAlreadyInProgress, payload.Error);
        Assert.Equal(AppMessages.ErrorCodes.GameRoundAlreadyInProgress, payload.Code);
    }

    [Fact]
    public async Task OpenCell_WhenAdminAndCellMissing_ReturnsNotFound()
    {
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);

        var response = await adminClient.PostAsync($"/api/game/cells/{Guid.NewGuid()}/open", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.Client.GameCellNotFound, payload.Error);
        Assert.Equal(AppMessages.ErrorCodes.GameBoardCellNotFound, payload.Code);
    }

    [Fact]
    public async Task GetGame_WhenServiceThrows_ReturnsInternalServerErrorPayload()
    {
        using var authenticatedClient = CreateAuthenticatedClient(
            [AuthRoleCodes.Viewer],
            gameBoardService: new ThrowingGameBoardService()
        );

        var response = await authenticatedClient.GetAsync("/api/game");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.Client.UnexpectedServerError, payload.Error);
        Assert.Equal(AppMessages.ErrorCodes.UnexpectedServerError, payload.Code);
        Assert.False(string.IsNullOrWhiteSpace(payload.RequestId));
    }

    [Fact]
    public async Task RealtimeSmoke_OpenCell_WhenAdmin_PublishesCellOpenedEvent()
    {
        var cellId = await SeedSingleCellAsync();
        var publisher = new RecordingGameBoardEventsPublisher();
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin], publisher: publisher);

        var response = await adminClient.PostAsync($"/api/game/cells/{cellId}/open", content: null);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var payload = Assert.Single(publisher.PublishedEvents);
        Assert.Equal(cellId.ToString(), payload.Cell.Id);
        Assert.Equal("open", payload.Cell.State.ToString().ToLowerInvariant());
        Assert.True(payload.Version >= 2);
    }

    [Fact]
    public async Task RealtimeSmoke_OpenCell_WhenCalledTwice_PublishesSingleEvent()
    {
        var cellId = await SeedSingleCellAsync();
        var publisher = new RecordingGameBoardEventsPublisher();
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin], publisher: publisher);

        var firstResponse = await adminClient.PostAsync($"/api/game/cells/{cellId}/open", content: null);
        var secondResponse = await adminClient.PostAsync($"/api/game/cells/{cellId}/open", content: null);

        Assert.Equal(HttpStatusCode.NoContent, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);

        var payload = Assert.Single(publisher.PublishedEvents);
        Assert.Equal(cellId.ToString(), payload.Cell.Id);
        Assert.Equal("open", payload.Cell.State.ToString().ToLowerInvariant());
    }

    [Fact]
    public async Task GetModifierCatalog_WhenAuthenticated_ReturnsSeededCatalog()
    {
        await EnsureModifierDefinitionsSeededAsync();
        using var authenticatedClient = CreateAuthenticatedClient([AuthRoleCodes.Viewer]);

        var response = await authenticatedClient.GetAsync("/api/game/modifiers/catalog");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<IReadOnlyList<GameModifierDefinitionDto>>();
        Assert.NotNull(payload);
        var chirik = Assert.Single(
            payload,
            modifier => modifier.Id == ModifierDefinitionSeedIds.Chirik.ToString()
        );
        Assert.Equal(1, chirik.Revision);
        Assert.Equal(2, chirik.BehaviorV2.SchemaVersion);
        Assert.Equal("rule", chirik.BehaviorV2.Kind);
        Assert.IsType<GameModifierRuleStatusResolutionDto>(chirik.BehaviorV2.Resolution);
        Assert.Contains("таймер", chirik.NormalizedTags);
    }

    [Fact]
    public async Task ModifierRevisionHistory_EnforcesRolesNoOpStaleArchiveAndFullReadModel()
    {
        var name = $"Revision test {Guid.NewGuid():N}";
        var createRequest = CreateRuleOnlyModifierRequest(name) with
        {
            ChangeNote = "Initial immutable revision"
        };
        var publisher = new RecordingGameBoardEventsPublisher();
        using var adminClient = CreateAuthenticatedClient(
            [AuthRoleCodes.Admin],
            publisher: publisher
        );
        using var moderatorClient = CreateAuthenticatedClient([AuthRoleCodes.Moderator]);
        using var viewerClient = CreateAuthenticatedClient([AuthRoleCodes.Viewer]);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await moderatorClient.PostAsJsonAsync("/api/game/modifiers", createRequest)).StatusCode
        );
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await _client.GetAsync("/api/game/modifiers/history")).StatusCode
        );

        var createResponse = await adminClient.PostAsJsonAsync("/api/game/modifiers", createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<GameModifierDefinitionDto>();
        Assert.NotNull(created);

        var noOpResponse = await adminClient.PutAsJsonAsync(
            $"/api/game/modifiers/{created.Id}",
            CreateRuleOnlyUpdateRequest(
                $"  {name}  ",
                1,
                "This note is ignored for a no-op"
            )
        );
        Assert.Equal(HttpStatusCode.OK, noOpResponse.StatusCode);
        Assert.Equal(
            1,
            (await noOpResponse.Content.ReadFromJsonAsync<GameModifierDefinitionDto>())?.Revision
        );

        var updateResponse = await adminClient.PutAsJsonAsync(
            $"/api/game/modifiers/{created.Id}",
            CreateRuleOnlyUpdateRequest(name + " v2", 1, "Raised the visible revision")
        );
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<GameModifierDefinitionDto>();
        Assert.NotNull(updated);
        Assert.Equal(2, updated.Revision);

        var staleResponse = await adminClient.PutAsJsonAsync(
            $"/api/game/modifiers/{created.Id}",
            CreateRuleOnlyUpdateRequest(name + " stale", 1)
        );
        Assert.Equal(HttpStatusCode.Conflict, staleResponse.StatusCode);
        Assert.Equal(
            AppMessages.ErrorCodes.GameModifierRevisionStale,
            (await staleResponse.Content.ReadFromJsonAsync<ErrorResponse>())?.Code
        );

        var versionsResponse = await viewerClient.GetAsync(
            $"/api/game/modifiers/{created.Id}/versions?limit=20"
        );
        Assert.Equal(HttpStatusCode.OK, versionsResponse.StatusCode);
        var versions = await versionsResponse.Content
            .ReadFromJsonAsync<ModifierHistoryPageDto<ModifierVersionSummaryDto>>();
        Assert.NotNull(versions);
        Assert.Equal([2, 1], versions.Items.Select(x => x.Revision));
        Assert.Equal("edited", versions.Items[0].ChangeType);
        Assert.Equal("Raised the visible revision", versions.Items[0].ChangeNote);
        Assert.Contains("name", versions.Items[0].ChangedFields);
        Assert.Equal("created", versions.Items[1].ChangeType);

        var v1Response = await viewerClient.GetAsync(
            $"/api/game/modifiers/{created.Id}/versions/1"
        );
        Assert.Equal(HttpStatusCode.OK, v1Response.StatusCode);
        var v1 = await v1Response.Content.ReadFromJsonAsync<ModifierVersionDetailDto>();
        Assert.NotNull(v1);
        Assert.Equal(name, v1.Name);
        Assert.False(v1.IsCurrent);
        Assert.Equal(2, v1.BehaviorV2.SchemaVersion);

        var tooLongNoteResponse = await adminClient.PutAsJsonAsync(
            $"/api/game/modifiers/{created.Id}",
            CreateRuleOnlyUpdateRequest(name + " rejected", 2, new string('x', 501))
        );
        Assert.Equal(HttpStatusCode.BadRequest, tooLongNoteResponse.StatusCode);

        Assert.Equal(
            HttpStatusCode.NoContent,
            (
                await adminClient.DeleteAsync(
                    $"/api/game/modifiers/{created.Id}?expectedRevision=2"
                )
            ).StatusCode
        );

        var catalog = await viewerClient.GetFromJsonAsync<IReadOnlyList<GameModifierDefinitionDto>>(
            "/api/game/modifiers/catalog"
        );
        Assert.DoesNotContain(catalog!, x => x.Id == created.Id);
        var history = await viewerClient.GetFromJsonAsync<
            ModifierHistoryPageDto<ModifierHistorySummaryDto>
        >($"/api/game/modifiers/history?status=archived&search={Uri.EscapeDataString(name)}");
        var archived = Assert.Single(history!.Items, x => x.ModifierId == created.Id);
        Assert.True(archived.IsArchived);
        Assert.Equal(2, archived.VersionCount);
        Assert.Equal(3, publisher.PublishedModifierCatalogChangedEvents.Count);
        Assert.Equal(
            [1, 2, 2],
            publisher.PublishedModifierCatalogChangedEvents
                .SelectMany(x => x.Modifiers)
                .Select(x => x.Revision)
        );
        Assert.True(publisher.PublishedModifierCatalogChangedEvents[^1].Modifiers[0].IsArchived);
    }

    [Fact]
    public async Task CompatibilityUpdate_CreatesSymmetricCascadeRevisions()
    {
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);
        using var viewerClient = CreateAuthenticatedClient([AuthRoleCodes.Viewer]);
        var left = await CreateModifierAsync(adminClient, $"Left {Guid.NewGuid():N}");
        var right = await CreateModifierAsync(adminClient, $"Right {Guid.NewGuid():N}");

        var updateResponse = await adminClient.PutAsJsonAsync(
            $"/api/game/modifiers/{left.Id}",
            CreateRuleOnlyUpdateRequest(
                left.Name,
                1,
                "Make the pair incompatible",
                [right.Id]
            )
        );
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var leftV2 = await viewerClient.GetFromJsonAsync<ModifierVersionDetailDto>(
            $"/api/game/modifiers/{left.Id}/versions/2"
        );
        var rightV2 = await viewerClient.GetFromJsonAsync<ModifierVersionDetailDto>(
            $"/api/game/modifiers/{right.Id}/versions/2"
        );
        Assert.NotNull(leftV2);
        Assert.NotNull(rightV2);
        Assert.Equal("edited", leftV2.ChangeType);
        Assert.Equal("compatibility_cascade", rightV2.ChangeType);
        Assert.Equal(left.Id, rightV2.CascadeSourceModifierId);
        Assert.Contains(leftV2.Conflicts, x => x.ModifierId == right.Id && x.Name == right.Name);
        Assert.Contains(rightV2.Conflicts, x => x.ModifierId == left.Id && x.Name == left.Name);
    }

    [Fact]
    public async Task EditingModifier_PreservesArchivedCompatibilityWithoutRewritingArchivedHistory()
    {
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);
        using var viewerClient = CreateAuthenticatedClient([AuthRoleCodes.Viewer]);
        var left = await CreateModifierAsync(adminClient, $"Left archive {Guid.NewGuid():N}");
        var right = await CreateModifierAsync(adminClient, $"Right archive {Guid.NewGuid():N}");
        Assert.Equal(
            HttpStatusCode.OK,
            (await adminClient.PutAsJsonAsync(
                $"/api/game/modifiers/{left.Id}",
                CreateRuleOnlyUpdateRequest(left.Name, 1, conflictingModifierIds: [right.Id])
            )).StatusCode
        );
        Assert.Equal(
            HttpStatusCode.NoContent,
            (await adminClient.DeleteAsync(
                $"/api/game/modifiers/{right.Id}?expectedRevision=2"
            )).StatusCode
        );

        var edit = await adminClient.PutAsJsonAsync(
            $"/api/game/modifiers/{left.Id}",
            CreateRuleOnlyUpdateRequest(left.Name + " renamed", 2, conflictingModifierIds: [])
        );

        Assert.Equal(HttpStatusCode.OK, edit.StatusCode);
        var leftV3 = await viewerClient.GetFromJsonAsync<ModifierVersionDetailDto>(
            $"/api/game/modifiers/{left.Id}/versions/3"
        );
        Assert.Contains(leftV3!.Conflicts, x => x.ModifierId == right.Id && x.Name == right.Name);
        var rightVersions = await viewerClient.GetFromJsonAsync<
            ModifierHistoryPageDto<ModifierVersionSummaryDto>
        >($"/api/game/modifiers/{right.Id}/versions");
        Assert.Equal([2, 1], rightVersions!.Items.Select(x => x.Revision));
    }

    [Fact]
    public async Task CreateModifier_WhenCompatibilityTouchesActiveGame_RollsBackEntireMutation()
    {
        await EnsureModifierDefinitionsSeededAsync();
        await SeedActiveGameWithEnabledModifiersAsync(["chirik"]);
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);
        using var viewerClient = CreateAuthenticatedClient([AuthRoleCodes.Viewer]);
        var name = $"Rejected cascade {Guid.NewGuid():N}";

        var response = await adminClient.PostAsJsonAsync(
            "/api/game/modifiers",
            CreateRuleOnlyModifierRequest(name) with
            {
                ConflictingModifierIds = [ModifierDefinitionSeedIds.Chirik.ToString()]
            }
        );

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(
            AppMessages.ErrorCodes.GameModifierCompatibilityLocked,
            (await response.Content.ReadFromJsonAsync<ErrorResponse>())?.Code
        );
        var history = await viewerClient.GetFromJsonAsync<
            ModifierHistoryPageDto<ModifierHistorySummaryDto>
        >($"/api/game/modifiers/history?search={Uri.EscapeDataString(name)}");
        Assert.Empty(history!.Items);
    }

    [Fact]
    public async Task CreateModifier_WhenAdminWithoutCode_ReturnsCreatedModifierWithId()
    {
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);

        var response = await adminClient.PostAsJsonAsync(
            "/api/game/modifiers",
            new CreateGameModifierRequestDto(
                "Fresh modifier",
                "Created without a manual code.",
                GameModifierCategories.Round,
                5,
                new GameModifierActivationLimitDto(1),
                [],
                null,
                null,
                ["Тест", " правило ", "тест"],
                new GameModifierBehaviorV2Dto(
                    2,
                    "rule",
                    "round",
                    "activeTeam",
                    false,
                    "Typed rule contract.",
                    "aggregateParameters",
                    new GameModifierRuleStatusResolutionDto(),
                    "none",
                    null
                )
            )
        );

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<GameModifierDefinitionDto>();
        Assert.NotNull(payload);
        Assert.True(Guid.TryParse(payload.Id, out _));
        Assert.Equal("Fresh modifier", payload.Name);
        Assert.Equal(1, payload.Revision);
        Assert.Equal(["Тест", "правило"], payload.NormalizedTags);
        Assert.Equal("!активировать fresh modifier", payload.ActivationCommand);
        Assert.Equal("Typed rule contract.", payload.BehaviorV2.Rule);
    }

    [Fact]
    public async Task CreateModifier_WhenBehaviorV2IsMissing_ReturnsBadRequest()
    {
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);
        var payload = JsonSerializer.SerializeToElement(
            CreateRuleOnlyModifierRequest("Missing behavior"),
            new JsonSerializerOptions(JsonSerializerDefaults.Web)
        );
        var requestWithoutBehavior = payload
            .EnumerateObject()
            .Where(property => property.Name != "behaviorV2")
            .ToDictionary(property => property.Name, property => property.Value);

        var response = await adminClient.PostAsJsonAsync(
            "/api/game/modifiers",
            requestWithoutBehavior
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PreviewModifier_WhenTypedDraftIsValid_ReturnsNormalizedAuthoritativeExample()
    {
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);
        var request = new CreateGameModifierRequestDto(
            "  Thirst preview  ",
            "  Growing kill reward.  ",
            GameModifierCategories.Result,
            5,
            new GameModifierActivationLimitDto(2),
            [],
            "💧",
            null,
            ["  Бой   вблизи ", "БОЙ ВБЛИЗИ", "Бонус"],
            new GameModifierBehaviorV2Dto(
                2,
                "scoring",
                "result",
                "activeTeam",
                false,
                "Growing kill reward.",
                "independentInstances",
                new GameModifierAutomaticRoundMetricResolutionDto("killsCount"),
                "points",
                new GameModifierFormulaReferenceV2Dto(
                    ModifierFormulaCodes.GrowingKillValue,
                    1,
                    new GameModifierGrowingKillValueParametersDto(5, 25)
                )
            )
        );

        var response = await adminClient.PostAsJsonAsync("/api/game/modifiers/preview", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var preview = await response.Content.ReadFromJsonAsync<GameModifierDraftPreviewDto>();
        Assert.NotNull(preview);
        Assert.Equal("Thirst preview", preview.Name);
        Assert.Equal(["Бой вблизи", "Бонус"], preview.NormalizedTags);
        Assert.Equal("!активировать thirst preview", preview.ActivationCommand);
        Assert.Equal(100, preview.Example.CardValue);
        Assert.Equal(3, preview.Example.KillsCount);
        Assert.Equal(45, preview.Example.PointsDelta);
        Assert.Equal(0, preview.Example.BonusKillsDelta);
        Assert.Equal(445, preview.Example.FinalScore);

        var tooManyTagsResponse = await adminClient.PostAsJsonAsync(
            "/api/game/modifiers/preview",
            request with { NormalizedTags = ["a", "b", "c", "d", "e", "f"] }
        );
        var tooLongTagResponse = await adminClient.PostAsJsonAsync(
            "/api/game/modifiers/preview",
            request with { NormalizedTags = [string.Concat(Enumerable.Repeat("👨‍👩‍👧‍👦", 33))] }
        );
        Assert.Equal(HttpStatusCode.BadRequest, tooManyTagsResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, tooLongTagResponse.StatusCode);
    }

    [Fact]
    public async Task CreateModifier_WhenActivationLimitCountIsInvalid_ReturnsBadRequest()
    {
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);

        var request = CreateRuleOnlyModifierRequest("Invalid-limit modifier") with
        {
            ActivationLimit = new GameModifierActivationLimitDto(0)
        };

        var response = await adminClient.PostAsJsonAsync("/api/game/modifiers", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.ErrorCodes.GameModifierInvalidRequest, payload.Code);
    }

    [Fact]
    public async Task UpdateModifier_WhenAdminSetsConflict_ActivationHonorsUpdatedConflict()
    {
        await EnsureModifierDefinitionsSeededAsync();
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);
        var updateResponse = await adminClient.PutAsJsonAsync(
            $"/api/game/modifiers/{ModifierDefinitionSeedIds.Chirik}",
            CreateRuleOnlyModifierRequest("Чирик") with
            {
                ConflictingModifierIds = [ModifierDefinitionSeedIds.Feyerverk.ToString()]
            }
        );

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<GameModifierDefinitionDto>();
        Assert.NotNull(updated);
        Assert.Equal(2, updated.Revision);
        Assert.Contains(ModifierDefinitionSeedIds.Feyerverk.ToString(), updated.ConflictingModifierIds);

        await SeedActiveGameWithEnabledModifiersAsync(["chirik", "feyerverk"], ["feyerverk"]);
        using var moderatorClient = CreateAuthenticatedClient([AuthRoleCodes.Moderator]);

        var activateResponse = await moderatorClient.PostAsync(
            $"/api/game/modifiers/{ModifierDefinitionSeedIds.Chirik}/activate",
            content: null
        );

        Assert.Equal(HttpStatusCode.Conflict, activateResponse.StatusCode);
        var payload = await activateResponse.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.ErrorCodes.GameModifierConflictActive, payload.Code);
    }

    [Fact]
    public async Task UpdateAndDeleteModifier_WhenIncludedInActiveGame_ReturnContentLock()
    {
        await EnsureModifierDefinitionsSeededAsync();
        await SeedActiveGameWithEnabledModifiersAsync(["chirik"]);
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);

        var catalogResponse = await adminClient.GetAsync("/api/game/modifiers/catalog");
        var catalog = await catalogResponse.Content
            .ReadFromJsonAsync<IReadOnlyList<GameModifierDefinitionDto>>();
        Assert.NotNull(catalog);
        var lockedModifier = Assert.Single(
            catalog,
            x => x.Id == ModifierDefinitionSeedIds.Chirik.ToString()
        );
        Assert.True(lockedModifier.IsLockedByActiveGame);

        var updateResponse = await adminClient.PutAsJsonAsync(
            $"/api/game/modifiers/{ModifierDefinitionSeedIds.Chirik}",
            CreateRuleOnlyUpdateRequest("Locked update", lockedModifier.Revision)
        );
        var deleteResponse = await adminClient.DeleteAsync(
            $"/api/game/modifiers/{ModifierDefinitionSeedIds.Chirik}?expectedRevision={lockedModifier.Revision}"
        );

        Assert.Equal(HttpStatusCode.Conflict, updateResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, deleteResponse.StatusCode);
        Assert.Equal(
            AppMessages.ErrorCodes.GameModifierContentLocked,
            (await updateResponse.Content.ReadFromJsonAsync<ErrorResponse>())?.Code
        );
        Assert.Equal(
            AppMessages.ErrorCodes.GameModifierContentLocked,
            (await deleteResponse.Content.ReadFromJsonAsync<ErrorResponse>())?.Code
        );

        var unlockedUpdateResponse = await adminClient.PutAsJsonAsync(
            $"/api/game/modifiers/{ModifierDefinitionSeedIds.Feyerverk}",
            CreateRuleOnlyModifierRequest("Unlocked modifier")
        );
        Assert.Equal(HttpStatusCode.OK, unlockedUpdateResponse.StatusCode);
    }

    [Fact]
    public async Task ActivateModifier_WhenModeratorAndEnabledForActiveGame_ReturnsNoContent()
    {
        await EnsureModifierDefinitionsSeededAsync();
        await SeedActiveGameWithEnabledModifiersAsync(["chirik"]);
        var userId = Guid.NewGuid();
        await SeedQuizPointsAsync(userId, 100);
        using var moderatorClient = CreateAuthenticatedClient([AuthRoleCodes.Moderator], userId);

        var response = await moderatorClient.PostAsync(
            $"/api/game/modifiers/{ModifierDefinitionSeedIds.Chirik}/activate",
            content: null
        );

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(
            1,
            await dbContext.GameModifierActivations.CountAsync(
                x =>
                    x.ModifierId == ModifierDefinitionSeedIds.Chirik
                    && x.ActivatedByUserId == userId
                    && x.ActivationCostSnapshot > 0
                )
        );
    }

    [Fact]
    public async Task ActivateModifier_WhenActiveGameBindingIsMissing_FailsClosed()
    {
        await EnsureModifierDefinitionsSeededAsync();
        await SeedActiveGameWithEnabledModifiersAsync(["chirik"]);
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var enabled = await dbContext.GameEnabledModifiers.SingleAsync();
            enabled.ModifierVersionId = null;
            enabled.VersionPinnedAtUtc = null;
            await dbContext.SaveChangesAsync();
        }
        var userId = Guid.NewGuid();
        await SeedQuizPointsAsync(userId, 100);
        using var viewerClient = CreateAuthenticatedClient([AuthRoleCodes.Viewer], userId);

        var response = await viewerClient.PostAsync(
            $"/api/game/modifiers/{ModifierDefinitionSeedIds.Chirik}/activate",
            content: null
        );

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(
            AppMessages.ErrorCodes.GameModifierVersionBindingMissing,
            (await response.Content.ReadFromJsonAsync<ErrorResponse>())?.Code
        );
    }

    [Fact]
    public async Task EmergencyDisableModifier_BlocksOnlyNewActivationsAndPreservesFirstAudit()
    {
        await EnsureModifierDefinitionsSeededAsync();
        await SeedActiveGameWithEnabledModifiersAsync(["chirik"], ["chirik"]);

        Guid adminUserId;
        Guid existingActivationId;
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            adminUserId = await dbContext.GameTeamMembers.Select(x => x.UserId).SingleAsync();
            existingActivationId = await dbContext.GameModifierActivations.Select(x => x.Id).SingleAsync();
        }

        var publisher = new RecordingGameBoardEventsPublisher();
        using var adminClient = CreateAuthenticatedClient(
            [AuthRoleCodes.Admin],
            adminUserId,
            publisher
        );
        var firstResponse = await adminClient.PostAsJsonAsync(
            $"/api/game/modifiers/{ModifierDefinitionSeedIds.Chirik}/emergency-disable",
            new EmergencyDisableGameModifierRequestDto("Production rule defect")
        );
        var secondResponse = await adminClient.PostAsJsonAsync(
            $"/api/game/modifiers/{ModifierDefinitionSeedIds.Chirik}/emergency-disable",
            new EmergencyDisableGameModifierRequestDto("Must not overwrite original audit")
        );

        Assert.Equal(HttpStatusCode.NoContent, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, secondResponse.StatusCode);
        var availabilityEvent = Assert.Single(publisher.PublishedModifierAvailabilityEvents);
        Assert.Equal(ModifierDefinitionSeedIds.Chirik, availabilityEvent.ModifierId);

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var enabled = await dbContext.GameEnabledModifiers.SingleAsync(
                x => x.ModifierId == ModifierDefinitionSeedIds.Chirik
            );
            Assert.Equal(adminUserId, enabled.EmergencyDisabledByUserId);
            Assert.Equal("Production rule defect", enabled.EmergencyDisableReason);
            Assert.NotNull(enabled.EmergencyDisabledAtUtc);
            Assert.True(await dbContext.GameModifierActivations.AnyAsync(x => x.Id == existingActivationId));
        }

        var outsiderId = Guid.NewGuid();
        await SeedQuizPointsAsync(outsiderId, 100);
        using var moderatorClient = CreateAuthenticatedClient([AuthRoleCodes.Moderator], outsiderId);
        var activateResponse = await moderatorClient.PostAsync(
            $"/api/game/modifiers/{ModifierDefinitionSeedIds.Chirik}/activate",
            content: null
        );
        Assert.Equal(HttpStatusCode.Conflict, activateResponse.StatusCode);
        Assert.Equal(
            AppMessages.ErrorCodes.GameModifierEmergencyDisabled,
            (await activateResponse.Content.ReadFromJsonAsync<ErrorResponse>())?.Code
        );

        var stateResponse = await moderatorClient.GetAsync("/api/game/modifiers/state");
        var state = await stateResponse.Content.ReadFromJsonAsync<GameModifierStateDto>();
        Assert.NotNull(state);
        var availability = Assert.Single(state.AvailableModifiers);
        Assert.True(availability.IsEmergencyDisabled);
        Assert.False(availability.CanActivate);
        Assert.Equal("emergency_disabled", availability.BlockedReason);
    }

    [Fact]
    public async Task ActivateModifier_WhenPlayerBelongsToCurrentRoundTeam_ReturnsConflictWithoutCharge()
    {
        await EnsureModifierDefinitionsSeededAsync();
        await SeedActiveGameWithEnabledModifiersAsync(["chirik"]);
        Guid activeTeamMemberId;
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            activeTeamMemberId = await dbContext.GameTeamMembers
                .Where(member => member.LeftAtUtc == null)
                .Select(member => member.UserId)
                .SingleAsync();
        }
        await SeedQuizPointsAsync(activeTeamMemberId, 25);
        using var playerClient = CreateAuthenticatedClient([AuthRoleCodes.Viewer], activeTeamMemberId);

        var state = await playerClient.GetFromJsonAsync<GameModifierStateDto>(
            "/api/game/modifiers/state"
        );
        Assert.NotNull(state);
        var availability = Assert.Single(state.AvailableModifiers);
        Assert.False(availability.CanActivate);
        Assert.Equal("active_team_member", availability.BlockedReason);

        var response = await playerClient.PostAsync(
            $"/api/game/modifiers/{ModifierDefinitionSeedIds.Chirik}/activate",
            content: null
        );

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.ErrorCodes.GameModifierActiveTeamMember, payload.Code);

        using var verificationScope = _factory.Services.CreateScope();
        var verificationDbContext =
            verificationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Empty(await verificationDbContext.GameModifierActivations.ToArrayAsync());
        var stateAfterAttempt = await playerClient.GetFromJsonAsync<GameModifierStateDto>(
            "/api/game/modifiers/state"
        );
        Assert.NotNull(stateAfterAttempt);
        Assert.Equal(25, stateAfterAttempt.AvailableQuizPoints);
        Assert.Equal(0, stateAfterAttempt.SpentQuizPoints);
    }

    [Fact]
    public async Task AdminActivateModifier_WhenTargetBelongsToCurrentRoundTeam_ReturnsConflict()
    {
        await EnsureModifierDefinitionsSeededAsync();
        await SeedActiveGameWithEnabledModifiersAsync(["chirik"]);
        Guid activeTeamMemberId;
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            activeTeamMemberId = await dbContext.GameTeamMembers
                .Where(member => member.LeftAtUtc == null)
                .Select(member => member.UserId)
                .SingleAsync();
        }
        await SeedQuizPointsAsync(activeTeamMemberId, 25);
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);

        var response = await adminClient.PostAsJsonAsync(
            "/api/game/modifiers/admin/activate",
            new AdminActivateGameModifierRequestDto(
                ModifierDefinitionSeedIds.Chirik.ToString(),
                activeTeamMemberId.ToString()
            )
        );

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.ErrorCodes.GameModifierActiveTeamMember, payload.Code);
    }

    [Fact]
    public async Task GetAdminModifierPlayers_WhenAdmin_ReturnsPlayersWithBalances()
    {
        await EnsureModifierDefinitionsSeededAsync();
        await SeedActiveGameWithEnabledModifiersAsync(["chirik"]);
        var userId = Guid.NewGuid();
        await SeedQuizPointsAsync(userId, 25);
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);

        var response = await adminClient.GetAsync("/api/game/modifiers/admin/players");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<GameModifierAdminPlayersResultDto>();
        Assert.NotNull(payload);
        Assert.True(payload.Summary.PlayersCount >= 1);
        Assert.True(payload.Summary.TotalAvailableQuizPoints >= 25);
        Assert.True(payload.Summary.TotalEarnedQuizPoints >= 25);
        Assert.Equal(0, payload.Summary.TotalSpentQuizPoints);
        var player = Assert.Single(payload.Players, item => item.UserId == userId.ToString());
        Assert.Equal(25, player.AvailableQuizPoints);
        Assert.Equal(25, player.EarnedQuizPoints);
        Assert.Equal(0, player.SpentQuizPoints);
    }

    [Fact]
    public async Task GetModifierState_WhenFreeActivationPriceChanges_KeepsZeroCostSnapshot()
    {
        await EnsureModifierDefinitionsSeededAsync();
        await SeedActiveGameWithEnabledModifiersAsync(["chirik"]);
        var userId = Guid.NewGuid();
        await SeedQuizPointsAsync(userId, 25);

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var gameId = await dbContext.Games
                .Where(x => x.Status == GameStatusValue.Active && !x.IsDeleted)
                .Select(x => x.Id)
                .SingleAsync();
            var roundId = await dbContext.GameRounds
                .Where(x => x.GameId == gameId && x.Status == GameRoundStatusValue.AwaitingModifiers)
                .Select(x => x.Id)
                .SingleAsync();
            var definition = await dbContext.ModifierDefinitions.SingleAsync(
                x => x.Id == ModifierDefinitionSeedIds.Chirik
            );
            dbContext.GameModifierActivations.Add(
                new backend.Data.Entities.GameModifierActivation
                {
                    Id = Guid.NewGuid(),
                    GameId = gameId,
                    RoundId = roundId,
                    ModifierId = definition.Id,
                    ActivatedByUserId = userId,
                    InitiatedByUserId = userId,
                    ActivationCostSnapshot = 0,
                    ActivatedAtUtc = DateTime.UtcNow.AddMinutes(-1)
                }
            );
            await dbContext.SaveChangesAsync();
        }

        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);
        var state = await adminClient.GetFromJsonAsync<GameModifierStateDto>(
            $"/api/game/modifiers/admin/state/{userId}"
        );

        Assert.NotNull(state);
        var activation = Assert.Single(state.ActiveModifiers);
        Assert.Equal(0, activation.ActivationCost);
        Assert.Equal(0, state.SpentQuizPoints);
        Assert.Equal(25, state.AvailableQuizPoints);
    }

    [Fact]
    public async Task GetModifierState_WhenEarnedPointsExceedContractRange_ClampsTotals()
    {
        await EnsureModifierDefinitionsSeededAsync();
        await SeedActiveGameWithEnabledModifiersAsync(["chirik"]);
        var userId = Guid.NewGuid();
        await SeedQuizPointsAsync(userId, int.MaxValue);
        await SeedQuizPointsAsync(userId, int.MaxValue);
        using var viewerClient = CreateAuthenticatedClient([AuthRoleCodes.Viewer], userId);

        var state = await viewerClient.GetFromJsonAsync<GameModifierStateDto>(
            "/api/game/modifiers/state"
        );

        Assert.NotNull(state);
        Assert.Equal(int.MaxValue, state.EarnedQuizPoints);
        Assert.Equal(int.MaxValue, state.AvailableQuizPoints);
        Assert.Equal(0, state.SpentQuizPoints);
    }

    [Fact]
    public async Task GetModifierState_WhenQuizRewardExists_UsesRewardOwner()
    {
        await EnsureModifierDefinitionsSeededAsync();
        await SeedActiveGameWithEnabledModifiersAsync(["chirik"]);
        var userId = Guid.NewGuid();
        await SeedQuizRewardPointsAsync(userId, 25);
        using var viewerClient = CreateAuthenticatedClient([AuthRoleCodes.Viewer], userId);

        var state = await viewerClient.GetFromJsonAsync<GameModifierStateDto>(
            "/api/game/modifiers/state"
        );

        Assert.NotNull(state);
        Assert.Equal(25, state.EarnedQuizPoints);
        Assert.Equal(25, state.AvailableQuizPoints);
    }

    [Fact]
    public async Task AdminActivateModifier_WhenAdminTargetsPlayer_UsesSameRulesAndChargesPlayer()
    {
        await EnsureModifierDefinitionsSeededAsync();
        await SeedActiveGameWithEnabledModifiersAsync(["chirik"]);
        var userId = Guid.NewGuid();
        await SeedQuizPointsAsync(userId, 25);
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);

        var response = await adminClient.PostAsJsonAsync(
            "/api/game/modifiers/admin/activate",
            new AdminActivateGameModifierRequestDto(
                ModifierDefinitionSeedIds.Chirik.ToString(),
                userId.ToString()
            )
        );

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var state = await adminClient.GetFromJsonAsync<GameModifierStateDto>(
            $"/api/game/modifiers/admin/state/{userId}"
        );
        Assert.NotNull(state);
        Assert.Equal(22, state.AvailableQuizPoints);
        Assert.Single(state.ActiveModifiers);
        Assert.Equal(userId.ToString(), state.ActiveModifiers[0].ActivatedByUserId);
        Assert.True(Guid.TryParse(state.ActiveModifiers[0].ActivationId, out _));
    }

    [Fact]
    public async Task CancelModifierActivation_WhenAdminAndUnused_RefundsPointsRemovesHistoryAndPublishesRealtime()
    {
        await EnsureModifierDefinitionsSeededAsync();
        await SeedActiveGameWithEnabledModifiersAsync(["chirik"]);
        var userId = Guid.NewGuid();
        await SeedQuizPointsAsync(userId, 25);
        var publisher = new RecordingGameBoardEventsPublisher();
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin], publisher: publisher);
        using var playerClient = CreateAuthenticatedClient([AuthRoleCodes.Viewer], userId);

        var activateResponse = await adminClient.PostAsJsonAsync(
            "/api/game/modifiers/admin/activate",
            new AdminActivateGameModifierRequestDto(
                ModifierDefinitionSeedIds.Chirik.ToString(),
                userId.ToString()
            )
        );
        Assert.Equal(HttpStatusCode.NoContent, activateResponse.StatusCode);

        var stateBeforeCancel = await adminClient.GetFromJsonAsync<GameModifierStateDto>(
            $"/api/game/modifiers/admin/state/{userId}"
        );
        Assert.NotNull(stateBeforeCancel);
        var activation = Assert.Single(stateBeforeCancel.ActiveModifiers);
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var persistedActivationId = await dbContext.GameModifierActivations
            .Where(x => x.ActivatedByUserId == userId && x.ModifierId == ModifierDefinitionSeedIds.Chirik)
            .Select(x => x.Id)
            .SingleAsync();
        Assert.Equal(persistedActivationId.ToString(), activation.ActivationId);

        var staleCancelResponse = await adminClient.PostAsJsonAsync(
            $"/api/game/modifiers/admin/activations/{persistedActivationId}/cancel",
            new CancelGameModifierActivationRequestDto(
                activation.RoundVersion - 1,
                "Stale command"
            )
        );
        Assert.Equal(HttpStatusCode.Conflict, staleCancelResponse.StatusCode);
        var stalePayload = await staleCancelResponse.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(stalePayload);
        Assert.Equal(AppMessages.ErrorCodes.GameRoundStaleVersion, stalePayload.Code);

        var cancelResponse = await adminClient.PostAsJsonAsync(
            $"/api/game/modifiers/admin/activations/{persistedActivationId}/cancel",
            new CancelGameModifierActivationRequestDto(
                activation.RoundVersion,
                "Incorrect proxy activation"
            )
        );

        Assert.Equal(HttpStatusCode.NoContent, cancelResponse.StatusCode);

        var stateAfterCancel = await adminClient.GetFromJsonAsync<GameModifierStateDto>(
            $"/api/game/modifiers/admin/state/{userId}"
        );
        Assert.NotNull(stateAfterCancel);
        Assert.Empty(stateAfterCancel.ActiveModifiers);
        Assert.Equal(25, stateAfterCancel.AvailableQuizPoints);

        var cancelledActivation = await dbContext.GameModifierActivations
            .AsNoTracking()
            .SingleAsync(x => x.Id == persistedActivationId);
        Assert.Equal(GameModifierActivationStatusValue.Cancelled, cancelledActivation.Status);
        Assert.Equal(activation.ActivationCost, cancelledActivation.RefundAmount);
        Assert.NotNull(cancelledActivation.CancelledAtUtc);
        Assert.NotNull(cancelledActivation.CancelledByUserId);
        Assert.Equal("Incorrect proxy activation", cancelledActivation.CancellationReason);

        var cancelledEvent = Assert.Single(publisher.PublishedModifierCancelledEvents);
        Assert.Equal(activation.ActivationId, cancelledEvent.ActivationId.ToString());
        var notificationEvent = Assert.Single(publisher.PublishedUserNotificationEvents);
        Assert.Equal(userId, notificationEvent.UserId);
        Assert.Equal(GameNotificationTypes.ModifierCancelled, notificationEvent.Notification.Type);
        Assert.Equal("Чирик", notificationEvent.Notification.ModifierName);
        Assert.Equal(activation.ActivationCost, notificationEvent.Notification.QuizPointsDelta);

        var notificationsResponse = await playerClient.GetAsync("/api/game/notifications");
        Assert.Equal(HttpStatusCode.OK, notificationsResponse.StatusCode);
        var notifications = await notificationsResponse.Content.ReadFromJsonAsync<IReadOnlyList<GameUserNotificationDto>>();
        Assert.NotNull(notifications);
        var notification = Assert.Single(notifications);
        Assert.Equal(GameNotificationTypes.ModifierCancelled, notification.Type);
        Assert.Equal("Чирик", notification.ModifierName);
        Assert.Equal(activation.ActivationCost, notification.QuizPointsDelta);

        var markReadResponse = await playerClient.PostAsync("/api/game/notifications/read", content: null);
        Assert.Equal(HttpStatusCode.NoContent, markReadResponse.StatusCode);

        var notificationsAfterRead = await playerClient.GetFromJsonAsync<IReadOnlyList<GameUserNotificationDto>>(
            "/api/game/notifications"
        );
        Assert.NotNull(notificationsAfterRead);
        Assert.Empty(notificationsAfterRead);

        var activeGameId = await dbContext.Games
            .Where(x => x.Status == GameStatusValue.Active && !x.IsDeleted)
            .Select(x => x.Id)
            .SingleAsync();
        var historyResponse = await adminClient.GetAsync($"/api/game/history/games/{activeGameId}");
        Assert.Equal(HttpStatusCode.OK, historyResponse.StatusCode);
        var history = await historyResponse.Content.ReadFromJsonAsync<GameHistoryGameDetailsDto>();
        Assert.NotNull(history);
        var historyActivation = Assert.Single(history.MainGame.ModifierActivations);
        Assert.Equal(activation.ActivationId, historyActivation.ActivationId);
        Assert.Equal("cancelled", historyActivation.Status);
        Assert.NotNull(historyActivation.CancelledAtUtc);
        Assert.Equal(activation.ActivationCost, historyActivation.RefundAmount);
    }

    [Fact]
    public async Task SelfCancelModifierActivation_WhenOwnerInAwaiting_RefundsExactlyOnce()
    {
        await EnsureModifierDefinitionsSeededAsync();
        await SeedActiveGameWithEnabledModifiersAsync(["chirik"]);
        var ownerId = Guid.NewGuid();
        var foreignUserId = Guid.NewGuid();
        await SeedQuizPointsAsync(ownerId, 25);
        await SeedQuizPointsAsync(foreignUserId, 1);
        var publisher = new RecordingGameBoardEventsPublisher();
        using var ownerClient = CreateAuthenticatedClient(
            [AuthRoleCodes.Viewer],
            ownerId,
            publisher
        );
        using var foreignClient = CreateAuthenticatedClient([AuthRoleCodes.Viewer], foreignUserId);

        var activateResponse = await ownerClient.PostAsync(
            $"/api/game/modifiers/{ModifierDefinitionSeedIds.Chirik}/activate",
            content: null
        );
        Assert.Equal(HttpStatusCode.NoContent, activateResponse.StatusCode);

        var state = await ownerClient.GetFromJsonAsync<GameModifierStateDto>(
            "/api/game/modifiers/state"
        );
        Assert.NotNull(state);
        var activation = Assert.Single(state.ActiveModifiers);
        var command = new CancelGameModifierActivationRequestDto(activation.RoundVersion);

        var foreignResponse = await foreignClient.PostAsJsonAsync(
            $"/api/game/modifiers/activations/{activation.ActivationId}/self-cancel",
            command
        );
        Assert.Equal(HttpStatusCode.Forbidden, foreignResponse.StatusCode);

        var firstResponse = await ownerClient.PostAsJsonAsync(
            $"/api/game/modifiers/activations/{activation.ActivationId}/self-cancel",
            command
        );
        var repeatedResponse = await ownerClient.PostAsJsonAsync(
            $"/api/game/modifiers/activations/{activation.ActivationId}/self-cancel",
            command
        );

        Assert.Equal(HttpStatusCode.NoContent, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, repeatedResponse.StatusCode);
        Assert.Single(publisher.PublishedModifierCancelledEvents);
        Assert.Single(publisher.PublishedUserNotificationEvents);

        var refundedState = await ownerClient.GetFromJsonAsync<GameModifierStateDto>(
            "/api/game/modifiers/state"
        );
        Assert.NotNull(refundedState);
        Assert.Equal(25, refundedState.AvailableQuizPoints);
        Assert.Equal(0, refundedState.SpentQuizPoints);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var persisted = await dbContext.GameModifierActivations
            .AsNoTracking()
            .SingleAsync(x => x.Id == Guid.Parse(activation.ActivationId));
        Assert.Equal(GameModifierActivationStatusValue.Cancelled, persisted.Status);
        Assert.Equal(persisted.ActivationCostSnapshot, persisted.RefundAmount);
        Assert.Equal(ownerId, persisted.CancelledByUserId);
        Assert.Null(persisted.CancellationReason);
    }

    [Fact]
    public async Task CancelModifierActivation_InPreparing_BlocksOwnerButAllowsAdminWithReason()
    {
        await EnsureModifierDefinitionsSeededAsync();
        await SeedActiveGameWithEnabledModifiersAsync(["chirik"]);
        var ownerId = Guid.NewGuid();
        await SeedQuizPointsAsync(ownerId, 25);
        using var ownerClient = CreateAuthenticatedClient([AuthRoleCodes.Viewer], ownerId);

        Assert.Equal(
            HttpStatusCode.NoContent,
            (await ownerClient.PostAsync(
                $"/api/game/modifiers/{ModifierDefinitionSeedIds.Chirik}/activate",
                content: null
            )).StatusCode
        );
        var state = await ownerClient.GetFromJsonAsync<GameModifierStateDto>(
            "/api/game/modifiers/state"
        );
        Assert.NotNull(state);
        var activation = Assert.Single(state.ActiveModifiers);

        using var moderatorClient = CreateAuthenticatedClient([AuthRoleCodes.Moderator], ownerId);
        var prepareResponse = await moderatorClient.PostAsJsonAsync(
            $"/api/game/rounds/{activation.RoundId}/prepare",
            new GameRoundVersionCommandRequestDto(activation.RoundVersion)
        );
        var prepared = await prepareResponse.Content.ReadFromJsonAsync<GameRoundDetailsDto>();
        Assert.NotNull(prepared);

        var ownerCancelResponse = await ownerClient.PostAsJsonAsync(
            $"/api/game/modifiers/activations/{activation.ActivationId}/self-cancel",
            new CancelGameModifierActivationRequestDto(prepared.RoundVersion)
        );
        Assert.Equal(HttpStatusCode.Conflict, ownerCancelResponse.StatusCode);

        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin], ownerId);
        var missingReasonResponse = await adminClient.PostAsJsonAsync(
            $"/api/game/modifiers/admin/activations/{activation.ActivationId}/cancel",
            new CancelGameModifierActivationRequestDto(prepared.RoundVersion)
        );
        Assert.Equal(HttpStatusCode.BadRequest, missingReasonResponse.StatusCode);
        var missingReasonPayload =
            await missingReasonResponse.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(missingReasonPayload);
        Assert.Equal(
            AppMessages.ErrorCodes.GameModifierActivationCancelReasonRequired,
            missingReasonPayload.Code
        );

        var adminCancelResponse = await adminClient.PostAsJsonAsync(
            $"/api/game/modifiers/admin/activations/{activation.ActivationId}/cancel",
            new CancelGameModifierActivationRequestDto(
                prepared.RoundVersion,
                "Duplicate purchase during preparation"
            )
        );
        Assert.Equal(HttpStatusCode.NoContent, adminCancelResponse.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var round = await dbContext.GameRounds.AsNoTracking().SingleAsync();
        Assert.Equal(GameRoundStatusValue.Preparing, round.Status);
        Assert.Equal(prepared.RoundVersion + 1, round.Version);
    }

    [Fact]
    public async Task CancelModifierActivation_WhenAlreadyConsumed_ReturnsLifecycleConflict()
    {
        var seeded = await SeedModifierHistoryGameAsync();
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);

        int roundVersion;
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            roundVersion = await dbContext.GameRounds
                .Where(x => x.GameId == seeded.GameId)
                .Select(x => x.Version)
                .SingleAsync();
        }

        var response = await adminClient.PostAsJsonAsync(
            $"/api/game/modifiers/admin/activations/{seeded.ActivationId}/cancel",
            new CancelGameModifierActivationRequestDto(roundVersion, "Historical correction")
        );

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.ErrorCodes.GameModifierActivationCancelInvalidState, payload.Code);
    }

    [Fact]
    public async Task ActivateModifier_WhenQuizPointsInsufficient_ReturnsConflictCode()
    {
        await EnsureModifierDefinitionsSeededAsync();
        await SeedActiveGameWithEnabledModifiersAsync(["chirik"]);
        var userId = Guid.NewGuid();
        await SeedQuizPointsAsync(userId, 0);
        using var authenticatedClient = CreateAuthenticatedClient([AuthRoleCodes.Viewer], userId);

        var response = await authenticatedClient.PostAsync(
            $"/api/game/modifiers/{ModifierDefinitionSeedIds.Chirik}/activate",
            content: null
        );

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.ErrorCodes.GameModifierInsufficientQuizPoints, payload.Code);
    }

    [Fact]
    public async Task ActivateModifier_WhenConflictAlreadyActive_ReturnsConflictCode()
    {
        await EnsureModifierDefinitionsSeededAsync();
        await SeedActiveGameWithEnabledModifiersAsync(["prokaznik", "mentorbait"], ["prokaznik"]);
        using var moderatorClient = CreateAuthenticatedClient([AuthRoleCodes.Moderator]);

        var response = await moderatorClient.PostAsync(
            $"/api/game/modifiers/{ModifierDefinitionSeedIds.Mentorbait}/activate",
            content: null
        );

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.ErrorCodes.GameModifierConflictActive, payload.Code);
    }

    [Fact]
    public async Task ActivateModifier_WhenLimitReached_ReturnsConflictCode()
    {
        await EnsureModifierDefinitionsSeededAsync();
        await SeedActiveGameWithEnabledModifiersAsync(["feyerverk"], ["feyerverk"]);
        using var moderatorClient = CreateAuthenticatedClient([AuthRoleCodes.Moderator]);

        var response = await moderatorClient.PostAsync(
            $"/api/game/modifiers/{ModifierDefinitionSeedIds.Feyerverk}/activate",
            content: null
        );

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.ErrorCodes.GameModifierLimitReached, payload.Code);
    }

    [Fact]
    public async Task GetModifierState_WhenNoActiveGame_ReturnsNoContent()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            dbContext.Games.RemoveRange(dbContext.Games);
            await dbContext.SaveChangesAsync();
        }

        using var viewerClient = CreateAuthenticatedClient([AuthRoleCodes.Viewer], Guid.NewGuid());

        var response = await viewerClient.GetAsync("/api/game/modifiers/state");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task GetModifierState_WhenLimitReached_ReturnsBlockedAvailability()
    {
        await EnsureModifierDefinitionsSeededAsync();
        await SeedActiveGameWithEnabledModifiersAsync(["zhazhda"], ["zhazhda", "zhazhda"]);
        var userId = Guid.NewGuid();
        await SeedQuizPointsAsync(userId, 100);
        using var viewerClient = CreateAuthenticatedClient([AuthRoleCodes.Viewer], userId);

        var state = await viewerClient.GetFromJsonAsync<GameModifierStateDto>(
            "/api/game/modifiers/state"
        );

        Assert.NotNull(state);
        var availability = Assert.Single(
            state.AvailableModifiers,
            x => x.Modifier.Id == ModifierDefinitionSeedIds.Zhazhda.ToString()
        );
        Assert.False(availability.CanActivate);
        Assert.Equal("limit_reached", availability.BlockedReason);
        Assert.Equal(2, availability.ActivationsCount);
        Assert.Equal(2, availability.Limit);
        Assert.Equal(2, availability.Modifier.ActivationLimit.Count);
    }

    [Fact]
    public async Task GetModifierState_WhenPreviousRoundModifiersWereArchived_ResetsLimitForNewCycle()
    {
        await EnsureModifierDefinitionsSeededAsync();
        await SeedActiveGameWithEnabledModifiersAsync(["feyerverk"], ["feyerverk"]);

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var activeModifiers = await dbContext.GameModifierActivations
                .Where(x => x.ModifierId == ModifierDefinitionSeedIds.Feyerverk)
                .ToArrayAsync();
            Assert.Single(activeModifiers);
            foreach (var modifier in activeModifiers)
            {
                modifier.Status = GameModifierActivationStatusValue.Consumed;
                modifier.ArchivedAtUtc = DateTime.UtcNow;
            }

            await dbContext.SaveChangesAsync();
        }

        var userId = Guid.NewGuid();
        await SeedQuizPointsAsync(userId, 100);
        using var viewerClient = CreateAuthenticatedClient([AuthRoleCodes.Viewer], userId);

        var state = await viewerClient.GetFromJsonAsync<GameModifierStateDto>(
            "/api/game/modifiers/state"
        );

        Assert.NotNull(state);
        var availability = Assert.Single(
            state.AvailableModifiers,
            x => x.Modifier.Id == ModifierDefinitionSeedIds.Feyerverk.ToString()
        );
        Assert.True(availability.CanActivate);
        Assert.Null(availability.BlockedReason);
        Assert.Equal(0, availability.ActivationsCount);
        Assert.Equal(1, availability.Limit);
    }

    [Fact]
    public async Task ActivateModifier_WhenRoundInProgress_ReturnsOrderingClosedConflict()
    {
        await EnsureModifierDefinitionsSeededAsync();
        var cellId = await SeedSingleCellAsync();

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var row = await dbContext.BoardCells
            .Where(cell => cell.Id == cellId)
            .Select(
                cell =>
                    new
                    {
                        cell.Board.GameId,
                        cell.Board.Game.ActiveTeamId,
                        cell.RowIndex,
                        cell.ColIndex,
                        cell.Title,
                        cell.Cost
                    }
            )
            .SingleAsync();
        Assert.NotNull(row.ActiveTeamId);

        var now = DateTime.UtcNow;
        var cell = await dbContext.BoardCells.SingleAsync(x => x.Id == cellId);
        cell.State = BoardCellState.Open;
        dbContext.GameEnabledModifiers.Add(
            new GameEnabledModifier
            {
                GameId = row.GameId,
                ModifierId = ModifierDefinitionSeedIds.Chirik,
                EnabledAtUtc = now
            }
        );
        dbContext.GameRounds.Add(
            new GameRound
            {
                Id = Guid.NewGuid(),
                GameId = row.GameId,
                BoardId = cell.BoardId,
                BoardCellId = cellId,
                TeamId = row.ActiveTeamId.Value,
                Status = GameRoundStatusValue.InProgress,
                BaseScore = row.Cost,
                TeamSlotIndexSnapshot = 1,
                CellRowIndex = row.RowIndex,
                CellColIndex = row.ColIndex,
                CellTitleSnapshot = row.Title,
                CellCostSnapshot = row.Cost,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            }
        );
        await dbContext.SaveChangesAsync();

        using var moderatorClient = CreateAuthenticatedClient([AuthRoleCodes.Moderator]);
        var response = await moderatorClient.PostAsync(
            $"/api/game/modifiers/{ModifierDefinitionSeedIds.Chirik}/activate",
            content: null
        );

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.ErrorCodes.GameModifierOrderingClosed, payload.Code);
        Assert.Equal(0, await dbContext.GameModifierActivations.CountAsync());
    }

    [Fact]
    public async Task GetQuestionCatalog_WhenAdmin_ReturnsCatalog()
    {
        await SeedQuestionCatalogWithQuestionsAsync(
            [
                new SeedQuestionItem("sample-q-1001", "lore", "Как называется демо вопрос?", "Демо", 1),
                new SeedQuestionItem("sample-q-1002", "locations", "Сколько точек эвакуации на демо карте?", "2", 2)
            ]
        );
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);

        var response = await adminClient.GetAsync("/api/game/questions/catalog");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<IReadOnlyList<GameQuestionCatalogItemDto>>();
        Assert.NotNull(payload);
        Assert.Contains(payload, question => question.QuestionCode == "sample-q-1001");
        Assert.Contains(payload, question => question.QuestionCode == "sample-q-1002");
    }

    [Fact]
    public async Task GetQuestionCategories_WhenAdmin_ReturnsDistinctCategoriesWithQuestionCounts()
    {
        await SeedQuestionCatalogWithQuestionsAsync(
            [
                new SeedQuestionItem("sample-q-1001", "lore", "Как называется демо вопрос?", "Демо", 1),
                new SeedQuestionItem("sample-q-1002", "lore", "Второй вопрос категории?", "Да", 2),
                new SeedQuestionItem("sample-q-1003", "locations", "Сколько точек эвакуации?", "2", 3)
            ]
        );
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);

        var response = await adminClient.GetAsync("/api/game/questions/categories");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload =
            await response.Content.ReadFromJsonAsync<IReadOnlyList<GameQuestionCategoryItemDto>>();
        Assert.NotNull(payload);
        Assert.Contains(payload, category => category.Name == "lore" && category.QuestionCount == 2);
        Assert.Contains(
            payload,
            category => category.Name == "locations" && category.QuestionCount == 1
        );
        Assert.Contains(
            payload,
            category =>
                category.Name == QuestionCatalogDefaults.UncategorizedCategoryName
                && category.IsProtected
        );
        Assert.DoesNotContain(payload, category => category.Name == "lore" && category.IsProtected);
    }

    [Fact]
    public async Task CreateQuestionCategory_WhenAdmin_CreatesCategory()
    {
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);
        var categoryName = $"История-{Guid.NewGuid():N}";

        var response = await adminClient.PostAsJsonAsync(
            "/api/game/questions/categories",
            new CreateGameQuestionCategoryRequestDto(categoryName)
        );

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<GameQuestionCategoryItemDto>();
        Assert.NotNull(payload);
        Assert.Equal(categoryName, payload.Name);
        Assert.Equal(0, payload.QuestionCount);
    }

    [Fact]
    public async Task DeleteQuestionCategory_WhenEmpty_DeletesCategory()
    {
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);

        var createResponse = await adminClient.PostAsJsonAsync(
            "/api/game/questions/categories",
            new CreateGameQuestionCategoryRequestDto("Удаляемая категория")
        );
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<GameQuestionCategoryItemDto>();
        Assert.NotNull(created);

        var deleteResponse = await adminClient.DeleteAsync(
            $"/api/game/questions/categories/{created.Id}"
        );

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var categoriesResponse = await adminClient.GetAsync("/api/game/questions/categories");
        Assert.Equal(HttpStatusCode.OK, categoriesResponse.StatusCode);
        var categories =
            await categoriesResponse.Content.ReadFromJsonAsync<IReadOnlyList<GameQuestionCategoryItemDto>>();
        Assert.NotNull(categories);
        Assert.DoesNotContain(categories, category => category.Id == created.Id);
    }

    [Fact]
    public async Task UpdateQuestionCategory_WhenAdmin_RenamesCategory()
    {
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);

        var createResponse = await adminClient.PostAsJsonAsync(
            "/api/game/questions/categories",
            new CreateGameQuestionCategoryRequestDto("Старая категория")
        );
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<GameQuestionCategoryItemDto>();
        Assert.NotNull(created);

        var updateResponse = await adminClient.PutAsJsonAsync(
            $"/api/game/questions/categories/{created.Id}",
            new CreateGameQuestionCategoryRequestDto("Новая категория")
        );

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<GameQuestionCategoryItemDto>();
        Assert.NotNull(updated);
        Assert.Equal(created.Id, updated.Id);
        Assert.Equal("Новая категория", updated.Name);
    }

    [Fact]
    public async Task UpdateQuestionCategory_WhenFallbackCategory_ReturnsProtectedConflict()
    {
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);

        var categoriesResponse = await adminClient.GetAsync("/api/game/questions/categories");
        var categories =
            await categoriesResponse.Content.ReadFromJsonAsync<IReadOnlyList<GameQuestionCategoryItemDto>>();
        Assert.NotNull(categories);
        var fallback = categories.Single(category => category.Name == QuestionCatalogDefaults.UncategorizedCategoryName);

        var updateResponse = await adminClient.PutAsJsonAsync(
            $"/api/game/questions/categories/{fallback.Id}",
            new CreateGameQuestionCategoryRequestDto("Переименованная системная категория")
        );

        Assert.Equal(HttpStatusCode.Conflict, updateResponse.StatusCode);
        var payload = await updateResponse.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.ErrorCodes.GameQuestionCategoryProtected, payload.Code);
    }

    [Fact]
    public async Task DeleteQuestionCategory_WhenFallbackCategory_ReturnsProtectedConflict()
    {
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);

        var categoriesResponse = await adminClient.GetAsync("/api/game/questions/categories");
        var categories =
            await categoriesResponse.Content.ReadFromJsonAsync<IReadOnlyList<GameQuestionCategoryItemDto>>();
        Assert.NotNull(categories);
        var fallback = categories.Single(category => category.Name == QuestionCatalogDefaults.UncategorizedCategoryName);

        var deleteResponse = await adminClient.DeleteAsync(
            $"/api/game/questions/categories/{fallback.Id}"
        );

        Assert.Equal(HttpStatusCode.Conflict, deleteResponse.StatusCode);
        var payload = await deleteResponse.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.ErrorCodes.GameQuestionCategoryProtected, payload.Code);
    }

    [Fact]
    public async Task DownloadQuestionImportTemplate_WhenAdmin_ReturnsCurrentCategoriesAndCommentedExample()
    {
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);
        var categoryName = $"История-{Guid.NewGuid():N}";
        await adminClient.PostAsJsonAsync(
            "/api/game/questions/categories",
            new CreateGameQuestionCategoryRequestDto(categoryName)
        );

        var response = await adminClient.GetAsync("/api/game/questions/import-template");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("// Required fields for each question: text, answer, reward.", content);
        Assert.Contains("// Available categories:", content);
        Assert.Contains("(БЕЗ КАТЕГОРИИ)", content);
        Assert.Contains($"({categoryName})", content);
        Assert.Contains("// Example:", content);
        Assert.Contains("\"questions\": [", content);
    }

    [Fact]
    public async Task DownloadQuestionImportTemplate_WhenRussianLocaleRequested_ReturnsRussianComments()
    {
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);

        var response = await adminClient.GetAsync("/api/game/questions/import-template?locale=ru");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("// Шаблон JSONC для массового импорта вопросов.", content);
        Assert.Contains("// Обязательные поля у вопроса: text, answer, reward.", content);
        Assert.Contains(QuestionCatalogDefaults.UncategorizedCategoryName, content);
    }

    [Fact]
    public async Task ImportQuestions_WhenOptionalFieldsAreMissing_UsesFallbackCategoryAndDefaults()
    {
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);

        var jsonc = $$"""
        {
          "questions": [
            {
              "text": "Импортированный вопрос?",
              "answer": "Да",
              "reward": 100,
              "externalCode": "import-q-1001"
            },
            {
              "text": "Вопрос с плохой категорией?",
              "answer": "Тоже да",
              "reward": 50,
              "categoryId": "not-a-guid",
              "externalCode": "import-q-1002"
            },
            {
              "text": "Вопрос без ответа",
              "reward": 10,
              "externalCode": "import-q-1003"
            }
          ]
        }
        """;

        var importResponse = await adminClient.PostAsync(
            "/api/game/questions/import",
            CreateJsonImportContent(jsonc, "questions.jsonc")
        );

        Assert.Equal(HttpStatusCode.OK, importResponse.StatusCode);
        var payload = await importResponse.Content.ReadFromJsonAsync<ImportGameQuestionsResultDto>();
        Assert.NotNull(payload);
        Assert.Equal(2, payload.ImportedCount);
        Assert.Single(payload.SkippedQuestions);
        Assert.Equal(3, payload.SkippedQuestions[0].RowNumber);
        Assert.Equal("Вопрос без ответа", payload.SkippedQuestions[0].QuestionText);
        Assert.Equal(
            AppMessages.ErrorCodes.GameQuestionImportInvalidFields,
            payload.SkippedQuestions[0].ReasonCode
        );
        Assert.Equal(
            "Missing or invalid required fields. Each question must include text, answer, and a non-negative reward.",
            payload.SkippedQuestions[0].Reason
        );
        var skippedSourceQuestion = Assert.IsType<ImportGameQuestionSourceDto>(
            payload.SkippedQuestions[0].SourceQuestion
        );
        Assert.Equal("Вопрос без ответа", skippedSourceQuestion.Text);
        Assert.Null(skippedSourceQuestion.Answer);
        Assert.Equal("import-q-1003", skippedSourceQuestion.ExternalCode);

        var catalogResponse = await adminClient.GetAsync("/api/game/questions/catalog");
        var catalog = await catalogResponse.Content.ReadFromJsonAsync<IReadOnlyList<GameQuestionCatalogItemDto>>();
        Assert.NotNull(catalog);
        var imported = Assert.Single(catalog, question => question.QuestionCode == "import-q-1001");
        Assert.Equal(QuestionCatalogDefaults.UncategorizedCategoryName, imported.CategoryName);
        Assert.False(imported.IsEnabled);
        Assert.Equal(0, imported.Priority);
        var importedWithBadCategory = Assert.Single(catalog, question => question.QuestionCode == "import-q-1002");
        Assert.Equal(QuestionCatalogDefaults.UncategorizedCategoryName, importedWithBadCategory.CategoryName);
        Assert.DoesNotContain(catalog, question => question.QuestionCode == "import-q-1003");
    }

    [Fact]
    public async Task ImportQuestions_WhenPayloadIsInvalidJson_ReturnsBadRequest()
    {
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);
        const string invalidJson = "{ \"questions\": [ { \"text\": \"Broken\" ";

        var response = await adminClient.PostAsync(
            "/api/game/questions/import",
            CreateJsonImportContent(invalidJson, "questions.jsonc")
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.ErrorCodes.GameQuestionInvalidRequest, payload.Code);
        Assert.Equal("The import file is not valid JSON/JSONC.", payload.Error);
    }

    [Fact]
    public async Task ImportQuestions_WhenFileExceedsLimit_ReturnsBadRequest()
    {
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);
        var oversizedJson = new string(' ', (int)GameQuestionImportLimits.MaxUploadBytes + 1);

        var response = await adminClient.PostAsync(
            "/api/game/questions/import",
            CreateJsonImportContent(oversizedJson, "questions.jsonc")
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json; charset=utf-8", response.Content.Headers.ContentType?.ToString());
    }

    [Fact]
    public async Task DeleteQuestionCategory_WhenContainsQuestions_ReturnsConflictCode()
    {
        await SeedQuestionCatalogWithQuestionsAsync(
            [new SeedQuestionItem("keep-q-1001", "lore", "Удалять категорию нельзя?", "Да", 1)]
        );
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);

        var categoriesResponse = await adminClient.GetAsync("/api/game/questions/categories");
        Assert.Equal(HttpStatusCode.OK, categoriesResponse.StatusCode);
        var categories =
            await categoriesResponse.Content.ReadFromJsonAsync<IReadOnlyList<GameQuestionCategoryItemDto>>();
        Assert.NotNull(categories);
        var loreCategoryId = categories.Single(category => category.Name == "lore").Id;

        var deleteResponse = await adminClient.DeleteAsync(
            $"/api/game/questions/categories/{loreCategoryId}"
        );

        Assert.Equal(HttpStatusCode.Conflict, deleteResponse.StatusCode);
        var payload = await deleteResponse.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.ErrorCodes.GameQuestionCategoryNotEmpty, payload.Code);
    }

    [Fact]
    public async Task SetQuestionCategoryEnabled_WhenCategoryMissing_ReturnsNotFoundCode()
    {
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);

        var response = await adminClient.PatchAsJsonAsync(
            $"/api/game/questions/categories/{Guid.NewGuid()}/enabled",
            new SetGameQuestionCategoryEnabledRequestDto(false)
        );

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.ErrorCodes.GameQuestionCategoryNotFound, payload.Code);
    }

    [Fact]
    public async Task SetQuestionCategoryEnabled_WhenCategoryExists_UpdatesMatchingQuestions()
    {
        await SeedQuestionCatalogWithQuestionsAsync(
            [
                new SeedQuestionItem("sample-q-1001", "lore", "Как называется демо вопрос?", "Демо", 1),
                new SeedQuestionItem("sample-q-1002", "lore", "Второй вопрос категории?", "Да", 2),
                new SeedQuestionItem("sample-q-1003", "locations", "Сколько точек эвакуации?", "2", 3)
            ]
        );
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);

        var categoriesResponse = await adminClient.GetAsync("/api/game/questions/categories");
        var categories =
            await categoriesResponse.Content.ReadFromJsonAsync<IReadOnlyList<GameQuestionCategoryItemDto>>();
        Assert.NotNull(categories);
        var loreCategoryId = categories.Single(category => category.Name == "lore").Id;

        var updateResponse = await adminClient.PatchAsJsonAsync(
            $"/api/game/questions/categories/{loreCategoryId}/enabled",
            new SetGameQuestionCategoryEnabledRequestDto(false)
        );

        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        var catalogResponse = await adminClient.GetAsync("/api/game/questions/catalog");
        Assert.Equal(HttpStatusCode.OK, catalogResponse.StatusCode);
        var catalog =
            await catalogResponse.Content.ReadFromJsonAsync<IReadOnlyList<GameQuestionCatalogItemDto>>();
        Assert.NotNull(catalog);
        Assert.All(
            catalog.Where(question => question.CategoryName == "lore"),
            question => Assert.False(question.IsEnabled)
        );
        Assert.All(
            catalog.Where(question => question.CategoryName == "locations"),
            question => Assert.True(question.IsEnabled)
        );
    }

    [Fact]
    public async Task DeleteQuestion_WhenAdmin_SoftDeletesAndHidesFromCatalog()
    {
        await SeedQuestionCatalogWithQuestionsAsync(
            [new SeedQuestionItem("delete-q-1001", "lore", "Вопрос для удаления?", "Да", 1)]
        );
        using var adminClient = CreateAuthenticatedClient([AuthRoleCodes.Admin]);

        var catalogResponse = await adminClient.GetAsync("/api/game/questions/catalog");
        Assert.Equal(HttpStatusCode.OK, catalogResponse.StatusCode);
        var catalog = await catalogResponse.Content.ReadFromJsonAsync<IReadOnlyList<GameQuestionCatalogItemDto>>();
        var question = Assert.Single(catalog!, x => x.QuestionCode == "delete-q-1001");

        var deleteResponse = await adminClient.DeleteAsync($"/api/game/questions/{question.QuestionId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var afterDeleteResponse = await adminClient.GetAsync("/api/game/questions/catalog");
        Assert.Equal(HttpStatusCode.OK, afterDeleteResponse.StatusCode);
        var afterDeleteCatalog =
            await afterDeleteResponse.Content.ReadFromJsonAsync<IReadOnlyList<GameQuestionCatalogItemDto>>();
        Assert.DoesNotContain(afterDeleteCatalog!, x => x.QuestionCode == "delete-q-1001");
    }

    [Fact]
    public async Task AskNextQuestion_WhenOnlySingleQuestionAvailable_SecondCallReturnsNotFound()
    {
        await SeedActiveGameForQuestionsAsync();
        await SeedQuestionCatalogWithQuestionsAsync(
            [new SeedQuestionItem("single-q-0001", "lore", "Одиночный вопрос?", "Да", 2)]
        );
        using var moderatorClient = CreateAuthenticatedClient([AuthRoleCodes.Moderator]);

        var firstResponse = await moderatorClient.PostAsync("/api/game/quiz/questions/ask-next", content: null);
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        var secondResponse = await moderatorClient.PostAsync("/api/game/quiz/questions/ask-next", content: null);
        Assert.Equal(HttpStatusCode.NotFound, secondResponse.StatusCode);
        var payload = await secondResponse.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.ErrorCodes.GameQuizNoAvailableQuestions, payload.Code);
    }

    [Fact]
    public async Task AskNextQuestion_WhenLeastUsedQuestionsExist_PicksHighestPriorityAmongThem()
    {
        await SeedActiveGameForQuestionsAsync();
        await SeedQuestionCatalogWithQuestionsAsync(
            [
                new SeedQuestionItem("priority-q-0001", "lore", "Более использованный вопрос?", "Да", 1, -100),
                new SeedQuestionItem("priority-q-0002", "lore", "Высокий приоритет?", "Да", 1, 10),
                new SeedQuestionItem("priority-q-0003", "lore", "Низкий приоритет?", "Да", 1, 1)
            ]
        );

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var moreUsedQuestion = await dbContext.QuestionDefinitions.SingleAsync(
                question => question.ExternalCode == "priority-q-0001"
            );
            var now = DateTime.UtcNow;
            var historicalGameId = Guid.NewGuid();
            dbContext.Games.Add(
                new Game
                {
                    Id = historicalGameId,
                    Title = "Historical quiz game",
                    Status = GameStatusValue.Finished,
                    CreatedAtUtc = now.AddDays(-1),
                    FinishedAtUtc = now.AddDays(-1)
                }
            );
            dbContext.GameQuizRounds.Add(
                new GameQuizRound
                {
                    Id = Guid.NewGuid(),
                    GameId = historicalGameId,
                    QuestionId = moreUsedQuestion.Id,
                    AskOrder = 1,
                    AskedAtUtc = now.AddDays(-1),
                    ClosesAtUtc = now.AddDays(-1).AddMinutes(1),
                    ClosedAtUtc = now.AddDays(-1).AddMinutes(1),
                    Status = GameQuizRoundStatusValue.Timeout,
                    QuestionRevisionSnapshot = moreUsedQuestion.Revision,
                    QuestionCodeSnapshot = moreUsedQuestion.ExternalCode,
                    CategoryNameSnapshot = "lore",
                    QuestionTextSnapshot = moreUsedQuestion.Text,
                    AcceptedAnswersSnapshot = ["Да"],
                    NormalizedAnswersSnapshot = ["да"],
                    RewardSnapshot = moreUsedQuestion.Reward,
                    DeliveryKind = "manual"
                }
            );
            await dbContext.SaveChangesAsync();
        }

        using var moderatorClient = CreateAuthenticatedClient([AuthRoleCodes.Moderator]);

        var response = await moderatorClient.PostAsync("/api/game/quiz/questions/ask-next", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var asked = await response.Content.ReadFromJsonAsync<AskedQuizQuestionDto>();
        Assert.NotNull(asked);
        Assert.Equal("priority-q-0002", asked.QuestionCode);
    }

    [Fact]
    public async Task AnswerQuizRound_WhenAnswerCorrect_ReturnsAnsweredCorrectWithPoints()
    {
        await SeedActiveGameForQuestionsAsync();
        await SeedQuestionCatalogWithQuestionsAsync(
            [new SeedQuestionItem("answer-q-0001", "stats", "Сколько будет 1+1?", "2", 3)]
        );
        var moderatorId = Guid.NewGuid();
        await SeedActiveUserAsync(moderatorId, "quiz-moderator", "Integration Tester");
        var publisher = new RecordingGameBoardEventsPublisher();
        using var moderatorClient = CreateAuthenticatedClient(
            [AuthRoleCodes.Moderator],
            userId: moderatorId,
            publisher: publisher
        );

        var askResponse = await moderatorClient.PostAsync("/api/game/quiz/questions/ask-next", content: null);
        Assert.Equal(HttpStatusCode.OK, askResponse.StatusCode);
        var asked = await askResponse.Content.ReadFromJsonAsync<AskedQuizQuestionDto>();
        Assert.NotNull(asked);
        var askEvent = Assert.Single(publisher.PublishedQuizStateChangedEvents);
        Assert.Equal(GameQuizStateChangeKinds.QuestionAsked, askEvent.ChangeKind);
        Assert.Equal(asked.GameId, askEvent.GameId.ToString());

        var answerResponse = await moderatorClient.PostAsJsonAsync(
            $"/api/game/quiz/rounds/{asked.RoundId}/answer",
            new AnswerQuizRoundRequestDto("2", "Integration Tester", null)
        );

        Assert.Equal(HttpStatusCode.OK, answerResponse.StatusCode);
        var answered = await answerResponse.Content.ReadFromJsonAsync<GameQuizRoundSummaryDto>();
        Assert.NotNull(answered);
        Assert.Equal("answered_correct", answered.Status);
        Assert.True(answered.IsCorrect);
        Assert.Equal(3, answered.AwardedPoints);
        Assert.Equal("Integration Tester", answered.AnsweredByDisplayName);
        Assert.Equal(2, publisher.PublishedQuizStateChangedEvents.Count);
        Assert.Equal(
            GameQuizStateChangeKinds.QuestionAnswered,
            publisher.PublishedQuizStateChangedEvents[1].ChangeKind
        );
    }

    [Fact]
    public async Task AnswerQuizRound_WhenCreditedPlayerDoesNotExist_ReturnsNotFoundWithoutMutation()
    {
        await SeedActiveGameForQuestionsAsync();
        await SeedQuestionCatalogWithQuestionsAsync(
            [new SeedQuestionItem("missing-player-q-0001", "stats", "Сколько будет 3+3?", "6", 3)]
        );
        var moderatorId = Guid.NewGuid();
        await SeedActiveUserAsync(moderatorId, "missing-player-moderator", "Moderator");
        using var moderatorClient = CreateAuthenticatedClient(
            [AuthRoleCodes.Moderator],
            userId: moderatorId
        );

        var askResponse = await moderatorClient.PostAsync(
            "/api/game/quiz/questions/ask-next",
            content: null
        );
        var asked = await askResponse.Content.ReadFromJsonAsync<AskedQuizQuestionDto>();
        Assert.NotNull(asked);

        var answerResponse = await moderatorClient.PostAsJsonAsync(
            $"/api/game/quiz/rounds/{asked.RoundId}/answer",
            new AnswerQuizRoundRequestDto("6", "Missing Player", Guid.NewGuid().ToString())
        );

        Assert.Equal(HttpStatusCode.NotFound, answerResponse.StatusCode);
        var error = await answerResponse.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal(AppMessages.ErrorCodes.GameQuizAnswerPlayerNotFound, error?.Code);

        using var verificationScope = _factory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var roundId = Guid.Parse(asked.RoundId);
        var persistedRound = await verificationDb.GameQuizRounds.SingleAsync(x => x.Id == roundId);
        Assert.Equal(GameQuizRoundStatusValue.Asked, persistedRound.Status);
        Assert.False(await verificationDb.GameQuizCorrectAnswers.AnyAsync(x => x.QuizRoundId == roundId));
        Assert.False(await verificationDb.GameQuizPointLedgerEntries.AnyAsync(x => x.GameId == persistedRound.GameId));
    }

    [Fact]
    public async Task AnswerQuizRound_WhenRoundBelongsToFinishedGame_ReturnsNotFoundWithoutMutation()
    {
        await SeedActiveGameForQuestionsAsync();
        await SeedQuestionCatalogWithQuestionsAsync(
            [new SeedQuestionItem("finished-q-0001", "stats", "Сколько будет 2+2?", "4", 3)]
        );
        using var moderatorClient = CreateAuthenticatedClient([AuthRoleCodes.Moderator]);

        var askResponse = await moderatorClient.PostAsync(
            "/api/game/quiz/questions/ask-next",
            content: null
        );
        var asked = await askResponse.Content.ReadFromJsonAsync<AskedQuizQuestionDto>();
        Assert.NotNull(asked);

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var gameId = Guid.Parse(asked.GameId);
            var game = await dbContext.Games.SingleAsync(candidate => candidate.Id == gameId);
            game.Status = GameStatusValue.Finished;
            game.FinishedAtUtc = DateTime.UtcNow;
            await dbContext.SaveChangesAsync();
        }

        var answerResponse = await moderatorClient.PostAsJsonAsync(
            $"/api/game/quiz/rounds/{asked.RoundId}/answer",
            new AnswerQuizRoundRequestDto("4", "Integration Tester", null)
        );

        Assert.Equal(HttpStatusCode.NotFound, answerResponse.StatusCode);
        using var verificationScope = _factory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var persistedRound = await verificationDb.GameQuizRounds.SingleAsync(
            candidate => candidate.Id == Guid.Parse(asked.RoundId)
        );
        Assert.Equal(GameQuizRoundStatusValue.Asked, persistedRound.Status);
        Assert.False(await verificationDb.GameQuizCorrectAnswers.AnyAsync(
            answer => answer.QuizRoundId == persistedRound.Id
        ));
    }

    [Fact]
    public async Task AwardManualQuizPoints_WhenAwarded_PublishesQuizRealtimeEvent()
    {
        await SeedActiveGameForQuestionsAsync();
        var playerId = Guid.NewGuid();
        var moderatorId = Guid.NewGuid();
        await SeedActiveUserAsync(playerId, "manual-award-player", "Manual Award Player");
        await SeedActiveUserAsync(moderatorId, "manual-award-moderator", "Manual Award Moderator");
        var publisher = new RecordingGameBoardEventsPublisher();
        using var moderatorClient = CreateAuthenticatedClient(
            [AuthRoleCodes.Moderator],
            userId: moderatorId,
            publisher: publisher
        );

        var response = await moderatorClient.PostAsJsonAsync(
            "/api/game/quiz/manual-awards",
            new ManualQuizAwardRequestDto(
                playerId.ToString(),
                GameQuizManualAdjustmentOperationValue.Award,
                5,
                "Moderator correction",
                Guid.NewGuid().ToString()
            )
        );

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ManualQuizAwardSummaryDto>();
        Assert.NotNull(payload);
        Assert.Equal(playerId.ToString(), payload.AwardedToUserId);

        var quizEvent = Assert.Single(publisher.PublishedQuizStateChangedEvents);
        Assert.Equal(payload.GameId, quizEvent.GameId.ToString());
        Assert.Equal(GameQuizStateChangeKinds.ManualAdjustmentApplied, quizEvent.ChangeKind);

        var players = await moderatorClient.GetFromJsonAsync<ManualQuizAwardPlayerDto[]>(
            "/api/game/quiz/manual-awards/players"
        );
        var playerBalance = Assert.Single(players!, x => x.UserId == playerId.ToString());
        Assert.Equal(5, playerBalance.EarnedQuizPoints);
        Assert.Equal(0, playerBalance.SpentQuizPoints);
        Assert.Equal(5, playerBalance.AvailableQuizPoints);

        var deductionRequestId = Guid.NewGuid().ToString();
        var deductionRequest = new ManualQuizAwardRequestDto(
            playerId.ToString(),
            GameQuizManualAdjustmentOperationValue.Deduct,
            3,
            "Fix duplicate moderator award",
            deductionRequestId
        );
        var deductionResponse = await moderatorClient.PostAsJsonAsync(
            "/api/game/quiz/manual-awards",
            deductionRequest
        );
        Assert.Equal(HttpStatusCode.Created, deductionResponse.StatusCode);
        var deduction = await deductionResponse.Content.ReadFromJsonAsync<ManualQuizAwardSummaryDto>();
        Assert.NotNull(deduction);
        Assert.Equal(-3, deduction.PointsDelta);
        Assert.Equal(5, deduction.AvailablePointsBefore);
        Assert.Equal(2, deduction.AvailablePointsAfter);
        Assert.Equal("Fix duplicate moderator award", deduction.Reason);
        Assert.Equal(deductionRequestId, deduction.RequestId);

        using (var verificationScope = _factory.Services.CreateScope())
        {
            var verificationDb = verificationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var persistedAdjustment = await verificationDb.GameQuizPointLedgerEntries
                .AsNoTracking()
                .SingleAsync(x => x.ManualRequestId == Guid.Parse(deductionRequestId));
            Assert.Equal(
                GameQuizPointEntryTypeValue.ManualAdjustment,
                persistedAdjustment.EntryType
            );
            Assert.Equal(-3, persistedAdjustment.PointsDelta);
            Assert.Equal("Fix duplicate moderator award", persistedAdjustment.Reason);
        }

        var replayResponse = await moderatorClient.PostAsJsonAsync(
            "/api/game/quiz/manual-awards",
            deductionRequest
        );
        Assert.Equal(HttpStatusCode.Created, replayResponse.StatusCode);
        var replay = await replayResponse.Content.ReadFromJsonAsync<ManualQuizAwardSummaryDto>();
        Assert.Equal(deduction.AwardId, replay?.AwardId);
        Assert.Equal(2, publisher.PublishedQuizStateChangedEvents.Count);

        var excessiveDeductionResponse = await moderatorClient.PostAsJsonAsync(
            "/api/game/quiz/manual-awards",
            deductionRequest with
            {
                Points = 3,
                RequestId = Guid.NewGuid().ToString()
            }
        );
        Assert.Equal(HttpStatusCode.Conflict, excessiveDeductionResponse.StatusCode);
        Assert.Equal(
            AppMessages.ErrorCodes.GameQuizManualAwardInsufficientPoints,
            (await excessiveDeductionResponse.Content.ReadFromJsonAsync<ErrorResponse>())?.Code
        );

        var history = await moderatorClient.GetFromJsonAsync<GameHistoryGameDetailsDto>(
            $"/api/game/history/games/{payload.GameId}"
        );
        Assert.NotNull(history);
        Assert.Collection(
            history.Quiz.ManualAwards.OrderBy(x => x.AwardedAtUtc),
            award => Assert.Equal(GameQuizManualAdjustmentOperationValue.Award, award.OperationType),
            adjustment =>
            {
                Assert.Equal(GameQuizManualAdjustmentOperationValue.Deduct, adjustment.OperationType);
                Assert.Equal(-3, adjustment.AwardedPoints);
                Assert.Equal("Fix duplicate moderator award", adjustment.Reason);
            }
        );
    }

    private async Task AssertRepositoryFallbackAsync(Guid finishedGameId)
    {
        using var scope = _factory.Services.CreateScope();
        var gameBoardService = scope.ServiceProvider.GetRequiredService<IGameBoardService>();

        var snapshot = await gameBoardService.GetCurrentBoardAsync();

        Assert.NotNull(snapshot);
        Assert.Equal(finishedGameId.ToString(), snapshot.GameId);
    }

    private async Task<Guid> SeedGamesAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        dbContext.GameModifierActivations.RemoveRange(dbContext.GameModifierActivations);
        dbContext.GameEnabledModifiers.RemoveRange(dbContext.GameEnabledModifiers);
        dbContext.BoardCells.RemoveRange(dbContext.BoardCells);
        dbContext.GameBoards.RemoveRange(dbContext.GameBoards);
        dbContext.Games.RemoveRange(dbContext.Games);
        await dbContext.SaveChangesAsync();

        var now = DateTime.UtcNow;
        var finishedGameId = Guid.NewGuid();
        var boardId = Guid.NewGuid();

        dbContext.Games.AddRange(
            new Game
            {
                Id = Guid.NewGuid(),
                Title = "Active without board",
                Status = GameStatusValue.Active,
                CreatedAtUtc = now,
                StartedAtUtc = now
            },
            new Game
            {
                Id = finishedGameId,
                Title = "Finished with board",
                Status = GameStatusValue.Finished,
                CreatedAtUtc = now.AddHours(-2),
                FinishedAtUtc = now.AddHours(-1)
            }
        );

        dbContext.GameBoards.Add(
            new GameBoard
            {
                Id = boardId,
                GameId = finishedGameId,
                Rows = 1,
                Cols = 1,
                RowLabels = ["A"],
                ColLabels = ["1"],
                CreatedAtUtc = now.AddHours(-1)
            }
        );

        dbContext.BoardCells.Add(
            new BoardCell
            {
                Id = Guid.NewGuid(),
                BoardId = boardId,
                RowIndex = 0,
                ColIndex = 0,
                Title = "Cell",
                Cost = 100,
                State = BoardCellState.Closed
            }
        );

        await dbContext.SaveChangesAsync();
        return finishedGameId;
    }

    private async Task<Guid> SeedSingleCellAsync(bool selectActiveTeam = true, string? teamName = null)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        dbContext.GameModifierActivations.RemoveRange(dbContext.GameModifierActivations);
        dbContext.GameEnabledModifiers.RemoveRange(dbContext.GameEnabledModifiers);
        dbContext.GameRoundModifierResults.RemoveRange(dbContext.GameRoundModifierResults);
        dbContext.GameRoundParticipants.RemoveRange(dbContext.GameRoundParticipants);
        dbContext.GameRounds.RemoveRange(dbContext.GameRounds);
        dbContext.GameTeamMembers.RemoveRange(dbContext.GameTeamMembers);
        dbContext.GameTeams.RemoveRange(dbContext.GameTeams);
        dbContext.GameTeamSlots.RemoveRange(dbContext.GameTeamSlots);
        dbContext.BoardCells.RemoveRange(dbContext.BoardCells);
        dbContext.GameBoards.RemoveRange(dbContext.GameBoards);
        dbContext.Games.RemoveRange(dbContext.Games);
        dbContext.Users.RemoveRange(dbContext.Users);
        await dbContext.SaveChangesAsync();

        var now = DateTime.UtcNow;
        var gameId = Guid.NewGuid();
        var boardId = Guid.NewGuid();
        var cellId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        dbContext.Games.Add(
            new Game
            {
                Id = gameId,
                Title = "Game",
                Status = GameStatusValue.Active,
                ActiveTeamId = selectActiveTeam ? teamId : null,
                CreatedAtUtc = now,
                StartedAtUtc = now
            }
        );

        dbContext.Users.Add(
            new User
            {
                Id = userId,
                TwitchUserId = $"single-cell-user-{userId:N}",
                Login = "single-cell-user",
                DisplayName = "Single Cell Player",
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            }
        );

        dbContext.GameTeamSlots.Add(
            new GameTeamSlot
            {
                Id = slotId,
                GameId = gameId,
                SlotIndex = 1,
                SlotType = TeamSlotTypeValue.Public,
                CreatedAtUtc = now
            }
        );

        dbContext.GameTeams.Add(
            new GameTeam
            {
                Id = teamId,
                GameId = gameId,
                SlotId = slotId,
                Name = teamName,
                RecruitmentOpen = false,
                Status = TeamStatusValue.Confirmed,
                CreatedByUserId = userId,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                ConfirmedAtUtc = now,
                ConfirmedByUserId = userId
            }
        );

        dbContext.GameTeamMembers.Add(
            new GameTeamMember
            {
                Id = Guid.NewGuid(),
                GameId = gameId,
                TeamId = teamId,
                UserId = userId,
                JoinedAtUtc = now
            }
        );

        dbContext.GameBoards.Add(
            new GameBoard
            {
                Id = boardId,
                GameId = gameId,
                Rows = 1,
                Cols = 1,
                RowLabels = ["A"],
                ColLabels = ["1"],
                CreatedAtUtc = now
            }
        );

        dbContext.BoardCells.Add(
            new BoardCell
            {
                Id = cellId,
                BoardId = boardId,
                RowIndex = 0,
                ColIndex = 0,
                Title = "Cell",
                Cost = 100,
                State = BoardCellState.Closed
            }
        );

        await dbContext.SaveChangesAsync();
        return cellId;
    }

    private async Task<Guid> SeedTwoActiveGamesAndReturnLatestTeamIdAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        dbContext.GameModifierActivations.RemoveRange(dbContext.GameModifierActivations);
        dbContext.GameEnabledModifiers.RemoveRange(dbContext.GameEnabledModifiers);
        dbContext.GameRoundModifierResults.RemoveRange(dbContext.GameRoundModifierResults);
        dbContext.GameRoundParticipants.RemoveRange(dbContext.GameRoundParticipants);
        dbContext.GameRounds.RemoveRange(dbContext.GameRounds);
        dbContext.GameTeamMembers.RemoveRange(dbContext.GameTeamMembers);
        dbContext.GameTeams.RemoveRange(dbContext.GameTeams);
        dbContext.GameTeamSlots.RemoveRange(dbContext.GameTeamSlots);
        dbContext.BoardCells.RemoveRange(dbContext.BoardCells);
        dbContext.GameBoards.RemoveRange(dbContext.GameBoards);
        dbContext.Games.RemoveRange(dbContext.Games);
        dbContext.Users.RemoveRange(dbContext.Users);
        await dbContext.SaveChangesAsync();

        var now = DateTime.UtcNow;
        var olderGameId = Guid.NewGuid();
        var olderSlotId = Guid.NewGuid();
        var olderTeamId = Guid.NewGuid();
        var olderUserId = Guid.NewGuid();
        var olderBoardId = Guid.NewGuid();

        var latestGameId = Guid.NewGuid();
        var latestSlotId = Guid.NewGuid();
        var latestTeamId = Guid.NewGuid();
        var latestUserId = Guid.NewGuid();
        var latestBoardId = Guid.NewGuid();

        dbContext.Users.AddRange(
            new User
            {
                Id = olderUserId,
                TwitchUserId = $"older-active-{olderUserId:N}",
                Login = "older-active-user",
                DisplayName = "Older Active User",
                IsActive = true,
                CreatedAtUtc = now.AddMinutes(-20),
                UpdatedAtUtc = now.AddMinutes(-20)
            },
            new User
            {
                Id = latestUserId,
                TwitchUserId = $"latest-active-{latestUserId:N}",
                Login = "latest-active-user",
                DisplayName = "Latest Active User",
                IsActive = true,
                CreatedAtUtc = now.AddMinutes(-10),
                UpdatedAtUtc = now.AddMinutes(-10)
            }
        );

        dbContext.Games.AddRange(
            new Game
            {
                Id = olderGameId,
                Title = "Older active game",
                Status = GameStatusValue.Active,
                ActiveTeamId = olderTeamId,
                CreatedAtUtc = now.AddMinutes(-20),
                StartedAtUtc = now.AddMinutes(-20)
            },
            new Game
            {
                Id = latestGameId,
                Title = "Latest active game",
                Status = GameStatusValue.Active,
                ActiveTeamId = latestTeamId,
                CreatedAtUtc = now.AddMinutes(-5),
                StartedAtUtc = now.AddMinutes(-5)
            }
        );

        dbContext.GameTeamSlots.AddRange(
            new GameTeamSlot
            {
                Id = olderSlotId,
                GameId = olderGameId,
                SlotIndex = 1,
                SlotType = TeamSlotTypeValue.Public,
                CreatedAtUtc = now.AddMinutes(-20)
            },
            new GameTeamSlot
            {
                Id = latestSlotId,
                GameId = latestGameId,
                SlotIndex = 1,
                SlotType = TeamSlotTypeValue.Public,
                CreatedAtUtc = now.AddMinutes(-5)
            }
        );

        dbContext.GameTeams.AddRange(
            new GameTeam
            {
                Id = olderTeamId,
                GameId = olderGameId,
                SlotId = olderSlotId,
                RecruitmentOpen = false,
                Status = TeamStatusValue.Confirmed,
                CreatedByUserId = olderUserId,
                CreatedAtUtc = now.AddMinutes(-20),
                UpdatedAtUtc = now.AddMinutes(-20),
                ConfirmedAtUtc = now.AddMinutes(-20),
                ConfirmedByUserId = olderUserId
            },
            new GameTeam
            {
                Id = latestTeamId,
                GameId = latestGameId,
                SlotId = latestSlotId,
                RecruitmentOpen = false,
                Status = TeamStatusValue.Confirmed,
                CreatedByUserId = latestUserId,
                CreatedAtUtc = now.AddMinutes(-5),
                UpdatedAtUtc = now.AddMinutes(-5),
                ConfirmedAtUtc = now.AddMinutes(-5),
                ConfirmedByUserId = latestUserId
            }
        );

        dbContext.GameTeamMembers.AddRange(
            new GameTeamMember
            {
                Id = Guid.NewGuid(),
                GameId = olderGameId,
                TeamId = olderTeamId,
                UserId = olderUserId,
                JoinedAtUtc = now.AddMinutes(-20)
            },
            new GameTeamMember
            {
                Id = Guid.NewGuid(),
                GameId = latestGameId,
                TeamId = latestTeamId,
                UserId = latestUserId,
                JoinedAtUtc = now.AddMinutes(-5)
            }
        );

        dbContext.GameBoards.AddRange(
            new GameBoard
            {
                Id = olderBoardId,
                GameId = olderGameId,
                Rows = 1,
                Cols = 1,
                RowLabels = ["A"],
                ColLabels = ["1"],
                CreatedAtUtc = now.AddMinutes(-20)
            },
            new GameBoard
            {
                Id = latestBoardId,
                GameId = latestGameId,
                Rows = 1,
                Cols = 1,
                RowLabels = ["A"],
                ColLabels = ["1"],
                CreatedAtUtc = now.AddMinutes(-5)
            }
        );

        dbContext.BoardCells.AddRange(
            new BoardCell
            {
                Id = Guid.NewGuid(),
                BoardId = olderBoardId,
                RowIndex = 0,
                ColIndex = 0,
                Title = "Older Cell",
                Cost = 100,
                State = BoardCellState.Closed
            },
            new BoardCell
            {
                Id = Guid.NewGuid(),
                BoardId = latestBoardId,
                RowIndex = 0,
                ColIndex = 0,
                Title = "Latest Cell",
                Cost = 150,
                State = BoardCellState.Closed
            }
        );

        await dbContext.SaveChangesAsync();
        return latestTeamId;
    }

    private async Task SeedActiveGameWithEnabledModifiersAsync(
        IReadOnlyList<string> enabledCodes,
        IReadOnlyList<string>? alreadyActiveCodes = null
    )
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        dbContext.GameRoundModifierResults.RemoveRange(dbContext.GameRoundModifierResults);
        dbContext.GameRoundParticipants.RemoveRange(dbContext.GameRoundParticipants);
        dbContext.GameRounds.RemoveRange(dbContext.GameRounds);
        dbContext.GameTeamMembers.RemoveRange(dbContext.GameTeamMembers);
        dbContext.GameTeams.RemoveRange(dbContext.GameTeams);
        dbContext.GameTeamSlots.RemoveRange(dbContext.GameTeamSlots);
        dbContext.GameModifierActivations.RemoveRange(dbContext.GameModifierActivations);
        dbContext.GameEnabledModifiers.RemoveRange(dbContext.GameEnabledModifiers);
        dbContext.BoardCells.RemoveRange(dbContext.BoardCells);
        dbContext.GameBoards.RemoveRange(dbContext.GameBoards);
        dbContext.Games.RemoveRange(dbContext.Games);
        await dbContext.SaveChangesAsync();

        var now = DateTime.UtcNow;
        var enabledModifierIds = enabledCodes.Select(GetModifierId).ToArray();
        var currentVersionIds = await dbContext.ModifierDefinitions
            .Where(x => enabledModifierIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.CurrentVersionId);
        Assert.All(enabledModifierIds, id => Assert.True(currentVersionIds[id].HasValue));
        var gameId = Guid.NewGuid();
        var boardId = Guid.NewGuid();
        var cellId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var roundId = Guid.NewGuid();

        dbContext.Users.Add(
            new User
            {
                Id = userId,
                TwitchUserId = $"mod-seed-{userId:N}",
                Login = $"mod-seed-{userId:N}"[..32],
                DisplayName = "Modifier Seed",
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            }
        );

        dbContext.Games.Add(
            new Game
            {
                Id = gameId,
                Title = "Game with modifiers",
                Status = GameStatusValue.Active,
                ActiveTeamId = teamId,
                CreatedAtUtc = now,
                StartedAtUtc = now
            }
        );
        dbContext.GameTeamSlots.Add(
            new GameTeamSlot
            {
                Id = slotId,
                GameId = gameId,
                SlotIndex = 1,
                SlotType = TeamSlotTypeValue.Public,
                CreatedAtUtc = now
            }
        );
        dbContext.GameTeams.Add(
            new GameTeam
            {
                Id = teamId,
                GameId = gameId,
                SlotId = slotId,
                RecruitmentOpen = false,
                Status = TeamStatusValue.Confirmed,
                CreatedByUserId = userId,
                ConfirmedByUserId = userId,
                ConfirmedAtUtc = now,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            }
        );
        dbContext.GameTeamMembers.Add(
            new GameTeamMember
            {
                Id = Guid.NewGuid(),
                GameId = gameId,
                TeamId = teamId,
                UserId = userId,
                JoinedAtUtc = now
            }
        );
        dbContext.GameBoards.Add(
            new GameBoard
            {
                Id = boardId,
                GameId = gameId,
                Rows = 1,
                Cols = 1,
                RowLabels = ["A"],
                ColLabels = ["1"],
                CreatedAtUtc = now,
            }
        );
        dbContext.BoardCells.Add(
            new BoardCell
            {
                Id = cellId,
                BoardId = boardId,
                RowIndex = 0,
                ColIndex = 0,
                Title = "Cell",
                Cost = 100,
                State = BoardCellState.Open
            }
        );
        dbContext.GameEnabledModifiers.AddRange(
            enabledCodes.Select(
                code =>
                    new GameEnabledModifier
                    {
                        GameId = gameId,
                        ModifierId = GetModifierId(code),
                        ModifierVersionId = currentVersionIds[GetModifierId(code)],
                        VersionPinnedAtUtc = now,
                        EnabledAtUtc = now
                    }
            )
        );
        dbContext.GameModifierActivations.AddRange(
            (alreadyActiveCodes ?? [])
                .Select(
                    code =>
                        new backend.Data.Entities.GameModifierActivation
                        {
                            Id = Guid.NewGuid(),
                            GameId = gameId,
                            RoundId = roundId,
                            ModifierId = GetModifierId(code),
                            ModifierVersionId = currentVersionIds[GetModifierId(code)]!.Value,
                            ActivatedByUserId = userId,
                            InitiatedByUserId = userId,
                            ActivatedAtUtc = now
                        }
                )
        );
        dbContext.GameRounds.Add(
            new GameRound
            {
                Id = roundId,
                GameId = gameId,
                BoardId = boardId,
                BoardCellId = cellId,
                TeamId = teamId,
                Status = GameRoundStatusValue.AwaitingModifiers,
                BaseScore = 100,
                TeamSlotIndexSnapshot = 1,
                CellRowIndex = 0,
                CellColIndex = 0,
                CellTitleSnapshot = "Cell",
                CellCostSnapshot = 100,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            }
        );
        await dbContext.SaveChangesAsync();
    }

    private async Task SeedQuizPointsAsync(Guid userId, int points)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var now = DateTime.UtcNow;
        var gameId = await dbContext.Games
            .Where(x => x.Status == GameStatusValue.Active && !x.IsDeleted)
            .Select(x => x.Id)
            .SingleAsync();

        if (!await dbContext.Users.AnyAsync(x => x.Id == userId))
        {
            dbContext.Users.Add(
                new User
                {
                    Id = userId,
                    TwitchUserId = $"modifier-user-{userId:N}",
                    Login = $"modifier-user-{userId:N}"[..32],
                    DisplayName = "Modifier Player",
                    IsActive = true,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                }
            );
        }

        if (points > 0)
        {
            dbContext.GameQuizPointLedgerEntries.Add(
                new GameQuizPointLedgerEntry
                {
                    Id = Guid.NewGuid(),
                    GameId = gameId,
                    UserId = userId,
                    EntryType = GameQuizPointEntryTypeValue.ManualAdjustment,
                    PointsDelta = points,
                    ManualRequestId = Guid.NewGuid(),
                    CreatedByUserId = userId,
                    Reason = "Integration test balance",
                    AvailablePointsBefore = 0,
                    AvailablePointsAfter = points,
                    OccurredAtUtc = now
                }
            );
        }

        await dbContext.SaveChangesAsync();
    }

    private async Task SeedQuizRewardPointsAsync(Guid userId, int points)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var now = DateTime.UtcNow;
        var gameId = await dbContext.Games
            .Where(x => x.Status == GameStatusValue.Active && !x.IsDeleted)
            .Select(x => x.Id)
            .SingleAsync();
        var categoryId = Guid.NewGuid();
        var questionId = Guid.NewGuid();

        dbContext.Users.Add(
            new User
            {
                Id = userId,
                TwitchUserId = $"quiz-reward-{userId:N}",
                Login = $"quiz-reward-{userId:N}"[..32],
                DisplayName = "Quiz Reward Player",
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            }
        );
        dbContext.QuestionCategories.Add(
            new QuestionCategory
            {
                Id = categoryId,
                Name = $"reward-{categoryId:N}",
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            }
        );
        dbContext.QuestionDefinitions.Add(
            new QuestionDefinition
            {
                Id = questionId,
                ExternalCode = $"reward-{questionId:N}",
                CategoryId = categoryId,
                Text = "Legacy quiz question?",
                Reward = points,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                AcceptedAnswers =
                [
                    new QuestionAcceptedAnswer
                    {
                        Id = Guid.NewGuid(),
                        QuestionId = questionId,
                        AnswerText = "answer",
                        NormalizedAnswer = "answer",
                        IsPrimary = true,
                        SortOrder = 0,
                        CreatedAtUtc = now
                    }
                ]
            }
        );
        var quizRoundId = Guid.NewGuid();
        var correctAnswerId = Guid.NewGuid();
        dbContext.GameQuizRounds.Add(
            new GameQuizRound
            {
                Id = quizRoundId,
                GameId = gameId,
                QuestionId = questionId,
                AskOrder = 1,
                AskedAtUtc = now.AddMinutes(-1),
                ClosesAtUtc = now.AddMinutes(1),
                ClosedAtUtc = now,
                AskedByUserId = userId,
                Status = GameQuizRoundStatusValue.AnsweredCorrect,
                QuestionRevisionSnapshot = 1,
                QuestionCodeSnapshot = $"reward-{questionId:N}",
                CategoryNameSnapshot = $"reward-{categoryId:N}",
                QuestionTextSnapshot = "Quiz reward question?",
                AcceptedAnswersSnapshot = ["answer"],
                NormalizedAnswersSnapshot = ["answer"],
                RewardSnapshot = points,
                DeliveryKind = "manual"
            }
        );
        dbContext.GameQuizCorrectAnswers.Add(
            new GameQuizCorrectAnswer
            {
                Id = correctAnswerId,
                GameId = gameId,
                QuizRoundId = quizRoundId,
                AwardedToUserId = userId,
                CapturedByUserId = userId,
                TwitchUserIdSnapshot = $"quiz-reward-{userId:N}",
                LoginSnapshot = $"quiz-reward-{userId:N}"[..32],
                DisplayNameSnapshot = "Quiz Reward Player",
                SubmittedAnswer = "answer",
                NormalizedAnswer = "answer",
                SourceProvider = "manual",
                AnsweredAtUtc = now
            }
        );
        dbContext.GameQuizPointLedgerEntries.Add(
            new GameQuizPointLedgerEntry
            {
                Id = Guid.NewGuid(),
                GameId = gameId,
                UserId = userId,
                EntryType = GameQuizPointEntryTypeValue.QuizReward,
                PointsDelta = points,
                CorrectAnswerId = correctAnswerId,
                AvailablePointsBefore = 0,
                AvailablePointsAfter = points,
                OccurredAtUtc = now
            }
        );
        await dbContext.SaveChangesAsync();
    }

    private async Task EnsureModifierDefinitionsSeededAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var requiredModifierIds = new[]
        {
            ModifierDefinitionSeedIds.Chirik,
            ModifierDefinitionSeedIds.Zhazhda,
            ModifierDefinitionSeedIds.Prokaznik,
            ModifierDefinitionSeedIds.Mentorbait,
            ModifierDefinitionSeedIds.Feyerverk
        };
        if (await dbContext.ModifierDefinitions.CountAsync(x =>
                requiredModifierIds.Contains(x.Id) && x.CurrentVersionId != null)
            == requiredModifierIds.Length)
        {
            return;
        }

        dbContext.ModifierDefinitions.RemoveRange(dbContext.ModifierDefinitions);
        await dbContext.SaveChangesAsync();

        ModifierBehaviorV2 Behavior(string code) =>
            BuiltInModifierBehaviorCatalog.Get(code).Behavior;
        await TestModifierVersionFactory.AddAsync(dbContext,
        [
            new(ModifierDefinitionSeedIds.Chirik, "Чирик", "Test", "round", 3, 5,
                Behavior(BuiltInModifierBehaviorCatalog.Chirik),
                BuiltInModifierBehaviorCatalog.Get(BuiltInModifierBehaviorCatalog.Chirik)
                    .NormalizedTags.ToArray()),
            new(ModifierDefinitionSeedIds.Zhazhda, "Жажда", "Test", "result", 3, 2,
                Behavior(BuiltInModifierBehaviorCatalog.Zhazhda)),
            new(ModifierDefinitionSeedIds.Prokaznik, "Проказник", "Test", "round", 6, 2,
                Behavior(BuiltInModifierBehaviorCatalog.Prokaznik),
                ConflictingModifierIds: [ModifierDefinitionSeedIds.Mentorbait]),
            new(ModifierDefinitionSeedIds.Mentorbait, "Менторбайт", "Test", "round", 8, 1,
                Behavior(BuiltInModifierBehaviorCatalog.Mentorbait),
                ConflictingModifierIds: [ModifierDefinitionSeedIds.Prokaznik]),
            new(ModifierDefinitionSeedIds.Feyerverk, "Фейерверк", "Test", "round", 11, 1,
                Behavior(BuiltInModifierBehaviorCatalog.Feyerverk))
        ]);
    }

    private async Task<SeededModifierHistoryGame> SeedModifierHistoryGameAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        dbContext.GameRoundModifierResults.RemoveRange(dbContext.GameRoundModifierResults);
        dbContext.GameRoundParticipants.RemoveRange(dbContext.GameRoundParticipants);
        dbContext.GameRounds.RemoveRange(dbContext.GameRounds);
        dbContext.GameQuizRounds.RemoveRange(dbContext.GameQuizRounds);
        dbContext.GameEnabledQuestions.RemoveRange(dbContext.GameEnabledQuestions);
        dbContext.QuestionDefinitions.RemoveRange(dbContext.QuestionDefinitions);
        dbContext.QuestionCategories.RemoveRange(dbContext.QuestionCategories);
        dbContext.GameModifierActivations.RemoveRange(dbContext.GameModifierActivations);
        dbContext.GameEnabledModifiers.RemoveRange(dbContext.GameEnabledModifiers);
        dbContext.ModifierDefinitions.RemoveRange(dbContext.ModifierDefinitions);
        dbContext.BoardCells.RemoveRange(dbContext.BoardCells);
        dbContext.GameBoards.RemoveRange(dbContext.GameBoards);
        dbContext.Games.RemoveRange(dbContext.Games);
        dbContext.Users.RemoveRange(dbContext.Users);
        await dbContext.SaveChangesAsync();

        var now = DateTime.UtcNow;
        var gameId = Guid.NewGuid();
        var boardId = Guid.NewGuid();
        var cellId = Guid.NewGuid();
        var activationId = Guid.NewGuid();
        var modifierId = Guid.NewGuid();
        var playerId = Guid.NewGuid();
        var moderatorId = Guid.NewGuid();
        var roundId = Guid.NewGuid();

        dbContext.Users.AddRange(
            new User
            {
                Id = playerId,
                TwitchUserId = "modifier-history-player",
                Login = "modifier-history-player",
                DisplayName = "Modifier History Player",
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new User
            {
                Id = moderatorId,
                TwitchUserId = "modifier-history-admin",
                Login = "modifier-history-admin",
                DisplayName = "Modifier History Admin",
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            }
        );

        dbContext.Games.Add(
            new Game
            {
                Id = gameId,
                Title = "Modifier Applied Game",
                Status = GameStatusValue.Active,
                CreatedAtUtc = now.AddHours(-1),
                StartedAtUtc = now.AddMinutes(-45)
            }
        );

        dbContext.GameBoards.Add(
            new GameBoard
            {
                Id = boardId,
                GameId = gameId,
                Rows = 1,
                Cols = 1,
                RowLabels = ["A"],
                ColLabels = ["1"],
                Version = 1,
                CreatedAtUtc = now.AddHours(-1)
            }
        );

        dbContext.BoardCells.Add(
            new BoardCell
            {
                Id = cellId,
                BoardId = boardId,
                RowIndex = 0,
                ColIndex = 0,
                Title = "Applied Cell",
                Cost = 100,
                State = BoardCellState.Open
            }
        );

        await TestModifierVersionFactory.AddAsync(
            dbContext,
            new TestModifierSpec(
                modifierId,
                "Applied modifier",
                "Used in round",
                GameModifierCategories.Round,
                3,
                1,
                BuiltInModifierBehaviorCatalog.Get(BuiltInModifierBehaviorCatalog.Chirik).Behavior),
            now);

        dbContext.GameEnabledModifiers.Add(
            new GameEnabledModifier
            {
                GameId = gameId,
                ModifierId = modifierId,
                EnabledAtUtc = now.AddMinutes(-40)
            }
        );

        dbContext.GameModifierActivations.Add(
            new backend.Data.Entities.GameModifierActivation
            {
                Id = activationId,
                GameId = gameId,
                RoundId = roundId,
                ModifierId = modifierId,
                ActivatedByUserId = playerId,
                InitiatedByUserId = playerId,
                ActivationCostSnapshot = 3,
                DefinitionRevisionSnapshot = 1,
                ModifierNameSnapshot = "Applied modifier",
                ModifierDescriptionSnapshot = "Used in round",
                ModifierCategorySnapshot = GameModifierCategories.Round,
                NormalizedTagsSnapshot = ["test"],
                BehaviorV2SnapshotJson = ModifierBehaviorV2Json.Serialize(BuiltInModifierBehaviorCatalog.Get(BuiltInModifierBehaviorCatalog.Chirik).Behavior),
                ActivatedAtUtc = now.AddMinutes(-35),
                Status = GameModifierActivationStatusValue.Consumed,
                ArchivedAtUtc = now.AddMinutes(-20)
            }
        );

        dbContext.GameRounds.Add(
            new GameRound
            {
                Id = roundId,
                GameId = gameId,
                BoardId = boardId,
                BoardCellId = cellId,
                TeamId = Guid.NewGuid(),
                Status = GameRoundStatusValue.Completed,
                FinishedAtUtc = now.AddMinutes(-20),
                BaseScore = 100,
                FinalScore = 100,
                TeamSlotIndexSnapshot = 1,
                CellRowIndex = 0,
                CellColIndex = 0,
                CellTitleSnapshot = "Applied Cell",
                CellCostSnapshot = 100,
                ResolvedByUserId = moderatorId,
                CreatedAtUtc = now.AddMinutes(-30),
                UpdatedAtUtc = now.AddMinutes(-20)
            }
        );

        dbContext.GameRoundModifierResults.Add(
            new GameRoundModifierResult
            {
                Id = Guid.NewGuid(),
                RoundId = roundId,
                GameModifierActivationId = activationId,
                ModifierId = modifierId,
                ModifierNameSnapshot = "Applied modifier",
                ModifierCategorySnapshot = GameModifierCategories.Round,
                OutcomeStatus = "applied",
                ScoreDelta = 0,
                KillDelta = 0,
                ResolvedByUserId = moderatorId,
                ResolvedAtUtc = now.AddMinutes(-20),
                CreatedAtUtc = now.AddMinutes(-20),
                UpdatedAtUtc = now.AddMinutes(-20)
            }
        );

        await dbContext.SaveChangesAsync();
        return new SeededModifierHistoryGame(gameId, activationId);
    }

    private static Guid GetModifierId(string code) =>
        code switch
        {
            "chirik" => ModifierDefinitionSeedIds.Chirik,
            "zhazhda" => ModifierDefinitionSeedIds.Zhazhda,
            "prokaznik" => ModifierDefinitionSeedIds.Prokaznik,
            "mentorbait" => ModifierDefinitionSeedIds.Mentorbait,
            "feyerverk" => ModifierDefinitionSeedIds.Feyerverk,
            _ => throw new InvalidOperationException($"Unknown modifier seed code '{code}'.")
        };

    private static async Task<GameModifierDefinitionDto> CreateModifierAsync(
        HttpClient adminClient,
        string name
    )
    {
        var response = await adminClient.PostAsJsonAsync(
            "/api/game/modifiers",
            CreateRuleOnlyModifierRequest(name)
        );
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return Assert.IsType<GameModifierDefinitionDto>(
            await response.Content.ReadFromJsonAsync<GameModifierDefinitionDto>()
        );
    }

    private static CreateGameModifierRequestDto CreateRuleOnlyModifierRequest(string name) =>
        new(
            name,
            "Rule-only modifier for integration tests.",
            GameModifierCategories.Round,
            5,
            new GameModifierActivationLimitDto(1),
            [],
            null,
            null,
            null,
            CreateRuleBehaviorDto()
        );

    private static UpdateGameModifierRequestDto CreateRuleOnlyUpdateRequest(
        string name,
        int expectedRevision,
        string? changeNote = null,
        string[]? conflictingModifierIds = null
    ) =>
        new(
            name,
            "Rule-only modifier for integration tests.",
            GameModifierCategories.Round,
            5,
            new GameModifierActivationLimitDto(1),
            conflictingModifierIds ?? [],
            null,
            null,
            null,
            CreateRuleBehaviorDto(),
            expectedRevision,
            changeNote
        );

    private static GameModifierBehaviorV2Dto CreateRuleBehaviorDto() =>
        new(
            2,
            "rule",
            "round",
            "activeTeam",
            false,
            "Rule-only modifier for integration tests.",
            "aggregateParameters",
            new GameModifierRuleStatusResolutionDto(),
            "none",
            null
        );

    private async Task SeedActiveGameForQuestionsAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        dbContext.GameQuizRounds.RemoveRange(dbContext.GameQuizRounds);
        dbContext.GameEnabledQuestions.RemoveRange(dbContext.GameEnabledQuestions);
        dbContext.QuestionDefinitions.RemoveRange(dbContext.QuestionDefinitions);
        dbContext.QuestionCategories.RemoveRange(dbContext.QuestionCategories);
        dbContext.GameModifierActivations.RemoveRange(dbContext.GameModifierActivations);
        dbContext.GameEnabledModifiers.RemoveRange(dbContext.GameEnabledModifiers);
        dbContext.BoardCells.RemoveRange(dbContext.BoardCells);
        dbContext.GameBoards.RemoveRange(dbContext.GameBoards);
        dbContext.Games.RemoveRange(dbContext.Games);
        await dbContext.SaveChangesAsync();

        var now = DateTime.UtcNow;
        dbContext.Games.Add(
            new Game
            {
                Id = Guid.NewGuid(),
                Title = "Questions Active Game",
                Status = GameStatusValue.Active,
                CreatedAtUtc = now,
                StartedAtUtc = now
            }
        );
        await dbContext.SaveChangesAsync();
    }

    private async Task SeedQuestionCatalogWithQuestionsAsync(IReadOnlyList<SeedQuestionItem> questions)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        dbContext.GameQuizRounds.RemoveRange(dbContext.GameQuizRounds);
        dbContext.QuestionDefinitions.RemoveRange(dbContext.QuestionDefinitions);
        dbContext.QuestionCategories.RemoveRange(dbContext.QuestionCategories);
        await dbContext.SaveChangesAsync();

        var now = DateTime.UtcNow;
        var categories = questions
            .Select(question => question.Category)
            .Distinct(StringComparer.Ordinal)
            .Select(
                category =>
                    new QuestionCategory
                    {
                        Id = Guid.NewGuid(),
                        Name = category,
                        CreatedAtUtc = now,
                        UpdatedAtUtc = now
                    }
            )
            .ToArray();
        dbContext.QuestionCategories.AddRange(categories);
        var categoryIdByName = categories.ToDictionary(
            category => category.Name,
            category => category.Id,
            StringComparer.Ordinal
        );

        var priority = 1;
        var seeded = new List<QuestionDefinition>();
        foreach (var question in questions)
        {
            var questionId = Guid.NewGuid();
            var normalizedAnswer = NormalizeAnswer(question.Answer);
            var definition = new QuestionDefinition
            {
                Id = questionId,
                ExternalCode = question.QuestionCode,
                CategoryId = categoryIdByName[question.Category],
                Text = question.Text,
                Reward = question.Reward,
                Revision = 1,
                IsEnabled = true,
                Priority = question.Priority ?? priority++,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                AcceptedAnswers =
                [
                    new QuestionAcceptedAnswer
                    {
                        Id = Guid.NewGuid(),
                        QuestionId = questionId,
                        AnswerText = question.Answer,
                        NormalizedAnswer = normalizedAnswer,
                        IsPrimary = true,
                        SortOrder = 0,
                        CreatedAtUtc = now
                    }
                ]
            };
            dbContext.QuestionDefinitions.Add(definition);
            seeded.Add(definition);
        }

        await dbContext.SaveChangesAsync();

        // Questions only become askable once selected for the active game
        // (per-game question selection). Mirror that here so ask-next tests
        // exercise a fully-configured active game.
        var activeGameId = await dbContext.Games
            .Where(game => game.Status == GameStatusValue.Active && !game.IsDeleted)
            .Select(game => (Guid?)game.Id)
            .FirstOrDefaultAsync();
        if (activeGameId is Guid gameId)
        {
            foreach (var definition in seeded)
            {
                dbContext.GameEnabledQuestions.Add(
                    new GameEnabledQuestion
                    {
                        GameId = gameId,
                        QuestionId = definition.Id,
                        EnabledAtUtc = now,
                        QuestionRevisionSnapshot = definition.Revision,
                        QuestionCodeSnapshot = definition.ExternalCode,
                        CategoryNameSnapshot = definition.CategoryDefinition?.Name
                            ?? categories.Single(category => category.Id == definition.CategoryId).Name,
                        QuestionTextSnapshot = definition.Text,
                        AcceptedAnswersSnapshot = definition.AcceptedAnswers
                            .OrderBy(answer => answer.SortOrder)
                            .Select(answer => answer.AnswerText)
                            .ToArray(),
                        NormalizedAnswersSnapshot = definition.AcceptedAnswers
                            .OrderBy(answer => answer.SortOrder)
                            .Select(answer => answer.NormalizedAnswer)
                            .ToArray(),
                        RewardSnapshot = definition.Reward,
                        PrioritySnapshot = definition.Priority,
                        SnapshotAtUtc = now
                    }
                );
            }

            await dbContext.SaveChangesAsync();
        }
    }

    private static string NormalizeAnswer(string answer)
    {
        return string.Join(
            " ",
            answer.Trim().ToLowerInvariant().Replace('ё', 'е').Split(' ', StringSplitOptions.RemoveEmptyEntries)
        );
    }

    private sealed record SeedQuestionItem(
        string QuestionCode,
        string Category,
        string Text,
        string Answer,
        int Reward,
        int? Priority = null
    );

    private sealed record SeededModifierHistoryGame(Guid GameId, Guid ActivationId);

    private static MultipartFormDataContent CreateJsonImportContent(string content, string fileName)
    {
        var multipart = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(content));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        multipart.Add(fileContent, "file", fileName);
        return multipart;
    }

    private async Task SeedInactiveUserAsync(Guid userId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.Users.Add(
            new User
            {
                Id = userId,
                TwitchUserId = $"inactive-{userId:N}",
                Login = "inactive-viewer",
                DisplayName = "Inactive Viewer",
                IsActive = false,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            }
        );
        await dbContext.SaveChangesAsync();
    }

    private async Task SeedActiveUserAsync(Guid userId, string login, string displayName)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.Users.Add(
            new User
            {
                Id = userId,
                TwitchUserId = $"{login}-{userId:N}",
                Login = login,
                DisplayName = displayName,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            }
        );
        await dbContext.SaveChangesAsync();
    }

    private HttpClient CreateAuthenticatedClient(
        string[] roles,
        Guid? userId = null,
        RecordingGameBoardEventsPublisher? publisher = null,
        IGameBoardService? gameBoardService = null
    )
    {
        var authenticatedFactory = CreateAuthenticatedFactory(roles, userId, publisher, gameBoardService);
        return authenticatedFactory.CreateClient();
    }

    private WebApplicationFactory<Program> CreateAuthenticatedFactory(
        string[] roles,
        Guid? userId = null,
        RecordingGameBoardEventsPublisher? publisher = null,
        IGameBoardService? gameBoardService = null
    )
    {
        var authenticatedFactory = _factory.WithWebHostBuilder(
            builder =>
                builder.ConfigureServices(
                    services =>
                    {
                        services
                            .RemoveAll<IClaimsTransformation>();
                        services.AddSingleton<IClaimsTransformation, PassthroughClaimsTransformation>();
                        if (publisher is not null)
                        {
                            services.RemoveAll<IGameBoardEventsPublisher>();
                            services.AddSingleton<IGameBoardEventsPublisher>(publisher);
                        }

                        if (gameBoardService is not null)
                        {
                            services.RemoveAll<IGameBoardService>();
                            services.AddSingleton(gameBoardService);
                        }

                        services
                            .AddAuthentication(options =>
                            {
                                options.DefaultAuthenticateScheme = TestAuthenticationHandler.SchemeName;
                                options.DefaultChallengeScheme = TestAuthenticationHandler.SchemeName;
                                options.DefaultScheme = TestAuthenticationHandler.SchemeName;
                            })
                            .AddScheme<TestAuthSchemeOptions, TestAuthenticationHandler>(
                                TestAuthenticationHandler.SchemeName,
                                options =>
                                {
                                    options.ClaimsIssuer = string.Join(',', roles);
                                    options.UserId = userId;
                                }
                            );
                    }
                )
        );

        return authenticatedFactory;
    }

    private sealed class TestAuthSchemeOptions : AuthenticationSchemeOptions
    {
        public Guid? UserId { get; set; }
    }

    private sealed class TestAuthenticationHandler : AuthenticationHandler<TestAuthSchemeOptions>
    {
        public const string SchemeName = "TestAuth";

        public TestAuthenticationHandler(
            IOptionsMonitor<TestAuthSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder
        )
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var roles = (Options.ClaimsIssuer ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var userId = Options.UserId ?? Guid.NewGuid();
            var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId.ToString()) };
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var identity = new ClaimsIdentity(
                claims,
                SchemeName
            );
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, SchemeName);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }

    private sealed class PassthroughClaimsTransformation : IClaimsTransformation
    {
        public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            return Task.FromResult(principal);
        }
    }

    private sealed class RecordingGameBoardEventsPublisher : IGameBoardEventsPublisher
    {
        public List<GameCellOpenedEvent> PublishedEvents { get; } = [];
        public List<GameModifierActivatedEvent> PublishedModifierEvents { get; } = [];
        public List<GameModifierActivationCancelledEvent> PublishedModifierCancelledEvents { get; } =
            [];
        public List<GameModifierAvailabilityChangedEvent> PublishedModifierAvailabilityEvents { get; } =
            [];
        public List<ModifierCatalogChangedEvent> PublishedModifierCatalogChangedEvents { get; } = [];
        public List<GameRoundStateChangedEvent> PublishedRoundStateChangedEvents { get; } = [];
        public List<GameQuizStateChangedEvent> PublishedQuizStateChangedEvents { get; } = [];
        public List<GameUserNotificationCreatedEvent> PublishedUserNotificationEvents { get; } = [];
        public List<GameLifecycleChangedEvent> PublishedLifecycleChangedEvents { get; } = [];

        public Task PublishCellOpenedAsync(
            GameCellOpenedEvent @event,
            CancellationToken cancellationToken = default
        )
        {
            PublishedEvents.Add(@event);
            return Task.CompletedTask;
        }

        public Task PublishModifierActivatedAsync(
            GameModifierActivatedEvent @event,
            CancellationToken cancellationToken = default
        )
        {
            PublishedModifierEvents.Add(@event);
            return Task.CompletedTask;
        }

        public Task PublishModifierActivationCancelledAsync(
            GameModifierActivationCancelledEvent @event,
            CancellationToken cancellationToken = default
        )
        {
            PublishedModifierCancelledEvents.Add(@event);
            return Task.CompletedTask;
        }

        public Task PublishModifierAvailabilityChangedAsync(
            GameModifierAvailabilityChangedEvent @event,
            CancellationToken cancellationToken = default
        )
        {
            PublishedModifierAvailabilityEvents.Add(@event);
            return Task.CompletedTask;
        }

        public Task PublishModifierCatalogChangedAsync(
            ModifierCatalogChangedEvent @event,
            CancellationToken cancellationToken = default
        )
        {
            PublishedModifierCatalogChangedEvents.Add(@event);
            return Task.CompletedTask;
        }

        public Task PublishRoundStateChangedAsync(
            GameRoundStateChangedEvent @event,
            CancellationToken cancellationToken = default
        )
        {
            PublishedRoundStateChangedEvents.Add(@event);
            return Task.CompletedTask;
        }

        public Task PublishQuizStateChangedAsync(
            GameQuizStateChangedEvent @event,
            CancellationToken cancellationToken = default
        )
        {
            PublishedQuizStateChangedEvents.Add(@event);
            return Task.CompletedTask;
        }

        public Task PublishUserNotificationCreatedAsync(
            GameUserNotificationCreatedEvent @event,
            CancellationToken cancellationToken = default
        )
        {
            PublishedUserNotificationEvents.Add(@event);
            return Task.CompletedTask;
        }

        public Task PublishGameLifecycleChangedAsync(
            GameLifecycleChangedEvent @event,
            CancellationToken cancellationToken = default
        )
        {
            PublishedLifecycleChangedEvents.Add(@event);
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingGameBoardService : IGameBoardService
    {
        public Task<GameBoardSnapshot?> GetCurrentBoardAsync(CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Simulated game board failure.");

        public Task<GameTeamQueueResult> GetCurrentTeamQueueAsync(
            CancellationToken cancellationToken = default
        ) => throw new InvalidOperationException("Simulated game board failure.");

        public Task<SetActiveGameTeamOutcome> SetActiveTeamAsync(
            Guid? teamId,
            CancellationToken cancellationToken = default
        ) => throw new InvalidOperationException("Simulated game board failure.");

        public Task<SetGameTeamPlayedStateOutcome> SetGameTeamPlayedStateAsync(
            Guid teamId,
            bool isPlayed,
            CancellationToken cancellationToken = default
        ) => throw new InvalidOperationException("Simulated game board failure.");

        public Task<bool> CurrentActiveGameHasActiveTeamAsync(
            CancellationToken cancellationToken = default
        ) => throw new InvalidOperationException("Simulated game board failure.");

        public Task<bool> CurrentActiveGameHasActiveRoundAsync(
            CancellationToken cancellationToken = default
        ) => throw new InvalidOperationException("Simulated game board failure.");

        public Task<bool> IsCurrentActiveGameCellAsync(
            Guid cellId,
            CancellationToken cancellationToken = default
        ) => throw new InvalidOperationException("Simulated game board failure.");

        public Task<OpenGameCellResult?> TryOpenCellAsync(
            Guid cellId,
            CancellationToken cancellationToken = default
        ) => throw new InvalidOperationException("Simulated game board failure.");
    }
}
