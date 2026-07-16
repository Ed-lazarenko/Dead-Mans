using System.Net;
using System.Net.Http.Json;
using backend.Api.Contracts;
using backend.Application.Abstractions.Auth;
using backend.Data;
using backend.Data.Entities;
using backend.Domain.Persistence;
using backend.Messaging;
using Backend.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Tests.Integration.GameEndpoints;

public sealed class GameCardRunContractTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public GameCardRunContractTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Start_WhenModeratorAndConfirmedTeam_ReturnsCreatedRunAndPersistsSnapshots()
    {
        var seeded = await SeedActiveGameAsync();
        using var client = TestAuthClientFactory.CreateClient(
            _factory,
            [AuthRoleCodes.Moderator],
            userId: seeded.ModeratorId
        );

        var response = await client.PostAsJsonAsync(
            "/api/game/card-runs",
            new StartGameCardRunRequestDto(seeded.CellId.ToString(), seeded.TeamId.ToString())
        );

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<GameCardRunDetailsDto>();
        Assert.NotNull(payload);
        Assert.Equal(GameCardRunStatusValue.InProgress, payload.Status);
        Assert.Equal(120, payload.BaseScore);
        Assert.Equal(2, payload.Participants.Count);
        Assert.Single(payload.ModifierResults);
        Assert.Equal(GameCardRunModifierOutcomeValue.Pending, payload.ModifierResults[0].OutcomeStatus);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(1, await dbContext.GameCardRuns.CountAsync());
        Assert.Equal(2, await dbContext.GameCardRunParticipants.CountAsync());
        Assert.Equal(1, await dbContext.GameCardRunModifierResults.CountAsync());
    }

    [Fact]
    public async Task Start_WhenAnotherRunAlreadyInProgress_ReturnsConflict()
    {
        var seeded = await SeedActiveGameAsync();
        await SeedInProgressRunAsync(seeded);
        using var client = TestAuthClientFactory.CreateClient(
            _factory,
            [AuthRoleCodes.Moderator],
            userId: seeded.ModeratorId
        );

        var response = await client.PostAsJsonAsync(
            "/api/game/card-runs",
            new StartGameCardRunRequestDto(seeded.CellId.ToString(), seeded.TeamId.ToString())
        );

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal(AppMessages.ErrorCodes.GameCardRunAlreadyInProgress, payload.Code);
    }

    [Fact]
    public async Task Finalize_WhenAdmin_ReturnsCompletedRunAndCancelsUnresolvedModifiers()
    {
        var seeded = await SeedActiveGameAsync();
        var startResponse = await StartRunAsync(seeded);
        var started = await startResponse.Content.ReadFromJsonAsync<GameCardRunDetailsDto>();
        Assert.NotNull(started);

        using var client = TestAuthClientFactory.CreateClient(
            _factory,
            [AuthRoleCodes.Admin],
            userId: seeded.ModeratorId
        );

        var response = await client.PostAsJsonAsync(
            $"/api/game/card-runs/{started.CardRunId}/finalize",
            new FinalizeGameCardRunRequestDto(
                GameCardRunStatusValue.Completed,
                null,
                "Clean finish",
                [
                    new FinalizeGameCardRunModifierRequestDto(
                        started.ModifierResults[0].ModifierResultId,
                        GameCardRunModifierOutcomeValue.Completed,
                        30,
                        1,
                        1.5m,
                        "{\"kills\":2}"
                    )
                ]
            )
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<GameCardRunDetailsDto>();
        Assert.NotNull(payload);
        Assert.Equal(GameCardRunStatusValue.Completed, payload.Status);
        Assert.Equal(150, payload.FinalScore);
        Assert.Equal("Clean finish", payload.Notes);
        Assert.Single(payload.ModifierResults);
        Assert.Equal(GameCardRunModifierOutcomeValue.Completed, payload.ModifierResults[0].OutcomeStatus);
        Assert.Equal(30, payload.ModifierResults[0].ScoreDelta);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var run = await dbContext.GameCardRuns.SingleAsync();
        Assert.Equal(GameCardRunStatusValue.Completed, run.Status);
        Assert.Equal(150, run.FinalScore);
        Assert.NotNull(run.FinishedAtUtc);
    }

    [Fact]
    public async Task GetActive_WhenRunExists_ReturnsCurrentInProgressRun()
    {
        var seeded = await SeedActiveGameAsync();
        var startResponse = await StartRunAsync(seeded);
        var started = await startResponse.Content.ReadFromJsonAsync<GameCardRunDetailsDto>();
        Assert.NotNull(started);

        using var client = TestAuthClientFactory.CreateClient(
            _factory,
            [AuthRoleCodes.Moderator],
            userId: seeded.ModeratorId
        );

        var response = await client.GetAsync("/api/game/card-runs/active");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<GameCardRunDetailsDto>();
        Assert.NotNull(payload);
        Assert.Equal(started.CardRunId, payload.CardRunId);
        Assert.Equal(GameCardRunStatusValue.InProgress, payload.Status);
    }

    [Fact]
    public async Task GetActive_WhenViewer_ReturnsCurrentInProgressRun()
    {
        var seeded = await SeedActiveGameAsync();
        var startResponse = await StartRunAsync(seeded);
        var started = await startResponse.Content.ReadFromJsonAsync<GameCardRunDetailsDto>();
        Assert.NotNull(started);

        using var client = TestAuthClientFactory.CreateClient(
            _factory,
            [AuthRoleCodes.Viewer],
            userId: Guid.NewGuid()
        );

        var response = await client.GetAsync("/api/game/card-runs/active");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<GameCardRunDetailsDto>();
        Assert.NotNull(payload);
        Assert.Equal(started.CardRunId, payload.CardRunId);
        Assert.Equal(GameCardRunStatusValue.InProgress, payload.Status);
    }

    [Fact]
    public async Task GetEligibleTeams_WhenModerator_ReturnsConfirmedTeamsWithParticipants()
    {
        var seeded = await SeedActiveGameAsync();
        using var client = TestAuthClientFactory.CreateClient(
            _factory,
            [AuthRoleCodes.Moderator],
            userId: seeded.ModeratorId
        );

        var response = await client.GetAsync("/api/game/card-runs/teams");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<IReadOnlyList<GameCardRunTeamOptionDto>>();
        Assert.NotNull(payload);
        var team = Assert.Single(payload);
        Assert.Equal(seeded.TeamId.ToString(), team.TeamId);
        Assert.Equal(1, team.TeamSlotIndex);
        Assert.Equal(2, team.Participants.Count);
    }

    private async Task<HttpResponseMessage> StartRunAsync(SeededActiveGame seeded)
    {
        using var client = TestAuthClientFactory.CreateClient(
            _factory,
            [AuthRoleCodes.Moderator],
            userId: seeded.ModeratorId
        );

        return await client.PostAsJsonAsync(
            "/api/game/card-runs",
            new StartGameCardRunRequestDto(seeded.CellId.ToString(), seeded.TeamId.ToString())
        );
    }

    private async Task<SeededActiveGame> SeedActiveGameAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        dbContext.GameCardRunModifierResults.RemoveRange(dbContext.GameCardRunModifierResults);
        dbContext.GameCardRunParticipants.RemoveRange(dbContext.GameCardRunParticipants);
        dbContext.GameCardRuns.RemoveRange(dbContext.GameCardRuns);
        dbContext.GameTeamMembers.RemoveRange(dbContext.GameTeamMembers);
        dbContext.GameTeams.RemoveRange(dbContext.GameTeams);
        dbContext.GameParticipationSlots.RemoveRange(dbContext.GameParticipationSlots);
        dbContext.GameQuestionRounds.RemoveRange(dbContext.GameQuestionRounds);
        dbContext.GameQuestionSelections.RemoveRange(dbContext.GameQuestionSelections);
        dbContext.QuestionDefinitions.RemoveRange(dbContext.QuestionDefinitions);
        dbContext.QuestionCategories.RemoveRange(dbContext.QuestionCategories);
        dbContext.GameActiveModifiers.RemoveRange(dbContext.GameActiveModifiers);
        dbContext.GameModifierSelections.RemoveRange(dbContext.GameModifierSelections);
        dbContext.ModifierConflicts.RemoveRange(dbContext.ModifierConflicts);
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
        var slotId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var alphaId = Guid.NewGuid();
        var bravoId = Guid.NewGuid();
        var moderatorId = Guid.NewGuid();
        var modifierId = Guid.NewGuid();
        var activeModifierId = Guid.NewGuid();

        dbContext.Users.AddRange(
            new User
            {
                Id = alphaId,
                TwitchUserId = "alpha-user",
                Login = "alpha",
                DisplayName = "Alpha",
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new User
            {
                Id = bravoId,
                TwitchUserId = "bravo-user",
                Login = "bravo",
                DisplayName = "Bravo",
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new User
            {
                Id = moderatorId,
                TwitchUserId = "mod-user",
                Login = "mod",
                DisplayName = "Moderator",
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            }
        );

        dbContext.Games.Add(
            new Game
            {
                Id = gameId,
                Title = "Runtime Match",
                Status = GameStatusValue.Active,
                CreatedAtUtc = now.AddHours(-1),
                ReadyAtUtc = now.AddMinutes(-50),
                StartedAtUtc = now.AddMinutes(-40)
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
                Version = 2,
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
                Title = "Main Card",
                Cost = 120,
                State = BoardCellState.Open
            }
        );

        dbContext.GameParticipationSlots.Add(
            new GameParticipationSlot
            {
                Id = slotId,
                GameId = gameId,
                SlotIndex = 1,
                Availability = "open",
                CreatedAtUtc = now.AddMinutes(-55)
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
                CreatedByUserId = moderatorId,
                ConfirmedByUserId = moderatorId,
                ConfirmedAtUtc = now.AddMinutes(-30),
                CreatedAtUtc = now.AddMinutes(-35),
                UpdatedAtUtc = now.AddMinutes(-30)
            }
        );

        dbContext.GameTeamMembers.AddRange(
            new GameTeamMember
            {
                Id = Guid.NewGuid(),
                GameId = gameId,
                TeamId = teamId,
                UserId = alphaId,
                JoinedAtUtc = now.AddMinutes(-34)
            },
            new GameTeamMember
            {
                Id = Guid.NewGuid(),
                GameId = gameId,
                TeamId = teamId,
                UserId = bravoId,
                JoinedAtUtc = now.AddMinutes(-33)
            }
        );

        dbContext.ModifierDefinitions.Add(
            new ModifierDefinition
            {
                Id = modifierId,
                Name = "Momentum",
                Description = "Bonus score modifier",
                ScoringType = "conditional_bonus",
                Category = "round",
                MetadataJson = "{\"effect\":{\"mechanicType\":\"multiplier\"}}",
                ActivationCost = 5,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            }
        );

        dbContext.GameActiveModifiers.Add(
            new GameActiveModifier
            {
                Id = activeModifierId,
                GameId = gameId,
                ModifierId = modifierId,
                ActivatedByUserId = moderatorId,
                ActivatedAtUtc = now.AddMinutes(-10)
            }
        );

        await dbContext.SaveChangesAsync();

        return new SeededActiveGame(gameId, cellId, teamId, moderatorId);
    }

    private async Task SeedInProgressRunAsync(SeededActiveGame seeded)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.GameCardRuns.Add(
            new GameCardRun
            {
                Id = Guid.NewGuid(),
                GameId = seeded.GameId,
                BoardCellId = seeded.CellId,
                TeamId = seeded.TeamId,
                Status = GameCardRunStatusValue.InProgress,
                StartedAtUtc = DateTime.UtcNow.AddMinutes(-5),
                BaseScore = 120,
                TeamSlotIndexSnapshot = 1,
                CellRowIndex = 0,
                CellColIndex = 0,
                CellTitleSnapshot = "Main Card",
                CellCostSnapshot = 120,
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(-5),
                UpdatedAtUtc = DateTime.UtcNow.AddMinutes(-5)
            }
        );
        await dbContext.SaveChangesAsync();
    }

    private sealed record SeededActiveGame(Guid GameId, Guid CellId, Guid TeamId, Guid ModeratorId);
}
